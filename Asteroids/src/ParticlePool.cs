using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Specialized particle pooling system for engine and explosion particles
    /// Provides high-performance particle management with automatic lifecycle handling
    /// </summary>
    public class ParticlePool : IDisposable
    {
        private readonly ObjectPool<PooledEngineParticle> _engineParticlePool;
        private readonly ObjectPool<PooledExplosionParticle> _explosionParticlePool;
        private readonly List<PooledEngineParticle> _activeEngineParticles;
        private readonly List<PooledExplosionParticle> _activeExplosionParticles;
        
        private bool _disposed;

        // Configuration
        public int MaxEngineParticles { get; private set; }
        public int MaxExplosionParticles { get; private set; }

        public ParticlePool(int maxEngineParticles = 200, int maxExplosionParticles = 500)
        {
            MaxEngineParticles = maxEngineParticles;
            MaxExplosionParticles = maxExplosionParticles;

            // Create pools with preloading
            _engineParticlePool = new ObjectPool<PooledEngineParticle>(
                maxPoolSize: maxEngineParticles,
                preloadCount: maxEngineParticles / 4);

            _explosionParticlePool = new ObjectPool<PooledExplosionParticle>(
                maxPoolSize: maxExplosionParticles,
                preloadCount: maxExplosionParticles / 4);

            _activeEngineParticles = new List<PooledEngineParticle>(maxEngineParticles);
            _activeExplosionParticles = new List<PooledExplosionParticle>(maxExplosionParticles);

            ErrorManager.LogInfo($"ParticlePool initialized - Engine: {maxEngineParticles}, Explosion: {maxExplosionParticles}");
        }

        /// <summary>
        /// Spawns an engine particle
        /// </summary>
        public bool SpawnEngineParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            if (_disposed) return false;

            return ErrorManager.SafeExecute(() =>
            {
                var particle = _engineParticlePool.Rent();
                if (particle == null) return false;

                particle.Initialize(position, velocity, lifespan, color);
                _activeEngineParticles.Add(particle);
                
                return true;
            }, false, "SpawnEngineParticle");
        }

        /// <summary>
        /// Spawns multiple engine particles (for engine thrust)
        /// </summary>
        public int SpawnEngineParticles(Vector2 basePosition, Vector2 baseVelocity, int count, float spread, float lifespan, Color color, Random random)
        {
            if (_disposed || count <= 0) return 0;

            return ErrorManager.SafeExecute(() =>
            {
                int spawned = 0;
                
                for (int i = 0; i < count; i++)
                {
                    // Add random spread to position and velocity
                    var spreadX = (float)(random.NextDouble() - 0.5) * spread;
                    var spreadY = (float)(random.NextDouble() - 0.5) * spread;
                    
                    var position = basePosition + new Vector2(spreadX, spreadY);
                    var velocity = baseVelocity + new Vector2(spreadX * 0.5f, spreadY * 0.5f);
                    
                    if (SpawnEngineParticle(position, velocity, lifespan, color))
                    {
                        spawned++;
                    }
                    else
                    {
                        break; // Pool exhausted
                    }
                }
                
                return spawned;
            }, 0, "SpawnEngineParticles");
        }

        /// <summary>
        /// Spawns an explosion particle
        /// </summary>
        public bool SpawnExplosionParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            if (_disposed) return false;

            return ErrorManager.SafeExecute(() =>
            {
                var particle = _explosionParticlePool.Rent();
                if (particle == null) return false;

                particle.Initialize(position, velocity, lifespan, color);
                _activeExplosionParticles.Add(particle);
                
                return true;
            }, false, "SpawnExplosionParticle");
        }

        /// <summary>
        /// Spawns multiple explosion particles (for explosions)
        /// </summary>
        public int SpawnExplosion(Vector2 position, int particleCount, float maxVelocity, float lifespan, Color color, Random random)
        {
            if (_disposed || particleCount <= 0) return 0;

            return ErrorManager.SafeExecute(() =>
            {
                int spawned = 0;
                
                for (int i = 0; i < particleCount; i++)
                {
                    // Generate random velocity in all directions
                    var angle = random.NextDouble() * Math.PI * 2;
                    var speed = random.NextDouble() * maxVelocity;
                    var velocity = new Vector2((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed));
                    
                    // Add slight position variance
                    var particlePos = position + new Vector2(
                        (float)(random.NextDouble() - 0.5) * 2,
                        (float)(random.NextDouble() - 0.5) * 2);
                    
                    if (SpawnExplosionParticle(particlePos, velocity, lifespan, color))
                    {
                        spawned++;
                    }
                    else
                    {
                        break; // Pool exhausted
                    }
                }
                
                return spawned;
            }, 0, "SpawnExplosion");
        }

        /// <summary>
        /// Updates all active particles and returns expired ones to pools
        /// </summary>
        public void Update()
        {
            if (_disposed) return;

            ErrorManager.SafeExecute(() =>
            {
                // Update engine particles
                for (int i = _activeEngineParticles.Count - 1; i >= 0; i--)
                {
                    var particle = _activeEngineParticles[i];
                    particle.Update();
                    
                    if (particle.Lifespan <= 0)
                    {
                        _activeEngineParticles.RemoveAt(i);
                        _engineParticlePool.Return(particle);
                    }
                }

                // Update explosion particles
                for (int i = _activeExplosionParticles.Count - 1; i >= 0; i--)
                {
                    var particle = _activeExplosionParticles[i];
                    particle.Update();
                    
                    if (particle.Lifespan <= 0)
                    {
                        _activeExplosionParticles.RemoveAt(i);
                        _explosionParticlePool.Return(particle);
                    }
                }
            }, "ParticlePool.Update");
        }

        /// <summary>
        /// Draws all active particles
        /// </summary>
        public void Draw()
        {
            if (_disposed) return;

            ErrorManager.SafeExecute(() =>
            {
                // Draw engine particles
                foreach (var particle in _activeEngineParticles)
                {
                    particle.Draw();
                }

                // Draw explosion particles
                foreach (var particle in _activeExplosionParticles)
                {
                    particle.Draw();
                }
            }, "ParticlePool.Draw");
        }

        /// <summary>
        /// Clears all active particles and returns them to pools
        /// </summary>
        public void Clear()
        {
            ErrorManager.SafeExecute(() =>
            {
                // Return all active particles to pools
                foreach (var particle in _activeEngineParticles)
                {
                    _engineParticlePool.Return(particle);
                }
                _activeEngineParticles.Clear();

                foreach (var particle in _activeExplosionParticles)
                {
                    _explosionParticlePool.Return(particle);
                }
                _activeExplosionParticles.Clear();

                ErrorManager.LogDebug("ParticlePool cleared all active particles");
            }, "ParticlePool.Clear");
        }

        /// <summary>
        /// Gets current particle statistics
        /// </summary>
        public ParticlePoolStatistics GetStatistics()
        {
            return new ParticlePoolStatistics
            {
                ActiveEngineParticles = _activeEngineParticles.Count,
                ActiveExplosionParticles = _activeExplosionParticles.Count,
                MaxEngineParticles = MaxEngineParticles,
                MaxExplosionParticles = MaxExplosionParticles,
                EnginePoolStats = _engineParticlePool.GetStatistics(),
                ExplosionPoolStats = _explosionParticlePool.GetStatistics()
            };
        }

        /// <summary>
        /// Optimizes pool sizes based on usage
        /// </summary>
        public void OptimizePools()
        {
            ErrorManager.SafeExecute(() =>
            {
                _engineParticlePool.OptimizePoolSize();
                _explosionParticlePool.OptimizePoolSize();
                ErrorManager.LogDebug("ParticlePool optimized");
            }, "OptimizePools");
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            Clear();
            _engineParticlePool?.Dispose();
            _explosionParticlePool?.Dispose();
            
            ErrorManager.LogInfo("ParticlePool disposed");
        }
    }

    /// <summary>
    /// Poolable engine particle implementation
    /// </summary>
    public class PooledEngineParticle : IPoolable
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifespan;
        public float InitialLifespan;
        public Color Color;

        public void Initialize(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            InitialLifespan = lifespan;
            Color = color;
        }

        public void Update()
        {
            Position += Velocity;
            Lifespan -= 1;
            
            // Fade out over time
            if (InitialLifespan > 0)
            {
                var alpha = Math.Max(0, Lifespan / InitialLifespan);
                Color = new Color(Color.R, Color.G, Color.B, (byte)(Color.A * alpha));
            }
        }

        public void Draw()
        {
            if (Lifespan > 0)
            {
                Raylib.DrawPixelV(Position, Color);
            }
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Lifespan = 0;
            InitialLifespan = 0;
            Color = Color.White;
        }
    }

    /// <summary>
    /// Poolable explosion particle implementation
    /// </summary>
    public class PooledExplosionParticle : IPoolable
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifespan;
        public float InitialLifespan;
        public Color Color;

        public void Initialize(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            InitialLifespan = lifespan;
            Color = color;
        }

        public void Update()
        {
            Position += Velocity;
            Velocity *= 0.98f; // Slight deceleration
            Lifespan -= 1;
            
            // Fade out over time
            if (InitialLifespan > 0)
            {
                var alpha = Math.Max(0, Lifespan / InitialLifespan);
                Color = new Color(Color.R, Color.G, Color.B, (byte)(Color.A * alpha));
            }
        }

        public void Draw()
        {
            if (Lifespan > 0)
            {
                Raylib.DrawPixelV(Position, Color);
            }
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Lifespan = 0;
            InitialLifespan = 0;
            Color = Color.White;
        }
    }

    /// <summary>
    /// Particle pool statistics
    /// </summary>
    public struct ParticlePoolStatistics
    {
        public int ActiveEngineParticles;
        public int ActiveExplosionParticles;
        public int MaxEngineParticles;
        public int MaxExplosionParticles;
        public PoolStatistics EnginePoolStats;
        public PoolStatistics ExplosionPoolStats;

        public override string ToString()
        {
            return $"Particles - Engine: {ActiveEngineParticles}/{MaxEngineParticles}, Explosion: {ActiveExplosionParticles}/{MaxExplosionParticles}";
        }
    }
}