using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace GuessMate
{
    public class ImageData
    {
        public string ImagePath { get; set; }
        public string Hint { get; set; }
        public string ImageName { get; set; }
        public BitmapImage ImagePreview { get; set; } // Store the preview image for UI

        // Constructor to initialize the properties
        public ImageData(string imagePath, string hint, string imageName = "")
        {
            ImagePath = imagePath;
            Hint = hint;
            ImageName = imageName;
            ImagePreview = LoadImageFromPath(imagePath); // Load image for UI display
        }

        // Method to load the image from a file path (used for BitmapImage)
        private BitmapImage LoadImageFromPath(string path)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
                bitmapImage.EndInit();
                return bitmapImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }

        // Convert BitmapImage to byte[] for saving to the database
        public byte[] ToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(ImagePreview));
                encoder.Save(ms);
                return ms.ToArray(); // Return the byte array of the image
            }
        }
    }
}
