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
        const string kLogPath = "log.txt";

        public static void Write(string []msg)
        {
            File.AppendAllLines(kLogPath, msg);
        }

        public static void Write(string msg)
        {
            using (var writer = new StreamWriter(kLogPath, append: true))
            {
                writer.WriteLine(msg);
            }
        }
    }
}
