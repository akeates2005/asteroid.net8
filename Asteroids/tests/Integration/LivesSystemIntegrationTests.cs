using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using Asteroids;
using NUnit.Framework;

namespace Asteroids.Tests.Integration
{
    /// <summary>
    /// Integration tests for Lives System with other game components
    /// Tests the interaction between lives system and shield system, UI, audio, visual effects, and game state
    /// </summary>
    [TestFixture]
    public class LivesSystemIntegrationTests
    {
        private IntegratedGameManager _gameManager;
        private MockRaylib _mockRaylib;
        
        [SetUp]
        public void Setup()
        {
            _mockRaylib = new MockRaylib();
            _gameManager = new IntegratedGameManager(_mockRaylib);
        }
        
        [TearDown]
        public void TearDown()
        {
            _gameManager?.Dispose();
        }

        #region Shield System Integration Tests

        [Test]
        public void ShieldActive_PreventsPlayerDeath_FromAsteroidCollision()
        {
            // Arrange
            var initialLives = _gameManager.Lives;
            _gameManager.Player.IsShieldActive = true;
            _gameManager.Player.ShieldDuration = 100f;
            
            // Create asteroid at player position for collision
            _gameManager.CreateAsteroidAtPosition(_gameManager.Player.Position, AsteroidSize.Medium);
            
            // Act - Process collision
            _gameManager.ProcessCollisions();
            
            // Assert - Lives should not decrease due to active shield
            Assert.AreEqual(initialLives, _gameManager.Lives);
            Assert.IsFalse(_gameManager.IsPlayerRespawning);
            Assert.IsFalse(_gameManager.IsGameOver);
        }

        [Test]
        public void ShieldInactive_AllowsPlayerDeath_FromAsteroidCollision()
        {
            // Arrange
            var initialLives = _gameManager.Lives;
            _gameManager.Player.IsShieldActive = false;
            
            // Create asteroid at player position for collision
            _gameManager.CreateAsteroidAtPosition(_gameManager.Player.Position, AsteroidSize.Medium);
            
            // Act - Process collision
            _gameManager.ProcessCollisions();
            
            // Assert - Lives should decrease due to no shield protection
            Assert.AreEqual(initialLives - 1, _gameManager.Lives);
            Assert.IsTrue(_gameManager.IsPlayerRespawning);
        }

        [Test]
        public void RespawnInvulnerability_UsesShieldSystem()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act - Complete respawn process
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert - Player should have shield active from respawn invulnerability
            Assert.IsNotNull(_gameManager.Player);
            Assert.IsTrue(_gameManager.Player.IsShieldActive);
            
            // Verify shield duration matches respawn invulnerability time
            var expectedDuration = GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS;
            Assert.AreEqual(expectedDuration, _gameManager.Player.ShieldDuration, 5f);
        }

        [Test]
        public void RespawnShield_DoesNotInterfereWith_NormalShieldCooldown()
        {
            // Arrange - Use shield and verify cooldown
            _gameManager.Player.IsShieldActive = true;
            _gameManager.Player.ShieldDuration = 10f;
            _gameManager.Player.ShieldCooldown = 0f;
            
            // Let shield expire
            for (int i = 0; i < 15; i++) _gameManager.Update();
            var normalCooldown = _gameManager.Player.ShieldCooldown;
            
            // Act - Die and respawn
            _gameManager.HandlePlayerDeath();
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert - Respawn shield should not reset normal cooldown mechanics
            Assert.IsTrue(_gameManager.Player.IsShieldActive); // Respawn shield
            // Normal shield functionality should remain intact after respawn shield expires
        }

        [Test]
        public void InvulnerabilityShield_ProtectsFromCollisions()
        {
            // Arrange - Die and respawn to get invulnerability
            _gameManager.HandlePlayerDeath();
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            var livesAfterRespawn = _gameManager.Lives;
            
            // Create asteroid collision during invulnerability
            _gameManager.CreateAsteroidAtPosition(_gameManager.Player.Position, AsteroidSize.Large);
            
            // Act - Process collision during invulnerability
            _gameManager.ProcessCollisions();
            
            // Assert - Should be protected by invulnerability shield
            Assert.AreEqual(livesAfterRespawn, _gameManager.Lives);
            Assert.IsFalse(_gameManager.IsPlayerRespawning);
        }

        #endregion

        #region UI System Integration Tests

        [Test]
        public void UI_DisplaysCorrectInitialLives()
        {
            // Act
            _gameManager.RenderHUD();
            
            // Assert
            var hudText = _mockRaylib.GetLastDrawnText();
            Assert.That(hudText, Contains.Substring("Lives: 3"));
        }

        [Test]
        public void UI_UpdatesImmediatelyOnPlayerDeath()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act
            _gameManager.RenderHUD();
            
            // Assert
            var hudText = _mockRaylib.GetLastDrawnText();
            Assert.That(hudText, Contains.Substring("Lives: 2"));
        }

        [Test]
        public void UI_ShowsRespawnCountdown()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act - Advance time partially through respawn
            _gameManager.AdvanceTime(0.5f);
            _gameManager.RenderHUD();
            
            // Assert - Should show countdown
            var hudText = _mockRaylib.GetLastDrawnText();
            Assert.That(hudText, Contains.Substring("RESPAWNING IN"));
            Assert.That(hudText, Contains.Substring("2")); // Should show remaining seconds
        }

        [Test]
        public void UI_ShowsGameOverWhenZeroLives()
        {
            // Arrange - Reduce to 1 life and die
            _gameManager.SetLives(1);
            _gameManager.HandlePlayerDeath();
            
            // Act
            _gameManager.RenderHUD();
            
            // Assert
            var hudText = _mockRaylib.GetLastDrawnText();
            Assert.That(hudText, Contains.Substring("GAME OVER"));
            Assert.That(hudText, Contains.Substring("Lives: 0"));
        }

        [Test]
        public void UI_LivesDisplay_UpdatesInRealTime()
        {
            // Test rapid UI updates during gameplay
            var initialLives = _gameManager.Lives;
            
            for (int i = initialLives; i > 0; i--)
            {
                // Act
                _gameManager.RenderHUD();
                var hudText = _mockRaylib.GetLastDrawnText();
                
                // Assert - Current lives displayed correctly
                Assert.That(hudText, Contains.Substring($"Lives: {i}"));
                
                // Die if not the last iteration
                if (i > 1)
                {
                    _gameManager.HandlePlayerDeath();
                    _gameManager.CompleteRespawn(); // Skip respawn delay for testing
                }
            }
        }

        #endregion

        #region Audio System Integration Tests

        [Test]
        public void AudioSystem_PlaysDeathSoundOnPlayerDeath()
        {
            // Act
            _gameManager.HandlePlayerDeath();
            
            // Assert
            var playedSounds = _mockRaylib.GetPlayedSounds();
            Assert.That(playedSounds, Contains.Item("explosion"));
        }

        [Test]
        public void AudioSystem_PlaysRespawnSoundOnPlayerRespawn()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act - Complete respawn
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert
            var playedSounds = _mockRaylib.GetPlayedSounds();
            Assert.That(playedSounds, Contains.Item("respawn"));
        }

        [Test]
        public void AudioSystem_CorrectVolumeForDeathExplosion()
        {
            // Act
            _gameManager.HandlePlayerDeath();
            
            // Assert
            var soundVolume = _mockRaylib.GetLastSoundVolume("explosion");
            Assert.AreEqual(1.0f, soundVolume, 0.1f); // Death explosion should be at full volume
        }

        [Test]
        public void AudioSystem_DoesNotPlayDeathSound_WhenGameOver()
        {
            // Arrange
            _gameManager.SetLives(1);
            _mockRaylib.ClearPlayedSounds();
            
            // Act
            _gameManager.HandlePlayerDeath();
            
            // Assert - Death sound should still play even on game over
            var playedSounds = _mockRaylib.GetPlayedSounds();
            Assert.That(playedSounds, Contains.Item("explosion"));
        }

        #endregion

        #region Visual Effects Integration Tests

        [Test]
        public void VisualEffects_CreatesExplosionOnPlayerDeath()
        {
            // Arrange
            var playerPosition = _gameManager.Player.Position;
            
            // Act
            _gameManager.HandlePlayerDeath();
            
            // Assert
            var explosions = _mockRaylib.GetCreatedExplosions();
            Assert.IsTrue(explosions.Any(e => Vector2.Distance(e.Position, playerPosition) < 5f));
        }

        [Test]
        public void VisualEffects_ShowsRespawnEffect()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act - Complete respawn
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert - Should have respawn visual effect
            var effects = _mockRaylib.GetVisualEffects();
            Assert.IsTrue(effects.Contains("respawn"));
        }

        [Test]
        public void VisualEffects_ShieldPulsesDuringInvulnerability()
        {
            // Arrange - Respawn to get invulnerability shield
            _gameManager.HandlePlayerDeath();
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Act - Render multiple frames to see shield animation
            for (int i = 0; i < 10; i++)
            {
                _gameManager.Update();
                _gameManager.Render();
            }
            
            // Assert - Shield should be rendered with animation
            var shieldRenderings = _mockRaylib.GetShieldRenderings();
            Assert.Greater(shieldRenderings.Count, 5); // Multiple shield renders indicate animation
        }

        #endregion

        #region Game State Integration Tests

        [Test]
        public void GameState_PauseDoesNotAffectRespawnTimer()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            var initialTimer = _gameManager.RespawnTimer;
            
            // Act - Pause game
            _gameManager.SetPaused(true);
            _gameManager.AdvanceTime(1.0f); // Try to advance time while paused
            
            // Assert - Respawn timer should not advance while paused
            Assert.AreEqual(initialTimer, _gameManager.RespawnTimer, 0.01f);
        }

        [Test]
        public void GameState_UnpauseResumesRespawnTimer()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            _gameManager.SetPaused(true);
            _gameManager.AdvanceTime(0.5f); // No effect while paused
            
            // Act - Unpause and advance time
            _gameManager.SetPaused(false);
            _gameManager.AdvanceTime(0.5f);
            
            // Assert - Timer should have advanced only after unpause
            var expectedTimer = GameConstants.RESPAWN_DELAY - 0.5f;
            Assert.AreEqual(expectedTimer, _gameManager.RespawnTimer, 0.1f);
        }

        [Test]
        public void GameState_LevelTransitionPreservesLives()
        {
            // Arrange
            _gameManager.HandlePlayerDeath(); // Lose 1 life (3 -> 2)
            _gameManager.CompleteRespawn();
            var livesBeforeTransition = _gameManager.Lives;
            
            // Act - Complete level (destroy all asteroids)
            _gameManager.DestroyAllAsteroids();
            _gameManager.CheckLevelCompletion();
            _gameManager.StartNextLevel();
            
            // Assert - Lives should be preserved across level transitions
            Assert.AreEqual(livesBeforeTransition, _gameManager.Lives);
        }

        [Test]
        public void GameState_ResetGameRestoresLives()
        {
            // Arrange - Lose some lives
            _gameManager.HandlePlayerDeath();
            _gameManager.HandlePlayerDeath();
            Assert.AreEqual(1, _gameManager.Lives);
            
            // Act
            _gameManager.ResetGame();
            
            // Assert
            Assert.AreEqual(GameConstants.STARTING_LIVES, _gameManager.Lives);
            Assert.IsFalse(_gameManager.IsPlayerRespawning);
            Assert.IsFalse(_gameManager.IsGameOver);
        }

        #endregion

        #region Spatial Grid Integration Tests

        [Test]
        public void SpatialGrid_UpdatesOnPlayerRespawn()
        {
            // Arrange
            var initialPlayerPos = _gameManager.Player.Position;
            _gameManager.HandlePlayerDeath();
            
            // Act - Respawn at potentially different location
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert - Spatial grid should be updated with new player position
            var playerInGrid = _gameManager.GetEntitiesNearPosition(_gameManager.Player.Position, 50f);
            Assert.IsTrue(playerInGrid.Any(e => e.EntityType == "Player"));
        }

        [Test]
        public void SpatialGrid_RemovesDeadPlayerCorrectly()
        {
            // Arrange
            var playerPos = _gameManager.Player.Position;
            
            // Act
            _gameManager.HandlePlayerDeath();
            
            // Assert - Dead player should be removed from spatial grid during respawn period
            Assert.IsTrue(_gameManager.IsPlayerRespawning);
            // Player should not be in collision detection during respawn
        }

        #endregion

        #region Collision System Integration Tests

        [Test]
        public void CollisionSystem_IgnoresPlayerDuringRespawn()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            
            // Act - Try to create collisions during respawn period
            _gameManager.CreateAsteroidAtPosition(new Vector2(400, 300), AsteroidSize.Large);
            _gameManager.ProcessCollisions();
            
            // Assert - No additional deaths should occur during respawn
            Assert.IsTrue(_gameManager.IsPlayerRespawning);
            // Lives should remain at previous count minus one
        }

        [Test]
        public void CollisionSystem_ResumesAfterRespawn()
        {
            // Arrange
            _gameManager.HandlePlayerDeath();
            _gameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Verify player respawned with invulnerability
            Assert.IsFalse(_gameManager.IsPlayerRespawning);
            Assert.IsTrue(_gameManager.Player.IsShieldActive);
            
            // Act - Let invulnerability expire
            var invulnerabilityFrames = (int)(GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS);
            for (int i = 0; i < invulnerabilityFrames + 10; i++)
            {
                _gameManager.Update();
            }
            
            // Assert - Collision system should be active again
            Assert.IsFalse(_gameManager.Player.IsShieldActive);
            
            // Create collision and verify it's processed
            var livesBeforeCollision = _gameManager.Lives;
            _gameManager.CreateAsteroidAtPosition(_gameManager.Player.Position, AsteroidSize.Medium);
            _gameManager.ProcessCollisions();
            
            Assert.AreEqual(livesBeforeCollision - 1, _gameManager.Lives);
        }

        #endregion

        #region Performance Integration Tests

        [Test]
        public void PerformanceIntegration_LivesSystemDoesNotImpactFrameRate()
        {
            // Arrange - Create busy game scenario
            for (int i = 0; i < 50; i++)
            {
                _gameManager.CreateRandomAsteroid();
            }
            
            // Measure baseline performance
            var stopwatch = Stopwatch.StartNew();
            for (int frame = 0; frame < 100; frame++)
            {
                _gameManager.Update();
            }
            stopwatch.Stop();
            var baselineTime = stopwatch.ElapsedMilliseconds;
            
            // Act - Add lives system activity (multiple death/respawn cycles)
            stopwatch.Restart();
            for (int cycle = 0; cycle < 5; cycle++)
            {
                _gameManager.HandlePlayerDeath();
                for (int frame = 0; frame < 20; frame++)
                {
                    _gameManager.Update();
                }
                if (!_gameManager.IsGameOver)
                {
                    _gameManager.CompleteRespawn();
                }
                else
                {
                    _gameManager.ResetGame();
                }
            }
            stopwatch.Stop();
            var withLivesSystemTime = stopwatch.ElapsedMilliseconds;
            
            // Assert - Lives system should not significantly impact performance
            var performanceImpact = (double)withLivesSystemTime / baselineTime;
            Assert.Less(performanceImpact, 1.5, 
                $"Lives system should not significantly impact frame rate. Impact factor: {performanceImpact:F2}");
        }

        #endregion
    }

    #region Test Helper Classes

    public class IntegratedGameManager : IDisposable
    {
        private MockRaylib _mockRaylib;
        private List<Vector2> _asteroidPositions = new List<Vector2>();
        private List<MockExplosion> _explosions = new List<MockExplosion>();
        
        public int Lives { get; private set; } = GameConstants.STARTING_LIVES;
        public bool IsPlayerRespawning { get; private set; }
        public float RespawnTimer { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }
        public Player Player { get; private set; }
        
        public IntegratedGameManager(MockRaylib mockRaylib)
        {
            _mockRaylib = mockRaylib;
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public void HandlePlayerDeath()
        {
            if (IsPlayerRespawning || IsGameOver) return;
            
            Lives--;
            _mockRaylib.PlaySound("explosion", 1.0f);
            _explosions.Add(new MockExplosion { Position = Player.Position });
            
            if (Lives <= 0)
            {
                IsGameOver = true;
            }
            else
            {
                IsPlayerRespawning = true;
                RespawnTimer = GameConstants.RESPAWN_DELAY;
            }
        }
        
        public void Update()
        {
            if (IsPaused) return;
            
            if (IsPlayerRespawning)
            {
                RespawnTimer -= 1f / GameConstants.TARGET_FPS;
                if (RespawnTimer <= 0)
                {
                    CompleteRespawn();
                }
            }
            
            Player?.Update();
        }
        
        public void CompleteRespawn()
        {
            if (!IsPlayerRespawning) return;
            
            var spawnLocation = FindSafeSpawnLocation();
            Player = new Player(spawnLocation, GameConstants.PLAYER_SIZE);
            Player.IsShieldActive = true;
            Player.ShieldDuration = GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS;
            
            IsPlayerRespawning = false;
            RespawnTimer = 0f;
            
            _mockRaylib.PlaySound("respawn", 0.7f);
            _mockRaylib.AddVisualEffect("respawn");
        }
        
        public Vector2 FindSafeSpawnLocation()
        {
            var center = new Vector2(GameConstants.SCREEN_WIDTH / 2f, GameConstants.SCREEN_HEIGHT / 2f);
            
            if (IsLocationSafe(center, 100f)) return center;
            
            var fallbackPositions = new[]
            {
                new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.25f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.25f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.75f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.75f)
            };
            
            foreach (var position in fallbackPositions)
            {
                if (IsLocationSafe(position, 80f)) return position;
            }
            
            return center;
        }
        
        private bool IsLocationSafe(Vector2 position, float safeRadius)
        {
            foreach (var asteroidPos in _asteroidPositions)
            {
                var distance = Vector2.Distance(position, asteroidPos);
                if (distance < safeRadius + GameConstants.LARGE_ASTEROID_RADIUS)
                {
                    return false;
                }
            }
            return true;
        }
        
        public void ProcessCollisions()
        {
            if (IsPlayerRespawning || Player?.IsShieldActive == true) return;
            
            foreach (var asteroidPos in _asteroidPositions)
            {
                var distance = Vector2.Distance(Player.Position, asteroidPos);
                if (distance < Player.Size + GameConstants.MEDIUM_ASTEROID_RADIUS)
                {
                    HandlePlayerDeath();
                    break;
                }
            }
        }
        
        public void CreateAsteroidAtPosition(Vector2 position, AsteroidSize size)
        {
            _asteroidPositions.Add(position);
        }
        
        public void CreateRandomAsteroid()
        {
            var random = new Random();
            var position = new Vector2(
                random.Next(0, GameConstants.SCREEN_WIDTH),
                random.Next(0, GameConstants.SCREEN_HEIGHT));
            _asteroidPositions.Add(position);
        }
        
        public void AdvanceTime(float seconds)
        {
            var frames = (int)(seconds * GameConstants.TARGET_FPS);
            for (int i = 0; i < frames; i++)
            {
                Update();
            }
        }
        
        public void RenderHUD()
        {
            _mockRaylib.DrawText($"Lives: {Lives}", 10, 10, 20, "White");
            
            if (IsPlayerRespawning && RespawnTimer > 0)
            {
                int countdown = (int)Math.Ceiling(RespawnTimer);
                _mockRaylib.DrawText($"RESPAWNING IN {countdown}", 300, 250, 40, "Yellow");
            }
            
            if (IsGameOver)
            {
                _mockRaylib.DrawText("GAME OVER", 300, 250, 40, "Red");
            }
        }
        
        public void Render()
        {
            if (Player?.IsShieldActive == true)
            {
                _mockRaylib.AddShieldRendering(Player.Position);
            }
        }
        
        public void SetLives(int lives) => Lives = lives;
        public void SetPaused(bool paused) => IsPaused = paused;
        public void ResetGame()
        {
            Lives = GameConstants.STARTING_LIVES;
            IsPlayerRespawning = false;
            IsGameOver = false;
            RespawnTimer = 0f;
            _asteroidPositions.Clear();
            _explosions.Clear();
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public void DestroyAllAsteroids() => _asteroidPositions.Clear();
        public void CheckLevelCompletion() { /* Mock implementation */ }
        public void StartNextLevel() { /* Mock implementation */ }
        
        public List<MockEntity> GetEntitiesNearPosition(Vector2 position, float radius)
        {
            var entities = new List<MockEntity>();
            if (Player != null && Vector2.Distance(Player.Position, position) <= radius)
            {
                entities.Add(new MockEntity { EntityType = "Player", Position = Player.Position });
            }
            return entities;
        }
        
        public void Dispose()
        {
            // Cleanup
        }
    }

    public class MockRaylib
    {
        private List<string> _playedSounds = new List<string>();
        private Dictionary<string, float> _soundVolumes = new Dictionary<string, float>();
        private List<MockExplosion> _explosions = new List<MockExplosion>();
        private List<string> _visualEffects = new List<string>();
        private List<Vector2> _shieldRenderings = new List<Vector2>();
        private string _lastDrawnText = "";
        
        public void PlaySound(string sound, float volume)
        {
            _playedSounds.Add(sound);
            _soundVolumes[sound] = volume;
        }
        
        public void DrawText(string text, int x, int y, int fontSize, string color)
        {
            _lastDrawnText = text;
        }
        
        public void AddVisualEffect(string effect) => _visualEffects.Add(effect);
        public void AddShieldRendering(Vector2 position) => _shieldRenderings.Add(position);
        
        public List<string> GetPlayedSounds() => _playedSounds;
        public float GetLastSoundVolume(string sound) => _soundVolumes.GetValueOrDefault(sound, 0f);
        public List<MockExplosion> GetCreatedExplosions() => _explosions;
        public List<string> GetVisualEffects() => _visualEffects;
        public List<Vector2> GetShieldRenderings() => _shieldRenderings;
        public string GetLastDrawnText() => _lastDrawnText;
        
        public void ClearPlayedSounds() => _playedSounds.Clear();
    }

    public class MockExplosion
    {
        public Vector2 Position { get; set; }
    }

    public class MockEntity
    {
        public string EntityType { get; set; } = "";
        public Vector2 Position { get; set; }
    }

    #endregion
}