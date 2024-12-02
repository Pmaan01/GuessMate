using GuessMate;
using static GuessMate.MainWindow;
using System.Windows.Controls;
using System.Windows;
using ServerSide;
namespace GuessMate
{
    public partial class GameLobby : Window
    {
        private readonly GameSession _gameSession;
        private readonly bool _isHost;
        public static int _playerCount;
        public static string playerName;
        public static string category;

        private GameClient _gameClient;
        private ServerSide.GameServer _gameServer; // Add this line

        public GameLobby(GameSession gameSession, int maxPlayers, bool isHost, GameClient gameClient, GameServer gameServer )
        {
            InitializeComponent();
            UpdateMusicState();
            _gameServer = gameServer;
            _gameClient = gameClient; // Assign the GameClient instance
            _gameSession = gameSession;
            _playerCount = maxPlayers;
            _isHost = isHost; // Set the host status

            if (_gameServer != null)
            {
                _gameServer.gameCode = Code.Text; // Set the game code
                UpdateCodeTextBlock(); // Update the TextBlock content
            }
            else
            {
                MessageBox.Show("Game server is not initialized.");
            }

            // If playing with PC, add PC as the second player
            if (_playerCount == 1)
            {
                _gameSession.AddPlayer(new Player("PC"));
                _playerCount++;
                Code.Visibility = Visibility.Hidden;
            }
            else
            {
                // Add other players to the session
                for (int i = 2; i <= MainWindow._maxPlayers; i++)
                {
                    gameSession.AddPlayer(new Player($"Player {i}"));
                    _playerCount++;
                }
                Code.Visibility = Visibility.Visible;
                Code.Text = $"Code: {MainWindow.gameCode}";
            }

            // Show or hide the GameModeComboBox based on host status
            ThemeButton.Visibility = _isHost ? Visibility.Visible : Visibility.Hidden;
            GameModeComboBox.Visibility = _isHost ? Visibility.Visible : Visibility.Hidden;
            GameModeComboBox.IsEnabled = _isHost; // Disable if not host

            // If not the host, hide the theme selection button and disable the GameModeComboBox
            if (!_isHost)
            {
                ThemeButton.Visibility = Visibility.Hidden;
                GameModeComboBox.IsEnabled = false;
            }
        }

        private void UpdateCodeTextBlock()
        {
            // Ensure _gameServer is not null before accessing its properties
            if (_gameServer != null)
            {
                Code.Text = $"Code: {_gameServer.gameCode}\nTheme: {_gameServer.SelectedTheme}";
            }
            else
            {
                Code.Text = "Game server is not available.";
            }
        }
        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isHost) // Only allow the host to select the theme
            {
                if (GameModeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a valid theme.");
                    return;
                }

                if (GameModeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    category = selectedItem.Content.ToString();
                    _gameServer.SetTheme(category); // Set the theme on the server
                    UpdateCodeTextBlock();

                    // Notify other players about the selected theme
                    MessageBox.Show($"Theme '{category}' selected by the host.");
                }
                else
                {
                    category = "Random";
                    _gameSession.SelectedTheme = category; // Store the random selection
                    _gameServer.SetTheme(category);
                    UpdateCodeTextBlock();

                    // Notify other players about the selected theme
                    MessageBox.Show($"Theme '{category}' selected by the host.");
                }
            }
            else
            {
                // Retrieve the currently selected theme set by the host
                string currentTheme = _gameServer.SelectedTheme ?? "No theme selected"; // Use the server's selected theme
                MessageBox.Show($"The selected theme by host is: {currentTheme}");
            }

            StartGame(category);
        }

        private void StartGame(string category)
        {
            try
            {
                // If playing single-player game
                if (_playerCount == 2 && GameSession.Players.Any(p => p.Name == "PC"))
                {
                    MessageBox.Show($"Starting SinglePlayer Game. Category selected: {category}...");
                }
                else
                {
                    MessageBox.Show($"Starting Multiplayer Game. Category selected: {category}");
                }

                playerName = PlayerNameTextBox.Text;

                // Add the human player (yourself) to the session
                _gameSession.AddPlayer(new Player(playerName));

                // Set the theme on the server if the host
                if (_isHost)
                {
                    _gameServer.SetTheme(category);
                }
                _gameServer.StoreHighScore(playerName, 0); 

                PlayerTurn playerTurnWindow = new PlayerTurn(_gameSession, _gameClient, _gameServer);
                playerTurnWindow.Show();

                // Close the current lobby window
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            GameModeComboBox.Visibility = GameModeComboBox.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (Music.Content.ToString() == "Turn Off Music")
            {
                BackgroundMusic.Stop();
                MusicState.IsMusicOn = false;
                Music.Content = "Turn On Music"; // Change the button text
            }
            else if (Music.Content.ToString() == "Turn On Music")
            {
                BackgroundMusic.Play();
                MusicState.IsMusicOn = true;
                Music.Content = "Turn Off Music";
            }
        }

        public void UpdateMusicState()
        {
            if (MainWindow.MusicState.IsMusicOn)
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

        private void BackgroundMusic_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Error playing music.");
        }



    }
} 