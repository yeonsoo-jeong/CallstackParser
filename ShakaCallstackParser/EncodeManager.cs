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

        Encoder encoder_;

        int current_enc_index_ = 0;
        List<EncodeJob> enc_jobs_;

        public EncodeManager(OnProgressChangedDelegate pc, OnFinishedDelegate f)
        {
            delegate_on_progress_changed = pc;
            delegate_on_finished = f;
            encoder_ = new Encoder(EncodeProgressChanged, EncodeFinished);
        }

        public bool Start(List<EncodeJob> jobs)
        {
            enc_jobs_ = jobs;
            current_enc_index_ = 0;
            if (jobs.Count() > 0)
            {
                Encode();
                return true;
            }
            
            return false;
        }

        public bool IsFinished()
        {
            return enc_jobs_.Count() <= current_enc_index_;
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
            return current_enc_index_;
        }

        private void Encode()
        {
            int index = GetCurrentIndex();
            string path = GetCurrentEncPath();
            encoder_.Encode(index, path);
            current_enc_index_++;
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            delegate_on_progress_changed(index, percentage);
        }

        private void EncodeFinished(int index)
        {
            delegate_on_finished(index);
            if (!IsFinished())
            {
                Encode();
            }
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
