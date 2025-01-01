using System;
using System.IO;

namespace HtmlGrabber.Models
{
    internal class ErrorHandle
    {
        private static readonly object locker = new object();

        public void WriteToLogFile(string message)
        {
            lock (locker)
            {
                string docPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                int min = DateTime.Now.Minute;
                string fileName;
                string format;

                if (min % 5 == 0)
                {
                    format = "dd-MM-yyyy HH-mm";
                }
                else
                {
                    int old_min = min - (min % 5);
                    format = "dd-MM-yyyy HH-" + old_min.ToString("D2");
                }

                fileName = "Log_" + DateTime.Now.ToString(format) + ".txt";
                string filePath = Path.Combine(docPath, fileName);

                if (!Directory.Exists(docPath))
                {
                    Directory.CreateDirectory(docPath);
                }

                using StreamWriter outputFile = new(filePath, append: true);
                outputFile.WriteLine($"{DateTime.Now}\t{message}");
            }
        }
    }
}
