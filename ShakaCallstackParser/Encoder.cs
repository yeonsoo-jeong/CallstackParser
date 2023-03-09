using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YsCommon;

namespace ShakaCallstackParser
{
    class Encoder
    {
        const string TAG = "Encoder.cs : ";
        const string kEncodingPrefix = "[Temp]";
        const string kEncSuccessPrefix = "RE_";
        const string kEncOversizePrefix = "ORG_";

        public class Callbacks
        {
            public delegate void OnProgressChanged(int id, int percentage);
            public Callbacks(OnProgressChanged pc)
            {
                progress_changed = pc;
            }

            public OnProgressChanged progress_changed;
        }
        Callbacks callbacks_;

        Process enc_process_ = null;

        bool is_encoding_ = false;
        bool is_canceled_ = false;

        public Encoder(Callbacks callback)
        {
            callbacks_ = callback;
        }

        public EncoderResult Encode(int id, string inpPath, string out_directory, int thread_num, int crf, long expect_size, out int ffmpeg_return_code, out double ret_ssim)
        {
            ffmpeg_return_code = -1;
            ret_ssim = -1;
            if (is_encoding_)
            {
                return EncoderResult.already_encoding;
            }
            is_encoding_ = true;
            is_canceled_ = false;

            long inp_size = GetFileSize(inpPath);
            string encoding_path = out_directory + "\\" + kEncodingPrefix + Path.GetFileName(inpPath);
            string encoding_option = GetEncodingOption(inpPath, encoding_path, thread_num, crf);

            using (enc_process_ = new Process())
            {
                EncodingFileManager.EncodingStarted(encoding_path);

                enc_process_.EnableRaisingEvents = true;
                enc_process_.StartInfo.FileName = "ffmpeg.exe";
                enc_process_.StartInfo.Arguments = encoding_option;

                Loger.Write(TAG + "Encode : option = " + enc_process_.StartInfo.Arguments);

                enc_process_.StartInfo.WorkingDirectory = "";
                enc_process_.StartInfo.CreateNoWindow = true;
                enc_process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                enc_process_.StartInfo.RedirectStandardOutput = true;
                enc_process_.StartInfo.RedirectStandardError = true;
                enc_process_.Start();

                string readStr = "";
                int duration_seconds = -1;

                while ((readStr = enc_process_.StandardError.ReadLine()) != null)
                {
                    if (is_canceled_)
                    {
                        is_encoding_ = false;
                        return EncoderResult.fail;
                    }

                    if (duration_seconds == -1)
                    {
                        if (readStr.Length > 12 && readStr.Substring(0, 12) == "  Duration: ")
                        {
                            string substr = readStr.Substring(12, 11);
                            duration_seconds = CalculateSeconds(substr);
                        }
                    } 
                    else
                    {
                        if (readStr.Length > 6 && readStr.Substring(0, 6) == "frame=")
                        {
                            int idx = readStr.IndexOf("time=");
                            if (idx >= 0)
                            {
                                string substr = readStr.Substring(idx + 5, 11);
                                int seconds = CalculateSeconds(substr);
                                int percentage = Convert.ToInt32((float)seconds / (float)duration_seconds * 100);
                                OnProgressChanged(id, percentage);
                            }
                        }
                    }

                    double parse_ssim = FFmpegUtil.ParseSSIM(readStr);
                    if (parse_ssim >= 0)
                    {
                        ret_ssim = parse_ssim;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                enc_process_.WaitForExit();
                ffmpeg_return_code = enc_process_.ExitCode;
            }

            if (is_canceled_)
            {
                is_encoding_ = false;
                return EncoderResult.fail;
            }

            EncoderResult ret = EncoderResult.success;
            if (ffmpeg_return_code != 0)
            {
                ret = EncoderResult.fail;
                EncodingFileManager.DeleteAllTempFiles();
            }
            else
            {
                long enc_size = GetFileSize(encoding_path);
                if (inp_size < enc_size)
                {
                    ret = EncoderResult.size_over;
                    EncodingFileManager.DeleteAllTempFiles();
                }
                else
                {
                    CustomRename(Path.GetFileName(inpPath), encoding_path);
                    EncodingFileManager.EncodingFinished(encoding_path);
                }

                double inp_size_mb = Math.Truncate((double)inp_size / 1024 / 1024);
                double enc_size_mb = Math.Truncate((double)enc_size / 1024 / 1024);
                double exp_size_mb = Math.Truncate((double)expect_size / 1024);
                if (inp_size_mb <= 0 || enc_size_mb <= 0 || exp_size_mb <= 0)
                {
                    Loger.Write(TAG + "Encode : one of these less than 0. inp_size=" + inp_size_mb + "M, result_size=" + enc_size_mb + "M, expect_size=" + exp_size_mb + "M");
                } 
                else
                {
                    double expect_ratio = Math.Round(enc_size_mb / exp_size_mb, 2);
                    double inp_ratio = Math.Round(enc_size_mb / inp_size_mb, 2);
                    Loger.Write(TAG + "Encode : expect_size=" + exp_size_mb + "M, result_size=" + enc_size_mb + "M, ratio=" + expect_ratio);
                    Loger.Write(TAG + "Encode : inp_size=" + inp_size_mb + "M, result_size=" + enc_size_mb + "M, ratio=" + inp_ratio);
                }
            }
            
            is_encoding_ = false;
            return ret;
        }

        private string GetEncodingOption(string inpPath, string encoding_path, int thread_num, int crf)
        {
            Tuple<int, int, int> num_tuple = MediaInfoUtil.GetStreamNum(inpPath);
            int video_num = num_tuple.Item1;
            int audio_num = num_tuple.Item2;
            int text_num = num_tuple.Item3;
            if (video_num <= 0)
            {
                Loger.Write(TAG + "Start : Video is no exist");
                return "";
            }

            string interlace_option = "";
            string video_map_option = "";
            string audio_map_option = "";
            string text_map_option = "";
            if (MediaInfoUtil.IsInterlaced(inpPath))
            {
                interlace_option = " -filter_complex \"[0:v:0]yadif=0:-1:0[v]\" -map [v]";
                for (int i = 1; i < video_num; i++)
                {
                    video_map_option = " -map 0:v:" + i;
                }
            }
            else
            {
                video_map_option = " -map 0:v";
            }

            if (audio_num > 0)
            {
                audio_map_option = " -map 0:a";
            }

            if (text_num > 0)
            {
                text_map_option = " -map 0:s";
            }

            string option = "-y -vsync passthrough -threads " + thread_num +
                " -i \"" + inpPath + "\"" + interlace_option + video_map_option + audio_map_option + text_map_option +
                " -c:a copy -c:s copy -c:v h264 -ssim 1 -crf " + crf + " \"" + encoding_path + "\"";

            return option;
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
            finally
            {
                EncodingFileManager.DeleteAllTempFiles();
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
            finally
            {
                EncodingFileManager.DeleteAllTempFiles();
            }
        }

        private void OnProgressChanged(int id, int percentage)
        {
            callbacks_.progress_changed(id, percentage);
            //MessageBox.Show(e.ProgressPercentage.ToString() + "%");
        }

        private int CalculateSeconds(string time_str)
        {
            int hour = int.Parse(time_str.Substring(0, 2));
            int minute = int.Parse(time_str.Substring(3, 2));
            int second = int.Parse(time_str.Substring(6, 2));
            return second + (minute * 60) + (hour * 3600);
        }

        private static long GetFileSize(string path)
        {
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                return info.Length;
            }
            return 0;
        }

        private static void CustomRename(string inp_name, string enc_path)
        {
            if (File.Exists(enc_path))
            {
                string base_directory = Path.GetDirectoryName(enc_path) + "\\";
                string out_name = base_directory + kEncSuccessPrefix + inp_name;
                if (File.Exists(out_name))
                {
                    const int max = 10000;
                    for (int i = 0; i < max; i++)
                    {
                        out_name = base_directory + kEncSuccessPrefix + i + "_" + inp_name;
                        if (!File.Exists(out_name))
                        {
                            File.Move(enc_path, out_name);
                            break;
                        }
                    }
                }
                else
                {
                    File.Move(enc_path, out_name);
                }
            }
            else
            {
                Loger.Write(TAG + "CustomRename : Encoding result file is not exist. name=" + enc_path);
            }
        }

        public enum EncoderResult
        {
            success,
            fail,
            already_encoding,
            size_over
        }
    }
}
