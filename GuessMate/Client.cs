using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GuessMate
{
    public class GameClient
    {
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private StreamReader reader;
        private StreamWriter writer;
        private string serverAddress = "127.0.0.1";  // Localhost for testing
        private int serverPort = 8080;
        private readonly object streamLock = new object(); // Lock object for stream synchronization
        public bool IsConnected { get; private set; } // Add this property

        public GameClient ()
        {
            tcpClient = new TcpClient();
            IsConnected = false; // Initialize as not connected
        }

        public void ConnectToServer(string gameCode)
        {
            try
            {
                tcpClient.Connect(serverAddress, serverPort);
                networkStream = tcpClient.GetStream();
                reader = new StreamReader(networkStream);
                writer = new StreamWriter(networkStream) { AutoFlush = true };
                IsConnected = true;

                // Send the game code to the server
                string connectionMessage = $"StartGame";
                byte[] msg = Encoding.UTF8.GetBytes(connectionMessage);
                networkStream.Write(msg, 0, msg.Length);

                // Start reading messages from the server in a separate thread
                Thread readThread = new Thread(ReadServerMessages);
                readThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
                IsConnected = false;
            }
        }
            public void UploadImages(string[] imagePaths)
            {
                if (networkStream != null && tcpClient.Connected)
                {
                    foreach (var path in imagePaths)
                    {
                        if (File.Exists(path))
                        {
                            string message = $"UploadImage {Path.GetFileName(path)}";
                            byte[] msg = Encoding.UTF8.GetBytes(message);
                            WriteToStream(msg);

                            // Send the image bytes
                            byte[] imageBytes = File.ReadAllBytes(path);
                            lock (streamLock)
                            {
                                networkStream.Write(imageBytes, 0, imageBytes.Length);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No connection to the server.");
                }
            }

            public void ReadServerMessages()
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    while (tcpClient.Connected)
                    {
                        int bytesRead;
                        lock (streamLock)
                        {
                            bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                        }

                        if (bytesRead > 0)
                        {
                            string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine("Server: " + serverMessage);
                            ProcessGameMessage(serverMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading from server: " + ex.Message);
                }
            }
        public async Task<Dictionary<string, int>> RequestHighScoresAsync()
        {
            var highScores = new Dictionary<string, int>();

            try
            {
                // Send the request to the server
                string requestMessage = "REQUEST_HIGHSCORES";
                byte[] msg = Encoding.UTF8.GetBytes(requestMessage);
                networkStream.Write(msg, 0, msg.Length);

                // Prepare to read the server's response
                byte[] buffer = new byte[4096];
                int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                // Convert the response to a string
                string serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Check if the response starts with "HighScores|"
                if (!string.IsNullOrEmpty(serverResponse) && serverResponse.StartsWith("HighScores|"))
                {
                    var highScoresJson = serverResponse.Substring("HighScores|".Length);
                    var scores = JsonConvert.DeserializeObject<Dictionary<string, int>>(highScoresJson);

                    // Populate the highScores dictionary
                    foreach (var score in scores)
                    {
                        highScores[score.Key] = score.Value;
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected response from server: " + serverResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error requesting high scores: " + ex.Message);
            }

            return highScores;
        }



        private void ProcessGameMessage(string message)
        {
            if (message.StartsWith("Round"))
            {
                Console.WriteLine(message); // Notify the player about the round
            }
            else if (message.StartsWith("Time's up"))
            {
                Console.WriteLine(message); // Notify players that time is up
            }
            else if (message.StartsWith("Game over"))
            {
                Console.WriteLine(message); // Notify players that the game is over
                CloseConnection(); // Optionally close the connection
            }
            else if (message.StartsWith("Selected theme:"))
            {
                Console.WriteLine(message); // Notify players about the selected theme
            }
            else if (message.StartsWith("You are not allowed to select the theme."))
            {
                Console.WriteLine(message); // Notify the player they cannot select the theme
            }
            else if (message.StartsWith("HighScores|"))
            {
                // Extract and display high scores
                string highScoresJson = message.Substring("HighScores|".Length);
                DisplayHighScores(highScoresJson);
            }
        }

        private void DisplayHighScores(string highScoresJson)
        {
            // Assuming you have a way to parse the high scores from JSON
            Console.WriteLine("High Scores:\n" + highScoresJson);
            // Here, you can use JSON parsing to convert highScoresJson into a list of player names and scores
            // and then display it in your UI or in the console
        }

        public void SendThemeUpdate(string theme)
        {
            // Logic to update the UI with the new theme
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Assuming you have a method to update the theme in the UI
                UpdateThemeInUI(theme);
            });
        }

        private void UpdateThemeInUI(string theme)
        {
            // Logic to update the UI with the new theme
            MessageBox.Show($"The selected theme is: {theme}");
        }

        public void SendScoreToServer(string playerName, int score)
        {
            if (networkStream != null && tcpClient.Connected)
            {
                string message = $"SubmitScore|{playerName}|{score}";
                byte[] msg = Encoding.UTF8.GetBytes(message);
                networkStream.Write(msg, 0, msg.Length);
                Console.WriteLine($"Sent score {score} for player {playerName} to server.");
            }
            else
            {
                Console.WriteLine("No connection to the server.");
            }
        }

        // Asynchronous method to request high scores
 
        // This method ensures that only one operation happens at a time on the networkStream
        private void WriteToStream(byte[] msg)
        {
            lock (streamLock)
            {
                if (networkStream != null && tcpClient.Connected)
                {
                    networkStream.Write(msg, 0, msg.Length);
                }
                else
                {
                    Console.WriteLine("No connection to the server.");
                }
            }
        }
        public void SendJoinGameMessage(string gameCode)
        {
            // Create the message to send to the server
            string message = $"JoinGame|{gameCode}";

            // Convert the message to bytes
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Send the message to the server
            NetworkStream stream = tcpClient.GetStream(); // Assuming tcpClient is your TcpClient instance
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
        public void SendStartGameMessage()
        {
            if (networkStream != null && tcpClient.Connected)
            {
                string startGameMessage = "StartGame";
                byte[] msg = Encoding.UTF8.GetBytes(startGameMessage);
                networkStream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent StartGame message to server.");
            }
            else
            {
                Console.WriteLine("No connection to the server.");
            }
        }
        public void JoinGame(string gameCode)
        {
            if (networkStream != null && tcpClient.Connected)
            {
                // Create the message to send to the server
                string message = $"JoinGame|{gameCode}";

                // Convert the message to bytes
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                // Send the message to the server
                lock (streamLock)
                {
                    networkStream.Write(messageBytes, 0, messageBytes.Length);
                    Console.WriteLine($"Sent JoinGame message for game code: {gameCode}");
                }
            }
            else
            {
                Console.WriteLine("No connection to the server.");
            }
        }
        public void SendMessageToServer(string message)
        {
            if (networkStream != null && tcpClient.Connected)
            {
                byte[] msg = Encoding.UTF8.GetBytes(message);
                networkStream.Write(msg, 0, msg.Length);
            }
            else
            {
                Console.WriteLine("No connection to the server.");
            }
        }

        public void CloseConnection()
        {
            if (tcpClient != null)
            {
                tcpClient.Close();
                Console.WriteLine("Connection closed.");
            }
        }
    }
}
