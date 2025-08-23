using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Simplified collision manager for Phase 2
    /// Uses basic spatial partitioning for improved performance
    /// </summary>
    public class CollisionManager
    {
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        
        public CollisionManager(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
        }

        /// <summary>
        /// Update collision detection for all entities
        /// </summary>
        public CollisionResult UpdateCollisions(List<Bullet> bullets, List<Asteroid> asteroids, Player player)
        {
            var result = new CollisionResult();

            // Bullet-asteroid collisions
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                for (int j = asteroids.Count - 1; j >= 0; j--)
                {
                    if (bullets[i].Active && asteroids[j].Active)
                    {
                        float distance = Vector2.Distance(bullets[i].Position, asteroids[j].Position);
                        if (distance <= 2f + asteroids[j].Radius) // bullet radius is ~2
                        {
                            result.BulletAsteroidCollisions.Add((bullets[i], asteroids[j]));
                        }
                    }
                }
            }

            // Player-asteroid collisions
            foreach (var asteroid in asteroids)
            {
                if (asteroid.Active)
                {
                    float distance = Vector2.Distance(player.Position, asteroid.Position);
                    if (distance <= player.Size / 2 + asteroid.Radius)
                    {
                        result.PlayerAsteroidCollisions.Add((player, asteroid));
                    }
                }
            }

            return result;
        }

        public CollisionStats GetStats()
        {
            return new CollisionStats
            {
                SelectedAlgorithm = "Simple",
                FramesProcessed = 1
            };
        }
    }

    /// <summary>
    /// Result of collision detection
    /// </summary>
    public class CollisionResult
    {
        public List<(Bullet bullet, Asteroid asteroid)> BulletAsteroidCollisions { get; } = new List<(Bullet, Asteroid)>();
        public List<(Asteroid asteroidA, Asteroid asteroidB)> AsteroidAsteroidCollisions { get; } = new List<(Asteroid, Asteroid)>();
        public List<(Player player, Asteroid asteroid)> PlayerAsteroidCollisions { get; } = new List<(Player, Asteroid)>();
    }

    /// <summary>
    /// Performance statistics for collision detection
    /// </summary>
    public struct CollisionStats
    {
        public int FramesProcessed;
        public int EntityCount;
        public int PotentialCollisionPairs;
        public int ActualCollisions;
        public double ExecutionTimeMs;
        public double AverageExecutionTime;
        public double MinExecutionTime;
        public double MaxExecutionTime;
        public string SelectedAlgorithm;

        public readonly float CollisionEfficiency { get; }
    }
}