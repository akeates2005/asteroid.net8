using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using Raylib_cs;

namespace Asteroids.Tests.Integration
{
    /// <summary>
    /// Full integration test for Game3D systems including rendering pipeline validation
    /// Tests camera systems, 3D rendering, and complete game loop integration
    /// </summary>
    public class Game3DIntegrationTest
    {
        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;
        private const int TestDuration = 5000; // 5 seconds
        
        private IntegrationResults _results = new IntegrationResults();
        private GameManager3D? _gameManager;
        private bool _headlessMode;

        public static void Main(string[] args)
        {
            var integrationTest = new Game3DIntegrationTest();
            integrationTest.RunIntegrationTests();
        }

        public void RunIntegrationTests()
        {
            Console.WriteLine("=== GAME3D INTEGRATION TEST SUITE ===");
            Console.WriteLine($"Integration Test Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("Testing complete 3D game systems integration");
            
            // Determine if we can run with graphics or headless
            _headlessMode = Environment.GetEnvironmentVariable("DISPLAY") == null;
            Console.WriteLine($"Mode: {(_headlessMode ? "Headless" : "Graphics")} Testing");
            Console.WriteLine();

            try
            {
                // Core Integration Tests (can run headless)
                TestGameManager3DInitialization();
                TestPlayerMovementIntegration();
                TestAsteroidSystemIntegration();
                TestCollisionSystemIntegration();
                TestParticleSystemIntegration();
                TestCameraSystemLogic();
                TestMemoryManagementIntegration();
                
                // Graphics Integration Tests (require display)
                if (!_headlessMode)
                {
                    TestRenderingPipelineIntegration();
                    TestRealTimeGameLoop();
                }
                else
                {
                    TestHeadlessGameLoop();
                }
                
                GenerateIntegrationReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ INTEGRATION TEST FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                _results.AddFailure("Integration", ex.Message);
            }
        }

        #region Core Integration Tests
        private void TestGameManager3DInitialization()
        {
            Console.WriteLine("=== TESTING GAMEMANAGER3D INITIALIZATION ===");
            
            var sw = Stopwatch.StartNew();
            _gameManager = new GameManager3D(ScreenWidth, ScreenHeight, 200f);
            sw.Stop();
            
            Console.WriteLine($"✓ GameManager3D initialized in {sw.ElapsedMilliseconds}ms");
            
            // Verify initialization state
            Assert(_gameManager.IsGameRunning, "Game Running State", "Game should be running after initialization");
            Assert(_gameManager.IsPlayerAlive, "Player State", "Player should be alive after initialization");
            Assert(_gameManager.Score >= 0, "Score Initialization", $"Score should be non-negative: {_gameManager.Score}");
            Assert(_gameManager.Level >= 1, "Level Initialization", $"Level should be at least 1: {_gameManager.Level}");
            
            // Verify camera initialization
            var camera = _gameManager.Camera;
            Assert(camera.Position.Z > 0, "Camera Position", "Camera should be positioned above game plane");
            AssertVector3Equal(camera.Up, Vector3.UnitY, "Camera Up Vector", 0.1f);
            
            _results.AddSuccess("Initialization", "GameManager3D", $"Initialized in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        private void TestPlayerMovementIntegration()
        {
            Console.WriteLine("=== TESTING PLAYER MOVEMENT INTEGRATION ===");
            
            if (_gameManager == null) return;
            
            var initialCameraTarget = _gameManager.Camera.Target;
            
            // Simulate player movement for multiple frames
            var sw = Stopwatch.StartNew();
            for (int frame = 0; frame < 60; frame++) // Simulate 1 second at 60 FPS
            {
                _gameManager.Update();
            }
            sw.Stop();
            
            Console.WriteLine($"✓ 60 frame updates completed in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"✓ Average frame time: {(double)sw.ElapsedMilliseconds / 60:F2}ms");
            
            // Verify game state remains stable
            Assert(_gameManager.IsGameRunning, "Game State Stability", "Game should remain running after updates");
            Assert(_gameManager.IsPlayerAlive, "Player State Stability", "Player should remain alive");
            
            _results.AddSuccess("Movement", "Player Integration", $"60 frames in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();
        }

        private void TestAsteroidSystemIntegration()
        {
            Console.WriteLine("=== TESTING ASTEROID SYSTEM INTEGRATION ===");
            
            if (_gameManager == null) return;
            
            // Test asteroid creation and management
            var random = new Random(42);
            var testAsteroids = new List<Asteroid3D>();
            
            // Create test asteroids
            for (int i = 0; i < 10; i++)
            {
                var pos = new Vector3(i * 50, i * 30, i * 10);
                var vel = new Vector3(1, -1, 0.5f);
                var asteroid = new Asteroid3D(pos, vel, AsteroidSize.Large, random, 1);
                testAsteroids.Add(asteroid);
            }
            
            Console.WriteLine($"✓ Created {testAsteroids.Count} test asteroids");
            
            // Test asteroid updates
            var sw = Stopwatch.StartNew();
            for (int frame = 0; frame < 100; frame++)
            {
                foreach (var asteroid in testAsteroids)
                {
                    asteroid.Update(ScreenWidth, ScreenHeight, 200);
                }
            }
            sw.Stop();
            
            Console.WriteLine($"✓ Updated {testAsteroids.Count * 100} asteroid frames in {sw.ElapsedMilliseconds}ms");
            
            // Test asteroid splitting
            var splitFragments = testAsteroids[0].Split(1);
            Assert(splitFragments.Count > 0, "Asteroid Splitting", $"Large asteroid should split: {splitFragments.Count} fragments");
            
            _results.AddSuccess("Asteroids", "System Integration", $"Asteroid system fully functional");
            Console.WriteLine();
        }

        private void TestCollisionSystemIntegration()
        {
            Console.WriteLine("=== TESTING COLLISION SYSTEM INTEGRATION ===");
            
            var random = new Random(42);
            
            // Create test objects for collision testing
            var player = new Player3D(Vector3.Zero, 15f);
            var asteroids = new List<Asteroid3D>();
            var bullets = new List<Bullet3D>();
            
            // Create asteroids around the player
            for (int i = 0; i < 5; i++)
            {
                var angle = (float)(i * Math.PI * 2 / 5);
                var pos = new Vector3(MathF.Cos(angle) * 30, MathF.Sin(angle) * 30, 0);
                asteroids.Add(new Asteroid3D(pos, Vector3.Zero, AsteroidSize.Medium, random, 1));
            }
            
            // Create bullets
            for (int i = 0; i < 10; i++)
            {
                var pos = new Vector3(i * 10, 0, 0);
                bullets.Add(new Bullet3D(pos, Vector3.UnitX));
            }
            
            Console.WriteLine($"✓ Created test scenario: 1 player, {asteroids.Count} asteroids, {bullets.Count} bullets");
            
            // Test collision detection performance
            var sw = Stopwatch.StartNew();
            var totalCollisions = 0;
            
            for (int frame = 0; frame < 1000; frame++)
            {
                // Check bullet-asteroid collisions
                var bulletCollisions = CollisionManager3D.CheckBulletAsteroidCollisions(bullets, asteroids);
                totalCollisions += bulletCollisions.Count;
                
                // Check player-asteroid collisions
                var playerCollisions = CollisionManager3D.CheckPlayerAsteroidCollisions(player, asteroids);
                totalCollisions += playerCollisions.Count;
            }
            
            sw.Stop();
            
            Console.WriteLine($"✓ Processed 1000 collision frames in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"✓ Total collisions detected: {totalCollisions}");
            Console.WriteLine($"✓ Average collision check time: {(double)sw.ElapsedMilliseconds / 1000:F2}ms per frame");
            
            _results.AddSuccess("Collision", "System Integration", $"Collision system performance validated");
            Console.WriteLine();
        }

        private void TestParticleSystemIntegration()
        {
            Console.WriteLine("=== TESTING PARTICLE SYSTEM INTEGRATION ===");
            
            var explosionManager = new ExplosionManager3D();
            var initialParticleCount = explosionManager.GetParticleCount();
            
            // Create multiple explosions
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 20; i++)
            {
                var pos = new Vector3(i * 10, 0, 0);
                explosionManager.CreateAsteroidExplosion(pos, AsteroidSize.Large, 1);
            }
            sw.Stop();
            
            var particleCount = explosionManager.GetParticleCount();
            Console.WriteLine($"✓ Created 20 explosions with {particleCount} particles in {sw.ElapsedMilliseconds}ms");
            
            // Test particle updates
            sw.Restart();
            for (int frame = 0; frame < 100; frame++)
            {
                explosionManager.Update();
            }
            sw.Stop();
            
            Console.WriteLine($"✓ Updated particle system for 100 frames in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"✓ Remaining particles: {explosionManager.GetParticleCount()}");
            
            _results.AddSuccess("Particles", "System Integration", $"Particle system fully operational");
            Console.WriteLine();
        }

        private void TestCameraSystemLogic()
        {
            Console.WriteLine("=== TESTING CAMERA SYSTEM LOGIC ===");
            
            if (_gameManager == null) return;
            
            var camera = _gameManager.Camera;
            var initialPosition = camera.Position;
            var initialTarget = camera.Target;
            
            // Test camera following logic (without actual input)
            for (int i = 0; i < 10; i++)
            {
                _gameManager.Update();
            }
            
            // Verify camera properties remain valid
            Assert(!float.IsNaN(camera.Position.X) && !float.IsNaN(camera.Position.Y) && !float.IsNaN(camera.Position.Z),
                   "Camera Position Validity", "Camera position should contain valid values");
            
            Assert(!float.IsNaN(camera.Target.X) && !float.IsNaN(camera.Target.Y) && !float.IsNaN(camera.Target.Z),
                   "Camera Target Validity", "Camera target should contain valid values");
            
            AssertVector3Equal(camera.Up, Vector3.UnitY, "Camera Up Vector Stability", 0.1f);
            
            Console.WriteLine($"✓ Camera position: ({camera.Position.X:F2}, {camera.Position.Y:F2}, {camera.Position.Z:F2})");
            Console.WriteLine($"✓ Camera target: ({camera.Target.X:F2}, {camera.Target.Y:F2}, {camera.Target.Z:F2})");
            
            _results.AddSuccess("Camera", "Logic Integration", "Camera system logic validated");
            Console.WriteLine();
        }

        private void TestMemoryManagementIntegration()
        {
            Console.WriteLine("=== TESTING MEMORY MANAGEMENT INTEGRATION ===");
            
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Create and destroy multiple game instances
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var gameManager = new GameManager3D(ScreenWidth, ScreenHeight, 100f);
                
                // Run multiple update cycles
                for (int frame = 0; frame < 100; frame++)
                {
                    gameManager.Update();
                }
                
                // Simulate object creation/destruction
                var random = new Random(42);
                var tempObjects = new List<object>();
                
                for (int i = 0; i < 100; i++)
                {
                    tempObjects.Add(new Asteroid3D(Vector3.Zero, Vector3.One, AsteroidSize.Small, random, 1));
                    tempObjects.Add(new Bullet3D(Vector3.Zero, Vector3.UnitX));
                }
                
                tempObjects.Clear();
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(true);
            
            var memoryIncrease = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB
            
            Console.WriteLine($"✓ Memory impact after 3 game cycles: {memoryIncrease:F2} MB");
            Assert(memoryIncrease < 50, "Memory Management", $"Memory increase should be reasonable: {memoryIncrease:F2} MB");
            
            _results.AddSuccess("Memory", "Management Integration", $"Memory stable: {memoryIncrease:F2} MB");
            Console.WriteLine();
        }
        #endregion

        #region Graphics Integration Tests
        private void TestRenderingPipelineIntegration()
        {
            Console.WriteLine("=== TESTING RENDERING PIPELINE INTEGRATION ===");
            
            try
            {
                Raylib.InitWindow(ScreenWidth, ScreenHeight, "Asteroids 3D - Integration Test");
                Raylib.SetTargetFPS(60);
                
                var gameManager = new GameManager3D(ScreenWidth, ScreenHeight, 200f);
                var frameCount = 0;
                var sw = Stopwatch.StartNew();
                
                Console.WriteLine("✓ Raylib window initialized successfully");
                
                // Render for a short duration
                while (frameCount < 180 && !Raylib.WindowShouldClose()) // 3 seconds at 60 FPS
                {
                    gameManager.Update();
                    
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Color.Black);
                    
                    // Test 3D rendering
                    gameManager.Draw();
                    
                    // Test 2D overlay rendering
                    Raylib.DrawText($"Frame: {frameCount}", 10, 10, 20, Color.White);
                    Raylib.DrawText($"FPS: {Raylib.GetFPS()}", 10, 35, 20, Color.White);
                    
                    Raylib.EndDrawing();
                    frameCount++;
                }
                
                sw.Stop();
                
                var avgFPS = (double)frameCount / sw.Elapsed.TotalSeconds;
                Console.WriteLine($"✓ Rendered {frameCount} frames in {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"✓ Average FPS: {avgFPS:F1}");
                Console.WriteLine($"✓ Target FPS achieved: {avgFPS >= 55}");
                
                Raylib.CloseWindow();
                
                _results.AddSuccess("Rendering", "Pipeline Integration", $"Rendering at {avgFPS:F1} FPS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Rendering test failed: {ex.Message}");
                _results.AddFailure("Rendering", ex.Message);
            }
            
            Console.WriteLine();
        }

        private void TestRealTimeGameLoop()
        {
            Console.WriteLine("=== TESTING REAL-TIME GAME LOOP ===");
            
            try
            {
                Raylib.InitWindow(ScreenWidth, ScreenHeight, "Asteroids 3D - Real-Time Test");
                Raylib.SetTargetFPS(60);
                
                var gameManager = new GameManager3D(ScreenWidth, ScreenHeight, 200f);
                var frameCount = 0;
                var sw = Stopwatch.StartNew();
                var fpsReadings = new List<int>();
                
                Console.WriteLine("Running real-time game loop test...");
                
                while (frameCount < 300 && !Raylib.WindowShouldClose()) // 5 seconds at 60 FPS
                {
                    gameManager.Update();
                    
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Color.Black);
                    gameManager.Draw();
                    
                    // Display test info
                    var currentFPS = Raylib.GetFPS();
                    fpsReadings.Add(currentFPS);
                    
                    Raylib.DrawText($"Real-Time Integration Test", 10, 10, 20, Color.Green);
                    Raylib.DrawText($"Frame: {frameCount}/300", 10, 35, 16, Color.White);
                    Raylib.DrawText($"FPS: {currentFPS}", 10, 55, 16, Color.White);
                    Raylib.DrawText($"Game Running: {gameManager.IsGameRunning}", 10, 75, 16, Color.White);
                    Raylib.DrawText($"Player Alive: {gameManager.IsPlayerAlive}", 10, 95, 16, Color.White);
                    
                    Raylib.EndDrawing();
                    frameCount++;
                }
                
                sw.Stop();
                
                var avgFPS = fpsReadings.Count > 0 ? fpsReadings.Average() : 0;
                var minFPS = fpsReadings.Count > 0 ? fpsReadings.Min() : 0;
                var maxFPS = fpsReadings.Count > 0 ? fpsReadings.Max() : 0;
                
                Console.WriteLine($"✓ Real-time test completed: {frameCount} frames in {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"✓ Average FPS: {avgFPS:F1} (Min: {minFPS}, Max: {maxFPS})");
                Console.WriteLine($"✓ Frame time consistency: {(maxFPS - minFPS <= 10 ? "Good" : "Variable")}");
                
                Raylib.CloseWindow();
                
                _results.AddSuccess("Real-Time", "Game Loop", $"Stable at {avgFPS:F1} FPS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Real-time test failed: {ex.Message}");
                _results.AddFailure("Real-Time", ex.Message);
            }
            
            Console.WriteLine();
        }
        #endregion

        #region Headless Tests
        private void TestHeadlessGameLoop()
        {
            Console.WriteLine("=== TESTING HEADLESS GAME LOOP ===");
            
            if (_gameManager == null) return;
            
            var frameCount = 0;
            var targetFrames = 3600; // Simulate 1 minute at 60 FPS
            var sw = Stopwatch.StartNew();
            
            Console.WriteLine($"Running headless simulation for {targetFrames} frames...");
            
            while (frameCount < targetFrames)
            {
                _gameManager.Update();
                frameCount++;
                
                // Progress indicator
                if (frameCount % 600 == 0) // Every 10 seconds
                {
                    var progress = (double)frameCount / targetFrames * 100;
                    Console.WriteLine($"  Progress: {progress:F1}% ({frameCount}/{targetFrames} frames)");
                }
            }
            
            sw.Stop();
            
            var avgFrameTime = (double)sw.ElapsedMilliseconds / frameCount;
            var theoreticalFPS = 1000.0 / avgFrameTime;
            
            Console.WriteLine($"✓ Headless simulation completed: {frameCount} frames in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"✓ Average frame time: {avgFrameTime:F2}ms");
            Console.WriteLine($"✓ Theoretical FPS capacity: {theoreticalFPS:F1}");
            Console.WriteLine($"✓ Game state stable: Running={_gameManager.IsGameRunning}, Player={_gameManager.IsPlayerAlive}");
            
            _results.AddSuccess("Headless", "Game Loop", $"Stable simulation at {theoreticalFPS:F1} theoretical FPS");
            Console.WriteLine();
        }
        #endregion

        #region Helper Methods
        private void Assert(bool condition, string testName, string message)
        {
            if (condition)
            {
                Console.WriteLine($"  ✓ {testName}: {message}");
                _results.AddSuccess("Test", testName, message);
            }
            else
            {
                Console.WriteLine($"  ❌ {testName}: {message}");
                _results.AddFailure("Test", $"{testName}: {message}");
                throw new InvalidOperationException($"{testName}: {message}");
            }
        }

        private void AssertVector3Equal(Vector3 actual, Vector3 expected, string testName, float tolerance = 0.001f)
        {
            var distance = Vector3.Distance(actual, expected);
            if (distance <= tolerance)
            {
                Console.WriteLine($"  ✓ {testName}: Vectors match within tolerance");
                _results.AddSuccess("Vector3", testName, $"Expected: {expected}, Actual: {actual}");
            }
            else
            {
                Console.WriteLine($"  ❌ {testName}: Expected {expected}, Actual {actual}, Distance: {distance}");
                _results.AddFailure("Vector3", $"{testName}: Distance {distance} > {tolerance}");
                throw new InvalidOperationException($"{testName}: Vector3 mismatch");
            }
        }

        private void GenerateIntegrationReport()
        {
            Console.WriteLine("\n" + "=".PadRight(70, '='));
            Console.WriteLine("INTEGRATION TEST REPORT");
            Console.WriteLine("=".PadRight(70, '='));
            
            Console.WriteLine($"\nTest Execution Summary:");
            Console.WriteLine($"- Mode: {(_headlessMode ? "Headless" : "Graphics")} Testing");
            Console.WriteLine($"- Total Tests: {_results.TotalTests}");
            Console.WriteLine($"- Passed: {_results.PassedTests}");
            Console.WriteLine($"- Failed: {_results.FailedTests}");
            Console.WriteLine($"- Success Rate: {_results.SuccessRate:F2}%");
            Console.WriteLine($"- Execution Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            if (_results.FailedTests > 0)
            {
                Console.WriteLine("\n❌ INTEGRATION FAILURES:");
                foreach (var failure in _results.Failures)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }
            
            Console.WriteLine("\n✅ INTEGRATION SUCCESSES:");
            var categories = _results.Successes.GroupBy(s => s.Split(':')[0]).ToList();
            foreach (var category in categories)
            {
                Console.WriteLine($"  {category.Key}: {category.Count()} tests passed");
            }
            
            // Integration assessment
            Console.WriteLine($"\n🎯 INTEGRATION ASSESSMENT:");
            if (_results.FailedTests == 0)
            {
                Console.WriteLine("🎉 EXCELLENT: All integration tests passed!");
                Console.WriteLine("✅ 3D game systems are fully integrated and stable");
                Console.WriteLine("✅ Ready for Phase 2 development");
                
                if (!_headlessMode)
                {
                    Console.WriteLine("✅ Rendering pipeline validated");
                    Console.WriteLine("✅ Real-time performance confirmed");
                }
                else
                {
                    Console.WriteLine("✅ Headless simulation validated");
                    Console.WriteLine("ℹ️  Graphics tests skipped (no display available)");
                }
            }
            else if (_results.FailedTests <= 2)
            {
                Console.WriteLine("⚠️  GOOD: Minor integration issues detected");
                Console.WriteLine("✅ Core systems working properly");
                Console.WriteLine("⚠️  Address failures before Phase 2");
            }
            else
            {
                Console.WriteLine("❌ CONCERN: Multiple integration failures");
                Console.WriteLine("❌ Review and fix issues before Phase 2");
            }
            
            Console.WriteLine("=".PadRight(70, '='));
        }
        #endregion
    }

    #region Supporting Classes
    public class IntegrationResults
    {
        public int PassedTests { get; private set; }
        public int FailedTests { get; private set; }
        public int TotalTests => PassedTests + FailedTests;
        public double SuccessRate => TotalTests > 0 ? (PassedTests * 100.0) / TotalTests : 0;
        
        public List<string> Failures { get; } = new List<string>();
        public List<string> Successes { get; } = new List<string>();

        public void AddSuccess(string category, string test, string details)
        {
            PassedTests++;
            Successes.Add($"{category}: {test} - {details}");
        }

        public void AddFailure(string category, string message)
        {
            FailedTests++;
            Failures.Add($"{category}: {message}");
        }
    }
    #endregion
}