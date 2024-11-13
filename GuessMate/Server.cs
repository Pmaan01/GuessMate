using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class GameServer
{
    private TcpListener tcpListener;
    private Thread listenerThread;
    private string gameCode;
    private bool isGameStarted = false;
    private List<TcpClient> connectedClients;
    private readonly object clientLock = new object();
    private int playerCount = 0; // Track the number of players connected
    private const int MaxPlayers = 4; // Maximum number of players allowed

    public object GameCode { get; internal set; }

    public GameServer(string gameCode)
    {
        this.gameCode = gameCode;
        this.connectedClients = new List<TcpClient>();
        this.tcpListener = new TcpListener(IPAddress.Any, 8888);  // Listen on port 8888
        this.listenerThread = new Thread(new ThreadStart(ListenForClients));
    }

    public void StartServer()
    {
        try
        {
            // Reuse the address in case of a crashed server
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpListener.Start();
            listenerThread.Start();
            Console.WriteLine($"Server started with game code: {gameCode}. Waiting for players to connect...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
        }
    }

    private void ListenForClients()
    {
        while (true)
        {
            try
            {
                // Block until a client connects
                TcpClient client = tcpListener.AcceptTcpClient();
                lock (clientLock)
                {
                    connectedClients.Add(client);
                    playerCount++;
                }

                // Send game status to client (multiplayer or single-player)
                NetworkStream networkStream = client.GetStream();
                string playerMessage = playerCount == 1 ? "Single-player game. Waiting for opponent..." : "Multiplayer game. Ready to start!";
                byte[] msg = Encoding.UTF8.GetBytes(playerMessage);
                networkStream.Write(msg, 0, msg.Length);

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client connection: {ex.Message}");
            }
        }
    }

    private void HandleClientComm(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream networkStream = tcpClient.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead;

        // Send game code to client
        string initialMessage = $"Connected to game code: {gameCode}";
        byte[] msg = Encoding.UTF8.GetBytes(initialMessage);
        networkStream.Write(msg, 0, msg.Length);

        // Listen for client messages (game actions)
        try
        {
            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received from client: " + message);

                // Start the game logic when "StartGame" is received
                if (message == "StartGame" && !isGameStarted)
                {
                    // Start the game logic
                    isGameStarted = true;

                    // Notify all clients that the game has started
                    byte[] gameStartMessage = Encoding.UTF8.GetBytes("Game started!");
                    lock (clientLock)
                    {
                        foreach (var client in connectedClients)
                        {
                            NetworkStream clientStream = client.GetStream();
                            clientStream.Write(gameStartMessage, 0, gameStartMessage.Length);
                        }
                    }

                    // If the player count is 1, start a single-player game
                    if (playerCount == 1)
                    {
                        StartSinglePlayerGame(tcpClient);
                    }
                    else
                    {
                        StartMultiplayerGame();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client communication: {ex.Message}");
        }
        finally
        {
            // Cleanup after client disconnects
            tcpClient.Close();
            lock (clientLock)
            {
                connectedClients.Remove(tcpClient);
                playerCount--;
            }
            Console.WriteLine("Client disconnected.");
        }
    }

    private void StartSinglePlayerGame(TcpClient client)
    {
        // Handle single-player game logic
        Console.WriteLine("Starting single-player game with a computer opponent.");
        // You can add more logic here for simulating a computer's turn, etc.
        // Send message to client indicating single-player mode
        byte[] message = Encoding.UTF8.GetBytes("Single-player mode started. Playing against the computer.");
        NetworkStream networkStream = client.GetStream();
        networkStream.Write(message, 0, message.Length);
    }

    private void StartMultiplayerGame()
    {
        // Handle multiplayer game logic
        Console.WriteLine("Starting multiplayer game.");
        // Send message to all connected clients that the game is starting
        byte[] message = Encoding.UTF8.GetBytes("Multiplayer mode started. Let's play!");
        lock (clientLock)
        {
            foreach (var client in connectedClients)
            {
                NetworkStream clientStream = client.GetStream();
                clientStream.Write(message, 0, message.Length);
            }
        }
    }

    public void StopServer()
    {
        try
        {
            tcpListener.Stop();  // Stop listening for new clients
            lock (clientLock)
            {
                foreach (var client in connectedClients)
                {
                    client.Close();  // Close each client's connection
                }
            }

            // Optionally, you can also stop the listener thread if needed
            listenerThread.Abort();
            Console.WriteLine("Server stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping server: {ex.Message}");
        }
    }
}
