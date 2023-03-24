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

using YsCommon;

namespace ShakaCallstackParser
{
    class SSIMCalculator
    {
        public class Callbacks
        {
            public delegate void OnProgressChanged(int percentage);
            public Callbacks(OnProgressChanged pc)
            {
                progress_changed = pc;
            }

            public OnProgressChanged progress_changed;
        }
        Callbacks callbacks_;

        public class AnalyzeItem
        {
            public AnalyzeItem(string _inp_tag, int _start_time, int _duration)
            {
                inp_tag = _inp_tag;
                start_time = _start_time;
                duration = _duration;
            }
            public string inp_tag;
            public int start_time;
            public int duration;
        }

        const string TAG = "SSIMCalculator.cs : ";

        Process enc_process_ = null;
        bool is_canceled_ = false;

        public SSIMCalculator(Callbacks callback)
        {
            callbacks_ = callback;
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

        private string MakeOption(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            bool is_use_filter = false;
            bool is_interlace = false;
            string interlace_option = "";
            string split_option = "";
            string filter_option = "";
            string ret_option = "";
            string inp_tag = "[0:v:0]";
            int items_num = time_list.Count;
            List<AnalyzeItem> analyze_items = new List<AnalyzeItem>();
            
            if (items_num < 1)
            {
                Loger.Write(TAG + "MakeOption : time list is empty.");
                return "";
            }
            is_interlace = MediaInfoUtil.IsInterlaced(path);

            if (is_interlace)
            {
                is_use_filter = true;
                interlace_option = "\"" + inp_tag + "yadif=0:-1:0[v]";
                inp_tag = "[v]";
                if (items_num == 1)
                {
                    analyze_items.Add(new AnalyzeItem("[v]", time_list[0].start_time, time_list[0].duration));
                }
            }

            if (items_num > 1)
            {
                is_use_filter = true;
                if (is_interlace)
                {
                    split_option = ";";
                }
                else
                {
                    split_option = "\"";
                }
                split_option += inp_tag + "split=outputs=" + items_num;

                for (int i = 0; i < items_num; i++)
                {
                    string tag = "[v" + i + "]";
                    split_option += tag;
                    analyze_items.Add(new AnalyzeItem(tag, time_list[i].start_time, time_list[i].duration));
                }
            }

            if (is_use_filter)
            {
                filter_option = " -filter_complex " + interlace_option + split_option + "\"";
            }
            else
            {
                if (analyze_items.Count != 0)
                {
                    Loger.Write(TAG + "MakeOption : No filter used, but analyze_items.Count is not zero. count=" + analyze_items.Count);
                    return "";
                }
                analyze_items.Add(new AnalyzeItem("0:v:0", time_list[0].start_time, time_list[0].duration));
            }

            ret_option = "-y -vsync passthrough -i \"" + path + "\"" + filter_option + " -threads " + thread_num + " -an -sn";
            for (int i = 0; i < analyze_items.Count; i++)
            {
                string _inp_tag = analyze_items[i].inp_tag;
                int _start = analyze_items[i].start_time;
                int _duration = analyze_items[i].duration;
                ret_option += " -map " + _inp_tag + " -ss " + _start + " -t " + _duration + " -crf " + crf + " -c:v h264 -ssim 1 -f null /dev/null";
            }

            return ret_option;
        }

        public Tuple<double, int, long> CalculateAverageSSIM(string path, int thread_num, int crf, List<AnalyzeTimeSelector.TimePair> time_list)
        {
            is_canceled_ = false;
            double ssim = 0;
            long size = 0;
            int count = 0;
            string command = MakeOption(path, thread_num, crf, time_list);

            Loger.Write(TAG + "CalculateAverageSSIM : command=" + command);

            try
            {
                using (enc_process_ = new Process())
                {
                    enc_process_.EnableRaisingEvents = true;
                    enc_process_.StartInfo.FileName = "ffmpeg.exe";
                    enc_process_.StartInfo.Arguments = command;
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
                            return new Tuple<double, int, long>(0, 0, 0);
                        }

                        long sz = FFmpegUtil.ParseSize(readStr);
                        if (sz >= 0)
                        {
                            size = sz;
                        }

                        double result = FFmpegUtil.ParseSSIM(readStr);
                        if (result >= 0)
                        {
                            ssim += result;
                            {
                                // Log
                                string name = Path.GetFileName(path);
                                string msg = TAG + " CalculateAverageSSIM : name=" + name + ", crf=" + crf + ", start_time=" +
                                    time_list[count].start_time + ", duration=" + time_list[count].duration + ", ssim=" + Math.Round(result, 4);
                                Loger.Write(msg);
                            }                            
                            count++;
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
            catch (Exception e)
            {
                Loger.Write(TAG + "CalculateAverageSSIM : " + e.ToString());
                return new Tuple<double, int, long>(0, 0, 0);
            }

            if (ssim <= 0 || size <= 0)
            {
                return new Tuple<double, int, long>(0, 0, 0);
            }            

            if (count > 0)
            {
                ssim /= (double)count;
            }

            int size_second = 0;
            for (int i = 0; i < time_list.Count; i++)
            {
                size_second += time_list[i].duration;
            }
            // callbacks_.progress_changed(100);
            
            return new Tuple<double, int, long>(ssim, size_second, size);
        }
    }
}