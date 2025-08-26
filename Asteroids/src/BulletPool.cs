using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// High-performance bullet pooling system
    /// Manages bullet lifecycle and reduces memory allocations during gameplay
    /// </summary>
    public class BulletPool : IDisposable
    {
        private readonly ObjectPool<PooledBullet> _bulletPool;
        private readonly List<PooledBullet> _activeBullets;
        private readonly int _maxBullets;
        private bool _disposed;

        // Statistics
        public int ActiveBulletCount => _activeBullets.Count;
        public int MaxBullets => _maxBullets;

        public BulletPool(int maxBullets = 50)
        {
            _maxBullets = maxBullets;
            
            // Create pool with preloading
            _bulletPool = new ObjectPool<PooledBullet>(
                maxPoolSize: maxBullets,
                preloadCount: maxBullets / 2);

            _activeBullets = new List<PooledBullet>(maxBullets);

            ErrorManager.LogInfo($"BulletPool initialized with max bullets: {maxBullets}");
        }

        /// <summary>
        /// Spawns a bullet from the pool
        /// </summary>
        public bool SpawnBullet(Vector2 position, Vector2 velocity)
        {
            if (_disposed) return false;

            return ErrorManager.SafeExecute(() =>
            {
                // Check if we've hit the active bullet limit
                if (_activeBullets.Count >= _maxBullets)
                {
                    ErrorManager.LogDebug("Maximum bullets reached, cannot spawn new bullet");
                    return false;
                }

                var bullet = _bulletPool.Rent();
                if (bullet == null)
                {
                    ErrorManager.LogWarning("Failed to rent bullet from pool");
                    return false;
                }

                bullet.Initialize(position, velocity);
                _activeBullets.Add(bullet);
                
                return true;
            }, false, "SpawnBullet");
        }

        /// <summary>
        /// Updates all active bullets and handles cleanup
        /// </summary>
        public void Update()
        {
            if (_disposed) return;

            ErrorManager.SafeExecute(() =>
            {
                for (int i = _activeBullets.Count - 1; i >= 0; i--)
                {
                    var bullet = _activeBullets[i];
                    bullet.Update();
                    
                    if (!bullet.Active)
                    {
                        _activeBullets.RemoveAt(i);
                        _bulletPool.Return(bullet);
                    }
                }
            }, "BulletPool.Update");
        }

        /// <summary>
        /// Draws all active bullets
        /// </summary>
        public void Draw()
        {
            if (_disposed) return;

            ErrorManager.SafeExecute(() =>
            {
                foreach (var bullet in _activeBullets)
                {
                    bullet.Draw();
                }
            }, "BulletPool.Draw");
        }

        /// <summary>
        /// Gets all active bullets for collision detection
        /// </summary>
        public IReadOnlyList<PooledBullet> GetActiveBullets()
        {
            return _activeBullets.AsReadOnly();
        }

        /// <summary>
        /// Deactivates a specific bullet (for collision handling)
        /// </summary>
        public void DeactivateBullet(PooledBullet bullet)
        {
            if (_disposed || bullet == null) return;

            ErrorManager.SafeExecute(() =>
            {
                bullet.Active = false;
                // The bullet will be returned to pool in next Update() call
            }, "DeactivateBullet");
        }

        /// <summary>
        /// Clears all active bullets
        /// </summary>
        public void Clear()
        {
            ErrorManager.SafeExecute(() =>
            {
                foreach (var bullet in _activeBullets)
                {
                    _bulletPool.Return(bullet);
                }
                _activeBullets.Clear();
                
                ErrorManager.LogDebug("BulletPool cleared all active bullets");
            }, "BulletPool.Clear");
        }

        /// <summary>
        /// Gets bullet pool statistics
        /// </summary>
        public BulletPoolStatistics GetStatistics()
        {
            return new BulletPoolStatistics
            {
                ActiveBullets = _activeBullets.Count,
                MaxBullets = _maxBullets,
                PoolStats = _bulletPool.GetStatistics()
            };
        }

        /// <summary>
        /// Optimizes pool size based on usage patterns
        /// </summary>
        public void OptimizePool()
        {
            ErrorManager.SafeExecute(() =>
            {
                _bulletPool.OptimizePoolSize();
                ErrorManager.LogDebug("BulletPool optimized");
            }, "OptimizeBulletPool");
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            Clear();
            _bulletPool?.Dispose();
            
            ErrorManager.LogInfo("BulletPool disposed");
        }
    }

    /// <summary>
    /// Poolable bullet implementation with enhanced features
    /// </summary>
    public class PooledBullet : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public bool Active { get; set; }
        public float TimeToLive { get; set; }
        public float MaxTimeToLive { get; set; }
        
        private const float DEFAULT_TTL = 360f; // 6 seconds at 60 FPS
        private const float BULLET_RADIUS = 2f;

        public float Radius => BULLET_RADIUS;

        public void Initialize(Vector2 position, Vector2 velocity, float timeToLive = DEFAULT_TTL)
        {
            Position = position;
            Velocity = velocity;
            Active = true;
            TimeToLive = timeToLive;
            MaxTimeToLive = timeToLive;
        }

        public void Update()
        {
            if (!Active) return;

            // Update position
            Position += Velocity;
            
            // Decrease time to live
            TimeToLive--;
            
            // Check screen boundaries and TTL
            var screenWidth = Raylib.GetScreenWidth();
            var screenHeight = Raylib.GetScreenHeight();
            
            if (Position.X < -BULLET_RADIUS || Position.X > screenWidth + BULLET_RADIUS ||
                Position.Y < -BULLET_RADIUS || Position.Y > screenHeight + BULLET_RADIUS ||
                TimeToLive <= 0)
            {
                Active = false;
            }
        }

        public void Draw()
        {
            if (!Active) return;

            // Create fade effect based on time to live
            float alpha = 1.0f;
            if (MaxTimeToLive > 0 && TimeToLive < MaxTimeToLive * 0.3f)
            {
                alpha = TimeToLive / (MaxTimeToLive * 0.3f);
            }
            
            var color = new Color(
                Theme.BulletColor.R,
                Theme.BulletColor.G,
                Theme.BulletColor.B,
                (byte)(Theme.BulletColor.A * alpha));
            
            Raylib.DrawCircle((int)Position.X, (int)Position.Y, BULLET_RADIUS, color);
        }

        /// <summary>
        /// Checks collision with a circular object
        /// </summary>
        public bool CheckCollision(Vector2 otherPosition, float otherRadius)
        {
            if (!Active) return false;
            
            return Raylib.CheckCollisionCircles(Position, BULLET_RADIUS, otherPosition, otherRadius);
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Active = false;
            TimeToLive = 0;
            MaxTimeToLive = 0;
        }
    }

    /// <summary>
    /// Bullet pool statistics
    /// </summary>
    public struct BulletPoolStatistics
    {
        public int ActiveBullets { get; set; }
        public int MaxBullets { get; set; }
        public PoolStatistics PoolStats { get; set; }

        public float UtilizationPercentage => MaxBullets > 0 ? (float)ActiveBullets / MaxBullets * 100f : 0f;

        public override string ToString()
        {
            return $"Bullets: {ActiveBullets}/{MaxBullets} ({UtilizationPercentage:F1}% utilization)";
        }
    }
}