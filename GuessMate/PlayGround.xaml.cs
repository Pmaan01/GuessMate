using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GuessMate
{
    public partial class PlayGround : Window
    {
        private List<Player> players;  // List of players
        private int currentPlayerIndex = 0; // Current player's turn
        private List<ImageData> playerImages; // The images of the current player
        private DispatcherTimer timer;
        private int timeLeft = 10;  // 10 seconds timer
        private bool imagesShown = false;
        private GameSession gameSession;

        public PlayGround(GameSession gameSession)
        {
            InitializeComponent();
            players = GameSession.Players;
            this.gameSession = gameSession;

            if (players == null || players.Count == 0)
            {
                MessageBox.Show("No players available.");
                return;
            }

            this.playerImages = new List<ImageData>();
            SetupTimer();
            ShowCurrentPlayer();
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
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
                if (!imagesShown)
                {
                    ShuffleImagesAndDisplayHint();
                }
            }
        }

        private void ShowCurrentPlayer()
        {
            // Check if the players list is not empty and currentPlayerIndex is valid
            if (players == null || players.Count == 0)
            {
                MessageBox.Show("No players available.");
                return;
            }

            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            {
                MessageBox.Show("Invalid player index.");
                return;
            }

            Player currentPlayer = players[currentPlayerIndex];
            PlayersList.Items.Clear();
            PlayersList.Items.Add($"{currentPlayer.Name} - Score: {currentPlayer.Score}");  // Display player's name and score

            if (currentPlayer.Pictures != null && currentPlayer.Pictures.Count > 0)
            {
                playerImages = currentPlayer.Pictures;  // Get the uploaded images for this player
                for (int i = 0; i < playerImages.Count; i++)
                {
                    StackPanel imagePanel = new StackPanel();

                    // Create a BitmapImage from the ImagePath
                    BitmapImage bitmapImage = new BitmapImage(new Uri(playerImages[i].ImagePath, UriKind.RelativeOrAbsolute));

                    // Create an Image control and set its source
                    Image img = new Image
                    {
                        Source = bitmapImage,
                        Width = 100,  // Set the size as needed
                        Height = 100  // Set the size as needed
                    };

                    // Create a TextBlock for the image name
                    TextBlock name = new TextBlock { Text = playerImages[i].ImageName, HorizontalAlignment = HorizontalAlignment.Center };

                    // Add the Image and TextBlock to the StackPanel
                    imagePanel.Children.Add(img);
                    imagePanel.Children.Add(name);

                    // Add the StackPanel to the PlayersList (ListBox)
                    PlayersList.Items.Add(imagePanel);
                }
            }
            else
            {
                // Handle the case where the current player has no images uploaded
                MessageBox.Show("No images uploaded for this player.");
            }

            // Start the timer to display images for 10 seconds
            timeLeft = 10;
            timer.Start();
        }

        private void ShuffleImagesAndDisplayHint()
        {
            imagesShown = true;

            // Shuffle the images and hide them
            Random rand = new Random();
            var shuffledImages = playerImages.OrderBy(x => rand.Next()).ToList();

            // Display shuffled images with hints
            HintTextBlock.Text = "Hint: " + shuffledImages[0].Hint;  // Show a hint related to the first image

            // Display the RadioButton options for guessing
            AnswerOptions.Visibility = Visibility.Visible;
            Option1.Content = shuffledImages[0].ImageName;
            Option2.Content = shuffledImages[1].ImageName;
            Option3.Content = shuffledImages[2].ImageName;
            Option4.Content = shuffledImages[3].ImageName;
            Option5.Content = shuffledImages[4].ImageName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // When the window is loaded, check the global music state and update accordingly
            if (!MainWindow.MusicState.IsMusicOn)
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
            // Toggle the music state based on the current button text
            if (Music.Content.ToString() == "Turn Off Music")
            {
                // Stop the background music and update the music state
                BackgroundMusic.Stop();
                MainWindow.MusicState.IsMusicOn = false;  // Update global state
                Music.Content = "Turn On Music"; // Update button text
            }
            else if (Music.Content.ToString() == "Turn On Music")
            {
                // Play the background music and update the music state
                BackgroundMusic.Play();
                MainWindow.MusicState.IsMusicOn = true;  // Set global music state to on
                Music.Content = "Turn Off Music"; // Update button text
            }
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            RadioButton clickedButton = sender as RadioButton;
            string selectedAnswer = clickedButton.Content.ToString();

            // Compare the selected answer with the correct image's name
            if (selectedAnswer == playerImages[0].ImageName)
            {
                players[currentPlayerIndex].Score += 1;  // Increment score for correct guess
                MessageBox.Show("Correct guess!");
            }
            else
            {
                MessageBox.Show("Incorrect guess. Try again!");
            }

            // Move to next player turn
            NextPlayerTurn();
        }

        private void NextPlayerTurn()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            timeLeft = 10;  // Reset timer for next player
            timer.Start();  // Start the timer for the next player
            ShowCurrentPlayer();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            // Start the game logic, initialize turns, etc.
            MessageBox.Show("Game started!");
            ShowCurrentPlayer();
        }
    }
}
