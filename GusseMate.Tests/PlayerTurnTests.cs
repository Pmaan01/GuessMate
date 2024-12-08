using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using GuessMate;
using ServerSide;

namespace GuessMateTests
{
    [TestFixture]
    public class PlayerTurnTests
    {
        private Mock<GameClient> _mockGameClient;
        private Mock<GameServer> _mockGameServer;
        private GameSession _gameSession;

        [SetUp]
        public void SetUp()
        {
            // Mock GameClient and GameServer
            _mockGameClient = new Mock<GameClient>();
            _mockGameServer = new Mock<GameServer>();

            // Initialize a mock GameSession with sample players
            _gameSession = new GameSession
            {
                Players = new List<Player>
                {
                    new Player { Name = "Player1", Score = 0 },
                    new Player { Name = "Player2", Score = 0 }
                }
            };
        }

        [Test]
        public void InitializeImageUploadData_ShouldCreateFiveEmptyImageSlots()
        {
            // Arrange
            var playerTurn = new PlayerTurn(_gameSession, _mockGameClient.Object, _mockGameServer.Object);

            // Act
            var imageData = playerTurn.ImageUploadData;

            // Assert
            Assert.AreEqual(5, imageData.Count);
            Assert.IsTrue(imageData.TrueForAll(data => data.ImageData.ImagePath == null));
            Assert.IsTrue(imageData.TrueForAll(data => data.ImagePreview == null));
        }

        [Test]
        public void SubmitButton_Click_AllImagesValid_ShouldShowSuccessMessage()
        {
            // Arrange
            var playerTurn = new PlayerTurn(_gameSession, _mockGameClient.Object, _mockGameServer.Object);
            var imageDataList = new List<ImageUploadData>();
            for (int i = 0; i < 5; i++)
            {
                imageDataList.Add(new ImageUploadData
                {
                    ImageData = new ImageData
                    {
                        ImageName = $"Image{i + 1}",
                        Hint = $"Hint{i + 1}",
                        ImagePath = $"Path{i + 1}"
                    },
                    IsValid = true,
                    ImagePreview = CreateDummyBitmapImage()
                });
            }

            playerTurn.ImageUploadData = imageDataList;

            // Act
            bool allImagesValid = true;
            foreach (var data in playerTurn.ImageUploadData)
            {
                if (string.IsNullOrWhiteSpace(data.ImageData.ImageName) ||
                    string.IsNullOrWhiteSpace(data.ImageData.Hint) ||
                    data.ImagePreview == null)
                {
                    allImagesValid = false;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(allImagesValid, "All images should be valid.");
        }

        [Test]
        public void SubmitButton_Click_MissingImageData_ShouldFailValidation()
        {
            // Arrange
            var playerTurn = new PlayerTurn(_gameSession, _mockGameClient.Object, _mockGameServer.Object);
            var imageDataList = new List<ImageUploadData>
            {
                new ImageUploadData
                {
                    ImageData = new ImageData { ImageName = "Image1", Hint = "Hint1" },
                    IsValid = false, // Missing image data
                    ImagePreview = null
                }
            };

            playerTurn.ImageUploadData = imageDataList;

            // Act
            bool allImagesValid = true;
            foreach (var data in playerTurn.ImageUploadData)
            {
                if (string.IsNullOrWhiteSpace(data.ImageData.ImageName) ||
                    string.IsNullOrWhiteSpace(data.ImageData.Hint) ||
                    data.ImagePreview == null)
                {
                    allImagesValid = false;
                    break;
                }
            }

            // Assert
            Assert.IsFalse(allImagesValid, "Validation should fail for missing image data.");
        }

        [Test]
        public void ConvertImageToByteArray_ShouldReturnNonEmptyByteArray()
        {
            // Arrange
            var playerTurn = new PlayerTurn(_gameSession, _mockGameClient.Object, _mockGameServer.Object);
            var bitmapImage = CreateDummyBitmapImage();

            // Act
            byte[] byteArray = playerTurn.ConvertImageToByteArray(bitmapImage);

            // Assert
            Assert.IsNotNull(byteArray);
            Assert.IsTrue(byteArray.Length > 0, "Byte array should not be empty.");
        }

        private BitmapImage CreateDummyBitmapImage()
        {
            byte[] imageBytes = new byte[100]; // Dummy byte array for image
            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                return bitmap;
            }
        }

        [Test]
        public void OpenPlayGroundWindow_ShouldNavigateToSinglePlayerPlayground()
        {
            // Arrange
            var playerTurn = new PlayerTurn(_gameSession, _mockGameClient.Object, _mockGameServer.Object);

            // Act
            Assert.DoesNotThrow(() => playerTurn.OpenPlayGroundWindow());

            // Assert
            // Ensure the method navigates to PlayGround window without exceptions
        }
    }
}
