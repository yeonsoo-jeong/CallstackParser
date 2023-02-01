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

        public Tuple<double, int, long> Calculate(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            is_canceled_ = false;
            return CalculateAverageSSIM(path, thread_num, crf, time_list);
        }

        private Tuple<double, int, long> CalculateAverageSSIM(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            double ret = -1;
            double ssim_sum = 0;
            int count = 0;
            long size = 0;
            int size_sec = 0;
            for (int i = 0; i < time_list.Count(); i++)
            {
                Tuple<double, long> result = CalculateSSIM(path, thread_num, crf, time_list[i].start_time, time_list[i].duration);
                if (is_canceled_)
                {
                    return new Tuple<double, int, long>(-1, -1, -1);
                }
                double ssim_result = result.Item1;
                long sz = result.Item2;
                if (ssim_result > 0)
                {
                    {
                        // Log
                        string name = Path.GetFileName(path);
                        string msg = TAG + "name=" + name + ", crf=" + crf + ", start_time=" +
                            time_list[i].start_time + ", duration=" + time_list[i].duration + ", ssim=" + ssim_result + ", size=" + sz;
                        Loger.Write(msg);
                    }

                    ssim_sum += ssim_result;

                    if (sz > 0)
                    {
                        size += sz;
                        size_sec += time_list[i].duration;
                    }
                    

                    count++;
                }
            }

            if (count > 0)
            {
                ret = ssim_sum / count;
            }

            return new Tuple<double, int, long>(ret, size_sec, size);
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

        private Tuple<double, long> CalculateSSIM(string path, int thread_num, int crf, int start_time, int duration)
        {
            double ssim = -1;
            long size = 0;

            string interlace_option = "";
            if (MediaInfoUtil.IsInterlaced(path))
            {
                interlace_option = " -filter_complex \"[0:v:0]yadif=0:-1:0[v]\" -map [v]";
            }

            using (enc_process_ = new Process())
            {
                enc_process_.EnableRaisingEvents = true;
                enc_process_.StartInfo.FileName = "ffmpeg.exe";
                enc_process_.StartInfo.Arguments = "-y -threads " + thread_num + " -i \"" + path + "\"" + interlace_option + " -an -sn -c:v h264 -crf " + crf + " -ss " + start_time + " -t " + duration + " -ssim 1 -f null /dev/null";
                enc_process_.StartInfo.WorkingDirectory = "";
                enc_process_.StartInfo.CreateNoWindow = true;
                enc_process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                enc_process_.StartInfo.RedirectStandardOutput = true;
                enc_process_.StartInfo.RedirectStandardError = true;
                enc_process_.Start();

                string readStr = "";
                while ((readStr = enc_process_.StandardError.ReadLine()) != null)
                {
                    if (is_canceled_)
                    {
                        break;
                    }

                    long sz = FFmpegUtil.ParseSize(readStr);
                    if (sz >= 0)
                    {
                        size = sz;
                    }

                    double result = FFmpegUtil.ParseSSIM(readStr);
                    if (result >= 0)
                    {
                        ssim = result;
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }

            return new Tuple<double, long>(ssim, size);
        }
    }
}
