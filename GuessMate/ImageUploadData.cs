using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace GuessMate
{
    public class ImageUploadData : INotifyPropertyChanged
    {
        private BitmapImage _imagePreview;
        private ImageData _imageData;
        private bool _isValid;

        public ImageData ImageData
        {
            get => _imageData;
            set
            {
                _imageData = value;
                OnPropertyChanged(nameof(ImageData));
            }
        }

        public BitmapImage ImagePreview
        {
            get => _imagePreview;
            set
            {
                _imagePreview = value;
                OnPropertyChanged(nameof(ImagePreview));
            }
        }

        // The IsValid property is now writable
        public bool IsValid
        {
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageUploadData()
        {
            // Initialize ImageData with default values
            ImageData = new ImageData(string.Empty, string.Empty);
            IsValid = false; // Default value
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
