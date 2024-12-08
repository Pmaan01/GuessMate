using ServerSide;
using System;
using System.Collections.Generic;
using System.Windows;
using static GuessMate.MainWindow;

namespace GuessMate
{
    public partial class FinalScores : Window
    {
        private List<Player> players;
        private PlayerImageDatabaseHelper playerImageDatabaseHelper = new PlayerImageDatabaseHelper();
        private GameClient _gameClient; // Reference to GameClient instance

        private string _currentPlayerName; // Store the current player's name

        public FinalScores(GameSession gameSession, GameClient gameClient, string currentPlayerName)
        {
            InitializeComponent();
            UpdateMusicState();
            players = GameSession.Players;

            _gameClient = gameClient; // Correctly assign the GameClient instance
            _currentPlayerName = currentPlayerName; // Store the current player's name
            // Variable to track the highest score and the corresponding players
            int highestScore = int.MinValue;
            List<string> winners = new List<string>(); // List to hold names of players with the highest score

            // Populate the FinalScoresList and determine the winners
            foreach (var player in GameSession.Players) // Use the passed gameSession
            {
                FinalScoresList.Items.Add($"{player.Name}: {player.Score}");

                // Check for the highest score
                if (player.Score > highestScore)
                {
                    highestScore = player.Score;
                    winners.Clear(); // Clear previous winners
                    winners.Add(player.Name); // Add new winner
                }
                else if (player.Score == highestScore)
                {
                    winners.Add(player.Name); // Add to winners if tied
                }
            }

            // Display the winner message
            if (winners.Count > 0)
            {
                if (winners.Count == 1)
                {
                    FinalScoresList.Items.Add($"Congratulations {winners[0]}, you are the Winner!");
                }
                else
                {
                    FinalScoresList.Items.Add("It's a tie between: " + string.Join(", ", winners));
                }
            }

            // Display consolation message for other players
            foreach (var player in GameSession.Players)
            {
                if (!winners.Contains(player.Name))
                {
                    FinalScoresList.Items.Add($"Better luck next time, {player.Name}!");
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Close the application
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

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var player in players)
            {
                playerImageDatabaseHelper.RemovePlayerImages(player.Name);
            }

            // Create a new instance of MainWindow
            MainWindow mainWindow = new MainWindow(new GameServerFactory()); // Pass the factory or appropriate parameters

            // Show the MainWindow
            mainWindow.Show();


            // Close the FinalScores window
            this.Close();
        }
        private async void ShowHighScores()
        {
            // Request high scores from the server
            var highScores = await _gameClient.RequestHighScoresAsync();
            
            // Display only the current player's high score
            string scoreList = $"High Score for {_currentPlayerName}:\n";

            if (highScores.TryGetValue(_currentPlayerName, out int playerScore))
            {
                scoreList += $"{_currentPlayerName}: {playerScore}\n";
            }
            else
            {
                scoreList += $"{_currentPlayerName}: No score available.\n";
            }

            MessageBox.Show(scoreList);
        }

        private void ViewHighScoresButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHighScores(); // Call the method to show high scores
        }
    }
}