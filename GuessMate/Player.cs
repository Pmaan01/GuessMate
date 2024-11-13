using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace GuessMate
{
    public class Player
    {
        public string Name { get; set; }
        public List<ImageData> Pictures { get; set; }  // Store the images as ImageData objects
        public int Score { get; set; }

        public Player(string name)
        {
            Name = name;
            Pictures = new List<ImageData>();  // List to hold ImageData objects
            Score = 0;
        }

        // Get an image by index (returning a BitmapImage)
        public BitmapImage GetPicture(int index)
        {
            if (index >= 0 && index < Pictures.Count)
            {
                return new BitmapImage(new Uri(Pictures[index].ImagePath, UriKind.RelativeOrAbsolute));
            }
            else
            {
                throw new ArgumentOutOfRangeException("Index is out of range for the player's images.");
            }
        }

        // Optionally add method for updating score
        public void IncreaseScore(int points)
        {
            Score += points;
        }
    }

   
}
