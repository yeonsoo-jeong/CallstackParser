using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ShakaCallstackParser
{
    class SSIMCalculator
    {
        public delegate void OnFinishedDelegate(int index, int crf, double ssim);
        OnFinishedDelegate delegate_on_finished;

        public SSIMCalculator(OnFinishedDelegate f)
        {
            delegate_on_finished = f;
        }

        public void Calculate(int index, string path, int crf, int start_time, int duration)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(EncodeBackground);
            worker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnFinished);

            CalcArgument arg = new CalcArgument(index, path, crf, start_time, duration);
            worker.RunWorkerAsync(argument: arg);
        }

        private void EncodeBackground(object sender, DoWorkEventArgs e)
        {
            CalcArgument arg = (CalcArgument)e.Argument;
            CalcResult result = new CalcResult(arg.index, arg.crf, -1);
            e.Result = result;

            using (Process p = new Process())
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                p.EnableRaisingEvents = true;
                p.StartInfo.FileName = "ffmpeg.exe";
                p.StartInfo.Arguments = "-y -i \"" + arg.path + "\" -an -sn -c:v h264 -crf " + arg.crf + " -ss " + arg.start_time + " -t " + arg.duration + " -ssim 1 -f null /dev/null";
                p.StartInfo.WorkingDirectory = "";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                string readStr = "";
                string findStr = "SSIM Mean Y:";
                while ((readStr = p.StandardError.ReadLine()) != null)
                {
                    int index = readStr.IndexOf(findStr);
                    if (index >= 0)
                    {
                        int start = index + findStr.Length;
                        string substr = readStr.Substring(start);
                        var arr = substr.Split(' ');
                        double ssim;
                        if (Double.TryParse(arr[0], out ssim))
                        {
                            ((CalcResult)(e.Result)).ssim = ssim;
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }
                p.WaitForExit();
            }
        }

        private void OnFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            int index = ((CalcResult)(e.Result)).index;
            int crf = ((CalcResult)(e.Result)).crf;
            double ssim = ((CalcResult)(e.Result)).ssim;
            delegate_on_finished(index, crf, ssim);
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private class CalcArgument
        {
            public CalcArgument(int _index, string _path, int _crf, int _start_time, int _duration)
            {
                index = _index;
                path = _path;
                crf = _crf;
                start_time = _start_time;
                duration = _duration;
            }

            public int index;
            public string path;
            public int crf;
            public int start_time;
            public int duration;
        }

        private class CalcResult
        {
            public CalcResult(int _index, int _crf, double _ssim)
            {
                index = _index;
                crf = _crf;
                ssim = _ssim;
            }

            public int index;
            public int crf;
            public double ssim;
        }
    }
}
