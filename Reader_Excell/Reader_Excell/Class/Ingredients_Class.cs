//Ingredients_Class.cs
using MySql.Data.MySqlClient;
using Reader_Excell.OOP; // Reference to the ingredients_table class
using System;
using System.Collections.Generic;

namespace Reader_Excell.Class
{
    internal class Ingredients_Class
    {
        // Add an ingredient
        public bool AddIngredient(ingredients_table ingredient)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();
                string query = "INSERT INTO ingredients_table (ingredient_id, qty, iteminventory_id) VALUES (@Ingredient_id, @Qty, @Iteminventory_id)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Ingredient_id", ingredient.Ingredient_id);
                cmd.Parameters.AddWithValue("@Qty", ingredient.Qty);
                cmd.Parameters.AddWithValue("@Iteminventory_id", ingredient.Iteminventory_id);

                return cmd.ExecuteNonQuery() > 0; // Returns true if insertion was successful
            }
        }

        // Update an ingredient
        public bool UpdateIngredient(ingredients_table ingredient)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();
                string query = "UPDATE ingredients_table SET qty = @Qty, iteminventory_id = @Iteminventory_id WHERE ingredient_id = @Ingredient_id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Ingredient_id", ingredient.Ingredient_id);
                cmd.Parameters.AddWithValue("@Qty", ingredient.Qty);
                cmd.Parameters.AddWithValue("@Iteminventory_id", ingredient.Iteminventory_id);

                return cmd.ExecuteNonQuery() > 0; // Returns true if update was successful
            }
        }

        // Delete an ingredient by ID
        public bool DeleteIngredient(string ingredientId)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();
                string query = "DELETE FROM ingredients_table WHERE ingredient_id = @Ingredient_id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Ingredient_id", ingredientId);

                return cmd.ExecuteNonQuery() > 0; // Returns true if deletion was successful
            }
        }

        // Fetch an ingredient by ID
        public ingredients_table GetIngredientById(string ingredientId)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();
                string query = "SELECT * FROM ingredients_table WHERE ingredient_id = @Ingredient_id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Ingredient_id", ingredientId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ingredients_table
                        {
                            Ingredient_id = reader["ingredient_id"].ToString(),
                            Qty = Convert.ToInt32(reader["qty"]),
                            Iteminventory_id = reader["iteminventory_id"].ToString()
                        };
                    }
                }
            }

            return null; // Return null if the ingredient is not found
        }


        // Method to fetch ingredients with item information
        // Method to fetch ingredients with item information
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

        // Fetch all ingredients
        public List<ingredients_table> GetAllIngredients()
        {
            List<ingredients_table> ingredients = new List<ingredients_table>();

            using (MySqlConnection conn = new MySqlConnection(ConnectionClass.GetConnectionString()))
            {
                conn.Open();
                string query = "SELECT * FROM ingredients_table";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ingredients.Add(new ingredients_table
                        {
                            Ingredient_id = reader["ingredient_id"].ToString(),
                            Qty = Convert.ToInt32(reader["qty"]),
                            Iteminventory_id = reader["iteminventory_id"].ToString()
                        });
                    }
                }
            }

            return ingredients;
        }
    }
}
