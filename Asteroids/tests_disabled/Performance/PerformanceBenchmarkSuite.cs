using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Linq;
using Raylib_cs;

namespace Asteroids.Tests.Performance
{
    /// <summary>
    /// Comprehensive performance benchmarking suite for 3D vs 2D comparison
    /// Tests rendering, collision detection, physics, and memory performance
    /// </summary>
    public class PerformanceBenchmarkSuite
    {
        private BenchmarkResults _results = new BenchmarkResults();
        
        public static void Main(string[] args)
        {
            var benchmark = new PerformanceBenchmarkSuite();
            benchmark.RunBenchmarks();
        }

        public void RunBenchmarks()
        {
            Console.WriteLine("=== ASTEROIDS 3D PERFORMANCE BENCHMARK SUITE ===");
            Console.WriteLine($"Benchmark Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.OSVersion}, .NET {Environment.Version}");
            Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine();

            try
            {
                // Warm up
                Console.WriteLine("Warming up systems...");
                WarmUp();
                
                // Core performance tests
                BenchmarkCollisionDetection();
                BenchmarkVector3Operations();
                BenchmarkObjectCreation();
                BenchmarkGameLoop();
                BenchmarkParticleSystem();
                BenchmarkMemoryAllocation();
                
                GenerateBenchmarkReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ BENCHMARK FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void WarmUp()
        {
            // Warm up GC and JIT
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            var random = new Random(42);
            for (int i = 0; i < 1000; i++)
            {
                var pos = new Vector3(i, i * 2, i * 3);
                var asteroid = new Asteroid3D(pos, Vector3.One, AsteroidSize.Small, random, 1);
                CollisionManager3D.CheckSphereCollision(pos, 10f, Vector3.Zero, 15f);
            }
            
            GC.Collect();
            Console.WriteLine("✓ Warm up completed\n");
        }

        #region Collision Detection Benchmarks
        private void BenchmarkCollisionDetection()
        {
            Console.WriteLine("=== COLLISION DETECTION BENCHMARKS ===");
            
            // Test 1: Basic sphere collision performance
            BenchmarkBasicSphereCollision();
            
            // Test 2: Bulk collision detection
            BenchmarkBulkCollisionDetection();
            
            // Test 3: Spatial partitioning performance
            BenchmarkSpatialPartitioning();
            
            Console.WriteLine();
        }

        private void BenchmarkBasicSphereCollision()
        {
            Console.WriteLine("Benchmarking basic sphere collision detection...");
            
            var iterations = 1_000_000;
            var random = new Random(42);
            var positions1 = new Vector3[iterations];
            var positions2 = new Vector3[iterations];
            
            // Generate test data
            for (int i = 0; i < iterations; i++)
            {
                positions1[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
                positions2[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            int collisions = 0;
            for (int i = 0; i < iterations; i++)
            {
                if (CollisionManager3D.CheckSphereCollision(positions1[i], 10f, positions2[i], 10f))
                    collisions++;
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} collision checks in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            Console.WriteLine($"  ✓ {collisions:N0} collisions detected");
            
            _results.AddBenchmark("Collision", "Basic Sphere", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkBulkCollisionDetection()
        {
            Console.WriteLine("Benchmarking bulk collision detection...");
            
            var random = new Random(42);
            var bulletCount = 100;
            var asteroidCount = 50;
            var iterations = 1000;
            
            // Create test objects
            var bullets = new List<Bullet3D>();
            var asteroids = new List<Asteroid3D>();
            
            for (int i = 0; i < bulletCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                bullets.Add(new Bullet3D(pos, Vector3.UnitX));
            }
            
            for (int i = 0; i < asteroidCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                asteroids.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Medium, random, 1));
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            int totalCollisions = 0;
            for (int i = 0; i < iterations; i++)
            {
                var collisions = CollisionManager3D.CheckBulletAsteroidCollisions(bullets, asteroids);
                totalCollisions += collisions.Count;
            }
            
            sw.Stop();
            
            var totalChecks = (long)bulletCount * asteroidCount * iterations;
            var opsPerSecond = (double)totalChecks / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {totalChecks:N0} collision checks in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            Console.WriteLine($"  ✓ {totalCollisions:N0} total collisions detected");
            
            _results.AddBenchmark("Collision", "Bulk Detection", (int)totalChecks, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkSpatialPartitioning()
        {
            Console.WriteLine("Benchmarking spatial partitioning...");
            
            var random = new Random(42);
            var objectCount = 1000;
            var iterations = 100;
            
            var asteroids = new List<Asteroid3D>();
            for (int i = 0; i < objectCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                asteroids.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Small, random, 1));
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var spatialGrid = CollisionManager3D.PartitionObjects(asteroids, a => a.Position, 50f);
            }
            
            sw.Stop();
            
            var totalOperations = objectCount * iterations;
            var opsPerSecond = (double)totalOperations / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {totalOperations:N0} partitioning operations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            
            _results.AddBenchmark("Collision", "Spatial Partitioning", totalOperations, sw.ElapsedMilliseconds, opsPerSecond);
        }
        #endregion

        #region Vector3 Operations Benchmarks
        private void BenchmarkVector3Operations()
        {
            Console.WriteLine("=== VECTOR3 OPERATIONS BENCHMARKS ===");
            
            BenchmarkVector3Math();
            BenchmarkVector3Distance();
            BenchmarkMatrixTransformations();
            
            Console.WriteLine();
        }

        private void BenchmarkVector3Math()
        {
            Console.WriteLine("Benchmarking Vector3 math operations...");
            
            var iterations = 10_000_000;
            var random = new Random(42);
            var vectors1 = new Vector3[iterations];
            var vectors2 = new Vector3[iterations];
            
            for (int i = 0; i < iterations; i++)
            {
                vectors1[i] = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                vectors2[i] = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var result = vectors1[i] + vectors2[i];
                result = result * 2.0f;
                result = Vector3.Normalize(result);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} Vector3 operations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            
            _results.AddBenchmark("Vector3", "Math Operations", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkVector3Distance()
        {
            Console.WriteLine("Benchmarking Vector3 distance calculations...");
            
            var iterations = 5_000_000;
            var random = new Random(42);
            var positions1 = new Vector3[iterations];
            var positions2 = new Vector3[iterations];
            
            for (int i = 0; i < iterations; i++)
            {
                positions1[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
                positions2[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            double totalDistance = 0;
            for (int i = 0; i < iterations; i++)
            {
                totalDistance += Vector3.Distance(positions1[i], positions2[i]);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} distance calculations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            Console.WriteLine($"  ✓ Total distance: {totalDistance:F2}");
            
            _results.AddBenchmark("Vector3", "Distance Calculation", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkMatrixTransformations()
        {
            Console.WriteLine("Benchmarking matrix transformations...");
            
            var iterations = 1_000_000;
            var random = new Random(42);
            var positions = new Vector3[iterations];
            var rotations = new Vector3[iterations];
            
            for (int i = 0; i < iterations; i++)
            {
                positions[i] = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, random.NextSingle() * 100);
                rotations[i] = new Vector3(random.NextSingle() * MathF.PI * 2, random.NextSingle() * MathF.PI * 2, random.NextSingle() * MathF.PI * 2);
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var transform = Matrix4x4.CreateFromYawPitchRoll(rotations[i].Y, rotations[i].X, rotations[i].Z) *
                               Matrix4x4.CreateTranslation(positions[i]);
                var transformed = Vector3.Transform(Vector3.UnitX, transform);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} matrix transformations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} operations per second");
            
            _results.AddBenchmark("Vector3", "Matrix Transforms", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }
        #endregion

        #region Object Creation Benchmarks
        private void BenchmarkObjectCreation()
        {
            Console.WriteLine("=== OBJECT CREATION BENCHMARKS ===");
            
            BenchmarkAsteroid3DCreation();
            BenchmarkBullet3DCreation();
            BenchmarkParticleCreation();
            
            Console.WriteLine();
        }

        private void BenchmarkAsteroid3DCreation()
        {
            Console.WriteLine("Benchmarking Asteroid3D creation...");
            
            var iterations = 100_000;
            var random = new Random(42);
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var pos = new Vector3(i % 800, (i * 2) % 600, (i * 3) % 100);
                var vel = new Vector3(random.NextSingle() * 2 - 1, random.NextSingle() * 2 - 1, random.NextSingle() * 2 - 1);
                var size = (AsteroidSize)(i % 3);
                var asteroid = new Asteroid3D(pos, vel, size, random, 1);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} Asteroid3D objects created in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} creations per second");
            
            _results.AddBenchmark("Objects", "Asteroid3D Creation", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkBullet3DCreation()
        {
            Console.WriteLine("Benchmarking Bullet3D creation...");
            
            var iterations = 500_000;
            var random = new Random(42);
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var pos = new Vector3(i % 800, (i * 2) % 600, (i * 3) % 100);
                var vel = Vector3.Normalize(new Vector3(random.NextSingle() * 2 - 1, random.NextSingle() * 2 - 1, random.NextSingle() * 2 - 1)) * 10f;
                var bullet = new Bullet3D(pos, vel);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} Bullet3D objects created in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} creations per second");
            
            _results.AddBenchmark("Objects", "Bullet3D Creation", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkParticleCreation()
        {
            Console.WriteLine("Benchmarking particle creation...");
            
            var iterations = 200_000;
            var random = new Random(42);
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                var pos = new Vector3(i % 800, (i * 2) % 600, (i * 3) % 100);
                var particle = ExplosionParticle3D.CreateExplosionParticle(pos, random, 1.0f);
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  ✓ {iterations:N0} particles created in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} creations per second");
            
            _results.AddBenchmark("Objects", "Particle Creation", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }
        #endregion

        #region Game Loop Benchmarks
        private void BenchmarkGameLoop()
        {
            Console.WriteLine("=== GAME LOOP BENCHMARKS ===");
            
            BenchmarkGameManager3DUpdate();
            BenchmarkMassiveObjectUpdate();
            
            Console.WriteLine();
        }

        private void BenchmarkGameManager3DUpdate()
        {
            Console.WriteLine("Benchmarking GameManager3D update loop...");
            
            var gameManager = new GameManager3D(800, 600, 100f);
            var iterations = 10_000;
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                gameManager.Update();
            }
            
            sw.Stop();
            
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            var fpsEquivalent = opsPerSecond; // Each update represents one frame
            
            Console.WriteLine($"  ✓ {iterations:N0} game updates in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} updates per second");
            Console.WriteLine($"  ✓ Equivalent to {fpsEquivalent:N0} FPS capability");
            
            _results.AddBenchmark("Game Loop", "GameManager3D Update", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkMassiveObjectUpdate()
        {
            Console.WriteLine("Benchmarking massive object updates...");
            
            var random = new Random(42);
            var asteroidCount = 100;
            var bulletCount = 200;
            var iterations = 1000;
            
            var asteroids = new List<Asteroid3D>();
            var bullets = new List<Bullet3D>();
            
            for (int i = 0; i < asteroidCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                asteroids.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Medium, random, 1));
            }
            
            for (int i = 0; i < bulletCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                bullets.Add(new Bullet3D(pos, Vector3.UnitX));
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                foreach (var asteroid in asteroids)
                {
                    asteroid.Update(800, 600, 100);
                }
                
                foreach (var bullet in bullets)
                {
                    bullet.Update(800, 600, 100);
                }
            }
            
            sw.Stop();
            
            var totalObjects = (asteroidCount + bulletCount) * iterations;
            var opsPerSecond = (double)totalObjects / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {totalObjects:N0} object updates in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} object updates per second");
            
            _results.AddBenchmark("Game Loop", "Massive Object Update", totalObjects, sw.ElapsedMilliseconds, opsPerSecond);
        }
        #endregion

        #region Particle System Benchmarks
        private void BenchmarkParticleSystem()
        {
            Console.WriteLine("=== PARTICLE SYSTEM BENCHMARKS ===");
            
            BenchmarkExplosionManager();
            BenchmarkParticleUpdate();
            
            Console.WriteLine();
        }

        private void BenchmarkExplosionManager()
        {
            Console.WriteLine("Benchmarking explosion manager...");
            
            var explosionManager = new ExplosionManager3D();
            var explosionCount = 100;
            var iterations = 100;
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < explosionCount; i++)
                {
                    var pos = new Vector3(i * 10, iteration * 10, 0);
                    explosionManager.CreateAsteroidExplosion(pos, AsteroidSize.Large, 1);
                }
                
                explosionManager.Update();
            }
            
            sw.Stop();
            
            var totalExplosions = explosionCount * iterations;
            var opsPerSecond = (double)totalExplosions / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {totalExplosions:N0} explosions processed in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} explosions per second");
            Console.WriteLine($"  ✓ Final particle count: {explosionManager.GetParticleCount()}");
            
            _results.AddBenchmark("Particles", "Explosion Manager", totalExplosions, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkParticleUpdate()
        {
            Console.WriteLine("Benchmarking particle updates...");
            
            var random = new Random(42);
            var particleCount = 10000;
            var iterations = 100;
            
            var particles = new List<ExplosionParticle3D>();
            for (int i = 0; i < particleCount; i++)
            {
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                particles.Add(ExplosionParticle3D.CreateExplosionParticle(pos, random, 1.0f));
            }
            
            GC.Collect();
            var sw = Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                foreach (var particle in particles)
                {
                    particle.Update();
                }
            }
            
            sw.Stop();
            
            var totalUpdates = particleCount * iterations;
            var opsPerSecond = (double)totalUpdates / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {totalUpdates:N0} particle updates in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} particle updates per second");
            
            _results.AddBenchmark("Particles", "Particle Update", totalUpdates, sw.ElapsedMilliseconds, opsPerSecond);
        }
        #endregion

        #region Memory Benchmarks
        private void BenchmarkMemoryAllocation()
        {
            Console.WriteLine("=== MEMORY ALLOCATION BENCHMARKS ===");
            
            BenchmarkMemoryThroughput();
            BenchmarkGarbageCollection();
            
            Console.WriteLine();
        }

        private void BenchmarkMemoryThroughput()
        {
            Console.WriteLine("Benchmarking memory throughput...");
            
            var iterations = 100_000;
            var random = new Random(42);
            
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);
            var sw = Stopwatch.StartNew();
            
            var objects = new List<object>();
            for (int i = 0; i < iterations; i++)
            {
                // Allocate various game objects
                var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                
                objects.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Small, random, 1));
                objects.Add(new Bullet3D(pos, Vector3.UnitX));
                objects.Add(ExplosionParticle3D.CreateExplosionParticle(pos, random, 1.0f));
                
                // Clear periodically to simulate game cleanup
                if (i % 10000 == 0)
                {
                    objects.Clear();
                }
            }
            
            sw.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            
            var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB
            var opsPerSecond = (double)iterations / sw.Elapsed.TotalSeconds;
            
            Console.WriteLine($"  ✓ {iterations:N0} allocations in {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"  ✓ {opsPerSecond:N0} allocations per second");
            Console.WriteLine($"  ✓ Memory used: {memoryUsed:F2} MB");
            
            _results.AddBenchmark("Memory", "Allocation Throughput", iterations, sw.ElapsedMilliseconds, opsPerSecond);
        }

        private void BenchmarkGarbageCollection()
        {
            Console.WriteLine("Benchmarking garbage collection impact...");
            
            var iterations = 10;
            var objectsPerIteration = 100_000;
            var gcTimes = new List<long>();
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var objects = new List<object>();
                var random = new Random(42);
                
                // Allocate objects
                for (int i = 0; i < objectsPerIteration; i++)
                {
                    var pos = new Vector3(random.NextSingle() * 800, random.NextSingle() * 600, random.NextSingle() * 100);
                    objects.Add(new Asteroid3D(pos, Vector3.One, AsteroidSize.Small, random, 1));
                }
                
                // Force GC and measure time
                var sw = Stopwatch.StartNew();
                objects.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                sw.Stop();
                
                gcTimes.Add(sw.ElapsedMilliseconds);
            }
            
            var avgGcTime = gcTimes.Average();
            var maxGcTime = gcTimes.Max();
            var minGcTime = gcTimes.Min();
            
            Console.WriteLine($"  ✓ {iterations} GC cycles completed");
            Console.WriteLine($"  ✓ Average GC time: {avgGcTime:F2}ms");
            Console.WriteLine($"  ✓ Min GC time: {minGcTime}ms, Max GC time: {maxGcTime}ms");
            
            _results.AddBenchmark("Memory", "GC Impact", iterations, (long)avgGcTime, 1000.0 / avgGcTime);
        }
        #endregion

        #region Report Generation
        private void GenerateBenchmarkReport()
        {
            Console.WriteLine("\n" + "=".PadRight(80, '='));
            Console.WriteLine("PERFORMANCE BENCHMARK REPORT");
            Console.WriteLine("=".PadRight(80, '='));
            
            Console.WriteLine($"\nSystem Information:");
            Console.WriteLine($"- OS: {Environment.OSVersion}");
            Console.WriteLine($"- .NET Version: {Environment.Version}");
            Console.WriteLine($"- Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"- Working Set: {Environment.WorkingSet / 1024.0 / 1024.0:F2} MB");
            
            Console.WriteLine($"\nBenchmark Results Summary:");
            Console.WriteLine($"- Total Benchmarks: {_results.Benchmarks.Count}");
            Console.WriteLine($"- Execution Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            // Group results by category
            var categories = _results.Benchmarks.GroupBy(b => b.Category).ToList();
            
            foreach (var category in categories)
            {
                Console.WriteLine($"\n{category.Key.ToUpper()} PERFORMANCE:");
                foreach (var benchmark in category)
                {
                    Console.WriteLine($"  {benchmark.Name}:");
                    Console.WriteLine($"    Operations: {benchmark.Operations:N0}");
                    Console.WriteLine($"    Time: {benchmark.Milliseconds}ms");
                    Console.WriteLine($"    Rate: {benchmark.OperationsPerSecond:N0} ops/sec");
                }
            }
            
            // Performance assessment
            Console.WriteLine($"\nPERFORMACE ASSESSMENT:");
            AssessPerformance();
            
            Console.WriteLine("=".PadRight(80, '='));
        }

        private void AssessPerformance()
        {
            var collisionBenchmarks = _results.Benchmarks.Where(b => b.Category == "Collision").ToList();
            var gameLoopBenchmarks = _results.Benchmarks.Where(b => b.Category == "Game Loop").ToList();
            var memoryBenchmarks = _results.Benchmarks.Where(b => b.Category == "Memory").ToList();
            
            // Collision performance assessment
            var basicCollisionRate = collisionBenchmarks.FirstOrDefault(b => b.Name == "Basic Sphere")?.OperationsPerSecond ?? 0;
            if (basicCollisionRate > 1_000_000)
                Console.WriteLine("✅ EXCELLENT: Collision detection performance is outstanding");
            else if (basicCollisionRate > 500_000)
                Console.WriteLine("✅ GOOD: Collision detection performance is acceptable");
            else
                Console.WriteLine("⚠️ CONCERN: Collision detection performance may need optimization");
            
            // Game loop assessment
            var gameUpdateRate = gameLoopBenchmarks.FirstOrDefault(b => b.Name == "GameManager3D Update")?.OperationsPerSecond ?? 0;
            if (gameUpdateRate > 10_000)
                Console.WriteLine("✅ EXCELLENT: Game loop can maintain high frame rates");
            else if (gameUpdateRate > 1_000)
                Console.WriteLine("✅ GOOD: Game loop performance is suitable for 60 FPS");
            else
                Console.WriteLine("⚠️ CONCERN: Game loop performance may struggle with 60 FPS");
            
            // Memory assessment
            var gcBenchmark = memoryBenchmarks.FirstOrDefault(b => b.Name == "GC Impact");
            if (gcBenchmark != null && gcBenchmark.Milliseconds < 10)
                Console.WriteLine("✅ EXCELLENT: Low garbage collection impact");
            else if (gcBenchmark != null && gcBenchmark.Milliseconds < 50)
                Console.WriteLine("✅ GOOD: Manageable garbage collection impact");
            else
                Console.WriteLine("⚠️ CONCERN: Garbage collection may cause frame drops");
            
            // Overall 3D readiness assessment
            Console.WriteLine($"\n🎯 OVERALL 3D READINESS: {AssessOverallReadiness()}");
        }

        private string AssessOverallReadiness()
        {
            var issues = 0;
            var strengths = 0;
            
            // Check key performance indicators
            var collisionRate = _results.Benchmarks.FirstOrDefault(b => b.Name == "Basic Sphere")?.OperationsPerSecond ?? 0;
            var gameLoopRate = _results.Benchmarks.FirstOrDefault(b => b.Name == "GameManager3D Update")?.OperationsPerSecond ?? 0;
            
            if (collisionRate > 500_000) strengths++; else issues++;
            if (gameLoopRate > 1_000) strengths++; else issues++;
            
            if (strengths >= 2 && issues == 0)
                return "PRODUCTION READY - Performance exceeds requirements";
            else if (strengths >= 1)
                return "GOOD - Ready for Phase 2 with minor optimizations";
            else
                return "NEEDS OPTIMIZATION - Address performance concerns before Phase 2";
        }
        #endregion
    }

    #region Supporting Classes
    public class BenchmarkResults
    {
        public List<BenchmarkResult> Benchmarks { get; } = new List<BenchmarkResult>();

        public void AddBenchmark(string category, string name, int operations, long milliseconds, double operationsPerSecond)
        {
            Benchmarks.Add(new BenchmarkResult
            {
                Category = category,
                Name = name,
                Operations = operations,
                Milliseconds = milliseconds,
                OperationsPerSecond = operationsPerSecond
            });
        }
    }

    public class BenchmarkResult
    {
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public int Operations { get; set; }
        public long Milliseconds { get; set; }
        public double OperationsPerSecond { get; set; }
    }
    #endregion
}