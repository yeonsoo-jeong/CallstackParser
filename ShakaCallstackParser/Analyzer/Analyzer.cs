using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YsCommon;

namespace ShakaCallstackParser
{
    class Analyzer
    {
        public class Callbacks
        {
            public delegate void OnProgressChanged(int id, int percentage);
            public Callbacks(OnProgressChanged pc)
            {
                progress_changed = pc;
            }

            public OnProgressChanged progress_changed;
        }
        Callbacks callbacks_;

        const string TAG = "Analyzer.cs : ";

        private const int kDefaultCrfValue = 28;
        private const double kTargetSSIMValue = 0.9870;
        private const double kTargetSSIMRangeMin = 0.9865;
        private const double kTargetSSIMRangeMax = 0.9875;
        private const double kTargetSSIMGapLimit = 0.0030;

        private const int kBaseProgressPercentage = 20;

        SSIMCalculator ssim_calculator_;

        List<AnalyzeJob> analyze_jobs_;

        bool is_analyzing_ = false;
        bool is_canceled_ = false;
        int parent_id_ = -1;
        int current_index_ = -1;

        public Analyzer(Callbacks callback)
        {
            callbacks_ = callback;
            ssim_calculator_ = new SSIMCalculator(new SSIMCalculator.Callbacks(OnSSIMCalculatorProgressChanged));
        }

        public AnalyzerResult Analyze(int id, string path, int thread_num, out int crf, out long expect_size)
        {
            parent_id_ = id;
            crf = -1;
            expect_size = -1;

            if (is_analyzing_)
            {
                return AnalyzerResult.already_analyzing;
            }
            is_analyzing_ = true;
            is_canceled_ = false;
            analyze_jobs_ = new List<AnalyzeJob>();

            Tuple<long, int> video_info = FFmpegUtil.GetSizeDurationSec(path);
            if (is_canceled_)
            {
                is_analyzing_ = false;
                return AnalyzerResult.fail;
            }
            long inp_size = video_info.Item1;
            int inp_seconds = video_info.Item2;
            if (inp_size < 0 || inp_seconds < 0)
            {
                is_analyzing_ = false;
                Loger.Write(TAG + "Analyze : [" + Path.GetFileName(path) + "] size or seconds is negative. size=" + inp_size + ", seconds=" + inp_seconds);
                return AnalyzerResult.fail;
            }
            List<AnalyzeTimeSelector.TimePair> time_pair = AnalyzeTimeSelector.Calculate(inp_seconds);
            {
                // Log
                string msg = TAG + "Analyze : " + Path.GetFileName(path) + " time: ";
                for (int i = 0; i < time_pair.Count(); i++)
                {
                    msg += "[" + time_pair[i].start_time + " : " + time_pair[i].duration + "] ";
                }
                Loger.Write(msg);
            }

            // Must be descending order!
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 29, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 28, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 27, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 26, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 25, time_pair));
            analyze_jobs_.Add(new AnalyzeJob(path, thread_num, 24, time_pair));

            Tuple<int, int, long> result = AnalyzeJobs();
            if (is_canceled_)
            {
                is_analyzing_ = false;
                return AnalyzerResult.fail;
            }

            is_analyzing_ = false;
            crf = result.Item1;
            int result_seconds = result.Item2;
            long result_size = result.Item3;
            expect_size = GetExpectedSize(inp_seconds, result_seconds, result_size);
            Loger.Write(TAG + "Analyze : [" + Path.GetFileName(path) + "] selected crf=" + crf + ", input_size=" + inp_size + "K, expected_size=" + expect_size + "K, ratio=" + Math.Round(expect_size / (float)inp_size, 2));
            if (crf <= 0)
            {
                return AnalyzerResult.fail;
            }
            if (inp_size <= (expect_size * 0.8))
            {
                Loger.Write(TAG + "Analyze : [" + Path.GetFileName(path) + "] file is not expected to decrease in size. input_size=" + inp_size + "K, expected_size=" + expect_size + "K");
                return AnalyzerResult.size_over;
            }

            return AnalyzerResult.success;
        }

        public void OnEncodeCanceled()
        {
            is_canceled_ = true;
            ssim_calculator_.OnEncodeCanceled();
        }

        public void OnWindowClosed()
        {
            is_canceled_ = true;
            ssim_calculator_.OnWindowClosed();
        }

        private void OnSSIMCalculatorProgressChanged(int percentage)
        {
            double job_num = analyze_jobs_.Count();

            // actual_percentage = base_percentage + processed_percentage + current_processing_percentage
            // 1. base_percentage
            int base_percentage = kBaseProgressPercentage;

            // 2. processed_percentage
            int job_proportion = 100 - base_percentage;
            double job_proportion_factor = job_proportion / 100.0f;
            int processed_percentage = (int)(current_index_ / job_num * job_proportion);

            // 3. current_processing_percentage
            double scale_factor = 1.0f / job_num * job_proportion_factor;
            int current_processing_percentage = (int)(percentage * scale_factor);

            int actual_percentage = (int)(base_percentage + processed_percentage + current_processing_percentage);
            callbacks_.progress_changed(parent_id_, actual_percentage);
        }

        private Tuple<int, int, long> AnalyzeJobs()
        {
            int result_crf = -1;
            int result_seconds = -1;
            long result_size = -1;
            current_index_ = 0;
            ref List<AnalyzeJob> jobs = ref analyze_jobs_;
            callbacks_.progress_changed(parent_id_, kBaseProgressPercentage);
            while (jobs.Count() > current_index_)
            {
                AnalyzeJob job = jobs[current_index_];
                Tuple<double, int, long> tuple = ssim_calculator_.CalculateAverageSSIM(job.path, job.thread_num, job.crf, job.time_pair_list);
                double avg_ssim = tuple.Item1;
                result_seconds = tuple.Item2;
                result_size = tuple.Item3;
                Loger.Write(TAG + "AnalyzeJobs : size_sum=" + result_size + ", size_second=" + result_seconds + ", avg_ssim=" + Math.Round(avg_ssim, 4));

                if (is_canceled_ || avg_ssim <= 0)
                {
                    break;
                }

                if (IsValidSSIM(avg_ssim))
                {
                    result_crf = job.crf;
                    AnalyzeFinished();
                    break;
                }

                current_index_++;
                if (current_index_ >= jobs.Count())
                {
                    result_crf = job.crf;
                    AnalyzeFinished();
                    break;
                }

                if (current_index_ + 3 < jobs.Count() && IsSSIMGapTripleOver(avg_ssim))
                {
                    current_index_ += 3;
                    Loger.Write(TAG + "AnalyzeJobs : ssim gap is triple over. skip next 3 state. gap = " + Math.Round((kTargetSSIMRangeMin - avg_ssim), 4));
                } 
                else if (current_index_ + 2 < jobs.Count() && IsSSIMGapDoubleOver(avg_ssim))
                {
                    current_index_ += 2;
                    Loger.Write(TAG + "AnalyzeJobs : ssim gap is doubled over. skip next 2 state. gap = " + Math.Round((kTargetSSIMRangeMin - avg_ssim), 4));
                }
                else if (current_index_ + 1 < jobs.Count() && IsSSIMGapOver(avg_ssim))
                {
                    current_index_++;
                    Loger.Write(TAG + "AnalyzeJobs : ssim gap is over. skip next 1 state. gap = " + Math.Round((kTargetSSIMRangeMin - avg_ssim), 4));
                }

                // int percentage = (int)((double)current_index_ / jobs.Count() * 100.0f);
                // callbacks_.progress_changed(parent_id_, percentage);
                OnSSIMCalculatorProgressChanged(0);
            }
            
            return new Tuple<int, int, long>(result_crf, result_seconds, result_size);
        }

        private void AnalyzeFinished()
        {
            analyze_jobs_.Clear();
        }

        private bool IsValidSSIM(double ssim)
        {
            //return ssim >= kTargetSSIMRangeMin && ssim <= kTargetSSIMRangeMax;
            return ssim >= kTargetSSIMRangeMin;
        }

        private bool IsSSIMGapOver(double ssim)
        {
            return kTargetSSIMGapLimit <= (kTargetSSIMRangeMin - ssim);
        }

        private bool IsSSIMGapDoubleOver(double ssim)
        {
            return kTargetSSIMGapLimit * 2 <= (kTargetSSIMRangeMin - ssim);
        }

        private bool IsSSIMGapTripleOver(double ssim)
        {
            return kTargetSSIMGapLimit * 3 <= (kTargetSSIMRangeMin - ssim);
        }

        private long GetExpectedSize(int inp_duration_sec, int result_sec, long result_size)
        {
            return (long)((double)inp_duration_sec / (double)result_sec * (double)result_size);
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

        public enum AnalyzerResult
        { 
            success,
            fail,
            already_analyzing,
            size_over
        }
    }
}
