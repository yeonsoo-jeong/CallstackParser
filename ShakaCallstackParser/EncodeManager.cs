using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class EncodeManager
    {
        public class Callbacks
        {
            public delegate void OnProgressChanged(int index, int percentage);
            public delegate void OnEncodeFinished(int index, int result_code);
            public delegate void OnAllEncodeFinished();
            public delegate void OnSSIMCalculated(int index, int crf, double ssim);
            public delegate void OnAnalyzeFinished(int index, int crf);
            public Callbacks(OnProgressChanged pc, OnEncodeFinished ef, OnAllEncodeFinished aef, OnSSIMCalculated sc, OnAnalyzeFinished af)
            {
                progress_changed = pc;
                encode_finished = ef;
                all_encode_finished = aef;
                ssim_calculated = sc;
                analyze_finished = af;
            }

            public OnProgressChanged progress_changed;
            public OnEncodeFinished encode_finished;
            public OnAllEncodeFinished all_encode_finished;
            public OnSSIMCalculated ssim_calculated;
            public OnAnalyzeFinished analyze_finished;
        }
        Callbacks callbacks_;

        Action<int, int> test_callback_analyze_finished = (index, crf) => Console.WriteLine("dd");

        Encoder encoder_;

        int current_enc_index_ = 0;
        List<EncodeJob> enc_jobs_;

        Analyzer analyzer_;

        bool is_canceled_ = false;

        public EncodeManager(Callbacks callback)
        {
            callbacks_ = callback;
            encoder_ = new Encoder(new Encoder.Callbacks(EncodeProgressChanged, EncodeFinished));
            analyzer_ = new Analyzer(new Analyzer.Callbacks(OnSSIMCalculated, OnAnalyzeFinished));
        }

        public static int GetCoreNumFromCpuUsage(string cpu_usage)
        {
            if (cpu_usage == "Half")
            {
                return Environment.ProcessorCount / 2;
            }
            else
            {
                return 0;
            }
        }

        public bool Start(List<EncodeJob> jobs)
        {
            is_canceled_ = false;
            enc_jobs_ = jobs;
            current_enc_index_ = 0;
            if (jobs.Count() > 0)
            {
                analyzer_.Analyze(jobs[0].index_, jobs[0].path_, jobs[0].thread_num_);
                return true;
            }
            
            return false;
        }

        public int GetCurrentEncThreads()
        {
            if (enc_jobs_.Count() <= current_enc_index_)
            {
                return 0;
            }
            return enc_jobs_[current_enc_index_].thread_num_;
        }

        public string GetCurrentEncPath()
        {
            if (enc_jobs_.Count() <= current_enc_index_)
            {
                return null;
            }
            return enc_jobs_[current_enc_index_].path_;
        }

        public int GetCurrentIndex()
        {
            if (enc_jobs_.Count() <= current_enc_index_)
            {
                return -1;
            }
            return enc_jobs_[current_enc_index_].index_;
        }

        public void OnEncodeCanceled()
        {
            is_canceled_ = true;
            encoder_.OnEncodeCanceled();
            analyzer_.OnEncodeCanceled();
        }

        public void OnWindowClosed()
        {
            is_canceled_ = true;
            encoder_.OnWindowClosed();
            analyzer_.OnWindowClosed();

        }

        private void Encode(int crf)
        {
            int index = GetCurrentIndex();
            string path = GetCurrentEncPath();
            int thread_num = GetCurrentEncThreads();
            encoder_.Encode(index, path, thread_num, crf);
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.progress_changed(index, percentage);
        }

        private void EncodeFinished(int index, int result_code)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.encode_finished(index, result_code);
            current_enc_index_++;
            if (enc_jobs_.Count() > current_enc_index_)
            {
                analyzer_.Analyze(enc_jobs_[current_enc_index_].index_, enc_jobs_[current_enc_index_].path_, enc_jobs_[current_enc_index_].thread_num_);
            }
            else
            {
                callbacks_.all_encode_finished();
            }
        }

        private void OnSSIMCalculated(int index, int crf, double ssim)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.ssim_calculated(index, crf, ssim);
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.analyze_finished(index, crf);
            Encode(crf);
        }
    }

    public class EncodeJob
    {
        public int index_;
        public string path_;
        public int thread_num_;

        public EncodeJob(int index, string path, int thread_num)
        {
            index_ = index;
            path_ = path;
            thread_num_ = thread_num;
        }
    }
}
