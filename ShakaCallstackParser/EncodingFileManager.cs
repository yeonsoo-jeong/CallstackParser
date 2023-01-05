using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace ShakaCallstackParser
{
    static class EncodingFileManager
    {
        const string TAG = "EncodingFileManager.cs : ";
        const string kManageFilePath = "config.txt";

        public static void EncodingStarted(string path)
        {
            using (StreamWriter writer = File.AppendText(kManageFilePath))
            {
                writer.WriteLine(path);
            }
        }

        public static void EncodingFinished(string path)
        {
            if (File.Exists(kManageFilePath))
            {
                List<string> lines = new List<string>(File.ReadAllLines(kManageFilePath));
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i] == path)
                    {
                        lines.RemoveAt(i);
                    }
                }
                File.WriteAllLines(kManageFilePath, lines);
            }
        }

        public static void DeleteAllTempFiles()
        {
            if (File.Exists(kManageFilePath))
            {
                List<string> lines = new List<string>(File.ReadAllLines(kManageFilePath));
                for (int i = lines.Count-1; i >= 0 ; i--)
                {
                    lines[i] = lines[i].Trim();
                    if (lines[i].Length <= 0)
                    {
                        lines.RemoveAt(i);
                    }
                    else if (!File.Exists(lines[i]))
                    {
                        lines.RemoveAt(i);
                    }
                    else
                    {
                        bool is_success = false;
                        int count = 0;
                        while (count++ < 5 && !is_success)
                        {
                            try
                            {
                                File.Delete(lines[i]);
                                lines.RemoveAt(i);
                                is_success = true;
                                Loger.Write(TAG + "DeleteAllTempFiles : Delete success. count=" + count);
                            }
                            catch (Exception e)
                            {
                                System.Threading.Thread.Sleep(10);
                            }
                        }
                        if (!is_success)
                        {
                            Loger.Write(TAG + "DeleteAllTempFiles : Delete failed. count=" + count);
                        }
                    }
                }
                File.WriteAllLines(kManageFilePath, lines);
            }
        }
    }
}
