using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShakaCallstackParser
{
    class VDVParser
    {
        public class Callbacks
        {
            public delegate void OnPTSParsed(string v_pts, string a_pts, string pict_type);
            public Callbacks(OnPTSParsed f)
            {
                parsed = f;
            }
            public OnPTSParsed parsed;
        }
        Callbacks callbacks_;

        Process parse_process_;

        public VDVParser(Callbacks callback)
        {
            callbacks_ = callback;
        }

        public void Start(string path)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(ParseBackground);
            //worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnFinished);

            worker.RunWorkerAsync(argument: path);
        }

        private void ParseBackground(object sender, DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            double ret = -1;
            using (parse_process_ = new Process())
            {
                parse_process_.EnableRaisingEvents = true;
                //parse_process_.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);
                //parse_process_.ErrorDataReceived += new DataReceivedEventHandler(process_ErrorDataReceived);
                //parse_process_.Exited += new EventHandler(process_Exited);

                parse_process_.StartInfo.FileName = "ffprobe.exe";
                parse_process_.StartInfo.Arguments = "-i \"" + path + "\" -show_frames";
                parse_process_.StartInfo.WorkingDirectory = "";
                parse_process_.StartInfo.CreateNoWindow = true;
                parse_process_.StartInfo.UseShellExecute = false;    // CreateNoWindow(true)가 적용되려면 반드시 false이어야 함
                parse_process_.StartInfo.RedirectStandardOutput = true;
                parse_process_.StartInfo.RedirectStandardError = true;
                parse_process_.Start();

                //parse_process_.BeginErrorReadLine();
                //parse_process_.BeginOutputReadLine();

                string readStr = "";
                const string frame_open_tag = "[FRAME]";
                const string frame_close_tag = "[/FRAME]";
                bool is_tag_open = false;
                List<string> metadata = new List<string>();
                while ((readStr = parse_process_.StandardOutput.ReadLine()) != null)
                {
                    if (is_tag_open)
                    {
                        int index = readStr.IndexOf(frame_close_tag);
                        if (index >= 0)
                        {
                            is_tag_open = false;
                            Result result = ParseFrameData(metadata);
                            if (result.stream_index < 2)
                            {
                                bool is_parsed = false;
                                if (result.media_type == "video")
                                {
                                    callbacks_.parsed(result.pts, "-1", result.pict_type);
                                    is_parsed = true;
                                }
                                else if (result.media_type == "audio")
                                {
                                    callbacks_.parsed("-1", result.pts, "n");
                                    is_parsed = true;
                                }

                                if (is_parsed)
                                {
                                    
                                }
                            }
                        }
                        else
                        {
                            metadata.Add(readStr);
                        }
                    }
                    else
                    {
                        int index = readStr.IndexOf(frame_open_tag);
                        if (index >= 0)
                        {
                            is_tag_open = true;
                            metadata.Clear();
                        }
                    }
                    System.Threading.Thread.Sleep(1);
                }
                parse_process_.WaitForExit();
            }
        }

        private Result ParseFrameData(List<string> frame_data)
        {
            const string kStreamIndex = "stream_index=";
            const string kMediaType = "media_type=";
            const string kPts = "pkt_pts="; // windows
            const string kPictType = "pict_type=";
            int stream_index = -1;
            string media_type = "";
            string pts = "";
            string pict_type = "";
            foreach (string data in frame_data)
            {
                if (data.IndexOf(kStreamIndex) == 0)
                {
                    stream_index = Int32.Parse(data.Substring(kStreamIndex.Length));
                }
                else if (data.IndexOf(kMediaType) == 0)
                {
                    media_type = data.Substring(kMediaType.Length);
                }
                else if (data.IndexOf(kPts) == 0)
                {
                    pts = data.Substring(kPts.Length);
                }
                else if (data.IndexOf(kPictType) == 0)
                {
                    pict_type = data.Substring(kPictType.Length);
                }
            }

            return new Result(stream_index, media_type, pts, pict_type);
        }

        private void process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine(string.Format("process exited with code {0}\n", parse_process_.ExitCode.ToString()));
        }

        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }

        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data + "\n");
        }


        class Result
        {
            public Result(int _stream_index, string _media_type, string _pts, string _pict_type)
            {
                stream_index = _stream_index;
                media_type = _media_type;
                pts = _pts;
                pict_type = _pict_type;
            }
            public int stream_index;
            public string media_type;
            public string pts;
            public string pict_type;
        }
    }
}
