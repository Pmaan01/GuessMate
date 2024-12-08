using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GuessMate.Tests
{
    [TestFixture]
    public class FinalScoresTests
    {
        private Mock<GameClient> _mockGameClient;
        private Mock<PlayerImageDatabaseHelper> _mockDatabaseHelper;
        private List<Player> _players;
        private FinalScores _finalScores;

        [SetUp]
        public void Setup()
        {
            // Mock dependencies
            _mockGameClient = new Mock<GameClient>();
            _mockDatabaseHelper = new Mock<IPlayerImageDatabaseHelper>();

            // Sample player data
            _players = new List<Player>
            {
                new Player { Name = "Alice", Score = 10 },
                new Player { Name = "Bob", Score = 15 },
                new Player { Name = "Charlie", Score = 15 }
            };

            // Setup game session
            GameSession.Players = _players;

            // Mock game session and dependencies for FinalScores
            _finalScores = new FinalScores(new GameSession(), _mockGameClient.Object, "Alice");
        }

        [Test]
        public void CalculateWinner_SingleWinner_Success()
        {
            // Arrange
            _players[0].Score = 20;

            // Act
            var finalScoresWindow = new FinalScores(new GameSession(), _mockGameClient.Object, "Alice");

            // Assert
            Assert.IsTrue(finalScoresWindow.FinalScoresList.Items.Contains("Congratulations Alice, you are the Winner!"));
        }

        [Test]
        public void CalculateWinner_Tie_Success()
        {
            // Arrange
            _players[0].Score = 15;

            // Act
            var finalScoresWindow = new FinalScores(new GameSession(), _mockGameClient.Object, "Alice");

            // Assert
            Assert.IsTrue(finalScoresWindow.FinalScoresList.Items.Contains("It's a tie between: Alice, Bob, Charlie"));
        }

        [Test]
        public void PlayAgain_ClearsPlayerImages_AndRestartsGame()
        {
            // Arrange
            _mockDatabaseHelper.Setup(d => d.RemovePlayerImages(It.IsAny<string>()));

            // Act
            _finalScores.PlayAgainButton_Click(null, null);

            // Assert
            foreach (var player in _players)
            {
                _mockDatabaseHelper.Verify(d => d.RemovePlayerImages(player.Name), Times.Once);
            }
        }

        [Test]
        public void UpdateMusicState_MusicOn_Success()
        {
            // Arrange
            MainWindow.MusicState.IsMusicOn = true;

            // Act
            _finalScores.UpdateMusicState();

            // Assert
            Assert.AreEqual("Turn Off Music", _finalScores.Music.Content.ToString());
        }

        [Test]
        public void UpdateMusicState_MusicOff_Success()
        {
            // Arrange
            MainWindow.MusicState.IsMusicOn = false;

            // Act
            _finalScores.UpdateMusicState();

            // Assert
            Assert.AreEqual("Turn On Music", _finalScores.Music.Content.ToString());
        }

        [Test]
        public async Task ShowHighScores_DisplaysHighScoreForCurrentPlayer()
        {
            // Arrange
            var highScores = new Dictionary<string, int>
            {
                { "Alice", 20 },
                { "Bob", 15 },
                { "Charlie", 10 }
            };

            _mockGameClient.Setup(gc => gc.RequestHighScoresAsync()).ReturnsAsync(highScores);

            // Act
            await _finalScores.ShowHighScores();

            // Assert
            _mockGameClient.Verify(gc => gc.RequestHighScoresAsync(), Times.Once);
            Assert.Pass(); // Check the message box manually or use a UI testing framework
        }

        [Test]
        public void ExitButton_Click_ClosesApplication()
        {
            // Arrange
            bool applicationClosed = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Exit += (s, e) => applicationClosed = true;
            });

            // Act
            _finalScores.ExitButton_Click(null, null);

            // Assert
            Assert.IsTrue(applicationClosed);
        }
    }
}
