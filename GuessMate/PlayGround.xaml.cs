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
    public partial class PlayGround : Window
    {
        private List<Player> players;
        private int currentPlayerIndex = 0;
        private List<ImageData> playerImages;
        private DispatcherTimer timer;
        private int timeLeft = 10;
        private bool imagesShown = false;
        private GameSession gameSession;
        private PlayerImageDatabaseHelper playerImageDatabaseHelper = new PlayerImageDatabaseHelper();
        private string currentImageNameToGuess;
        Player currentPlayer;
        Player oppositePlayer;
        private int currentRound = 0; // Track the current round
        private const int totalRounds = 5; // Total rounds to play

        public PlayGround(GameSession gameSession)
        {
            InitializeComponent();
            UpdateMusicState();
            this.gameSession = gameSession;
            players = GameSession.Players;

            if (players == null || players.Count == 0)
            {
                MessageBox.Show("No players available.");
                Close();
                return;
            }

            playerImages = new List<ImageData>();
            SetupTimer();
            ShowCurrentPlayer();
            this.Closing += PlayGround_Closing;
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

        private void PlayGround_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           

            foreach (var player in players)
            {
                playerImageDatabaseHelper.RemovePlayerImages(player.Name);
            }

            if (timer != null && timer.IsEnabled)
            {
                timer.Stop();
            }
        }

        private void SetOptionButtonsEnabled(bool isEnabled)
        {
            Option1.IsEnabled = isEnabled;
            Option2.IsEnabled = isEnabled;
            Option3.IsEnabled = isEnabled;
            Option4.IsEnabled = isEnabled;
            Option5.IsEnabled = isEnabled;
        }
        private void StartComputerTurn()
        {
            DispatcherTimer computerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4) // 5 seconds delay for PC
            };

            computerTimer.Tick += (sender, e) =>
            {
                computerTimer.Stop();
                ComputerPlayerTurn();
            };

            computerTimer.Start();
        }
        private void ComputerPlayerTurn()
        {
            // Create a new instance of Random with a time-based seed
            Random rand = new Random(DateTime.Now.Millisecond + currentPlayerIndex); // Use time and player index to seed

            // Create a list of possible answers (including the correct one)
            List<string> possibleAnswers = new List<string>
        {
            currentImageDataToGuess.ImageName, // Include the correct answer
            Option2.Content.ToString(),
            Option3.Content.ToString(),
            Option4.Content.ToString(),
            Option5.Content.ToString()
        };

            // Randomly select an answer from the possible answers
            int randomIndex = rand.Next(possibleAnswers.Count);
            string computerGuess = possibleAnswers[randomIndex];

            // Check if the guess is correct
            if (computerGuess == currentImageNameToGuess)
            {
                players[currentPlayerIndex].Score++;
                MessageBox.Show("Computer guessed correctly!");
            }
            else
            {
                MessageBox.Show("Computer guessed incorrectly.");
            }

            // Move to the next player's turn
            NextPlayerTurn();
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
                // Set the dimensions and stretching mode
                PictureDisplay.Width = 103;
                PictureDisplay.Height = 149;
                PictureDisplay.Stretch = Stretch.Fill; // Ensure the image fills the area
                if (isCorrectGuess)
                {
                    players[currentPlayerIndex].Score++;
                    MessageBox.Show("You guessed correctly!");
                }
                else
                {
                    MessageBox.Show("Incorrect guess. Try again!");
                }

                // Move to the next player's turn
                NextPlayerTurn();
            }
        }
        private ImageData currentImageDataToGuess; // Add this field to store the image data

        private void ShuffleImagesAndDisplayHint()
        {
            var shuffledImages = playerImages.OrderBy(x => Guid.NewGuid()).ToList();

            // Set the hint for the first image in the shuffled list
            if (shuffledImages.Count > 0)
            {
                // Set the current image name to guess
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
        private void FetchPCImages()
        {
            var images = ImageDatabaseHelper.GetImagesFromDatabase(GameLobby.category);
            if (images?.Count > 0)
            {
                LoadPlayerImages(images);
            }
            else
            {
                MessageBox.Show("No images available for the PC.");
            }
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
                    Width = 80, Height =20
                    
                };


                imagePanel.Children.Add(img);
                imagePanel.Children.Add(name);
                ImageArea.Children.Add(imagePanel);
            }
        }
        private void SetupTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Timer ticks every second
            };
            timer.Tick += Timer_Tick; // Subscribe to the Tick event
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
                TimerDisplay.Text = $"Time Left: {timeLeft--}s"; // Update the displayed time
            }
            else
            {
                timer.Stop(); // Stop the timer
                ImageArea.Visibility = Visibility.Hidden; // Hide all images

                // Notify the player that they missed their chance
                MessageBox.Show($"{currentPlayer.Name} missed their chance to guess!");

                // Move to the next player's turn
                NextPlayerTurn();
            }
        }

        private void NextPlayerTurn()
        {
            // Check if players list is empty
            if (players.Count == 0)
            {
                MessageBox.Show("No players available.");
                return;
            }

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
                    Round.Content = $"Round: {currentRound} out of {totalRounds}"; // Update the label
                    ChangeImageForNextRound(); // Proceed to the next round
                }
            }

            // Show the current player and start their turn
            ShowCurrentPlayer();
        }
        private void ShowAllImagesForGuessing(Player targetPlayer)
        {
            // Unsubscribe from any existing tick event to avoid multiple subscriptions
            timer.Tick -= Timer_Tick; // Unsubscribe if already subscribed

            List<ImageData> images;

            // Check if the target player is "PC"
            if (targetPlayer.Name == "PC")
            {
                images = ImageDatabaseHelper.GetImagesFromDatabase(GameLobby.category);
                if (images == null || images.Count == 0)
                {
                    MessageBox.Show("No images found for the PC.");
                    return;
                }
            }
            else
            {
                images = playerImageDatabaseHelper.GetPlayerImages(targetPlayer.Name);
                if (images == null || images.Count == 0)
                {
                    MessageBox.Show($"No images found for {targetPlayer.Name}.");
                    return;
                }
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

            // Set up the timer tick event to handle time expiration
            timer.Tick += (sender, e) =>
            {
                if (timeLeft <= 0)
                {
                    timer.Stop();
                    ImageArea.Visibility = Visibility.Hidden; // Hide all images
                    NextPlayerTurn(); // Move to the next player's turn
                }
            };
        }
        private void ShowCurrentPlayer()
        {
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            {
                MessageBox.Show("Invalid player index.");
                return;
            }

            currentPlayer = players[currentPlayerIndex];
            Turn.Content = $"Current Player: {currentPlayer.Name}";

            // Calculate the opposite player index
            int oppositePlayerIndex = (currentPlayerIndex + (players.Count / 2)) % players.Count;
            oppositePlayer = players[oppositePlayerIndex];

            PlayersList.Items.Clear();
            PlayersList.Items.Add($"{currentPlayer.Name} - Score: {currentPlayer.Score}");

            // Update the waiting message
            textBlockWaitingForPlayers.Text = currentPlayer.Name == "PC"
                ? "Wait for PC to guess..."
                : "Your turn to guess...";

            // Disable/enable options based on who's turn it is
            bool isPC = currentPlayer.Name == "PC"; // Check if the current player is PC
            SetOptionButtonsEnabled(!isPC); // Disable options for PC's turn

            if (isPC)
            {
                // If it's the PC's turn, fetch images for the opposite player and start its turn
                FetchMultiplayerImages(oppositePlayer); // Fetch images for the human player
                ShowAllImagesForGuessing(oppositePlayer); // Show images for the human player
                StartComputerTurn(); // Start the computer's guessing turn
            }
            else
            {
                // If it's a human player's turn, fetch images for the opposite player
                FetchPCImages(); // Fetch images for the PC
                ShowAllImagesForGuessing(oppositePlayer); // Show images for the PC
            }

            StartTimer();
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
    }
}