//Program.cs
using OfficeOpenXml;
using Reader_Excell.Utilities;

namespace Reader_Excel
{
    internal class Program
    {
        // Semaphore for managing concurrency
        private static SemaphoreSlim semaphore = new SemaphoreSlim(10);

        // Counter to track the number of 'true' results
        private static int trueCount = 0;

        static async Task Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string folderPath = @"C:\Users\Giebert\Documents\";

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("The folder does not exist.");
                return;
            }

            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = folderPath,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            watcher.Created += async (sender, e) => await BasicUtilities.OnFileCreatedAsync(e.FullPath);
            watcher.Renamed += async (sender, e) => await BasicUtilities.OnFileRenamedAsync(e.FullPath);
            watcher.Error += BasicUtilities.OnError;

            watcher.EnableRaisingEvents = true;

            Console.WriteLine($"Monitoring folder: {folderPath} for Excel files (.xlsx, .xls)...");
            await Task.Delay(-1);
        }
    }
}
