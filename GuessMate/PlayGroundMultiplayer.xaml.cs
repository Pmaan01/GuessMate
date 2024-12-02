using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // For file dialog
using ServerSide;

namespace GuessMate
{
    public partial class PlayGroundMultiplayer : Window
    {
        private string gameCode;
        private string selectedTheme;
        private List<string> playerImages; // Store the paths of images uploaded by the player
        private GameClient _gameClient;
        private GameServer _gameServer;

        public PlayGroundMultiplayer(string gameCode, GameClient gameClient, GameServer gameServer)
        {
            InitializeComponent();
            _gameClient = gameClient;
            _gameServer = gameServer;
            this.gameCode = gameCode;

            // Connect to the server
            _gameClient.ConnectToServer(gameCode);
            
        }

        private void UpdatePlayerList(List<string> players)
        {
            // This method will be called when a new player connects
            Application.Current.Dispatcher.Invoke(() =>
            {
                PlayersList.Items.Clear();
                foreach (var player in players)
                {
                    PlayersList.Items.Add(player);
                }
            });
        }

        private void StartGame()
        {
            // This method will be called when all players are connected
            Application.Current.Dispatcher.Invoke(() =>
            {
                PlayerWait.Content = "All players are connected! The game is starting...";
                // Further initialization for the game
                // For example, you might want to fetch and display images here
            });
        }

   
        private void UploadImagesButton_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to select images
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Images",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true // Allow multiple file selection
            };

            if (openFileDialog.ShowDialog() == true)
            {
                playerImages = new List<string>(openFileDialog.FileNames); // Store selected image paths
                _gameClient.UploadImages(playerImages.ToArray()); // Upload images to the server
            }
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton clickedButton)
            {
                string selectedAnswer = clickedButton.Content.ToString().Trim();
                // Send the guess to the server
                _gameClient.SendMessageToServer($"Guess {selectedAnswer}");
            }
        }

        private void TurnOffMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (Music.Content == "Turn Off Music")
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

        // Additional methods to handle game state, timer, etc.
    }
}