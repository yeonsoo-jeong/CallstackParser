using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YsCommon;
using ShakaCallstackParser.Model;

namespace ShakaCallstackParser
{
    class EncodeManager
    {
        const string TAG = "EncodeManager.cs : ";

        public enum EncodeCallbackStatus
        {
            AnalyzeStarted,
            AnalyzeCancled,
            AnalyzeFailed,
            AnalyzeFinished,
            EncodeStarted,
            EncodeCanceled,
            EncodeFailed,
            EncodeFinished,
            AllEncodeFinished
        }

        public class Callbacks
        {
            public delegate void OnEncodeStatusChanged(int id, EncodeCallbackStatus status, string msg);
            public delegate void OnAnalyzeProgressChanged(int id, int percentage);
            public delegate void OnEncodeProgressChanged(int id, int percentage);

            public Callbacks(OnEncodeStatusChanged esc, OnAnalyzeProgressChanged apc, OnEncodeProgressChanged epc)
            {
                encode_status_changed = esc;
                analyze_progress_changed = apc;
                encode_progress_changed = epc;
            }

            public OnEncodeStatusChanged encode_status_changed;
            public OnAnalyzeProgressChanged analyze_progress_changed;
            public OnEncodeProgressChanged encode_progress_changed;
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
            analyzer_ = new Analyzer(new Analyzer.Callbacks(AnalyzeProgressChanged));
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
                EncodeItem enc_list_item = enc_item_manager_.GetToEncodeFirstItem();
                int id = Convert.ToInt32(enc_list_item.Id);
                string path = enc_list_item.Path;
                int thread_num = GetCoreNumFromCpuUsage(enc_list_item.CpuUsageSelected);
                Result result;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                result = Analyze(id, path, thread_num, out int crf, out long expect_size);
                stopwatch.Stop();
                Loger.Write(TAG + "Start : Analyzation Time=" + stopwatch.ElapsedMilliseconds / 1000 + "s");
                if (result == Result.fail_stop)
                {
                    break;
                }
                else if (result == Result.fail_continue)
                {
                    continue;
                }

                stopwatch.Start();
                result = Encode(id, path, out_directory, thread_num, crf, expect_size);
                stopwatch.Stop();
                Loger.Write(TAG + "Start : Encoding Time=" + stopwatch.ElapsedMilliseconds / 1000 + "s");
                if (result == Result.fail_stop)
                {
                    break;
                }
                else if (result == Result.fail_continue)
                {
                    continue;
                }
            }

            if (!is_canceled_)
            {
                callbacks_.encode_status_changed(-1, EncodeCallbackStatus.AllEncodeFinished, "");
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

        private void AnalyzeProgressChanged(int id, int percentage)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.analyze_progress_changed(id, percentage);
        }

        private void EncodeProgressChanged(int id, int percentage)
        {
            if (is_canceled_)
            {
                return;
            }
            callbacks_.encode_progress_changed(id, percentage);
        }

        private Result Analyze(int id, string path, int thread_num, out int crf, out long expect_size)
        {
            callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeStarted, "");

            Analyzer.AnalyzerResult result = analyzer_.Analyze(id, path, thread_num, out crf, out expect_size);
            if (is_canceled_)
            {
                callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeCancled, "");
                return Result.fail_stop;
            }
            switch (result)
            {
                case Analyzer.AnalyzerResult.already_analyzing:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeFailed, "Error: Already analyzation started.");
                    return Result.fail_stop;
                case Analyzer.AnalyzerResult.fail:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeFailed, "Unexpected error occured.");
                    return Result.fail_continue;
                case Analyzer.AnalyzerResult.size_over:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeFailed, "It is not expected to decrease in size.");
                    return Result.fail_continue;
            }
            if (crf < 0)
            {
                callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeFailed, "Unexpected error occured.");
                return Result.fail_continue;
            }
            callbacks_.encode_status_changed(id, EncodeCallbackStatus.AnalyzeFinished, crf.ToString());

            return Result.success;
        }

        private Result Encode(int id, string path, string out_directory, int thread_num, int crf, long expect_size)
        {
            callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeStarted, crf.ToString());
            Encoder.EncoderResult result = encoder_.Encode(id, path, out_directory, thread_num, crf, expect_size, out int return_code, out double ssim);
            if (is_canceled_)
            {
                callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeCanceled, "");
                return Result.fail_stop;
            }
            
            switch (result)
            {
                case Encoder.EncoderResult.already_encoding:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeFailed, "Error: Already encoding started.(" + return_code + ")");
                    return Result.fail_stop;
                case Encoder.EncoderResult.size_over:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeFailed, "Encoding succeeded, but the file size did not decrease.(" + return_code + ")");
                    return Result.fail_continue;
                case Encoder.EncoderResult.fail:
                    callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeFailed, "Unexpected error occured.(" + return_code + ")");
                    return Result.fail_continue;
            }
            callbacks_.encode_status_changed(id, EncodeCallbackStatus.EncodeFinished, "");

            return Result.success;
        }

        private enum Result
        { 
            success,
            fail_continue,
            fail_stop,
        }
    }
}
