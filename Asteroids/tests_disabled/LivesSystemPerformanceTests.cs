using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Linq;
using Asteroids;
using NUnit.Framework;

namespace Asteroids.Tests.Performance
{
    /// <summary>
    /// Performance and stress tests for the Lives System
    /// Tests performance impact, memory usage, and system stability under extreme conditions
    /// </summary>
    [TestFixture]
    public class LivesSystemPerformanceTests
    {
        private PerformanceTestManager _testManager;
        private PerformanceMetrics _metrics;
        
        [SetUp]
        public void Setup()
        {
            _testManager = new PerformanceTestManager();
            _metrics = new PerformanceMetrics();
        }
        
        [TearDown]
        public void TearDown()
        {
            _testManager?.Dispose();
        }

        #region Safe Spawn Location Performance Tests

        [Test]
        public void SafeSpawnLocation_Performance_MinimalAsteroids()
        {
            // Arrange - Minimal asteroid scenario (best case)
            _testManager.CreateAsteroids(5);
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                var spawnPos = _testManager.FindSafeSpawnLocation();
                Assert.IsNotNull(spawnPos);
            }
            stopwatch.Stop();
            
            // Assert
            var avgTimeMs = (double)stopwatch.ElapsedMilliseconds / 10000;
            Assert.Less(avgTimeMs, 0.01, $"Safe spawn with minimal asteroids should be < 0.01ms per call, actual: {avgTimeMs:F4}ms");
            
            _metrics.RecordSafeSpawnPerformance("MinimalAsteroids", 10000, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Minimal asteroids: 10,000 calls in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimeMs:F4}ms)");
        }

        [Test]
        public void SafeSpawnLocation_Performance_ModerateAsteroids()
        {
            // Arrange - Moderate asteroid scenario (typical gameplay)
            _testManager.CreateAsteroids(50);
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                var spawnPos = _testManager.FindSafeSpawnLocation();
                Assert.IsNotNull(spawnPos);
            }
            stopwatch.Stop();
            
            // Assert
            var avgTimeMs = (double)stopwatch.ElapsedMilliseconds / 5000;
            Assert.Less(avgTimeMs, 0.05, $"Safe spawn with moderate asteroids should be < 0.05ms per call, actual: {avgTimeMs:F4}ms");
            
            _metrics.RecordSafeSpawnPerformance("ModerateAsteroids", 5000, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Moderate asteroids: 5,000 calls in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimeMs:F4}ms)");
        }

        [Test]
        public void SafeSpawnLocation_Performance_MaximumAsteroids()
        {
            // Arrange - Maximum asteroid scenario (worst case)
            _testManager.CreateAsteroids(GameConstants.MAX_ASTEROIDS);
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                var spawnPos = _testManager.FindSafeSpawnLocation();
                Assert.IsNotNull(spawnPos);
            }
            stopwatch.Stop();
            
            // Assert
            var avgTimeMs = (double)stopwatch.ElapsedMilliseconds / 1000;
            Assert.Less(avgTimeMs, 0.1, $"Safe spawn with maximum asteroids should be < 0.1ms per call, actual: {avgTimeMs:F4}ms");
            
            _metrics.RecordSafeSpawnPerformance("MaximumAsteroids", 1000, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Maximum asteroids: 1,000 calls in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimeMs:F4}ms)");
        }

        [Test]
        public void SafeSpawnLocation_Performance_WorstCaseScenario()
        {
            // Arrange - Pathological case: screen completely filled
            _testManager.FillScreenWithAsteroids();
            
            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var spawnPos = _testManager.FindSafeSpawnLocation();
                Assert.IsNotNull(spawnPos);
            }
            stopwatch.Stop();
            
            // Assert - Even in worst case, should complete reasonably quickly
            var avgTimeMs = (double)stopwatch.ElapsedMilliseconds / 100;
            Assert.Less(avgTimeMs, 1.0, $"Safe spawn in worst case should be < 1ms per call, actual: {avgTimeMs:F4}ms");
            
            _metrics.RecordSafeSpawnPerformance("WorstCase", 100, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Worst case scenario: 100 calls in {stopwatch.ElapsedMilliseconds}ms (avg: {avgTimeMs:F4}ms)");
        }

        #endregion

        #region Lives System Update Performance Tests

        [Test]
        public void LivesSystem_UpdatePerformance_NormalGameplay()
        {
            // Arrange - Typical game state
            _testManager.CreateAsteroids(25);
            _testManager.CreateBullets(10);
            
            // Act & Measure - Simulate normal gameplay updates
            var stopwatch = Stopwatch.StartNew();
            for (int frame = 0; frame < 3600; frame++) // 60 seconds at 60 FPS
            {
                _testManager.Update();
            }
            stopwatch.Stop();
            
            // Assert - Should maintain 60 FPS performance
            var avgFrameTime = (double)stopwatch.ElapsedMilliseconds / 3600;
            Assert.Less(avgFrameTime, 16.67, $"Average frame time should be < 16.67ms (60 FPS), actual: {avgFrameTime:F2}ms");
            
            _metrics.RecordUpdatePerformance("NormalGameplay", 3600, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Normal gameplay: 3,600 frames in {stopwatch.ElapsedMilliseconds}ms (avg: {avgFrameTime:F2}ms)");
        }

        [Test]
        public void LivesSystem_UpdatePerformance_RespawnCycle()
        {
            // Arrange
            _testManager.CreateAsteroids(30);
            
            // Act & Measure - Multiple death/respawn cycles
            var stopwatch = Stopwatch.StartNew();
            
            for (int cycle = 0; cycle < 10; cycle++)
            {
                // Death phase
                _testManager.HandlePlayerDeath();
                
                // Respawn waiting phase (2 seconds = 120 frames)
                for (int frame = 0; frame < 120; frame++)
                {
                    _testManager.Update();
                }
                
                // Post-respawn invulnerability phase (3 seconds = 180 frames)
                for (int frame = 0; frame < 180; frame++)
                {
                    _testManager.Update();
                }
                
                // Reset for next cycle
                if (_testManager.IsGameOver)
                {
                    _testManager.ResetGame();
                }
            }
            
            stopwatch.Stop();
            
            // Assert
            var totalFrames = 10 * 300; // 10 cycles * 300 frames per cycle
            var avgFrameTime = (double)stopwatch.ElapsedMilliseconds / totalFrames;
            Assert.Less(avgFrameTime, 16.67, $"Respawn cycles should maintain 60 FPS, avg frame time: {avgFrameTime:F2}ms");
            
            _metrics.RecordUpdatePerformance("RespawnCycles", totalFrames, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Respawn cycles: {totalFrames} frames in {stopwatch.ElapsedMilliseconds}ms (avg: {avgFrameTime:F2}ms)");
        }

        [Test]
        public void LivesSystem_UpdatePerformance_HighStressGameplay()
        {
            // Arrange - High stress scenario
            _testManager.CreateAsteroids(200);
            _testManager.CreateBullets(50);
            
            // Act & Measure - High intensity updates
            var stopwatch = Stopwatch.StartNew();
            for (int frame = 0; frame < 1800; frame++) // 30 seconds at 60 FPS
            {
                _testManager.Update();
                
                // Simulate player deaths occasionally
                if (frame % 300 == 0 && !_testManager.IsPlayerRespawning)
                {
                    _testManager.HandlePlayerDeath();
                }
            }
            stopwatch.Stop();
            
            // Assert - Should handle high stress reasonably
            var avgFrameTime = (double)stopwatch.ElapsedMilliseconds / 1800;
            Assert.Less(avgFrameTime, 25.0, $"High stress should be manageable, avg frame time: {avgFrameTime:F2}ms");
            
            _metrics.RecordUpdatePerformance("HighStress", 1800, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ High stress: 1,800 frames in {stopwatch.ElapsedMilliseconds}ms (avg: {avgFrameTime:F2}ms)");
        }

        #endregion

        #region Memory Performance Tests

        [Test]
        public void LivesSystem_MemoryUsage_SingleRespawnCycle()
        {
            // Arrange
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Single death/respawn cycle
            _testManager.HandlePlayerDeath();
            _testManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            
            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
            
            Assert.Less(memoryIncreaseMB, 0.1, $"Single respawn should use minimal memory: {memoryIncreaseMB:F3}MB");
            
            _metrics.RecordMemoryUsage("SingleRespawn", memoryIncrease);
            Console.WriteLine($"✓ Single respawn memory usage: {memoryIncreaseMB:F3}MB");
        }

        [Test]
        public void LivesSystem_MemoryUsage_MultipleRespawnCycles()
        {
            // Arrange
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Multiple death/respawn cycles
            for (int cycle = 0; cycle < 20; cycle++)
            {
                _testManager.HandlePlayerDeath();
                _testManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
                
                if (_testManager.IsGameOver)
                {
                    _testManager.ResetGame();
                }
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(true);
            
            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
            
            Assert.Less(memoryIncreaseMB, 1.0, $"Multiple respawns should not leak memory: {memoryIncreaseMB:F3}MB");
            
            _metrics.RecordMemoryUsage("MultipleRespawns", memoryIncrease);
            Console.WriteLine($"✓ Multiple respawns memory usage: {memoryIncreaseMB:F3}MB");
        }

        [Test]
        public void LivesSystem_MemoryUsage_SafeSpawnLocationStress()
        {
            // Arrange
            _testManager.CreateAsteroids(100);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Many safe spawn location calls
            for (int i = 0; i < 10000; i++)
            {
                var spawnPos = _testManager.FindSafeSpawnLocation();
            }
            
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            
            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseMB = memoryIncrease / 1024.0 / 1024.0;
            
            Assert.Less(memoryIncreaseMB, 0.5, $"Safe spawn calls should not allocate much: {memoryIncreaseMB:F3}MB");
            
            _metrics.RecordMemoryUsage("SafeSpawnStress", memoryIncrease);
            Console.WriteLine($"✓ Safe spawn stress memory usage: {memoryIncreaseMB:F3}MB");
        }

        [Test]
        public void LivesSystem_GarbageCollection_Impact()
        {
            // Arrange - Create objects that will need GC
            var objects = new List<object>();
            for (int i = 0; i < 10000; i++)
            {
                objects.Add(new { Position = new Vector2(i, i), Value = i });
            }
            
            // Act & Measure - GC impact during lives system operation
            var gcWatch = Stopwatch.StartNew();
            
            // Force GC
            objects.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            gcWatch.Stop();
            
            // Test lives system continues to work normally after GC
            _testManager.HandlePlayerDeath();
            _testManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.1f);
            
            // Assert
            Assert.Less(gcWatch.ElapsedMilliseconds, 100, $"GC should not severely impact performance: {gcWatch.ElapsedMilliseconds}ms");
            Assert.IsFalse(_testManager.IsPlayerRespawning);
            Assert.IsNotNull(_testManager.Player);
            
            _metrics.RecordGCImpact(gcWatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ GC impact: {gcWatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Stress Tests

        [Test]
        public void LivesSystem_StressTest_RapidDeathRespawn()
        {
            // Arrange - Test rapid death/respawn cycles
            var stopwatch = Stopwatch.StartNew();
            int successfulCycles = 0;
            
            // Act - Try to overwhelm the system
            for (int cycle = 0; cycle < 100; cycle++)
            {
                try
                {
                    _testManager.HandlePlayerDeath();
                    
                    // Rapidly advance to respawn
                    _testManager.AdvanceTime(GameConstants.RESPAWN_DELAY + 0.01f);
                    
                    if (_testManager.IsGameOver)
                    {
                        _testManager.ResetGame();
                    }
                    
                    successfulCycles++;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Lives system failed under stress at cycle {cycle}: {ex.Message}");
                }
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.AreEqual(100, successfulCycles);
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, 
                $"100 rapid cycles should complete quickly: {stopwatch.ElapsedMilliseconds}ms");
            
            _metrics.RecordStressTest("RapidDeathRespawn", successfulCycles, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Rapid death/respawn stress: {successfulCycles} cycles in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void LivesSystem_StressTest_MassiveAsteroidField()
        {
            // Arrange - Create pathological asteroid scenario
            _testManager.FillScreenWithAsteroids();
            var asteroidCount = _testManager.GetAsteroidCount();
            
            var stopwatch = Stopwatch.StartNew();
            int successfulSpawns = 0;
            
            // Act - Try to find safe spawns in crowded field
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var spawnPos = _testManager.FindSafeSpawnLocation();
                    Assert.IsNotNull(spawnPos);
                    successfulSpawns++;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Safe spawn failed with massive asteroid field: {ex.Message}");
                }
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.AreEqual(100, successfulSpawns);
            var avgTime = (double)stopwatch.ElapsedMilliseconds / 100;
            Assert.Less(avgTime, 5.0, $"Should handle massive asteroid fields: avg {avgTime:F2}ms per call");
            
            _metrics.RecordStressTest("MassiveAsteroidField", successfulSpawns, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Massive asteroid field stress: {asteroidCount} asteroids, {successfulSpawns} spawns in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void LivesSystem_StressTest_ConcurrentOperations()
        {
            // Simulate multiple operations happening simultaneously
            var stopwatch = Stopwatch.StartNew();
            int operationsCompleted = 0;
            
            // Act - Mix of operations that might happen simultaneously
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    // Simulate complex game state
                    if (i % 10 == 0) _testManager.HandlePlayerDeath();
                    if (i % 5 == 0) _testManager.FindSafeSpawnLocation();
                    if (i % 3 == 0) _testManager.Update();
                    if (i % 20 == 0) _testManager.CreateRandomAsteroid();
                    
                    if (_testManager.IsGameOver && i % 25 == 0)
                    {
                        _testManager.ResetGame();
                    }
                    
                    operationsCompleted++;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Concurrent operations failed at iteration {i}: {ex.Message}");
                }
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.AreEqual(1000, operationsCompleted);
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, 
                $"Concurrent operations should be efficient: {stopwatch.ElapsedMilliseconds}ms");
            
            _metrics.RecordStressTest("ConcurrentOperations", operationsCompleted, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Concurrent operations stress: {operationsCompleted} operations in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void LivesSystem_StressTest_LongRunningSession()
        {
            // Simulate a very long gaming session
            var stopwatch = Stopwatch.StartNew();
            int framesProcessed = 0;
            int deathsProcessed = 0;
            
            // Act - Simulate 10 minutes of gameplay at 60 FPS
            for (int frame = 0; frame < 36000; frame++) // 10 minutes * 60 seconds * 60 FPS
            {
                try
                {
                    _testManager.Update();
                    framesProcessed++;
                    
                    // Occasional deaths
                    if (frame % 1800 == 0 && !_testManager.IsPlayerRespawning) // Every 30 seconds
                    {
                        _testManager.HandlePlayerDeath();
                        deathsProcessed++;
                        
                        if (_testManager.IsGameOver)
                        {
                            _testManager.ResetGame();
                        }
                    }
                    
                    // Occasional new asteroids
                    if (frame % 600 == 0) // Every 10 seconds
                    {
                        _testManager.CreateRandomAsteroid();
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Long running session failed at frame {frame}: {ex.Message}");
                }
            }
            
            stopwatch.Stop();
            
            // Assert - Should maintain reasonable performance throughout
            var avgFrameTime = (double)stopwatch.ElapsedMilliseconds / framesProcessed;
            Assert.Less(avgFrameTime, 20.0, $"Long session should maintain performance: avg {avgFrameTime:F2}ms per frame");
            
            _metrics.RecordStressTest("LongRunningSession", framesProcessed, stopwatch.ElapsedMilliseconds);
            Console.WriteLine($"✓ Long running session: {framesProcessed} frames, {deathsProcessed} deaths in {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Performance Regression Tests

        [Test]
        public void LivesSystem_PerformanceRegression_Baseline()
        {
            // Establish baseline performance without lives system active
            _testManager.CreateAsteroids(50);
            _testManager.DisableLivesSystem();
            
            var stopwatch = Stopwatch.StartNew();
            for (int frame = 0; frame < 3600; frame++)
            {
                _testManager.Update();
            }
            stopwatch.Stop();
            
            var baselineTime = stopwatch.ElapsedMilliseconds;
            
            // Now test with lives system active
            _testManager.EnableLivesSystem();
            _testManager.ResetGame();
            
            stopwatch.Restart();
            for (int frame = 0; frame < 3600; frame++)
            {
                _testManager.Update();
                if (frame % 600 == 0) // Occasional death to activate lives system
                {
                    _testManager.HandlePlayerDeath();
                }
            }
            stopwatch.Stop();
            
            var withLivesSystemTime = stopwatch.ElapsedMilliseconds;
            
            // Assert - Lives system should not add significant overhead
            var performanceImpact = (double)withLivesSystemTime / baselineTime;
            Assert.Less(performanceImpact, 1.2, 
                $"Lives system should add < 20% overhead. Impact: {performanceImpact:F2}x");
            
            _metrics.RecordPerformanceRegression(baselineTime, withLivesSystemTime);
            Console.WriteLine($"✓ Performance regression: Baseline: {baselineTime}ms, With lives: {withLivesSystemTime}ms, Impact: {performanceImpact:F2}x");
        }

        #endregion

        [OneTimeTearDown]
        public void GeneratePerformanceReport()
        {
            Console.WriteLine("\n" + "=".PadRight(60, '='));
            Console.WriteLine("LIVES SYSTEM PERFORMANCE TEST REPORT");
            Console.WriteLine("=".PadRight(60, '='));
            
            _metrics.PrintReport();
            
            // Performance assessment
            Console.WriteLine("\nPERFORMANCE ASSESSMENT:");
            if (_metrics.HasPerformanceConcerns())
            {
                Console.WriteLine("⚠️  Some performance concerns detected. Review metrics above.");
            }
            else
            {
                Console.WriteLine("✅ Lives system performance is acceptable for production use.");
            }
            
            Console.WriteLine("=".PadRight(60, '='));
        }
    }

    #region Performance Test Helper Classes

    public class PerformanceTestManager : IDisposable
    {
        private List<Vector2> _asteroidPositions = new List<Vector2>();
        private List<Vector2> _bulletPositions = new List<Vector2>();
        private bool _livesSystemEnabled = true;
        
        public int Lives { get; private set; } = GameConstants.STARTING_LIVES;
        public bool IsPlayerRespawning { get; private set; }
        public float RespawnTimer { get; private set; }
        public bool IsGameOver { get; private set; }
        public Player Player { get; private set; }
        
        public PerformanceTestManager()
        {
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public void CreateAsteroids(int count)
        {
            _asteroidPositions.Clear();
            var random = new Random(42); // Fixed seed for consistent testing
            
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2(
                    random.Next(50, GameConstants.SCREEN_WIDTH - 50),
                    random.Next(50, GameConstants.SCREEN_HEIGHT - 50));
                _asteroidPositions.Add(position);
            }
        }
        
        public void CreateBullets(int count)
        {
            _bulletPositions.Clear();
            var random = new Random(42);
            
            for (int i = 0; i < count; i++)
            {
                var position = new Vector2(
                    random.Next(0, GameConstants.SCREEN_WIDTH),
                    random.Next(0, GameConstants.SCREEN_HEIGHT));
                _bulletPositions.Add(position);
            }
        }
        
        public void CreateRandomAsteroid()
        {
            var random = new Random();
            var position = new Vector2(
                random.Next(0, GameConstants.SCREEN_WIDTH),
                random.Next(0, GameConstants.SCREEN_HEIGHT));
            _asteroidPositions.Add(position);
        }
        
        public void FillScreenWithAsteroids()
        {
            _asteroidPositions.Clear();
            
            // Create dense asteroid field
            for (int x = 30; x < GameConstants.SCREEN_WIDTH - 30; x += 40)
            {
                for (int y = 30; y < GameConstants.SCREEN_HEIGHT - 30; y += 40)
                {
                    _asteroidPositions.Add(new Vector2(x, y));
                }
            }
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
        
        public void HandlePlayerDeath()
        {
            if (!_livesSystemEnabled || IsPlayerRespawning || IsGameOver) return;
            
            Lives--;
            
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
            if (IsPlayerRespawning && _livesSystemEnabled)
            {
                RespawnTimer -= 1f / GameConstants.TARGET_FPS;
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
        }
        
        public void ResetGame()
        {
            Lives = GameConstants.STARTING_LIVES;
            IsPlayerRespawning = false;
            IsGameOver = false;
            RespawnTimer = 0f;
            Player = new Player(new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2), 
                               GameConstants.PLAYER_SIZE);
        }
        
        public int GetAsteroidCount() => _asteroidPositions.Count;
        public void EnableLivesSystem() => _livesSystemEnabled = true;
        public void DisableLivesSystem() => _livesSystemEnabled = false;
        
        public void Dispose()
        {
            // Cleanup
        }
    }

    public class PerformanceMetrics
    {
        private Dictionary<string, PerformanceResult> _safeSpawnResults = new Dictionary<string, PerformanceResult>();
        private Dictionary<string, PerformanceResult> _updateResults = new Dictionary<string, PerformanceResult>();
        private List<long> _memoryUsageResults = new List<long>();
        private List<long> _gcImpactResults = new List<long>();
        private Dictionary<string, PerformanceResult> _stressTestResults = new Dictionary<string, PerformanceResult>();
        private PerformanceRegressionResult? _regressionResult;
        
        public void RecordSafeSpawnPerformance(string scenario, int operations, long milliseconds)
        {
            _safeSpawnResults[scenario] = new PerformanceResult
            {
                Operations = operations,
                Milliseconds = milliseconds,
                OperationsPerSecond = (double)operations / (milliseconds / 1000.0)
            };
        }
        
        public void RecordUpdatePerformance(string scenario, int frames, long milliseconds)
        {
            _updateResults[scenario] = new PerformanceResult
            {
                Operations = frames,
                Milliseconds = milliseconds,
                OperationsPerSecond = (double)frames / (milliseconds / 1000.0)
            };
        }
        
        public void RecordMemoryUsage(string test, long bytes)
        {
            _memoryUsageResults.Add(bytes);
        }
        
        public void RecordGCImpact(long milliseconds)
        {
            _gcImpactResults.Add(milliseconds);
        }
        
        public void RecordStressTest(string test, int operations, long milliseconds)
        {
            _stressTestResults[test] = new PerformanceResult
            {
                Operations = operations,
                Milliseconds = milliseconds,
                OperationsPerSecond = (double)operations / (milliseconds / 1000.0)
            };
        }
        
        public void RecordPerformanceRegression(long baselineMs, long withLivesSystemMs)
        {
            _regressionResult = new PerformanceRegressionResult
            {
                BaselineMs = baselineMs,
                WithLivesSystemMs = withLivesSystemMs,
                ImpactFactor = (double)withLivesSystemMs / baselineMs
            };
        }
        
        public void PrintReport()
        {
            Console.WriteLine("\nSAFE SPAWN LOCATION PERFORMANCE:");
            foreach (var result in _safeSpawnResults)
            {
                var avgMs = (double)result.Value.Milliseconds / result.Value.Operations;
                Console.WriteLine($"  {result.Key}: {result.Value.Operations:N0} ops in {result.Value.Milliseconds}ms (avg: {avgMs:F4}ms)");
            }
            
            Console.WriteLine("\nUPDATE PERFORMANCE:");
            foreach (var result in _updateResults)
            {
                var avgMs = (double)result.Value.Milliseconds / result.Value.Operations;
                var fps = 1000.0 / avgMs;
                Console.WriteLine($"  {result.Key}: {result.Value.Operations:N0} frames in {result.Value.Milliseconds}ms (avg: {avgMs:F2}ms, ~{fps:F0} FPS)");
            }
            
            Console.WriteLine("\nMEMORY USAGE:");
            if (_memoryUsageResults.Any())
            {
                var avgMemoryMB = _memoryUsageResults.Average() / 1024.0 / 1024.0;
                var maxMemoryMB = _memoryUsageResults.Max() / 1024.0 / 1024.0;
                Console.WriteLine($"  Average: {avgMemoryMB:F3}MB, Peak: {maxMemoryMB:F3}MB");
            }
            
            Console.WriteLine("\nGARBAGE COLLECTION IMPACT:");
            if (_gcImpactResults.Any())
            {
                var avgGC = _gcImpactResults.Average();
                var maxGC = _gcImpactResults.Max();
                Console.WriteLine($"  Average GC time: {avgGC:F2}ms, Max: {maxGC}ms");
            }
            
            Console.WriteLine("\nSTRESS TEST RESULTS:");
            foreach (var result in _stressTestResults)
            {
                Console.WriteLine($"  {result.Key}: {result.Value.Operations:N0} ops in {result.Value.Milliseconds}ms");
            }
            
            if (_regressionResult != null)
            {
                Console.WriteLine("\nPERFORMANCE REGRESSION:");
                Console.WriteLine($"  Baseline: {_regressionResult.BaselineMs}ms");
                Console.WriteLine($"  With Lives System: {_regressionResult.WithLivesSystemMs}ms");
                Console.WriteLine($"  Impact Factor: {_regressionResult.ImpactFactor:F2}x");
            }
        }
        
        public bool HasPerformanceConcerns()
        {
            // Check for performance concerns
            foreach (var result in _safeSpawnResults.Values)
            {
                var avgMs = (double)result.Milliseconds / result.Operations;
                if (avgMs > 0.1) return true; // > 0.1ms per safe spawn call
            }
            
            foreach (var result in _updateResults.Values)
            {
                var avgMs = (double)result.Milliseconds / result.Operations;
                if (avgMs > 20.0) return true; // > 20ms per frame (< 50 FPS)
            }
            
            if (_memoryUsageResults.Any() && _memoryUsageResults.Max() > 10 * 1024 * 1024) // > 10MB
                return true;
            
            if (_regressionResult != null && _regressionResult.ImpactFactor > 1.5) // > 50% performance impact
                return true;
            
            return false;
        }
    }

    public class PerformanceResult
    {
        public int Operations { get; set; }
        public long Milliseconds { get; set; }
        public double OperationsPerSecond { get; set; }
    }

    public class PerformanceRegressionResult
    {
        public long BaselineMs { get; set; }
        public long WithLivesSystemMs { get; set; }
        public double ImpactFactor { get; set; }
    }

    #endregion
}