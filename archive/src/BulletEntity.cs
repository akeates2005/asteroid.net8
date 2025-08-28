using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Bullet entity implementation following the IGameEntity interface.
    /// Integrates with the modular entity management system and collision detection.
    /// </summary>
    public class BulletEntity : IGameEntity, IDisposable
    {
        private Vector2 _position;
        private Vector2 _velocity;
        private bool _active;
        private readonly float _speed;
        private readonly float _lifetime;
        private float _timeAlive;

        /// <summary>
        /// Unique identifier for the bullet entity
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Current position of the bullet
        /// </summary>
        public Vector2 Position 
        { 
            get => _position; 
            set => _position = value; 
        }

        /// <summary>
        /// Whether the bullet is active and should be processed
        /// </summary>
        public bool Active 
        { 
            get => _active; 
            set => _active = value; 
        }

        /// <summary>
        /// Current velocity of the bullet
        /// </summary>
        public Vector2 Velocity 
        { 
            get => _velocity; 
            set => _velocity = value; 
        }

        /// <summary>
        /// Create a new bullet entity
        /// </summary>
        /// <param name="id">Unique entity ID</param>
        /// <param name="position">Starting position</param>
        /// <param name="velocity">Movement velocity</param>
        /// <param name="speed">Bullet speed (optional override)</param>
        /// <param name="lifetime">Bullet lifetime in seconds</param>
        public BulletEntity(int id, Vector2 position, Vector2 velocity, float speed = GameConstants.BULLET_SPEED, float lifetime = GameConstants.BULLET_LIFETIME)
        {
            Id = id;
            _position = position;
            _velocity = Vector2.Normalize(velocity) * speed;
            _speed = speed;
            _lifetime = lifetime;
            _timeAlive = 0f;
            _active = true;
        }

        /// <summary>
        /// Initialize the bullet entity
        /// </summary>
        public void Initialize()
        {
            _active = true;
            _timeAlive = 0f;
            ErrorManager.LogInfo($"BulletEntity {Id} initialized at {Position}");
        }

        /// <summary>
        /// Update bullet position and lifetime
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(float deltaTime)
        {
            if (!_active) return;

            _timeAlive += deltaTime;

            // Update position
            _position += _velocity * deltaTime;

            // Screen wrapping (following Asteroids game mechanics)
            if (_position.X < 0) _position.X = GameConstants.SCREEN_WIDTH;
            if (_position.X > GameConstants.SCREEN_WIDTH) _position.X = 0;
            if (_position.Y < 0) _position.Y = GameConstants.SCREEN_HEIGHT;
            if (_position.Y > GameConstants.SCREEN_HEIGHT) _position.Y = 0;

            // Deactivate after lifetime expires
            if (_timeAlive >= _lifetime)
            {
                _active = false;
            }
        }

        /// <summary>
        /// Render the bullet using the provided renderer
        /// </summary>
        /// <param name="renderer">Renderer to use for drawing</param>
        public void Render(IRenderer renderer)
        {
            if (!_active || renderer == null) return;

            try
            {
                // Use the standardized RenderBullet method from IRenderer
                Color bulletColor = DynamicTheme.GetBulletColor();
                renderer.RenderBullet(_position, bulletColor);
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error rendering BulletEntity {Id}", ex);
            }
        }

        /// <summary>
        /// Get collision radius for the bullet
        /// </summary>
        /// <returns>Bullet collision radius</returns>
        public float GetCollisionRadius()
        {
            return GameConstants.BULLET_SIZE;
        }

        /// <summary>
        /// Mark bullet as hit/destroyed
        /// </summary>
        public void OnHit()
        {
            _active = false;
            ErrorManager.LogInfo($"BulletEntity {Id} hit target");
        }

        /// <summary>
        /// Reset bullet for object pooling
        /// </summary>
        /// <param name="position">New position</param>
        /// <param name="velocity">New velocity</param>
        public void Reset(Vector2 position, Vector2 velocity)
        {
            _position = position;
            _velocity = Vector2.Normalize(velocity) * _speed;
            _timeAlive = 0f;
            _active = true;
        }

        /// <summary>
        /// Dispose bullet resources
        /// </summary>
        public void Dispose()
        {
            _active = false;
            // Bullet entities don't have complex resources to dispose
        }

        /// <summary>
        /// Get bullet statistics for debugging
        /// </summary>
        /// <returns>Bullet state information</returns>
        public BulletInfo GetBulletInfo()
        {
            return new BulletInfo
            {
                Id = Id,
                Position = _position,
                Velocity = _velocity,
                Active = _active,
                TimeAlive = _timeAlive,
                RemainingLifetime = Math.Max(0, _lifetime - _timeAlive)
            };
        }
    }

    /// <summary>
    /// Bullet state information for debugging and diagnostics
    /// </summary>
    public struct BulletInfo
    {
        public int Id { get; init; }
        public Vector2 Position { get; init; }
        public Vector2 Velocity { get; init; }
        public bool Active { get; init; }
        public float TimeAlive { get; init; }
        public float RemainingLifetime { get; init; }
    }
}