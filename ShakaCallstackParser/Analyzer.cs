using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class Analyzer
    {
        private const int kDefaultCrfValue = 28;
        private const double kTargetSSIMValue = 0.9870;
        private const double kTargetSSIMRangeMin = 0.9869;
        private const double kTargetSSIMRangeMax = 0.9875;

        public class Callbacks
        {
            public delegate void OnAnalyzeFinished(int index, int crf);
            public delegate void OnCalculated(int index, int crf, double ssim);

            public Callbacks(OnCalculated c, OnAnalyzeFinished af)
            {
                analyze_finished = af;
                calculated = c;
            }

            public OnAnalyzeFinished analyze_finished;
            public OnCalculated calculated;
        }
        Callbacks callbacks_;

        SSIMCalculator ssim_calculator_;

        List<AnalyzeJob> analyze_jobs_;

        bool is_analyzing_ = false;
        bool is_canceled_ = false;
        int current_analyze_index_ = -1;

        public Analyzer(Callbacks callback)
        {
            callbacks_ = callback;
            ssim_calculator_ = new SSIMCalculator(new SSIMCalculator.Callbacks(OnCalcuateFinished));
        }

        public bool Analyze(int index, string path, int thread_num)
        {
            if (is_analyzing_)
            {
                return false;
            }
            is_analyzing_ = true;
            is_canceled_ = false;
            current_analyze_index_ = 0;
            analyze_jobs_ = new List<AnalyzeJob>();

            AnalyzeTimeSelector selector = new AnalyzeTimeSelector();
            List<AnalyzeTimeSelector.TimePair> time_pair = selector.Calculate(path);
            {
                // Log

                string msg = Path.GetFileName(path) + " time: ";
                for (int i = 0; i < time_pair.Count(); i++)
                {
                    msg += "[" + time_pair[i].start_time + " : " + time_pair[i].duration + "] ";
                }
                Loger.Write(msg);
            }

            // Should be descending order!
            analyze_jobs_.Add(new AnalyzeJob(index, path, thread_num, 28, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(index, path, thread_num, 27, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(index, path, thread_num, 26, time_pair));

            CalculateSSIM(analyze_jobs_[0]);

            return true;
        }

        public void OnEncodeCanceled()
        {
            is_analyzing_ = false;
            is_canceled_ = true;
            ssim_calculator_.OnEncodeCanceled();
        }

        public void OnWindowClosed()
        {
            is_analyzing_ = false;
            is_canceled_ = true;
            ssim_calculator_.OnWindowClosed();
        }

        private int CalculateSSIM(AnalyzeJob job)
        {
            ssim_calculator_.Calculate(job.index, job.path, job.thread_num, job.crf, job.time_pair_list);
            return 0;
        }

        private void AnalyzeFinished()
        {
            analyze_jobs_.Clear();
            current_analyze_index_ = -1;
            is_analyzing_ = false;
        }

        private void OnCalcuateFinished(int _index, int crf, double ssim)
        {
            if (is_canceled_)
            {
                return;
            }

            callbacks_.calculated(_index, crf, ssim);
            if (IsValidSSIM(ssim))
            {
                callbacks_.analyze_finished(_index, crf);
                AnalyzeFinished();
                {
                    // Log
                    string msg = "selected crf = " + crf;
                    Loger.Write(msg);
                }
                return;
            }

            current_analyze_index_++;
            if (analyze_jobs_.Count() > current_analyze_index_)
            {
                CalculateSSIM(analyze_jobs_[current_analyze_index_]);
            }
            else
            {
                callbacks_.analyze_finished(_index, crf);
                AnalyzeFinished();
                {
                    // Log
                    string msg = "selected crf = " + crf;
                    Loger.Write(msg);
                }
            }
        }

        private bool IsValidSSIM(double ssim)
        {
            //return ssim >= kTargetSSIMRangeMin && ssim <= kTargetSSIMRangeMax;
            return ssim >= kTargetSSIMRangeMin;
        }

        private class AnalyzeJob
        {
            public AnalyzeJob(int _index, string _path, int _thread_num, int _crf, List<AnalyzeTimeSelector.TimePair> time_list)
            {
                index = _index;
                path = _path;
                thread_num = _thread_num;
                crf = _crf;
                time_pair_list = time_list;
            }
            public int index;
            public string path;
            public int thread_num;
            public int crf;
            public List<AnalyzeTimeSelector.TimePair> time_pair_list;
        }
    }
}
