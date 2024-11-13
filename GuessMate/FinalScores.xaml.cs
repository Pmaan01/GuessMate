using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GuessMate
{
    public partial class FinalScores : Window
    {
        public FinalScores(GameSession gameSession)
        {
            InitializeComponent();
            foreach (var player in GameSession.Players)
            {
                FinalScoresList.Items.Add($"{player.Name}: {player.Score}");
            }
        }

        public static void ShowFinalScores()
        {

        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(); // Close the application
        }
    }

}
