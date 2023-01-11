using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            public delegate void OnProgressChanged(int index, int percentage);
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


        public int Encode(int index, string inpPath, string out_directory, int thread_num, int crf)
        {
            if (is_encoding_)
            {
                return -1;
            }
            is_encoding_ = true;
            is_canceled_ = false;
            int result_code = -1;

            string encoding_path = "";

            double result = -1.0f;
            using (enc_process_ = new Process())
            {
                encoding_path = out_directory + "\\" + kEncodingPrefix + Path.GetFileName(inpPath);

                EncodingFileManager.EncodingStarted(encoding_path);

                enc_process_.EnableRaisingEvents = true;
                enc_process_.StartInfo.FileName = "ffmpeg.exe";
                enc_process_.StartInfo.Arguments = "-y -i \"" + inpPath + "\" -threads " + thread_num + " -c:a copy -c:s copy -c:v h264 -ssim 1 -crf " + crf + " \"" + encoding_path + "\"";
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
                        return -1;
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
                                OnProgressChanged(index, percentage);
                            }
                        }
                    }

                    double ssim = SSIMCalculator.ParseSSIM(readStr);
                    if (ssim >= 0)
                    {
                        result = ssim;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                enc_process_.WaitForExit();
                result_code = enc_process_.ExitCode;
            }

            if (is_canceled_)
            {
                return -1;
            }



            {
                // Log
                string msg = TAG + "OnFinished : Encode Finished. name=" + Path.GetFileName(inpPath) + ", ssim=" + result;
                Loger.Write(msg);
            }

            CustomRename(inpPath, Path.GetFileName(inpPath), encoding_path);

            is_encoding_ = false;

            return result_code;
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
                is_encoding_ = false;
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
                is_encoding_ = false;
                EncodingFileManager.DeleteAllTempFiles();
            }
        }

        private void OnProgressChanged(int index, int percentage)
        {
            callbacks_.progress_changed(index, percentage);
            //MessageBox.Show(e.ProgressPercentage.ToString() + "%");
        }

        private int CalculateSeconds(string time_str)
        {
            int hour = int.Parse(time_str.Substring(0, 2));
            int minute = int.Parse(time_str.Substring(3, 2));
            int second = int.Parse(time_str.Substring(6, 2));
            return second + (minute * 60) + (hour * 3600);
        }

        private static void CustomRename(string inp_path, string inp_name, string enc_path)
        {
            if (File.Exists(enc_path))
            {
                string base_directory = Path.GetDirectoryName(enc_path) + "\\";
                if (File.Exists(inp_path))
                {
                    FileInfo info = new FileInfo(inp_path);
                    long inp_size = info.Length;
                    info = new FileInfo(enc_path);
                    long enc_size = info.Length;
                    if (enc_size > inp_size)
                    {
                        string oversize_out_name = base_directory + kEncOversizePrefix + inp_name;
                        File.Copy(inp_path, oversize_out_name);
                        File.Delete(enc_path);
                        EncodingFileManager.EncodingFinished(enc_path);
                        return;
                    }
                }

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
                            EncodingFileManager.EncodingFinished(enc_path);
                            break;
                        }
                    }
                }
                else
                {
                    File.Move(enc_path, out_name);
                    EncodingFileManager.EncodingFinished(enc_path);
                }
            } 
            else
            {
                {
                    // Log
                    string msg = TAG + "CustomRename : Encoding result file is not exist. name=" + enc_path;
                    Loger.Write(msg);
                    Loger.Write("");
                }
            }
        }
    }
}
