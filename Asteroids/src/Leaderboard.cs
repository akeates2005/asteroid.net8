
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Asteroids
{
    /// <summary>
    /// Manages high score persistence and ranking for the Asteroids game.
    /// Handles loading, saving, and maintaining sorted score records in a text file.
    /// </summary>
    public class Leaderboard
    {
        private const string LeaderboardFile = "leaderboard.txt";
        /// <summary>
        /// Collection of all scores sorted in descending order (highest first)
        /// </summary>
        public List<int> Scores { get; private set; }

        /// <summary>
        /// Initializes a new leaderboard instance and loads existing scores from file
        /// </summary>
        public Leaderboard()
        {
            Scores = new List<int>();
            LoadScores();
        }

        /// <summary>
        /// Adds a new score to the leaderboard, maintains descending sort order, and saves to file
        /// </summary>
        /// <param name="score">The score value to add to the leaderboard</param>
        public void AddScore(int score)
        {
            Scores.Add(score);
            Scores = Scores.OrderByDescending(s => s).ToList();
            SaveScores();
        }

        /// <summary>
        /// Loads scores from the leaderboard file and sorts them in descending order.
        /// Creates an empty list if the file doesn't exist or contains invalid data.
        /// </summary>
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

        /// <summary>
        /// Persists the current scores to the leaderboard text file.
        /// Each score is written on a separate line.
        /// </summary>
        private void SaveScores()
        {
            File.WriteAllLines(LeaderboardFile, Scores.Select(s => s.ToString()));
        }
    }
}
