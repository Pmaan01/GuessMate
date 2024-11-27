
using System.Data.SqlClient;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;

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

            // Insert image data into the database (including ImagePath)
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Images (Category, ImageName, ImageData, ImageHint, ImagePath) VALUES (@Category, @ImageName, @ImageData, @ImageHint, @ImagePath)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Category", category);
                    command.Parameters.AddWithValue("@ImageName", imageName);
                    command.Parameters.AddWithValue("@ImageData", imageData);
                    command.Parameters.AddWithValue("@ImageHint", imageHint);
                    command.Parameters.AddWithValue("@ImagePath", imagePath); // Insert ImagePath

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

        // Gets images from the database based on category (including ImagePath)
        public static List<ImageData> GetImagesFromDatabase(string category)
        {
            List<ImageData> images = new List<ImageData>();
            string connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=GameResourcesDB;Trusted_Connection=True;";

            string query = "SELECT ImageName, ImageData, ImageHint, ImagePath FROM Images WHERE Category = @Category"; // Include ImagePath in the query

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
                            byte[] imageData = reader["ImageData"] as byte[];  // Fetch the image data (byte array)
                            string imageHint = reader.GetString(2);
                            string imagePath = reader.GetString(3); // Fetch the image path from the database

                            BitmapImage imagePreview = null;

                            // If image data is present, convert it to a BitmapImage
                            if (imageData != null)
                            {
                                imagePreview = ConvertByteArrayToBitmapImage(imageData);
                            }
                            else if (File.Exists(imagePath)) // If image data is not available, use the image from the path
                            {
                                imagePreview = LoadImageFromPath(imagePath);
                            }
                            else
                            {
                                MessageBox.Show($"Image data and path are missing for image: {imageName}");
                            }

                            // Create an ImageData object with the image byte data and image path
                            ImageData imageInfo = new ImageData(
                                imagePath,  // Include the image path
                                imageHint,  // Hint for the image
                                imageName  // Image name
                            )
                            {
                                ImagePreview = imagePreview // Set the preview image (BitmapImage)
                            };

                            images.Add(imageInfo);  // Add the image info to the list
                        }
                    }
                }
            }

            return images;
        }

        public static BitmapImage ConvertByteArrayToBitmapImage(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                return null; // Handle the case where the byte array is empty or null
            }

            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Optional: Improves performance if image is used in a multi-threaded environment
            }
            return bitmap;
        }

        // Loads image from the file path and converts to BitmapImage
        public static BitmapImage LoadImageFromPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return null; // Return null if the path is invalid or file doesn't exist
            }

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // Optional: Improves performance if image is used in a multi-threaded environment

            return bitmap;
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
