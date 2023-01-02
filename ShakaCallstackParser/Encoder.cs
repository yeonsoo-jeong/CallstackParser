﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class Encoder
    {
        public class Callbacks
        {
            public delegate void OnProgressChanged(int index, int percentage);
            public delegate void OnFinished(int index, int result_code);
            public Callbacks(OnProgressChanged pc, OnFinished f)
            {
                progress_changed = pc;
                finished = f;
            }

            public OnProgressChanged progress_changed;
            public OnFinished finished;
        }
        Callbacks callbacks_;

        Process enc_process_ = null;

        bool is_encoding_ = false;
        bool is_canceled_ = false;
        int index_ = 0;
        int result_code_;
        string encoding_name_;
        string org_name_;
        

        public Encoder(Callbacks callback)
        {
            callbacks_ = callback;
        }


        public bool Encode(int index, string inpPath, int thread_num, int crf)
        {
            if (is_encoding_)
            {
                return false;
            }
            is_encoding_ = true;
            is_canceled_ = false;
            index_ = index;
            result_code_ = -1;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(EncodeBackground);
            worker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnFinished);

            EncArgument ea = new EncArgument(index, inpPath, thread_num, crf);
            worker.RunWorkerAsync(argument: ea);

            return true;
        }

        public void OnEncodeCanceled()
        {
            is_canceled_ = true;
            try
            {
                if (enc_process_ != null)
                {
                    if (!enc_process_.HasExited)
                    {
                        enc_process_.Kill();
                    }
                }
            }
            catch (Exception e)
            {
                Loger.Write("Exceltion: Encoder.cs OnEncodeCanceled()");
                Loger.Write(e.ToString());
                Loger.Write("");
            }
        }

        public void OnWindowClosed()
        {
            is_canceled_ = true;
            try
            {
                if (enc_process_ != null)
                {
                    if (!enc_process_.HasExited)
                    {
                        enc_process_.Kill();
                    }
                }
            }
            catch (Exception e)
            {
                Loger.Write("Exceltion: Encoder.cs OnWindowClosed()");
                Loger.Write(e.ToString());
                Loger.Write("");
            }
        }

        private void EncodeBackground(object sender, DoWorkEventArgs e)
        {
            EncArgument arg = (EncArgument)e.Argument;
            e.Result = -1.0f;
            using (enc_process_ = new Process())
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                org_name_ = Path.GetFileName(arg.path);
                encoding_name_ = "[ENC]" + org_name_;

                enc_process_.EnableRaisingEvents = true;
                enc_process_.StartInfo.FileName = "ffmpeg.exe";
                enc_process_.StartInfo.Arguments = "-y -i \"" + arg.path + "\" -threads " + arg.thread_num + " -c:a copy -c:s copy -c:v h264 -ssim 1 -crf " + arg.crf + " \"" + encoding_name_ + "\"";
                enc_process_.StartInfo.WorkingDirectory = "";
                enc_process_.StartInfo.CreateNoWindow = true;
                enc_process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                enc_process_.StartInfo.RedirectStandardOutput = true;
                enc_process_.StartInfo.RedirectStandardError = true;
                enc_process_.Start();

                string readStr = "";
                int duration_seconds = 0;

                while ((readStr = enc_process_.StandardError.ReadLine()) != null)
                {
                    if (readStr.Length > 12 && readStr.Substring(0, 12) == "  Duration: ")
                    {
                        string substr = readStr.Substring(12, 11);
                        duration_seconds = CalculateSeconds(substr);
                    }

                    if (readStr.Length > 6 && readStr.Substring(0, 6) == "frame=")
                    {
                        int index = readStr.IndexOf("time=");
                        if (index >= 0)
                        {
                            string substr = readStr.Substring(index + 5, 11);
                            int seconds = CalculateSeconds(substr);
                            int percentage = Convert.ToInt32((float)seconds / (float)duration_seconds * 100);
                            worker.ReportProgress(percentage);
                            //delegate_on_progress_changed(arg.index, percentage);
                        }
                    }

                    double ssim = SSIMCalculator.ParseSSIM(readStr);
                    if (ssim >= 0)
                    {
                        e.Result = ssim;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                enc_process_.WaitForExit();
                result_code_ = enc_process_.ExitCode;
            }
        }

        private void OnFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            is_encoding_ = false;
            if (is_canceled_)
            {
                return; 
            }

            if (e.Error != null)
            {
                // handle the error
            }
            else if (e.Cancelled)
            {
                // handle cancellation
            }
            else
            {
                double ssim = -1;
                if (e.Result != null)
                {
                    Double.TryParse(e.Result.ToString(), out ssim);
                }

                {
                    // Log
                    string msg = "Encode Finished. name=" + org_name_ + ", ssim=" + ssim;
                    Loger.Write(msg);
                    Loger.Write("");
                }

                CustomRename(encoding_name_, org_name_);
                callbacks_.finished(index_, result_code_);
            }
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            callbacks_.progress_changed(index_, e.ProgressPercentage);
            //MessageBox.Show(e.ProgressPercentage.ToString() + "%");
        }

        private int CalculateSeconds(string time_str)
        {
            int hour = int.Parse(time_str.Substring(0, 2));
            int minute = int.Parse(time_str.Substring(3, 2));
            int second = int.Parse(time_str.Substring(6, 2));
            return second + (minute * 60) + (hour * 3600);
        }

        private static void CustomRename(string src_name, string org_name)
        {
            if (File.Exists(src_name))
            {
                string out_name = "RE_" + org_name;
                if (File.Exists(out_name))
                {
                    const int max = 1000;
                    for (int i = 0; i < max; i++)
                    {
                        out_name = "RE" + i + "_" + org_name;
                        if (!File.Exists(out_name))
                        {
                            File.Move(src_name, out_name);
                            break;
                        }
                    }
                }
                else
                {
                    File.Move(src_name, out_name);
                }
            }
        }

        class EncArgument
        {
            public EncArgument(int _index, string _path, int _thread_num, int _crf)
            {
                index = _index;
                path = _path;
                crf = _crf;
                thread_num = _thread_num;
            }

            public int index;
            public string path;
            public int thread_num;
            public int crf;
        }
    }
}
