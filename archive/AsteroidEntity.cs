using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Asteroid entity implementation following the IGameEntity interface.
    /// Manages asteroid movement, collision detection, and rendering with procedural shapes.
    /// </summary>
    public class AsteroidEntity : IGameEntity
    {
        public int Id { get; private set; }
        public Vector2 Position { get; set; }
        public bool Active { get; set; }

        /// <summary>
        /// Current movement velocity
        /// </summary>
        public Vector2 Velocity { get; set; }
        
        /// <summary>
        /// Size category of the asteroid
        /// </summary>
        public AsteroidSize AsteroidSize { get; set; }
        
        /// <summary>
        /// Collision radius based on size
        /// </summary>
        public float Radius { get; set; }

        private AsteroidShape _shape;
        private readonly Random _random;
        private readonly int _level;
        private float _timer;
        private float _changeInterval;
        private readonly int _shapeHash; // For consistent shape rendering

        public AsteroidEntity(int id, Vector2 position, Vector2 velocity, AsteroidSize size, Random random, int level)
        {
            Id = id;
            Position = position;
            AsteroidSize = size;
            Active = true;
            _random = random;
            _level = level;
            _shapeHash = id; // Use ID as shape seed for consistency

            InitializeSize();
            InitializeMovement(velocity, level);
            InitializeShape();

            ErrorManager.LogInfo($"Asteroid entity {Id} created: Size={size}, Radius={Radius}, Level={level}");
        }

        public void Initialize()
        {
            // Entity is already initialized in constructor
        }

        public void Update(float deltaTime)
        {
            if (!Active) return;

            UpdateMovement(deltaTime);
            HandleScreenWrapping();
        }

        public void Render(IRenderer renderer)
        {
            if (!Active) return;

            Color asteroidColor = DynamicTheme.GetAsteroidColor(AsteroidSize);
            int lodLevel = CalculateLOD();
            
            renderer.RenderAsteroid(Position, Radius, asteroidColor, _shapeHash, lodLevel);
        }

        public float GetCollisionRadius()
        {
            return Radius;
        }

        public void Dispose()
        {
            ErrorManager.LogInfo($"Asteroid entity {Id} disposed");
        }

        /// <summary>
        /// Update asteroid velocity with periodic changes
        /// </summary>
        /// <param name="newVelocity">New velocity vector</param>
        public void UpdateVelocity(Vector2 newVelocity)
        {
            Velocity = newVelocity;
        }

        /// <summary>
        /// Get the procedural shape of this asteroid
        /// </summary>
        /// <returns>Asteroid shape</returns>
        public AsteroidShape GetShape()
        {
            return _shape;
        }

        /// <summary>
        /// Create child asteroids when this asteroid is destroyed
        /// </summary>
        /// <returns>Collection of child asteroids, empty if no children should be created</returns>
        public AsteroidEntity[] CreateChildren()
        {
            // Implementation for asteroid fragmentation (future enhancement)
            // For now, return empty array as this feature is not in Phase 1
            return Array.Empty<AsteroidEntity>();
        }

        /// <summary>
        /// Get level-adjusted score value for this asteroid
        /// </summary>
        /// <returns>Score points for destroying this asteroid</returns>
        public int GetScoreValue()
        {
            int baseValue = AsteroidSize switch
            {
                AsteroidSize.Large => 20,
                AsteroidSize.Medium => 50,
                AsteroidSize.Small => 100,
                _ => 10
            };

            // Apply level multiplier
            float levelMultiplier = 1f + (_level - 1) * 0.1f;
            return (int)(baseValue * levelMultiplier);
        }

        private void InitializeSize()
        {
            Radius = AsteroidSize switch
            {
                AsteroidSize.Large => GameConstants.LARGE_ASTEROID_RADIUS,
                AsteroidSize.Medium => GameConstants.MEDIUM_ASTEROID_RADIUS,
                AsteroidSize.Small => GameConstants.SMALL_ASTEROID_RADIUS,
                _ => GameConstants.MEDIUM_ASTEROID_RADIUS
            };
        }

        private void InitializeMovement(Vector2 baseVelocity, int level)
        {
            // Apply level-based speed scaling
            float speedMultiplier = 1f + (level - 1) * GameConstants.ASTEROID_SPEED_MULTIPLIER;
            Velocity = baseVelocity * speedMultiplier;

            // Set up velocity change intervals
            float changeIntervalMultiplier = Math.Max(0.2f, 1f - (level - 1) * GameConstants.ASTEROID_CHANGE_INTERVAL_MULTIPLIER);
            _changeInterval = (float)(_random.NextDouble() * 5 + 2) * changeIntervalMultiplier;
            _timer = _changeInterval;
        }

        private void InitializeShape()
        {
            var shapeRandom = new Random(_shapeHash);
            int vertexCount = shapeRandom.Next(GameConstants.MIN_ASTEROID_POINTS, GameConstants.MAX_ASTEROID_POINTS + 1);
            _shape = new AsteroidShape(vertexCount, Radius, shapeRandom);
        }

        private void UpdateMovement(float deltaTime)
        {
            _timer -= deltaTime;

            if (_timer <= 0)
            {
                // Generate new random velocity
                Velocity = new Vector2(
                    (float)(_random.NextDouble() * GameConstants.PARTICLE_VELOCITY_RANGE - GameConstants.PARTICLE_VELOCITY_RANGE / 2),
                    (float)(_random.NextDouble() * GameConstants.PARTICLE_VELOCITY_RANGE - GameConstants.PARTICLE_VELOCITY_RANGE / 2)
                );

                // Apply level scaling
                float speedMultiplier = 1f + (_level - 1) * GameConstants.ASTEROID_SPEED_MULTIPLIER;
                Velocity *= speedMultiplier;

                _timer = _changeInterval;
            }

            Position += Velocity;
        }

        private void HandleScreenWrapping()
        {
            var screenBounds = new Vector2(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT);
            
            if (Position.X < -Radius) Position = new Vector2(screenBounds.X + Radius, Position.Y);
            if (Position.X > screenBounds.X + Radius) Position = new Vector2(-Radius, Position.Y);
            if (Position.Y < -Radius) Position = new Vector2(Position.X, screenBounds.Y + Radius);
            if (Position.Y > screenBounds.Y + Radius) Position = new Vector2(Position.X, -Radius);
        }

        private int CalculateLOD()
        {
            // Simple distance-based LOD calculation
            // This could be enhanced with camera distance in 3D mode
            Vector2 screenCenter = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
            float distanceFromCenter = Vector2.Distance(Position, screenCenter);
            float maxDistance = MathF.Sqrt(GameConstants.SCREEN_WIDTH * GameConstants.SCREEN_WIDTH + 
                                          GameConstants.SCREEN_HEIGHT * GameConstants.SCREEN_HEIGHT) / 2f;

            float normalizedDistance = distanceFromCenter / maxDistance;

            return normalizedDistance switch
            {
                < 0.3f => 0, // High detail
                < 0.7f => 1, // Medium detail
                _ => 2       // Low detail
            };
        }
    }
}