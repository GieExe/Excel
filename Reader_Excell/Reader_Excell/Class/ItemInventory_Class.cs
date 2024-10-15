//ItemInventory_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excel;

namespace Reader_Excell.Class
{
    internal class ItemInventory_Class
    {

        public static async Task<ItemInventory> FetchItemInventoryAsync(string name)
        {
            ItemInventory item = null;

            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "SELECT ListID, Name, FullName, SalesDesc, AssetAccountRef_ListID FROM iteminventory WHERE SalesDesc = @name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                item = new ItemInventory
                                {
                                    ListID = reader.GetString(reader.GetOrdinal("ListID")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                                    SalesDesc = reader.GetString(reader.GetOrdinal("SalesDesc")),
                                    AssetAccountRef_ListID = reader.GetString(reader.GetOrdinal("AssetAccountRef_ListID"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error FetchItemInventoryAsync: {ex.Message}");
            }

            return item; // Return the ItemInventory object or null if not found
        }

        public static async Task<bool> DoesItemInventoryExistAsync(string name)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "SELECT COUNT(*) FROM iteminventory WHERE SalesDesc = @name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);

                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0; // Return true if the item exists, false otherwise
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error DoesItemInventoryExistAsync: {ex.Message}");
                return false; // Return false if there was an error
            }
        }


    }
}
