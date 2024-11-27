using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using static GuessMate.MainWindow;

namespace GuessMate
{
    public partial class PlayerTurn : Window, INotifyPropertyChanged
    {
        private const int MaxImages = 5;
        private List<ImageUploadData> _imageUploadData;
        private readonly GameSession _gameSession;


        public event PropertyChangedEventHandler PropertyChanged;

        public List<ImageUploadData> ImageUploadData
        {
            get => _imageUploadData;
            set
            {
                _imageUploadData = value;
                OnPropertyChanged(nameof(ImageUploadData));
            }
        }

        public PlayerTurn(GameSession gameSession)
        {
            InitializeComponent();
            UpdateMusicState();

              _gameSession = gameSession;
            InitializeImageUploadData();
            DataContext = this; // Set DataContext for binding
        }

        private void InitializeImageUploadData()
        {
            ImageUploadData = new List<ImageUploadData>();
            for (int i = 0; i < MaxImages; i++)
            {
                ImageUploadData.Add(new ImageUploadData());
            }
        }

        private void UploadImagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.Tag is string tagString && int.TryParse(tagString, out int index))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Set the image path
                    ImageUploadData[index].ImageData.ImagePath = openFileDialog.FileName;

                    // Update the UI image control
                    var imageControl = (Image)FindName($"Image{index + 1}");
                    if (imageControl != null)
                    {
                        var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                        imageControl.Source = bitmap;
                        ImageUploadData[index].ImagePreview = bitmap;
                    }
                }
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            bool allImagesValid = true;

            // Loop through all images to validate name, hint, and image data
            foreach (var data in ImageUploadData)
            {
                var nameTextBox = (TextBlock)FindName($"ImageName{ImageUploadData.IndexOf(data) + 1}");
                var hintTextBox = (TextBox)FindName($"ImageHint{ImageUploadData.IndexOf(data) + 1}");

                // Validate name and hint here
                if (nameTextBox != null)
                {
                    data.ImageData.ImageName = nameTextBox.Text.Trim(); // Trim whitespace
                }

                if (hintTextBox != null)
                {
                    data.ImageData.Hint = hintTextBox.Text.Trim(); // Trim whitespace
                }

                // Check if all fields (name, hint, and image) are filled out
                if (string.IsNullOrWhiteSpace(data.ImageData.ImageName) ||
                    string.IsNullOrWhiteSpace(data.ImageData.Hint) ||
                    data.ImagePreview == null)
                {
                    data.IsValid = false; // Mark as invalid
                    allImagesValid = false;
                }
                else
                {
                    data.IsValid = true; // Mark as valid
                }
            }

            if (allImagesValid)
            {
                MessageBox.Show("All images and hints submitted successfully!");

                // Collect valid images and hints for the game session
                List<ImageData> images = new List<ImageData>();

                foreach (var data in ImageUploadData)
                {
                    if (data.IsValid)
                    {
                        // Convert the image to a byte array
                        byte[] imageBytes = ConvertImageToByteArray(data.ImagePreview);

                        // Add the image data to the list (including ImagePath)
                        images.Add(new ImageData(data.ImageData.ImagePath, data.ImageData.Hint, data.ImageData.ImageName));

                        // Save image to database using PlayerImageDatabaseHelper
                        PlayerImageDatabaseHelper databaseHelper = new PlayerImageDatabaseHelper();
                        databaseHelper.SavePlayerImage(
                            GameLobby.playerName,
                            data.ImageData.ImageName,
                            data.ImageData.Hint,
                            imageBytes,
                            data.ImageData.ImagePath // Save the ImagePath as well
                        );
                    }
                }

                // Set the round details
                int roundNumber = 1;

                // Check if the game has a PC player (e.g., "PC" as player name)
                bool isSinglePlayer = GameSession.Players.Any(player => player.Name == "PC");

                if (isSinglePlayer)
                {
                    // Open the PlaygroundSinglePlayer window
                    OpenPlayGroundWindow();
                }
                else
                {
                    // Open the PlaygroundMultiplayer window
                    OpenPlayGroundMultiplayerWindow();
                }
            }
            else
            {
                MessageBox.Show("Please fill in the name, hint, and upload an image for all images.");
            }
        }

        private void OpenPlayGroundWindow()
        {
            // Create an instance of the Playground window for single-player (against PC)
            PlayGround playGroundWindow = new PlayGround(_gameSession);

            // Show the PlayGround window
            playGroundWindow.Show();

            // Close the current window (PlayerTurn) if it's no longer needed
            this.Close();
        }
        private void OpenPlayGroundMultiplayerWindow()
        {
            // Ensure that MainWindow.gameServer is not null before creating the instance
            if (MainWindow.gameServer == null)
            {
                MessageBox.Show("Game server is not initialized.");
                return;
            }

            // Create an instance of the PlaygroundMultiplayer window for multiplayer
            PlayGroundMultiplayer playGroundMultiplayerWindow = new PlayGroundMultiplayer(_gameSession, MainWindow.gameServer);

            // Show the PlaygroundMultiplayer window
            playGroundMultiplayerWindow.Show();

            // Close the current window (PlayerTurn) if it's no longer needed
            this.Close();
        }



        private byte[] ConvertImageToByteArray(BitmapImage bitmapImage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert the BitmapImage to a byte array.
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private void BackgroundMusic_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Error: " + e.ErrorException.Message);
        }

        private void ToggleMusicButton_Click(object sender, RoutedEventArgs e)
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


        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
