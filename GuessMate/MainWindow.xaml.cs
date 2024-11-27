using System;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.IO;

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

        public MainWindow()
        {
            InitializeComponent();
            BackgroundMusic.Play();
            MusicState.IsMusicOn = true;
        }

        public static class MusicState
        {
            public static bool IsMusicOn { get; set; }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_maxPlayers == 1)
                {
                    _gameSession = new GameSession(GameMode.SinglePlayer);
                    gameServer = new GameServer("SinglePlayer");
                    gameServer.StartServer();

                    MessageBox.Show("Game started in single-player mode.");
                    BackgroundMusic.Stop();

                    gameLobby = new GameLobby(_gameSession, _maxPlayers, true, null); // No client needed for single-player
                    gameLobby.Show();
                    this.Hide();
                }
                else
                {
                    gameCode = GenerateRandomCode();
                    _gameSession = new GameSession(GameMode.MultiPlayer);
                    gameServer = new GameServer(gameCode);
                    gameServer.StartServer();

                    MessageBox.Show($"Game started with code: {gameCode}. Share this code with your friends.");
                    BackgroundMusic.Stop();

                    gameLobby = new GameLobby(_gameSession, _maxPlayers, true, null); // Pass null for now
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

            if (result == true && !string.IsNullOrEmpty(inputDialog.GameCode) && _maxPlayers > 1)
            {
                string gameCode = inputDialog.GameCode;

                // Create the game client and connect to the server
                gameClient = new GameClient();
                gameClient.ConnectToServer(gameCode); // Connect to the server using the game code

                if (gameClient.IsConnected) // Check if the connection was successful
                {
                    _gameSession = new GameSession(GameMode.MultiPlayer); // Set to MultiPlayer mode

                    // Pass the client to the GameLobby
                    gameLobby = new GameLobby(_gameSession, _maxPlayers, false, gameClient); // Pass the client
                    gameLobby.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Failed to connect to the server. Please check the game code and try again.");
                }
            }
            else if (_maxPlayers == 1)
            {
                MessageBox.Show("Single-player mode does not require a game code. Please start a game instead.");
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
