using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class AnalyzeTimeSelector
    {
        const int kAnalyzeDuration = 10;
        public AnalyzeTimeSelector()
        {

        }

        public List<TimePair> Calculate(string path)
        {
            int duration_sec = GetDurationSec(path);
            if (duration_sec < 0)
            {
                return new List<TimePair>();
            }
            return CalculateAnalyzeTime(duration_sec, kAnalyzeDuration);
        }

        private int GetDurationSec(string path)
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
                    if (readStr.Length > 12 && readStr.Substring(0, 12) == "  Duration: ")
                    {
                        string substr = readStr.Substring(12, 11);
                        duration_seconds = CalculateSeconds(substr);
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                return duration_seconds;
            }
        }

        private List<TimePair> CalculateAnalyzeTime(int input_duration, int analyze_duration)
        {
            List<TimePair> result = new List<TimePair>();
            if (input_duration <= analyze_duration)
            {
                result.Add(new TimePair(0, analyze_duration));
            } 
            else if (input_duration < 60)
            {
                int middle_sec = (input_duration - analyze_duration) / 2;
                result.Add(new TimePair(middle_sec, analyze_duration));
            }
            else if (input_duration < 60 * 10)
            {
                // 10 minute
                int middle_sec = (input_duration - analyze_duration) / 2;
                int left = middle_sec / 2;
                int right = middle_sec + (middle_sec / 2);
                result.Add(new TimePair(left, analyze_duration));
                result.Add(new TimePair(right, analyze_duration));
            }
            else
            {
                const int max = 10;
                int count = 2;
                while (count < max)
                {
                    if (input_duration < 600 * count++)
                    {
                        break;
                    }
                }

                int base_sec = (input_duration - analyze_duration) / 2;
                int denominator = count + 1;
                for (int i = 1; i <= count; i++)
                {
                    result.Add(new TimePair(base_sec * (i / denominator), analyze_duration));
                }
            }
            return result;
        }

        private int CalculateSeconds(string time_str)
        {
            int hour = int.Parse(time_str.Substring(0, 2));
            int minute = int.Parse(time_str.Substring(3, 2));
            int second = int.Parse(time_str.Substring(6, 2));
            return second + (minute * 60) + (hour * 3600);
        }

        public class TimePair
        {
            public TimePair(int _start_time, int _duration)
            {
                start_time = _start_time;
                duration = _duration;
            }
            public int start_time;
            public int duration;
        }
    }
}
