using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Modular game engine that orchestrates all game systems and managers.
    /// Replaces the monolithic GameProgram class with clean separation of concerns.
    /// </summary>
    public class ModularGameEngine
    {
        // Core Managers
        private readonly GameStateManager _gameState;
        private readonly InputManager _inputManager;
        private readonly RenderingManager _renderingManager;
        private readonly CollisionManager _collisionManager;
        private readonly LevelManager _levelManager;
        private readonly EntityManager _entityManager;
        private readonly AudioManager _audioManager;
        private readonly SettingsManager _settingsManager;

        // Game Entities
        private PlayerEntity _player;
        private readonly List<AsteroidEntity> _asteroids;

        // Performance Systems
        private readonly GraphicsSettings _graphicsSettings;
        private readonly GraphicsProfiler _profiler;
        private readonly EnhancedAdaptiveGraphicsManager _adaptiveGraphics;
        
        // UI Systems
        private AnimatedHUD? _animatedHUD;

        // Game State
        private bool _isInitialized;
        private bool _isRunning;

        public ModularGameEngine()
        {
            // Initialize managers
            _gameState = new GameStateManager();
            _inputManager = new InputManager();
            _entityManager = new EntityManager();
            _audioManager = new AudioManager();
            _settingsManager = new SettingsManager();

            // Initialize performance systems
            _graphicsSettings = new GraphicsSettings();
            _profiler = new GraphicsProfiler();
            _adaptiveGraphics = new EnhancedAdaptiveGraphicsManager(_graphicsSettings, _profiler);

            // Initialize rendering and collision managers
            var effectsManager = new AdvancedEffectsManager();
            _renderingManager = new RenderingManager(_graphicsSettings, _profiler, effectsManager);
            _collisionManager = new CollisionManager(_entityManager);
            _levelManager = new LevelManager(_entityManager, _gameState);

            // Initialize entity collections
            _asteroids = new List<AsteroidEntity>();

            _isInitialized = false;
            _isRunning = false;
        }

        /// <summary>
        /// Initialize the game engine
        /// </summary>
        /// <returns>True if initialization successful</returns>
        public bool Initialize()
        {
            try
            {
                ErrorManager.LogInfo("Initializing ModularGameEngine");

                // Initialize Raylib
                Raylib.InitWindow(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT, "Asteroids - Modular");
                Raylib.SetTargetFPS(GameConstants.TARGET_FPS);

                // Initialize all managers
                if (!InitializeManagers())
                {
                    ErrorManager.LogError("Failed to initialize managers");
                    return false;
                }

                // Initialize UI systems
                _animatedHUD = new AnimatedHUD();
                ErrorManager.LogInfo("AnimatedHUD initialized");

                // Initialize game entities
                InitializeGameEntities();

                // Setup event handlers
                SetupEventHandlers();

                // Initialize dynamic theme
                DynamicTheme.ResetToLevel(_gameState.Level);

                _isInitialized = true;
                ErrorManager.LogInfo("ModularGameEngine initialization completed");
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Critical error during engine initialization", ex);
                return false;
            }
        }

        /// <summary>
        /// Run the main game loop
        /// </summary>
        public void Run()
        {
            if (!_isInitialized)
            {
                ErrorManager.LogError("Cannot run engine - not initialized");
                return;
            }

            _isRunning = true;
            ErrorManager.LogInfo("Starting game loop");

            while (!Raylib.WindowShouldClose() && _isRunning)
            {
                try
                {
                    float deltaTime = Raylib.GetFrameTime();
                    Update(deltaTime);
                    Render();
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error in game loop", ex);
                }
            }

            Cleanup();
        }

        /// <summary>
        /// Update all game systems
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private void Update(float deltaTime)
        {
            // Update core managers
            _inputManager.Update(deltaTime);
            _adaptiveGraphics.Update(deltaTime);
            _renderingManager.Update(deltaTime);
            
            // Update UI systems
            _animatedHUD?.Update(deltaTime);

            // Handle input if game is active
            if (!_gameState.IsGameOver && !_gameState.IsLevelComplete && !_gameState.IsPaused)
            {
                ProcessInput();
                UpdateGameLogic(deltaTime);
            }
            else
            {
                HandleMenuInput();
            }

            // Update entities through EntityManager
            _entityManager.Update(deltaTime);
        }

        /// <summary>
        /// Render all game elements
        /// </summary>
        private void Render()
        {
            _renderingManager.BeginFrame();

            if (!_gameState.IsGameOver && !_gameState.IsLevelComplete)
            {
                // Render background grid
                _renderingManager.RenderGrid(_settingsManager?.Current.Graphics.Basic.ShowGrid ?? false);

                // Render all entities through EntityManager
                _entityManager.Render(_renderingManager.GetCurrentRenderer());

                // Render HUD and UI
                RenderUI();
            }
            else
            {
                RenderMenuScreens();
            }

            if (_gameState.IsPaused)
            {
                RenderPauseScreen();
            }

            _renderingManager.EndFrame();
        }

        /// <summary>
        /// Process player input
        /// </summary>
        private void ProcessInput()
        {
            var inputState = _inputManager.GetInputState();

            // Handle pause input
            if (inputState.PauseInput)
            {
                _gameState.TogglePause();
            }

            // Handle player movement and actions
            if (_player != null)
            {
                _player.ProcessInput(inputState);

                // Handle shield input
                if (inputState.ShieldInput)
                {
                    if (!_player.IsShieldActive && _player.ShieldCooldown <= 0)
                    {
                        _player.ActivateShield();
                        _audioManager?.PlaySound("shield", 0.8f);
                    }
                }
            }

            // Handle shooting
            if (inputState.ShootInput)
            {
                FireBullet();
            }

            // Handle 3D mode toggle
            if (inputState.Toggle3DInput)
            {
                bool current3D = _renderingManager.Is3DActive();
                _renderingManager.SwitchRenderMode(!current3D);
                ErrorManager.LogInfo($"3D rendering toggled: {(!current3D ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Handle menu navigation input
        /// </summary>
        private void HandleMenuInput()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (_gameState.IsGameOver)
                {
                    RestartGame();
                }
                else if (_gameState.IsLevelComplete)
                {
                    StartNextLevel();
                }
            }
        }

        /// <summary>
        /// Update core game logic
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private void UpdateGameLogic(float deltaTime)
        {
            // Process collisions
            _collisionManager.ProcessCollisions();

            // Check level completion
            _levelManager.CheckLevelCompletion();
        }

        /// <summary>
        /// Initialize all manager systems
        /// </summary>
        /// <returns>True if all managers initialized successfully</returns>
        private bool InitializeManagers()
        {
            // Initialize rendering manager
            if (!_renderingManager.Initialize())
            {
                ErrorManager.LogError("Failed to initialize rendering manager");
                return false;
            }

            // Setup input handlers
            SetupInputHandlers();

            // Setup collision handlers
            SetupCollisionHandlers();

            ErrorManager.LogInfo("All managers initialized successfully");
            return true;
        }

        /// <summary>
        /// Initialize game entities
        /// </summary>
        private void InitializeGameEntities()
        {
            // Create player entity
            Vector2 playerStart = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
            _player = new PlayerEntity(_entityManager.GetNextEntityId(), playerStart, GameConstants.PLAYER_SIZE);
            _entityManager.AddEntity(_player);

            // Start first level
            _levelManager.StartLevel(_gameState.Level, _player.Position);

            ErrorManager.LogInfo("Game entities initialized");
        }

        /// <summary>
        /// Setup event handlers between managers
        /// </summary>
        private void SetupEventHandlers()
        {
            // Game state events
            _gameState.LevelChanged += OnLevelChanged;
            _gameState.GameOverChanged += OnGameOverChanged;
            _gameState.ScoreChanged += OnScoreChanged;

            // Input events
            _inputManager.ActionInput += OnActionInput;
        }

        /// <summary>
        /// Setup input action handlers
        /// </summary>
        private void SetupInputHandlers()
        {
            _inputManager.RegisterActionHandler("pause", () => _gameState.TogglePause());
            _inputManager.RegisterActionHandler("toggle_3d", () => {
                bool current3D = _renderingManager.Is3DActive();
                _renderingManager.SwitchRenderMode(!current3D);
            });
        }

        /// <summary>
        /// Setup collision event handlers
        /// </summary>
        private void SetupCollisionHandlers()
        {
            _collisionManager.RegisterCollisionHandler<PlayerEntity, AsteroidEntity>(OnPlayerAsteroidCollision);
            _collisionManager.RegisterCollisionHandler<BulletEntity, AsteroidEntity>(OnBulletAsteroidCollision);
        }

        /// <summary>
        /// Handle player-asteroid collision
        /// </summary>
        private void OnPlayerAsteroidCollision(PlayerEntity player, AsteroidEntity asteroid)
        {
            if (player.IsShieldActive)
            {
                // Destroy asteroid, player survives
                _entityManager.RemoveEntity(asteroid.Id);
                _gameState.AddScore(asteroid.GetScoreValue());
                _audioManager?.PlaySound("explosion", 0.6f);
            }
            else
            {
                // Use lives system instead of instant game over
                bool gameOver = _gameState.LoseLife();
                _entityManager.RemoveEntity(asteroid.Id);
                _audioManager?.PlaySound("explosion", 1.0f);
                
                if (!gameOver)
                {
                    // Player has lives remaining - respawn or continue
                    ErrorManager.LogInfo($"Player hit, {_gameState.Lives} lives remaining");
                    // TODO: Could add temporary invincibility or respawn logic here
                }
            }
        }

        /// <summary>
        /// Handle bullet-asteroid collision
        /// </summary>
        private void OnBulletAsteroidCollision(BulletEntity bullet, AsteroidEntity asteroid)
        {
            // Mark bullet as hit
            bullet.OnHit();
            _entityManager.RemoveEntity(bullet.Id);

            // Add score for destroyed asteroid
            _gameState.AddScore(asteroid.GetScoreValue());

            // Remove asteroid
            _entityManager.RemoveEntity(asteroid.Id);

            // Play explosion sound
            _audioManager?.PlaySound("explosion", 0.8f);

            ErrorManager.LogInfo($"Bullet {bullet.Id} destroyed asteroid {asteroid.Id}");
        }

        /// <summary>
        /// Fire a bullet from player position
        /// </summary>
        private void FireBullet()
        {
            if (_player == null) return;

            // Create bullet entity with player position and rotation
            Vector2 bulletDirection = new Vector2(
                MathF.Cos(_player.Rotation * MathF.PI / 180f),
                MathF.Sin(_player.Rotation * MathF.PI / 180f)
            );

            // Offset bullet spawn position to player front
            Vector2 bulletSpawnPos = _player.Position + bulletDirection * (GameConstants.PLAYER_SIZE + 5);

            // Create and add bullet entity
            var bulletEntity = new BulletEntity(
                _entityManager.GetNextEntityId(),
                bulletSpawnPos,
                bulletDirection
            );

            _entityManager.AddEntity(bulletEntity);
            _audioManager?.PlaySound("shoot", 0.6f);
        }

        /// <summary>
        /// Restart the game
        /// </summary>
        private void RestartGame()
        {
            _gameState.ResetGame();
            _entityManager.Clear();
            InitializeGameEntities();
        }

        /// <summary>
        /// Start the next level
        /// </summary>
        private void StartNextLevel()
        {
            _gameState.StartNextLevel();
            DynamicTheme.UpdateLevel(_gameState.Level);
            _levelManager.StartLevel(_gameState.Level, _player.Position);
        }

        /// <summary>
        /// Render UI elements
        /// </summary>
        private void RenderUI()
        {
            // Enhanced UI rendering with lives display
            string scoreText = $"Score: {_gameState.Score}";
            string levelText = $"Level: {_gameState.Level}";
            string livesText = $"Lives: {_gameState.Lives}";
            Color textColor = DynamicTheme.GetTextColor();

            // Score (top-left)
            Raylib.DrawText(scoreText, GameConstants.UI_PADDING, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, textColor);
            
            // Level (top-right)
            Raylib.DrawText(levelText, GameConstants.SCREEN_WIDTH - 100, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, textColor);

            // Lives (top-center)
            Raylib.DrawText(livesText, GameConstants.SCREEN_WIDTH / 2 - 30, GameConstants.UI_PADDING, 
                GameConstants.FONT_SIZE_MEDIUM, textColor);

            // Render shield status
            if (_player?.IsShieldActive == true)
            {
                Raylib.DrawText("SHIELD ACTIVE", GameConstants.SCREEN_WIDTH / 2 - 50, 50, 
                    GameConstants.FONT_SIZE_SMALL, DynamicTheme.GetShieldColor(0.8f));
            }

            // TODO: Re-enable AnimatedHUD in Phase 0.6 with PlayerEntity compatibility
        }

        /// <summary>
        /// Render menu screens
        /// </summary>
        private void RenderMenuScreens()
        {
            if (_gameState.IsLevelComplete)
            {
                string levelCompleteText = $"LEVEL {_gameState.Level} COMPLETE";
                string continueText = "PRESS [ENTER] TO START NEXT LEVEL";
                Color completeColor = DynamicTheme.GetLevelCompleteColor();
                Color textColor = DynamicTheme.GetTextColor();

                Raylib.DrawText(levelCompleteText, GameConstants.SCREEN_WIDTH / 2 - 150, 
                    GameConstants.SCREEN_HEIGHT / 2 - 20, GameConstants.FONT_SIZE_LARGE, completeColor);
                Raylib.DrawText(continueText, GameConstants.SCREEN_WIDTH / 2 - 200, 
                    GameConstants.SCREEN_HEIGHT / 2 + 20, GameConstants.FONT_SIZE_MEDIUM, textColor);
            }
            else if (_gameState.IsGameOver)
            {
                Raylib.DrawText("GAME OVER", GameConstants.SCREEN_WIDTH / 2 - 100, 
                    GameConstants.SCREEN_HEIGHT / 2 - 80, GameConstants.FONT_SIZE_LARGE, DynamicTheme.GetGameOverColor());
                Raylib.DrawText("PRESS [ENTER] TO RESTART", GameConstants.SCREEN_WIDTH / 2 - 150, 
                    GameConstants.SCREEN_HEIGHT / 2 + 120, GameConstants.FONT_SIZE_MEDIUM, DynamicTheme.GetTextColor());
            }
        }

        /// <summary>
        /// Render pause screen overlay
        /// </summary>
        private void RenderPauseScreen()
        {
            Raylib.DrawText("PAUSED", GameConstants.SCREEN_WIDTH / 2 - 60, 
                GameConstants.SCREEN_HEIGHT / 2 - 20, GameConstants.FONT_SIZE_LARGE, DynamicTheme.GetTextColor());
        }

        // Event handlers
        private void OnLevelChanged(int newLevel) => ErrorManager.LogInfo($"Level changed to {newLevel}");
        private void OnGameOverChanged(bool gameOver) => ErrorManager.LogInfo($"Game over: {gameOver}");
        private void OnScoreChanged(int newScore) => ErrorManager.LogInfo($"Score changed to {newScore}");
        private void OnActionInput(string action) => ErrorManager.LogInfo($"Action input: {action}");

        /// <summary>
        /// Cleanup all resources
        /// </summary>
        private void Cleanup()
        {
            try
            {
                ErrorManager.LogInfo("Cleaning up ModularGameEngine");

                _renderingManager?.Cleanup();
                _entityManager?.Clear();
                _audioManager?.Dispose();
                _settingsManager?.SaveSettings();

                ErrorManager.CleanupOldLogs();
                _isRunning = false;

                ErrorManager.LogInfo("ModularGameEngine cleanup completed");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error during engine cleanup", ex);
            }
        }
    }
}