
using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace ServerSide
{
    public class GameServer : IGameServer
    {
        private static Dictionary<string, GameServer> activeServers = new Dictionary<string, GameServer>();
        // Dictionary to store player names and their scores
        private Dictionary<string, int> highScores = new Dictionary<string, int>();
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
        private readonly string _highScoresFile = "highscores.txt";

        public bool IsServerReady { get; private set; } = false;
        public int Expectedplayer { get; set; }

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

                LoadHighScores();  // Load existing high scores

                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start();
                listenerThread = new Thread(ListenForClients);  // Ensure new thread is created
                listenerThread.Start();
                IsServerReady = true;

                // Add this server to the active servers list
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
        public void StoreHighScore(string playerName, int score)
        {
            if (highScores.ContainsKey(playerName))
            {
                // Update the score if the player already exists
                highScores[playerName] = Math.Max(highScores[playerName], score);
            }
            else
            {
                // Add new player and score
                highScores.Add(playerName, score);
            }
        }

        public Dictionary<string, int> GetHighScores()
        {
            return highScores;
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

        public void SaveHighScores()
        {
            string json = JsonConvert.SerializeObject(highScores);
            File.WriteAllText("highscores.json", json);
        }

        public static GameServer StartNewServer(string gameCode)
        {
            LoadActiveGameCodes();  // Load existing game codes from file

            lock (activeServers)
            {
                if (IsGameCodeActive(gameCode))
                {
                    Console.WriteLine($"A server with game code {gameCode} already exists.");
                    return activeServers[gameCode];  // Return the existing server
                }

                GameServer newServer = new GameServer(gameCode);
                newServer.StartServer();

                // Ensure that the server is added to activeServers
                activeServers[gameCode] = newServer; // Add the new server
                Console.WriteLine($"New server started with game code: {gameCode}");

                AddActiveGameCode(gameCode);  // Save the new game code to file
                return newServer;
            }
        }


        public void HandleClientComm(object obj)
        {
            TcpClient tcpClient = (TcpClient)obj;
            NetworkStream networkStream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received from client: " + message);

                    if (message.StartsWith("StartGame"))
                    {
                        isGameStarted = true;
                        BroadcastMessage("Game started!");
                        if (Expectedplayer == 1)
                        {
                            StartSinglePlayerGame(tcpClient);
                        }
                        else
                        {
                            StartMultiplayerGame();
                        }
                    }
                    if (message.StartsWith("JoinGame"))
                    {
                        string[] parts = message.Split('|');
                        if (parts.Length == 2)
                        {
                            string playerName = parts[1].Trim();
                            if (playerCount < MaxPlayers)
                            {
                                playerCount++;
                                Console.WriteLine($"New client '{playerName}' joined. Total players: {playerCount}/{MaxPlayers}");
                                byte[] msg = Encoding.UTF8.GetBytes($"Welcome {playerName}! You have joined the game.");
                                networkStream.Write(msg, 0, msg.Length);
                                StartGameIfReady(); // Check if the game can start
                            }
                            else
                            {
                                Console.WriteLine("Game is full, cannot accept more players.");
                            }
                        }
                    }
                    else if (message.StartsWith("SubmitScore"))
                    {
                        // Split the message by '|'
                        string[] parts = message.Split('|');

                        // Check if there are exactly 3 parts
                        if (parts.Length == 3)
                        {
                            // Extract the player name and score
                            string playerName = parts[1].Trim(); // This will give you 'a'
                            int score;

                            // Try to parse the score
                            if (int.TryParse(parts[2].Trim(), out score))
                            {
                                // Store the high score and perform other actions
                                StoreHighScore(playerName, score);
                                SaveHighScores();
                                BroadcastMessage($"Player {playerName} submitted a score of {score}.");
                            }
                            else
                            {
                                // Handle invalid score format
                                BroadcastMessage($"Failed to parse score for player {playerName}. Invalid score format.");
                            }
                        }
                        else
                        {
                            // Handle incorrect message format
                            BroadcastMessage("SubmitScore message format is incorrect. Expected format: SubmitScore|<PlayerName>|<Score>");
                        }
                    }
                    else if (message.Equals("REQUEST_HIGHSCORES"))
                    {
                        SendHighScores(networkStream);
                    }
                }
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
            }
        }
        public void BroadcastHighScores()
        {
            string highScoresJson = JsonConvert.SerializeObject(GetHighScores());
            BroadcastMessage($"HighScores|{highScoresJson}");
        }

        public void SendHighScores(NetworkStream networkStream)
        {
            try
            {
                // Retrieve high scores
                var highScores = GetHighScores();

                // Debug: Log the high scores to ensure they are retrieved correctly
                Console.WriteLine("High Scores Retrieved: " + JsonConvert.SerializeObject(highScores));

                // Serialize high scores into JSON format
                string highScoresJson = JsonConvert.SerializeObject(highScores);

                // Prepare the message to send
                string messageToSend = $"HighScores|{highScoresJson}";

                // Debug: Log the serialized message
                Console.WriteLine("Sending High Scores Message: " + messageToSend);

                // Convert the message to bytes
                byte[] highScoresMessage = Encoding.UTF8.GetBytes(messageToSend);

                // Check if the network stream is still connected
                if (networkStream.CanWrite)
                {
                    networkStream.Write(highScoresMessage, 0, highScoresMessage.Length);
                    Console.WriteLine("High Scores sent successfully.");
                }
                else
                {
                    Console.WriteLine("Network stream is not writable.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending high scores: {ex.Message}");
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
        public void LoadHighScores()
        {
            if (File.Exists("highscores.json"))
            {
                string json = File.ReadAllText("highscores.json");
                highScores = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
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


        public void StartGameIfReady()
        {
            if (playerCount == MaxPlayers)
            {
                BroadcastMessage("All players are connected. The game will start now!");
                StartMultiplayerGame();  // Start the game logic
            }
            else
            {
                BroadcastMessage($"Waiting for more players. {MaxPlayers - playerCount} spots left.");
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
            SaveHighScores();  // Save high scores before stopping
            RemoveActiveGameCode(gameCode);
            
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

        private const string GameCodesFile = "D:\\Assignment2\\GuessMate\\GuessMate\\GuessMate\\bin\\Debug\\net8.0-windows\\active_game_codes.json";
        private static HashSet<string> activeGameCodes = new HashSet<string>();
        private static readonly object fileLock = new object();  // Lock object to synchronize file access

        // Load active game codes from the file
        public static HashSet<string> LoadActiveGameCodes()
        {
            using (var mutex = new Mutex(false, "Global\\GameServerManagerMutex"))
            {
                mutex.WaitOne();
                try
                {
                    if (File.Exists(GameCodesFile))
                    {
                        string json = File.ReadAllText(GameCodesFile);
                        activeGameCodes = JsonConvert.DeserializeObject<HashSet<string>>(json) ?? new HashSet<string>();
                    }
                    else
                    {
                        Console.WriteLine("Game codes file does not exist. Creating a new one.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading game codes from file: {ex.Message}");
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            return activeGameCodes; // Return the loaded game codes
        }
        // Save active game codes to the file

        public static void SaveActiveGameCodes()
        {
            try
            {
                lock (fileLock)
                {
                    string json = JsonConvert.SerializeObject(activeGameCodes);
                    File.WriteAllText(GameCodesFile, json);
                    Console.WriteLine("Game codes saved successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game codes to file: {ex.Message}");
            }
        }


        // Check if a game code is active
        public static bool IsGameCodeActive(string gameCode)
        {
            lock (fileLock)  // Ensure thread safety when accessing the active game codes set
            {
                return activeGameCodes.Contains(gameCode);
            }
        }

        // Add a new game code to the active set and save it
        public static void AddActiveGameCode(string gameCode)
        {
            try
            {
                lock (fileLock)  // Ensure thread safety when modifying the active game codes set
                {
                    activeGameCodes.Add(gameCode);
                    SaveActiveGameCodes();
                    Console.WriteLine($"Game code {gameCode} added.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding game code {gameCode}: {ex.Message}");
            }
        }
        public static GameServer JoinGame(string gameCode, string playerName)
        {
            LoadActiveGameCodes();
            LoadActiveServers();
            lock (activeServers)
            {
                Console.WriteLine($"Active servers count: {activeServers.Count}");
                if (activeServers.TryGetValue(gameCode, out GameServer server))
                {
                    Console.WriteLine($"Joining existing game server with game code: {gameCode}");
                   
                    return server;
                }
                else
                {
                    Console.WriteLine($"No active server found for game code: {gameCode}");
                    return null;
                }
            }
        }
        public static void SaveActiveServers()
        {
            var serverStates = activeServers.Values.Select(server => new GameServerState
            {
                GameCode = server.gameCode,
                HighScores = server.GetHighScores(),
                PlayerCount = server.playerCount
            }).ToList();

            string json = JsonConvert.SerializeObject(serverStates);
            File.WriteAllText("active_servers.json", json);
            Console.WriteLine("Active servers saved successfully.");
        }
        public static void LoadActiveServers()
        {
            if (File.Exists("active_servers.json"))
            {
                string json = File.ReadAllText("active_servers.json");
                var serverStates = JsonConvert.DeserializeObject<List<GameServerState>>(json);

                lock (activeServers)
                {
                    foreach (var state in serverStates)
                    {
                        GameServer server = new GameServer(state.GameCode);
                        server.highScores = state.HighScores; // Restore high scores
                        server.playerCount = state.PlayerCount; // Restore player count
                        activeServers[state.GameCode] = server; // Add to active servers
                        Console.WriteLine($"Loaded active server for game code: {state.GameCode}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No active servers file found.");
            }
        }
        public static void PopulateActiveServers()
        {
            // Load active game codes
            var loadedGameCodes = LoadActiveGameCodes();

            lock (activeServers) // Locking for thread safety
            {
                foreach (var gameCode in loadedGameCodes)
                {
                    // Check if a server instance exists for this game code
                    if (IsGameCodeActive(gameCode))
                    {
                        // Create a new instance of GameServer for this game code
                        GameServer server = new GameServer(gameCode);
                        activeServers[gameCode] = server; // Add to active servers
                        Console.WriteLine($"Loaded active server for game code: {gameCode}");
                    }
                }
            }
        }
        // Remove a game code from the active set and save it
        public static void RemoveActiveGameCode(string gameCode)
        {
            try
            {
                lock (fileLock)  // Ensure thread safety when modifying the active game codes set
                {
                    if (activeGameCodes.Contains(gameCode))
                    {
                        activeGameCodes.Remove(gameCode);
                        SaveActiveGameCodes();
                        Console.WriteLine($"Game code {gameCode} removed.");
                    }
                    else
                    {
                        Console.WriteLine($"Game code {gameCode} not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing game code {gameCode}: {ex.Message}");
            }
        }

        public class GameServerState
        {
            public string GameCode { get; set; }
            public Dictionary<string, int> HighScores { get; set; }
            public int PlayerCount { get; set; }

            // Add any other properties that represent the state of your server
        }
    }
}