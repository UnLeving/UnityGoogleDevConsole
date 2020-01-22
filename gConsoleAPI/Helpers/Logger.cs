using System;
using System.IO;

namespace gConsoleAPI.Helpers
{
    public static class Logger
    {
        public static void WrightLog(string logLine)
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Logs.txt"), true))
            {
                outputFile.WriteLine(DateTime.Now + ": " + logLine);
            }
        }
    }
}