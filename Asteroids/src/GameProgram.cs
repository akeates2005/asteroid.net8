using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Main game program containing the complete Asteroids game implementation
    /// Enhanced with spatial partitioning for O(n+k) collision detection
    /// </summary>
    public class GameProgram
    {
        private AudioManager? _audioManager;
        private SettingsManager? _settingsManager;
        private AdvancedEffectsManager? _visualEffects;
        private BulletPool? _bulletPool;
        private AdvancedParticlePool? _explosionPool;
        private AnimatedHUD? _animatedHUD;
        private GraphicsSettings? _graphicsSettings;
        private GraphicsProfiler? _graphicsProfiler;
        private AdaptiveGraphicsManager? _adaptiveGraphics;

        // Spatial Partitioning System for enhanced collision detection
        private SpatialGrid? _spatialGrid;
        private PlayerSpatialEntity? _playerEntity;
        private List<AsteroidSpatialEntity>? _asteroidEntities;
        private List<BulletSpatialEntity>? _bulletEntities;

        // Enhanced Rendering System
        private IRenderer? _renderer;
        private LODManager? _lodManager;
        private ProceduralAsteroidGenerator? _asteroidGenerator;
        private bool _render3D = false;

        // Power-Up System
        private PowerUpManager? _powerUpManager;

        private Player? _player;
        private List<Asteroid>? _asteroids;
        private List<ExplosionParticle>? _explosions;
        private Leaderboard? _leaderboard;
        private Random? _random;

        private int _score;
        private int _level;
        private bool _gameOver;
        private bool _levelComplete;
        private bool _gamePaused;

        public void Run()
        {
            try
            {
                Initialize();
                RunGameLoop();
                Cleanup();
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Critical error in main program", ex);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void Initialize()
        {
            ErrorManager.LogInfo("Initializing game");

            // Initialize Raylib
            Raylib.InitWindow(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT, "Asteroids - Enhanced");
            Raylib.SetTargetFPS(GameConstants.TARGET_FPS);

            // Initialize graphics settings first
            _graphicsSettings = new GraphicsSettings();
            _graphicsProfiler = new GraphicsProfiler();
            
            // Initialize managers
            _settingsManager = new SettingsManager();
            _audioManager = new AudioManager();
            _visualEffects = new AdvancedEffectsManager();
            _animatedHUD = new AnimatedHUD();

            // Initialize adaptive graphics manager
            _adaptiveGraphics = new AdaptiveGraphicsManager(_graphicsSettings, _graphicsProfiler);

            // Initialize LOD manager for performance optimization
            _lodManager = new LODManager(_graphicsProfiler);

            // Initialize procedural asteroid generator
            _asteroidGenerator = new ProceduralAsteroidGenerator();

            // Initialize renderer using factory pattern
            _renderer = RendererFactory.CreateRenderer(_graphicsSettings);
            
            if (!_renderer.Initialize())
            {
                ErrorManager.LogError("Failed to initialize renderer");
                throw new InvalidOperationException("Renderer initialization failed");
            }

            // Initialize object pools with graphics settings scaling
            _bulletPool = new BulletPool(GameConstants.MAX_BULLETS);
            _explosionPool = new AdvancedParticlePool(_graphicsSettings.MaxParticles);
            
            // Initialize power-up system
            _powerUpManager = new PowerUpManager(_explosionPool, _audioManager);
            
            // Initialize dynamic theme
            DynamicTheme.ResetToLevel(_level);

            // Initialize spatial partitioning system
            _spatialGrid = new SpatialGrid(GameConstants.SPATIAL_GRID_CELL_SIZE);
            _asteroidEntities = new List<AsteroidSpatialEntity>();
            _bulletEntities = new List<BulletSpatialEntity>();

            // Initialize game objects
            _player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), GameConstants.PLAYER_SIZE);
            _playerEntity = new PlayerSpatialEntity(_player);
            _asteroids = new List<Asteroid>();
            _explosions = new List<ExplosionParticle>();
            _leaderboard = new Leaderboard();
            _random = new Random();

            // 3D rendering is now handled by the IRenderer abstraction
            if (Renderer3DIntegration.Initialize())
            {
                _render3D = true;
                ErrorManager.LogInfo("3D rendering enabled via IRenderer");
            }
            else
            {
                ErrorManager.LogInfo("3D rendering disabled, using 2D mode via IRenderer");
            }

            // Initialize game state
            ResetGame();

            ErrorManager.LogInfo("Game initialization completed");
        }

        private void RunGameLoop()
        {
            while (!Raylib.WindowShouldClose())
            {
                try
                {
                    Update();
                    Render();
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error in game loop", ex);
                }
            }
        }

        private void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            
            // Begin frame profiling
            _graphicsProfiler?.BeginFrame();
            
            // Update enhanced systems
            DynamicTheme.Update(deltaTime);
            if (_audioManager != null) _audioManager.Update();
            if (_visualEffects != null) _visualEffects.Update(deltaTime);
            if (_animatedHUD != null) _animatedHUD.Update(deltaTime);
            if (_adaptiveGraphics != null) _adaptiveGraphics.Update(deltaTime);

            // Handle pause toggle
            if (Raylib.IsKeyPressed(KeyboardKey.P))
            {
                _gamePaused = !_gamePaused;
            }

            // Handle 3D toggle
            if (Raylib.IsKeyPressed(KeyboardKey.F3))
            {
                Renderer3DIntegration.Toggle3DMode(); // TODO: Integrate with IRenderer
            }

            // Handle camera controls in 3D mode
            if (Renderer3DIntegration.Is3DEnabled)
            {
                Renderer3DIntegration.HandleCameraInput(); // TODO: Integrate with IRenderer
            }

            if (!_gameOver && !_levelComplete && !_gamePaused)
            {
                UpdateGameLogic();
            }
            else
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (_gameOver)
                    {
                        if (_leaderboard != null) _leaderboard.AddScore(_score);
                        ResetGame();
                    }
                    else if (_levelComplete)
                    {
                        _level++;
                        _levelComplete = false;
                        
                        // Update theme for new level
                        DynamicTheme.UpdateLevel(_level);
                        
                        // Level transition effect
                        _visualEffects?.OnLevelComplete();
                        
                        // Reset player position and state for new level
                        if (_player != null)
                        {
                            _player.Position = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
                            _player.Velocity = Vector2.Zero;
                            _player.Rotation = 0;
                            _player.IsShieldActive = false;
                            _player.ShieldCooldown = 0;
                        }
                        
                        // Clear any remaining bullets and effects
                        _bulletPool?.Clear();
                        if (_explosions != null) _explosions.Clear();
                        _explosionPool?.Clear(); // Clear explosion particle pool
                        _powerUpManager?.Clear(); // Clear power-ups
                        if (_visualEffects != null) _visualEffects.Clear();
                        if (_player != null) _player.ClearEngineParticles(); // Clear player engine particles
                        
                        StartLevel(_level);
                    }
                }
            }
        }

        private void UpdateGameLogic()
        {
            if (_player == null || _asteroids == null || _explosions == null || _random == null) return;

            // Update player
            _player.Update();

            // Handle shooting
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                FireBullet();
            }

            // Handle shield activation
            if (Raylib.IsKeyPressed(KeyboardKey.X) && !_player.IsShieldActive && _player.ShieldCooldown <= 0)
            {
                _player.IsShieldActive = true;
                _player.ShieldDuration = GameConstants.MAX_SHIELD_DURATION;
                if (_audioManager != null) _audioManager.PlaySound("shield", 0.8f);
                _visualEffects?.OnShieldActivated();
            }

            // Update bullets through pool
            _bulletPool?.Update();
            
            // Update explosion particles through pool
            _explosionPool?.Update();

            // Update power-ups
            _powerUpManager?.UpdatePowerUps(Raylib.GetFrameTime());

            // Update asteroids
            foreach (var asteroid in _asteroids)
            {
                asteroid.Update();
            }

            // Update explosions
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                _explosions[i].Update();
                if (!_explosions[i].IsActive)
                {
                    var explosion = _explosions[i];
                    _explosions.RemoveAt(i);
                    // Particles are managed by the pool automatically
                }
            }

            // Simple collision detection (brute force for now)
            CheckCollisions();

            // Check level completion
            if (_asteroids.Count == 0)
            {
                _levelComplete = true;
            }
        }

        private void CheckCollisions()
        {
            if (_player == null || _bulletPool == null || _asteroids == null || _spatialGrid == null ||
                _playerEntity == null || _asteroidEntities == null || _bulletEntities == null) return;

            // Clear and repopulate spatial grid
            _spatialGrid.Clear();

            // Add player to spatial grid
            _spatialGrid.Insert(_playerEntity);

            // Update and add active asteroids to spatial grid
            _asteroidEntities.Clear();
            foreach (var asteroid in _asteroids)
            {
                if (asteroid.Active)
                {
                    var asteroidEntity = new AsteroidSpatialEntity(asteroid);
                    _asteroidEntities.Add(asteroidEntity);
                    _spatialGrid.Insert(asteroidEntity);
                }
            }

            // Update and add active bullets to spatial grid
            _bulletEntities.Clear();
            var activeBullets = _bulletPool.GetActiveBullets();
            foreach (var bullet in activeBullets)
            {
                if (bullet.Active)
                {
                    var bulletEntity = new BulletSpatialEntity((PooledBullet)bullet);
                    _bulletEntities.Add(bulletEntity);
                    _spatialGrid.Insert(bulletEntity);
                }
            }

            // Bullet-asteroid collisions using spatial partitioning
            foreach (var bulletEntity in _bulletEntities)
            {
                var bullet = bulletEntity.GetBullet();
                if (!bullet.Active) continue;

                var nearbyEntities = _spatialGrid.Query(bullet.Position, GameConstants.BULLET_RADIUS);
                foreach (var entity in nearbyEntities)
                {
                    if (entity is AsteroidSpatialEntity asteroidEntity)
                    {
                        var asteroid = asteroidEntity.GetAsteroid();
                        if (!asteroid.Active) continue;

                        float distance = Vector2.Distance(bullet.Position, asteroid.Position);
                        if (distance <= GameConstants.BULLET_RADIUS + asteroid.Radius)
                        {
                            _bulletPool.DeactivateBullet((PooledBullet)bullet);
                            asteroid.Active = false;
                            _score += GameConstants.BULLET_SCORE_VALUE;
                            
                            // Enhanced explosion effects
                            _visualEffects?.OnAsteroidDestroyed(asteroid.Position, asteroid.AsteroidSize);
                            CreateExplosionAt(asteroid.Position);
                            if (_audioManager != null) _audioManager.PlaySound("explosion", 0.8f);
                            
                            // Spawn power-up with chance
                            if (_powerUpManager != null && _random != null && _random.Next(0, 100) < GameConstants.POWERUP_SPAWN_CHANCE)
                            {
                                var powerUpType = (PowerUpType)_random.Next(0, 5);
                                _powerUpManager.SpawnPowerUp(asteroid.Position, powerUpType);
                            }
                            
                            // Add small camera shake on asteroid destruction
                            if (Renderer3DIntegration.Is3DEnabled)
                            {
                                Renderer3DIntegration.AddCameraShake(1f, 0.2f);
                            }
                            break; // Bullet can only hit one asteroid
                        }
                    }
                }
            }

            // Player-asteroid collisions using spatial partitioning
            var nearbyPlayerEntities = _spatialGrid.Query(_player.Position, _player.Size / 2);
            foreach (var entity in nearbyPlayerEntities)
            {
                if (entity is AsteroidSpatialEntity asteroidEntity)
                {
                    var asteroid = asteroidEntity.GetAsteroid();
                    if (!asteroid.Active) continue;

                    float distance = Vector2.Distance(_player.Position, asteroid.Position);
                    if (distance <= _player.Size / 2 + asteroid.Radius)
                    {
                        if (_player.IsShieldActive)
                        {
                            asteroid.Active = false;
                            CreateExplosionAt(asteroid.Position);
                            if (_audioManager != null) _audioManager.PlaySound("explosion", 0.6f);
                            
                            // Spawn power-up with chance when destroyed by shield
                            if (_powerUpManager != null && _random != null && _random.Next(0, 100) < GameConstants.POWERUP_SPAWN_CHANCE)
                            {
                                var powerUpType = (PowerUpType)_random.Next(0, 5);
                                _powerUpManager.SpawnPowerUp(asteroid.Position, powerUpType);
                            }
                        }
                        else
                        {
                            _gameOver = true;
                            _visualEffects?.OnGameOver();
                            CreateExplosionAt(_player.Position);
                            if (_audioManager != null) _audioManager.PlaySound("explosion", 1.0f);
                            
                            // Add camera shake on player death
                            if (Renderer3DIntegration.Is3DEnabled)
                            {
                                Renderer3DIntegration.AddCameraShake(5f, 1f);
                            }
                        }
                        break; // Player collision processed
                    }
                }
            }

            // Check power-up collisions
            if (_powerUpManager != null && _player != null)
            {
                _powerUpManager.CheckCollision(_player);
            }

            // Remove inactive objects
            _asteroids.RemoveAll(a => !a.Active);
        }

        private void FireBullet()
        {
            if (_bulletPool == null || _player == null) return;

            Vector2 bulletVelocity = Vector2.Transform(new Vector2(0, -1), Matrix3x2.CreateRotation(MathF.PI / 180 * _player.Rotation)) * (GameConstants.BULLET_SPEED / GameConstants.TARGET_FPS);
            if (_bulletPool.SpawnBullet(_player.Position, bulletVelocity))
            {
                // Bullet spawned successfully
                if (_audioManager != null) _audioManager.PlaySound("shoot", 0.6f);
                _visualEffects?.OnBulletFired();
                
                // Create bullet trail effect
                if (_graphicsSettings?.EnableParticleTrails == true)
                {
                    _explosionPool?.CreateBulletTrail(_player.Position, bulletVelocity * GameConstants.TARGET_FPS, DynamicTheme.GetBulletColor());
                }
            }
        }

        private void CreateExplosionAt(Vector2 position)
        {
            if (_explosionPool == null || _explosions == null || _random == null) return;

            // Create enhanced explosion effects
            _explosionPool?.CreateExplosionBurst(position, 12, 100.0f);
            
            // Also create debris field
            if (_graphicsSettings?.EnableParticleTrails == true)
            {
                _explosionPool?.CreateDebrisField(position, AsteroidSize.Medium);
            }
        }

        private void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Update LOD manager
            float deltaTime = Raylib.GetFrameTime();
            _lodManager?.Update(deltaTime);
            
            // Begin frame rendering via IRenderer
            _renderer?.BeginFrame();

            // Begin 3D rendering if enabled (legacy support)
            if (Renderer3DIntegration.Is3DEnabled && _player != null)
            {
                Renderer3DIntegration.BeginFrame(_player.Position, _player.Velocity, deltaTime);
            }

            // Draw grid via IRenderer abstraction
            if (_settingsManager?.Current.Graphics.Basic.ShowGrid == true)
            {
                _renderer?.RenderGrid(true, DynamicTheme.GetGridColor());
            }

            if (!_gameOver && !_levelComplete)
            {
                // Draw game objects using IRenderer with LOD support
                
                // Render player
                if (_player != null) 
                {
                    _renderer?.RenderPlayer(_player.Position, _player.Rotation, 
                        Theme.PlayerColor, _player.IsShieldActive);
                        
                    // Render player's engine particles (thrust trail) using legacy rendering
                    // This ensures thrust trails work until we fully integrate particle rendering into IRenderer
                    _player.DrawEngineParticles();
                }

                // Render bullets with culling
                if (_bulletPool != null)
                {
                    var activeBullets = _bulletPool.GetActiveBullets();
                    foreach (var bullet in activeBullets)
                    {
                        if (bullet.Active && (_lodManager?.ShouldRender(bullet.Position, GameConstants.BULLET_RADIUS) ?? true))
                        {
                            _renderer?.RenderBullet(bullet.Position, Theme.BulletColor);
                        }
                    }
                }

                // Render asteroids with LOD support
                if (_asteroids != null)
                {
                    foreach (var asteroid in _asteroids)
                    {
                        if (asteroid.Active && (_lodManager?.ShouldRender(asteroid.Position, asteroid.Radius) ?? true))
                        {
                            int lodLevel = _lodManager?.CalculateAsteroidLOD(asteroid.Position, asteroid.Radius) ?? 0;
                            _renderer?.RenderAsteroid(asteroid.Position, asteroid.Radius, 
                                Theme.AsteroidColor, asteroid.GetHashCode(), lodLevel);
                        }
                    }
                }

                // Render power-ups
                if (_powerUpManager != null)
                {
                    if (Renderer3DIntegration.Is3DEnabled)
                        _powerUpManager.RenderPowerUps3D(_renderer);
                    else
                        _powerUpManager.RenderPowerUps2D();
                }

                // Render explosions with culling
                if (_explosions != null)
                {
                    foreach (var explosion in _explosions)
                    {
                        if (explosion.IsActive)
                        {
                            float explosionRadius = GameConstants.EXPLOSION_MAX_RADIUS * (explosion.Lifespan / (float)GameConstants.EXPLOSION_PARTICLE_LIFESPAN);
                            if (_lodManager?.ShouldRender(explosion.Position, explosionRadius) ?? true)
                            {
                                float intensity = explosion.Lifespan / (float)GameConstants.EXPLOSION_PARTICLE_LIFESPAN;
                                _renderer?.RenderExplosion(explosion.Position, intensity, explosion.Color);
                            }
                        }
                    }
                }
                
                // Legacy 3D rendering (for compatibility during transition)
                if (Renderer3DIntegration.Is3DEnabled)
                {
                    // Keep original rendering as fallback
                }
                
                // Legacy 2D rendering for particle effects (temporary during transition)
                if (!Renderer3DIntegration.Is3DEnabled)
                {
                    // Begin particle rendering profiling
                    _graphicsProfiler?.BeginParticleRender();
                    _explosionPool?.Draw();
                    _graphicsProfiler?.EndParticleRender(_explosionPool?.GetActiveParticleCount() ?? 0);
                }

                // Begin HUD rendering profiling
                _graphicsProfiler?.BeginHUDRender();
                
                // Draw enhanced animated UI
                if (_animatedHUD != null && _player != null)
                {
                    int lives = 3; // Simplified - could be tracked properly
                    _animatedHUD.DrawHUD(_player, _level, _score, lives);
                }
                else
                {
                    // Fallback to static UI with dynamic colors
                    Raylib.DrawText($"Score: {_score}", GameConstants.UI_PADDING, GameConstants.UI_PADDING, 
                        GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
                    Raylib.DrawText($"Level: {_level}", GameConstants.SCREEN_WIDTH - 100, GameConstants.UI_PADDING, 
                        GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());

                    if (_player?.IsShieldActive == true)
                    {
                        Raylib.DrawText("SHIELD ACTIVE", GameConstants.SCREEN_WIDTH / 2 - 50, 50, 
                            GameConstants.FONT_SIZE_SMALL, DynamicTheme.GetShieldColor());
                    }
                }
                
                _graphicsProfiler?.EndHUDRender();
            }
            else if (_levelComplete)
            {
                Raylib.DrawText($"LEVEL {_level} COMPLETE", GameConstants.SCREEN_WIDTH / 2 - 150, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                    GameConstants.FONT_SIZE_LARGE, DynamicTheme.GetLevelCompleteColor());
                Raylib.DrawText("PRESS [ENTER] TO START NEXT LEVEL", GameConstants.SCREEN_WIDTH / 2 - 200, 
                    GameConstants.SCREEN_HEIGHT / 2 + 20, GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
            }
            else
            {
                Raylib.DrawText("GAME OVER", GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 - 80, 
                    GameConstants.FONT_SIZE_LARGE, DynamicTheme.GetGameOverColor());

                if (_leaderboard?.Scores != null)
                {
                    Raylib.DrawText("LEADERBOARD", GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                        GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
                    for (int i = 0; i < Math.Min(_leaderboard.Scores.Count, 5); i++)
                    {
                        Raylib.DrawText($"{i + 1}. {_leaderboard.Scores[i]}", 
                            GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 + 10 + (i * 20), 
                            GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
                    }
                }

                Raylib.DrawText("PRESS [ENTER] TO RESTART", GameConstants.SCREEN_WIDTH / 2 - 150, 
                    GameConstants.SCREEN_HEIGHT / 2 + 120, GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
            }

            if (_gamePaused)
            {
                Raylib.DrawText("PAUSED", GameConstants.SCREEN_WIDTH / 2 - 60, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                    GameConstants.FONT_SIZE_LARGE, DynamicTheme.GetTextColor());
            }

            // End frame rendering via IRenderer
            _renderer?.EndFrame();
            
            // End 3D rendering if enabled (legacy support)
            if (Renderer3DIntegration.Is3DEnabled)
            {
                Renderer3DIntegration.EndFrame();
            }

            // Begin effects rendering profiling
            _graphicsProfiler?.BeginEffectsRender();
            
            // Render screen effects (overlays like flash, fade)
            _visualEffects?.RenderScreenEffects();
            
            _graphicsProfiler?.EndEffectsRender(_visualEffects?.ActiveEffectCount ?? 0);
            
            // Draw performance overlay (F12 or when enabled)
            _graphicsProfiler?.DrawPerformanceOverlay(_graphicsSettings ?? new GraphicsSettings());

            // Draw enhanced performance info in debug mode
            #if DEBUG
            var renderStats = _renderer?.GetRenderStats() ?? new RenderStats { RenderMode = "Unknown" };
            string mode = Renderer3DIntegration.Is3DEnabled ? "3D" : "2D";
            Raylib.DrawText($"FPS: {Raylib.GetFPS()} | Mode: {renderStats.RenderMode} | F3: Toggle 3D", 10, GameConstants.SCREEN_HEIGHT - 30, 16, Color.Green);
            
            // Show LOD and culling stats
            if (_lodManager != null)
            {
                var lodStats = _lodManager.GetStats();
                Raylib.DrawText($"Rendered: {renderStats.RenderedItems}/{renderStats.TotalItems} | Culled: {renderStats.CulledItems} | LOD Bias: {lodStats.PerformanceLODBias}", 
                    10, GameConstants.SCREEN_HEIGHT - 50, 14, Color.Yellow);
                Raylib.DrawText($"Detail: H{lodStats.HighDetailObjects} M{lodStats.MediumDetailObjects} L{lodStats.LowDetailObjects}", 
                    10, GameConstants.SCREEN_HEIGHT - 70, 12, Color.SkyBlue);
                    
                // Show procedural asteroid stats
                if (_asteroidGenerator != null)
                {
                    var asteroidStats = _asteroidGenerator.GetStats();
                    Raylib.DrawText($"Cached Meshes: {asteroidStats.CachedMeshes} | Memory: {asteroidStats.MemoryUsageEstimate / 1024}KB", 
                        10, GameConstants.SCREEN_HEIGHT - 90, 12, Color.Lime);
                }
            }
            #endif

            // Draw controls help
            if (_gamePaused)
            {
                Raylib.DrawText("F3: Toggle 3D/2D | P: Pause | 1-4: Camera Modes | Ctrl+M: Motion Sickness", GameConstants.SCREEN_WIDTH / 2 - 200, 
                    GameConstants.SCREEN_HEIGHT / 2 + 40, GameConstants.FONT_SIZE_SMALL, Theme.TextColor);
                Raylib.DrawText("F5-F8: Camera Modes | F1-F4: Follow Styles | Q/E: Height | Wheel: Zoom", GameConstants.SCREEN_WIDTH / 2 - 200, 
                    GameConstants.SCREEN_HEIGHT / 2 + 60, GameConstants.FONT_SIZE_SMALL, Theme.TextColor);
            }
            
            // Draw camera info in 3D mode
            if (Renderer3DIntegration.Is3DEnabled && !_gamePaused)
            {
                var cameraInfo = Renderer3DIntegration.GetRenderStats();
                Raylib.DrawText("3D Camera Controls: 1-4 (Mode) | F1-F4 (Style) | Q/E (Height) | Wheel (Zoom)", 
                    10, GameConstants.SCREEN_HEIGHT - 70, 12, Color.Yellow);
            }

            Raylib.EndDrawing();
        }

        private void ResetGame()
        {
            _score = 0;
            _level = 1;
            _gameOver = false;
            _levelComplete = false;
            _gamePaused = false;

            if (_player != null)
            {
                _player.Position = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
                _player.Velocity = Vector2.Zero;
                _player.Rotation = 0;
                _player.IsShieldActive = false;
                _player.ShieldCooldown = 0;
            }

            // Clear bullets from pool
            _bulletPool?.Clear();
            
            // Clear explosion particle pool
            _explosionPool?.Clear();
            
            // Clear power-ups
            _powerUpManager?.Clear();
            
            // Clear player engine particles
            if (_player != null) _player.ClearEngineParticles();

            if (_explosions != null)
            {
                foreach (var explosion in _explosions)
                {
                    // Particles are managed by the pool automatically
                }
                _explosions.Clear();
            }

            if (_asteroids != null) _asteroids.Clear();

            if (_visualEffects != null) _visualEffects.Clear();

            StartLevel(_level);
        }

        private void StartLevel(int level)
        {
            if (_asteroids == null || _random == null || _player == null) return;

            _asteroids.Clear();
            int asteroidCount = GameConstants.BASE_ASTEROIDS_PER_LEVEL + (level - 1) * GameConstants.ASTEROIDS_INCREMENT_PER_LEVEL;
            
            for (int i = 0; i < asteroidCount; i++)
            {
                AsteroidSize size = (AsteroidSize)_random.Next(0, 3);
                Vector2 position;
                
                do
                {
                    position = new Vector2(_random.Next(0, GameConstants.SCREEN_WIDTH), _random.Next(0, GameConstants.SCREEN_HEIGHT));
                } while (Vector2.Distance(position, _player.Position) < 100);

                Vector2 velocity = new Vector2(
                    (float)(_random.NextDouble() * 4 - 2),
                    (float)(_random.NextDouble() * 4 - 2)
                );
                
                _asteroids.Add(new Asteroid(position, velocity, size, _random, level));
            }

            ErrorManager.LogInfo($"Started level {level} with {asteroidCount} asteroids");
        }

        private void Cleanup()
        {
            try
            {
                ErrorManager.LogInfo("Cleaning up game");
                
                if (_audioManager != null) _audioManager.Dispose();
                if (_visualEffects != null) _visualEffects.Clear();
                if (_bulletPool != null) _bulletPool.Clear();
                if (_explosionPool != null) _explosionPool.Clear();
                if (_settingsManager != null) _settingsManager.SaveSettings();
                
                // Cleanup enhanced rendering system
                _renderer?.Cleanup();
                _asteroidGenerator?.ClearCache();
                
                // Cleanup 3D rendering (legacy support)
                Renderer3DIntegration.Cleanup();
                
                ErrorManager.CleanupOldLogs();
                ErrorManager.LogInfo("Game cleanup completed");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error during cleanup", ex);
            }
        }
    }
}