//FileFunction.cs
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OfficeOpenXml;
using Reader_Excell.Class;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using Reader_Excel.Class;
using Reader_Excell.OOP;
using Reader_Excel.OOP; // Ensure this namespace is included

namespace Reader_Excell.Utilities
{
    internal class FileFunctions
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // Adjust concurrency if needed
        private static int trueCount = 0; // Track the number of successful records

        // Global fields
        public static List<string> txnLineIDs = new List<string>();
        public static string? txnID = null;
        public static string? newtxnID = null;
        public static int re_id;

        public static async Task ProcessExcelFileAsync(string filePath, string logDirectoryPath, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(1000, cancellationToken); // Small delay to ensure file is fully written, respecting cancellation

                // Initialize the logger with the log folder path
                AppLogger.InitializeLogger(logDirectoryPath);

                // Log the beginning of processing
                AppLogger.LogInfo($"Processing file: {filePath}");

                // Clear previous global data
                txnID = null;
                newtxnID = null;
                txnLineIDs.Clear();

                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".xlsx")
                {
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        await ProcessXlsxWorksheetAsync(worksheet);
                    }
                }
                else if (extension == ".xls")
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var workbook = new HSSFWorkbook(fs);
                        var sheet = workbook.GetSheetAt(0);
                        await ProcessXlsWorksheetAsync(sheet);
                    }
                }

                // Log the end of processing
                AppLogger.LogEnd();

                // Delete the file after successful processing
                BasicUtilities.DeleteFile(filePath);
            }
            catch (OperationCanceledException)
            {
                // Handle the operation being canceled
                AppLogger.LogError($"Processing of file {filePath} was canceled.");
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred while processing the file {filePath}: {ex.Message}";
                Console.WriteLine(errorMessage);
                AppLogger.LogError(errorMessage);  // Corrected from LogInfo to LogError

                // Perform cleanup in case of failure
                
            }
        }

       

        private static async Task ProcessXlsxWorksheetAsync(ExcelWorksheet worksheet)
        {
            txnID = null;
            txnLineIDs.Clear(); // Clear previous IDs

            if (worksheet.Dimension == null) return;

            int totalRows = worksheet.Dimension.End.Row;
            int totalColumns = worksheet.Dimension.End.Column;

            int descriptionColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "DESCRIPTION");
            int qtyColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "QTY");
            int totalAmountColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "TOTAL AMOUNT");
            int refNoColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "CLASS");

            if (descriptionColumnIndex == -1 || qtyColumnIndex == -1 || totalAmountColumnIndex == -1 || refNoColumnIndex == -1)
            {
                Console.WriteLine("One or more required headers not found.");
                return;
            }

            var tasks = new ConcurrentBag<Task>();


            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("yyyyMMdd");

            var maxIncNumReference = await IncrementTable_Class.FetchLastReferenceAsync();
            int inc_num = (maxIncNumReference?.Inc_num ?? 0) + 1;

            if (maxIncNumReference != null)
            {
                Console.WriteLine($"Max Inc_num Reference - Id: {maxIncNumReference.Id}, DateTime: {maxIncNumReference.DateTime}, Inc_num: {maxIncNumReference.Inc_num}");
            }
            else
            {
                Console.WriteLine("No records found.");
            }

            Console.WriteLine(string.Concat($"{formattedDate}-{inc_num}"));


            var reference = new Referencetable
            {
                Id = 1, // Or the appropriate value, e.g., your PK logic
                DateTime = dateTime, // Format as needed
                Inc_num = inc_num // This should be your calculated inc_num value
            };

            bool result = await IncrementTable_Class.InsertReferenceTableAsync(reference);

            if (result)
            {
                Console.WriteLine("Insert successful.");

            }
            else
            {
                Console.WriteLine("Insert failed.");
            }

            string txnIDGenerated = BasicUtilities.GenerateTxnID();
            string class_header = "";
            decimal totalAmount = 0;


            for (int row = 1; row < totalRows; row++)
            {
                var description = worksheet.Cells[row, descriptionColumnIndex]?.Text.Trim();
                var qtyText = worksheet.Cells[row, qtyColumnIndex]?.Text.Trim();
                var totalAmountText = worksheet.Cells[row, totalAmountColumnIndex]?.Text.Trim();
                var className = worksheet.Cells[row, refNoColumnIndex]?.Text.Trim();

                AppLogger.LogInfo($"Processing row with description: {description}");

                if (!string.IsNullOrEmpty(description) && int.TryParse(qtyText, out int quantity) && decimal.TryParse(totalAmountText, out decimal amount))
                {

                    bool itemExists = await ItemInventory_Class.DoesItemInventoryExistAsync(description);
                    if (itemExists)
                    {
                        var item = await ItemInventory_Class.FetchItemInventoryAsync(description);

                        var detail = new SalesReceiptLineDetail
                        {
                            TxnLineID = Guid.NewGuid().ToString(), // Or however you want to generate TxnLineID
                            ItemRefListID = item.ListID, // Replace this with actual value if needed
                            ItemRefFullName = item.FullName, // Or whatever logic you need
                            Description = description,
                            Quantity = quantity,
                            Rate = amount / quantity, // Assuming you want to calculate rate as totalAmount / quantity
                            Amount = amount,
                            IdKey = txnIDGenerated // Set this to the relevant ID
                        };

                        totalAmount += amount;

                        class_header = className;
                        tasks.Add(SalesReceiptLineDetail_Class.InsertSalesReceiptLineDetailAsync(detail));

                    }

                }
            }

            var res = await ClassName_Class.FetchClassAsync(class_header);

            var receipt = new SalesReceipt
            {
                TxnID = txnIDGenerated, // Replace with your generated TxnID
                ClassRef_ListID = res.ListId, // Replace with the actual ClassRef_ListID
                ClassRef_FullName = res.Name, // Replace with the actual ClassRef_FullName
                TxnDate = DateTime.Now, // Current date and time
                RefNumber = string.Concat($"{formattedDate}-{inc_num}"), // Replace with the actual reference number
                Subtotal = totalAmount, // Replace with the actual subtotal
                TotalAmount = totalAmount, // Replace with the actual total amount
                Status = "ADD" // Replace with the actual status
            };

            tasks.Add(SalesReceipt_Class.InsertSalesReceiptAsync(receipt));

            await Task.WhenAll(tasks);

        }

        private static async Task ProcessXlsWorksheetAsync(ISheet sheet)
        {
            txnID = null;
            txnLineIDs.Clear(); // Clear previous IDs

            if (sheet.PhysicalNumberOfRows == 0) return;

            IRow headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                Console.WriteLine("No header found in the .xls file.");
                return;
            }

            int descriptionColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "DESCRIPTION");
            int qtyColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "QTY");
            int priceColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "PRICE");
            int totalAmountColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "TOTAL AMOUNT");
            int classColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "CLASS");

            if (descriptionColumnIndex == -1 || qtyColumnIndex == -1 || priceColumnIndex == -1 || totalAmountColumnIndex == -1 || classColumnIndex == -1)
            {
                Console.WriteLine("One or more required headers not found.");
                return;
            }

            var tasks = new ConcurrentBag<Task>();


            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("yyyyMMdd");

            var maxIncNumReference = await IncrementTable_Class.FetchLastReferenceAsync();
            int inc_num = (maxIncNumReference?.Inc_num ?? 0) + 1;

            if (maxIncNumReference != null)
            {
                Console.WriteLine($"Max Inc_num Reference - Id: {maxIncNumReference.Id}, DateTime: {maxIncNumReference.DateTime}, Inc_num: {maxIncNumReference.Inc_num}");
            }
            else
            {
                Console.WriteLine("No records found.");
            }

            Console.WriteLine(string.Concat($"{formattedDate}-{inc_num}"));


            var reference = new Referencetable
            {
                Id = 1, // Or the appropriate value, e.g., your PK logic
                DateTime = dateTime, // Format as needed
                Inc_num = inc_num // This should be your calculated inc_num value
            };

            await IncrementTable_Class.InsertReferenceTableAsync(reference);


            string txnIDGenerated = BasicUtilities.GenerateTxnID();
            string class_header = "";
            decimal totalAmount = 0;


            for (int row = 1; row < sheet.PhysicalNumberOfRows; row++)
            {
                var description = sheet.GetRow(row)?.GetCell(descriptionColumnIndex)?.ToString().Trim();
                var qtyText = sheet.GetRow(row)?.GetCell(qtyColumnIndex)?.ToString().Trim();
                var totalAmountText = sheet.GetRow(row)?.GetCell(totalAmountColumnIndex)?.ToString().Trim();
                var className = sheet.GetRow(row)?.GetCell(classColumnIndex)?.ToString().Trim();

                AppLogger.LogInfo($"Processing row with description: {description}");

                if (!string.IsNullOrEmpty(description) && int.TryParse(qtyText, out int quantity) && decimal.TryParse(totalAmountText, out decimal amount))
                {

                    bool itemExists = await ItemInventory_Class.DoesItemInventoryExistAsync(description);
                    if (itemExists)
                    {
                        var item = await ItemInventory_Class.FetchItemInventoryAsync(description);

                        var detail = new SalesReceiptLineDetail
                        {
                            TxnLineID = Guid.NewGuid().ToString(), // Or however you want to generate TxnLineID
                            ItemRefListID = item.ListID, // Replace this with actual value if needed
                            ItemRefFullName = item.FullName, // Or whatever logic you need
                            Description = description,
                            Quantity = quantity,
                            Rate = amount / quantity, // Assuming you want to calculate rate as totalAmount / quantity
                            Amount = amount,
                            IdKey = txnIDGenerated // Set this to the relevant ID
                        };

                        totalAmount += amount;

                        class_header = className;
                        tasks.Add(SalesReceiptLineDetail_Class.InsertSalesReceiptLineDetailAsync(detail));

                    }
                
                }
            }

            var res = await ClassName_Class.FetchClassAsync(class_header);

            var receipt = new SalesReceipt
            {
                TxnID = txnIDGenerated, // Replace with your generated TxnID
                ClassRef_ListID = res.ListId, // Replace with the actual ClassRef_ListID
                ClassRef_FullName = res.Name, // Replace with the actual ClassRef_FullName
                TxnDate = DateTime.Now, // Current date and time
                RefNumber = string.Concat($"{formattedDate}-{inc_num}"), // Replace with the actual reference number
                Subtotal = totalAmount, // Replace with the actual subtotal
                TotalAmount = totalAmount, // Replace with the actual total amount
                Status = "ADD" // Replace with the actual status
            };

            tasks.Add(SalesReceipt_Class.InsertSalesReceiptAsync(receipt));

            await Task.WhenAll(tasks);

        }
    }
}
