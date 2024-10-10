using MySql.Data.MySqlClient;
using Reader_Excell.OOP;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // or MySql.Data.MySqlClient for MySQL
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.Class
{
    internal class inventorysite_class
    {
        public async Task<List<inventorysite_>> GetInventorySiteByNameAsync(string name)
        {
            List<inventorysite_> inventoryList = new List<inventorysite_>();

            string connectionString = ConnectionClass.GetConnectionString();
            using (var connection = new MySqlConnection(connectionString)) // Use MySqlConnection for MySQL
            {
                connection.Open();

                string query = "SELECT ListID, Name FROM inventorysite WHERE Name = @Name";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var inventoryItem = new inventorysite_()
                            {
                                ListID1 = reader["ListID"].ToString(),
                                Name1 = reader["Name"].ToString()
                            };
                            inventoryList.Add(inventoryItem);
                        }
                    }
                }
            }

            return inventoryList;
        }
    }
}
