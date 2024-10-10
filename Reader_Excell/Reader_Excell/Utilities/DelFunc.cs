using MySql.Data.MySqlClient;
using Reader_Excel;
using Reader_Excell.Class;
using Reader_Excell.Utilities; // Add this to access the MainLoop method
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reader_Excell.Properties;

namespace Reader_Excell.Utilities
{
    public class DelFunc
    {
        public static async Task CleanupFailedTransactionAsync(
            List<string> txnLineIDs,
            string? txnID,
            int? ref_id,
            List<string> inventoryadjustment,
            List<string> inventoryadjustmenlinedetail,
            int? refinv_id)
        {
            try
            {
                // Existing code...
                // Print or log the inventoryadjustment list
                if (inventoryadjustment != null && inventoryadjustment.Any())
                {
                    Console.WriteLine("Inventory Adjustment List:");
                    AppLogger.LogInfo("Inventory Adjustment List:");
                    foreach (var invad in inventoryadjustment)
                    {
                        if (!string.IsNullOrEmpty(invad))
                        {
                            Console.WriteLine($"TxnID: {invad}"); // Print to console
                            AppLogger.LogInfo($"TxnID: {invad}"); // Log to your application logger
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Inventory Adjustment List is empty or null.");
                    AppLogger.LogInfo("Inventory Adjustment List is empty or null.");
                }

                // Print or log the inventoryadjustmenlinedetail list
                if (inventoryadjustmenlinedetail != null && inventoryadjustmenlinedetail.Any())
                {
                    Console.WriteLine("Inventory Adjustment Line Detail List:");
                    AppLogger.LogInfo("Inventory Adjustment Line Detail List:");
                    foreach (var invadde in inventoryadjustmenlinedetail)
                    {
                        if (!string.IsNullOrEmpty(invadde))
                        {
                            Console.WriteLine($"TxnLineID: {invadde}"); // Print to console
                            AppLogger.LogInfo($"TxnLineID: {invadde}"); // Log to your application logger
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Inventory Adjustment Line Detail List is empty or null.");
                    AppLogger.LogInfo("Inventory Adjustment Line Detail List is empty or null.");
                }

                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // Check if txnLineIDs is not null or empty before iterating
                    if (txnLineIDs != null && txnLineIDs.Any())
                    {
                        foreach (var txnLineID in txnLineIDs)
                        {
                            string deleteLineDetailQuery = "DELETE FROM salesreceiptlinedetail WHERE TxnLineID = @TxnLineID";
                            using (var deleteLineDetailCmd = new MySqlCommand(deleteLineDetailQuery, connection))
                            {
                                AppLogger.LogInfo($"Reverting Inserted in SaleReceiptLine!: {txnLineID}");
                                deleteLineDetailCmd.Parameters.AddWithValue("@TxnLineID", txnLineID);
                                await deleteLineDetailCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Check if inventoryadjustment is not null or empty before iterating
                    if (inventoryadjustment != null && inventoryadjustment.Any())
                    {
                        foreach (var invad in inventoryadjustment)
                        {
                            string DeleteinventoryAD = "DELETE FROM inventoryadjustment WHERE TxnID = @inventoryadjustment";
                            using (var DeleteinventoryADcmd = new MySqlCommand(DeleteinventoryAD, connection))
                            {
                                AppLogger.LogInfo($"Reverting Inserted in inventoryadjustment!: {invad}");
                                DeleteinventoryADcmd.Parameters.AddWithValue("@inventoryadjustment", invad);
                                await DeleteinventoryADcmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Check if inventoryadjustmenlinedetail is not null or empty before iterating
                    if (inventoryadjustmenlinedetail != null && inventoryadjustmenlinedetail.Any())
                    {
                        foreach (var invadde in inventoryadjustmenlinedetail)
                        {
                            string DeleteinventoryADS = "DELETE FROM inventoryadjustmentlinedetail WHERE TxnLineID = @inventoryadjustmentlinedetail";
                            using (var DeleteinventoryADcmdS = new MySqlCommand(DeleteinventoryADS, connection))
                            {
                                AppLogger.LogInfo($"Reverting Inserted in inventoryadjustmentlinedetail!: {invadde}");
                                DeleteinventoryADcmdS.Parameters.AddWithValue("@inventoryadjustmentlinedetail", invadde);
                                await DeleteinventoryADcmdS.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Check if ref_id has a value before running the query
                    if (ref_id.HasValue)
                    {
                        string deleteReceiptQuery = "DELETE FROM ref_table WHERE inc_num = @ref_id";
                        using (var deleteReceiptCmd = new MySqlCommand(deleteReceiptQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in ref_table!: {ref_id}");
                            deleteReceiptCmd.Parameters.AddWithValue("@ref_id", ref_id.Value);
                            await deleteReceiptCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Check if txnID is not null
                    if (!string.IsNullOrEmpty(txnID))
                    {
                        string deleteReceiptQuery = "DELETE FROM salesreceipt WHERE TxnID = @TxnID";
                        using (var deleteReceiptCmd = new MySqlCommand(deleteReceiptQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in SaleReceipt!: {txnID}");
                            deleteReceiptCmd.Parameters.AddWithValue("@TxnID", txnID);
                            await deleteReceiptCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Check if refinv_id has a value before running the query
                    if (refinv_id.HasValue)
                    {
                        string deletereftableQuery = "DELETE FROM ingredientsref WHERE INC_NUM = @INC_NUM";
                        using (var deleteReceiptCmd = new MySqlCommand(deletereftableQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in ingredientsref!: {refinv_id}");
                            deleteReceiptCmd.Parameters.AddWithValue("@INC_NUM", refinv_id.Value);
                            await deleteReceiptCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                AppLogger.LogInfo("All Inserted Has Been Reverted!");
                
                // Call the main menu after cleanup
                await Program.MainLoop(Settings.Default.Path); // Passing empty string to signify no change in folder path

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error cleaning up failed transaction: {ex.Message}";
                AppLogger.LogError(errorMessage);
            }
        }
    }
}
