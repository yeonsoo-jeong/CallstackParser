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
        public class Callbacks
        {
            public delegate void OnFinished(int index, int crf, double ssim);
            public Callbacks(OnFinished f)
            {
                finished = f;
            }
            public OnFinished finished;
        }
        Callbacks callbacks_;

        public SSIMCalculator(Callbacks callback)
        {
            callbacks_ = callback;
        }

        public static double ParseSSIM(string line)
        {
            double ret = -1;
            string findStr = "SSIM Mean Y:";
            int index = line.IndexOf(findStr);
            if (index >= 0)
            {
                int start = index + findStr.Length;
                string substr = line.Substring(start);
                var arr = substr.Split(' ');
                double ssim;
                if (Double.TryParse(arr[0], out ssim))
                {
                    ret = ssim;
                }
            }
            return ret;
        }

        public void Calculate(int index, string path, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(EncodeBackground);
            worker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnFinished);

            CalcArgument arg = new CalcArgument(index, path, crf, time_list);
            worker.RunWorkerAsync(argument: arg);
        }

        private void EncodeBackground(object sender, DoWorkEventArgs e)
        {
            CalcArgument arg = (CalcArgument)e.Argument;
            List<AnalyzeTimeSelector.TimePair> time_list = arg.time_pair_list;

            double ssim = 0;
            int count = 0;   
            for (int i = 0; i < time_list.Count(); i++)
            {
                double ssim_result = CalculateSSIM(arg.path, arg.crf, time_list[i].start_time, time_list[i].duration);
                if (ssim_result > 0)
                {
                    {
                        // Log
                        string name = Path.GetFileName(arg.path);
                        string msg = "name=" + name + ", crf=" + arg.crf + ", start_time=" + time_list[i].start_time + ", duration=" + time_list[i].duration + ", ssim=" + ssim_result;

                        Loger.Write(msg);
                    }
                    
                    ssim += ssim_result;
                    count++;
                }
            }

            if (count > 0)
            {
                ssim /= (double)count;
            }

            e.Result = new CalcResult(arg.index, arg.crf, ssim);
        }

        private double CalculateSSIM(string path, int crf, int start_time, int duration)
        {
            double ret = -1;
            using (Process p = new Process())
            {
                p.EnableRaisingEvents = true;
                p.StartInfo.FileName = "ffmpeg.exe";
                p.StartInfo.Arguments = "-y -i \"" + path + "\" -an -sn -c:v h264 -crf " + crf + " -ss " + start_time + " -t " + duration + " -ssim 1 -f null /dev/null";
                p.StartInfo.WorkingDirectory = "";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                string readStr = "";
                //string findStr = "SSIM Mean Y:";
                while ((readStr = p.StandardError.ReadLine()) != null)
                {
                    double result = ParseSSIM(readStr);
                    if (result >= 0)
                    {
                        ret = result;
                    }
                    //int index = readStr.IndexOf(findStr);
                    //if (index >= 0)
                    //{
                    //    int start = index + findStr.Length;
                    //    string substr = readStr.Substring(start);
                    //    var arr = substr.Split(' ');
                    //    double ssim;
                    //    if (Double.TryParse(arr[0], out ssim))
                    //    {
                    //        ret = ssim;
                    //    }
                    //}
                    System.Threading.Thread.Sleep(10);
                }
                p.WaitForExit();
            }

            return ret;
        }

        private void OnFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            int index = ((CalcResult)(e.Result)).index;
            int crf = ((CalcResult)(e.Result)).crf;
            double ssim = ((CalcResult)(e.Result)).ssim;
            callbacks_.finished(index, crf, ssim);
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private class CalcArgument
        {
            public CalcArgument(int _index, string _path, int _crf, List<AnalyzeTimeSelector.TimePair> _time_list)
            {
                index = _index;
                path = _path;
                crf = _crf;
                time_pair_list = _time_list;
            }

            public int index;
            public string path;
            public int crf;
            public List<AnalyzeTimeSelector.TimePair> time_pair_list;
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
