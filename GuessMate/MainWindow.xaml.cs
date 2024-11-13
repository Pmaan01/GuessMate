using System;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.IO;


namespace GuessMate
{
    public partial class MainWindow : Window
    {
        private GameServer gameServer;
        private GameClient gameClient;
        public static int _maxPlayers;
        private GameSession _gameSession; 
        private ImageLibrary _library;
        public static string gameCode;
        GameLobby gameLobby;


        public MainWindow()
        {
            InitializeComponent();
            BackgroundMusic.Play();
        }
        public static class MusicState
        {
            public static bool IsMusicOn { get; set; } = true; // Default music state is on
        }
     

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_maxPlayers == 1)
            {
                _gameSession = new GameSession(GameMode.SinglePlayer); // For SinglePlayer mode
                gameServer = new GameServer("SinglePlayer");
                gameServer.StartServer();

                MessageBox.Show("Game started in single-player mode.");
                BackgroundMusic.Stop();

                // Open the GameLobby directly for single-player mode
                gameLobby = new GameLobby(_gameSession, _maxPlayers);
                gameLobby.Show();
                this.Hide();
            }
            else
            {
                // Generate a game code for multiplayer and display it
                gameCode = GenerateRandomCode();
                _gameSession = new GameSession(GameMode.MultiPlayer); // Set the mode to MultiPlayer
                gameServer = new GameServer(gameCode);
                gameServer.StartServer();

                // Show the generated game code to the user for sharing
                MessageBox.Show($"Game started with code: {gameCode}. Share this code with your friends.");
                BackgroundMusic.Stop();
                // Open the GameLobby window
                gameLobby = new GameLobby(_gameSession, _maxPlayers);
                gameLobby.Show();
                this.Hide();
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
                gameClient.ConnectToServer(gameCode);
                gameClient.SendMessageToServer("StartGame");

                _gameSession = new GameSession(GameMode.MultiPlayer); // Set to MultiPlayer mode


                // Instantiate the GameLobby with paths and player count
                gameLobby = new GameLobby(_gameSession, _maxPlayers);
                gameLobby.Show();
                gameLobby.Show();
                this.Hide();
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
            // When the window is loaded, check the global music state and update accordingly
            if (MusicState.IsMusicOn)
            {
                BackgroundMusic.Stop();
                Music.Content = "Turn On Music";  // Ensure button text reflects the correct state
            }
            else
            {
                BackgroundMusic.Play();
                Music.Content = "Turn Off Music";  // Ensure button text reflects the correct state
            }
        }

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the music is currently playing
            if (Music.Content.ToString() == "Turn Off Music")
            {
                // Stop the background music in the current window
                BackgroundMusic.Stop();
                MusicState.IsMusicOn = false; // Set music state to off
                Music.Content = "Turn On Music"; // Change the button text
            }
            else if (Music.Content.ToString() == "Turn On Music")
            {
                // Play the background music in the current window
                BackgroundMusic.Play();
                MusicState.IsMusicOn = true; // Set music state to on
                Music.Content = "Turn Off Music"; // Change the button text
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
