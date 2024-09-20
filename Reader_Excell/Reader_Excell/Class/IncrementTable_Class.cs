// ReferenceTable_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excell.Class;
using Reader_Excell.OOP;
using Reader_Excell.Utilities;
using System;
using System.Threading.Tasks;

namespace Reader_Excel.Class
{
    public class IncrementTable_Class
    {
        public static async Task<bool> InsertReferenceTableAsync(Referencetable reference)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                                    INSERT INTO ref_table 
                                    (date, inc_num)
                                    VALUES 
                                    (@DateTime, @Inc_num)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DateTime", reference.DateTime);
                        command.Parameters.AddWithValue("@Inc_num", reference.Inc_num);

                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"Successfully Inserted In ref_Table: {reference.DateTime}");
                        AppLogger.LogInfo($"Successfully Inserted In ref_Table: {reference.DateTime}");
                        FileFunctions.re_id = reference.Inc_num;
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

        public static async Task<Referencetable> FetchLastReferenceAsync()
        {
            Referencetable lastReference = null;

            try
            {
                using (var connection = new MySqlConnection(ConnectionClass.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = @"
                            SELECT id, date, inc_num 
                            FROM ref_table 
                            ORDER BY id DESC 
                            LIMIT 1"; // Get the last row based on id

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                lastReference = new Referencetable
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    DateTime = reader.GetDateTime(reader.GetOrdinal("date")), // Read as DateTime
                                    Inc_num = reader.GetInt32(reader.GetOrdinal("inc_num"))
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
