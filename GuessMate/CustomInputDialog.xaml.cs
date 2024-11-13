using System.Windows;

namespace GuessMate
{
    public partial class CustomInputDialog : Window
    {
        public string GameCode { get; private set; }

        public CustomInputDialog()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            GameCode = InputTextBox.Text;
            this.DialogResult = true; // Set the result to true and close the dialog
            this.Close();
        }
    }
}
