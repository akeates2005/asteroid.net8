using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Enhanced game features and mechanics for Phase 2
    /// </summary>
    public static class GameEnhancements
    {
        /// <summary>
        /// Split asteroid into smaller pieces when destroyed
        /// </summary>
        public static List<Asteroid> SplitAsteroid(Asteroid asteroid, Vector2 impactPoint, Random random)
        {
            var newAsteroids = new List<Asteroid>();
            
            if (asteroid.AsteroidSize == AsteroidSize.Small)
            {
                // Small asteroids don't split further
                return newAsteroids;
            }
            
            var newSize = asteroid.AsteroidSize == AsteroidSize.Large 
                ? AsteroidSize.Medium 
                : AsteroidSize.Small;
                
            int pieces = random.Next(2, 4); // Create 2-3 pieces
            
            for (int i = 0; i < pieces; i++)
            {
                // Calculate split direction (away from impact)
                float angle = (float)(random.NextDouble() * 2 * Math.PI);
                Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                
                // Add some randomness to the split direction
                Vector2 splitDirection = Vector2.Normalize(asteroid.Position - impactPoint);
                direction = Vector2.Lerp(direction, splitDirection, 0.6f);
                
                float speed = (float)(random.NextDouble() * 3 + 1);
                Vector2 velocity = direction * speed;
                
                // Position slightly offset from original asteroid
                Vector2 offset = direction * (asteroid.Radius * 0.3f);
                Vector2 newPosition = asteroid.Position + offset;
                
                // Create new asteroid (you'll need to modify the Asteroid constructor or create a method for this)
                var newAsteroid = new Asteroid(newPosition, velocity, newSize, random, 1); // Assume level 1 for now
                newAsteroids.Add(newAsteroid);
            }
            
            return newAsteroids;
        }
        
        /// <summary>
        /// Create explosion effect when asteroid is destroyed
        /// </summary>
        public static List<ExplosionParticle> CreateExplosionEffect(Vector2 position, float intensity, Color baseColor, Random random)
        {
            var particles = new List<ExplosionParticle>();
            int particleCount = (int)(intensity * 8); // Reduced particle count to minimize dots
            
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (float)(random.NextDouble() * 2 * Math.PI);
                float speed = (float)(random.NextDouble() * intensity * 3 + 1);
                Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
                
                // Vary the position slightly
                Vector2 offset = new Vector2(
                    (float)(random.NextDouble() * 10 - 5),
                    (float)(random.NextDouble() * 10 - 5)
                );
                
                int lifespan = random.Next(5, 15); // 0.08 to 0.25 seconds at 60 FPS
                
                // Vary color slightly
                Color particleColor = new Color(
                    Math.Min(255, baseColor.R + random.Next(-30, 31)),
                    Math.Min(255, baseColor.G + random.Next(-30, 31)),
                    Math.Min(255, baseColor.B + random.Next(-30, 31)),
                    baseColor.A
                );
                
                particles.Add(new ExplosionParticle(position + offset, velocity, lifespan, particleColor));
            }
            
            return particles;
        }
        
        /// <summary>
        /// Enhanced asteroid generation with better distribution
        /// </summary>
        public static List<Asteroid> GenerateAsteroidField(int screenWidth, int screenHeight, int count, int level, Random random, Vector2? playerPosition = null)
        {
            var asteroids = new List<Asteroid>();
            float safeZone = 100f; // Safe zone around player
            
            for (int i = 0; i < count; i++)
            {
                Vector2 position;
                int attempts = 0;
                
                // Find a position that's not too close to the player
                do
                {
                    position = new Vector2(
                        random.Next(0, screenWidth),
                        random.Next(0, screenHeight)
                    );
                    attempts++;
                } while (playerPosition.HasValue && 
                         Vector2.Distance(position, playerPosition.Value) < safeZone && 
                         attempts < 10);
                
                // Random velocity
                Vector2 velocity = new Vector2(
                    (float)(random.NextDouble() * 4 - 2),
                    (float)(random.NextDouble() * 4 - 2)
                );
                
                // Larger asteroids are rarer
                AsteroidSize size = random.NextDouble() switch
                {
                    < 0.2 => AsteroidSize.Large,
                    < 0.6 => AsteroidSize.Medium,
                    _ => AsteroidSize.Small
                };
                
                asteroids.Add(new Asteroid(position, velocity, size, random, level));
            }
            
            return asteroids;
        }
        
        /// <summary>
        /// Calculate score based on asteroid size and level
        /// </summary>
        public static int CalculateAsteroidScore(AsteroidSize size, int level)
        {
            int baseScore = size switch
            {
                AsteroidSize.Large => 100,
                AsteroidSize.Medium => 50,
                AsteroidSize.Small => 25,
                _ => 10
            };
            
            // Higher levels give more points
            float levelMultiplier = 1 + (level - 1) * 0.1f;
            return (int)(baseScore * levelMultiplier);
        }
        
        /// <summary>
        /// Enhanced power-up system
        /// </summary>
        public static bool ShouldSpawnPowerUp(Random random, int asteroidCount, float spawnChance = 0.15f)
        {
            // Higher chance when fewer asteroids remain
            float adjustedChance = spawnChance + (1 - (float)asteroidCount / 10) * 0.1f;
            return random.NextDouble() < adjustedChance;
        }
        
        /// <summary>
        /// Level progression logic
        /// </summary>
        public static bool ShouldAdvanceLevel(int remainingAsteroids)
        {
            return remainingAsteroids == 0;
        }
        
        /// <summary>
        /// Calculate next level asteroid count
        /// </summary>
        public static int GetAsteroidCountForLevel(int level)
        {
            return Math.Min(4 + level * 2, 20); // Cap at 20 asteroids
        }
        
        /// <summary>
        /// Enhanced collision response with realistic physics
        /// </summary>
        public static void ApplyCollisionResponse(Asteroid asteroidA, Asteroid asteroidB)
        {
            // Calculate collision normal
            Vector2 normal = Vector2.Normalize(asteroidB.Position - asteroidA.Position);
            
            // Relative velocity
            Vector2 relativeVelocity = asteroidB.Velocity - asteroidA.Velocity;
            
            // Relative velocity along normal
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
            
            // Objects are separating
            if (velocityAlongNormal > 0) return;
            
            // Assume equal mass for simplicity
            float restitution = 0.8f; // Bounciness
            float impulse = -(1 + restitution) * velocityAlongNormal / 2;
            
            // Apply impulse
            Vector2 impulseVector = impulse * normal;
            asteroidA.Velocity -= impulseVector;
            asteroidB.Velocity += impulseVector;
            
            // Separate overlapping objects
            float overlap = asteroidA.Radius + asteroidB.Radius - Vector2.Distance(asteroidA.Position, asteroidB.Position);
            if (overlap > 0)
            {
                Vector2 separation = normal * (overlap / 2 + 0.1f);
                asteroidA.Position -= separation;
                asteroidB.Position += separation;
            }
        }
        
        /// <summary>
        /// Screen wrapping with smooth transition
        /// </summary>
        public static Vector2 ApplyScreenWrapping(Vector2 position, float radius, int screenWidth, int screenHeight)
        {
            Vector2 wrappedPosition = position;
            
            if (position.X < -radius)
                wrappedPosition.X = screenWidth + radius;
            else if (position.X > screenWidth + radius)
                wrappedPosition.X = -radius;
                
            if (position.Y < -radius)
                wrappedPosition.Y = screenHeight + radius;
            else if (position.Y > screenHeight + radius)
                wrappedPosition.Y = -radius;
                
            return wrappedPosition;
        }
        
        /// <summary>
        /// Enhanced visual effects with screen shake
        /// </summary>
        public static Vector2 CalculateScreenShake(float intensity, float duration, float elapsed, Random random)
        {
            if (elapsed >= duration) return Vector2.Zero;
            
            float progress = elapsed / duration;
            float currentIntensity = intensity * (1 - progress); // Decay over time
            
            return new Vector2(
                (float)(random.NextDouble() * 2 - 1) * currentIntensity,
                (float)(random.NextDouble() * 2 - 1) * currentIntensity
            );
        }
    }
}