//Ingredients_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excell.OOP; // Reference to the ingredients_table class
using System;
using System.Collections.Generic;

namespace Reader_Excell.Class
{
    internal class Ingredients_Class
    {

        public List<ingredients_table> GetIngredientsByItemInventoryId(string itemInventoryId)
        {
            List<ingredients_table> ingredientsWithItems = new List<ingredients_table>();

            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();

                string query = @"
                    SELECT
                        i.ListID AS _ingredient_id,
                        i.FullName,
                        ing.qty AS _qty,
                        ing.iteminventory_id AS _iteminventory_id
                    FROM
                        ingredients_table AS ing
                    INNER JOIN 
                        iteminventory AS i
                        ON i.ListID = ing.ingredient_id
                    WHERE
                        ing.iteminventory_id = @ItemInventoryId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ItemInventoryId", itemInventoryId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ingredientsWithItems.Add(new ingredients_table
                        {
                            Ingredient_id = reader["_ingredient_id"].ToString(), // Maps to _ingredient_id
                            FullName = reader["FullName"].ToString(),           // Maps to FullName
                            Qty = Convert.ToInt32(reader["_qty"]),              // Maps to _qty
                            Iteminventory_id = reader["_iteminventory_id"].ToString() // Maps to _iteminventory_id
                        });
                    }
                }
            }

            return ingredientsWithItems;
        }
    }
}
