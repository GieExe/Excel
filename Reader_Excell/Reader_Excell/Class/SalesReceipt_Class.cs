// SalesReceipt_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excel.OOP;
using Reader_Excell.Class;
using Reader_Excell.Utilities;
using System;
using System.Threading.Tasks;

namespace Reader_Excel.Class
{
    internal class SalesReceipt_Class
    {
        public static async Task<bool> InsertSalesReceiptAsync(SalesReceipt receipt)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                                    INSERT INTO salesreceipt 
                                    (TxnID, ClassRef_ListID, ClassRef_FullName, TxnDate, RefNumber, Subtotal, TotalAmount, Status)
                                    VALUES 
                                    (@TxnID, @ClassRef_ListID, @ClassRef_FullName, @TxnDate, @RefNumber, @Subtotal, @TotalAmount, @Status)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TxnID", receipt.TxnID);
                        command.Parameters.AddWithValue("@ClassRef_ListID", receipt.ClassRef_ListID);
                        command.Parameters.AddWithValue("@ClassRef_FullName", receipt.ClassRef_FullName);
                        command.Parameters.AddWithValue("@TxnDate", receipt.TxnDate);
                        command.Parameters.AddWithValue("@RefNumber", receipt.RefNumber);
                        command.Parameters.AddWithValue("@Subtotal", receipt.Subtotal);
                        command.Parameters.AddWithValue("@TotalAmount", receipt.TotalAmount);
                        command.Parameters.AddWithValue("@Status", receipt.Status);

                        await command.ExecuteNonQueryAsync();
                        AppLogger.LogInfo($"Successfully Inserted : {receipt.TxnID}");
                        Console.WriteLine($"Successfully Inserted : {receipt.TxnID}");
                        return true; // Return true if the insert was successful
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error Inserting : {receipt.TxnID}");
                Console.WriteLine($"Database error: {ex.Message}");
                await DelFunc.CleanupFailedTransactionAsync(FileFunctions.txnLineIDs, FileFunctions.newtxnID, FileFunctions.re_id);
                return false; // Return false if there was an error
            }
        }
    }
}
