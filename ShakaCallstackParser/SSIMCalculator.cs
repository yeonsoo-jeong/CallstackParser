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
        const string TAG = "SSIMCalculator.cs : ";

        Process enc_process_ = null;
        bool is_canceled_ = false;

        public SSIMCalculator()
        {
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

        public Tuple<double, bool> Calculate(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            is_canceled_ = false;
            return CalculateAverageSSIM(path, thread_num, crf, time_list);
        }

        private Tuple<double, bool> CalculateAverageSSIM(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            double ret = -1;
            double ssim_sum = 0;
            int count = 0;
            bool is_cancel = false;
            for (int i = 0; i < time_list.Count(); i++)
            {
                if (is_canceled_)
                {
                    is_cancel = true;
                    break;
                }

                double ssim_result = CalculateSSIM(path, thread_num, crf, time_list[i].start_time, time_list[i].duration);
                if (ssim_result > 0)
                {
                    {
                        // Log
                        string name = Path.GetFileName(path);
                        string msg = "name=" + name + ", thread_num= " + thread_num + ", crf=" + crf + ", start_time=" +
                            time_list[i].start_time + ", duration=" + time_list[i].duration + ", ssim=" + ssim_result;
                        Loger.Write(msg);
                    }

                    ssim_sum += ssim_result;
                    count++;
                }
            }

            if (count > 0)
            {
                ret = ssim_sum / count;
            }

            return new Tuple<double, bool>(ret, is_cancel);
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
            }
        }

        private double CalculateSSIM(string path, int thread_num, int crf, int start_time, int duration)
        {
            double ret = -1;
            using (enc_process_ = new Process())
            {
                enc_process_.EnableRaisingEvents = true;
                enc_process_.StartInfo.FileName = "ffmpeg.exe";
                enc_process_.StartInfo.Arguments = "-y -i \"" + path + "\" -threads " + thread_num + " -an -sn -c:v h264 -crf " + crf + " -ss " + start_time + " -t " + duration + " -ssim 1 -f null /dev/null";
                enc_process_.StartInfo.WorkingDirectory = "";
                enc_process_.StartInfo.CreateNoWindow = true;
                enc_process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                enc_process_.StartInfo.RedirectStandardOutput = true;
                enc_process_.StartInfo.RedirectStandardError = true;
                enc_process_.Start();

                string readStr = "";
                while ((readStr = enc_process_.StandardError.ReadLine()) != null)
                {
                    double result = ParseSSIM(readStr);
                    if (result >= 0)
                    {
                        ret = result;
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }

            return ret;
        }
    }
}
