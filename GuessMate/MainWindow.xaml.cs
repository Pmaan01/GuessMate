using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace GuessMate
{
    public partial class MainWindow : Window
    {
        public static GameServer gameServer;
        private GameClient gameClient;
        public static int _maxPlayers;
        private GameSession _gameSession;
        public static string gameCode;
        GameLobby gameLobby;
        private Thread serverThread;

        // Use constructor dependency injection
        private readonly IGameServerFactory _serverFactory;

        // Constructor accepting IGameServerFactory via DI
        public IGameServerFactory ServerFactory { get; set; }

        public MainWindow(IGameServerFactory serverFactory)
        {
            InitializeComponent();
            _serverFactory = serverFactory;

            // Initialize other things like music and game state
            BackgroundMusic.Play();
            MusicState.IsMusicOn = true;
        }

        public static class MusicState
        {
            public static bool IsMusicOn { get; set; }
        }

        // Start the server on a separate thread
        public void StartServer(string gameCode)
        {
            serverThread = new Thread(() =>
            {
                gameServer = new GameServer(gameCode);
                gameServer.StartServer();
            });

            serverThread.IsBackground = true; // Ensure thread stops when app closes
            serverThread.Start();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_maxPlayers == 1)
                {
                    _gameSession = new GameSession(GameMode.SinglePlayer);
                    gameServer = (GameServer)_serverFactory.CreateServer("SinglePlayer");
                    gameServer.StartServer();

                    MessageBox.Show("Game started in single-player mode.");
                    BackgroundMusic.Stop();

                    gameLobby = new GameLobby(_gameSession, _maxPlayers, true, null, gameServer);
                    gameLobby.Show();
                    this.Hide();
                }
                else
                {
                    _gameSession = new GameSession(GameMode.MultiPlayer); // Initialize for multiplayer
                    gameCode = GenerateRandomCode();
                    gameServer = (GameServer)_serverFactory.CreateServer(gameCode);
                    gameServer.StartServer();

                    gameClient = new GameClient();
                    gameClient.ConnectToServer(gameCode);

                    MessageBox.Show($"Game started with code: {gameCode}. Share this code with your friends.");
                    BackgroundMusic.Stop();

                    gameLobby = new GameLobby(_gameSession, _maxPlayers, true, gameClient, gameServer);
                    gameLobby.Show();
                    this.Hide();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting the game: {ex.Message}");
            }
        }

        private void JoinGameButton_Click(object sender, RoutedEventArgs e)
        {
            CustomInputDialog inputDialog = new CustomInputDialog();
            bool? result = inputDialog.ShowDialog();
             _gameSession = new GameSession(GameMode.MultiPlayer);
            string enteredGameCode = inputDialog.InputTextBox.Text;

            // Attempt to connect using the entered game code
            gameServer = GameServer.Connect(enteredGameCode);
            if(gameServer == null)
            {
                 MessageBox.Show("Error: Game server is null.");
            }
           else
            {
                MessageBox.Show($"Successfully connected to game server with code {enteredGameCode}.");
            }
            if (gameServer != null)
            {
                try
                {
                    // If connection is successful, connect the client and proceed to the game lobby
                    gameClient = new GameClient();
                   
                if (_gameSession == null)
                    {
                        MessageBox.Show("Error: Game session is not initialized.");
                        return;
                    }

                    if (gameClient == null)
                    {
                        MessageBox.Show("Error: Game client is not initialized.");
                        return;
                    }

                    if (gameServer == null)
                    {
                        MessageBox.Show("Error: Game server is not initialized.");
                        return;
                    }
                    gameLobby = new GameLobby(_gameSession, _maxPlayers, false, gameClient, gameServer);
                    if (gameLobby == null)
                    {
                        MessageBox.Show("Error: Game lobby is null.");
                        return; // or handle this case appropriately
                    }
                    gameLobby.Show();
                    this.Hide();
                }
                catch (Exception ex)
                {
                       MessageBox.Show($"Exception: {ex.ToString()}");  // This will give more detailed stack trace

                    MessageBox.Show($"Failed to join game: {ex.Message}");
                }
            }
            else
            {
                // If no active game is found with the entered code, show an error
                MessageBox.Show("No active game found with this code. Please check the code and try again.");
            }

            if (_maxPlayers == 1)
            {
                MessageBox.Show("Single-player mode does not require a game code.");
            }
        }


        private string GenerateRandomCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // Generate a random 6-digit number
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check the music state on window load
            UpdateMusicState();
        }

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle music state on button click
            if (MusicState.IsMusicOn)
            {
                BackgroundMusic.Stop();
                MusicState.IsMusicOn = false;
                Music.Content = "Turn On Music";
            }
            else
            {
                BackgroundMusic.Play();
                MusicState.IsMusicOn = true;
                Music.Content = "Turn Off Music";
            }

            // Notify other windows about the change
            if (gameLobby != null)
            {
                gameLobby.UpdateMusicState();
            }
        }

        private void UpdateMusicState()
        {
            if (MusicState.IsMusicOn)
            {
                BackgroundMusic.Play();
                Music.Content = "Turn Off Music";
            }
            else
            {
                BackgroundMusic.Stop();
                Music.Content = "Turn On Music";
            }
        }

        private void PlayerCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected player count from the ComboBox
            if (PlayerCountComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedCount = selectedItem.Content.ToString();

                // Based on the selected count, adjust the game setup
                switch (selectedCount)
                {
                    case "1 Player":
                        _maxPlayers = 1;
                        break;

                    case "2 Players":
                        _maxPlayers = 2;
                        break;

                    case "3 Players":
                        _maxPlayers = 3;
                        break;

                    case "4 Players":
                        _maxPlayers = 4;
                        break;

                    default:
                        _maxPlayers = 1; // Default to 1 player
                        break;
                }
            }
        }

    }

}
