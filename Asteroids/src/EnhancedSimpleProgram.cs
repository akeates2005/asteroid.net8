using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Enhanced version of SimpleProgram with Phase 2 improvements:
    /// - Advanced collision detection using spatial partitioning
    /// - Object pooling for performance
    /// - Performance monitoring with real-time dashboard
    /// - Enhanced game mechanics and visual effects
    /// </summary>
    public class EnhancedSimpleProgram
    {
        // Core systems
        private CollisionManager? _collisionManager;
        private PerformanceMonitor? _performanceMonitor;
        private PoolManager? _poolManager;
        
        // Existing systems
        private AudioManager? _audioManager;
        private SettingsManager? _settingsManager;
        private VisualEffectsManager? _visualEffects;
        
        // 3D Rendering
        private bool _render3D = false;

        // Game objects
        private Player? _player;
        private List<Bullet>? _bullets;
        private List<Asteroid>? _asteroids;
        private List<ExplosionParticle>? _explosions;
        private Leaderboard? _leaderboard;
        private Random? _random;

        // Game state
        private int _score;
        private int _level;
        private int _lives;
        private bool _gameOver;
        private bool _levelComplete;
        private bool _gamePaused;

        // Performance tracking
        private float _screenShakeIntensity;
        private float _screenShakeDuration;
        private float _screenShakeElapsed;

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
                ErrorManager.LogError("Critical error in enhanced program", ex);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void Initialize()
        {
            ErrorManager.LogInfo("Initializing enhanced Asteroids game");

            // Initialize Raylib
            Raylib.InitWindow(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT, "Asteroids - Enhanced Phase 2");
            Raylib.SetTargetFPS(GameConstants.TARGET_FPS);

            // Initialize core systems
            _collisionManager = new CollisionManager(
                GameConstants.SCREEN_WIDTH, 
                GameConstants.SCREEN_HEIGHT
            );
            
            _performanceMonitor = new PerformanceMonitor();
            _poolManager = new PoolManager();

            // Initialize existing managers
            _settingsManager = new SettingsManager();
            _audioManager = new AudioManager();
            _visualEffects = new VisualEffectsManager();

            // Initialize game objects
            _player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), GameConstants.PLAYER_SIZE);
            _bullets = new List<Bullet>();
            _asteroids = new List<Asteroid>();
            _explosions = new List<ExplosionParticle>();
            _leaderboard = new Leaderboard();
            _random = new Random();

            // Initialize 3D rendering
            if (Renderer3DIntegration.Initialize())
            {
                _render3D = true;
                ErrorManager.LogInfo("3D rendering enabled");
            }
            else
            {
                ErrorManager.LogInfo("3D rendering disabled, using 2D mode");
            }

            // Initialize game state
            ResetGame();

            ErrorManager.LogInfo("Enhanced game initialization completed");
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
                    ErrorManager.LogError("Error in enhanced game loop", ex);
                }
            }
        }

        private void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            
            // Update performance monitoring
            using (_performanceMonitor.ProfileOperation("TotalUpdate"))
            {
                UpdateSystems(deltaTime);
                
                if (!_gameOver && !_levelComplete && !_gamePaused)
                {
                    UpdateGameLogic();
                }
                else
                {
                    HandleGameStateInput();
                }
                
                // Update performance monitor
                int totalObjects = (_bullets?.Count ?? 0) + (_asteroids?.Count ?? 0) + (_explosions?.Count ?? 0);
                _performanceMonitor.Update(
                    deltaTime, 
                    totalObjects,
                    _asteroids?.Count ?? 0,
                    _bullets?.Count ?? 0,
                    _explosions?.Count ?? 0,
                    _score,
                    _level,
                    _lives
                );
            }
        }

        private void UpdateSystems(float deltaTime)
        {
            using (_performanceMonitor.ProfileOperation("SystemsUpdate"))
            {
                if (_audioManager != null) _audioManager.Update();
                if (_visualEffects != null) _visualEffects.Update();

                // Handle pause toggle
                if (Raylib.IsKeyPressed(KeyboardKey.P))
                {
                    _gamePaused = !_gamePaused;
                }

                // Handle 3D toggle
                if (Raylib.IsKeyPressed(KeyboardKey.F3))
                {
                    Renderer3DIntegration.Toggle3DMode();
                }

                // Handle camera controls in 3D mode
                if (Renderer3DIntegration.Is3DEnabled)
                {
                    Renderer3DIntegration.HandleCameraInput();
                }

                // Update screen shake
                if (_screenShakeElapsed < _screenShakeDuration)
                {
                    _screenShakeElapsed += deltaTime;
                    if (_screenShakeElapsed >= _screenShakeDuration)
                    {
                        _screenShakeIntensity = 0f;
                        _screenShakeElapsed = 0f;
                    }
                }
            }
        }

        private void UpdateGameLogic()
        {
            if (_player == null || _bullets == null || _asteroids == null || _explosions == null || _random == null) 
                return;

            using (_performanceMonitor.ProfileOperation("GameLogic"))
            {
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
                    if (_visualEffects != null) _visualEffects.AddFlashEffect(Theme.ShieldColor, 0.3f, 0.2f);
                }

                // Update bullets
                using (_performanceMonitor.ProfileOperation("BulletUpdate"))
                {
                    for (int i = _bullets.Count - 1; i >= 0; i--)
                    {
                        _bullets[i].Update();
                        if (!_bullets[i].Active)
                        {
                            var bullet = _bullets[i];
                            _bullets.RemoveAt(i);
                            _poolManager.ReturnBullet(bullet);
                        }
                    }
                }

                // Update asteroids
                using (_performanceMonitor.ProfileOperation("AsteroidUpdate"))
                {
                    foreach (var asteroid in _asteroids)
                    {
                        asteroid.Update();
                    }
                }

                // Update explosions
                using (_performanceMonitor.ProfileOperation("ParticleUpdate"))
                {
                    for (int i = _explosions.Count - 1; i >= 0; i--)
                    {
                        _explosions[i].Update();
                        if (!_explosions[i].IsActive)
                        {
                            var explosion = _explosions[i];
                            _explosions.RemoveAt(i);
                            _poolManager.ReturnParticle(explosion);
                        }
                    }
                }

                // Advanced collision detection using spatial partitioning
                using (_performanceMonitor.ProfileOperation("CollisionDetection"))
                {
                    CheckCollisionsAdvanced();
                }

                // Check level completion
                if (_asteroids.Count == 0)
                {
                    _levelComplete = true;
                }
            }
        }

        private void CheckCollisionsAdvanced()
        {
            if (_player == null || _bullets == null || _asteroids == null) return;

            // Use the advanced collision detection system
            var result = _collisionManager.UpdateCollisions(_bullets, _asteroids, _player);

            // Process bullet-asteroid collisions
            foreach (var (bullet, asteroid) in result.BulletAsteroidCollisions)
            {
                bullet.Active = false;
                asteroid.Active = false;
                _score += GameEnhancements.CalculateAsteroidScore(asteroid.AsteroidSize, _level);
                
                // Create explosion
                CreateExplosionAt(asteroid.Position, asteroid.Radius / 20f);
                
                // Split asteroid if needed
                var newAsteroids = GameEnhancements.SplitAsteroid(asteroid, bullet.Position, _random);
                _asteroids.AddRange(newAsteroids);
                
                if (_audioManager != null) _audioManager.PlaySound("explosion", 0.8f);
                AddScreenShake(asteroid.Radius / 20f, 0.2f);
            }

            // Process player-asteroid collisions
            foreach (var (player, asteroid) in result.PlayerAsteroidCollisions)
            {
                if (player.IsShieldActive)
                {
                    asteroid.Active = false;
                    CreateExplosionAt(asteroid.Position, asteroid.Radius / 20f);
                    if (_audioManager != null) _audioManager.PlaySound("explosion", 0.6f);
                    AddScreenShake(asteroid.Radius / 20f, 0.15f);
                }
                else
                {
                    _lives--;
                    if (_lives <= 0)
                    {
                        _gameOver = true;
                    }
                    
                    if (_visualEffects != null) _visualEffects.CreateExplosionEffects(player.Position, 1.0f);
                    if (_audioManager != null) _audioManager.PlaySound("explosion", 1.0f);
                    AddScreenShake(3f, 1f);
                    
                    // Reset player position if still alive
                    if (!_gameOver)
                    {
                        player.Position = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
                        player.Velocity = Vector2.Zero;
                        player.IsShieldActive = true;
                        player.ShieldDuration = GameConstants.MAX_SHIELD_DURATION;
                    }
                }
            }

            // Process asteroid-asteroid collisions (optional physics)
            foreach (var (asteroidA, asteroidB) in result.AsteroidAsteroidCollisions)
            {
                GameEnhancements.ApplyCollisionResponse(asteroidA, asteroidB);
            }

            // Remove inactive objects
            _asteroids.RemoveAll(a => !a.Active);
        }

        private void FireBullet()
        {
            if (_bullets == null || _player == null) return;

            var bullet = _poolManager.GetBullet();
            if (bullet != null)
            {
                Vector2 bulletDirection = Vector2.Transform(new Vector2(0, -1), Matrix3x2.CreateRotation(MathF.PI / 180 * _player.Rotation));
                bullet.Position = _player.Position;
                bullet.Velocity = bulletDirection * GameConstants.BULLET_SPEED;
                bullet.Active = true;
                _bullets.Add(bullet);

                if (_audioManager != null) _audioManager.PlaySound("shoot", 0.6f);
                AddScreenShake(0.5f, 0.05f);
            }
        }

        private void CreateExplosionAt(Vector2 position, float intensity = 0.5f)
        {
            if (_explosions == null || _random == null) return;

            if (_visualEffects != null) _visualEffects.CreateExplosionEffects(position, intensity);

            var particles = GameEnhancements.CreateExplosionEffect(position, intensity, Theme.ExplosionColor, _random);
            foreach (var particle in particles)
            {
                var pooledParticle = _poolManager.GetParticle();
                if (pooledParticle != null)
                {
                    pooledParticle.Position = particle.Position;
                    pooledParticle.Velocity = particle.Velocity;
                    pooledParticle.Lifespan = particle.Lifespan;
                    pooledParticle.Color = particle.Color;
                    // IsActive is automatically managed by Lifespan property
                    _explosions.Add(pooledParticle);
                }
            }
        }

        private void AddScreenShake(float intensity, float duration)
        {
            if (intensity > _screenShakeIntensity)
            {
                _screenShakeIntensity = intensity;
                _screenShakeDuration = duration;
                _screenShakeElapsed = 0f;
            }
            
            if (Renderer3DIntegration.Is3DEnabled)
            {
                Renderer3DIntegration.AddCameraShake(intensity * 2f, duration);
            }
        }

        private void HandleGameStateInput()
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
                    StartLevel(_level);
                }
            }
        }

        private void Render()
        {
            Raylib.BeginDrawing();
            
            // Apply screen shake if active
            Vector2 screenShake = Vector2.Zero;
            if (_screenShakeElapsed < _screenShakeDuration && _random != null)
            {
                screenShake = GameEnhancements.CalculateScreenShake(_screenShakeIntensity, _screenShakeDuration, _screenShakeElapsed, _random);
            }

            Raylib.ClearBackground(Color.Black);

            // Begin 3D rendering if enabled
            if (Renderer3DIntegration.Is3DEnabled && _player != null)
            {
                float deltaTime = Raylib.GetFrameTime();
                Renderer3DIntegration.BeginFrame(_player.Position, _player.Velocity, deltaTime);
            }

            using (_performanceMonitor.ProfileOperation("Rendering"))
            {
                DrawGameContent(screenShake);
                DrawUI();
            }

            // End 3D rendering if enabled
            if (Renderer3DIntegration.Is3DEnabled)
            {
                Renderer3DIntegration.EndFrame();
            }

            // Draw visual effects
            if (_visualEffects != null) _visualEffects.Draw();

            DrawDebugInfo();

            Raylib.EndDrawing();
        }

        private void DrawGameContent(Vector2 screenShake)
        {
            // Draw grid
            if (_settingsManager?.Current.Graphics.ShowGrid == true)
            {
                if (Renderer3DIntegration.Is3DEnabled)
                {
                    Renderer3DIntegration.RenderGrid(true);
                }
                else
                {
                    for (int i = 0; i < GameConstants.SCREEN_WIDTH; i += GameConstants.GRID_SIZE)
                    {
                        Raylib.DrawLine((int)(i + screenShake.X), 0, (int)(i + screenShake.X), GameConstants.SCREEN_HEIGHT, Theme.GridColor);
                    }
                    for (int i = 0; i < GameConstants.SCREEN_HEIGHT; i += GameConstants.GRID_SIZE)
                    {
                        Raylib.DrawLine(0, (int)(i + screenShake.Y), GameConstants.SCREEN_WIDTH, (int)(i + screenShake.Y), Theme.GridColor);
                    }
                }
            }

            if (!_gameOver && !_levelComplete)
            {
                DrawGameObjects(screenShake);
            }
            else if (_levelComplete)
            {
                DrawLevelComplete();
            }
            else
            {
                DrawGameOver();
            }

            if (_gamePaused)
            {
                DrawPauseMenu();
            }
        }

        private void DrawGameObjects(Vector2 screenShake)
        {
            // Draw game objects (3D or 2D based on mode)
            if (Renderer3DIntegration.Is3DEnabled)
            {
                // Render in 3D
                if (_player != null) 
                {
                    Renderer3DIntegration.RenderPlayer(_player.Position, _player.Rotation, 
                        Theme.PlayerColor, _player.IsShieldActive);
                }

                if (_bullets != null)
                {
                    foreach (var bullet in _bullets)
                    {
                        if (bullet.Active) 
                        {
                            Renderer3DIntegration.RenderBullet(bullet.Position, Theme.BulletColor);
                        }
                    }
                }

                if (_asteroids != null)
                {
                    foreach (var asteroid in _asteroids)
                    {
                        if (asteroid.Active) 
                        {
                            Renderer3DIntegration.RenderAsteroid(asteroid.Position, asteroid.Radius, 
                                Theme.AsteroidColor, asteroid.GetHashCode());
                        }
                    }
                }

                if (_explosions != null)
                {
                    foreach (var explosion in _explosions)
                    {
                        if (explosion.IsActive)
                        {
                            float intensity = explosion.Lifespan / (float)GameConstants.EXPLOSION_PARTICLE_LIFESPAN;
                            Renderer3DIntegration.RenderExplosion(explosion.Position, intensity, explosion.Color);
                        }
                    }
                }
            }
            else
            {
                // Render in 2D with screen shake
                if (_player != null) 
                {
                    var originalPos = _player.Position;
                    _player.Position += screenShake;
                    _player.Draw();
                    _player.Position = originalPos;
                }

                if (_bullets != null)
                {
                    foreach (var bullet in _bullets)
                    {
                        if (bullet.Active)
                        {
                            var originalPos = bullet.Position;
                            bullet.Position += screenShake;
                            bullet.Draw();
                            bullet.Position = originalPos;
                        }
                    }
                }

                if (_asteroids != null)
                {
                    foreach (var asteroid in _asteroids)
                    {
                        if (asteroid.Active)
                        {
                            var originalPos = asteroid.Position;
                            asteroid.Position += screenShake;
                            asteroid.Draw();
                            asteroid.Position = originalPos;
                        }
                    }
                }

                if (_explosions != null)
                {
                    foreach (var explosion in _explosions)
                    {
                        if (explosion.IsActive)
                        {
                            var originalPos = explosion.Position;
                            explosion.Position += screenShake;
                            explosion.Draw();
                            explosion.Position = originalPos;
                        }
                    }
                }
            }
        }

        private void DrawUI()
        {
            // Game UI
            Raylib.DrawText($"Score: {_score}", GameConstants.UI_PADDING, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
            Raylib.DrawText($"Level: {_level}", GameConstants.SCREEN_WIDTH - 100, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
            Raylib.DrawText($"Lives: {_lives}", GameConstants.SCREEN_WIDTH / 2 - 30, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);

            if (_player?.IsShieldActive == true)
            {
                Raylib.DrawText("SHIELD ACTIVE", GameConstants.SCREEN_WIDTH / 2 - 50, 50, 
                    GameConstants.FONT_SIZE_SMALL, Theme.ShieldColor);
            }
        }

        private void DrawLevelComplete()
        {
            Raylib.DrawText($"LEVEL {_level} COMPLETE", GameConstants.SCREEN_WIDTH / 2 - 150, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                GameConstants.FONT_SIZE_LARGE, Theme.LevelCompleteColor);
            Raylib.DrawText("PRESS [ENTER] TO START NEXT LEVEL", GameConstants.SCREEN_WIDTH / 2 - 200, 
                GameConstants.SCREEN_HEIGHT / 2 + 20, GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
        }

        private void DrawGameOver()
        {
            Raylib.DrawText("GAME OVER", GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 - 80, 
                GameConstants.FONT_SIZE_LARGE, Theme.GameOverColor);

            if (_leaderboard?.Scores != null)
            {
                Raylib.DrawText("LEADERBOARD", GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                    GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
                for (int i = 0; i < Math.Min(_leaderboard.Scores.Count, 5); i++)
                {
                    Raylib.DrawText($"{i + 1}. {_leaderboard.Scores[i]}", 
                        GameConstants.SCREEN_WIDTH / 2 - 100, GameConstants.SCREEN_HEIGHT / 2 + 10 + (i * 20), 
                        GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
                }
            }

            Raylib.DrawText("PRESS [ENTER] TO RESTART", GameConstants.SCREEN_WIDTH / 2 - 150, 
                GameConstants.SCREEN_HEIGHT / 2 + 120, GameConstants.FONT_SIZE_MEDIUM, Theme.TextColor);
        }

        private void DrawPauseMenu()
        {
            Raylib.DrawText("PAUSED", GameConstants.SCREEN_WIDTH / 2 - 60, GameConstants.SCREEN_HEIGHT / 2 - 20, 
                GameConstants.FONT_SIZE_LARGE, Theme.TextColor);
            
            Raylib.DrawText("F3: Toggle 3D/2D | P: Pause | F12: Performance Monitor", GameConstants.SCREEN_WIDTH / 2 - 250, 
                GameConstants.SCREEN_HEIGHT / 2 + 40, GameConstants.FONT_SIZE_SMALL, Theme.TextColor);
                
            if (Renderer3DIntegration.Is3DEnabled)
            {
                Raylib.DrawText("1-4: Camera Modes | F1-F4: Follow Styles | Q/E: Height | Wheel: Zoom", GameConstants.SCREEN_WIDTH / 2 - 280, 
                    GameConstants.SCREEN_HEIGHT / 2 + 60, GameConstants.FONT_SIZE_SMALL, Theme.TextColor);
            }
        }

        private void DrawDebugInfo()
        {
            // Performance and collision statistics
            var collisionStats = _collisionManager.GetStats();
            var performanceStats = _performanceMonitor.CurrentStats;
            
            if (performanceStats.IsPerformanceWarning)
            {
                Raylib.DrawText("⚠️ PERFORMANCE WARNING", 10, 10, 16, Color.Red);
            }

            #if DEBUG
            string mode = Renderer3DIntegration.Is3DEnabled ? "3D" : "2D";
            Raylib.DrawText($"FPS: {Raylib.GetFPS()} | Mode: {mode} | Objects: {performanceStats.TotalObjects}", 
                10, GameConstants.SCREEN_HEIGHT - 50, 14, Color.Green);
            
            Raylib.DrawText($"Collisions: {collisionStats.ActualCollisions}/{collisionStats.PotentialCollisionPairs} " +
                $"({collisionStats.CollisionEfficiency:F1}%) | Algorithm: {collisionStats.SelectedAlgorithm}", 
                10, GameConstants.SCREEN_HEIGHT - 30, 12, Color.Yellow);
            #endif

            // Draw controls help
            if (!_gamePaused)
            {
                Raylib.DrawText("F12: Performance Monitor | F3: Toggle 3D | P: Pause", 
                    10, GameConstants.SCREEN_HEIGHT - 70, 12, Color.Gray);
            }
        }

        private void ResetGame()
        {
            _score = 0;
            _level = 1;
            _lives = 3;
            _gameOver = false;
            _levelComplete = false;
            _gamePaused = false;
            
            _screenShakeIntensity = 0f;
            _screenShakeDuration = 0f;
            _screenShakeElapsed = 0f;

            if (_player != null)
            {
                _player.Position = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
                _player.Velocity = Vector2.Zero;
                _player.Rotation = 0;
                _player.IsShieldActive = false;
                _player.ShieldCooldown = 0;
            }

            // Clear and return pooled objects
            if (_bullets != null)
            {
                foreach (var bullet in _bullets)
                {
                    _poolManager.ReturnBullet(bullet);
                }
                _bullets.Clear();
            }

            if (_explosions != null)
            {
                foreach (var explosion in _explosions)
                {
                    _poolManager.ReturnParticle(explosion);
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
            
            // Use enhanced asteroid generation
            var newAsteroids = GameEnhancements.GenerateAsteroidField(
                GameConstants.SCREEN_WIDTH, 
                GameConstants.SCREEN_HEIGHT, 
                GameEnhancements.GetAsteroidCountForLevel(level), 
                level, 
                _random, 
                _player.Position
            );
            
            _asteroids.AddRange(newAsteroids);

            ErrorManager.LogInfo($"Started enhanced level {level} with {_asteroids.Count} asteroids");
        }

        private void Cleanup()
        {
            try
            {
                ErrorManager.LogInfo("Cleaning up enhanced game");
                
                // Export performance report
                _performanceMonitor.ExportReport($"asteroids_performance_level_{_level}.txt");
                
                if (_audioManager != null) _audioManager.Dispose();
                if (_visualEffects != null) _visualEffects.Clear();
                if (_settingsManager != null) _settingsManager.SaveSettings();
                
                _poolManager?.Dispose();
                
                // Cleanup 3D rendering
                Renderer3DIntegration.Cleanup();
                
                ErrorManager.CleanupOldLogs();
                ErrorManager.LogInfo("Enhanced game cleanup completed");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error during enhanced cleanup", ex);
            }
        }
    }
}