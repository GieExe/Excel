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
using MySql.Data.MySqlClient; // Ensure this namespace is included

namespace Reader_Excell.Utilities
{
    internal class FileFunctions
    {
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // Adjust concurrency if needed
        private static int trueCount = 0; // Track the number of successful records

        // Global fields
        private static List<string> txnLineIDs = new List<string>();
        private static string? txnID = null;
        private static string? newtxnID = null;

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
                await CleanupFailedTransactionAsync(txnLineIDs, newtxnID);
            }
        }

        private static async Task ProcessRowAsync(string description, string? qtyText, string? totalAmountText, string? refNo)
        {
            await semaphore.WaitAsync();
            MySqlTransaction? transaction = null;

            // Retry logic
            const int maxRetries = 3;
            int retryCount = 0;
            bool success = false;

            while (retryCount < maxRetries && !success)
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        transaction = await connection.BeginTransactionAsync();

                        // Normalize RefNo to handle cases where the value might be inconsistent
                        string normalizedRefNo = (refNo ?? string.Empty).Trim().ToUpper();

                        AppLogger.LogInfo($"Processing row with description: {description}");

                        var item = await ItemInventory_Class.GetItemDetailsFromDatabaseAsync(description);

                        if (item != null)
                        {
                            AppLogger.LogInfo($"Item found for description '{description}': {item.FullName}");

                            Interlocked.Increment(ref trueCount);
                            Console.WriteLine(
                                $"true\t{item.ListID,-10}\t{item.Name,-30}\t{item.FullName,-50}\t{item.SalesDesc,-70}"
                            );

                            string txnIDGenerated = BasicUtilities.GenerateTxnID();
                            string txnLineID = BasicUtilities.GenerateTxnLineID();
                            txnLineIDs.Add(txnLineID); // Track the generated TxnLineID
                            txnID = txnIDGenerated; // Track generated TxnID
                            DateTime txnDate = DateTime.Now;

                            if (!int.TryParse(qtyText ?? "0", out int quantity))
                            {
                                string errorMessage = $"Invalid quantity '{qtyText}' for description '{description}'.";
                                Console.WriteLine(errorMessage);
                                AppLogger.LogError(errorMessage);
                                await transaction.RollbackAsync(); // Rollback transaction
                                return;
                            }

                            decimal totalAmount;
                            if (!decimal.TryParse(totalAmountText ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture, out totalAmount))
                            {
                                string errorMessage = $"Invalid total amount '{totalAmountText}' for description '{description}'.";

                                // Attempt to clean the total amount value
                                string cleanedAmount = new string((totalAmountText ?? string.Empty).Where(ch => char.IsDigit(ch) || ch == '.').ToArray());
                                if (decimal.TryParse(cleanedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out totalAmount))
                                {
                                    Console.WriteLine($"Total amount '{totalAmountText}' cleaned to '{totalAmount}' for description '{description}'.");
                                }
                                else
                                {
                                    errorMessage = $"Failed to parse cleaned total amount '{cleanedAmount}' for description '{description}'.";
                                    Console.WriteLine(errorMessage);
                                    AppLogger.LogError(errorMessage);
                                    await transaction.RollbackAsync(); // Rollback transaction
                                    return;
                                }
                            }

                            decimal rate = quantity > 0 ? totalAmount / quantity : 0;
                            rate = Math.Round(rate, 6);

                            string txnIDForRefNumber = null;

                            // Check if RefNumber already exists
                            string checkRefNumberQuery = "SELECT TxnID FROM salesreceipt WHERE RefNumber = @RefNumber";
                            using (var checkCmd = new MySqlCommand(checkRefNumberQuery, connection, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@RefNumber", normalizedRefNo);
                                using (var reader = await checkCmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        txnIDForRefNumber = reader.GetString(0);
                                    }
                                }
                            }

                            if (txnIDForRefNumber == null)
                            {
                                txnIDForRefNumber = txnID;
                                txnID = txnIDForRefNumber; // Track inserted TxnID

                                // Insert into salesreceipt
                                AppLogger.LogInfo($"Inserting into salesreceipt: TxnID={txnIDForRefNumber}, TxnDate={txnDate}, RefNumber={normalizedRefNo}, Subtotal={totalAmount}, TotalAmount={totalAmount}, Status=ADD");

                                string insertSalesReceiptQuery = @"
INSERT INTO salesreceipt (TxnID, TxnDate, RefNumber, Subtotal, TotalAmount, Status)
VALUES (@TxnID, @TxnDate, @RefNumber, @Subtotal, @TotalAmount, @Status)";

                                using (var command = new MySqlCommand(insertSalesReceiptQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@TxnID", txnIDForRefNumber);
                                    command.Parameters.AddWithValue("@TxnDate", txnDate);
                                    command.Parameters.AddWithValue("@RefNumber", normalizedRefNo);
                                    command.Parameters.AddWithValue("@Subtotal", totalAmount);
                                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                                    command.Parameters.AddWithValue("@Status", "ADD");   
                                    newtxnID = txnIDForRefNumber;
                                    await command.ExecuteNonQueryAsync();
                                    AppLogger.LogInfo($"Inserted into salesreceipt with TxnID={txnIDForRefNumber}");
                                }

                                
                            }

                            // Insert into salesreceiptlinedetail
                            AppLogger.LogInfo($"Inserting into salesreceiptlinedetail: TxnLineID={txnLineID}, ItemRef_ListID={item.AssetAccountRef_ListID}, ItemRef_FullName={item.FullName}, Description={description}, Quantity={quantity}, Rate={rate}, Amount={totalAmount}, IDKEY={txnIDForRefNumber ?? txnID}");

                            string insertSalesReceiptLineDetailQuery = @"
INSERT INTO salesreceiptlinedetail (TxnLineID, ItemRef_ListID, ItemRef_FullName, Description, Quantity, Rate, Amount, IDKEY)
VALUES (@TxnLineID, @ItemRef_ListID, @ItemRef_FullName, @Description, @Quantity, @Rate, @Amount, @IDKEY)";

                            using (var lineDetailCommand = new MySqlCommand(insertSalesReceiptLineDetailQuery, connection, transaction))
                            {
                                lineDetailCommand.Parameters.AddWithValue("@TxnLineID", txnLineID);
                                lineDetailCommand.Parameters.AddWithValue("@ItemRef_ListID", item.AssetAccountRef_ListID);
                                lineDetailCommand.Parameters.AddWithValue("@ItemRef_FullName", item.FullName);
                                lineDetailCommand.Parameters.AddWithValue("@Description", description);
                                lineDetailCommand.Parameters.AddWithValue("@Quantity", quantity);
                                lineDetailCommand.Parameters.AddWithValue("@Rate", rate);
                                lineDetailCommand.Parameters.AddWithValue("@Amount", totalAmount);
                                lineDetailCommand.Parameters.AddWithValue("@IDKEY", txnIDForRefNumber ?? txnID);

                                await lineDetailCommand.ExecuteNonQueryAsync();
                                AppLogger.LogInfo($"Inserted into salesreceiptlinedetail with TxnLineID={txnLineID}");
                            }

                            // Update salesreceipt Subtotal and TotalAmount
                            AppLogger.LogInfo($"Updating salesreceipt Subtotal and TotalAmount for TxnID={txnIDForRefNumber ?? txnID}");

                            string updateSalesReceiptQuery = @"
UPDATE salesreceipt
SET Subtotal = (SELECT IFNULL(SUM(Amount), 0) FROM salesreceiptlinedetail WHERE IDKEY = @IDKEY),
    TotalAmount = (SELECT IFNULL(SUM(Amount), 0) FROM salesreceiptlinedetail WHERE IDKEY = @IDKEY)
WHERE TxnID = @TxnID";

                            using (var updateCommand = new MySqlCommand(updateSalesReceiptQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@IDKEY", txnIDForRefNumber ?? txnID);
                                updateCommand.Parameters.AddWithValue("@TxnID", txnIDForRefNumber ?? txnID);

                                await updateCommand.ExecuteNonQueryAsync();
                                AppLogger.LogInfo($"Updated salesreceipt Subtotal and TotalAmount for TxnID={txnIDForRefNumber ?? txnID}");
                            }
                        }

                        await transaction.CommitAsync(); // Commit transaction if all operations succeed
                        success = true; // Exit retry loop if successful
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error processing row with description '{description}': {ex.Message}";
                    Console.WriteLine(errorMessage);
                    AppLogger.LogError(errorMessage);

                    // Rollback transaction if not already committed
                    if (transaction != null)
                    {
                        try
                        {
                            await transaction.RollbackAsync();
                        }
                        catch (Exception rollbackEx)
                        {
                            AppLogger.LogError($"Error rolling back transaction: {rollbackEx.Message}");
                        }
                    }

                    // Perform cleanup
                    await CleanupFailedTransactionAsync(txnLineIDs, newtxnID);

                    // Exit retry loop
                    success = true;
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private static async Task CleanupFailedTransactionAsync(List<string> txnLineIDs, string? txnID)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // Cleanup salesreceiptlinedetail
                    foreach (var txnLineID in txnLineIDs)
                    {
                        string deleteLineDetailQuery = "DELETE FROM salesreceiptlinedetail WHERE TxnLineID = @TxnLineID";
                        using (var deleteLineDetailCmd = new MySqlCommand(deleteLineDetailQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in SaleRceiptLine!: {txnLineID}");
                            deleteLineDetailCmd.Parameters.AddWithValue("@TxnLineID", txnLineID);
                            await deleteLineDetailCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Cleanup salesreceipt if txnID is not null
                    if (txnID != null)
                    {
                        string deleteReceiptQuery = "DELETE FROM salesreceipt WHERE TxnID = @TxnID";
                        using (var deleteReceiptCmd = new MySqlCommand(deleteReceiptQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in SaleReceipt!: {txnID}");
                            deleteReceiptCmd.Parameters.AddWithValue("@TxnID", txnID);
                            await deleteReceiptCmd.ExecuteNonQueryAsync();
                        }
                    }

                    AppLogger.LogInfo($"Reverted!");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error cleaning up failed transaction: {ex.Message}";
                Console.WriteLine(errorMessage);
                AppLogger.LogError(errorMessage);
            }

            Console.WriteLine(txnID);
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
            int refNoColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "REF NO.");

            if (descriptionColumnIndex == -1 || qtyColumnIndex == -1 || totalAmountColumnIndex == -1 || refNoColumnIndex == -1)
            {
                Console.WriteLine("One or more required headers not found.");
                return;
            }

            var tasks = new ConcurrentBag<Task>();

            for (int row = 2; row <= totalRows; row++)
            {
                var description = worksheet.Cells[row, descriptionColumnIndex]?.Text.Trim();
                var qtyText = worksheet.Cells[row, qtyColumnIndex]?.Text.Trim();
                var totalAmountText = worksheet.Cells[row, totalAmountColumnIndex]?.Text.Trim();
                var refNo = worksheet.Cells[row, refNoColumnIndex]?.Text.Trim();

                AppLogger.LogInfo($"Row {row}: Description: {description}, Qty: {qtyText}, Total Amount: {totalAmountText}, Ref No: {refNo}");

                if (!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(qtyText) && !string.IsNullOrEmpty(totalAmountText) && !string.IsNullOrEmpty(refNo))
                {
                    tasks.Add(ProcessRowAsync(description, qtyText, totalAmountText, refNo));
                }
            }

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
            int refNoColumnIndex = BasicUtilities.FindColumnIndex(headerRow, "REF NO.");

            if (descriptionColumnIndex == -1 || qtyColumnIndex == -1 || priceColumnIndex == -1 || totalAmountColumnIndex == -1 || refNoColumnIndex == -1)
            {
                Console.WriteLine("One or more required headers not found.");
                return;
            }

            var tasks = new ConcurrentBag<Task>();

            for (int row = 1; row < sheet.PhysicalNumberOfRows; row++)
            {
                var description = sheet.GetRow(row)?.GetCell(descriptionColumnIndex)?.ToString().Trim();
                var qty = sheet.GetRow(row)?.GetCell(qtyColumnIndex)?.ToString().Trim();
                var totalAmount = sheet.GetRow(row)?.GetCell(totalAmountColumnIndex)?.ToString().Trim();
                var refNo = sheet.GetRow(row)?.GetCell(refNoColumnIndex)?.ToString().Trim();

                if (!string.IsNullOrEmpty(description))
                {
                    tasks.Add(ProcessRowAsync(description, qty, totalAmount, refNo));
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}
