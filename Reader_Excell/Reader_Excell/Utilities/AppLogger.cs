//AppLogger.cs
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace Reader_Excell.Utilities
{
    public static class AppLogger
    {
        private static string logDirectory;
        private static readonly string logFileName = "LOG.txt";
        private static string logFilePath;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static string logStartTimestamp;

        public static void InitializeLogger(string folderPath) // Accept folderPath
        {
            logDirectory = folderPath;

            // Ensure the directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Configure NLog programmatically
            var config = new LoggingConfiguration();

            var fileTarget = new FileTarget("logfile")
            {
                FileName = Path.Combine(logDirectory, logFileName),
                Layout = "${longdate} ${level:uppercase=true} ${message}",
                ArchiveAboveSize = 10_000_000,
                MaxArchiveFiles = 10,
                KeepFileOpen = false,
                OpenFileCacheSize = 1
            };

            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            LogManager.Configuration = config;

            // Set log file path
            logFilePath = Path.Combine(logDirectory, logFileName);
            logStartTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            WriteLogHeader();
        }

        private static void WriteLogHeader()
        {
            using (var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(new string('-', 80));
                writer.WriteLine($"Begin Log for {logStartTimestamp}");
                writer.WriteLine(new string('-', 80));
            }
        }

        public static void LogInfo(string message)
        {
            logger.Info(message);
        }

        public static void LogError(string message)
        {
            logger.Error(message);
        }

        public static void LogEnd()
        {
            using (var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(new string('-', 80));
                writer.WriteLine($"End Log for {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine(new string('-', 80));
            }
        }
    }
}
