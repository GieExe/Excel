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

            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("yyyyMMdd");

            var maxIncNumReference = await IncrementTable_Class.FetchLastReferenceAsync();
            int inc_num = (maxIncNumReference?.Inc_num ?? 0) + 1;

            Console.WriteLine(string.Concat($"{formattedDate}-{inc_num}"));

            var reference = new Referencetable
            {
                Id = 1, // Or the appropriate value, e.g., your PK logic
                DateTime = dateTime, // Format as needed
                Inc_num = inc_num // This should be your calculated inc_num value
            };

            await IncrementTable_Class.InsertReferenceTableAsync(reference);


            



            string txnIDGenerated = BasicUtilities.GenerateTxnID();
            string txnIDGeneratedforItem = BasicUtilities.GenerateTxnID(); // Ensure it's unique per transaction
            string txnIDGeneratedforEachItem = "";
            string class_header = "";
            decimal totalAmount = 0;

            // List to store inventory adjustments
            List<OOP.inventoryadjustmentlinedetail> adjustmentDetails = new List<OOP.inventoryadjustmentlinedetail>();

            for (int row = 1; row < sheet.PhysicalNumberOfRows; row++)
            {
                var rowObj = sheet.GetRow(row);
                if (rowObj == null)
                {
                    AppLogger.LogError($"Row {row} is null.");
                    continue; // Skip this row if it's null
                }

                var descriptionCell = rowObj.GetCell(descriptionColumnIndex);
                var description = descriptionCell?.ToString().Trim();

                var qtyCell = rowObj.GetCell(qtyColumnIndex);
                var qtyText = qtyCell?.ToString().Trim();

                var totalAmountCell = rowObj.GetCell(totalAmountColumnIndex);
                var totalAmountText = totalAmountCell?.ToString().Trim();

                var classNameCell = rowObj.GetCell(classColumnIndex);
                var className = classNameCell?.ToString().Trim();

                AppLogger.LogInfo($"Processing row {row} with description: {description}");

                if (string.IsNullOrEmpty(description) || !int.TryParse(qtyText, out int quantity) || !decimal.TryParse(totalAmountText, out decimal amount))
                {
                    AppLogger.LogError($"Invalid data in row {row}: Description: {description}, Qty: {qtyText}, Total Amount: {totalAmountText}");
                    continue; // Skip to the next row
                }

                bool itemExists = await ItemInventory_Class.DoesItemInventoryExistAsync(description);
                if (!itemExists)
                {
                    AppLogger.LogError($"Item does not exist in inventory for description: {description}");
                    continue;
                }

                var item = await ItemInventory_Class.FetchItemInventoryAsync(description);
                if (item == null)
                {
                    AppLogger.LogError($"Item not found in inventory for description: {description}");
                    continue;
                }

                var detail = new SalesReceiptLineDetail
                {
                    TxnLineID = Guid.NewGuid().ToString(), // Generate unique TxnLineID
                    ItemRefListID = item.ListID, // Actual value needed
                    ItemRefFullName = item.FullName, // Full name of item
                    Description = description,
                    Quantity = quantity,
                    Rate = amount / quantity, // Calculate rate as totalAmount / quantity
                    Amount = amount,
                    IdKey = txnIDGenerated // Set to the relevant ID
                };

                totalAmount += amount;
                class_header = className;

                // Insert SalesReceiptLineDetail and check if successful
                bool insertResult = await SalesReceiptLineDetail_Class.InsertSalesReceiptLineDetailAsync(detail);

                if (insertResult)
                {
                    string item_id = detail.ItemRefListID; // Parent item ID
                    int item_qty = detail.Quantity; // Parent item quantity
                    txnIDGeneratedforEachItem = BasicUtilities.GenerateTxnID();

                    Ingredients_Class ingredientClass = new Ingredients_Class();
                    List<ingredients_table> ingredients = ingredientClass.GetIngredientsByItemInventoryId(item_id);

                    // Process each ingredient
                    if (ingredients.Count > 0)
                    {
                        foreach (var ingredient in ingredients)
                        {
                            int multipliedQty = ingredient.Qty * item_qty * -1; // Multiply ingredient qty by item qty

                            // Create a new adjustment entry for the ingredient
                            var adjustment = new OOP.inventoryadjustmentlinedetail
                            {
                                TxTLineID1 = txnIDGeneratedforEachItem, // Generate a new TxTLineID
                                ItemRef_ListID1 = ingredient.Ingredient_id, // Ingredient ID
                                ItemRef_FullName1 = ingredient.FullName, // Ingredient full name
                                QuantityDifference1 = multipliedQty, // Multiplied quantity
                                IDKEY1 = txnIDGeneratedforEachItem // TxnID for this adjustment
                            };

                            adjustmentDetails.Add(adjustment); // Add adjustment to the list
                        }

                        // **New code to insert a separate adjustment for the item only if ingredients exist**
                        var itemAdjustment = new OOP.inventoryadjustmentlinedetail
                        {
                            TxTLineID1 = BasicUtilities.GenerateTxnID(), // Generate a new TxTLineID for the item adjustment
                            ItemRef_ListID1 = item_id, // Use the item ID
                            ItemRef_FullName1 = item.FullName, // Use the full name of the item
                            QuantityDifference1 = item_qty, // Use the item quantity
                            IDKEY1 = txnIDGeneratedforEachItem // TxnID for this adjustment
                        };

                        adjustmentDetails.Add(itemAdjustment); // Add the item adjustment to the list

                        var maxIncNumIngredientReference = await IngredientsTableRef.FetchLastReferenceIngredientsAsync();
                        int Ingreadient_num = (maxIncNumIngredientReference?.INC_NUM ?? 0) + 1;

                        if (maxIncNumIngredientReference != null)
                        {
                            Console.WriteLine($"Max Inc_num Reference - Id: {maxIncNumIngredientReference.ID}, DateTime: {maxIncNumIngredientReference.DATE}, Inc_num: {maxIncNumIngredientReference.INC_NUM}");
                        }
                        else
                        {
                            Console.WriteLine("No records found.");
                        }

                        Console.WriteLine(string.Concat($"{formattedDate}-{inc_num}"));


                        var ING_reference = new ingredientsref
                        {
                            ID = 1, // Or the appropriate value, e.g., your PK logic
                            DATE = dateTime, // Format as needed
                            INC_NUM = Ingreadient_num // This should be your calculated inc_num value
                        };

                        await IngredientsTableRef.InsertIngredientRefTable(ING_reference);

                        var ress = await ClassName_Class.FetchClassAsync(class_header);

                        if (ress == null)
                        {
                            AppLogger.LogError($"Class not found for class header: {class_header}");
                            return; // Stop processing if class is not found
                        }

                        var adjustments = new inventoryadjustment
                        {
                            TxnLineID1 = txnIDGeneratedforEachItem,
                            TimeCreated1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ClassRef_ListID1 = ress.ListId,
                            ClassRef_FullName1 = ress.Name,
                            RefNumber1 = string.Concat($"{formattedDate}-{Ingreadient_num}")
                        };

                        var adjustmentClass = new inventoryadjustment_class();
                        await adjustmentClass.InsertInventoryAdjustmentAsync(adjustments);

                    }
                    else
                    {
                        AppLogger.LogError($"No ingredients found for the given item ID: {detail.ItemRefListID}. Skipping item adjustment.");
                    }
                }
            }

            // After processing all rows, insert the inventory adjustments
            foreach (var adjustment in adjustmentDetails)
            {
                inventoryadjustment_class adjustmentClass = new inventoryadjustment_class();
                bool result = await adjustmentClass.InsertInventoryAdjustmentAsync(adjustment);

                if (result)
                {
                    AppLogger.LogInfo($"Inventory adjustment inserted successfully: {adjustment.ItemRef_FullName1}");
                }
                else
                {
                    AppLogger.LogError($"Inventory adjustment insertion failed for ingredient: {adjustment.ItemRef_FullName1}");
                }
            }

            var res = await ClassName_Class.FetchClassAsync(class_header);

            if (res == null)
            {
                AppLogger.LogError($"Class not found for class header: {class_header}");
                return; // Stop processing if class is not found
            }

            var receipt = new SalesReceipt
            {
                TxnID = txnIDGenerated,
                ClassRef_ListID = res.ListId,
                ClassRef_FullName = res.Name,
                TxnDate = DateTime.Now,
                RefNumber = string.Concat($"{formattedDate}-{inc_num}"),
                Subtotal = totalAmount,
                TotalAmount = totalAmount,
                Status = "ADD"
            };

            bool sealse = await SalesReceipt_Class.InsertSalesReceiptAsync(receipt);

            if (sealse)
            {

            }
        }


    }
}
