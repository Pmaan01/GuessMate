using NUnit.Framework;
using Moq;
using GuessMate;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GuessMateTests
{
    [TestFixture]
    public class PlayGroundTests
    {
        private Mock<GameSession> mockGameSession;
        private Mock<GameClient> mockGameClient;
        private Mock<PlayerImageDatabaseHelper> mockImageDatabaseHelper;
        private PlayGround playGround;
        private List<Player> mockPlayers;

        [SetUp]
        public void SetUp()
        {
            mockGameSession = new Mock<GameSession>();
            mockGameClient = new Mock<GameClient>();
            mockImageDatabaseHelper = new Mock<PlayerImageDatabaseHelper>();

            // Creating mock players for testing
            mockPlayers = new List<Player>
            {
                new Player { Name = "Player1", Score = 0 },
                new Player { Name = "Player2", Score = 0 },
                new Player { Name = "PC", Score = 0 }
            };

            // Setting up the mock game session
            mockGameSession.Setup(g => g.Players).Returns(mockPlayers);

            // Initializing the PlayGround window
            playGround = new PlayGround(mockGameSession.Object, mockGameClient.Object);
        }

        [Test]
        public void TestEndGame_ShouldShowFinalScores()
        {
            // Arrange: Add some mock scores to players
            mockPlayers[0].Score = 10;
            mockPlayers[1].Score = 5;

            // Act: Call EndGame method
            playGround.EndGame();

            // Assert: Check if the final scores are shown correctly
            // We will mock the interaction with the final scores window
            mockGameClient.Verify(client => client.SendScoreToServer("Player1", 10), Times.Once);
            mockGameClient.Verify(client => client.SendScoreToServer("Player2", 5), Times.Once);
        }

        [Test]
        public void TestNextPlayerTurn_ShouldMoveToNextPlayer()
        {
            // Arrange: Initial setup has Player1 as the first player
            var initialPlayerIndex = playGround.CurrentPlayerIndex;

            // Act: Simulate the next player's turn
            playGround.NextPlayerTurn();

            // Assert: Ensure the current player index is updated to the next player
            Assert.AreEqual(initialPlayerIndex + 1, playGround.CurrentPlayerIndex);
        }

        [Test]
        public void TestNextPlayerTurn_ShouldLoopBackAfterLastPlayer()
        {
            // Arrange: Set currentPlayerIndex to last player
            playGround.CurrentPlayerIndex = 2;

            // Act: Move to next player, should loop back to first player
            playGround.NextPlayerTurn();

            // Assert: Ensure the current player index is 0 after looping
            Assert.AreEqual(0, playGround.CurrentPlayerIndex);
        }

        [Test]
        public void TestStartTimer_ShouldStartTimerAndUpdateDisplay()
        {
            // Arrange: Start the timer
            playGround.StartTimer();

            // Act: Simulate a tick on the timer (This can be tricky since we are using a DispatcherTimer)
            // Ideally, we can use a timer mock or simulate the Tick event
            playGround.Timer_Tick(null, null);

            // Assert: Ensure timeLeft has been updated and timer is running
            Assert.AreEqual(4, playGround.TimeLeft); // Initially set to 5, and it decreases by 1
        }

        [Test]
        public void TestOptionButtonClick_ShouldCheckAnswerAndUpdateScore()
        {
            // Arrange: Set the correct answer
            playGround.currentImageNameToGuess = "correctAnswer";

            var correctButton = new RadioButton
            {
                Content = "correctAnswer"
            };

            // Act: Simulate a click on the correct answer
            playGround.OptionButton_Click(correctButton, null);

            // Assert: Ensure the score is updated for the current player
            Assert.AreEqual(1, mockPlayers[playGround.CurrentPlayerIndex].Score);
        }

        [Test]
        public void TestShuffleImagesAndDisplayHint_ShouldSetHint()
        {
            // Arrange: Mock images
            var mockImages = new List<ImageData>
            {
                new ImageData { ImageName = "Image1", Hint = "Hint 1", ImagePath = "path1.jpg" },
                new ImageData { ImageName = "Image2", Hint = "Hint 2", ImagePath = "path2.jpg" }
            };

            playGround.playerImages = mockImages;

            // Act: Shuffle images and display the hint
            playGround.ShuffleImagesAndDisplayHint();

            // Assert: Ensure the hint is set
            Assert.AreEqual("Hint: Hint 1", playGround.HintTextBlock.Text);
        }

        [Test]
        public void TestShowCurrentPlayer_ShouldDisplayCorrectPlayerName()
        {
            // Arrange: Current player should be Player1
            playGround.CurrentPlayerIndex = 0;

            // Act: Call ShowCurrentPlayer method
            playGround.ShowCurrentPlayer();

            // Assert: Ensure the player name is displayed correctly
            Assert.AreEqual("Current Player: Player1", playGround.Turn.Content.ToString());
        }
    }
}
