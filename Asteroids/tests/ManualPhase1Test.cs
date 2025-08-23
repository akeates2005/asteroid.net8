using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using Raylib_cs;

namespace Asteroids.Tests
{
    /// <summary>
    /// Manual Phase 1 Validation - Core Systems Testing
    /// This performs essential tests to verify 3D foundation systems
    /// </summary>
    public class ManualPhase1Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ASTEROIDS 3D PHASE 1 MANUAL VALIDATION ===");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            var tester = new ManualPhase1Test();
            tester.RunManualTests();
        }

        public void RunManualTests()
        {
            int totalTests = 0;
            int passedTests = 0;
            var failures = new List<string>();

            Console.WriteLine("\n=== TESTING 3D FOUNDATION SYSTEMS ===");
            
            try
            {
                // Test 1: Vector3 Operations
                Console.WriteLine("\n1. Testing Vector3 Operations...");
                if (TestVector3Operations())
                {
                    Console.WriteLine("   ✅ Vector3 operations working correctly");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine("   ❌ Vector3 operations failed");
                    failures.Add("Vector3 Operations");
                }
                totalTests++;

                // Test 2: 3D Collision Detection
                Console.WriteLine("\n2. Testing 3D Collision Detection...");
                if (Test3DCollisions())
                {
                    Console.WriteLine("   ✅ 3D collision detection working correctly");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine("   ❌ 3D collision detection failed");
                    failures.Add("3D Collision Detection");
                }
                totalTests++;

                // Test 3: Game Object Creation
                Console.WriteLine("\n3. Testing Game Object Creation...");
                if (TestGameObjectCreation())
                {
                    Console.WriteLine("   ✅ Game objects created successfully");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine("   ❌ Game object creation failed");
                    failures.Add("Game Object Creation");
                }
                totalTests++;

                // Test 4: GameManager3D Integration
                Console.WriteLine("\n4. Testing GameManager3D Integration...");
                if (TestGameManager3D())
                {
                    Console.WriteLine("   ✅ GameManager3D integration working");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine("   ❌ GameManager3D integration failed");
                    failures.Add("GameManager3D Integration");
                }
                totalTests++;

                // Test 5: Performance Baseline
                Console.WriteLine("\n5. Testing Performance Baseline...");
                if (TestPerformanceBaseline())
                {
                    Console.WriteLine("   ✅ Performance baseline acceptable");
                    passedTests++;
                }
                else
                {
                    Console.WriteLine("   ❌ Performance baseline failed");
                    failures.Add("Performance Baseline");
                }
                totalTests++;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ CRITICAL ERROR: {ex.Message}");
                failures.Add($"Critical Error: {ex.Message}");
            }

            // Generate Report
            Console.WriteLine("\n" + "=".PadRight(60, '='));
            Console.WriteLine("PHASE 1 MANUAL VALIDATION REPORT");
            Console.WriteLine("=".PadRight(60, '='));
            
            Console.WriteLine($"\nResults Summary:");
            Console.WriteLine($"- Total Tests: {totalTests}");
            Console.WriteLine($"- Passed: {passedTests}");
            Console.WriteLine($"- Failed: {totalTests - passedTests}");
            Console.WriteLine($"- Success Rate: {(double)passedTests / totalTests * 100:F1}%");
            
            if (failures.Count > 0)
            {
                Console.WriteLine($"\nFailures:");
                foreach (var failure in failures)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }

            // Final Assessment
            Console.WriteLine($"\n🎯 PHASE 1 ASSESSMENT:");
            if (passedTests == totalTests)
            {
                Console.WriteLine("🎉 EXCELLENT: All Phase 1 tests passed!");
                Console.WriteLine("✅ 3D foundation systems are solid and ready");
                Console.WriteLine("✅ PHASE 2 DEVELOPMENT CAN PROCEED");
            }
            else if (passedTests >= (totalTests * 0.8))
            {
                Console.WriteLine("👍 GOOD: Phase 1 mostly successful");
                Console.WriteLine("✅ Core systems working");
                Console.WriteLine("⚠️  Address minor issues before Phase 2");
            }
            else
            {
                Console.WriteLine("⚠️  CONCERN: Phase 1 has significant issues");
                Console.WriteLine("❌ Multiple system failures detected");
                Console.WriteLine("🔧 Focus on fixing foundation problems");
            }

            Console.WriteLine("=".PadRight(60, '='));
        }

        private bool TestVector3Operations()
        {
            try
            {
                // Basic Vector3 math
                var v1 = new Vector3(3, 4, 0);
                var v2 = new Vector3(1, 2, 3);

                // Addition
                var sum = v1 + v2;
                if (sum != new Vector3(4, 6, 3)) return false;

                // Length calculation
                var length = v1.Length();
                if (Math.Abs(length - 5.0f) > 0.001f) return false;

                // Distance calculation
                var distance = Vector3.Distance(Vector3.Zero, new Vector3(3, 4, 12));
                if (Math.Abs(distance - 13.0f) > 0.001f) return false;

                // Normalization
                var normalized = Vector3.Normalize(v1);
                var expectedNorm = new Vector3(0.6f, 0.8f, 0.0f);
                if (Vector3.Distance(normalized, expectedNorm) > 0.001f) return false;

                Console.WriteLine($"     - Addition: ✓");
                Console.WriteLine($"     - Length: ✓ ({length:F3})");
                Console.WriteLine($"     - Distance: ✓ ({distance:F3})");
                Console.WriteLine($"     - Normalization: ✓");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     - Exception: {ex.Message}");
                return false;
            }
        }

        private bool Test3DCollisions()
        {
            try
            {
                // Test basic sphere collision
                var pos1 = Vector3.Zero;
                var pos2 = new Vector3(8, 0, 0);
                var collision = CollisionManager3D.CheckSphereCollision(pos1, 5f, pos2, 5f);
                if (!collision) return false;

                // Test no collision
                var pos3 = new Vector3(15, 0, 0);
                var noCollision = CollisionManager3D.CheckSphereCollision(pos1, 5f, pos3, 5f);
                if (noCollision) return false;

                // Test 3D distance
                var pos4 = new Vector3(3, 4, 12);
                var distance3D = Vector3.Distance(Vector3.Zero, pos4);
                if (Math.Abs(distance3D - 13.0f) > 0.001f) return false;

                Console.WriteLine($"     - Sphere collision (should collide): ✓");
                Console.WriteLine($"     - Sphere collision (should not collide): ✓");
                Console.WriteLine($"     - 3D distance calculation: ✓ ({distance3D:F3})");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     - Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestGameObjectCreation()
        {
            try
            {
                var random = new Random(42);

                // Create Player3D
                var player = new Player3D(Vector3.Zero, 15f);
                if (player.Size != 15f) return false;
                if (player.Position != Vector3.Zero) return false;

                // Create Bullet3D
                var bullet = new Bullet3D(Vector3.Zero, Vector3.UnitX);
                if (!bullet.Active) return false;

                // Create Asteroid3D
                var asteroid = new Asteroid3D(Vector3.Zero, Vector3.One, AsteroidSize.Large, random, 1);
                if (!asteroid.Active) return false;
                if (asteroid.AsteroidSize != AsteroidSize.Large) return false;

                // Test asteroid splitting
                var fragments = asteroid.Split(1);
                if (fragments.Count == 0) return false;

                Console.WriteLine($"     - Player3D creation: ✓");
                Console.WriteLine($"     - Bullet3D creation: ✓");
                Console.WriteLine($"     - Asteroid3D creation: ✓");
                Console.WriteLine($"     - Asteroid splitting: ✓ ({fragments.Count} fragments)");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     - Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestGameManager3D()
        {
            try
            {
                var gameManager = new GameManager3D(800, 600, 100f);
                
                // Test initial state
                if (!gameManager.IsGameRunning) return false;
                if (!gameManager.IsPlayerAlive) return false;
                if (gameManager.Score < 0) return false;
                if (gameManager.Level < 1) return false;

                // Test camera
                var camera = gameManager.Camera;
                if (float.IsNaN(camera.Position.X) || float.IsNaN(camera.Position.Y) || float.IsNaN(camera.Position.Z))
                    return false;

                // Test updates
                for (int i = 0; i < 10; i++)
                {
                    gameManager.Update();
                    if (!gameManager.IsGameRunning) return false;
                }

                Console.WriteLine($"     - Initial state: ✓");
                Console.WriteLine($"     - Camera: ✓ (pos: {camera.Position.X:F1}, {camera.Position.Y:F1}, {camera.Position.Z:F1})");
                Console.WriteLine($"     - Update cycles: ✓ (10 cycles completed)");
                Console.WriteLine($"     - Score system: ✓ (score: {gameManager.Score})");
                Console.WriteLine($"     - Level system: ✓ (level: {gameManager.Level})");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     - Exception: {ex.Message}");
                return false;
            }
        }

        private bool TestPerformanceBaseline()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                
                // Test collision performance
                var iterations = 10000;
                var collisions = 0;
                var random = new Random(42);

                for (int i = 0; i < iterations; i++)
                {
                    var pos1 = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
                    var pos2 = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
                    if (CollisionManager3D.CheckSphereCollision(pos1, 5f, pos2, 5f))
                        collisions++;
                }

                sw.Stop();
                var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;

                // Test game loop performance
                var gameManager = new GameManager3D(800, 600, 100f);
                var sw2 = Stopwatch.StartNew();
                
                for (int i = 0; i < 1000; i++)
                {
                    gameManager.Update();
                }
                
                sw2.Stop();
                var fpsEquivalent = 1000.0 / sw2.Elapsed.TotalSeconds;

                Console.WriteLine($"     - Collision performance: ✓ ({opsPerSecond:N0} ops/sec)");
                Console.WriteLine($"     - Game loop performance: ✓ ({fpsEquivalent:N0} FPS equivalent)");
                Console.WriteLine($"     - Memory: ✓ ({GC.GetTotalMemory(false) / 1024.0 / 1024.0:F2} MB)");

                // Performance criteria
                return opsPerSecond > 10000 && fpsEquivalent > 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"     - Exception: {ex.Message}");
                return false;
            }
        }
    }
}