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
            public delegate void OnEncodeStarted(int index, int crf);
            public delegate void OnProgressChanged(int index, int percentage);
            public delegate void OnEncodeFinished(int index, int result_code);
            public delegate void OnAllEncodeFinished();
            public delegate void OnEncodeCancled(int index);
            public delegate void OnAnalyzeStarted(int index);
            public delegate void OnAnalyzeFinished(int index, int crf);
            public Callbacks(OnEncodeStarted es, OnProgressChanged pc, OnEncodeFinished ef, OnAllEncodeFinished aef, OnEncodeCancled ec,
                OnAnalyzeStarted ast, OnAnalyzeFinished af)
            {
                encode_started = es;
                progress_changed = pc;
                encode_finished = ef;
                all_encode_finished = aef;
                encode_canceled = ec;
                analyze_started = ast;
                analyze_finished = af;
            }

            public OnEncodeStarted encode_started;
            public OnProgressChanged progress_changed;
            public OnEncodeFinished encode_finished;
            public OnAllEncodeFinished all_encode_finished;
            public OnEncodeCancled encode_canceled;
            public OnAnalyzeStarted analyze_started;
            public OnAnalyzeFinished analyze_finished;
        }
        Callbacks callbacks_;

        Encoder encoder_;

        int current_enc_index_ = 0;
        List<EncodeJob> enc_jobs_;
        string out_directory_;

        Analyzer analyzer_;

        bool is_canceled_ = false;

        public EncodeManager(Callbacks callback)
        {
            callbacks_ = callback;
            encoder_ = new Encoder(new Encoder.Callbacks(EncodeProgressChanged));
            analyzer_ = new Analyzer();
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

        public void Start(List<EncodeJob> jobs, string out_directory)
        {
            is_canceled_ = false;
            enc_jobs_ = jobs;
            out_directory_ = out_directory;
            int enc_index;

            current_enc_index_ = 0;

            while (enc_jobs_.Count() > current_enc_index_)
            {
                enc_index = jobs[current_enc_index_].index;
                string path = jobs[current_enc_index_].path;
                int thread_num = jobs[current_enc_index_].thread_num;

                callbacks_.analyze_started(enc_index);
                int crf = analyzer_.Analyze(enc_index, path, thread_num);
                if (is_canceled_)
                {
                    return;
                }
                callbacks_.analyze_finished(enc_index, crf);

                callbacks_.encode_started(enc_index, crf);
                int result_code = encoder_.Encode(enc_index, path, out_directory_, thread_num, crf);
                if (is_canceled_)
                {
                    return;
                }
                callbacks_.encode_finished(enc_index, result_code);
                current_enc_index_++;
            }

            if (is_canceled_)
            {
                return;
            }
            callbacks_.all_encode_finished();

            return;
        }

        public void OnEncodeCanceled()
        {
            is_canceled_ = true;
            encoder_.OnEncodeCanceled();
            analyzer_.OnEncodeCanceled();
            callbacks_.encode_canceled(GetCurrentIndex());
        }

        public void OnWindowClosed()
        {
            is_canceled_ = true;
            encoder_.OnWindowClosed();
            analyzer_.OnWindowClosed();
        }

        private int GetCurrentIndex()
        {
            if (enc_jobs_.Count() <= current_enc_index_)
            {
                return -1;
            }
            return enc_jobs_[current_enc_index_].index;
        }

        private void EncodeProgressChanged(int index, int percentage)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.progress_changed(index, percentage);
        }
    }

    public class EncodeJob
    {
        public int index;
        public string path;
        public int thread_num;

        public EncodeJob(int _index, string _path, int _thread_num)
        {
            index = _index;
            path = _path;
            thread_num = _thread_num;
        }
    }
}
