//FileFunctions.cs
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using OfficeOpenXml;
using Reader_Excell.Class;
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace Reader_Excell.Utilities
{
    internal class FileFunctions
    {
        public static int trueCount = 0;
        public static SemaphoreSlim semaphore = new SemaphoreSlim(10);

        public static async Task ProcessExcelFileAsync(string filePath)
        {
            try
            {
                await Task.Delay(1000); // Small delay to ensure file is fully written

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

                // After processing the file, print the total count of true results
                Console.WriteLine($"\nTotal 'true' results: {trueCount}");

                BasicUtilities.DeleteFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing the file: {ex.Message}");
            }
        }

        public static async Task ProcessXlsWorksheetAsync(ISheet sheet)
        {
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

        public static async Task ProcessXlsxWorksheetAsync(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension == null) return;

            int totalRows = worksheet.Dimension.End.Row;
            int totalColumns = worksheet.Dimension.End.Column;

            int descriptionColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "DESCRIPTION");
            int qtyColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "QTY");
            int totalAmountColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "TOTAL AMOUNT");
            int refNoColumnIndex = BasicUtilities.FindColumnIndex(worksheet, totalColumns, "REF NO.");

            if (descriptionColumnIndex == -1 || qtyColumnIndex == -1 || totalAmountColumnIndex == -1 || refNoColumnIndex == -1)
            {
                Console.WriteLine("Required headers not found.");
                return;
            }

            var tasks = new ConcurrentBag<Task>();

            for (int row = 2; row <= totalRows; row++)
            {
                var description = worksheet.Cells[row, descriptionColumnIndex]?.Text.Trim();
                var qtyText = worksheet.Cells[row, qtyColumnIndex]?.Text.Trim();
                var totalAmountText = worksheet.Cells[row, totalAmountColumnIndex]?.Text.Trim();
                var refNo = worksheet.Cells[row, refNoColumnIndex]?.Text.Trim();

                if (!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(qtyText) && !string.IsNullOrEmpty(totalAmountText) && !string.IsNullOrEmpty(refNo))
                {
                    tasks.Add(ProcessRowAsync(description, qtyText, totalAmountText, refNo));
                }
            }

            await Task.WhenAll(tasks);
        }


        public static async Task ProcessRowAsync(string description, string? qtyText, string? totalAmountText, string? refNo)
        {
            await semaphore.WaitAsync();
            try
            {
                // Normalize RefNo to handle cases where the value might be inconsistent
                string normalizedRefNo = (refNo ?? string.Empty).Trim().ToUpper();



                var item = await ItemInventory_Class.GetItemDetailsFromDatabaseAsync(description);

                if (item != null)
                {

                    Console.WriteLine($"Original RefNo: '{refNo}'");
                    Console.WriteLine($"Normalized RefNo: '{normalizedRefNo}'");

                    Interlocked.Increment(ref trueCount);
                    Console.WriteLine(
                        $"true\t{item.ListID,-10}\t{item.Name,-30}\t{item.FullName,-50}\t{item.SalesDesc,-70}"
                    );

                    string txnID = BasicUtilities.GenerateTxnID();
                    string txnLineID = BasicUtilities.GenerateTxnLineID();
                    DateTime txnDate = DateTime.Now;

                    if (!int.TryParse(qtyText ?? "0", out int quantity))
                    {
                        Console.WriteLine($"Invalid quantity '{qtyText}' for description '{description}'.");
                        return;
                    }

                    decimal totalAmount;
                    if (!decimal.TryParse(totalAmountText ?? "0", NumberStyles.Number, CultureInfo.InvariantCulture, out totalAmount))
                    {
                        Console.WriteLine($"Invalid total amount '{totalAmountText}' for description '{description}'.");

                        // Attempt to clean the total amount value
                        string cleanedAmount = new string((totalAmountText ?? string.Empty).Where(ch => char.IsDigit(ch) || ch == '.').ToArray());
                        if (decimal.TryParse(cleanedAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out totalAmount))
                        {
                            Console.WriteLine($"Total amount '{totalAmountText}' cleaned to '{totalAmount}' for description '{description}'.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to parse cleaned total amount '{cleanedAmount}' for description '{description}'.");
                            return;
                        }
                    }

                    decimal rate = quantity > 0 ? totalAmount / quantity : 0;
                    rate = Math.Round(rate, 6);

                    using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                    {
                        await connection.OpenAsync();

                        string txnIDForRefNumber = null;

                        // Check if RefNumber already exists
                        string checkRefNumberQuery = "SELECT TxnID FROM salesreceipt WHERE RefNumber = @RefNumber";
                        using (var checkCmd = new MySqlCommand(checkRefNumberQuery, connection))
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

                            // Insert into salesreceipt
                            string insertSalesReceiptQuery = @"
                    INSERT INTO salesreceipt (TxnID, TxnDate, RefNumber, Subtotal, TotalAmount, Status)
                    VALUES (@TxnID, @TxnDate, @RefNumber, @Subtotal, @TotalAmount, @Status)";

                            using (var command = new MySqlCommand(insertSalesReceiptQuery, connection))
                            {
                                command.Parameters.AddWithValue("@TxnID", txnIDForRefNumber);
                                command.Parameters.AddWithValue("@TxnDate", txnDate);
                                command.Parameters.AddWithValue("@RefNumber", normalizedRefNo);
                                command.Parameters.AddWithValue("@Subtotal", totalAmount);
                                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                                command.Parameters.AddWithValue("@Status", "ADD");

                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        // Insert or update salesreceiptlinedetail
                        string checkLineDetailQuery = "SELECT IFNULL(SUM(Amount), 0) FROM salesreceiptlinedetail WHERE IDKEY = @IDKEY";
                        decimal existingTotalAmount = 0;
                        using (var checkLineCmd = new MySqlCommand(checkLineDetailQuery, connection))
                        {
                            checkLineCmd.Parameters.AddWithValue("@IDKEY", txnIDForRefNumber ?? txnID);
                            existingTotalAmount = (await checkLineCmd.ExecuteScalarAsync()) as decimal? ?? 0;
                        }

                        decimal newTotalAmount = existingTotalAmount + totalAmount;

                        // Insert into salesreceiptlinedetail
                        string insertSalesReceiptLineDetailQuery = @"
                INSERT INTO salesreceiptlinedetail (TxnLineID, ItemRef_ListID, ItemRef_FullName, Description, Quantity, Rate, Amount, IDKEY)
                VALUES (@TxnLineID, @ItemRef_ListID, @ItemRef_FullName, @Description, @Quantity, @Rate, @Amount, @IDKEY)";

                        using (var lineDetailCommand = new MySqlCommand(insertSalesReceiptLineDetailQuery, connection))
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
                        }

                        // Update salesreceipt Subtotal and TotalAmount
                        string updateSalesReceiptQuery = @"
                UPDATE salesreceipt
                SET Subtotal = (SELECT IFNULL(SUM(Amount), 0) FROM salesreceiptlinedetail WHERE IDKEY = @IDKEY),
                    TotalAmount = (SELECT IFNULL(SUM(Amount), 0) FROM salesreceiptlinedetail WHERE IDKEY = @IDKEY)
                WHERE TxnID = @TxnID";

                        using (var updateCommand = new MySqlCommand(updateSalesReceiptQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@IDKEY", txnIDForRefNumber ?? txnID);
                            updateCommand.Parameters.AddWithValue("@TxnID", txnIDForRefNumber ?? txnID);

                            await updateCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing row with description '{description}': {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

    }
}
