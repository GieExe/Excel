using MySql.Data.MySqlClient;
using Reader_Excell.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.Utilities
{
    public class DelFunc
    {
        public static async Task CleanupFailedTransactionAsync(List<string> txnLineIDs, string? txnID, int ref_id) // Change to public
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

                    if (ref_id != null)
                    {
                        string deleteReceiptQuery = "DELETE FROM ref_table WHERE inc_num = @ref_id";
                        using (var deleteReceiptCmd = new MySqlCommand(deleteReceiptQuery, connection))
                        {
                            AppLogger.LogInfo($"Reverting Inserted in ref_table!: {ref_id}");
                            deleteReceiptCmd.Parameters.AddWithValue("@ref_id", ref_id);
                            await deleteReceiptCmd.ExecuteNonQueryAsync();
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
                    
                }
                Console.WriteLine("All Inserted Has Been Reverted!");
                AppLogger.LogInfo($"All Inserted Has Been Reverted!");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error cleaning up failed transaction: {ex.Message}";
                Console.WriteLine(errorMessage);
                AppLogger.LogError(errorMessage);
            }

            Console.WriteLine(txnID);
        }
    }

}
