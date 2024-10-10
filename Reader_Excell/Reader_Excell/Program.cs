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
        private static bool _isMonitoringActive = false; // Flag to check if monitoring is active

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

            // Check and verify license
            bool isValidLicense = await CheckAndVerifyLicenseAsync();
            if (!isValidLicense)
            {
                Console.WriteLine("Invalid license. Exiting application.");
                return; // Exit if the license is invalid
            }

            // Validate and set directory path
            string userDirectoryPath = await ValidateAndSetDirectoryPath();

            // Main loop for processing Excel files or updating directory path
            await MainLoop(userDirectoryPath);
        }

        // Method to check and verify the license
        private static async Task<bool> CheckAndVerifyLicenseAsync()
        {
            // Check if the license key is already stored in settings
            string storedLicenseKey = Settings.Default.LicenseKey;

            if (!string.IsNullOrEmpty(storedLicenseKey))
            {
                Console.WriteLine("License key found in settings.");

                // Optional: you can verify the stored license with the server if needed
                bool isValid = await VerifyLicenseAsync(storedLicenseKey);
                if (isValid)
                {
                    Console.WriteLine("Stored license is valid. Proceeding...");
                    return true;
                }
                else
                {
                    Console.WriteLine("Stored license is invalid. Re-verifying...");
                }
            }

            // If no valid stored license, prompt the user for a new license key
            Console.WriteLine("Enter your license key:");
            string licenseKey = Console.ReadLine();

            // Verify the entered license key
            bool isLicenseValid = await VerifyLicenseAsync(licenseKey);
            if (isLicenseValid)
            {
                // Store the valid license key in settings
                Settings.Default.LicenseKey = licenseKey;
                Settings.Default.Save();
                Console.WriteLine("License key verified and saved.");
            }

            return isLicenseValid;
        }

        // License verification method
        private static async Task<bool> VerifyLicenseAsync(string licenseKey)
        {
            // URL of your Render-deployed API
            string apiUrl = "https://license-verification.onrender.com/verify-license";

            using (HttpClient client = new HttpClient())
            {
                var requestBody = new { licenseKey = licenseKey };
                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                try
                {
                    // Sending POST request to the Render API
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                        bool isValid = jsonResponse.GetProperty("valid").GetBoolean();

                        return isValid; // Return true if license is valid
                    }
                    else
                    {
                        Console.WriteLine($"License verification failed. Status code: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }
            }

            return false; // Return false if any error occurs
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

        public static async Task MainLoop(string folderPath)
        {
            while (true)
            {
                Console.WriteLine("<---------------------------------------------->");
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Start processing Excel files");
                Console.WriteLine("2. Update directory path");
                Console.Write("Select: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        if (!_isMonitoringActive) // Check if monitoring is already active
                        {
                            _isMonitoringActive = true; // Set the flag to true
                            await StartMonitoringExcelFiles(folderPath);
                            _isMonitoringActive = false; // Reset the flag when monitoring stops
                        }
                        else
                        {
                            Console.WriteLine("Monitoring is already active. Please stop monitoring before starting again.");
                        }
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
            Console.WriteLine("<---------------------------------------------->");
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

            // Reset the flag here to ensure it reflects that monitoring is no longer active
            _isMonitoringActive = false;
            watcher.EnableRaisingEvents = false; // Stop the watcher from monitoring
        }

    }
}
