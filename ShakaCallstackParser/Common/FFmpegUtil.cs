using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YsCommon
{
    class FFmpegUtil
    {
        private static string TAG = "FFmpegUtil.cs : ";
        public static int GetDurationSec(string path)
        {
            try
            {
                using (Process p = new Process())
                {
                    p.EnableRaisingEvents = true;
                    p.StartInfo.FileName = "ffmpeg.exe";
                    p.StartInfo.Arguments = "-y -i \"" + path + "\" -c copy -f null /dev/null";
                    p.StartInfo.WorkingDirectory = "";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

                    string readStr = "";
                    int duration_seconds = -1;
                    while ((readStr = p.StandardError.ReadLine()) != null)
                    {
                        int sec = ParseDurationSeconds(readStr);
                        if (sec >= 0)
                        {
                            duration_seconds = sec;
                            break;
                        }
                        System.Threading.Thread.Sleep(10);
                    }

                    return duration_seconds;
                }
            } 
            catch (Exception e)
            {
                Loger.Write(TAG + "GetDurationSec : " + e.ToString());
                return -1;
            }
        }

        public static Tuple<long, int> GetSizeDurationSec(string path)
        {
            int duration_seconds = -1;
            long size = -1;

            try
            {
                using (Process p = new Process())
                {
                    p.EnableRaisingEvents = true;
                    p.StartInfo.FileName = "ffmpeg.exe";
                    p.StartInfo.Arguments = "-y -i \"" + path + "\" -c copy -f null /dev/null";
                    p.StartInfo.WorkingDirectory = "";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

                    string readStr = "";

                    while ((readStr = p.StandardError.ReadLine()) != null)
                    {
                        long sz = ParseSize(readStr);
                        if (sz >= 0)
                        {
                            size = sz;
                        }

                        int sec = ParseDurationSeconds(readStr);
                        if (sec >= 0)
                        {
                            duration_seconds = sec;
                        }

                        if (size >= 0 && duration_seconds >= 0)
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(10);
                    }

                    return new Tuple<long, int>(size, duration_seconds);
                }
            }
            catch (Exception e)
            {
                Loger.Write(TAG + "GetSizeDurationSec : " + e.ToString());
                return new Tuple<long, int>(-1, -1);
            }
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

        private static int ParseDurationSeconds(string duration_str)
        {
            if (duration_str.Length > 12 && duration_str.Substring(0, 12) == "  Duration: ")
            {
                string substr = duration_str.Substring(12, 11);
                int hour = int.Parse(substr.Substring(0, 2));
                int minute = int.Parse(substr.Substring(3, 2));
                int second = int.Parse(substr.Substring(6, 2));
                return second + (minute * 60) + (hour * 3600);
            }

            return -1;
        }
    }
}

