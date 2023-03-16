using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YsCommon
{
    class Loger
    {
        private static string TAG = "Loger.cs : ";
        const string kLogPath = "log.txt";

        public static void Write(string []msg)
        {
            File.AppendAllLines(kLogPath, msg);
        }

        public static void Write(string msg)
        {
            try
            {
                using (var writer = new StreamWriter(kLogPath, append: true))
                {
                    writer.WriteLine(msg);
                }
            }
            catch (Exception e)
            {
                Loger.Write(TAG + "Write : " + e.ToString());
            }
        }
    }
}
