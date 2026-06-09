using System;
using System.IO;
using System.Text;

namespace SteamAutoLauncher.Core.Logging
{
    public static class Logger
    {
        private static readonly object LockObject = new();
        private static string LogPath = "logs";

        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success
        }

        static Logger()
        {
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (LockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var levelStr = level switch
                {
                    LogLevel.Info => "[INFO]",
                    LogLevel.Warning => "[WARN]",
                    LogLevel.Error => "[ERROR]",
                    LogLevel.Success => "[OK]",
                    _ => "[LOG]"
                };

                var logMessage = $"{timestamp} {levelStr} {message}";
                Console.WriteLine(logMessage);

                // Write to file
                var logFile = Path.Combine(LogPath, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
                try
                {
                    File.AppendAllText(logFile, logMessage + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Ignore file write errors
                }
            }
        }

        public static void LogInfo(string message) => Log(message, LogLevel.Info);
        public static void LogWarning(string message) => Log(message, LogLevel.Warning);
        public static void LogError(string message) => Log(message, LogLevel.Error);
        public static void LogSuccess(string message) => Log(message, LogLevel.Success);
    }
}