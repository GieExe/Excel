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
            PrependLogToFile(new string('-', 80) + "\n" +
                             $"Begin Log for {logStartTimestamp}\n" +
                             new string('-', 80) + "\n");
        }

        public static void LogInfo(string message)
        {
            PrependLogToFile($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO {message}\n");
        }

        public static void LogError(string message)
        {
            PrependLogToFile($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {message}\n");
        }

        public static void LogEnd()
        {
            PrependLogToFile(new string('-', 80) + "\n" +
                             $"End Log for {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                             new string('-', 80) + "\n");
        }

        private static void PrependLogToFile(string newLogEntry)
        {
            lock (logFilePath) // Synchronize file access
            {
                string tempFilePath = Path.Combine(logDirectory, "temp_" + logFileName);

                try
                {
                    // Write the new log entry to the temporary file
                    File.WriteAllText(tempFilePath, newLogEntry);

                    // Append the contents of the existing log file to the temp file, if it exists
                    if (File.Exists(logFilePath))
                    {
                        using (var originalStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var tempStream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                        {
                            originalStream.CopyTo(tempStream); // Append the original log contents to the temp file
                        }
                    }

                    // Replace the original log file with the temp file
                    File.Delete(logFilePath); // Delete the old log file
                    File.Move(tempFilePath, logFilePath); // Rename temp file to log file
                }
                catch (IOException ex)
                {
                    // Handle file access issues
                    logger.Error("Error while prepending log entry: " + ex.Message);
                }
                finally
                {
                    // Ensure temp file is cleaned up in case of failure
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
        }
    }
}
