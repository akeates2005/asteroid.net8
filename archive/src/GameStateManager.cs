using System;

namespace Asteroids
{
    /// <summary>
    /// Manages game state including score, level progression, and game flow control.
    /// Centralizes game state logic previously scattered throughout GameProgram.
    /// </summary>
    public class GameStateManager
    {
        /// <summary>
        /// Current player score
        /// </summary>
        public int Score { get; private set; }
        
        /// <summary>
        /// Current game level
        /// </summary>
        public int Level { get; private set; }
        
        /// <summary>
        /// Current player lives
        /// </summary>
        public int Lives { get; private set; }
        
        /// <summary>
        /// Whether the game is over
        /// </summary>
        public bool IsGameOver { get; private set; }
        
        /// <summary>
        /// Whether the current level is complete
        /// </summary>
        public bool IsLevelComplete { get; private set; }
        
        /// <summary>
        /// Whether the game is paused
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Event fired when score changes
        /// </summary>
        public event Action<int>? ScoreChanged;
        
        /// <summary>
        /// Event fired when level changes
        /// </summary>
        public event Action<int>? LevelChanged;
        
        /// <summary>
        /// Event fired when lives change
        /// </summary>
        public event Action<int>? LivesChanged;
        
        /// <summary>
        /// Event fired when game over state changes
        /// </summary>
        public event Action<bool>? GameOverChanged;
        
        /// <summary>
        /// Event fired when level complete state changes
        /// </summary>
        public event Action<bool>? LevelCompleteChanged;
        
        /// <summary>
        /// Event fired when pause state changes
        /// </summary>
        public event Action<bool>? PauseStateChanged;

        public GameStateManager()
        {
            ResetGame();
        }

        /// <summary>
        /// Add points to the current score
        /// </summary>
        /// <param name="points">Points to add</param>
        public void AddScore(int points)
        {
            if (points <= 0) return;
            
            Score += points;
            ScoreChanged?.Invoke(Score);
            
            ErrorManager.LogInfo($"Score increased by {points}, total: {Score}");
        }

        /// <summary>
        /// Start the next level
        /// </summary>
        public void StartNextLevel()
        {
            if (IsGameOver) return;
            
            Level++;
            IsLevelComplete = false;
            
            LevelChanged?.Invoke(Level);
            LevelCompleteChanged?.Invoke(false);
            
            ErrorManager.LogInfo($"Started level {Level}");
        }

        /// <summary>
        /// Mark the current level as complete
        /// </summary>
        public void CompleteLevel()
        {
            if (IsGameOver || IsLevelComplete) return;
            
            IsLevelComplete = true;
            LevelCompleteChanged?.Invoke(true);
            
            ErrorManager.LogInfo($"Level {Level} completed");
        }

        /// <summary>
        /// Lose a life (for player hit)
        /// </summary>
        /// <returns>True if game over, false if lives remaining</returns>
        public bool LoseLife()
        {
            if (IsGameOver || Lives <= 0) return true;
            
            Lives--;
            LivesChanged?.Invoke(Lives);
            
            if (Lives <= 0)
            {
                EndGame();
                ErrorManager.LogInfo("Game over - no lives remaining");
                return true;
            }
            
            ErrorManager.LogInfo($"Life lost, {Lives} remaining");
            return false;
        }

        /// <summary>
        /// Gain an extra life
        /// </summary>
        public void GainLife()
        {
            if (IsGameOver) return;
            
            Lives++;
            LivesChanged?.Invoke(Lives);
            
            ErrorManager.LogInfo($"Extra life gained, total: {Lives}");
        }

        /// <summary>
        /// End the game
        /// </summary>
        public void EndGame()
        {
            if (IsGameOver) return;
            
            IsGameOver = true;
            GameOverChanged?.Invoke(true);
            
            ErrorManager.LogInfo($"Game ended with score: {Score}, level: {Level}");
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            IsPaused = !IsPaused;
            PauseStateChanged?.Invoke(IsPaused);
            
            ErrorManager.LogInfo($"Game {(IsPaused ? "paused" : "resumed")}");
        }

        /// <summary>
        /// Set pause state explicitly
        /// </summary>
        /// <param name="paused">Whether to pause the game</param>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            
            IsPaused = paused;
            PauseStateChanged?.Invoke(IsPaused);
            
            ErrorManager.LogInfo($"Game {(IsPaused ? "paused" : "resumed")}");
        }

        /// <summary>
        /// Reset the game to initial state
        /// </summary>
        public void ResetGame()
        {
            var wasGameOver = IsGameOver;
            var wasLevelComplete = IsLevelComplete;
            var wasPaused = IsPaused;
            
            Score = 0;
            Level = 1;
            Lives = 3; // Start with 3 lives
            IsGameOver = false;
            IsLevelComplete = false;
            IsPaused = false;
            
            // Fire events for changed states
            ScoreChanged?.Invoke(Score);
            LevelChanged?.Invoke(Level);
            LivesChanged?.Invoke(Lives);
            
            if (wasGameOver) GameOverChanged?.Invoke(false);
            if (wasLevelComplete) LevelCompleteChanged?.Invoke(false);
            if (wasPaused) PauseStateChanged?.Invoke(false);
            
            ErrorManager.LogInfo("Game state reset");
        }

        /// <summary>
        /// Get current game state summary
        /// </summary>
        /// <returns>Game state summary</returns>
        public GameStateInfo GetCurrentState()
        {
            return new GameStateInfo
            {
                Score = Score,
                Level = Level,
                Lives = Lives,
                IsGameOver = IsGameOver,
                IsLevelComplete = IsLevelComplete,
                IsPaused = IsPaused
            };
        }
    }

    /// <summary>
    /// Immutable snapshot of game state
    /// </summary>
    public struct GameStateInfo
    {
        public int Score { get; init; }
        public int Level { get; init; }
        public int Lives { get; init; }
        public bool IsGameOver { get; init; }
        public bool IsLevelComplete { get; init; }
        public bool IsPaused { get; init; }
    }
}