using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace GuessMate
{
    public class ImageData
    {
        public string ImagePath { get; set; }
        public string Hint { get; set; }
        public string ImageName { get; set; }
        public BitmapImage ImagePreview { get; set; }

        // Constructor for initializing the ImageData
        public ImageData(string imagePath, string hint, string imageName = "")
        {
            ImagePath = imagePath;
            Hint = hint;
            ImageName = imageName;
            ImagePreview = ImageDatabaseHelper.LoadImageFromPath(imagePath); // Load image for PC player UI display
        }
    }

}
