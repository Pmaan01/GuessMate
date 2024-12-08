using NUnit.Framework;
using Moq;
using System.Threading;
using ServerSide;
using System.Windows.Controls;
using GuessMate;

namespace GuessMate.Tests
{
    [TestFixture]
    public class MainWindowTests
    {
        private Mock<IGameServerFactory> _mockServerFactory;
        private Mock<GameServer> _mockGameServer;
        private Mock<GameClient> _mockGameClient;
        private MainWindow _mainWindow;

        [SetUp]
        public void SetUp()
        {
            _mockServerFactory = new Mock<IGameServerFactory>();
            _mockGameServer = new Mock<GameServer>();
            _mockGameClient = new Mock<GameClient>();

            // Mock server factory to return a mock server
            _mockServerFactory.Setup(f => f.CreateServer(It.IsAny<string>()))
                              .Returns(_mockGameServer.Object);

            // Create an instance of MainWindow with mock dependencies
            _mainWindow = new MainWindow(_mockServerFactory.Object);
        }

        [Test]
        public void StartServer_StartsServerInBackgroundThread()
        {
            // Arrange
            var gameCode = "123456";
            _mockGameServer.Setup(s => s.IsServerReady).Returns(true);

            // Act
            _mainWindow.StartServer(gameCode);

            // Assert
            Thread.Sleep(200); // Allow time for background thread to start
            _mockServerFactory.Verify(f => f.CreateServer(gameCode), Times.Once);
            _mockGameServer.Verify(s => s.StartServer(), Times.Once);
        }

        [Test]
        public void StartGameButton_Click_StartsSinglePlayerGame()
        {
            // Arrange
            _mainWindow._maxPlayers = 1;

            // Act
            _mainWindow.StartGameButton_Click(null, null);

            // Assert
            _mockServerFactory.Verify(f => f.CreateServer("SinglePlayer"), Times.Once);
            _mockGameServer.VerifySet(s => s.Expectedplayer = 1);
            _mockGameServer.Verify(s => s.StartServer(), Times.Once);
        }

        [Test]
        public void StartGameButton_Click_StartsMultiplayerGame()
        {
            // Arrange
            _mainWindow._maxPlayers = 2;

            // Act
            _mainWindow.StartGameButton_Click(null, null);

            // Assert
            _mockServerFactory.Verify(f => f.CreateServer(It.IsAny<string>()), Times.Once);
            _mockGameServer.VerifySet(s => s.Expectedplayer = 2);
            _mockGameServer.Verify(s => s.StartServer(), Times.Once);
        }

        [Test]
        public void JoinGameButton_Click_HandlesInvalidGameCodeGracefully()
        {
            // Arrange
            _mockGameServer.Setup(s => s.JoinGame(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns((GameServer)null);

            // Act
            _mainWindow.JoinGameButton_Click(null, null);

            // Assert
            Assert.IsNull(_mainWindow.gameLobby);
        }

        [Test]
        public void TurnOffMusicButton_Click_TogglesMusicState()
        {
            // Arrange
            MainWindow.MusicState.IsMusicOn = true;

            // Act
            _mainWindow.TurnOffMusicButton_Click(null, null);

            // Assert
            Assert.IsFalse(MainWindow.MusicState.IsMusicOn);
        }

        [Test]
        public void PlayerCountComboBox_SelectionChanged_UpdatesMaxPlayers()
        {
            // Arrange
            var comboBox = new ComboBox { Items = { "1 Player", "2 Players" }, SelectedIndex = 1 };
            var selectedItem = new ComboBoxItem { Content = "2 Players" };

            // Act
            _mainWindow.PlayerCountComboBox_SelectionChanged(comboBox, null);

            // Assert
            Assert.AreEqual(2, _mainWindow._maxPlayers);
        }
    }
}
