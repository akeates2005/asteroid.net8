using System;

namespace Asteroids
{
    /// <summary>
    /// Centralized manager for all object pools in the game
    /// Provides easy access to different types of pooled objects
    /// </summary>
    public class PoolManager : IDisposable
    {
        private readonly ObjectPool<PoolManagerBullet> _bulletPool;
        private readonly ObjectPool<PoolManagerExplosionParticle> _particlePool;
        private readonly ObjectPool<PoolManagerEngineParticle> _engineParticlePool;
        
        // Pool statistics
        public PoolStatistics BulletPoolStats => _bulletPool.GetStatistics();
        public PoolStatistics ParticlePoolStats => _particlePool.GetStatistics();
        public PoolStatistics EngineParticlePoolStats => _engineParticlePool.GetStatistics();
        
        public PoolManager()
        {
            // Initialize bullet pool
            _bulletPool = new ObjectPool<PoolManagerBullet>(
                maxPoolSize: 200,
                preloadCount: 50
            );
            
            // Initialize particle pool
            _particlePool = new ObjectPool<PoolManagerExplosionParticle>(
                maxPoolSize: 500,
                preloadCount: 100
            );
            
            // Initialize engine particle pool
            _engineParticlePool = new ObjectPool<PoolManagerEngineParticle>(
                maxPoolSize: 300,
                preloadCount: 50
            );
            
            ErrorManager.LogInfo("PoolManager initialized with all object pools");
        }
        
        // Bullet pool methods
        public Bullet GetBullet()
        {
            return _bulletPool.Rent();
        }
        
        public void ReturnBullet(Bullet bullet)
        {
            if (bullet is PoolManagerBullet pooledBullet)
                _bulletPool.Return(pooledBullet);
        }
        
        // Particle pool methods
        public ExplosionParticle GetParticle()
        {
            return _particlePool.Rent();
        }
        
        public void ReturnParticle(ExplosionParticle particle)
        {
            if (particle is PoolManagerExplosionParticle pooledParticle)
                _particlePool.Return(pooledParticle);
        }
        
        // Engine particle pool methods
        public EngineParticle GetEngineParticle()
        {
            return _engineParticlePool.Rent();
        }
        
        public void ReturnEngineParticle(EngineParticle particle)
        {
            if (particle is PoolManagerEngineParticle pooledParticle)
                _engineParticlePool.Return(pooledParticle);
        }
        
        // Pool management methods
        public void OptimizeAllPools()
        {
            _bulletPool.OptimizePoolSize();
            _particlePool.OptimizePoolSize();
            _engineParticlePool.OptimizePoolSize();
            
            ErrorManager.LogInfo("All object pools optimized");
        }
        
        public void ClearAllPools()
        {
            _bulletPool.Clear();
            _particlePool.Clear();
            _engineParticlePool.Clear();
            
            ErrorManager.LogInfo("All object pools cleared");
        }
        
        public string GetPoolingReport()
        {
            var bulletStats = _bulletPool.GetStatistics();
            var particleStats = _particlePool.GetStatistics();
            var engineStats = _engineParticlePool.GetStatistics();
            
            return $"Pool Statistics:\n" +
                   $"Bullets: {bulletStats}\n" +
                   $"Particles: {particleStats}\n" +
                   $"Engine Particles: {engineStats}";
        }
        
        public void Dispose()
        {
            ErrorManager.LogInfo("Disposing PoolManager and all pools");
            
            _bulletPool?.Dispose();
            _particlePool?.Dispose();
            _engineParticlePool?.Dispose();
            
            ErrorManager.LogInfo("PoolManager disposal completed");
        }
    }
    
    /// <summary>
    /// Poolable bullet implementation for PoolManager
    /// </summary>
    public class PoolManagerBullet : Bullet, IPoolable
    {
        public PoolManagerBullet() : base(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero)
        {
        }
        
        public void Reset()
        {
            Position = System.Numerics.Vector2.Zero;
            Velocity = System.Numerics.Vector2.Zero;
            Active = false;
        }
    }
    
    /// <summary>
    /// Poolable explosion particle implementation for PoolManager
    /// </summary>
    public class PoolManagerExplosionParticle : ExplosionParticle, IPoolable
    {
        public PoolManagerExplosionParticle() : base(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero, 0, Raylib_cs.Color.White)
        {
        }
        
        public void Reset()
        {
            Position = System.Numerics.Vector2.Zero;
            Velocity = System.Numerics.Vector2.Zero;
            Lifespan = 0;
            Color = Raylib_cs.Color.White;
        }
    }
    
    /// <summary>
    /// Poolable engine particle implementation for PoolManager
    /// </summary>
    public class PoolManagerEngineParticle : EngineParticle, IPoolable
    {
        public PoolManagerEngineParticle() : base(System.Numerics.Vector2.Zero, System.Numerics.Vector2.Zero, 0, Raylib_cs.Color.White)
        {
        }
        
        public void Reset()
        {
            Position = System.Numerics.Vector2.Zero;
            Velocity = System.Numerics.Vector2.Zero;
            Lifespan = 0;
            Color = Raylib_cs.Color.White;
        }
    }
}