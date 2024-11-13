using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Threading;
using System.Windows;

namespace GuessMate
{
    public class GameSession
    {
        public static List<Player> Players { get; private set; }
        public int CurrentTurnIndex { get; private set; }
        public List<ImageData> ImagesForGuessing { get; private set; }
        public int CurrentImageIndex { get; private set; }
        private ImageDatabaseHelper _dbHelper;
        readonly string bollywoodFolderPath = @"C:\Users\gurwi\Downloads\Bollywood";
        readonly string randomFolderPath = @"C:\Users\gurwi\Downloads\Random";
        readonly string householdFolderPath = @"C:\Users\gurwi\Downloads\Household";

        public ImageData? CurrentImageData
        {
            get
            {
                return CurrentImageIndex < ImagesForGuessing.Count ? ImagesForGuessing[CurrentImageIndex] : null;
            }
        }
        public List<string> RoundHints { get; private set; }

        public int CurrentRoundNumber { get; private set; }
        public GameMode GameMode { get; private set; } // Add GameMode to track multiplayer/single-player mode
        public GameState CurrentGameState { get; private set; } // Add GameState to track the current state of the game

        public GameSession(GameMode gameMode)
        {
            Players = new List<Player>();
            _dbHelper = new ImageDatabaseHelper();
            // Save images to the database during initialization
            SaveImagesToDatabase(bollywoodFolderPath, "Bollywood");
            SaveImagesToDatabase(randomFolderPath, "Random");
            SaveImagesToDatabase(householdFolderPath, "Household");
            CurrentTurnIndex = 0;
            ImagesForGuessing = new List<ImageData>();
            CurrentImageIndex = 0;
            RoundHints = new List<string>();
            CurrentRoundNumber = 0;
            GameMode = gameMode;
            CurrentGameState = GameState.WaitingForPlayers; // Set initial state as WaitingForPlayers
        }
        

        // Add a player to the game session
        public void AddPlayer(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            Players.Add(player);

            Console.WriteLine($"Player added: {player.Name}");

            // Change state to InProgress if there are more than one player
            if (Players.Count > 1 || (Players.Count == 1 && GameMode == GameMode.SinglePlayer))
            {
                CurrentGameState = GameState.InProgress;
            }
        }
        // Property for the current player
        public Player CurrentPlayer
        {
            get
            {
                if (Players.Count == 0)
                    throw new InvalidOperationException("No players in the game.");
                return Players[CurrentTurnIndex];
            }
        }

        // Add an image for guessing rounds
        public void AddImageForGuessing(ImageData imageData)
        {
            if (imageData == null) throw new ArgumentNullException(nameof(imageData));
            ImagesForGuessing.Add(imageData);
        }

        // Modify the SetRoundDetails method to accept players and image data
        public void SetRoundDetails(int roundNumber, List<ImageData> images)
        {
            if (images == null || images.Count == 0)
                throw new ArgumentException("Images for the round cannot be null or empty.", nameof(images));

         

            // Set the round details
            CurrentRoundNumber = roundNumber;
            ImagesForGuessing = new List<ImageData>(images);  // Set images for the round

            Console.WriteLine($"Round {roundNumber} has been set with {images.Count} images and hints.");
        }

        // Get the hint for the current image
        public string GetHintForCurrentImage()
        {
            if (CurrentImageIndex < RoundHints.Count)
            {
                return RoundHints[CurrentImageIndex];
            }
            return "No hint available.";
        }

        // Check the guess and update the score
        public bool CheckGuess(string guess)
        {
            if (Players.Count == 0)
            {
                throw new InvalidOperationException("No players in the game.");
            }

            if (CurrentImageData == null)
                return false;

            bool isCorrect = guess.Equals(System.IO.Path.GetFileNameWithoutExtension(CurrentImageData.ImagePath), StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                // Update score of the player who guessed correctly
                CurrentPlayer.Score++;

                // Move to the next image
                CurrentImageIndex++;
            }

            // Move to the next player's turn
            CurrentTurnIndex = (CurrentTurnIndex + 1) % Players.Count;

            return isCorrect;
        }
        public void SaveImagesToDatabase(string folderPath, string category)
        {
            var files = Directory.GetFiles(folderPath, "*.jpg");  // You can adjust the file type if necessary (e.g., "*.png")
            foreach (var file in files)
            {
                // Check if the image already exists in the database
                bool imageExists = _dbHelper.CheckImageExists(Path.GetFileNameWithoutExtension(file), category);

                if (!imageExists)
                {
                    // Only add image if it doesn't already exist in the database
                    _dbHelper.AddImageToDatabase(category, Path.GetFileNameWithoutExtension(file), file, "Nature");
                }
            }
        }
        private void StartSinglePlayerGame(string category)
        {
            // Fetch images and folder path for single-player mode
            ImageDatabaseHelper dbHelper = new ImageDatabaseHelper();
            string folderPath = dbHelper.GetFolderPath(category);
            var images = dbHelper.GetImagesFromDatabase(category);

            if (images.Count > 0 && !string.IsNullOrEmpty(folderPath))
            {
                // Pass images and folder path to the GameLobby for the single-player game
                MessageBox.Show($"Starting single-player game with category {category}. Folder path: {folderPath}. Selected images: {string.Join(", ", images.Select(i => i.ImageName))}");       
            }
            else
            {
                MessageBox.Show("No images found in the selected category or folder path not found.");
            }
        }

        private void StartMultiplayerGame(string category)
        {
            // Fetch images and folder path for multiplayer mode
            ImageDatabaseHelper dbHelper = new ImageDatabaseHelper();
            string folderPath = dbHelper.GetFolderPath(category);
            var images = dbHelper.GetImagesFromDatabase(category);

            if (images.Count > 0 && !string.IsNullOrEmpty(folderPath))
            {


            }
            else
            {
                MessageBox.Show("No images found in the selected category or folder path not found.");
            }
        }

        // Get the current player's turn
        public Player GetCurrentPlayer()
        {
            if (Players.Count == 0)
                throw new InvalidOperationException("No players in the game.");

            return Players[CurrentTurnIndex];
        }

        // Retrieve the current score of the player
        public int GetCurrentPlayerScore()
        {
            if (Players.Count == 0)
                throw new InvalidOperationException("No players in the game.");

            return Players[CurrentTurnIndex].Score;
        }

        // Check if the game has ended
        public bool IsGameOver(int totalRounds)
        {
            return CurrentImageIndex >= totalRounds;
        }
        private void EndGame()
        {
            // Remove player images from the database
            PlayerImageDatabaseHelper databaseHelper = new PlayerImageDatabaseHelper();
            databaseHelper.RemovePlayerImages();

            // Close the current window or proceed to the next screen
            MessageBox.Show("Game Over! All player images have been removed from the database.");
        }


        // Get all players' final scores
        public List<Player> GetFinalScores()
        {
            // Return a copy to ensure immutability outside the class
            return new List<Player>(Players);
        }

        // Get the current round hints
        public List<string> GetCurrentRoundHints()
        {
            return RoundHints; // Returns the hints for the current round
        }

        // Move to the next round
        public void MoveToNextRound()
        {
            CurrentRoundNumber++;
            CurrentImageIndex = 0; // Reset the image index for the next round
        }

        // Get the current round images
        public List<ImageData> GetCurrentRoundImages()
        {
            return ImagesForGuessing; // Returns the images for the current round
        }

        // Reset the game session
        public void ResetGameSession()
        {
            CurrentTurnIndex = 0;
            CurrentImageIndex = 0;
            CurrentRoundNumber = 0;
            ImagesForGuessing.Clear();
            RoundHints.Clear();
            foreach (var player in Players)
            {
                player.Score = 0;
            }

            Console.WriteLine("Game session has been reset.");
            CurrentGameState = GameState.WaitingForPlayers; // Reset to waiting for players

            
        }

        // Print all player scores
        public void PrintPlayerScores()
        {
            Console.WriteLine("Player Scores:");
            foreach (var player in Players)
            {
                Console.WriteLine($"{player.Name}: {player.Score}");
            }
        }

        // Timeout functionality (optional): Timeout a player's turn after a certain period
        public void StartPlayerTurnTimer(int timeoutSeconds)
        {
            // Example: Timeout logic for player turns could be implemented here
            // You can use a Timer to handle turn timeouts for each player
        }
    }

    // Enum to represent the game mode (SinglePlayer or MultiPlayer)
    public enum GameMode
    {
        SinglePlayer,
        MultiPlayer
    }

    // Enum to represent the state of the game
    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        GameOver
    }
}
