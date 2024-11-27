using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Media.Imaging;

namespace GuessMate
{
    public class PlayerImageDatabaseHelper
    {
        private string _connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=PlayerImagesDB;Integrated Security=True;";


        // Save player image to the database with image path
        public void SavePlayerImage(string playerName, string imageName, string hint, byte[] imageData, string imagePath)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO PlayerImages (PlayerName, ImageName, Hint, ImageData, ImagePath) VALUES (@PlayerName, @ImageName, @Hint, @ImageData, @ImagePath)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName);
                    command.Parameters.AddWithValue("@ImageName", imageName);
                    command.Parameters.AddWithValue("@Hint", hint);
                    command.Parameters.AddWithValue("@ImageData", imageData);
                    command.Parameters.AddWithValue("@ImagePath", imagePath); // Add the ImagePath parameter

                    command.ExecuteNonQuery();
                }
            }
        }

        // Remove all player images from the database
        public void RemovePlayerImages(string playerName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "DELETE FROM PlayerImages WHERE PlayerName = @PlayerName"; // Delete only specific player's images
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName); // Use parameterized query to prevent SQL injection
                    command.ExecuteNonQuery();
                }
            }
        }


        // Get all images for a specific player from the database
        public List<ImageData> GetPlayerImages(string playerName)
        {
            List<ImageData> images = new List<ImageData>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT PlayerName, ImageName, Hint, ImageData, ImagePath FROM PlayerImages WHERE PlayerName = @PlayerName"; // Add PlayerName to the query

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Retrieve data from the reader
                            string imageName = reader.GetString(reader.GetOrdinal("ImageName"));
                            string hint = reader.GetString(reader.GetOrdinal("Hint"));
                            byte[] imageData = (byte[])reader["ImageData"];
                            string imagePath = reader.GetString(reader.GetOrdinal("ImagePath")); // Get the ImagePath

                            // Convert byte array to BitmapImage
                            BitmapImage bitmapImage = new BitmapImage();
                            using (MemoryStream ms = new MemoryStream(imageData))
                            {
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = ms;
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Ensure the image is cached
                                bitmapImage.EndInit();
                                bitmapImage.Freeze(); // Freeze the image for cross-thread access
                            }

                            // Create ImageData object with all properties and add to list
                            // Create an ImageData object with the image byte data and image path
                            ImageData imageInfo = new ImageData(
                                imagePath,  // Include the image path
                                hint,  // Hint for the image
                                imageName  // Image name
                            )
                            {
                                ImagePreview = bitmapImage // Set the preview image (BitmapImage)
                            };

                            images.Add(imageInfo);
                        }
                    }
                }
            }
            return images;
        }
    }
}