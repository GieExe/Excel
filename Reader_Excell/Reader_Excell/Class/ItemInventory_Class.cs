//ItemInventory_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excel;

namespace Reader_Excell.Class
{
    internal class ItemInventory_Class
    {
        public static async Task<ItemInventory> GetItemDetailsFromDatabaseAsync(string description)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT ListID, Name, FullName, SalesDesc, AssetAccountRef_ListID
                FROM iteminventory
                WHERE LOWER(SalesDesc) = LOWER(@Description)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Description", description);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new ItemInventory
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
                Console.WriteLine($"Database error: {ex.Message}");
            }
            return null;
        }

    }
}
