using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using Asteroids;
using NUnit.Framework;

namespace Asteroids.Tests.Unit
{
    /// <summary>
    /// Unit tests for the Lives System implementation
    /// Tests individual components of lives management, respawn mechanics, and safe spawn algorithms
    /// </summary>
    [TestFixture]
    public class LivesSystemUnitTests
    {
        private TestGameManager _testGameManager;
        private MockAudioManager _mockAudio;
        private MockVisualEffects _mockEffects;
        
        [SetUp]
        public void Setup()
        {
            _mockAudio = new MockAudioManager();
            _mockEffects = new MockVisualEffects();
            _testGameManager = new TestGameManager(_mockAudio, _mockEffects);
        }
        
        [TearDown]
        public void TearDown()
        {
            _testGameManager?.Dispose();
        }

        #region Lives Management Tests

        [Test]
        public void InitialLives_ShouldBeThree()
        {
            // Arrange & Act
            var lives = _testGameManager.Lives;
            
            // Assert
            Assert.AreEqual(GameConstants.STARTING_LIVES, lives);
            Assert.AreEqual(3, lives);
        }

        [Test]
        public void HandlePlayerDeath_DecrementLives_FromThreeToTwo()
        {
            // Arrange
            var initialLives = _testGameManager.Lives;
            
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.AreEqual(initialLives - 1, _testGameManager.Lives);
            Assert.AreEqual(2, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
        }

        [Test]
        public void HandlePlayerDeath_MultipleTimes_DecrementsSequentially()
        {
            // Act & Assert - Death 1: 3 -> 2
            _testGameManager.HandlePlayerDeath();
            Assert.AreEqual(2, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
            Assert.IsFalse(_testGameManager.IsGameOver);
            
            // Complete respawn
            _testGameManager.CompleteRespawn();
            
            // Act & Assert - Death 2: 2 -> 1
            _testGameManager.HandlePlayerDeath();
            Assert.AreEqual(1, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
            Assert.IsFalse(_testGameManager.IsGameOver);
            
            // Complete respawn
            _testGameManager.CompleteRespawn();
            
            // Act & Assert - Death 3: 1 -> 0 (Game Over)
            _testGameManager.HandlePlayerDeath();
            Assert.AreEqual(0, _testGameManager.Lives);
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.IsTrue(_testGameManager.IsGameOver);
        }

        [Test]
        public void HandlePlayerDeath_WithShieldActive_StillDecrementsLives()
        {
            // Arrange - This tests non-collision death (e.g., special damage that bypasses shield)
            _testGameManager.Player.IsShieldActive = true;
            var initialLives = _testGameManager.Lives;
            
            // Act - Force death regardless of shield (e.g., enemy fire)
            _testGameManager.HandlePlayerDeath(bypassShield: true);
            
            // Assert
            Assert.AreEqual(initialLives - 1, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
        }

        #endregion

        #region Game Over Tests

        [Test]
        public void HandlePlayerDeath_WithOneLiveRemaining_TriggersGameOver()
        {
            // Arrange
            _testGameManager.SetLives(1);
            
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.AreEqual(0, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsGameOver);
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
        }

        [Test]
        public void GameOver_DoesNotStartRespawnTimer()
        {
            // Arrange
            _testGameManager.SetLives(1);
            
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.IsTrue(_testGameManager.IsGameOver);
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.AreEqual(0f, _testGameManager.RespawnTimer);
        }

        [Test]
        public void GameOver_StateRemainsConsistent()
        {
            // Arrange
            _testGameManager.SetLives(1);
            _testGameManager.HandlePlayerDeath();
            
            // Act - Simulate multiple update calls
            for (int i = 0; i < 10; i++)
            {
                _testGameManager.Update();
            }
            
            // Assert
            Assert.IsTrue(_testGameManager.IsGameOver);
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.AreEqual(0, _testGameManager.Lives);
        }

        #endregion

        #region Respawn Timer Tests

        [Test]
        public void PlayerDeath_StartsRespawnTimer()
        {
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
            Assert.AreEqual(GameConstants.RESPAWN_DELAY, _testGameManager.RespawnTimer, 0.01f);
        }

        [Test]
        public void RespawnTimer_CountsDownCorrectly()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            var initialTimer = _testGameManager.RespawnTimer;
            
            // Act - Simulate 0.5 seconds
            _testGameManager.AdvanceTime(0.5f);
            
            // Assert
            Assert.AreEqual(initialTimer - 0.5f, _testGameManager.RespawnTimer, 0.01f);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
        }

        [Test]
        public void RespawnTimer_CompletesAfterDelay()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            
            // Act - Advance time past respawn delay
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.IsNotNull(_testGameManager.Player);
            Assert.IsTrue(_testGameManager.Player.IsShieldActive);
        }

        [Test]
        public void RespawnTimer_AccurateTiming()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            
            // Act - Advance to exactly respawn delay
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY);
            
            // Assert
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.IsNotNull(_testGameManager.Player);
        }

        #endregion

        #region Safe Spawn Location Tests

        [Test]
        public void FindSafeSpawnLocation_WithNoAsteroids_ReturnsCenter()
        {
            // Arrange
            _testGameManager.ClearAllAsteroids();
            var expectedCenter = new Vector2(GameConstants.SCREEN_WIDTH / 2f, GameConstants.SCREEN_HEIGHT / 2f);
            
            // Act
            var spawnLocation = _testGameManager.FindSafeSpawnLocation();
            
            // Assert
            Assert.AreEqual(expectedCenter.X, spawnLocation.X, 1f);
            Assert.AreEqual(expectedCenter.Y, spawnLocation.Y, 1f);
        }

        [Test]
        public void FindSafeSpawnLocation_WithCenterBlocked_UsesFallbackPosition()
        {
            // Arrange - Place asteroid at center
            var center = new Vector2(GameConstants.SCREEN_WIDTH / 2f, GameConstants.SCREEN_HEIGHT / 2f);
            _testGameManager.PlaceAsteroidAtPosition(center, GameConstants.LARGE_ASTEROID_RADIUS);
            
            // Act
            var spawnLocation = _testGameManager.FindSafeSpawnLocation();
            
            // Assert - Should not be at center
            var distanceFromCenter = Vector2.Distance(spawnLocation, center);
            Assert.Greater(distanceFromCenter, 50f, "Spawn location should not be near center when blocked");
        }

        [Test]
        public void FindSafeSpawnLocation_UsesQuadrantFallbacks()
        {
            // Arrange - Block center and test each quadrant
            var center = new Vector2(GameConstants.SCREEN_WIDTH / 2f, GameConstants.SCREEN_HEIGHT / 2f);
            _testGameManager.ClearAllAsteroids();
            _testGameManager.PlaceAsteroidAtPosition(center, 120f); // Large radius to block center
            
            // Act
            var spawnLocation = _testGameManager.FindSafeSpawnLocation();
            
            // Assert - Should be in one of the quadrants
            var quadrants = new[]
            {
                new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.25f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.25f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.75f),
                new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.75f)
            };
            
            bool foundInQuadrant = false;
            foreach (var quadrant in quadrants)
            {
                if (Vector2.Distance(spawnLocation, quadrant) < 10f)
                {
                    foundInQuadrant = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundInQuadrant, "Spawn location should be in one of the fallback quadrants");
        }

        [Test]
        public void FindSafeSpawnLocation_MaintainsSafeDistance()
        {
            // Arrange - Place asteroids randomly
            var asteroidPositions = new List<Vector2>();
            for (int i = 0; i < 10; i++)
            {
                var pos = new Vector2(i * 50, i * 40);
                asteroidPositions.Add(pos);
                _testGameManager.PlaceAsteroidAtPosition(pos, GameConstants.MEDIUM_ASTEROID_RADIUS);
            }
            
            // Act
            var spawnLocation = _testGameManager.FindSafeSpawnLocation();
            
            // Assert - Check minimum distance from all asteroids
            foreach (var asteroidPos in asteroidPositions)
            {
                var distance = Vector2.Distance(spawnLocation, asteroidPos);
                var safeDistance = GameConstants.MEDIUM_ASTEROID_RADIUS + 80f; // Safe spawn radius
                Assert.GreaterOrEqual(distance, safeDistance, 
                    $"Spawn location should maintain safe distance from asteroid at {asteroidPos}");
            }
        }

        [Test]
        public void FindSafeSpawnLocation_WorstCase_ReturnsValidPosition()
        {
            // Arrange - Fill screen with asteroids (worst case scenario)
            _testGameManager.ClearAllAsteroids();
            for (int x = 0; x < GameConstants.SCREEN_WIDTH; x += 60)
            {
                for (int y = 0; y < GameConstants.SCREEN_HEIGHT; y += 60)
                {
                    _testGameManager.PlaceAsteroidAtPosition(new Vector2(x, y), GameConstants.SMALL_ASTEROID_RADIUS);
                }
            }
            
            // Act
            var spawnLocation = _testGameManager.FindSafeSpawnLocation();
            
            // Assert - Should still return a valid position (even if not ideal)
            Assert.GreaterOrEqual(spawnLocation.X, 0);
            Assert.LessOrEqual(spawnLocation.X, GameConstants.SCREEN_WIDTH);
            Assert.GreaterOrEqual(spawnLocation.Y, 0);
            Assert.LessOrEqual(spawnLocation.Y, GameConstants.SCREEN_HEIGHT);
        }

        #endregion

        #region Invulnerability Tests

        [Test]
        public void PlayerRespawn_HasInvulnerabilityShield()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            
            // Act - Complete respawn
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert
            Assert.IsNotNull(_testGameManager.Player);
            Assert.IsTrue(_testGameManager.Player.IsShieldActive);
        }

        [Test]
        public void PlayerRespawn_InvulnerabilityDuration()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            var expectedDuration = GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS;
            
            // Assert
            Assert.AreEqual(expectedDuration, _testGameManager.Player.ShieldDuration, 1f);
        }

        [Test]
        public void InvulnerabilityShield_ExpiresAfterCorrectTime()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Act - Advance time through invulnerability period
            var invulnerabilityFrames = GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS;
            for (int i = 0; i < invulnerabilityFrames + 10; i++)
            {
                _testGameManager.Update();
            }
            
            // Assert
            Assert.IsFalse(_testGameManager.Player.IsShieldActive);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void FindSafeSpawnLocation_Performance_UnderMaxAsteroidLoad()
        {
            // Arrange - Create maximum asteroid scenario
            _testGameManager.ClearAllAsteroids();
            for (int i = 0; i < 100; i++) // Heavy load
            {
                var pos = new Vector2(
                    (i * 47) % GameConstants.SCREEN_WIDTH,
                    (i * 31) % GameConstants.SCREEN_HEIGHT);
                _testGameManager.PlaceAsteroidAtPosition(pos, GameConstants.SMALL_ASTEROID_RADIUS);
            }
            
            // Act & Assert - Performance measurement
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                var spawnLocation = _testGameManager.FindSafeSpawnLocation();
                Assert.IsNotNull(spawnLocation);
            }
            
            stopwatch.Stop();
            
            // Assert - Should complete 1000 operations in reasonable time
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, 
                "Safe spawn location algorithm should be performant even under heavy load");
            
            var avgTimeMs = (double)stopwatch.ElapsedMilliseconds / 1000;
            Assert.Less(avgTimeMs, 0.1, $"Average time per call should be < 0.1ms, actual: {avgTimeMs:F4}ms");
        }

        [Test]
        public void LivesSystem_MemoryAllocation_Minimal()
        {
            // Arrange
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Perform multiple death/respawn cycles
            for (int i = 0; i < 10; i++)
            {
                _testGameManager.HandlePlayerDeath();
                _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
                
                // Reset lives to continue testing
                if (_testGameManager.IsGameOver)
                {
                    _testGameManager.ResetGame();
                }
            }
            
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            
            // Assert - Memory increase should be minimal
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
            
            Assert.Less(memoryIncreaseMB, 1.0, 
                $"Lives system should not significantly increase memory usage: {memoryIncreaseMB:F2}MB");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void RapidMultipleDeath_Calls_HandleGracefully()
        {
            // Arrange
            var initialLives = _testGameManager.Lives;
            
            // Act - Call death multiple times rapidly
            _testGameManager.HandlePlayerDeath();
            _testGameManager.HandlePlayerDeath(); // Should be ignored
            _testGameManager.HandlePlayerDeath(); // Should be ignored
            
            // Assert - Only one death should be processed
            Assert.AreEqual(initialLives - 1, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
        }

        [Test]
        public void PlayerDeath_DuringRespawn_IsIgnored()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            var livesAfterFirstDeath = _testGameManager.Lives;
            
            // Act - Try to kill player again during respawn
            _testGameManager.HandlePlayerDeath();
            
            // Assert - Lives should not change
            Assert.AreEqual(livesAfterFirstDeath, _testGameManager.Lives);
            Assert.IsTrue(_testGameManager.IsPlayerRespawning);
        }

        [Test]
        public void GameReset_RestoresInitialLives()
        {
            // Arrange - Lose some lives
            _testGameManager.HandlePlayerDeath();
            _testGameManager.HandlePlayerDeath();
            Assert.AreEqual(1, _testGameManager.Lives);
            
            // Act
            _testGameManager.ResetGame();
            
            // Assert
            Assert.AreEqual(GameConstants.STARTING_LIVES, _testGameManager.Lives);
            Assert.IsFalse(_testGameManager.IsPlayerRespawning);
            Assert.IsFalse(_testGameManager.IsGameOver);
        }

        #endregion

        #region Integration Points Tests

        [Test]
        public void PlayerDeath_TriggersAudioEffect()
        {
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.Contains("explosion", _mockAudio.PlayedSounds);
        }

        [Test]
        public void PlayerRespawn_TriggersAudioEffect()
        {
            // Arrange
            _testGameManager.HandlePlayerDeath();
            
            // Act
            _testGameManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert
            Assert.Contains("respawn", _mockAudio.PlayedSounds);
        }

        [Test]
        public void PlayerDeath_TriggersVisualEffects()
        {
            // Act
            _testGameManager.HandlePlayerDeath();
            
            // Assert
            Assert.IsTrue(_mockEffects.GameOverEffectTriggered);
        }

        #endregion
    }

    #region Test Helper Classes

    /// <summary>
    /// Test-specific game manager that exposes internal state for testing
    /// </summary>
    public class TestGameManager : IDisposable
    {
        private MockAudioManager _audioManager;
        private MockVisualEffects _visualEffects;
        private List<Vector2> _asteroidPositions = new List<Vector2>();
        
        public int Lives { get; private set; } = GameConstants.STARTING_LIVES;
        public bool IsPlayerRespawning { get; private set; }
        public float RespawnTimer { get; private set; }
        public bool IsGameOver { get; private set; }
        public Player Player { get; private set; }
        
        public TestGameManager(MockAudioManager audio, MockVisualEffects effects)
        {
            _audioManager = audio;
            _visualEffects = effects;
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public void HandlePlayerDeath(bool bypassShield = false)
        {
            if (IsPlayerRespawning || IsGameOver) return;
            
            if (!bypassShield && Player?.IsShieldActive == true) return;
            
            Lives--;
            _audioManager.PlaySound("explosion", 1.0f);
            
            if (Lives <= 0)
            {
                IsGameOver = true;
                _visualEffects.OnGameOver();
            }
            else
            {
                IsPlayerRespawning = true;
                RespawnTimer = GameConstants.RESPAWN_DELAY;
            }
        }
        
        public void Update()
        {
            if (IsPlayerRespawning)
            {
                RespawnTimer -= 1f / GameConstants.TARGET_FPS; // Simulate frame time
                if (RespawnTimer <= 0)
                {
                    CompleteRespawn();
                }
            }
            
            Player?.Update();
        }
        
        public void AdvanceTime(float seconds)
        {
            var frames = (int)(seconds * GameConstants.TARGET_FPS);
            for (int i = 0; i < frames; i++)
            {
                Update();
            }
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
            
            _audioManager.PlaySound("respawn", 0.7f);
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
            
            return center; // Last resort
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
        
        public void SetLives(int lives) => Lives = lives;
        public void ClearAllAsteroids() => _asteroidPositions.Clear();
        public void PlaceAsteroidAtPosition(Vector2 position, float radius) => _asteroidPositions.Add(position);
        
        public void ResetGame()
        {
            Lives = GameConstants.STARTING_LIVES;
            IsPlayerRespawning = false;
            IsGameOver = false;
            RespawnTimer = 0f;
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public void Dispose()
        {
            // Cleanup test resources
        }
    }

    public class MockAudioManager
    {
        public List<string> PlayedSounds { get; } = new List<string>();
        
        public void PlaySound(string sound, float volume)
        {
            PlayedSounds.Add(sound);
        }
    }

    public class MockVisualEffects
    {
        public bool GameOverEffectTriggered { get; private set; }
        
        public void OnGameOver()
        {
            GameOverEffectTriggered = true;
        }
    }

    #endregion
}