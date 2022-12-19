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

        public EncodeManager(Callbacks callback)
        {
            callbacks_ = callback;
            encoder_ = new Encoder(new Encoder.Callbacks(EncodeProgressChanged, EncodeFinished));
            analyzer_ = new Analyzer(new Analyzer.Callbacks(OnSSIMCalculated, OnAnalyzeFinished));
        }

        public bool Start(List<EncodeJob> jobs)
        {
            enc_jobs_ = jobs;
            current_enc_index_ = 0;
            if (jobs.Count() > 0)
            {
                analyzer_.Analyze(jobs[0].index_, jobs[0].path_);
                return true;
            }
            
            return false;
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

        private void Encode(int crf)
        {
            int index = GetCurrentIndex();
            string path = GetCurrentEncPath();
            encoder_.Encode(index, path, crf);
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            callbacks_.progress_changed(index, percentage);
        }

        private void EncodeFinished(int index, int result_code)
        {
            callbacks_.encode_finished(index, result_code);
            current_enc_index_++;
            if (enc_jobs_.Count() > current_enc_index_)
            {
                analyzer_.Analyze(enc_jobs_[current_enc_index_].index_, enc_jobs_[current_enc_index_].path_);
            } 
            else
            {
                callbacks_.all_encode_finished();
            }
        }

        private void OnSSIMCalculated(int index, int crf, double ssim)
        {
            callbacks_.ssim_calculated(index, crf, ssim);
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            callbacks_.analyze_finished(index, crf);
            Encode(crf);
        }
    }

    public class EncodeJob
    {
        public int index_;
        public string path_;

        public EncodeJob(int index, string path)
        {
            index_ = index;
            path_ = path;
        }
    }
}
