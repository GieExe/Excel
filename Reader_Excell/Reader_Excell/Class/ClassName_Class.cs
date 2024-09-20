using MySql.Data.MySqlClient;
using Reader_Excell.OOP;
using System;
using System.Threading.Tasks;

namespace Reader_Excell.Class
{
    public class ClassName_Class
    {
        public static async Task<ClassName> FetchClassAsync(string name)
        {
            ClassName classItem = null;

            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "SELECT ListId, Name FROM class WHERE Name = @name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                classItem = new ClassName
                                {
                                    ListId = reader.GetString(reader.GetOrdinal("ListId")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
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

            return classItem; // Return the ClassName object or null if not found
        }
    }
}
