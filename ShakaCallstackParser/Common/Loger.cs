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
            int count = 0;
            while (count++ < 10)
            {
                try
                {
                    using (var writer = new StreamWriter(kLogPath, append: true))
                    {
                        writer.WriteLine(msg);
                    }
                    return;
                }
                catch (Exception e)
                {
                    Loger.Write(TAG + "Write : tried to " + count);
                }
            }

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
