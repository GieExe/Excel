using MySql.Data.MySqlClient;
using Reader_Excell.OOP;
using Reader_Excell.Utilities;
using System;



namespace Reader_Excell.Class
{
    internal class inventoryadjustment_class
    {
        // Insert inventory adjustment into the salesreceiptlinedetail table asynchronously
        public async Task<bool> InsertInventoryAdjustmentAsync(inventoryadjustmentlinedetail adjustment)
        {
            try
            {
                // Establish the connection to the database
                using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await conn.OpenAsync(); // Use asynchronous open

                    // Prepare the SQL insert command
                    string query = @"
                        INSERT INTO inventoryadjustmentlinedetail (TxnLineID, ItemRef_ListID, ItemRef_FullName, QuantityDifference, IDKEY)
                        VALUES (@TxnLineID, @ItemRef_ListID, @ItemRef_FullName, @QuantityDifference, @IDKEY)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Use parameters to prevent SQL injection
                        cmd.Parameters.AddWithValue("@TxnLineID", adjustment.TxTLineID1);
                        cmd.Parameters.AddWithValue("@ItemRef_ListID", adjustment.ItemRef_ListID1);
                        cmd.Parameters.AddWithValue("@ItemRef_FullName", adjustment.ItemRef_FullName1);
                        cmd.Parameters.AddWithValue("@QuantityDifference", adjustment.QuantityDifference1);
                        cmd.Parameters.AddWithValue("@IDKEY", adjustment.IDKEY1);

                        // Execute the insert command asynchronously
                        int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Use asynchronous execute
                        FileFunctions.InventoryADline.Add(adjustment.TxTLineID1);
                        // Return true if the insert was successful
                        return rowsAffected > 0;


                    }
                    
                }


            }
            catch (Exception ex)
            {
                // Log the exception (you can implement logging here)
                Console.WriteLine($"Error inserting inventory adjustment: {ex.Message}");
                await DelFunc.CleanupFailedTransactionAsync(FileFunctions.txnLineIDs, FileFunctions.newtxnID, FileFunctions.refinv_, FileFunctions.InventoryAD, FileFunctions.InventoryADline, FileFunctions.refinv_id);
                return false;
            }
        }

        public async Task<bool> InsertInventoryAdjustmentAsync(inventoryadjustment linedetails)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await conn.OpenAsync();

                    string query = @"
                        INSERT INTO inventoryadjustment (TxnID, TimeCreated, ClassRef_ListID, ClassRef_FullName, RefNumber)
                        VALUES (@TxnID, @TimeCreated, @ClassRef_ListID, @ClassRef_FullName, @RefNumber)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TxnID", linedetails.TxnLineID1);
                        cmd.Parameters.AddWithValue("@TimeCreated", linedetails.TimeCreated1);
                        cmd.Parameters.AddWithValue("@ClassRef_ListID", linedetails.ClassRef_ListID1);
                        cmd.Parameters.AddWithValue("@ClassRef_FullName", linedetails.ClassRef_FullName1);
                        cmd.Parameters.AddWithValue("@RefNumber", linedetails.RefNumber1);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        FileFunctions.InventoryAD.Add(linedetails.TxnLineID1);
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Implement logging instead of Console.WriteLine
                await DelFunc.CleanupFailedTransactionAsync(FileFunctions.txnLineIDs,
                                            FileFunctions.newtxnID,
                                            FileFunctions.refinv_,
                                            FileFunctions.InventoryAD,
                                            FileFunctions.InventoryADline,
                                            FileFunctions.refinv_id);

                Console.WriteLine($"Error inserting inventory adjustment: {ex.Message}");
                return false;
            }
        }

    }
}
