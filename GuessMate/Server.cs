using GuessMate;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class GameServer : IGameServer
{
    private static Dictionary<string, GameServer> activeServers = new Dictionary<string, GameServer>();
    public TcpListener tcpListener;
    private Thread listenerThread;
    public string gameCode;
    private bool isGameStarted = false;
    private List<TcpClient> connectedClients;
    private readonly object clientLock = new object();
    public int playerCount = 0; // Track the number of players connected
    public const int MaxPlayers = 4; // Maximum number of players allowed
    private const int MaxRounds = 5; // Total rounds
    private const int GuessTimeLimit = 5; // Time limit for guessing in seconds
    public int currentRound = 0; // Track the current round
    private int currentPlayerIndex = 0; // Track whose turn it is
    public event Action OnPlayerConnected;
    public event Action OnAllPlayersConnected;
    private Dictionary<string, List<byte[]>> playerImages = new Dictionary<string, List<byte[]>>();

    public GameSession GameSession { get; private set; } // Add GameSession property
    public string SelectedTheme { get; private set; }
    IPAddress localIP;

    // Add a field to track whether the current player is the host or not
    private bool isHost = false;

    public GameServer(string gameCode)
    {   
        this.gameCode = gameCode;
        this.connectedClients = new List<TcpClient>();
        this.localIP = IPAddress.Loopback; // Use the Loopback IP explicitly
        this.tcpListener = new TcpListener(localIP, 8080);  // Listen on port 8080
        this.listenerThread = new Thread(ListenForClients);
        this.listenerThread.Start();
    }


    private IPAddress GetLocalIPAddress()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                return ip;
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }



    public void StartServer()
    {
        try
        {
            if (listenerThread != null && listenerThread.IsAlive)
            {
                Console.WriteLine("Server thread is already running.");
                return;
            }
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            tcpListener.Start();
            listenerThread = new Thread(ListenForClients);  // Ensure new thread is created
            listenerThread.Start();
            lock (activeServers)
            {
                activeServers[gameCode] = this; // Add this server to the active servers list
            }
            Console.WriteLine($"Server started with game code: {gameCode}. Waiting for players to connect...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
        }
    }


    public void ListenForClients()
    {
        try
        {
            tcpListener.Start();  // Start listening only once
            Console.WriteLine("Server is listening for clients...");
            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                connectedClients.Add(client);
                Console.WriteLine("Client connected.");

                lock (clientLock)
                {
                    connectedClients.Add(client);
                    playerCount++;
                }

                // Notify the new client of connection status
                NetworkStream networkStream = client.GetStream();
                string playerMessage = playerCount == 1 ? "You are the host. Waiting for opponents..." : "Waiting for the host or other players to join.";
                byte[] msg = Encoding.UTF8.GetBytes(playerMessage);
                networkStream.Write(msg, 0, msg.Length);

                if (playerCount == 1)
                {
                    isHost = true; // First player is the host
                    OnPlayerConnected?.Invoke();
                }

                // Start the communication thread for the client
                Thread clientThread = new Thread(HandleClientComm);
                clientThread.Start(client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting client connection: {ex.Message}");
        }
    }


    public static GameServer StartNewServer(string gameCode)
    {
        lock (activeServers)
        {
            if (activeServers.ContainsKey(gameCode))
            {
                Console.WriteLine($"A server with game code {gameCode} already exists.");
                return activeServers[gameCode]; // Return the existing server
            }

            GameServer newServer = new GameServer(gameCode);
            newServer.StartServer();
            activeServers[gameCode] = newServer; // Add the new server
            return newServer;
        }
    }


    public void HandleClientComm(object obj)
    {
        TcpClient tcpClient = (TcpClient)obj;
        NetworkStream networkStream = tcpClient.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead;

        // Send the game code to client
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
                    SetTheme(selectedTheme);
                }

                // Handle image uploads
                if (message.StartsWith("UploadImage"))
                {
                    string imageName = message.Substring("UploadImage ".Length).Trim();
                    List<byte[]> images;
                    if (!playerImages.ContainsKey(imageName))
                    {
                        playerImages[imageName] = new List<byte[]>();
                    }

                    // Read the image bytes from the client
                    byte[] imageBuffer = new byte[4096];
                    int imageBytesRead = networkStream.Read(imageBuffer, 0, imageBuffer.Length);
                    byte[] imageData = new byte[imageBytesRead];
                    Array.Copy(imageBuffer, imageData, imageBytesRead);
                    playerImages[imageName].Add(imageData);

                    BroadcastMessage($"Player uploaded image: {imageName}");
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
        if (currentRound < MaxRounds && playerCount > 0)
        {
            // Ensure playerCount is valid and start the round
            currentPlayerIndex = currentPlayerIndex % playerCount; // Reset in case of any inconsistency
            BroadcastMessage($"Round {currentRound + 1} has started! Player {connectedClients[currentPlayerIndex]} it's your turn to guess!");

            // Start the timer for guessing, the time is set based on GuessTimeLimit
            //Timer timer = new Timer(GuessTimeExpired, null, GuessTimeLimit * 1000, Timeout.Infinite);
        }
        else
        {
            //EndGame(); // End the game if rounds are finished or no players are available
        }
    }


    public void StartSinglePlayerGame(TcpClient client)
    {
        Console.WriteLine("Starting single-player game with a computer opponent.");
        byte[] message = Encoding.UTF8.GetBytes("Single-player mode started. Playing against the computer.");
        NetworkStream networkStream = client.GetStream();
        networkStream.Write(message, 0, message.Length);
    }

    public void BroadcastMessage(string message)
    {
        byte[] msg = Encoding.UTF8.GetBytes(message);
        List<Thread> threads = new List<Thread>();

        lock (clientLock)
        {
            foreach (var client in connectedClients)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        NetworkStream clientStream = client.GetStream();
                        clientStream.Write(msg, 0, msg.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error broadcasting message to client: {ex.Message}");
                    }
                });
                thread.Start();
                threads.Add(thread);
            }
        }

        // Optional: Wait for all threads to finish (blocking)
        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    public static GameServer Connect(string gameCode)
    {
        lock (activeServers)
        {
            if (activeServers.TryGetValue(gameCode, out GameServer server))
            {
                Console.WriteLine($"Connected to existing server with game code: {gameCode}");
                return server;
            }
            else
            {
                Console.WriteLine($"No active server found with game code: {gameCode}");
                return null;
            }
        }
    }
    public void SetTheme(string theme)
    {
        SelectedTheme = theme;
        Console.WriteLine($"Theme set to: {theme}");

        // You could implement theme-specific logic here (e.g., changing images, etc.)
    }

    public void StopServer()
    {
        if (connectedClients.Count == 0) // Ensure no active clients
        {
            tcpListener.Stop();
            listenerThread.Abort();
            Console.WriteLine($"Server {gameCode} stopped.");
        }
        else
        {
            Console.WriteLine($"Cannot stop the server. Active clients are connected.");
        }
    }
}  
