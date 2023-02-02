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

        public List<TimePair> Calculate(int duration_seconds)
        {
            return CalculateAnalyzeTime(duration_seconds, kAnalyzeDuration);
        }

        private List<TimePair> CalculateAnalyzeTime(int input_duration, int analyze_duration)
        {
            List<TimePair> result = new List<TimePair>();
            if (input_duration <= analyze_duration)
            {
                result.Add(new TimePair(0, input_duration));
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
                int input_minute = input_duration / 60;
                int analyze_num = Math.Min(input_minute / 10 + 2, 5);

                int base_sec = (input_duration - analyze_duration) / 2;
                double denominator = analyze_num + 1;
                for (int i = 1; i <= analyze_num; i++)
                {
                    int start = (int)(base_sec * (i / denominator));
                    result.Add(new TimePair(start, analyze_duration));
                }
            }
            return result;
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
