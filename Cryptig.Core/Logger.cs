using System;
using System.IO;

namespace Cryptig.Core
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cryptig", "logs"
        );

        private static readonly string LogFile = Path.Combine(LogDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
            }
            catch { }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warn(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
                File.AppendAllLines(LogFile, new[] { line });
            }
            catch
            {
            }
        }
    }
}
