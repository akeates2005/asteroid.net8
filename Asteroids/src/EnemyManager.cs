using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Centralized enemy management system with spawning, AI coordination, and integration
    /// Handles enemy lifecycle, collision detection, shooting, and performance optimization
    /// </summary>
    public class EnemyManager
    {
        private readonly List<EnemyShip> _activeEnemies;
        private readonly EnemyAI _enemyAI;
        private readonly BulletPool _enemyBulletPool;
        private readonly AudioManager? _audioManager;
        private readonly AdvancedParticlePool? _particlePool;
        private readonly Random _random;

        // Performance optimization - update AI at 30 Hz
        private int _aiUpdateCounter = 0;
        private const int AI_UPDATE_FREQUENCY = 2; // Every 2 frames (30 Hz at 60 FPS)

        // Enemy spawning configuration
        private float _lastSpawnTime = 0f;
        private int _maxEnemiesPerLevel = 5;
        private float _spawnInterval = 8f; // seconds between spawns

        // Statistics
        public int ActiveEnemyCount => _activeEnemies.Count(e => e.Active);
        public int TotalEnemiesSpawned { get; private set; }
        public int EnemiesDestroyed { get; private set; }

        public EnemyManager(BulletPool bulletPool, AudioManager? audioManager = null, AdvancedParticlePool? particlePool = null)
        {
            _activeEnemies = new List<EnemyShip>();
            _enemyAI = new EnemyAI();
            _enemyBulletPool = bulletPool; // Reuse existing bullet pool for enemy bullets
            _audioManager = audioManager;
            _particlePool = particlePool;
            _random = new Random();

            ErrorManager.LogInfo("EnemyManager initialized");
        }

        /// <summary>
        /// Update all active enemies, AI behaviors, and spawning logic
        /// </summary>
        public void UpdateEnemies(Player player, float deltaTime, int currentLevel)
        {
            if (player == null) return;

            // Update AI counter for performance optimization
            _aiUpdateCounter++;
            bool updateAI = (_aiUpdateCounter % AI_UPDATE_FREQUENCY) == 0;

            // Update existing enemies
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                
                if (!enemy.Active)
                {
                    // Remove destroyed enemies
                    _activeEnemies.RemoveAt(i);
                    EnemiesDestroyed++;
                    continue;
                }

                // Always update physics
                enemy.Update(deltaTime);

                // Update AI every other frame for performance (30 Hz)
                if (updateAI)
                {
                    _enemyAI.UpdateAIState(enemy, player, deltaTime * AI_UPDATE_FREQUENCY);
                    
                    // Handle enemy shooting
                    HandleEnemyShooting(enemy, player);
                }
            }

            // Handle enemy spawning
            UpdateEnemySpawning(currentLevel, deltaTime);

            // Setup formations for destroyer-type enemies
            SetupFormations();
        }

        /// <summary>
        /// Handle enemy collision detection with player and bullets
        /// </summary>
        public void HandleEnemyCollisions(Player player, IReadOnlyList<PooledBullet> bullets)
        {
            if (player == null) return;

            foreach (var enemy in _activeEnemies)
            {
                if (!enemy.Active) continue;

                // Check collision with player (Player class doesn't have Active property, assume always active)
                if (enemy.IsCollidingWith(player.Position, player.Size))
                {
                    HandleEnemyPlayerCollision(enemy, player);
                }

                // Check collision with bullets
                foreach (var bullet in bullets)
                {
                    if (bullet.Active && enemy.IsCollidingWith(bullet.Position, GameConstants.BULLET_RADIUS))
                    {
                        HandleEnemyBulletCollision(enemy, bullet);
                    }
                }
            }
        }

        /// <summary>
        /// Handle collision between enemy and player
        /// </summary>
        private void HandleEnemyPlayerCollision(EnemyShip enemy, Player player)
        {
            // Check if player has shield active
            if (player.IsShieldActive)
            {
                // Destroy enemy, player survives
                DestroyEnemy(enemy);
                _audioManager?.PlaySound("shield", 0.8f);
            }
            else
            {
                // Both take damage or are destroyed
                DestroyEnemy(enemy);
                // Player doesn't have TakeDamage method in current implementation
                // This would need to be added or handled at game level
                _audioManager?.PlaySound("explosion", 0.8f);
            }

            // Create collision explosion effect
            _particlePool?.CreatePowerUpDespawnEffect(enemy.Position, enemy.GetRenderColor());
        }

        /// <summary>
        /// Handle collision between enemy and bullet
        /// </summary>
        private void HandleEnemyBulletCollision(EnemyShip enemy, PooledBullet bullet)
        {
            // Apply damage to enemy
            enemy.TakeDamage(25f);
            bullet.Active = false; // Bullet is consumed
            
            _audioManager?.PlaySound("enemy_hit", 0.6f);

            // Create hit effect
            _particlePool?.CreatePowerUpSpawnEffect(enemy.Position, enemy.GetRenderColor());

            // If enemy is destroyed, handle destruction
            if (!enemy.Active)
            {
                DestroyEnemy(enemy);
            }
        }

        /// <summary>
        /// Destroy an enemy and create appropriate effects
        /// </summary>
        private void DestroyEnemy(EnemyShip enemy)
        {
            enemy.Active = false;
            
            // Create explosion effect
            _particlePool?.CreatePowerUpCollectionEffect(enemy.Position, enemy.GetRenderColor());
            _audioManager?.PlaySound("explosion", 0.7f);

            ErrorManager.LogDebug($"Enemy {enemy.Type} destroyed at {enemy.Position}");
        }

        /// <summary>
        /// Handle enemy shooting behavior
        /// </summary>
        private void HandleEnemyShooting(EnemyShip enemy, Player player)
        {
            // Only shoot if in attacking state and can shoot
            if (enemy.CurrentState != AIState.Attacking || !enemy.CanShoot())
                return;

            // Check if player is within range and in line of sight
            Vector2 toPlayer = player.Position - enemy.Position;
            float distanceToPlayer = toPlayer.Length();

            if (distanceToPlayer <= enemy.AttackRange && distanceToPlayer > 20f) // Minimum distance to avoid point-blank
            {
                // Calculate bullet direction with some lead prediction
                Vector2 predictedPlayerPos = _enemyAI.CalculateInterceptPath(
                    enemy.Position, player.Position, player.Velocity, GameConstants.BULLET_SPEED);
                
                Vector2 bulletDirection = Vector2.Normalize(predictedPlayerPos - enemy.Position);
                Vector2 bulletVelocity = bulletDirection * GameConstants.BULLET_SPEED;

                // Spawn bullet from enemy bullet pool
                if (_enemyBulletPool.SpawnBullet(enemy.Position, bulletVelocity))
                {
                    enemy.RecordShot();
                    _audioManager?.PlaySound("enemy_shoot", 0.5f);
                }
            }
        }

        /// <summary>
        /// Update enemy spawning logic based on level
        /// </summary>
        private void UpdateEnemySpawning(int currentLevel, float deltaTime)
        {
            _maxEnemiesPerLevel = Math.Min(3 + currentLevel * 2, 15); // Scale with level, max 15
            
            // Check if we should spawn more enemies
            bool shouldSpawn = ActiveEnemyCount < _maxEnemiesPerLevel &&
                              (Raylib.GetTime() - _lastSpawnTime) >= _spawnInterval;

            if (shouldSpawn)
            {
                SpawnEnemyWave(currentLevel);
                _lastSpawnTime = (float)Raylib.GetTime();
            }
        }

        /// <summary>
        /// Spawn a wave of enemies based on current level
        /// </summary>
        public void SpawnEnemyWave(int level)
        {
            int enemiesToSpawn = Math.Min(2 + level / 3, 4); // 2-4 enemies per wave

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnRandomEnemy(level);
            }

            ErrorManager.LogInfo($"Spawned {enemiesToSpawn} enemies for level {level}");
        }

        /// <summary>
        /// Spawn a single random enemy at screen edge
        /// </summary>
        private void SpawnRandomEnemy(int level)
        {
            // Determine enemy type based on level
            EnemyType type = DetermineEnemyType(level);
            
            // Spawn at random screen edge position
            Vector2 spawnPosition = GetRandomSpawnPosition();
            
            var enemy = new EnemyShip(type, spawnPosition);
            _activeEnemies.Add(enemy);
            TotalEnemiesSpawned++;

            ErrorManager.LogDebug($"Spawned {type} enemy at {spawnPosition}");
        }

        /// <summary>
        /// Determine enemy type based on level progression
        /// </summary>
        private EnemyType DetermineEnemyType(int level)
        {
            // Progressive difficulty: introduce new enemy types as levels increase
            List<EnemyType> availableTypes = new List<EnemyType> { EnemyType.Scout };

            if (level >= 2) availableTypes.Add(EnemyType.Hunter);
            if (level >= 4) availableTypes.Add(EnemyType.Interceptor);
            if (level >= 6) availableTypes.Add(EnemyType.Destroyer);

            // Weight distribution based on level
            if (level < 3)
            {
                // Early levels: mostly scouts
                return _random.NextDouble() < 0.8 ? EnemyType.Scout : EnemyType.Hunter;
            }
            else if (level < 6)
            {
                // Mid levels: mixed with more hunters
                double rand = _random.NextDouble();
                if (rand < 0.4) return EnemyType.Scout;
                if (rand < 0.7) return EnemyType.Hunter;
                return EnemyType.Interceptor;
            }
            else
            {
                // High levels: all types with emphasis on stronger enemies
                double rand = _random.NextDouble();
                if (rand < 0.2) return EnemyType.Scout;
                if (rand < 0.4) return EnemyType.Hunter;
                if (rand < 0.7) return EnemyType.Interceptor;
                return EnemyType.Destroyer;
            }
        }

        /// <summary>
        /// Get random spawn position at screen edges
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            int edge = _random.Next(4); // 0=top, 1=right, 2=bottom, 3=left
            
            return edge switch
            {
                0 => new Vector2(_random.Next(0, GameConstants.SCREEN_WIDTH), -30f), // Top
                1 => new Vector2(GameConstants.SCREEN_WIDTH + 30f, _random.Next(0, GameConstants.SCREEN_HEIGHT)), // Right
                2 => new Vector2(_random.Next(0, GameConstants.SCREEN_WIDTH), GameConstants.SCREEN_HEIGHT + 30f), // Bottom
                3 => new Vector2(-30f, _random.Next(0, GameConstants.SCREEN_HEIGHT)), // Left
                _ => new Vector2(100f, 100f) // Fallback
            };
        }

        /// <summary>
        /// Setup formation flying for destroyer-type enemies
        /// </summary>
        private void SetupFormations()
        {
            var destroyers = _activeEnemies.Where(e => e.Active && e.Type == EnemyType.Destroyer && e.FormationIndex < 0).ToList();
            
            if (destroyers.Count >= 2)
            {
                // Group destroyers into formations of 2-4
                for (int i = 0; i < destroyers.Count; i += 3)
                {
                    var formationGroup = destroyers.Skip(i).Take(3).ToList();
                    Vector2 centerPosition = formationGroup[0].Position;
                    
                    _enemyAI.SetupFormation(formationGroup, centerPosition);
                }
            }
        }

        /// <summary>
        /// Render all active enemies using the provided renderer
        /// </summary>
        public void RenderEnemies(IRenderer renderer)
        {
            foreach (var enemy in _activeEnemies)
            {
                if (!enemy.Active) continue;

                enemy.Render(renderer);
            }
        }

        /// <summary>
        /// Draw health bar above enemy
        /// </summary>
        private void DrawHealthBar(Vector2 position, float enemySize, float healthPercentage)
        {
            Vector2 barPos = position + new Vector2(-enemySize * 0.8f, -enemySize * 1.5f);
            float barWidth = enemySize * 1.6f;
            float barHeight = 4f;

            // Background
            Raylib.DrawRectangle((int)barPos.X, (int)barPos.Y, (int)barWidth, (int)barHeight, new Color(169, 169, 169, 255)); // DarkGray
            
            // Health bar
            Color healthColor = healthPercentage > 0.6f ? new Color(0, 128, 0, 255) : // Green 
                               healthPercentage > 0.3f ? new Color(255, 255, 0, 255) : new Color(255, 0, 0, 255); // Yellow : Red
            Raylib.DrawRectangle((int)barPos.X, (int)barPos.Y, (int)(barWidth * healthPercentage), (int)barHeight, healthColor);
        }

        /// <summary>
        /// Draw type indicator symbol
        /// </summary>
        private void DrawTypeIndicator(Vector2 position, EnemyType type, float size)
        {
            char symbol = type switch
            {
                EnemyType.Scout => 'S',
                EnemyType.Hunter => 'H',
                EnemyType.Destroyer => 'D',
                EnemyType.Interceptor => 'I',
                _ => '?'
            };

            Vector2 textPos = position + new Vector2(-4f, size + 8f);
            Raylib.DrawText(symbol.ToString(), (int)textPos.X, (int)textPos.Y, 10, new Color(255, 255, 255, 255)); // White
        }

        /// <summary>
        /// Clear all active enemies (for level transitions)
        /// </summary>
        public void ClearAllEnemies()
        {
            _activeEnemies.Clear();
            ErrorManager.LogInfo("All enemies cleared");
        }

        /// <summary>
        /// Get all active enemies for external systems (like spatial collision)
        /// </summary>
        public List<EnemyShip> GetActiveEnemies()
        {
            return _activeEnemies.Where(e => e.Active).ToList();
        }

        /// <summary>
        /// Get enemy statistics for debugging/UI
        /// </summary>
        public (int active, int total, int destroyed) GetStatistics()
        {
            return (ActiveEnemyCount, TotalEnemiesSpawned, EnemiesDestroyed);
        }
    }
}