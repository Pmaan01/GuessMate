using System.Data.SqlClient;


namespace GuessMate
{ 
    public class PlayerImageDatabaseHelper
    {
        private string _connectionString = "Server=localhost\\TEW_SQLEXPRESS;Database=PlayerImagesDB;Integrated Security=True;";

        public void SavePlayerImage(string playerName, string imageName, string hint, byte[] imageData)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO PlayerImages (PlayerName, ImageName, Hint, ImageData) VALUES (@PlayerName, @ImageName, @Hint, @ImageData)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlayerName", playerName);
                    command.Parameters.AddWithValue("@ImageName", imageName);
                    command.Parameters.AddWithValue("@Hint", hint);
                    command.Parameters.AddWithValue("@ImageData", imageData);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void RemovePlayerImages()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "DELETE FROM PlayerImages";
                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }

}
