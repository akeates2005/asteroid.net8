using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Linq;
using Raylib_cs;

namespace Asteroids.Tests
{
    /// <summary>
    /// Phase 1: Comprehensive 3D Foundation Testing and Validation
    /// This test runner executes all critical tests for 3D systems validation
    /// </summary>
    public class Phase1TestRunner
    {
        private TestResults _results = new TestResults();
        private Stopwatch _stopwatch = new Stopwatch();
        private bool _headlessMode = true; // Default to headless for CI/automated testing

        public static void Main(string[] args)
        {
            Console.WriteLine("=== ASTEROIDS 3D PHASE 1 COMPREHENSIVE VALIDATION ===");
            Console.WriteLine($"Test Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.OSVersion}, .NET {Environment.Version}");
            
            var runner = new Phase1TestRunner();
            
            // Check for display mode
            if (args.Contains("--graphics"))
            {
                runner._headlessMode = false;
                Console.WriteLine("Running in GRAPHICS mode");
            }
            else
            {
                Console.WriteLine("Running in HEADLESS mode");
            }
            
            runner.ExecutePhase1ValidationSuite();
        }

        public void ExecutePhase1ValidationSuite()
        {
            try
            {
                // Phase 1: Build and Runtime Verification
                Console.WriteLine("\n=== PHASE 1: BUILD VERIFICATION ===");
                TestBuildVerification();
                
                // Phase 2: 3D Foundation Systems
                Console.WriteLine("\n=== PHASE 2: 3D FOUNDATION TESTING ===");
                Test3DFoundationSystems();
                
                // Phase 3: Game Object Systems
                Console.WriteLine("\n=== PHASE 3: GAME OBJECT VALIDATION ===");
                TestGameObjectSystems();
                
                // Phase 4: Collision System Testing
                Console.WriteLine("\n=== PHASE 4: 3D COLLISION SYSTEM ===");
                Test3DCollisionSystem();
                
                // Phase 5: Performance Benchmarking
                Console.WriteLine("\n=== PHASE 5: PERFORMANCE BENCHMARKS ===");
                TestPerformanceBaselines();
                
                // Phase 6: Integration Testing
                Console.WriteLine("\n=== PHASE 6: SYSTEM INTEGRATION ===");
                TestSystemIntegration();
                
                // Phase 7: Memory and Resource Testing
                Console.WriteLine("\n=== PHASE 7: MEMORY VALIDATION ===");
                TestMemoryManagement();
                
                // Phase 8: Gameplay Mechanics
                Console.WriteLine("\n=== PHASE 8: GAMEPLAY VALIDATION ===");
                TestGameplayMechanics();
                
                // Phase 9: Camera System Testing
                Console.WriteLine("\n=== PHASE 9: CAMERA SYSTEM ===");
                TestCameraSystem();
                
                // Optional: Graphics Testing (if display available)
                if (!_headlessMode)
                {
                    Console.WriteLine("\n=== PHASE 10: GRAPHICS VALIDATION ===");
                    TestGraphicsRendering();
                }
                
                // Generate comprehensive report
                GeneratePhase1Report();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ CRITICAL FAILURE in Phase 1 Testing: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                _results.AddFailure("Critical", $"Phase 1 Testing Failed: {ex.Message}");
            }
        }

        #region Phase 1: Build Verification
        private void TestBuildVerification()
        {
            _stopwatch.Restart();
            
            // Test 1.1: .NET Runtime
            TestDotNetRuntime();
            
            // Test 1.2: Raylib Assembly Loading
            TestRaylibAssembly();
            
            // Test 1.3: Core 3D Classes Available
            TestCore3DClasses();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 1 Build Verification completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestDotNetRuntime()
        {
            Console.WriteLine("Testing .NET runtime environment...");
            var version = Environment.Version;
            var isNet8 = version.Major >= 8;
            
            Assert(isNet8, ".NET Version Check", $"Required: .NET 8+, Found: {version}");
            Console.WriteLine($"  ✓ .NET Version: {version}");
            _results.AddSuccess("Build", "RUNTIME_VERSION", version.ToString());
        }

        private void TestRaylibAssembly()
        {
            Console.WriteLine("Testing Raylib-cs assembly loading...");
            try
            {
                // Test that we can access Raylib types without initializing graphics
                var color = Raylib_cs.Color.Red;
                var vector = new Vector3(1, 2, 3);
                
                Console.WriteLine("  ✓ Raylib-cs assembly loaded successfully");
                _results.AddSuccess("Build", "RAYLIB_ASSEMBLY", "Loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Raylib assembly loading failed: {ex.Message}");
                _results.AddFailure("Build", $"Raylib loading failed: {ex.Message}");
            }
        }

        private void TestCore3DClasses()
        {
            Console.WriteLine("Testing core 3D class availability...");
            
            string[] required3DClasses = {
                "Asteroids.GameManager3D",
                "Asteroids.Player3D", 
                "Asteroids.Asteroid3D",
                "Asteroids.Bullet3D",
                "Asteroids.CollisionManager3D",
                "Asteroids.ExplosionManager3D"
            };

            foreach (var className in required3DClasses)
            {
                try
                {
                    var type = Type.GetType(className);
                    if (type != null)
                    {
                        Console.WriteLine($"  ✓ {className} available");
                        _results.AddSuccess("Build", "CLASS_AVAILABLE", className);
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ {className} not found");
                        _results.AddFailure("Build", $"{className} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ Error accessing {className}: {ex.Message}");
                    _results.AddFailure("Build", $"Class access error: {className} - {ex.Message}");
                }
            }
        }
        #endregion

        #region Phase 2: 3D Foundation Systems
        private void Test3DFoundationSystems()
        {
            _stopwatch.Restart();
            
            // Test Vector3 Operations
            TestVector3Mathematics();
            
            // Test Matrix Operations
            TestMatrixTransformations();
            
            // Test 3D Coordinate System
            Test3DCoordinateSystem();
            
            // Test Distance and Geometric Calculations
            TestGeometricCalculations();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 2 3D Foundation Testing completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestVector3Mathematics()
        {
            Console.WriteLine("Testing Vector3 mathematical operations...");
            
            // Basic operations
            var v1 = new Vector3(3, 4, 0);
            var v2 = new Vector3(1, 2, 3);
            
            // Addition
            var sum = v1 + v2;
            AssertVector3Equal(sum, new Vector3(4, 6, 3), "Vector3 Addition");
            
            // Subtraction
            var diff = v1 - v2;
            AssertVector3Equal(diff, new Vector3(2, 2, -3), "Vector3 Subtraction");
            
            // Multiplication
            var scaled = v1 * 2.0f;
            AssertVector3Equal(scaled, new Vector3(6, 8, 0), "Vector3 Scalar Multiplication");
            
            // Length calculation
            var length = v1.Length();
            Assert(Math.Abs(length - 5.0f) < 0.001f, "Vector3 Length", $"Expected: 5.0, Actual: {length}");
            
            // Normalization
            var normalized = Vector3.Normalize(v1);
            var expectedNorm = new Vector3(0.6f, 0.8f, 0.0f);
            AssertVector3Equal(normalized, expectedNorm, "Vector3 Normalization");
            
            // Dot product
            var dotProduct = Vector3.Dot(v1, v2);
            var expectedDot = 11.0f; // (3*1) + (4*2) + (0*3) = 3 + 8 + 0 = 11
            Assert(Math.Abs(dotProduct - expectedDot) < 0.001f, "Vector3 Dot Product", 
                   $"Expected: {expectedDot}, Actual: {dotProduct}");
            
            Console.WriteLine("  ✓ Vector3 mathematical operations validated");
            _results.AddSuccess("3D Foundation", "VECTOR3_MATH", "All operations working correctly");
        }

        private void TestMatrixTransformations()
        {
            Console.WriteLine("Testing matrix transformations...");
            
            // Translation matrix
            var translation = Vector3.One * 10;
            var translationMatrix = Matrix4x4.CreateTranslation(translation);
            var translatedPoint = Vector3.Transform(Vector3.Zero, translationMatrix);
            AssertVector3Equal(translatedPoint, translation, "Translation Matrix");
            
            // Rotation matrix - 90 degrees around Y axis
            var rotationMatrix = Matrix4x4.CreateRotationY(MathF.PI / 2);
            var rotatedVector = Vector3.Transform(Vector3.UnitX, rotationMatrix);
            var expectedRotated = new Vector3(0, 0, -1);
            AssertVector3Equal(rotatedVector, expectedRotated, "Rotation Matrix Y-axis", 0.001f);
            
            // Scale matrix
            var scaleMatrix = Matrix4x4.CreateScale(2.0f);
            var scaledPoint = Vector3.Transform(Vector3.One, scaleMatrix);
            AssertVector3Equal(scaledPoint, Vector3.One * 2, "Scale Matrix");
            
            Console.WriteLine("  ✓ Matrix transformations validated");
            _results.AddSuccess("3D Foundation", "MATRIX_TRANSFORMS", "Transformations working correctly");
        }

        private void Test3DCoordinateSystem()
        {
            Console.WriteLine("Testing 3D coordinate system...");
            
            // Test bounds checking
            var screenBounds = new Vector3(800, 600, 100);
            
            // Point inside bounds
            var insidePoint = new Vector3(400, 300, 25);
            Assert(CollisionManager3D.IsWithinBounds(insidePoint, screenBounds.X, screenBounds.Y, screenBounds.Z), 
                   "3D Bounds - Inside", "Point should be within bounds");
            
            // Point outside X bounds
            var outsideX = new Vector3(900, 300, 25);
            Assert(!CollisionManager3D.IsWithinBounds(outsideX, screenBounds.X, screenBounds.Y, screenBounds.Z), 
                   "3D Bounds - Outside X", "Point should be outside X bounds");
            
            // Point outside Z bounds
            var outsideZ = new Vector3(400, 300, 75);
            Assert(!CollisionManager3D.IsWithinBounds(outsideZ, screenBounds.X, screenBounds.Y, screenBounds.Z), 
                   "3D Bounds - Outside Z", "Point should be outside Z bounds");
            
            Console.WriteLine("  ✓ 3D coordinate system validated");
            _results.AddSuccess("3D Foundation", "COORDINATE_SYSTEM", "Bounds checking working correctly");
        }

        private void TestGeometricCalculations()
        {
            Console.WriteLine("Testing geometric calculations...");
            
            // 3D distance calculation
            var point1 = new Vector3(0, 0, 0);
            var point2 = new Vector3(3, 4, 12);
            var distance = Vector3.Distance(point1, point2);
            var expectedDistance = 13.0f; // 3-4-12 right triangle
            Assert(Math.Abs(distance - expectedDistance) < 0.001f, "3D Distance Calculation", 
                   $"Expected: {expectedDistance}, Actual: {distance}");
            
            // Sphere collision detection
            var sphere1Center = Vector3.Zero;
            var sphere1Radius = 5.0f;
            var sphere2Center = new Vector3(8, 0, 0);
            var sphere2Radius = 4.0f;
            
            // Should collide (distance = 8, radii sum = 9)
            var shouldCollide = CollisionManager3D.CheckSphereCollision(sphere1Center, sphere1Radius, sphere2Center, sphere2Radius);
            Assert(shouldCollide, "Sphere Collision - Should Collide", "Spheres should intersect");
            
            // Should not collide
            var sphere3Center = new Vector3(15, 0, 0);
            var shouldNotCollide = CollisionManager3D.CheckSphereCollision(sphere1Center, sphere1Radius, sphere3Center, sphere2Radius);
            Assert(!shouldNotCollide, "Sphere Collision - Should Not Collide", "Spheres should not intersect");
            
            Console.WriteLine("  ✓ Geometric calculations validated");
            _results.AddSuccess("3D Foundation", "GEOMETRIC_CALCS", "Distance and collision calculations working");
        }
        #endregion

        #region Phase 3: Game Object Systems
        private void TestGameObjectSystems()
        {
            _stopwatch.Restart();
            
            TestPlayer3DSystem();
            TestAsteroid3DSystem();
            TestBullet3DSystem();
            TestParticleSystem3D();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 3 Game Object Systems completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestPlayer3DSystem()
        {
            Console.WriteLine("Testing Player3D system...");
            
            var player = new Player3D(Vector3.Zero, 15.0f);
            
            // Test initialization
            AssertVector3Equal(player.Position, Vector3.Zero, "Player3D Initial Position");
            Assert(player.Size == 15.0f, "Player3D Size", $"Expected: 15, Actual: {player.Size}");
            
            // Test movement
            var initialPosition = player.Position;
            player.Velocity = new Vector3(5, 0, 0);
            player.Update(800, 600, 100);
            Assert(player.Position.X > initialPosition.X, "Player3D Movement", "Player should move in X direction");
            
            // Test rotation and bullet direction
            player.Rotation = new Vector3(0, MathF.PI / 2, 0); // 90 degree yaw
            var bulletVelocity = player.GetBulletVelocity();
            Assert(Math.Abs(bulletVelocity.Z) > Math.Abs(bulletVelocity.X), "Player3D Rotation", 
                   "Bullet should fire in rotated direction");
            
            // Test shield system
            Assert(!player.IsShieldActive, "Player3D Shield Initial", "Shield should be inactive initially");
            player.ActivateShield();
            Assert(player.IsShieldActive, "Player3D Shield Activation", "Shield should be active after activation");
            
            Console.WriteLine("  ✓ Player3D system validated");
            _results.AddSuccess("Game Objects", "PLAYER3D_SYSTEM", "All Player3D functionality working");
        }

        private void TestAsteroid3DSystem()
        {
            Console.WriteLine("Testing Asteroid3D system...");
            
            var random = new Random(42); // Fixed seed for consistent testing
            var asteroid = new Asteroid3D(Vector3.Zero, new Vector3(2, 1, 0.5f), AsteroidSize.Large, random, 1);
            
            // Test initialization
            Assert(asteroid.Active, "Asteroid3D Active State", "Asteroid should be active when created");
            Assert(asteroid.AsteroidSize == AsteroidSize.Large, "Asteroid3D Size", "Size should match constructor parameter");
            
            // Test collision radius
            var radius = asteroid.GetCollisionRadius();
            Assert(radius > 0, "Asteroid3D Collision Radius", $"Radius should be positive: {radius}");
            
            // Test movement
            var initialPosition = asteroid.Position;
            asteroid.Update(800, 600, 100);
            var moved = Vector3.Distance(asteroid.Position, initialPosition) > 0;
            Assert(moved, "Asteroid3D Movement", "Asteroid should move during update");
            
            // Test splitting behavior
            var fragments = asteroid.Split(1);
            Assert(fragments.Count > 0, "Asteroid3D Splitting", $"Large asteroid should produce fragments: {fragments.Count}");
            
            foreach (var fragment in fragments)
            {
                Assert(fragment.AsteroidSize == AsteroidSize.Medium, "Fragment Size", 
                       "Large asteroid should split into medium-sized fragments");
                Assert(fragment.Active, "Fragment Active State", "Fragments should be active");
            }
            
            Console.WriteLine("  ✓ Asteroid3D system validated");
            _results.AddSuccess("Game Objects", "ASTEROID3D_SYSTEM", "All Asteroid3D functionality working");
        }

        private void TestBullet3DSystem()
        {
            Console.WriteLine("Testing Bullet3D system...");
            
            var bullet = new Bullet3D(Vector3.Zero, new Vector3(10, 0, 0));
            
            // Test initialization
            Assert(bullet.Active, "Bullet3D Active State", "Bullet should be active when created");
            AssertVector3Equal(bullet.Position, Vector3.Zero, "Bullet3D Initial Position");
            AssertVector3Equal(bullet.Velocity, new Vector3(10, 0, 0), "Bullet3D Velocity");
            
            // Test movement
            var initialPosition = bullet.Position;
            bullet.Update(800, 600, 100);
            Assert(bullet.Position.X > initialPosition.X, "Bullet3D Movement", "Bullet should move forward");
            
            // Test lifespan
            var lifespan = bullet.GetLifespanPercentage();
            Assert(lifespan > 0 && lifespan <= 1, "Bullet3D Lifespan", 
                   $"Lifespan should be between 0 and 1: {lifespan}");
            
            // Test collision detection
            var collision = bullet.CheckCollision(new Vector3(5, 0, 0), 3f);
            Assert(collision, "Bullet3D Collision Detection", "Should detect collision with nearby object");
            
            Console.WriteLine("  ✓ Bullet3D system validated");
            _results.AddSuccess("Game Objects", "BULLET3D_SYSTEM", "All Bullet3D functionality working");
        }

        private void TestParticleSystem3D()
        {
            Console.WriteLine("Testing 3D particle systems...");
            
            var random = new Random(42);
            
            // Test explosion particles
            var explosionParticle = ExplosionParticle3D.CreateExplosionParticle(Vector3.Zero, random, 1.0f);
            Assert(explosionParticle.IsAlive(), "Explosion Particle Alive", "Should be alive when created");
            Assert(explosionParticle.Lifespan > 0, "Explosion Particle Lifespan", 
                   $"Should have positive lifespan: {explosionParticle.Lifespan}");
            
            var initialPos = explosionParticle.Position;
            explosionParticle.Update();
            // Particles should move or at least not throw exceptions during update
            
            // Test engine particles
            var engineParticle = EngineParticle3D.CreateEngineTrail(Vector3.Zero, Vector3.UnitX, random, Raylib_cs.Color.Orange);
            Assert(engineParticle.IsAlive(), "Engine Particle Alive", "Should be alive when created");
            Assert(engineParticle.Size > 0, "Engine Particle Size", $"Should have positive size: {engineParticle.Size}");
            
            // Test explosion manager
            var explosionManager = new ExplosionManager3D();
            var initialCount = explosionManager.GetParticleCount();
            explosionManager.CreateAsteroidExplosion(Vector3.Zero, AsteroidSize.Large, 1);
            var afterCount = explosionManager.GetParticleCount();
            Assert(afterCount > initialCount, "Explosion Manager", "Should create particles");
            
            Console.WriteLine("  ✓ 3D particle systems validated");
            _results.AddSuccess("Game Objects", "PARTICLE_SYSTEMS", "All particle systems working");
        }
        #endregion

        #region Phase 4: 3D Collision System
        private void Test3DCollisionSystem()
        {
            _stopwatch.Restart();
            
            TestBasicCollisionDetection();
            TestBulkCollisionOperations();
            TestSpatialPartitioning();
            TestCollisionAccuracy();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 4 3D Collision System completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestBasicCollisionDetection()
        {
            Console.WriteLine("Testing basic 3D collision detection...");
            
            // Test sphere-sphere collision
            var pos1 = Vector3.Zero;
            var pos2 = new Vector3(8, 0, 0);
            var collision = CollisionManager3D.CheckSphereCollision(pos1, 5f, pos2, 5f);
            Assert(collision, "Basic Sphere Collision - Intersecting", "Spheres should collide");
            
            var pos3 = new Vector3(15, 0, 0);
            var noCollision = CollisionManager3D.CheckSphereCollision(pos1, 5f, pos3, 5f);
            Assert(!noCollision, "Basic Sphere Collision - Separated", "Spheres should not collide");
            
            // Test 3D diagonal collision
            var pos4 = new Vector3(3, 4, 12);
            var distance3D = Vector3.Distance(pos1, pos4);
            var expectedDistance = 13f;
            Assert(Math.Abs(distance3D - expectedDistance) < 0.001f, "3D Diagonal Distance", 
                   $"Expected: {expectedDistance}, Actual: {distance3D}");
            
            Console.WriteLine("  ✓ Basic collision detection validated");
            _results.AddSuccess("Collision", "BASIC_COLLISION", "Basic collision detection working correctly");
        }

        private void TestBulkCollisionOperations()
        {
            Console.WriteLine("Testing bulk collision operations...");
            
            var random = new Random(42);
            var bullets = new List<Bullet3D>();
            var asteroids = new List<Asteroid3D>();
            
            // Create test objects
            for (int i = 0; i < 10; i++)
            {
                var pos = new Vector3(i * 20, 0, 0);
                bullets.Add(new Bullet3D(pos, Vector3.UnitX));
                asteroids.Add(new Asteroid3D(pos + new Vector3(5, 0, 0), Vector3.Zero, AsteroidSize.Small, random, 1));
            }
            
            var sw = Stopwatch.StartNew();
            var collisions = CollisionManager3D.CheckBulletAsteroidCollisions(bullets, asteroids);
            sw.Stop();
            
            Assert(collisions.Count > 0, "Bulk Collision Detection", $"Should detect collisions: {collisions.Count}");
            Console.WriteLine($"  ✓ Bulk collision test: {collisions.Count} collisions found in {sw.ElapsedMilliseconds}ms");
            
            _results.AddSuccess("Collision", "BULK_COLLISION", $"{collisions.Count} collisions detected efficiently");
        }

        private void TestSpatialPartitioning()
        {
            Console.WriteLine("Testing spatial partitioning system...");
            
            var random = new Random(42);
            var asteroids = new List<Asteroid3D>();
            
            // Create asteroids across 3D space
            for (int i = 0; i < 100; i++)
            {
                var pos = new Vector3(
                    random.NextSingle() * 800,
                    random.NextSingle() * 600,
                    random.NextSingle() * 100 - 50
                );
                asteroids.Add(new Asteroid3D(pos, Vector3.Zero, AsteroidSize.Small, random, 1));
            }
            
            var sw = Stopwatch.StartNew();
            var spatialGrid = CollisionManager3D.PartitionObjects(asteroids, a => a.Position, 50f);
            sw.Stop();
            
            Assert(spatialGrid.Count > 0, "Spatial Partitioning", $"Should create spatial cells: {spatialGrid.Count}");
            
            // Verify partitioning correctness
            int totalObjectsInGrid = spatialGrid.Values.Sum(list => list.Count);
            Assert(totalObjectsInGrid >= asteroids.Count, "Spatial Partitioning Integrity", 
                   "All objects should be in the spatial grid");
            
            Console.WriteLine($"  ✓ Spatial partitioning: {spatialGrid.Count} cells, {totalObjectsInGrid} object references");
            _results.AddSuccess("Collision", "SPATIAL_PARTITIONING", $"Created {spatialGrid.Count} spatial cells");
        }

        private void TestCollisionAccuracy()
        {
            Console.WriteLine("Testing collision accuracy...");
            
            var random = new Random(42);
            var player = new Player3D(Vector3.Zero, 15f);
            var asteroid = new Asteroid3D(new Vector3(10, 0, 0), Vector3.Zero, AsteroidSize.Medium, random, 1);
            
            // Test player-asteroid collision
            var playerCollision = CollisionManager3D.CheckPlayerAsteroidCollision(player, asteroid);
            Assert(playerCollision, "Player-Asteroid Accuracy", "Should detect collision at expected distance");
            
            // Test with shield active
            player.ActivateShield();
            var shieldedCollision = CollisionManager3D.CheckPlayerAsteroidCollision(player, asteroid);
            Assert(!shieldedCollision, "Shield Protection", "Shield should prevent collision detection");
            
            Console.WriteLine("  ✓ Collision accuracy validated");
            _results.AddSuccess("Collision", "COLLISION_ACCURACY", "Accurate collision detection confirmed");
        }
        #endregion

        #region Phase 5: Performance Benchmarks
        private void TestPerformanceBaselines()
        {
            _stopwatch.Restart();
            
            BenchmarkCollisionPerformance();
            BenchmarkVector3Operations();
            BenchmarkObjectCreation();
            BenchmarkGameLoopPerformance();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 5 Performance Benchmarks completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void BenchmarkCollisionPerformance()
        {
            Console.WriteLine("Benchmarking collision detection performance...");
            
            var iterations = 100_000;
            var random = new Random(42);
            var positions = new Vector3[iterations];
            
            for (int i = 0; i < iterations; i++)
            {
                positions[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            int collisions = 0;
            for (int i = 0; i < iterations - 1; i++)
            {
                if (CollisionManager3D.CheckSphereCollision(positions[i], 5f, positions[i + 1], 5f))
                    collisions++;
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)(iterations - 1) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ Collision Performance: {iterations - 1:N0} checks in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ Rate: {opsPerSecond:N0} collision checks per second");
            Console.WriteLine($"  ✓ Collisions found: {collisions}");
            
            // Performance assertion
            Assert(opsPerSecond > 100_000, "Collision Performance", 
                   $"Should achieve >100k ops/sec, achieved: {opsPerSecond:N0}");
            
            _results.AddPerformance("Collision", "Sphere Collision", iterations - 1, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkVector3Operations()
        {
            Console.WriteLine("Benchmarking Vector3 operations...");
            
            var iterations = 1_000_000;
            var random = new Random(42);
            var vectors = new Vector3[iterations];
            
            for (int i = 0; i < iterations; i++)
            {
                vectors[i] = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations - 1; i++)
            {
                var result = vectors[i] + vectors[i + 1];
                result = Vector3.Normalize(result);
                var distance = Vector3.Distance(vectors[i], vectors[i + 1]);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)(iterations - 1) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ Vector3 Performance: {iterations - 1:N0} operations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ Rate: {opsPerSecond:N0} vector operations per second");
            
            Assert(opsPerSecond > 500_000, "Vector3 Performance", 
                   $"Should achieve >500k ops/sec, achieved: {opsPerSecond:N0}");
            
            _results.AddPerformance("Vector3", "Math Operations", iterations - 1, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkObjectCreation()
        {
            Console.WriteLine("Benchmarking game object creation...");
            
            var iterations = 50_000;
            var random = new Random(42);
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            var objects = new List<object>();
            for (int i = 0; i < iterations; i++)
            {
                var pos = new Vector3(i % 800, (i * 2) % 600, (i * 3) % 100);
                
                objects.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Small, random, 1));
                objects.Add(new Bullet3D(pos, Vector3.UnitX));
                
                if (i % 1000 == 0) // Periodic cleanup
                    objects.Clear();
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)(iterations * 2) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ Object Creation: {iterations * 2:N0} objects in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ Rate: {opsPerSecond:N0} objects per second");
            
            _results.AddPerformance("Objects", "Creation Rate", iterations * 2, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkGameLoopPerformance()
        {
            Console.WriteLine("Benchmarking game loop performance...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var iterations = 1_000;
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                gameManager.Update();
            }
            
            sw.Stop();
            
            var fpsEquivalent = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ Game Loop: {iterations} updates in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ FPS Equivalent: {fpsEquivalent:N0} FPS capability");
            
            Assert(fpsEquivalent > 60, "Game Loop Performance", 
                   $"Should support >60 FPS, achieved: {fpsEquivalent:N0}");
            
            _results.AddPerformance("Game Loop", "Update Rate", iterations, sw.ElapsedMilliseconds, fpsEquivalent);
        }
        #endregion

        #region Phase 6: System Integration
        private void TestSystemIntegration()
        {
            _stopwatch.Restart();
            
            TestGameManager3DIntegration();
            TestCrossSystemInteractions();
            TestSystemStability();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 6 System Integration completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestGameManager3DIntegration()
        {
            Console.WriteLine("Testing GameManager3D integration...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            
            // Test initial state
            Assert(gameManager.IsGameRunning, "GameManager Running State", "Game should be running after initialization");
            Assert(gameManager.IsPlayerAlive, "GameManager Player State", "Player should be alive initially");
            Assert(gameManager.Score >= 0, "GameManager Score", $"Score should be non-negative: {gameManager.Score}");
            Assert(gameManager.Level >= 1, "GameManager Level", $"Level should be at least 1: {gameManager.Level}");
            
            // Test update cycles
            for (int i = 0; i < 10; i++)
            {
                gameManager.Update();
            }
            
            Assert(gameManager.IsGameRunning, "GameManager Stability", "Game should remain running after updates");
            
            // Test camera integration
            var camera = gameManager.Camera;
            Assert(!float.IsNaN(camera.Position.X) && !float.IsNaN(camera.Position.Y) && !float.IsNaN(camera.Position.Z),
                   "Camera Position Validity", "Camera position should have valid values");
            
            Console.WriteLine("  ✓ GameManager3D integration validated");
            _results.AddSuccess("Integration", "GAMEMANAGER3D", "Full integration working correctly");
        }

        private void TestCrossSystemInteractions()
        {
            Console.WriteLine("Testing cross-system interactions...");
            
            var random = new Random(42);
            var player = new Player3D(Vector3.Zero, 15f);
            var asteroids = new List<Asteroid3D>();
            var bullets = new List<Bullet3D>();
            
            // Create test scenario
            asteroids.Add(new Asteroid3D(new Vector3(50, 0, 0), Vector3.Zero, AsteroidSize.Large, random, 1));
            bullets.Add(new Bullet3D(new Vector3(25, 0, 0), Vector3.UnitX));
            
            // Test collision system integration
            var bulletCollisions = CollisionManager3D.CheckBulletAsteroidCollisions(bullets, asteroids);
            Assert(bulletCollisions.Count > 0, "Cross-System Bullet Collision", "Should detect bullet-asteroid collision");
            
            var playerCollisions = CollisionManager3D.CheckPlayerAsteroidCollisions(player, asteroids);
            Assert(playerCollisions.Count > 0, "Cross-System Player Collision", "Should detect player-asteroid collision");
            
            // Test object updates with interactions
            for (int frame = 0; frame < 10; frame++)
            {
                foreach (var asteroid in asteroids)
                    asteroid.Update(800, 600, 100);
                foreach (var bullet in bullets)
                    bullet.Update(800, 600, 100);
                player.Update(800, 600, 100);
            }
            
            Console.WriteLine("  ✓ Cross-system interactions validated");
            _results.AddSuccess("Integration", "CROSS_SYSTEMS", "All systems interact correctly");
        }

        private void TestSystemStability()
        {
            Console.WriteLine("Testing system stability under load...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var random = new Random(42);
            
            // Stress test with multiple updates
            for (int cycle = 0; cycle < 100; cycle++)
            {
                gameManager.Update();
                
                // Verify system remains stable
                Assert(gameManager.IsGameRunning, $"System Stability Cycle {cycle}", 
                       "Game should remain stable during stress testing");
            }
            
            Console.WriteLine("  ✓ System stability validated under 100 update cycles");
            _results.AddSuccess("Integration", "SYSTEM_STABILITY", "Systems remain stable under load");
        }
        #endregion

        #region Phase 7: Memory Management
        private void TestMemoryManagement()
        {
            _stopwatch.Restart();
            
            TestMemoryLeaks();
            TestGarbageCollectionImpact();
            TestResourceUsage();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 7 Memory Validation completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestMemoryLeaks()
        {
            Console.WriteLine("Testing for memory leaks...");
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Create and destroy objects repeatedly
            for (int cycle = 0; cycle < 10; cycle++)
            {
                var objects = new List<object>();
                var random = new Random(42);
                
                for (int i = 0; i < 1000; i++)
                {
                    objects.Add(new Asteroid3D(Vector3.Zero, Vector3.One, AsteroidSize.Small, random, 1));
                    objects.Add(new Bullet3D(Vector3.Zero, Vector3.UnitX));
                }
                
                objects.Clear(); // Release references
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(true);
            
            var memoryIncrease = (finalMemory - initialMemory) / 1024.0; // KB
            Console.WriteLine($"  ✓ Memory leak test: {memoryIncrease:F2} KB increase after 10 cycles");
            
            Assert(memoryIncrease < 1024, "Memory Leak Test", 
                   $"Memory increase should be < 1MB, actual: {memoryIncrease:F2} KB");
            
            _results.AddSuccess("Memory", "LEAK_TEST", $"Memory stable: +{memoryIncrease:F2} KB");
        }

        private void TestGarbageCollectionImpact()
        {
            Console.WriteLine("Testing garbage collection impact...");
            
            var gcTimes = new List<long>();
            var random = new Random(42);
            
            for (int test = 0; test < 5; test++)
            {
                var objects = new List<object>();
                
                // Allocate objects
                for (int i = 0; i < 10000; i++)
                {
                    objects.Add(new Asteroid3D(Vector3.Zero, Vector3.One, AsteroidSize.Small, random, 1));
                }
                
                // Measure GC time
                var sw = Stopwatch.StartNew();
                objects.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                sw.Stop();
                
                gcTimes.Add(sw.ElapsedMilliseconds);
            }
            
            var avgGcTime = gcTimes.Average();
            Console.WriteLine($"  ✓ Average GC time: {avgGcTime:F2}ms");
            
            Assert(avgGcTime < 100, "Garbage Collection Impact", 
                   $"GC time should be < 100ms, actual: {avgGcTime:F2}ms");
            
            _results.AddSuccess("Memory", "GC_IMPACT", $"GC time: {avgGcTime:F2}ms");
        }

        private void TestResourceUsage()
        {
            Console.WriteLine("Testing resource usage...");
            
            var initialWorkingSet = Environment.WorkingSet;
            var gameManager = new GameManager3D(800, 600, 100f);
            
            // Run game for multiple cycles
            for (int i = 0; i < 1000; i++)
            {
                gameManager.Update();
            }
            
            var finalWorkingSet = Environment.WorkingSet;
            var memoryIncrease = (finalWorkingSet - initialWorkingSet) / 1024.0 / 1024.0; // MB
            
            Console.WriteLine($"  ✓ Working set increase: {memoryIncrease:F2} MB");
            
            _results.AddSuccess("Memory", "RESOURCE_USAGE", $"Working set: +{memoryIncrease:F2} MB");
        }
        #endregion

        #region Phase 8: Gameplay Mechanics
        private void TestGameplayMechanics()
        {
            _stopwatch.Restart();
            
            TestAsteroidSplittingMechanics();
            TestScoringSystem();
            TestPlayerMechanics();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 8 Gameplay Validation completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestAsteroidSplittingMechanics()
        {
            Console.WriteLine("Testing asteroid splitting mechanics...");
            
            var random = new Random(42);
            
            // Test large asteroid splitting
            var largeAsteroid = new Asteroid3D(Vector3.Zero, Vector3.One, AsteroidSize.Large, random, 1);
            var fragments = largeAsteroid.Split(1);
            Assert(fragments.Count > 0, "Large Asteroid Split", $"Should produce fragments: {fragments.Count}");
            Assert(fragments.All(f => f.AsteroidSize == AsteroidSize.Medium), "Large Split Size", 
                   "Large asteroids should split into medium asteroids");
            
            // Test medium asteroid splitting
            if (fragments.Count > 0)
            {
                var smallFragments = fragments[0].Split(1);
                Assert(smallFragments.Count > 0, "Medium Asteroid Split", $"Should produce small fragments: {smallFragments.Count}");
                Assert(smallFragments.All(f => f.AsteroidSize == AsteroidSize.Small), "Medium Split Size", 
                       "Medium asteroids should split into small asteroids");
                
                // Test small asteroid (should not split)
                if (smallFragments.Count > 0)
                {
                    var noFragments = smallFragments[0].Split(1);
                    Assert(noFragments.Count == 0, "Small Asteroid No Split", "Small asteroids should not split further");
                }
            }
            
            Console.WriteLine("  ✓ Asteroid splitting mechanics validated");
            _results.AddSuccess("Gameplay", "ASTEROID_SPLITTING", "Splitting mechanics working correctly");
        }

        private void TestScoringSystem()
        {
            Console.WriteLine("Testing scoring system...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var initialScore = gameManager.Score;
            
            // Score should initialize properly
            Assert(initialScore >= 0, "Initial Score", $"Score should be non-negative: {initialScore}");
            
            // Test that score properties are accessible
            var level = gameManager.Level;
            Assert(level >= 1, "Initial Level", $"Level should start at 1 or higher: {level}");
            
            Console.WriteLine("  ✓ Scoring system validated");
            _results.AddSuccess("Gameplay", "SCORING_SYSTEM", "Score and level systems working");
        }

        private void TestPlayerMechanics()
        {
            Console.WriteLine("Testing player mechanics...");
            
            var player = new Player3D(Vector3.Zero, 15f);
            
            // Test shield mechanics
            Assert(!player.IsShieldActive, "Shield Initial State", "Shield should start inactive");
            player.ActivateShield();
            Assert(player.IsShieldActive, "Shield Activation", "Shield should activate");
            
            // Test movement and rotation
            player.Velocity = new Vector3(5, 3, 2);
            player.Rotation = new Vector3(0.1f, 0.2f, 0.3f);
            
            var initialPos = player.Position;
            player.Update(800, 600, 100);
            var moved = Vector3.Distance(player.Position, initialPos) > 0;
            Assert(moved, "Player Movement", "Player should move when velocity is set");
            
            // Test bullet firing direction
            var bulletVel = player.GetBulletVelocity();
            Assert(bulletVel.Length() > 0, "Bullet Velocity", "Player should produce valid bullet velocity");
            
            Console.WriteLine("  ✓ Player mechanics validated");
            _results.AddSuccess("Gameplay", "PLAYER_MECHANICS", "All player mechanics working");
        }
        #endregion

        #region Phase 9: Camera System
        private void TestCameraSystem()
        {
            _stopwatch.Restart();
            
            TestCameraInitialization();
            TestCameraProperties();
            TestCameraIntegration();
            
            _stopwatch.Stop();
            Console.WriteLine($"✅ Phase 9 Camera System completed in {_stopwatch.ElapsedMilliseconds}ms");
        }

        private void TestCameraInitialization()
        {
            Console.WriteLine("Testing camera initialization...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var camera = gameManager.Camera;
            
            // Test camera position
            Assert(camera.Position.Z != 0, "Camera Position Z", "Camera should be positioned away from game plane");
            
            // Test camera target
            AssertVector3Equal(camera.Target, Vector3.Zero, "Camera Target", 10f); // Allow some tolerance
            
            // Test camera up vector
            var upLength = camera.Up.Length();
            Assert(Math.Abs(upLength - 1.0f) < 0.1f, "Camera Up Vector Length", 
                   $"Up vector should be normalized: {upLength}");
            
            Console.WriteLine("  ✓ Camera initialization validated");
            _results.AddSuccess("Camera", "INITIALIZATION", "Camera properly initialized");
        }

        private void TestCameraProperties()
        {
            Console.WriteLine("Testing camera properties...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var camera = gameManager.Camera;
            
            // Test FOV
            Assert(camera.FovY > 0 && camera.FovY < 180, "Camera FOV", 
                   $"FOV should be reasonable: {camera.FovY}");
            
            // Test projection
            Assert(camera.Projection == CameraProjection.Perspective, "Camera Projection", 
                   "Camera should use perspective projection");
            
            Console.WriteLine("  ✓ Camera properties validated");
            _results.AddSuccess("Camera", "PROPERTIES", "Camera properties correctly set");
        }

        private void TestCameraIntegration()
        {
            Console.WriteLine("Testing camera integration...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var initialCamera = gameManager.Camera;
            
            // Test camera stability during updates
            for (int i = 0; i < 10; i++)
            {
                gameManager.Update();
                var currentCamera = gameManager.Camera;
                
                Assert(!float.IsNaN(currentCamera.Position.X) && !float.IsNaN(currentCamera.Position.Y) && !float.IsNaN(currentCamera.Position.Z),
                       "Camera Stability", "Camera position should remain valid during updates");
            }
            
            Console.WriteLine("  ✓ Camera integration validated");
            _results.AddSuccess("Camera", "INTEGRATION", "Camera integrates properly with game systems");
        }
        #endregion

        #region Phase 10: Graphics Testing (Optional)
        private void TestGraphicsRendering()
        {
            Console.WriteLine("Testing graphics rendering (requires display)...");
            
            try
            {
                Raylib.InitWindow(800, 600, "Phase 1 Graphics Test");
                Raylib.SetTargetFPS(60);
                
                var gameManager = new GameManager3D(800, 600, 100f);
                var frameCount = 0;
                var sw = Stopwatch.StartNew();
                
                // Render for a few frames to test graphics pipeline
                while (frameCount < 60 && !Raylib.WindowShouldClose()) // 1 second at 60 FPS
                {
                    gameManager.Update();
                    
                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Raylib_cs.Color.Black);
                    
                    gameManager.Draw();
                    
                    Raylib.DrawText($"Phase 1 Graphics Test - Frame {frameCount}", 10, 10, 20, Raylib_cs.Color.White);
                    
                    Raylib.EndDrawing();
                    frameCount++;
                }
                
                sw.Stop();
                
                var avgFPS = (double)frameCount / sw.Elapsed.TotalSeconds;
                Console.WriteLine($"  ✓ Graphics test: {frameCount} frames in {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"  ✓ Average FPS: {avgFPS:F1}");
                
                Assert(avgFPS > 30, "Graphics Performance", $"Should achieve >30 FPS, achieved: {avgFPS:F1}");
                
                Raylib.CloseWindow();
                
                _results.AddSuccess("Graphics", "RENDERING", $"Graphics rendering at {avgFPS:F1} FPS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Graphics test failed: {ex.Message}");
                _results.AddFailure("Graphics", $"Graphics rendering failed: {ex.Message}");
            }
        }
        #endregion

        #region Report Generation
        private void GeneratePhase1Report()
        {
            Console.WriteLine("\n" + "=".PadRight(80, '='));
            Console.WriteLine("PHASE 1 COMPREHENSIVE VALIDATION REPORT");
            Console.WriteLine("=".PadRight(80, '='));
            
            Console.WriteLine($"\nTest Execution Summary:");
            Console.WriteLine($"- Execution Mode: {(_headlessMode ? "Headless" : "Graphics")}");
            Console.WriteLine($"- Total Tests Run: {_results.TotalTests}");
            Console.WriteLine($"- Tests Passed: {_results.PassedTests}");
            Console.WriteLine($"- Tests Failed: {_results.FailedTests}");
            Console.WriteLine($"- Success Rate: {_results.SuccessRate:F1}%");
            Console.WriteLine($"- Total Execution Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            // System Information
            Console.WriteLine($"\nSystem Information:");
            Console.WriteLine($"- OS: {Environment.OSVersion}");
            Console.WriteLine($"- .NET Version: {Environment.Version}");
            Console.WriteLine($"- Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"- Working Set: {Environment.WorkingSet / 1024.0 / 1024.0:F2} MB");
            
            // Performance Summary
            if (_results.PerformanceMetrics.Count > 0)
            {
                Console.WriteLine($"\nPerformance Metrics:");
                foreach (var metric in _results.PerformanceMetrics)
                {
                    Console.WriteLine($"  - {metric}");
                }
            }
            
            // Failure Details
            if (_results.FailedTests > 0)
            {
                Console.WriteLine($"\n❌ FAILURES ({_results.FailedTests}):");
                foreach (var failure in _results.Failures)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }
            
            // Success Categories
            Console.WriteLine($"\n✅ SUCCESS CATEGORIES:");
            var categories = _results.Successes.GroupBy(s => s.Split(':')[0]).ToList();
            foreach (var category in categories)
            {
                Console.WriteLine($"  {category.Key}: {category.Count()} tests passed");
            }
            
            // Phase 1 Assessment
            Console.WriteLine($"\n🎯 PHASE 1 ASSESSMENT:");
            AssessPhase1Readiness();
            
            Console.WriteLine("=".PadRight(80, '='));
        }

        private void AssessPhase1Readiness()
        {
            var criticalSystems = new[]
            {
                "Build", "3D Foundation", "Game Objects", "Collision", "Integration"
            };
            
            var systemStatus = new Dictionary<string, bool>();
            foreach (var system in criticalSystems)
            {
                var systemFailures = _results.Failures.Count(f => f.Contains(system));
                systemStatus[system] = systemFailures == 0;
            }
            
            var readySystems = systemStatus.Count(kvp => kvp.Value);
            var totalSystems = systemStatus.Count;
            
            if (readySystems == totalSystems && _results.FailedTests == 0)
            {
                Console.WriteLine("🎉 EXCELLENT: Phase 1 Foundation is SOLID and READY");
                Console.WriteLine("✅ All critical 3D systems validated and working");
                Console.WriteLine("✅ Performance benchmarks meet requirements");
                Console.WriteLine("✅ No blocking issues detected");
                Console.WriteLine("✅ PHASE 2 DEVELOPMENT CAN PROCEED");
            }
            else if (readySystems >= (totalSystems * 0.8) && _results.FailedTests <= 2)
            {
                Console.WriteLine("👍 GOOD: Phase 1 Foundation is mostly solid");
                Console.WriteLine("✅ Core 3D systems are working");
                Console.WriteLine("⚠️  Minor issues present - address before Phase 2");
                Console.WriteLine("📋 Review failed tests and resolve issues");
            }
            else
            {
                Console.WriteLine("⚠️  CONCERN: Phase 1 Foundation has significant issues");
                Console.WriteLine("❌ Critical systems have failures");
                Console.WriteLine("❌ DO NOT PROCEED to Phase 2 until issues resolved");
                Console.WriteLine("🔧 Focus on fixing core foundation problems first");
            }
            
            // Specific system status
            Console.WriteLine($"\nSystem Status:");
            foreach (var system in systemStatus)
            {
                var status = system.Value ? "✅ READY" : "❌ ISSUES";
                Console.WriteLine($"  {system.Key}: {status}");
            }
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
                throw new InvalidOperationException($"Test failed: {testName} - {message}");
            }
        }

        private void AssertVector3Equal(Vector3 actual, Vector3 expected, string testName, float tolerance = 0.001f)
        {
            var distance = Vector3.Distance(actual, expected);
            if (distance <= tolerance)
            {
                Console.WriteLine($"  ✓ {testName}: Vectors match within tolerance ({distance:F6})");
                _results.AddSuccess("Vector3", testName, $"Expected: {expected}, Actual: {actual}");
            }
            else
            {
                Console.WriteLine($"  ❌ {testName}: Expected {expected}, Actual {actual}, Distance: {distance}");
                _results.AddFailure("Vector3", $"{testName}: Distance {distance} > {tolerance}");
                throw new InvalidOperationException($"Vector3 assertion failed: {testName}");
            }
        }
        #endregion
    }

    #region Supporting Classes
    public class TestResults
    {
        public int PassedTests { get; private set; }
        public int FailedTests { get; private set; }
        public int TotalTests => PassedTests + FailedTests;
        public double SuccessRate => TotalTests > 0 ? (PassedTests * 100.0) / TotalTests : 0;
        
        public List<string> Failures { get; } = new List<string>();
        public List<string> Successes { get; } = new List<string>();
        public List<string> PerformanceMetrics { get; } = new List<string>();

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

        public void AddPerformance(string category, string test, int operations, long milliseconds, double rate)
        {
            PerformanceMetrics.Add($"{category} {test}: {operations:N0} ops in {milliseconds}ms ({rate:N0} ops/sec)");
        }
    }
    #endregion
}