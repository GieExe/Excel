using MySql.Data.MySqlClient;
using Reader_Excell.OOP;
using Reader_Excell.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader_Excell.Class
{
    internal class IngredientsTableRef
    {
        public static async Task<bool> InsertIngredientRefTable(ingredientsref reference)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                                    INSERT INTO ingredientsref 
                                    (DATE, INC_NUM)
                                    VALUES 
                                    (@DATE, @INC_NUM)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DATE", reference.DATE);
                        command.Parameters.AddWithValue("@INC_NUM", reference.INC_NUM);

                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Successfully Inserted In ref_Table: {reference.DATE}");
                        AppLogger.LogInfo($"Successfully Inserted In ref_Table: {reference.DATE}");
                        FileFunctions.re_id = reference.INC_NUM;
                        return true; // Return true if the insert was successful
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return false; // Return false if there was an error
            }
        }

        public static async Task<ingredientsref> FetchLastReferenceIngredientsAsync()
        {
            ingredientsref lastReference = null;

            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                            SELECT ID, DATE, INC_NUM 
                            FROM ingredientsref 
                            ORDER BY id DESC 
                            LIMIT 1"; // Get the last row based on id

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                lastReference = new ingredientsref
                                {
                                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                    DATE = reader.GetDateTime(reader.GetOrdinal("DATE")), // Read as DateTime
                                    INC_NUM = reader.GetInt32(reader.GetOrdinal("INC_NUM"))
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

            return lastReference; // Return the last reference or null if not found
        }
    }
}
