﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YsCommon
{
    static class MediaInfoUtil
    {
        private static string TAG = "MediaInfoUtil.cs : ";
        public static bool IsInterlaced(string path)
        {
            const string kInterlaced = "Interlaced";
            string scan_type = GetScanType(path);
            if (scan_type == kInterlaced)
            {
                return true;
            }
            return false;
        }

        public static string GetScanType(string path)
        {
            int result_code = -1;
            string result = "";
            try
            {
                using (Process process = new Process())
                {
                    process.EnableRaisingEvents = true;
                    process.StartInfo.FileName = "MediaInfo.exe";
                    process.StartInfo.Arguments = path;
                    process.StartInfo.WorkingDirectory = "";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    string readStr = "";

                    while ((readStr = process.StandardOutput.ReadLine()) != null)
                    {
                        string scan_type = ParseScanType(readStr);
                        if (scan_type.Length > 0)
                        {
                            result = scan_type;
                            break;
                        }
                    }

                    int millsec = 2000;
                    bool is_exit = process.WaitForExit(millsec);
                    if (!is_exit)
                    {
                        process.Kill();
                    }
                    result_code = process.ExitCode;
                }
            }
            catch (Exception e)
            {
                Loger.Write(TAG + "GetScanType : " + e.ToString());
            }
            
            return result;
        }

        public static Tuple<int, int, int> GetStreamNum(string path)
        {
            int result_code = -1;
            int video_count = 0;
            int audio_count = 0;
            int text_count = 0;
            path = "\"" + path + "\"";

            try
            {
                using (Process process = new Process())
                {
                    process.EnableRaisingEvents = true;
                    process.StartInfo.FileName = "MediaInfo.exe";
                    process.StartInfo.Arguments = path;
                    process.StartInfo.WorkingDirectory = "";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    string readStr = "";
                    while ((readStr = process.StandardOutput.ReadLine()) != null)
                    {
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

                    process.WaitForExit();
                    result_code = process.ExitCode;
                }
            }
            catch (Exception e)
            {
                Loger.Write(TAG + "GetStreamNum : " + e.ToString());
            }

            return new Tuple<int, int, int>(video_count, audio_count, text_count);
        }

        private static string ParseScanType(string line)
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

        private static bool IsVideoStartTag(string line)
        {
            string tag = "Video";
            if (line.StartsWith(tag))
            {
                return true;
            }

            return false;
        }

        private static bool IsAudioStartTag(string line)
        {
            string tag = "Audio";
            if (line.StartsWith(tag))
            {
                return true;
            }

            return false;
        }

        private static bool IsTextStartTag(string line)
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
