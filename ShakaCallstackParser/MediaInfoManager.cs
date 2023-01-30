using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class MediaInfoManager
    {
        Process process_;
        bool is_cancel_ = false;

        public bool IsInterlaced(string path)
        {
            const string kInterlaced = "Interlaced";
            string scan_type = GetScanType(path);
            if (scan_type == kInterlaced)
            {
                return true;
            }
            return false;
        }

        public string GetScanType(string path)
        {
            is_cancel_ = false;

            int result_code = -1;
            string result = "";
            using (process_ = new Process())
            {
                process_.EnableRaisingEvents = true;
                process_.StartInfo.FileName = "MediaInfo.exe";
                process_.StartInfo.Arguments = path;
                process_.StartInfo.WorkingDirectory = "";
                process_.StartInfo.CreateNoWindow = true;
                process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                process_.StartInfo.RedirectStandardOutput = true;
                process_.StartInfo.RedirectStandardError = true;
                process_.Start();

                string readStr = "";
                
                while ((readStr = process_.StandardOutput.ReadLine()) != null)
                {
                    if (is_cancel_)
                    {
                        return "";
                    }
                    string scan_type = ParseScanType(readStr);
                    if (scan_type.Length > 0)
                    {
                        result = scan_type;
                        break;
                    }
                }

                process_.WaitForExit();
                result_code = process_.ExitCode;
            }
            
            return result;
        }

        public Tuple<int, int, int> GetStreamNum(string path)
        {
            is_cancel_ = false;

            int result_code = -1;
            int video_count = 0;
            int audio_count = 0;
            int text_count = 0;
            using (process_ = new Process())
            {
                process_.EnableRaisingEvents = true;
                process_.StartInfo.FileName = "MediaInfo.exe";
                process_.StartInfo.Arguments = path;
                process_.StartInfo.WorkingDirectory = "";
                process_.StartInfo.CreateNoWindow = true;
                process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                process_.StartInfo.RedirectStandardOutput = true;
                process_.StartInfo.RedirectStandardError = true;
                process_.Start();

                string readStr = "";
                while ((readStr = process_.StandardOutput.ReadLine()) != null)
                {
                    if (is_cancel_)
                    {
                        return new Tuple<int, int, int>(-1, -1, -1);
                    }
                    if (IsVideoStartTag(readStr))
                    {
                        video_count++;
                    } 
                    else if (IsAudioStartTag(readStr))
                    {
                        audio_count++;
                    }
                    else if (IsTextStartTag(readStr))
                    {
                        text_count++;
                    }
                }

                process_.WaitForExit();
                result_code = process_.ExitCode;
            }

            return new Tuple<int, int, int>(video_count, audio_count, text_count);
        }

        public void OnCanceled()
        {
            is_cancel_ = true;
        }

        public void OnWindowClosed()
        {
            is_cancel_ = true;
        }

        private string ParseScanType(string line)
        {
            string tag = "Scan type";
            if (line.StartsWith(tag))
            {
                string[] items = line.Split(':');
                if (items.Length == 2)
                {
                    return items[1].Trim();
                }
            }

            return "";
        }

        private bool IsVideoStartTag(string line)
        {
            string tag = "Video";
            if (line.StartsWith(tag))
            {
                return true;
            }

            return false;
        }

        private bool IsAudioStartTag(string line)
        {
            string tag = "Audio";
            if (line.StartsWith(tag))
            {
                return true;
            }

            return false;
        }

        private bool IsTextStartTag(string line)
        {
            string tag = "Text";
            if (line.StartsWith(tag))
            {
                return true;
            }

            return false;
        }
    }
}
