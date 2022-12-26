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
        List<ResultData> result_data_;

        bool is_analyzing_ = false;
        int current_analyze_index_ = -1;

        public Analyzer(Callbacks callback)
        {
            callbacks_ = callback;
            ssim_calculator_ = new SSIMCalculator(new SSIMCalculator.Callbacks(OnCalcuateFinished));
        }

        public bool Analyze(int index, string path)
        {
            if (is_analyzing_)
            {
                return false;
            }
            is_analyzing_ = true;
            current_analyze_index_ = 0;
            result_data_ = new List<ResultData>();
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


            analyze_jobs_.Add(new AnalyzeJob(index, path, 28, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(index, path, 27, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(index, path, 26, time_pair));

            CalculateSSIM(analyze_jobs_[0]);

            return true;
        }

        public void OnWindowClosed()
        {
            ssim_calculator_.OnWindowClosed();
        }

        private int CalculateSSIM(AnalyzeJob job)
        {
            ssim_calculator_.Calculate(job.index, job.path, job.crf, job.time_pair_list);
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
            callbacks_.calculated(_index, crf, ssim);
            if (ssim > 0)
            {
                result_data_.Add(new ResultData(crf, ssim));
            }

            if (IsReadySSIM(result_data_, kTargetSSIMValue))
            {
                int index = GetMinSSIMDistanceIndex(result_data_, kTargetSSIMValue);
                callbacks_.analyze_finished(_index, result_data_[index].crf);
                AnalyzeFinished();
                {
                    // Log
                    string msg = "selected crf = " + result_data_[index].crf;
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
                int index = GetMinSSIMDistanceIndex(result_data_, kTargetSSIMValue);
                if (index > 0)
                {
                    callbacks_.analyze_finished(_index, result_data_[index].crf);
                }
                else
                {
                    callbacks_.analyze_finished(_index, kDefaultCrfValue);
                }
                AnalyzeFinished();

                {
                    // Log
                    string msg = "selected crf = " + result_data_[index].crf;
                    Loger.Write(msg);
                    Loger.Write("");
                }
            }
        }

        private bool IsValidSSIM(double ssim)
        {
            return ssim >= kTargetSSIMRangeMin && ssim <= kTargetSSIMRangeMax;
        }

        private bool IsReadySSIM(List<ResultData> data, double target)
        {
            if (data.Count() < 1)
            {
                return false;
            }

            if (data.Count() == 1)
            {
                return IsValidSSIM(data[0].ssim);
            }

            for (int i = 0; i < data.Count() - 1; i++)
            {
                if (IsValidSSIM(data[i].ssim))
                {
                    return true;
                }
                if (IsValidSSIM(data[i+1].ssim))
                {
                    return true;
                }
                double diff1 = data[i].ssim - target;
                double diff2 = data[i + 1].ssim - target;
                int sign1 = Math.Sign(diff1);
                int sign2 = Math.Sign(diff2);
                if (sign1 == 0 || sign2 == 0)
                {
                    // impossible scenario
                    return true;
                }
                if (sign1 != sign2)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetMinSSIMDistanceIndex(List<ResultData> data, double target)
        {
            int index = -1;
            double min_diff = double.MinValue;
            for (int i = 0; i < data.Count(); i++)
            {
                double diff = Math.Abs(data[i].ssim - target);
                if (diff >= min_diff)
                {
                    index = i;
                }
            }
            return index;
        }

        private class AnalyzeJob
        {
            public AnalyzeJob(int _index, string _path, int _crf, List<AnalyzeTimeSelector.TimePair> time_list)
            {
                index = _index;
                path = _path;
                crf = _crf;
                time_pair_list = time_list;
            }
            public int index;
            public string path;
            public int crf;
            public List<AnalyzeTimeSelector.TimePair> time_pair_list;
        }

        private class ResultData
        {
            public ResultData(int _crf, double _ssim)
            {
                crf = _crf;
                ssim = _ssim;
            }
            public int crf;
            public double ssim;
        }
    }
}
