using System;
using System.IO;

namespace HtmlGrabber.Models
{
    class ErrorHandle
    {
        private static object locker = new object();
        public void WriteToLogFile(string message)
        {
            lock (locker)
            {
                string docPath = AppDomain.CurrentDomain.BaseDirectory + "Log";
                int min = DateTime.Now.Minute;
                string fileName;
                string formate;
                if (min % 5 == 0)
                {
                    formate = "dd-MM-yyyy HH-mm";
                }
                else
                {
                    int old_min = min - (min % 5);
                    formate = "dd-MM-yyyy HH-" + old_min.ToString();
                }
                fileName = "Log_" + DateTime.Now.ToString(formate) + ".txt";
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, fileName)))
                {
                    outputFile.Write(DateTime.Now.ToString() + "\t" + message + "\n");
                }
            }
        }

    }
}
