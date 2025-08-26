using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Wrapper entities for spatial grid integration with existing game objects
    /// </summary>
    
    /// <summary>
    /// Wrapper for Player class to implement IGameEntity for spatial partitioning
    /// </summary>
    public class PlayerSpatialEntity : IGameEntity
    {
        private readonly Player _player;
        private static int _nextId = 1;

        public PlayerSpatialEntity(Player player)
        {
            _player = player;
            Id = _nextId++;
        }

        public int Id { get; }
        
        public Vector2 Position 
        { 
            get => _player.Position; 
            set => _player.Position = value; 
        }
        
        public bool Active { get; set; } = true;

        public void Update(float deltaTime)
        {
            // Player update is handled by GameProgram
        }

        public void Render(IRenderer renderer)
        {
            // Player rendering is handled by GameProgram  
        }

        public float GetCollisionRadius()
        {
            return _player.Size / 2f;
        }

        public void Initialize()
        {
            // Player initialization is handled by GameProgram
        }

        public void Dispose()
        {
            // Player cleanup is handled by GameProgram
        }

        public Player GetPlayer() => _player;
    }

    /// <summary>
    /// Wrapper for Asteroid class to implement IGameEntity for spatial partitioning
    /// </summary>
    public class AsteroidSpatialEntity : IGameEntity
    {
        private readonly Asteroid _asteroid;
        private static int _nextId = 1000; // Start at 1000 to avoid conflicts

        public AsteroidSpatialEntity(Asteroid asteroid)
        {
            _asteroid = asteroid;
            Id = _nextId++;
        }

        public int Id { get; }
        
        public Vector2 Position 
        { 
            get => _asteroid.Position; 
            set => _asteroid.Position = value; 
        }
        
        public bool Active 
        { 
            get => _asteroid.Active; 
            set => _asteroid.Active = value; 
        }

        public void Update(float deltaTime)
        {
            // Asteroid update is handled by GameProgram
        }

        public void Render(IRenderer renderer)
        {
            // Asteroid rendering is handled by GameProgram
        }

        public float GetCollisionRadius()
        {
            return _asteroid.Radius;
        }

        public void Initialize()
        {
            // Asteroid initialization is handled by GameProgram
        }

        public void Dispose()
        {
            // Asteroid cleanup is handled by GameProgram
        }

        public Asteroid GetAsteroid() => _asteroid;
    }

    /// <summary>
    /// Wrapper for Bullet class to implement IGameEntity for spatial partitioning
    /// </summary>
    public class BulletSpatialEntity : IGameEntity
    {
        private readonly PooledBullet _bullet;
        private static int _nextId = 2000; // Start at 2000 to avoid conflicts

        public BulletSpatialEntity(PooledBullet bullet)
        {
            _bullet = bullet;
            Id = _nextId++;
        }

        public int Id { get; }
        
        public Vector2 Position 
        { 
            get => _bullet.Position; 
            set => _bullet.Position = value; 
        }
        
        public bool Active 
        { 
            get => _bullet.Active; 
            set => _bullet.Active = value; 
        }

        public void Update(float deltaTime)
        {
            // Bullet update is handled by GameProgram/BulletPool
        }

        public void Render(IRenderer renderer)
        {
            // Bullet rendering is handled by GameProgram/BulletPool
        }

        public float GetCollisionRadius()
        {
            return GameConstants.BULLET_RADIUS;
        }

        public void Initialize()
        {
            // Bullet initialization is handled by BulletPool
        }

        public void Dispose()
        {
            // Bullet cleanup is handled by BulletPool
        }

        public PooledBullet GetBullet() => (PooledBullet)_bullet;
    }
}