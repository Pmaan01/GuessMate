using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GuessMate
{
    public class GameClient
    {
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private string serverAddress = "127.0.0.1";  // Localhost for testing (change this to the actual server's IP address if needed)
        private int serverPort = 8888;

        public GameClient()
        {
            tcpClient = new TcpClient();
        }

        public void ConnectToServer(string gameCode)
        {
            try
            {
                tcpClient.Connect(serverAddress, serverPort);
                networkStream = tcpClient.GetStream();

                // Send the game code to the server
                string connectionMessage = $"Joining game with code: {gameCode}";
                byte[] msg = Encoding.UTF8.GetBytes(connectionMessage);
                networkStream.Write(msg, 0, msg.Length);

                // Start reading messages from the server asynchronously
                Thread readThread = new Thread(ReadServerMessages);
                readThread.Start();

                // Read the initial server response (for debugging or initial confirmation)
                byte[] buffer = new byte[4096];
                int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                string serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Server says: " + serverResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error connecting to server: " + ex.Message);
            }
        }

        // Method to handle asynchronous reading from the server
        private void ReadServerMessages()
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading from server: " + ex.Message);
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
            try
            {
                networkStream?.Close();
                tcpClient?.Close();
                Console.WriteLine("Connection closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error closing connection: " + ex.Message);
            }
        }
    }
}
