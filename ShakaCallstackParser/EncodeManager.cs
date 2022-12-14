using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class EncodeManager
    {
        public delegate void OnProgressChangedDelegate(int index, int percentage);
        OnProgressChangedDelegate delegate_on_progress_changed;
        public delegate void OnFinishedDelegate(int index);
        OnFinishedDelegate delegate_on_finished;

        public delegate void DelegateOnSSIMCalculated(int index, int crf, double ssim);
        DelegateOnSSIMCalculated callback_ssim_calculated;
        public delegate void DelegateOnAnalyzeFinished(int index, int crf);
        DelegateOnAnalyzeFinished callback_analyze_finished;

        Action<int, int> test_callback_analyze_finished = (index, crf) => Console.WriteLine("dd");

        Encoder encoder_;

        int current_enc_index_ = 0;
        List<EncodeJob> enc_jobs_;

        Analyzer analyzer_;

        public EncodeManager(OnProgressChangedDelegate pc, OnFinishedDelegate f, DelegateOnSSIMCalculated sc, DelegateOnAnalyzeFinished af)
        {
            delegate_on_progress_changed = pc;
            delegate_on_finished = f;
            callback_ssim_calculated = sc;
            callback_analyze_finished = af;
            encoder_ = new Encoder(EncodeProgressChanged, EncodeFinished);
            analyzer_ = new Analyzer(OnSSIMCalculated, OnAnalyzeFinished);
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
            delegate_on_progress_changed(index, percentage);
        }

        private void EncodeFinished(int index)
        {
            delegate_on_finished(index);
            current_enc_index_++;
            if (enc_jobs_.Count() > current_enc_index_)
            {
                analyzer_.Analyze(enc_jobs_[current_enc_index_].index_, enc_jobs_[current_enc_index_].path_);
            }
        }

        private void OnSSIMCalculated(int index, int crf, double ssim)
        {
            callback_ssim_calculated(index, crf, ssim);
        }

        private void OnAnalyzeFinished(int index, int crf)
        {
            callback_analyze_finished(index, crf);
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
