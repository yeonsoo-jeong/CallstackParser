using System;
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
        public delegate void OnProgressChangedDelegate(int index, int percentage);
        OnProgressChangedDelegate delegate_on_progress_changed;
        public delegate void OnFinishedDelegate(int index);
        OnFinishedDelegate delegate_on_finished;

        bool is_encoding_ = false;
        int index_ = 0;
        string encoding_name_;
        string org_name_;

        public Encoder(OnProgressChangedDelegate pc, OnFinishedDelegate f)
        {
            delegate_on_progress_changed = pc;
            delegate_on_finished = f;
        }

        public bool Encode(int index, string inpPath)
        {
            if (is_encoding_)
            {
                return false;
            }
            is_encoding_ = true;
            index_ = index;
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(EncodeBackground);
            worker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnFinished);

            EncArgument ea = new EncArgument(index, inpPath);
            worker.RunWorkerAsync(argument: ea);

            return true;
        }

        private void EncodeBackground(object sender, DoWorkEventArgs e)
        {
            EncArgument arg = (EncArgument)e.Argument;
            e.Result = arg.index;
            using (Process p = new Process())
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                org_name_ = Path.GetFileName(arg.path);
                encoding_name_ = "[ENC]" + org_name_;
                

                p.EnableRaisingEvents = true;
                p.StartInfo.FileName = "ffmpeg.exe";
                p.StartInfo.Arguments = "-y -i \"" + arg.path + "\" -c:a copy -c:s copy -c:v h264 -crf 28 \"" + encoding_name_ + "\"";
                p.StartInfo.WorkingDirectory = "";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                string readStr = "";
                int duration_seconds = 0;

                while ((readStr = p.StandardError.ReadLine()) != null)
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
                    System.Threading.Thread.Sleep(10);
                }
                p.WaitForExit();
            }
        }

        private void OnFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            is_encoding_ = false;
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
                CustomRename(encoding_name_, org_name_);
                delegate_on_finished(index_);
                if (e.Result != null)
                {
                }
            }
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            delegate_on_progress_changed(index_, e.ProgressPercentage);
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
            public EncArgument(int _index, string _path)
            {
                index = _index;
                path = _path;
            }

            public int index;
            public string path;
        }
    }
}
