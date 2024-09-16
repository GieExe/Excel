//BasicUtilities.cs
using NPOI.SS.UserModel;
using OfficeOpenXml;
using Reader_Excell.Properties;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Reader_Excell.Utilities
{
    internal class BasicUtilities
    {
        public static async Task OnFileCreatedAsync(string filePath, CancellationToken cancellationToken)
        {
            if (IsExcelFile(filePath))
            {
                Console.WriteLine($"Excel file detected: {Path.GetFileName(filePath)}");

                // Get the log directory path from Settings
                string logDirectoryPath = Settings.Default.Path;

                // Pass the logDirectoryPath along with filePath and cancellationToken
                await FileFunctions.ProcessExcelFileAsync(filePath, logDirectoryPath, cancellationToken);
            }
        }

        public static async Task OnFileRenamedAsync(string filePath, CancellationToken cancellationToken)
        {
            if (IsExcelFile(filePath))
            {
                Console.WriteLine($"Excel file renamed or moved: {Path.GetFileName(filePath)}");

                // Get the log directory path from Settings
                string logDirectoryPath = Settings.Default.Path;

                // Pass the logDirectoryPath along with filePath and cancellationToken
                await FileFunctions.ProcessExcelFileAsync(filePath, logDirectoryPath, cancellationToken);
            }
        }

        public static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"Error: {e.GetException().Message}");
        }

        public static int FindColumnIndex(IRow headerRow, string columnName)
        {
            for (int col = 0; col < headerRow.LastCellNum; col++)
            {
                if (headerRow.GetCell(col)?.ToString().Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return col;
                }
            }
            return -1;
        }

        public static int FindColumnIndex(ExcelWorksheet worksheet, int totalColumns, string columnName)
        {
            for (int col = 1; col <= totalColumns; col++)
            {
                if (worksheet.Cells[1, col].Text.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return col;
                }
            }
            return -1;
        }

        public static bool IsExcelFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".xlsx" || extension == ".xls";
        }

        public static string GenerateTxnID()
        {
            var prefix = new Random().Next(100, 999).ToString("D3"); // Random 3-digit prefix
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(); // Unix timestamp
            return $"{prefix}-{timestamp}";
        }

        public static string GenerateTxnLineID()
        {
            var prefix = new Random().Next(100, 999).ToString("D3"); // Random 3-digit prefix
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(); // Unix timestamp
            return $"{prefix}-{timestamp}";
        }

        public static void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Console.WriteLine($"File '{Path.GetFileName(filePath)}' has been deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"File '{Path.GetFileName(filePath)}' does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file '{Path.GetFileName(filePath)}': {ex.Message}");
            }
        }

    }
}
