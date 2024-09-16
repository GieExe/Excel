//Program.cs
using OfficeOpenXml;
using Reader_Excell.Utilities;
using Reader_Excell.Properties;


namespace Reader_Excel
{
    internal class Program
    {
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Handle application exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => cts.Cancel();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true; // Prevent immediate exit
                cts.Cancel();
            };

            String userDirectoryPath = Settings.Default.Path;


            // Validate and set the directory path
            if (Directory.Exists(userDirectoryPath))
            {
                Settings.Default.Path = userDirectoryPath;
                Settings.Default.Save();
                Console.WriteLine($"Directory path set to: {userDirectoryPath}");
            }
            else
            {
                Console.WriteLine("The directory does not exist. Please enter a valid directory path.");
                Console.WriteLine("Example of valid directory paths:");
                Console.WriteLine("C:\\Users\\<YourUsername>\\Documents\\ExcelFiles");
                Console.WriteLine("D:\\Data\\ExcelProcessing");
                Console.WriteLine("Please enter the directory path to monitor for Excel files:");
                userDirectoryPath = Console.ReadLine();
            }

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Start processing Excel files");
                Console.WriteLine("2. Update directory path");
                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    // Retrieve the directory path from Settings.settings
                    string folderPath = Settings.Default.Path;

                    if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                    {
                        Console.WriteLine("The directory path is not set or does not exist. Please update the directory path first.");
                        Console.WriteLine("Example of valid directory paths:");
                        Console.WriteLine("C:\\Users\\<YourUsername>\\Documents\\ExcelFiles");
                        Console.WriteLine("D:\\Data\\ExcelProcessing");
                        continue;
                    }

                    Console.WriteLine($"Monitoring folder: {folderPath} for Excel files (.xlsx, .xls)...");

                    FileSystemWatcher watcher = new FileSystemWatcher
                    {
                        Path = folderPath,
                        Filter = "*.*", // Watch all files
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
                    };

                    watcher.Created += async (sender, e) =>
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            await BasicUtilities.OnFileCreatedAsync(e.FullPath, cts.Token);
                        }
                    };
                    watcher.Renamed += async (sender, e) =>
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            await BasicUtilities.OnFileRenamedAsync(e.FullPath, cts.Token);
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
                                cts.Cancel(); // Stop monitoring
                                Console.WriteLine("Monitoring stopped.");
                                break;
                            }
                        }

                        await Task.Delay(500); // Polling delay to reduce CPU usage
                    }

                    watcher.Dispose(); // Clean up FileSystemWatcher
                }
                else if (choice == "2")
                {
                    Console.WriteLine("Enter the new directory path:");
                    string newFolderPath = Console.ReadLine();

                    if (Directory.Exists(newFolderPath))
                    {
                        // Update the settings with the new path
                        Settings.Default.Path = newFolderPath;
                        Settings.Default.Save();

                        Console.WriteLine($"Directory path updated to: {newFolderPath}");
                    }
                    else
                    {
                        Console.WriteLine("The directory does not exist. Please enter a valid directory path.");
                        Console.WriteLine("Example of valid directory paths:");
                        Console.WriteLine("C:\\Users\\<YourUsername>\\Documents\\ExcelFiles");
                        Console.WriteLine("D:\\Data\\ExcelProcessing");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid choice. Please select either 1 or 2.");
                }
            }
        }
    }
}
