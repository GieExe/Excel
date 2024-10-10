// SalesReceiptLineDetail_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excel.OOP;
using Reader_Excell.Class;
using Reader_Excell.Utilities;
using System;
using System.Threading.Tasks;

namespace Reader_Excel.Class
{
    internal class SalesReceiptLineDetail_Class
    {
        public static async Task<bool> InsertSalesReceiptLineDetailAsync(SalesReceiptLineDetail detail)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                                    INSERT INTO salesreceiptlinedetail 
                                    (TxnLineID, ItemRef_ListID, ItemRef_FullName, Description, Quantity, Rate, Amount, IDKEY, InventorySiteRef_ListID, InventorySiteRef_FullName)
                                    VALUES 
                                    (@TxnLineID, @ItemRefListID, @ItemRefFullName, @Description, @Quantity, @Rate, @Amount, @IdKey, @InventorySiteRef_ListID, @InventorySiteRef_FullName)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TxnLineID", detail.TxnLineID);
                        command.Parameters.AddWithValue("@ItemRefListID", detail.ItemRefListID);
                        command.Parameters.AddWithValue("@ItemRefFullName", detail.ItemRefFullName);
                        command.Parameters.AddWithValue("@Description", detail.Description);
                        command.Parameters.AddWithValue("@Quantity", detail.Quantity);
                        command.Parameters.AddWithValue("@Rate", detail.Rate);
                        command.Parameters.AddWithValue("@Amount", detail.Amount);
                        command.Parameters.AddWithValue("@IdKey", detail.IdKey);
                        command.Parameters.AddWithValue("@InventorySiteRef_ListID", detail.InventorySiteRef_ListID);
                        command.Parameters.AddWithValue("@InventorySiteRef_FullName", detail.InventorySiteRef_FullName);

                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Successfully Inserted : {detail.ItemRefFullName}");
                        AppLogger.LogInfo($"Successfully Inserted : {detail.ItemRefFullName}");
                        FileFunctions.txnLineIDs.Add(detail.TxnLineID);
                        return true; // Return true if the insert was successful
                    }

                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error Inserting : {detail.ItemRefFullName}");
                Console.WriteLine($"Database error: {ex.Message}");
                await DelFunc.CleanupFailedTransactionAsync(FileFunctions.txnLineIDs, FileFunctions.newtxnID, FileFunctions.refinv_, FileFunctions.InventoryAD, FileFunctions.InventoryADline, FileFunctions.refinv_id);
                return false; // Return false if there was an error
            }
        }
    }
}
