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

        public static long ParseSize(string line)
        {
            long ret = -1;
            string video_str = "video:";
            string audio_str = "audio:";
            int video_index = line.IndexOf(video_str);
            int audio_index = line.IndexOf(audio_str);
            if (video_index >= 0 && audio_index >= 0)
            {
                int video_start = video_index + video_str.Length;
                int audio_start = audio_index + audio_str.Length;
                int video_end = line.IndexOf("kB", video_start);
                int audio_end = line.IndexOf("kB", audio_start);
                int video_length = video_end - video_start;
                int audio_length = audio_end - audio_start;
                if (video_length > 0 && audio_length > 0)
                {
                    string video_size_str = line.Substring(video_start, video_length);
                    string audio_size_str = line.Substring(audio_start, audio_length);

                    int video_size = -1;
                    if (int.TryParse(video_size_str, out int v_size))
                    {
                        video_size = v_size;
                    }

                    int audio_size = -1;
                    if (int.TryParse(audio_size_str, out int a_size))
                    {
                        audio_size = a_size;
                    }

                    if (video_size >= 0 && audio_size >= 0)
                    {
                        ret = video_size + audio_size;
                    }
                }
            }
            return ret;
        }

        public Tuple<double, long, int> Calculate(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            is_canceled_ = false;
            return CalculateAverageSSIM(path, thread_num, crf, time_list);
        }

        private Tuple<double, long, int> CalculateAverageSSIM(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            double ret = -1;
            double ssim_sum = 0;
            int count = 0;
            long size = 0;
            int size_sec = 0;
            for (int i = 0; i < time_list.Count(); i++)
            {
                if (is_canceled_)
                {
                    break;
                }


                Tuple<double, long> result = CalculateSSIM(path, thread_num, crf, time_list[i].start_time, time_list[i].duration);
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

            return new Tuple<double, long, int>(ret, size, size_sec);
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
                    long sz = ParseSize(readStr);
                    if (sz >= 0)
                    {
                        size = sz;
                    }

                    double result = ParseSSIM(readStr);
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
