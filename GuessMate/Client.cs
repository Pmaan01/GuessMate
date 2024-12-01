using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace GuessMate
{
    public class GameClient
    {
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private string serverAddress = "127.0.0.1";  // Localhost for testing
        private int serverPort = 8888;
        public bool IsConnected { get; private set; } // Add this property

        public GameClient()
        {
            tcpClient = new TcpClient();
            IsConnected = false; // Initialize as not connected
        }

       public void ConnectToServer(string gameCode)
    {
        try
        {
            string serverAddress = "127.0.0.1"; // or use "localhost"
            int serverPort = 8080; // Ensure this matches the server's listening port

            tcpClient.Connect(serverAddress, serverPort);
            networkStream = tcpClient.GetStream();
            IsConnected = true;

            // Send the game code to the server
            string connectionMessage = $"Joining game with code: {gameCode}";
            byte[] msg = Encoding.UTF8.GetBytes(connectionMessage);
            networkStream.Write(msg, 0, msg.Length);

            // Start reading messages from the server
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
                        networkStream.Write(msg, 0, msg.Length);

                        // Send the image bytes
                        byte[] imageBytes = File.ReadAllBytes(path);
                        networkStream.Write(imageBytes, 0, imageBytes.Length);
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
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
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