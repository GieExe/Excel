using OfficeOpenXml;
using Reader_Excell.Utilities;
using Reader_Excell.Properties;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Reader_Excel
{
    internal class Program
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();


        static async Task Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Handle application exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => _cts.Cancel();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // Prevent immediate exit
                _cts.Cancel();
            };

            // Validate and set directory path
            string userDirectoryPath = await ValidateAndSetDirectoryPath();

            

            // Main loop for processing Excel files or updating directory path
            await MainLoop(userDirectoryPath);
        }

        private static async Task<string> ValidateAndSetDirectoryPath()
        {
            string userDirectoryPath = Settings.Default.Path;

            // Check if the directory path is valid
            if (string.IsNullOrEmpty(userDirectoryPath) || !Directory.Exists(userDirectoryPath))
            {
                Console.WriteLine("The directory does not exist or is not set.");
                userDirectoryPath = await GetValidDirectoryPathFromUser();
            }

            Settings.Default.Path = userDirectoryPath;
            Settings.Default.Save();
            Console.WriteLine($"Directory path set to: {userDirectoryPath}");

            return userDirectoryPath;
        }

        private static async Task<string> GetValidDirectoryPathFromUser()
        {
            while (true)
            {
                Console.WriteLine("Please enter a valid directory path to monitor for Excel files:");
                Console.WriteLine("Example: C:\\Users\\<YourUsername>\\Documents\\ExcelFiles");
                string inputPath = Console.ReadLine();

                if (Directory.Exists(inputPath))
                {
                    return inputPath; // Return valid path
                }
                else
                {
                    Console.WriteLine("Invalid directory. Please try again.");
                }
            }
        }

       

        private static async Task MainLoop(string folderPath)
        {
            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Start processing Excel files");
                Console.WriteLine("2. Update directory path");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await StartMonitoringExcelFiles(folderPath);
                        break;
                    case "2":
                        folderPath = await GetValidDirectoryPathFromUser();
                        Settings.Default.Path = folderPath;
                        Settings.Default.Save();
                        Console.WriteLine($"Directory path updated to: {folderPath}");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select either 1 or 2.");
                        break;
                }
            }
        }

        private static async Task StartMonitoringExcelFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("The directory path is not set or does not exist. Please update the directory path first.");
                return; // Exit if the directory is invalid
            }

            Console.WriteLine($"Monitoring folder: {folderPath} for Excel files (.xlsx, .xls)...");

            using var watcher = new FileSystemWatcher
            {
                Path = folderPath,
                Filter = "*.*", // Watch all files
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            // Event handlers for file changes
            watcher.Created += async (sender, e) =>
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    await BasicUtilities.OnFileCreatedAsync(e.FullPath, _cts.Token);
                }
            };

            watcher.Renamed += async (sender, e) =>
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    await BasicUtilities.OnFileRenamedAsync(e.FullPath, _cts.Token);
                }
            };

            watcher.Error += BasicUtilities.OnError;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Type 'exit' to stop monitoring and return to the menu.");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    string input = Console.ReadLine();
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        _cts.Cancel(); // Stop monitoring
                        Console.WriteLine("Monitoring stopped.");
                        break; // Exit monitoring loop
                    }
                }

                await Task.Delay(500); // Polling delay to reduce CPU usage
            }
        }
    }



}
