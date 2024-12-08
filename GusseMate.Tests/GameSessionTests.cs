using System;
using System.Collections.Generic;
using GuessMate;
using NUnit.Framework;
using Moq; // For mocking dependencies like ImageDatabaseHelper

namespace GuessMateTests
{
    [TestFixture]
    public class GameSessionTests
    {
        private GameSession _gameSession;
        private Mock<ImageDatabaseHelper> _mockDbHelper;

        [SetUp]
        public void SetUp()
        {
            // Mock the ImageDatabaseHelper
            _mockDbHelper = new Mock<ImageDatabaseHelper>();
            _gameSession = new GameSession(GameMode.SinglePlayer, 5)
            {
                // Inject the mock dependency
                _dbHelper = _mockDbHelper.Object
            };
        }

        [Test]
        public void Test_AddPlayer_ValidPlayer_ChangesGameStateToInProgress()
        {
            // Arrange
            var player = new Player("Player1", "Code123");

            // Act
            _gameSession.AddPlayer(player);

            // Assert
            Assert.AreEqual(GameState.InProgress, _gameSession.CurrentGameState);
            Assert.AreEqual(1, _gameSession.Players.Count);
        }

        [Test]
        public void Test_AddPlayer_WithMultiplePlayers_ChangesGameStateToInProgress()
        {
            // Arrange
            var player1 = new Player("Player1", "Code123");
            var player2 = new Player("Player2", "Code456");

            // Act
            _gameSession.AddPlayer(player1);
            _gameSession.AddPlayer(player2);

            // Assert
            Assert.AreEqual(GameState.InProgress, _gameSession.CurrentGameState);
            Assert.AreEqual(2, _gameSession.Players.Count);
        }

        [Test]
        public void Test_AddPlayer_ThrowsException_WhenPlayerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _gameSession.AddPlayer(null));
        }

        [Test]
        public void Test_SaveImagesToDatabase_AddsImagesWhenNotExist()
        {
            // Arrange
            var category = "TestCategory";
            var folderPath = @"C:\Images";

            // Mock the CheckImageExists method to return false
            _mockDbHelper.Setup(m => m.CheckImageExists(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            // Act
            _gameSession.SaveImagesToDatabase(folderPath, category);

            // Assert
            _mockDbHelper.Verify(m => m.AddImageToDatabase(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Test]
        public void Test_CurrentPlayer_ReturnsCorrectPlayer()
        {
            // Arrange
            var player1 = new Player("Player1", "Code123");
            var player2 = new Player("Player2", "Code456");

            _gameSession.AddPlayer(player1);
            _gameSession.AddPlayer(player2);

            // Act
            var currentPlayer = _gameSession.CurrentPlayer;

            // Assert
            Assert.AreEqual(player1, currentPlayer);
        }

        [Test]
        public void Test_Constructor_WithValidGameMode_SetsGameStateToWaitingForPlayers()
        {
            // Arrange & Act
            var gameSession = new GameSession(GameMode.MultiPlayer);

            // Assert
            Assert.AreEqual(GameState.WaitingForPlayers, gameSession.CurrentGameState);
        }

        [Test]
        public void Test_CurrentPlayer_ThrowsException_WhenNoPlayersInGame()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _gameSession.CurrentPlayer);
        }
    }
}
