using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GuessMate
{
    public class GameSession
    {
        public static List<Player> Players { get; private set; }
        public int CurrentTurnIndex { get; private set; }
        public List<ImageData> ImagesForGuessing { get; private set; }
        public int CurrentImageIndex { get; private set; }
        private ImageDatabaseHelper _dbHelper;
        readonly string bollywoodFolderPath = @"C:\Users\gurwi\Downloads\Flags";
        readonly string randomFolderPath = @"C:\Users\gurwi\Downloads\Random";
        readonly string householdFolderPath = @"C:\Users\gurwi\Downloads\Household";

        public string SelectedTheme { get; set; }
        public string PlayerCode { get; set; } // Property to store the player's code


        public GameSession(GameMode gameMode, int totalRounds = 5)
        {
            Players = new List<Player>();
            _dbHelper = new ImageDatabaseHelper();
            // Save images to the database during initialization
            SaveImagesToDatabase(bollywoodFolderPath, "Flags");
            SaveImagesToDatabase(randomFolderPath, "Random");
            SaveImagesToDatabase(householdFolderPath, "Household");
            CurrentTurnIndex = 0;
            ImagesForGuessing = new List<ImageData>();
            CurrentImageIndex = 0;
            RoundHints = new List<string>();
            GameMode = gameMode;
            CurrentGameState = GameState.WaitingForPlayers; // Set initial state as WaitingForPlayers
            MaxRounds = totalRounds;
        }

        public GameMode GameMode { get; private set; }
        public GameState CurrentGameState { get; private set; }
        public List<string> RoundHints { get; private set; }
        public int CurrentRoundNumber { get; private set; }
        public int MaxRounds { get; private set; } // Maximum number of rounds

        // Add a player to the game session
        public void AddPlayer(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            Players.Add(player);

            // Change state to InProgress if there are more than one player
            if (Players.Count > 1 || (Players.Count == 1 && GameMode == GameMode.SinglePlayer))
            {
                CurrentGameState = GameState.InProgress;
            }
        }
        public GameSession()
        {
           
            SelectedTheme = string.Empty;
        }


        // Get the current player
        public Player CurrentPlayer
        {
            get
            {
                if (Players.Count == 0)
                    throw new InvalidOperationException("No players in the game.");
                return Players[CurrentTurnIndex];
            }
        }


        // Save images to the database
        public void SaveImagesToDatabase(string folderPath, string category)
        {
            var files = Directory.GetFiles(folderPath, "*.jpg");
            foreach (var file in files)
            {
                bool imageExists = _dbHelper.CheckImageExists(Path.GetFileNameWithoutExtension(file), category);
                if (!imageExists)
                {
                    _dbHelper.AddImageToDatabase(category, Path.GetFileNameWithoutExtension(file), file, "Nature");
                }
            }
        }
    }

    public enum GameMode
    {
        SinglePlayer,
        MultiPlayer
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        GameOver
    }
}