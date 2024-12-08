using NUnit.Framework;
using Moq;
using System.Windows;
using GuessMate;
using ServerSide;
using System.Windows.Controls;


public class GameLobbyTests
{
    private Mock<GameSession> _mockGameSession;
    private Mock<GameClient> _mockGameClient;
    private Mock<GameServer> _mockGameServer;
    private GameLobby _gameLobby;

    [SetUp]
    public void Setup()
    {
        // Mock the GameSession
        _mockGameSession = new Mock<GameSession>(GameMode.MultiPlayer);
        // Mock the GameClient
        _mockGameClient = new Mock<GameClient>();
        // Mock the GameServer
        _mockGameServer = new Mock<GameServer>("123456");
        _mockGameServer.Setup(s => s.SelectedTheme).Returns("TestTheme");
        _mockGameServer.Setup(s => s.SetTheme(It.IsAny<string>()));

        // Create an instance of GameLobby with mocked dependencies
        _gameLobby = new GameLobby(
            _mockGameSession.Object,
            4, // Max players
            true, // Is host
            _mockGameClient.Object,
            _mockGameServer.Object
        );
    }

    [Test]
    public void StartGameButton_Click_ValidTheme_SetsThemeOnServer()
    {
        // Simulate selecting a theme
        ComboBoxItem comboBoxItem = new ComboBoxItem { Content = "Sports" };
        _gameLobby.GameModeComboBox.SelectedItem = comboBoxItem;

        // Call the StartGameButton_Click method
        _gameLobby.StartGameButton_Click(null, null);

        // Verify that the theme was set on the server
        _mockGameServer.Verify(s => s.SetTheme("Sports"), Times.Once);
    }

    [Test]
    public void StartGameButton_Click_NoThemeSelected_ShowsErrorMessage()
    {
        // Clear selected item
        _gameLobby.GameModeComboBox.SelectedItem = null;

        // Attach MessageBox mock
        string expectedError = "Please Fill all the details.";
        Assert.Throws<Exception>(() => _gameLobby.StartGameButton_Click(null, null), expectedError);
    }

    [Test]
    public void UpdateCodeTextBlock_UpdatesCodeTextBasedOnServer()
    {
        // Call the UpdateCodeTextBlock method
        _gameLobby.UpdateCodeTextBlock();

        // Assert the Code.Text is updated correctly
        Assert.AreEqual($"Code: 123456\nTheme: TestTheme", _gameLobby.Code.Text);
    }

    [Test]
    public void TurnOffMusicButton_Click_TogglesMusicState()
    {
        // Initial state
        MainWindow.MusicState.IsMusicOn = true;

        // Simulate button click
        _gameLobby.TurnOffMusicButton_Click(null, null);

        // Assert that music is turned off
        Assert.IsFalse(MainWindow.MusicState.IsMusicOn);
        Assert.AreEqual("Turn On Music", _gameLobby.Music.Content.ToString());

        // Simulate another button click
        _gameLobby.TurnOffMusicButton_Click(null, null);

        // Assert that music is turned on again
        Assert.IsTrue(MainWindow.MusicState.IsMusicOn);
        Assert.AreEqual("Turn Off Music", _gameLobby.Music.Content.ToString());
    }

    [Test]
    public void Constructor_AddsPlayersCorrectlyForSinglePlayerMode()
    {
        // Create a new single-player GameLobby
        _gameLobby = new GameLobby(_mockGameSession.Object, 1, true, _mockGameClient.Object, _mockGameServer.Object);

        // Assert that the PC player is added
        _mockGameSession.Verify(s => s.AddPlayer(It.Is<Player>(p => p.Name == "PC")), Times.Once);
    }

    [Test]
    public void StartGame_ValidSetup_OpensPlayerTurnWindow()
    {
        // Set player name
        _gameLobby.PlayerNameTextBox.Text = "Player1";

        // Call StartGame with a category
        _gameLobby.StartGame("Sports");

        // Verify that the player was added to the session
        _mockGameSession.Verify(s => s.AddPlayer(It.Is<Player>(p => p.Name == "Player1")), Times.Once);

        // Verify that PlayerTurn window is shown
        // (Mocking UI elements like window Show() might need additional frameworks)
    }
}
