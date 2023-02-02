using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YsCommon
{
    static class ConfigManager
    {
        const string TAG = "ConfigManager.cs : ";
        const string kConfigFilePath = "config.txt";

        public static void SetDestPath(string path)
        {
            File.WriteAllText(kConfigFilePath, path);
        }

        public static string GetDestPath()
        {
            string ret = "";
            if (File.Exists(kConfigFilePath))
            {
                string path = File.ReadAllText(kConfigFilePath).Trim();
                if (Directory.Exists(path))
                {
                    ret = path;
                }
            }

            if (ret == "")
            {
                ret = AppDomain.CurrentDomain.BaseDirectory;
            }
            return ret;
        }
    }
}
