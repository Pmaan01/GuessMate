using NUnit.Framework;
using Moq;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using GuessMate;

namespace GusseMate.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private GameServer _gameServer;
        private Mock<TcpClient> _mockTcpClient;
        private Mock<NetworkStream> _mockNetworkStream;

        [SetUp]
        public void Setup()
        {
            // Initialize the GameServer with a dummy game code.
            _gameServer = new GameServer("123456");
            _mockTcpClient = new Mock<TcpClient>();
            _mockNetworkStream = new Mock<NetworkStream>();

            // Mock the TcpClient's GetStream method to return our mocked NetworkStream.
            _mockTcpClient.Setup(client => client.GetStream()).Returns(_mockNetworkStream.Object);
        }

        [Test]
        public void StartServer_InitializesListenerAndWaitsForClients()
        {
            // Arrange
            var serverThread = new Thread(new ThreadStart(_gameServer.StartServer));
            ManualResetEvent serverStarted = new ManualResetEvent(false);

            _gameServer.OnPlayerConnected += () =>
            {
                serverStarted.Set(); // Signal that the server has started
            };

            // Act
            serverThread.Start();

            // Wait for server to start listening
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(serverStarted.WaitOne(5000), "Server did not start within the expected time.");

            // Assert
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("123456", _gameServer.gameCode); // Verify the game code
        }

        [Test]
        public void HandleClientComm_SendsGameCodeToClient()
        {
            // Arrange
            _gameServer.HandleClientComm(_mockTcpClient.Object);

            // Act
            // Simulate sending the game code to the client
            _mockNetworkStream.Raise(stream => stream.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()));

            // Assert
            _mockNetworkStream.Verify(stream => stream.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Once);
        }

        [Test]
        public void StartGameButton_StartsGameWhenPlayersAreConnected()
        {
            // Arrange
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate 1st player connection
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate 2nd player connection

            // Act
            _gameServer.StartMultiplayerGame();

            // Assert
            _mockNetworkStream.Verify(stream => stream.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Exactly(2)); // Sent to both clients
        }

        [Test]
        public void StartRound_HandlesRoundsCorrectly()
        {
            // Arrange
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate player connection
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate 2nd player connection
            _gameServer.StartMultiplayerGame(); // Start the game

            // Act
            _gameServer.StartRound();

            // Assert
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(1, _gameServer.currentRound); // Round should have incremented
        }

        [Test]
        public void BroadcastMessage_SendsMessageToAllClients()
        {
            // Arrange
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate 1st player connection
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate 2nd player connection

            // Act
            _gameServer.BroadcastMessage("Game started!");

            // Assert
            _mockNetworkStream.Verify(stream => stream.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Exactly(2)); // Sent to both clients
        }

        [Test]
        public void StopServer_StopsServerAndClosesConnections()
        {
            // Arrange
            _gameServer.HandleClientComm(_mockTcpClient.Object); // Simulate player connection

            // Act
            _gameServer.StopServer();

            // Assert
            _mockTcpClient.Verify(client => client.Close(), Times.Once); // Ensure the connection was closed
        }

        [Test]
        public void HandleClientComm_HandlesClientMessagesCorrectly()
        {
            // Arrange
            string expectedMessage = "StartGame";
            byte[] buffer = Encoding.UTF8.GetBytes(expectedMessage);
            _mockNetworkStream.Setup(ns => ns.Read(It.IsAny<byte[]>(), 0, It.IsAny<int>())).Returns(buffer.Length);

            // Act
            _gameServer.HandleClientComm(_mockTcpClient.Object);

            // Assert
            _mockNetworkStream.Verify(stream => stream.Write(It.IsAny<byte[]>(), 0, It.IsAny<int>()), Times.Once);
        }
    }
}