
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Asteroids
{
    class Leaderboard
    {
        private const string LeaderboardFile = "leaderboard.txt";
        public List<int> Scores { get; private set; }

        public Leaderboard()
        {
            Scores = new List<int>();
            LoadScores();
        }

        public void AddScore(int score)
        {
            Scores.Add(score);
            Scores = Scores.OrderByDescending(s => s).ToList();
            SaveScores();
        }

        private void LoadScores()
        {
            if (File.Exists(LeaderboardFile))
            {
                var scores = File.ReadAllLines(LeaderboardFile);
                foreach (var score in scores)
                {
                    if (int.TryParse(score, out int value))
                    {
                        Scores.Add(value);
                    }
                }
                Scores = Scores.OrderByDescending(s => s).ToList();
            }
        }

        private void SaveScores()
        {
            File.WriteAllLines(LeaderboardFile, Scores.Select(s => s.ToString()));
        }
    }
}
