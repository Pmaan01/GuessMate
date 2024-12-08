using NUnit.Framework;
using Moq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GuessMate.Tests
{
    [TestFixture]
    public class GameClientTests
    {
        private Mock<TcpClient> mockTcpClient;
        private Mock<NetworkStream> mockNetworkStream;
        private Mock<StreamReader> mockReader;
        private Mock<StreamWriter> mockWriter;
        private GameClient gameClient;

        [SetUp]
        public void Setup()
        {
            // Mock TcpClient, NetworkStream, StreamReader, and StreamWriter
            mockTcpClient = new Mock<TcpClient>();
            mockNetworkStream = new Mock<NetworkStream>();
            mockReader = new Mock<StreamReader>(new MemoryStream());
            mockWriter = new Mock<StreamWriter>(new MemoryStream());

            // Set up TcpClient mock to return the mocked NetworkStream
            mockTcpClient.Setup(tc => tc.GetStream()).Returns(mockNetworkStream.Object);

            // Create the GameClient instance
            gameClient = new GameClient
            {
                IsConnected = false
            };
        }

        [Test]
        public void ConnectToServer_ShouldConnectSuccessfully()
        {
            // Arrange
            string gameCode = "1234";
            mockTcpClient.Setup(tc => tc.Connect(It.IsAny<string>(), It.IsAny<int>())).Verifiable();

            // Act
            gameClient.ConnectToServer(gameCode);

            // Assert
            mockTcpClient.Verify(tc => tc.Connect(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            Assert.IsTrue(gameClient.IsConnected);
        }

        [Test]
        public void UploadImages_ShouldSendImageToServer()
        {
            // Arrange
            var imagePaths = new string[] { "image1.jpg" };
            mockNetworkStream.Setup(ns => ns.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            gameClient.ConnectToServer("1234");

            // Act
            gameClient.UploadImages(imagePaths);

            // Assert
            mockNetworkStream.Verify(ns => ns.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Test]
        public void SendScoreToServer_ShouldSendScoreMessage()
        {
            // Arrange
            string playerName = "Player1";
            int score = 100;
            mockNetworkStream.Setup(ns => ns.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            gameClient.ConnectToServer("1234");

            // Act
            gameClient.SendScoreToServer(playerName, score);

            // Assert
            mockNetworkStream.Verify(ns => ns.Write(It.Is<byte[]>(msg => Encoding.UTF8.GetString(msg) == $"SubmitScore|{playerName}|{score}"), 0, It.IsAny<int>()), Times.Once);
        }

        [Test]
        public void CloseConnection_ShouldCloseTcpClient()
        {
            // Arrange
            mockTcpClient.Setup(tc => tc.Close()).Verifiable();

            // Act
            gameClient.CloseConnection();

            // Assert
            mockTcpClient.Verify(tc => tc.Close(), Times.Once);
        }

        [Test]
        public void SendMessageToServer_ShouldSendMessage()
        {
            // Arrange
            string message = "Test message";
            mockNetworkStream.Setup(ns => ns.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
            gameClient.ConnectToServer("1234");

            // Act
            gameClient.SendMessageToServer(message);

            // Assert
            mockNetworkStream.Verify(ns => ns.Write(It.Is<byte[]>(msg => Encoding.UTF8.GetString(msg) == message), 0, It.IsAny<int>()), Times.Once);
        }

        [Test]
        public void RequestHighScoresAsync_ShouldReturnHighScores()
        {
            // Arrange
            var highScoresJson = "{\"Player1\": 100, \"Player2\": 200}";
            var expectedHighScores = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Player1", 100 },
                { "Player2", 200 }
            };

            mockNetworkStream.Setup(ns => ns.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>())).ReturnsAsync(Encoding.UTF8.GetBytes($"HighScores|{highScoresJson}"));
            gameClient.ConnectToServer("1234");

            // Act
            var highScores = gameClient.RequestHighScoresAsync().Result;

            // Assert
            Assert.AreEqual(expectedHighScores, highScores);
        }
    }
}
