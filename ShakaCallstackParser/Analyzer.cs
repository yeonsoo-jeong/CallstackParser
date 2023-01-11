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
        const string TAG = "Analyzer.cs : ";

        private const int kDefaultCrfValue = 28;
        private const double kTargetSSIMValue = 0.9870;
        private const double kTargetSSIMRangeMin = 0.9869;
        private const double kTargetSSIMRangeMax = 0.9875;

        SSIMCalculator ssim_calculator_;

        List<AnalyzeJob> analyze_jobs_;

        bool is_analyzing_ = false;
        bool is_canceled_ = false;

        public Analyzer()
        {
            ssim_calculator_ = new SSIMCalculator();
        }

        public int Analyze(string path, int thread_num)
        {
            if (is_analyzing_)
            {
                return -1;
            }
            is_analyzing_ = true;
            is_canceled_ = false;
            analyze_jobs_ = new List<AnalyzeJob>();

            AnalyzeTimeSelector selector = new AnalyzeTimeSelector();
            List<AnalyzeTimeSelector.TimePair> time_pair = selector.Calculate(path);
            {
                // Log

                string msg = TAG + "Analyze : " + Path.GetFileName(path) + " time: ";
                for (int i = 0; i < time_pair.Count(); i++)
                {
                    msg += "[" + time_pair[i].start_time + " : " + time_pair[i].duration + "] ";
                }
                Loger.Write(msg);
            }

            // Should be descending order!
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 28, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 27, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 26, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 25, time_pair));

            return CalculateAverageSSIM(analyze_jobs_);
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

        private int CalculateAverageSSIM(List<AnalyzeJob> jobs)
        {
            int result = -1;
            int current_index = 0;
            while (analyze_jobs_.Count() > current_index)
            {
                AnalyzeJob job = jobs[current_index];
                Tuple<double, bool> tuple = ssim_calculator_.Calculate(job.path, job.thread_num, job.crf, job.time_pair_list);
                double avg_ssim = tuple.Item1;
                bool is_cancel = tuple.Item2;
                
                if (is_canceled_ || avg_ssim < 0)
                {
                    break;
                }

                if (IsValidSSIM(avg_ssim))
                {
                    result = job.crf;
                    AnalyzeFinished();
                    {
                        Loger.Write(TAG + "CalculateAverageSSIM : selected crf = " + job.crf);
                    }
                    break;
                }

                current_index++;
                if (current_index >= jobs.Count())
                {
                    result = job.crf;
                    AnalyzeFinished();
                    {
                        Loger.Write(TAG + "CalculateAverageSSIM : selected crf = " + job.crf);
                    }
                    break;
                }
            }
            
            return result;
        }

        private void AnalyzeFinished()
        {
            analyze_jobs_.Clear();
            is_analyzing_ = false;
        }

        private bool IsValidSSIM(double ssim)
        {
            //return ssim >= kTargetSSIMRangeMin && ssim <= kTargetSSIMRangeMax;
            return ssim >= kTargetSSIMRangeMin;
        }

        private class AnalyzeJob
        {
            public AnalyzeJob(string _path, int _thread_num, int _crf, List<AnalyzeTimeSelector.TimePair> time_list)
            {
                path = _path;
                thread_num = _thread_num;
                crf = _crf;
                time_pair_list = time_list;
            }
            public string path;
            public int thread_num;
            public int crf;
            public List<AnalyzeTimeSelector.TimePair> time_pair_list;
        }
    }
}
