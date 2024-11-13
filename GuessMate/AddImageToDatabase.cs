using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;  // For image handling
using System.IO;

namespace GuessMate
{
    public class ImageDatabaseHelper
    {
        // Hardcoded connection string for testing (not recommended for production)
        string connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=GameResourcesDB;Trusted_Connection=True;";
        // Adds an image to the database if it doesn't already exist
        public void AddImageToDatabase(string category, string imageName, string imagePath, string imageHint)
        {

            // Check if image already exists in the database
            if (CheckImageExists(category, imageName))
            {
                Console.WriteLine("Image already exists in the database. Skipping insert.");
                return; // Exit the method if the image already exists
            }

            // Read the image data into a byte array
            byte[] imageData;
            using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    imageData = br.ReadBytes((int)fs.Length);
                }
            }

            // Insert image data into the database
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Images (Category, ImageName, ImageData, ImageHint) VALUES (@Category, @ImageName, @ImageData, @ImageHint)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@ImageName", imageName);
                    command.Parameters.AddWithValue("@ImageData", imageData);
                    command.Parameters.AddWithValue("@ImageHint", imageHint);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Image added successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to add the image.");
                    }
                }
            }
        }
        public string GetFolderPath(string category)
        {
            string folderPath = string.Empty;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT FolderPath FROM Categories WHERE Category = @Category"; // Adjust table and column names as needed
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", category);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            folderPath = reader["FolderPath"].ToString();
                        }
                    }
                }
            }

            return folderPath;
        }
        // Gets images from the database based on category
        public List<ImageData> GetImagesFromDatabase(string category)
        {
            List<ImageData> images = new List<ImageData>();
            string connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=GameResourcesDB;Trusted_Connection=True;";

            string query = "SELECT ImageName, ImageData, ImageHint FROM Images WHERE Category = @Category";

            using (var connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", category);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string imageName = reader.GetString(0);
                            byte[] imageData = reader["ImageData"] as byte[];
                            string imageHint = reader.GetString(2);

                            if (imageData != null)
                            {
                                // Convert byte[] to Image
                                using (MemoryStream ms = new MemoryStream(imageData))
                                {
                                    Image image = Image.FromStream(ms); // Create an Image from the byte array

                                    // Create an ImageData object and add to the list
                                    ImageData imageInfo = new ImageData(
                                         imageName, // ImagePath will be the imageName or you can modify it
                                         imageHint, // Hint for the image
                                         imageName  // ImageName as the name of the image
                                     );

                                    images.Add(imageInfo);
                                }
                            }
                        }
                    }
                }
            }

            return images;
        }

        // Checks if the image already exists in the database based on category and image name
        public bool CheckImageExists(string category, string imageName)
        {
            string connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=GameResourcesDB;Trusted_Connection=True;";

            // Query to check if the image already exists based on category and image name
            string query = "SELECT COUNT(*) FROM Images WHERE Category = @Category AND ImageName = @ImageName";

            using (var connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@ImageName", imageName);

                    connection.Open();
                    int count = (int)command.ExecuteScalar();

                    return count > 0; // If count is greater than 0, image exists
                }
            }
        }
    }
}
