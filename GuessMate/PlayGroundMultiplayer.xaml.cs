using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static GuessMate.MainWindow;

namespace GuessMate
{
    public partial class PlayGroundMultiplayer : Window
    {
        private List<Player> players;
        private int currentPlayerIndex = 0;
        private List<ImageData> playerImages;
        private DispatcherTimer timer;
        private int timeLeft = 10;
        private GameSession gameSession;
        private PlayerImageDatabaseHelper playerImageDatabaseHelper = new PlayerImageDatabaseHelper();
        private ImageData currentImageDataToGuess;
        private string currentImageNameToGuess;
        private int currentRound = 0; // Track the current round
        private const int totalRounds = 5; // Total rounds to play
        private GameServer gameServer;
        private int totalPlayers; // Total number of players expected
        private int currentPlayers; // Current number of players who have joined

        public PlayGroundMultiplayer(GameSession gameSession, GameServer server)
        {
            InitializeComponent();
            UpdateMusicState();
            this.gameSession = gameSession;
            this.gameServer = server;
            totalPlayers = GameSession.Players.Count; // Set total players based on game session
            currentPlayers = 0; // Initialize current players to 0
            // Subscribe to the event
            this.gameServer.OnAllPlayersConnected += StartGame;

            players = GameSession.Players;

            if (players == null || players.Count == 0)
            {
                MessageBox.Show("No players available.");
                Close();
                return;
            }
              // Optionally, subscribe to an event from the GameServer to handle player connections
            gameServer.OnPlayerConnected += HandlePlayerConnected;
            UpdateWaitingLabel();
            playerImages = new List<ImageData>();
            SetupTimer();
            // Do not call ShowCurrentPlayer() here; wait for the server signal
            this.Closing += PlayGround_Closing;
        }
        private void StartGame()
        {
            // This method will be called when all players are connected
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("All players have joined! The game is starting.");
                ShowCurrentPlayer(); // Start the game by showing the current player
            });
        }
        private void EndGame()
        {
            // Close the current game window
            this.Close();
            timer.Stop(); // Stop the timer
            string finalScores = "Final Scores:\n";
            foreach (var player in players)
            {
                finalScores += $"{player.Name}: {player.Score}\n";
            }

            // Create an instance of the FinalScores window
            FinalScores finalScoresWindow = new FinalScores(gameSession);

            // Show the FinalScores window
            finalScoresWindow.Show();
        }
        private void HandlePlayerConnected()
        {
            currentPlayers++; // Increment the count of current players
            UpdateWaitingLabel(); // Update the label visibility
        }

        private void UpdateWaitingLabel()
        {
            // Show the label if not all players have joined
            if (currentPlayers < totalPlayers)
            {
                PlayerWait.Visibility = Visibility.Visible; // Show waiting label
            }
            else
            {
                PlayerWait.Visibility = Visibility.Collapsed; // Hide waiting label
            }
        }

        private void PlayGround_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cleanup player images if necessary
            foreach (var player in players)
            {
                playerImageDatabaseHelper.RemovePlayerImages(player.Name);
            }

            if (timer != null && timer.IsEnabled)
            {
                timer.Stop();
            }
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        private void StartTimer()
        {
            timeLeft = 5; // Reset timer for each turn to 5 seconds
            TimerDisplay.Text = $"Time Left: {timeLeft}s"; // Show initial time on the UI
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (timeLeft > 0)
            {
                TimerDisplay.Text = $"Time Left: {timeLeft--}s";
            }
            else
            {
                timer.Stop();
                ImageArea.Visibility = Visibility.Hidden;

                // Notify the player that they missed their chance
                MessageBox.Show($"{players[currentPlayerIndex].Name} missed their chance to guess!");

                // Move to the next player's turn
                NextPlayerTurn();
            }
        }

        private void NextPlayerTurn()
        {
            // Increment the current player index
            currentPlayerIndex++;

            // Check if the index exceeds the number of players
            if (currentPlayerIndex >= players.Count)
            {
                currentPlayerIndex = 0; // Reset to the first player
                currentRound++; // Increment the round after all players have played

                if (currentRound >= totalRounds)
                {
                    EndGame(); // End the game after total rounds
                    return; // Exit the method after ending the game
                }
                else
                {
                    // Update the Round label to show the current round
                    Round.Content = $"Round: {currentRound + 1} out of {totalRounds}"; // Update the label
                    ChangeImageForNextRound(); // Proceed to the next round
                }
            }

            // Show the current player and start their turn
            ShowCurrentPlayer();
        }

        private void ShowCurrentPlayer()
        {
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            {
                MessageBox.Show("Invalid player index.");
                return;
            }

            Player currentPlayer = players[currentPlayerIndex];
            Turn.Content = $"Current Player: {currentPlayer.Name}";

            // Show the images for the current player
            FetchMultiplayerImages(currentPlayer);
            ShowAllImagesForGuessing(currentPlayer); // Show images for the current player
            StartTimer(); // Start the timer for guessing
        }
        private void FetchMultiplayerImages(Player currentPlayer)
        {
            var images = playerImageDatabaseHelper.GetPlayerImages(currentPlayer.Name);
            if (images == null || images.Count == 0)
            {
                MessageBox.Show("No images found for the current player.");
                return;
            }
            LoadPlayerImages(images);
        }
        private void ShowAllImagesForGuessing(Player targetPlayer)
        {
            List<ImageData> images = playerImageDatabaseHelper.GetPlayerImages(targetPlayer.Name);
            if (images == null || images.Count == 0)
            {
                MessageBox.Show($"No images found for {targetPlayer.Name}.");
                return;
            }

            // Clear previous images and load new ones
            playerImages.Clear();
            LoadPlayerImages(images);

            // Shuffle the images before displaying them
            ShuffleImagesAndDisplayHint();

            // Show dummy image initially
            PictureDisplay.Source = new BitmapImage(new Uri("C:\\Users\\gurwi\\Downloads\\a.jpg", UriKind.Absolute));

            // Start the timer for guessing
            timeLeft = 5;
            TimerDisplay.Text = "Time Left: 5s";
            StartTimer();
        }

        private void ShuffleImagesAndDisplayHint()
        {
            var shuffledImages = playerImages.OrderBy(x => Guid.NewGuid()).ToList();

            // Set the hint for the first image in the shuffled list
            if (shuffledImages.Count > 0)
            {
                currentImageDataToGuess = shuffledImages[0]; // Store the ImageData object
                currentImageNameToGuess = currentImageDataToGuess.ImageName; // Populate the variable here

                HintTextBlock.Text = "Hint: " + currentImageDataToGuess.Hint;
                AnswerOptions.Visibility = Visibility.Visible;

                // Set the content of the option buttons
                Option1.Content = $"{shuffledImages[0].ImageName}";
                Option1.Tag = shuffledImages[0];

                for (int i = 1; i < 5 && i < shuffledImages.Count; i++)
                {
                    if (i == 1) Option2.Content = $"{shuffledImages[i].ImageName}";
                    if (i == 2) Option3.Content = $"{shuffledImages[i].ImageName}";
                    if (i == 3) Option4.Content = $"{shuffledImages[i].ImageName}";
                    if (i == 4) Option5.Content = $"{shuffledImages[i].ImageName}";

                    // Set tags for each option
                    Option2.Tag = shuffledImages[i];
                    Option3.Tag = shuffledImages[i];
                    Option4.Tag = shuffledImages[i];
                    Option5.Tag = shuffledImages[i];
                }
            }
        }

        private void LoadPlayerImages(IEnumerable<ImageData> images)
        {
            playerImages.Clear();
            foreach (var image in images)
            {
                if (File.Exists(image.ImagePath))
                {
                    image.ImagePreview = new BitmapImage(new Uri(image.ImagePath, UriKind.Absolute));
                    playerImages.Add(image);
                }
                else
                {
                    MessageBox.Show($"Image not found: {image.ImagePath}");
                }
            }
            DisplayImages();
        }

        private void DisplayImages()
        {
            if (playerImages.Count == 0)
            {
                MessageBox.Show("No images to display.");
                return;
            }

            ImageArea.Children.Clear();

            foreach (var imageData in playerImages)
            {
                StackPanel imagePanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(imageData.ImagePath, UriKind.Absolute)),
                    Stretch = System.Windows.Media.Stretch.Fill,
                    ClipToBounds = true,
                    Margin = new System.Windows.Thickness(10, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MaxWidth = 107,
                    MaxHeight = 190
                };

                TextBlock name = new TextBlock
                {
                    Text = $"Name: {imageData.ImageName}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Colors.Beige),
                    FontSize = 16,
                    FontFamily = new FontFamily("Ink Free"),
                    Background = new SolidColorBrush(Colors.Black),
                    Width = 80,
                    Height = 20
                };

                imagePanel.Children.Add(img);
                imagePanel.Children.Add(name);
                ImageArea.Children.Add(imagePanel);
            }
        }

        private void ChangeImageForNextRound()
        {
            // Reset the images and hint for the next round
            ShuffleImagesAndDisplayHint();
            timeLeft = 5; // Reset timer for the next round
            TimerDisplay.Text = $"Time Left: {timeLeft}s";
            StartTimer();
        }

        private void UpdateMusicState()
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

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (Music.Content == "Turn Off Music")
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
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton clickedButton)
            {
                string selectedAnswer = clickedButton.Content.ToString().Trim(); // Trim whitespace
                string correctAnswer = currentImageNameToGuess.Trim(); // Trim whitespace

                // Debugging output
                Console.WriteLine($"Selected Answer: '{selectedAnswer}', Current Image Name to Guess: '{correctAnswer}'");

                // Compare the selected answer with the current image name to guess
                bool isCorrectGuess = string.Equals(selectedAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase);

                // Show the actual image that is supposed to be guessed
                PictureDisplay.Source = new BitmapImage(new Uri(currentImageDataToGuess.ImagePath, UriKind.Absolute));

                if (isCorrectGuess)
                {
                    players[currentPlayerIndex].Score++;
                    MessageBox.Show($"{players[currentPlayerIndex].Name} guessed correctly!");
                }
                else
                {
                    MessageBox.Show("Incorrect guess. Try again!");
                }

                // Move to the next player's turn
                NextPlayerTurn();
            }
        }
    }
}