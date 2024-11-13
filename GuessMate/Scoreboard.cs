using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuessMate
{
    public class Scoreboard
    {
        public List<ScoreEntry> Entries { get; set; }

        public Scoreboard()
        {
            Entries = new List<ScoreEntry>();
        }

        public void UpdateScore(Player player, int points)
        {
            player.Score += points;
        }
    }


    public class ScoreEntry
    {
        public Player Player { get; set; }
        public int Score { get; set; }

        public ScoreEntry(Player player, int score)
        {
            Player = player;
            Score = score;
        }
    }

}
