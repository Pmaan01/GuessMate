using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class GameServer
{
    public TcpListener tcpListener;
    private Thread listenerThread;
    public string gameCode;
    private bool isGameStarted = false;
    private List<TcpClient> connectedClients;
    private readonly object clientLock = new object();
    public int playerCount = 0; // Track the number of players connected
    private const int MaxPlayers = 4; // Maximum number of players allowed
    private const int MaxRounds = 5; // Total rounds
    private const int GuessTimeLimit = 5; // Time limit for guessing in seconds
    public int currentRound = 0; // Track the current round
    private int currentPlayerIndex = 0; // Track whose turn it is
    public event Action OnPlayerConnected;
    public event Action OnAllPlayersConnected; // Event to notify when all players are connected

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

    public void ListenForClients()
    {
        while (true)
        {
            try
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                lock (clientLock)
                {
                    connectedClients.Add(client);
                    playerCount++;
                }

                // Notify new client of connection
                NetworkStream networkStream = client.GetStream();
                string playerMessage = playerCount == 1 ? "Single-player game. Waiting for opponent..." : "Multiplayer game. Ready to start!";
                byte[] msg = Encoding.UTF8.GetBytes(playerMessage);
                networkStream.Write(msg, 0, msg.Length);

                // Notify that a player has connected
                OnPlayerConnected?.Invoke();

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting client connection: {ex.Message}");
            }
        }
    }

    public void HandleClientComm(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream networkStream = tcpClient.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead;

        // Send game code to client
        string initialMessage = $"Connected to game code: {gameCode}";
        byte[] msg = Encoding.UTF8.GetBytes(initialMessage);
        networkStream.Write(msg, 0, msg.Length);

        try
        {
            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received from client: " + message);

                if (message.StartsWith("StartGame") && !isGameStarted)
                {
                    isGameStarted = true;
                    BroadcastMessage("Game started!");

                    if (playerCount == 1)
                    {
                        StartSinglePlayerGame(tcpClient);
                    }
                    else
                    {
                        StartMultiplayerGame();
                    }
                }

                // Handle theme selection
                if (message.StartsWith("SelectTheme"))
                {
                    string selectedTheme = message.Substring("SelectTheme ".Length).Trim();
                    BroadcastThemeToClients(selectedTheme);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client communication: {ex.Message}");
        }
        finally
        {
            tcpClient.Close();
            lock (clientLock)
            {
                connectedClients.Remove(tcpClient);
                playerCount--;
            }
            Console.WriteLine("Client disconnected.");
            // Handle player disconnection logic if necessary
        }
    }

    public void BroadcastThemeToClients(string theme)
    {
        byte[] themeMessage = Encoding.UTF8.GetBytes($"Selected theme: {theme}");
        lock (clientLock)
        {
            foreach (var client in connectedClients)
            {
                NetworkStream clientStream = client.GetStream();
                clientStream.Write(themeMessage, 0, themeMessage.Length);
            }
        }
    }

    public void StartSinglePlayerGame(TcpClient client)
    {
        Console.WriteLine("Starting single-player game with a computer opponent.");
        byte[] message = Encoding.UTF8.GetBytes("Single-player mode started. Playing against the computer.");
        NetworkStream networkStream = client.GetStream();
        networkStream.Write(message, 0, message.Length);
    }

    public void StartMultiplayerGame()
    {
        if (playerCount == MaxPlayers) // Check if max players are connected
        {
            isGameStarted = true;
            BroadcastMessage("Multiplayer mode started. Let's play!");
            OnAllPlayersConnected?.Invoke(); // Notify that all players are connected
            StartRound();
        }
        else
        {
            BroadcastMessage($"Waiting for more players to join... {MaxPlayers - playerCount} spots left.");
        }
    }

    public void StartRound()
    {
        if (currentRound < MaxRounds)
        {
            BroadcastMessage($"Round {currentRound + 1} has started! Player {connectedClients[currentPlayerIndex]} it's your turn to guess!");
            Timer timer = new Timer(GuessTimeExpired, null, GuessTimeLimit * 1000, Timeout.Infinite);
        }
        else
        {
            EndGame();
        }
    }

    public void GuessTimeExpired(object state)
    {
        BroadcastMessage("Time's up! Moving to the next player's turn.");
        MoveToNextPlayer();
    }

    public void MoveToNextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % playerCount;
        StartRound();
    }

    public void EndGame()
    {
        BroadcastMessage("Game over! Thank you for playing!");
    }

    public void BroadcastMessage(string message)
    {
        byte[] msg = Encoding.UTF8.GetBytes(message);
        lock (clientLock)
        {
            foreach (var client in connectedClients)
            {
                NetworkStream clientStream = client.GetStream();
                clientStream.Write(msg, 0, msg.Length);
            }
        }
    }

    public void StopServer()
    {
        try
        {
            tcpListener.Stop();
            lock (clientLock)
            {
                foreach (var client in connectedClients)
                {
                    client.Close();
                }
            }
            listenerThread.Abort();
            Console.WriteLine("Server stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping server: {ex.Message}");
        }
    }
}