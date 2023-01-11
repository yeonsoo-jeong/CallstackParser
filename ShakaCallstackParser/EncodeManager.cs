using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class EncodeManager
    {
        const string TAG = "EncodeManager.cs : ";

        public class Callbacks
        {
            public delegate void OnEncodeStarted(int index, int crf);
            public delegate void OnProgressChanged(int index, int percentage);
            public delegate void OnEncodeCanceled(int index);
            public delegate void OnEncodeFailed(int index, int result_code);
            public delegate void OnEncodeFinished(int index);
            public delegate void OnAllEncodeFinished();
            public delegate void OnAnalyzeStarted(int index);
            public delegate void OnAnalyzeCanceled(int index);
            public delegate void OnAnalyzeFailed(int index);
            public delegate void OnAnalyzeFinished(int index, int crf);
            
            public Callbacks(OnEncodeStarted es, OnProgressChanged pc, OnEncodeCanceled ec, OnEncodeFailed ef, OnEncodeFinished efin, OnAllEncodeFinished aef, 
                OnAnalyzeStarted ast, OnAnalyzeCanceled ac, OnAnalyzeFailed af, OnAnalyzeFinished afin)
            {
                encode_started = es;
                progress_changed = pc;
                encode_canceled = ec;
                encode_failed = ef;
                encode_finished = efin;
                all_encode_finished = aef;
                analyze_started = ast;
                analyze_canceled = ac;
                analyze_failed = af;
                analyze_finished = afin;
            }

            public OnEncodeStarted encode_started;
            public OnProgressChanged progress_changed;
            public OnEncodeCanceled encode_canceled;
            public OnEncodeFailed encode_failed;
            public OnEncodeFinished encode_finished;
            public OnAllEncodeFinished all_encode_finished;
            public OnAnalyzeStarted analyze_started;
            public OnAnalyzeCanceled analyze_canceled;
            public OnAnalyzeFailed analyze_failed;
            public OnAnalyzeFinished analyze_finished;
        }

        private readonly Callbacks callbacks_;

        private readonly EncItemManager enc_item_manager_;
        private readonly Encoder encoder_;
        private readonly Analyzer analyzer_;

        bool is_encoding_ = false;
        bool is_canceled_ = false;

        public EncodeManager(Callbacks callback, EncItemManager enc_item_manager)
        {
            callbacks_ = callback;
            enc_item_manager_ = enc_item_manager;
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

        public void Start(string out_directory)
        {
            if (enc_item_manager_.GetToEncodeItemsNum() <= 0 || is_encoding_)
            {
                return;
            }

            is_encoding_ = true;
            is_canceled_ = false;

            while (enc_item_manager_.GetToEncodeItemsNum() > 0)
            {
                EncListItems enc_list_item = enc_item_manager_.GetToEncodeFirstItem();
                int number = Convert.ToInt32(enc_list_item.number);
                int index = enc_item_manager_.GetIndexByNumber(number);
                string path = enc_list_item.path;
                int thread_num = GetCoreNumFromCpuUsage(enc_list_item.cpu_usage_selected);
                Result result;

                result = Analyze(index, path, thread_num, out int crf);
                if (result == Result.cancel)
                {
                    break;
                }
                else if (result == Result.fail)
                {
                    continue;
                }
                
                result = Encode(index, path, out_directory, thread_num, crf);
                if (result == Result.cancel)
                {
                    break;
                }
                else if (result == Result.fail)
                {
                    continue;
                }
            }

            #region original code
            //while (enc_jobs_.Count() > current_enc_index_)
            //{
            //    enc_index = jobs[current_enc_index_].index;
            //    string path = jobs[current_enc_index_].path;
            //    int thread_num = jobs[current_enc_index_].thread_num;

            //    callbacks_.analyze_started(enc_index);
            //    int crf = analyzer_.Analyze(enc_index, path, thread_num);
            //    if (is_canceled_)
            //    {
            //        break;
            //    }
            //    callbacks_.analyze_finished(enc_index, crf);

            //    callbacks_.encode_started(enc_index, crf);
            //    int result_code = encoder_.Encode(enc_index, path, out_directory_, thread_num, crf);
            //    if (is_canceled_)
            //    {
            //        break;
            //    }
            //    callbacks_.encode_finished(enc_index, result_code);
            //    current_enc_index_++;
            //}
            #endregion

            if (!is_canceled_)
            {
                callbacks_.all_encode_finished();
            } 
            is_encoding_ = false;

            return;
        }

        public bool IsEncoding()
        {
            return is_encoding_;
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

        private void EncodeProgressChanged(int index, int percentage)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.progress_changed(index, percentage);
        }

        private Result Analyze(int index, string path, int thread_num, out int crf)
        {
            callbacks_.analyze_started(index);
            crf = analyzer_.Analyze(path, thread_num);
            if (is_canceled_)
            {
                callbacks_.encode_canceled(index);
                return Result.cancel;
            }
            if (crf < 0)
            {
                callbacks_.analyze_failed(index);
                return Result.fail;
            }
            callbacks_.analyze_finished(index, crf);

            return Result.success;
        }

        private Result Encode(int index, string path, string out_directory, int thread_num, int crf)
        {
            callbacks_.encode_started(index, crf);
            int result_code = encoder_.Encode(index, path, out_directory, thread_num, crf);
            if (is_canceled_)
            {
                callbacks_.encode_canceled(index);
                return Result.cancel;
            }
            if (result_code < 0)
            {
                callbacks_.encode_failed(index, result_code);
                return Result.fail;
            }
            callbacks_.encode_finished(index);

            return Result.success;
        }

        private enum Result
        { 
            success,
            fail,
            cancel
        }
    }
}
