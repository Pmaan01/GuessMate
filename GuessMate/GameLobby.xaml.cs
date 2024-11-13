using static GuessMate.MainWindow;
using System.Windows.Controls;
using System.Windows;

namespace GuessMate
{
    public partial class GameLobby : Window
    {
        private readonly GameSession _gameSession;
        public static int _playerCount;
        public static string playerName;
        string category;

        public GameLobby(GameSession gameSession, int playerCount)
        {
            InitializeComponent();
            BackgroundMusic.Play();

            _gameSession = gameSession;
            _playerCount = MainWindow._maxPlayers;

            

            // If playing with PC, add PC as the second player
            if (_maxPlayers == 1)
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
                    _gameSession.AddPlayer(new Player($"Player {i}"));
                    _playerCount++;
                }
                Code.Visibility = Visibility.Visible;
                Code.Text = $"Code: {MainWindow.gameCode}";
            }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (GameModeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid theme.");

                return;
            }

            if (GameModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                category = selectedItem.Content.ToString();
            }
            else
            {
                category = "Random";  // Correct assignment without using `as string`
            }

            StartGame(category);
        }

        private void StartGame(string category)
        {
            try
            {
                // If playing single-player game
                if (_playerCount == 1)
                {
                    MessageBox.Show($"Starting SinglePlayer Game. Category selected: {category}...");
                }
                else
                {
                    MessageBox.Show($"Starting Multiplayer Game. Category selected: {category}");
                    playerName = PlayerNameTextBox.Text;

                    // Add the human player (yourself) to the session
                    _gameSession.AddPlayer(new Player(playerName));
                    PlayerTurn playerTurnWindow = new PlayerTurn(_gameSession);
                    playerTurnWindow.Show();
                    this.Close();
                }
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!MusicState.IsMusicOn)
            {
                BackgroundMusic.Stop();
                Music.Content = "Turn On Music";
            }
            else
            {
                BackgroundMusic.Play();
                Music.Content = "Turn Off Music";
            }
        }

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (Music.Content.ToString() == "Turn Off Music")
            {
                BackgroundMusic.Stop();
            }
            else if (Music.Content.ToString() == "Turn On Music")
            {
                BackgroundMusic.Play();
                MusicState.IsMusicOn = true;
                Music.Content = "Turn Off Music";
            }
        }

        private void BackgroundMusic_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Error playing music.");
        }
    }
}
