using System;
using System.Collections.Generic;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Manages level progression, asteroid spawning, and difficulty scaling.
    /// Centralizes level logic previously embedded in GameProgram.
    /// </summary>
    public class LevelManager
    {
        private readonly EntityManager _entityManager;
        private readonly Random _random;
        private readonly GameStateManager _gameState;

        /// <summary>
        /// Event fired when a new level starts
        /// </summary>
        public event Action<int>? LevelStarted;
        
        /// <summary>
        /// Event fired when level completion is checked
        /// </summary>
        public event Action<bool>? LevelCompletionChecked;

        public LevelManager(EntityManager entityManager, GameStateManager gameState)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _random = new Random();
        }

        /// <summary>
        /// Start a specific level
        /// </summary>
        /// <param name="level">Level number to start</param>
        /// <param name="playerPosition">Player position to avoid spawning asteroids too close</param>
        public void StartLevel(int level, Vector2 playerPosition)
        {
            try
            {
                ClearLevelEntities();
                SpawnAsteroids(level, playerPosition);
                
                // Update theme for new level
                DynamicTheme.UpdateLevel(level);
                
                LevelStarted?.Invoke(level);
                ErrorManager.LogInfo($"Started level {level} with {GetAsteroidCount(level)} asteroids");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error starting level {level}", ex);
            }
        }

        /// <summary>
        /// Check if the current level is complete
        /// </summary>
        /// <returns>True if level is complete</returns>
        public bool CheckLevelCompletion()
        {
            var asteroids = _entityManager.GetEntitiesOfType<AsteroidEntity>();
            bool isComplete = !asteroids.Any(a => a.Active);
            
            if (isComplete && !_gameState.IsLevelComplete)
            {
                _gameState.CompleteLevel();
            }
            
            LevelCompletionChecked?.Invoke(isComplete);
            return isComplete;
        }

        /// <summary>
        /// Get the number of asteroids for a given level
        /// </summary>
        /// <param name="level">Level number</param>
        /// <returns>Number of asteroids to spawn</returns>
        public int GetAsteroidCount(int level)
        {
            return GameConstants.BASE_ASTEROIDS_PER_LEVEL + 
                   (level - 1) * GameConstants.ASTEROIDS_INCREMENT_PER_LEVEL;
        }

        /// <summary>
        /// Get level difficulty multiplier
        /// </summary>
        /// <param name="level">Level number</param>
        /// <returns>Difficulty multiplier</returns>
        public float GetDifficultyMultiplier(int level)
        {
            return 1f + (level - 1) * GameConstants.ASTEROID_SPEED_MULTIPLIER;
        }

        /// <summary>
        /// Get asteroid change interval for a level
        /// </summary>
        /// <param name="level">Level number</param>
        /// <returns>Change interval multiplier</returns>
        public float GetChangeIntervalMultiplier(int level)
        {
            return Math.Max(0.2f, 1f - (level - 1) * GameConstants.ASTEROID_CHANGE_INTERVAL_MULTIPLIER);
        }

        /// <summary>
        /// Create a single asteroid with level-appropriate difficulty
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="size">Asteroid size</param>
        /// <param name="level">Current level</param>
        /// <returns>Created asteroid entity</returns>
        public AsteroidEntity CreateAsteroid(Vector2 position, AsteroidSize size, int level)
        {
            // Generate random velocity
            Vector2 velocity = new Vector2(
                (float)(_random.NextDouble() * GameConstants.PARTICLE_VELOCITY_RANGE - GameConstants.PARTICLE_VELOCITY_RANGE / 2),
                (float)(_random.NextDouble() * GameConstants.PARTICLE_VELOCITY_RANGE - GameConstants.PARTICLE_VELOCITY_RANGE / 2)
            );

            // Apply level-based speed scaling
            float speedMultiplier = GetDifficultyMultiplier(level);
            velocity *= speedMultiplier;

            var asteroid = new AsteroidEntity(_entityManager.GetNextEntityId(), position, velocity, size, _random, level);
            return asteroid;
        }

        private void SpawnAsteroids(int level, Vector2 playerPosition)
        {
            int asteroidCount = GetAsteroidCount(level);
            const float MIN_DISTANCE_FROM_PLAYER = 100f;

            for (int i = 0; i < asteroidCount; i++)
            {
                AsteroidSize size = (AsteroidSize)_random.Next(0, 3);
                Vector2 position;
                int attempts = 0;
                const int MAX_ATTEMPTS = 50;

                // Find a safe spawn position
                do
                {
                    position = new Vector2(
                        _random.Next(0, GameConstants.SCREEN_WIDTH),
                        _random.Next(0, GameConstants.SCREEN_HEIGHT)
                    );
                    attempts++;
                } while (Vector2.Distance(position, playerPosition) < MIN_DISTANCE_FROM_PLAYER && attempts < MAX_ATTEMPTS);

                // If we couldn't find a safe position after many attempts, use the last position
                if (attempts >= MAX_ATTEMPTS)
                {
                    ErrorManager.LogWarning($"Could not find safe spawn position for asteroid {i}, using position at distance {Vector2.Distance(position, playerPosition):F1}");
                }

                var asteroid = CreateAsteroid(position, size, level);
                _entityManager.AddEntity(asteroid);
            }
        }

        private void ClearLevelEntities()
        {
            // Clear all asteroids from previous level
            var asteroids = _entityManager.GetEntitiesOfType<AsteroidEntity>();
            foreach (var asteroid in asteroids.ToList())
            {
                _entityManager.RemoveEntity(asteroid.Id);
            }
        }
    }

    /// <summary>
    /// Level configuration for customizable level design
    /// </summary>
    public struct LevelConfiguration
    {
        public int AsteroidCount { get; init; }
        public float SpeedMultiplier { get; init; }
        public float ChangeIntervalMultiplier { get; init; }
        public Dictionary<AsteroidSize, float> SizeDistribution { get; init; }
        public bool HasSpecialFeatures { get; init; }
    }
}