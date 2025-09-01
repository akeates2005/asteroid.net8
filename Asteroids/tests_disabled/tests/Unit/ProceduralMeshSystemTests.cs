using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asteroids;

namespace Asteroids.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for ProceduralMeshSystem following TDD methodology.
    /// Tests mesh generation, caching, LOD management, and performance optimization.
    /// </summary>
    [TestClass]
    public class ProceduralMeshSystemTests
    {
        private ProceduralMeshSystem _meshSystem;

        [TestInitialize]
        public void Setup()
        {
            _meshSystem = new ProceduralMeshSystem();
            _meshSystem.Initialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _meshSystem?.Cleanup();
        }

        #region Initialization Tests

        [TestMethod]
        public void Initialize_SetsUpCacheAndSystems()
        {
            // ARRANGE
            var meshSystem = new ProceduralMeshSystem();

            // ACT
            bool result = meshSystem.Initialize();

            // ASSERT
            Assert.IsTrue(result, "Initialization should succeed");
            Assert.IsTrue(meshSystem.IsInitialized, "System should be marked as initialized");
        }

        [TestMethod]
        public void Initialize_WithCustomSettings_AppliesConfiguration()
        {
            // ARRANGE
            var meshSystem = new ProceduralMeshSystem();
            var settings = new MeshGenerationSettings
            {
                DefaultVertexCount = 2000,
                MaxCacheSize = 100 * 1024 * 1024, // 100MB
                EnableOptimization = true,
                DefaultLODLevels = 4
            };

            // ACT
            bool result = meshSystem.Initialize(settings);
            var config = meshSystem.GetCurrentSettings();

            // ASSERT
            Assert.IsTrue(result, "Initialization with custom settings should succeed");
            Assert.AreEqual(2000, config.DefaultVertexCount);
            Assert.AreEqual(100 * 1024 * 1024, config.MaxCacheSize);
            Assert.IsTrue(config.EnableOptimization);
        }

        #endregion

        #region Asteroid Mesh Generation Tests

        [TestMethod]
        public void GenerateAsteroidMesh_CreatesValidMesh()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Large,
                Seed = 12345,
                LODLevel = 0,
                DisplacementStrength = 0.3f,
                VertexCount = 1000
            };

            // ACT
            var mesh = _meshSystem.GenerateAsteroidMesh(config);

            // ASSERT
            Assert.IsNotNull(mesh, "Generated mesh should not be null");
            Assert.IsTrue(mesh.Vertices.Length > 0, "Mesh should have vertices");
            Assert.IsTrue(mesh.Indices.Length > 0, "Mesh should have indices");
            Assert.IsTrue(mesh.Vertices.Length <= config.VertexCount, "Vertex count should not exceed limit");
            Assert.AreEqual(0, mesh.Indices.Length % 3, "Indices should form complete triangles");
            
            // Validate vertex properties
            foreach (var vertex in mesh.Vertices)
            {
                Assert.IsFalse(float.IsNaN(vertex.Position.X), "Vertex position X should be valid");
                Assert.IsFalse(float.IsNaN(vertex.Position.Y), "Vertex position Y should be valid");
                Assert.IsFalse(float.IsNaN(vertex.Position.Z), "Vertex position Z should be valid");
                Assert.IsTrue(vertex.Normal.Length() > 0.5f, "Vertex normal should be normalized-ish");
            }
        }

        [TestMethod]
        public void GenerateAsteroidMesh_ConsistentWithSameSeed()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Medium,
                Seed = 98765,
                LODLevel = 1,
                DisplacementStrength = 0.5f,
                VertexCount = 500
            };

            // ACT
            var mesh1 = _meshSystem.GenerateAsteroidMesh(config);
            var mesh2 = _meshSystem.GenerateAsteroidMesh(config);

            // ASSERT
            Assert.AreEqual(mesh1.Vertices.Length, mesh2.Vertices.Length, "Same seed should produce same vertex count");
            Assert.AreEqual(mesh1.Indices.Length, mesh2.Indices.Length, "Same seed should produce same index count");
            
            for (int i = 0; i < mesh1.Vertices.Length; i++)
            {
                Assert.IsTrue(Vector3.Distance(mesh1.Vertices[i].Position, mesh2.Vertices[i].Position) < 0.001f,
                    $"Vertex {i} position should be identical with same seed");
            }
        }

        [TestMethod]
        public void GenerateAsteroidMesh_LODReducesComplexity()
        {
            // ARRANGE
            var baseConfig = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Large,
                Seed = 11111,
                LODLevel = 0,
                DisplacementStrength = 0.4f,
                VertexCount = 2000
            };

            var lodConfig = baseConfig;
            lodConfig.LODLevel = 2;

            // ACT
            var highDetailMesh = _meshSystem.GenerateAsteroidMesh(baseConfig);
            var lowDetailMesh = _meshSystem.GenerateAsteroidMesh(lodConfig);

            // ASSERT
            Assert.IsTrue(lowDetailMesh.Vertices.Length < highDetailMesh.Vertices.Length,
                "LOD 2 should have fewer vertices than LOD 0");
            Assert.IsTrue(lowDetailMesh.Indices.Length < highDetailMesh.Indices.Length,
                "LOD 2 should have fewer indices than LOD 0");
        }

        [TestMethod]
        public void GenerateAsteroidMesh_DifferentSizes_ProduceDifferentComplexity()
        {
            // ARRANGE
            var smallConfig = new AsteroidMeshConfig { Size = AsteroidSize.Small, Seed = 1, LODLevel = 0 };
            var mediumConfig = new AsteroidMeshConfig { Size = AsteroidSize.Medium, Seed = 1, LODLevel = 0 };
            var largeConfig = new AsteroidMeshConfig { Size = AsteroidSize.Large, Seed = 1, LODLevel = 0 };

            // ACT
            var smallMesh = _meshSystem.GenerateAsteroidMesh(smallConfig);
            var mediumMesh = _meshSystem.GenerateAsteroidMesh(mediumConfig);
            var largeMesh = _meshSystem.GenerateAsteroidMesh(largeConfig);

            // ASSERT
            Assert.IsTrue(smallMesh.Vertices.Length <= mediumMesh.Vertices.Length,
                "Small asteroids should have same or fewer vertices than medium");
            Assert.IsTrue(mediumMesh.Vertices.Length <= largeMesh.Vertices.Length,
                "Medium asteroids should have same or fewer vertices than large");
        }

        #endregion

        #region Mesh Caching Tests

        [TestMethod]
        public void MeshCache_StoreAndRetrieve_WorksCorrectly()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Medium,
                Seed = 54321,
                LODLevel = 0
            };
            
            // ACT
            var originalMesh = _meshSystem.GenerateAsteroidMesh(config);
            var cachedMesh = _meshSystem.GenerateAsteroidMesh(config); // Should return cached version

            // ASSERT
            Assert.AreSame(originalMesh, cachedMesh, "Second generation should return cached mesh");
            
            var cacheStats = _meshSystem.GetCacheStatistics();
            Assert.IsTrue(cacheStats.HitCount > 0, "Cache should have recorded hit");
        }

        [TestMethod]
        public void MeshCache_MemoryLimit_EvictsOldEntries()
        {
            // ARRANGE
            var settings = new MeshGenerationSettings
            {
                MaxCacheSize = 1024 * 1024 // 1MB limit
            };
            _meshSystem.Initialize(settings);

            var meshes = new List<EnhancedMesh>();
            
            // ACT - Generate many meshes to exceed cache limit
            for (int i = 0; i < 50; i++)
            {
                var config = new AsteroidMeshConfig
                {
                    Size = AsteroidSize.Large,
                    Seed = i,
                    LODLevel = 0,
                    VertexCount = 1000
                };
                
                var mesh = _meshSystem.GenerateAsteroidMesh(config);
                meshes.Add(mesh);
            }

            var cacheStats = _meshSystem.GetCacheStatistics();

            // ASSERT
            Assert.IsTrue(cacheStats.EvictionCount > 0, "Cache should have evicted entries");
            Assert.IsTrue(cacheStats.CurrentMemoryUsage <= settings.MaxCacheSize,
                "Cache should not exceed memory limit");
        }

        [TestMethod]
        public void MeshCache_LRUEviction_KeepsRecentlyUsedMeshes()
        {
            // ARRANGE
            var config1 = new AsteroidMeshConfig { Size = AsteroidSize.Large, Seed = 1, LODLevel = 0 };
            var config2 = new AsteroidMeshConfig { Size = AsteroidSize.Large, Seed = 2, LODLevel = 0 };
            
            // ACT
            var mesh1First = _meshSystem.GenerateAsteroidMesh(config1);
            var mesh2First = _meshSystem.GenerateAsteroidMesh(config2);
            
            // Generate many other meshes to potentially evict
            for (int i = 10; i < 40; i++)
            {
                var tempConfig = new AsteroidMeshConfig { Size = AsteroidSize.Large, Seed = i, LODLevel = 0 };
                _meshSystem.GenerateAsteroidMesh(tempConfig);
            }
            
            // Access mesh1 again (making it recently used)
            var mesh1Second = _meshSystem.GenerateAsteroidMesh(config1);
            
            var cacheStats = _meshSystem.GetCacheStatistics();

            // ASSERT
            Assert.AreSame(mesh1First, mesh1Second, "Recently accessed mesh should still be cached");
        }

        #endregion

        #region LOD System Tests

        [TestMethod]
        public void LODCalculation_BasedOnDistance_ReturnsCorrectLevel()
        {
            // ARRANGE
            var cameraPosition = Vector3.Zero;
            var closeObject = new Vector3(0, 0, 10);
            var mediumObject = new Vector3(0, 0, 50);
            var farObject = new Vector3(0, 0, 150);

            // ACT
            var closeLOD = _meshSystem.CalculateLODLevel(closeObject, cameraPosition, AsteroidSize.Large);
            var mediumLOD = _meshSystem.CalculateLODLevel(mediumObject, cameraPosition, AsteroidSize.Large);
            var farLOD = _meshSystem.CalculateLODLevel(farObject, cameraPosition, AsteroidSize.Large);

            // ASSERT
            Assert.AreEqual(0, closeLOD, "Close objects should use highest detail (LOD 0)");
            Assert.AreEqual(1, mediumLOD, "Medium distance objects should use medium detail (LOD 1)");
            Assert.AreEqual(2, farLOD, "Far objects should use lowest detail (LOD 2)");
        }

        [TestMethod]
        public void LODCalculation_DifferentSizes_AdjustsThresholds()
        {
            // ARRANGE
            var cameraPosition = Vector3.Zero;
            var position = new Vector3(0, 0, 100);

            // ACT
            var smallLOD = _meshSystem.CalculateLODLevel(position, cameraPosition, AsteroidSize.Small);
            var largeLOD = _meshSystem.CalculateLODLevel(position, cameraPosition, AsteroidSize.Large);

            // ASSERT
            Assert.IsTrue(smallLOD >= largeLOD, "Small asteroids should use same or lower detail at same distance");
        }

        [TestMethod]
        public void LODThresholdAdjustment_BasedOnPerformance_ModifiesCorrectly()
        {
            // ARRANGE
            var initialThresholds = _meshSystem.GetLODThresholds(AsteroidSize.Large);
            var performanceMetrics = new PerformanceMetrics
            {
                AverageFrameRate = 30.0f, // Below target
                MemoryUsage = 80 * 1024 * 1024 // 80MB
            };

            // ACT
            _meshSystem.AdjustLODThresholds(performanceMetrics);
            var adjustedThresholds = _meshSystem.GetLODThresholds(AsteroidSize.Large);

            // ASSERT
            for (int i = 0; i < initialThresholds.Length; i++)
            {
                Assert.IsTrue(adjustedThresholds[i] < initialThresholds[i],
                    $"LOD threshold {i} should be reduced when performance is poor");
            }
        }

        #endregion

        #region Mesh Optimization Tests

        [TestMethod]
        public void OptimizeMesh_ReducesTriangleCount()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Large,
                Seed = 99999,
                LODLevel = 0,
                VertexCount = 2000
            };
            
            var originalMesh = _meshSystem.GenerateAsteroidMesh(config);
            var originalTriangleCount = originalMesh.Indices.Length / 3;

            // ACT
            var optimizedMesh = originalMesh;
            _meshSystem.OptimizeMesh(ref optimizedMesh, originalTriangleCount / 2);

            // ASSERT
            var optimizedTriangleCount = optimizedMesh.Indices.Length / 3;
            Assert.IsTrue(optimizedTriangleCount <= originalTriangleCount / 2 + 10,
                "Optimized mesh should have approximately half the triangles");
            Assert.IsTrue(optimizedMesh.IsOptimized, "Mesh should be marked as optimized");
        }

        [TestMethod]
        public void GenerateLODMesh_ProducesSimplifiedVersion()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Large,
                Seed = 77777,
                LODLevel = 0,
                VertexCount = 1500
            };
            
            var baseMesh = _meshSystem.GenerateAsteroidMesh(config);

            // ACT
            var lod1Mesh = _meshSystem.GenerateLODMesh(baseMesh, 1);
            var lod2Mesh = _meshSystem.GenerateLODMesh(baseMesh, 2);

            // ASSERT
            Assert.IsTrue(lod1Mesh.Vertices.Length < baseMesh.Vertices.Length,
                "LOD 1 should have fewer vertices than base mesh");
            Assert.IsTrue(lod2Mesh.Vertices.Length < lod1Mesh.Vertices.Length,
                "LOD 2 should have fewer vertices than LOD 1");
        }

        [TestMethod]
        public void BatchOptimizeMeshes_ProcessesMultipleMeshes()
        {
            // ARRANGE
            var meshes = new List<EnhancedMesh>();
            for (int i = 0; i < 5; i++)
            {
                var config = new AsteroidMeshConfig { Size = AsteroidSize.Medium, Seed = i, LODLevel = 0 };
                meshes.Add(_meshSystem.GenerateAsteroidMesh(config));
            }

            // ACT
            _meshSystem.BatchOptimizeMeshes(meshes);

            // ASSERT
            Assert.IsTrue(meshes.All(m => m.IsOptimized), "All meshes should be optimized");
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        [Timeout(5000)] // 5 second timeout
        public void GenerateAsteroidMesh_PerformanceUnder100ms()
        {
            // ARRANGE
            var config = new AsteroidMeshConfig
            {
                Size = AsteroidSize.Large,
                Seed = 12345,
                LODLevel = 0,
                VertexCount = 1000
            };

            // ACT
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var mesh = _meshSystem.GenerateAsteroidMesh(config);
            stopwatch.Stop();

            // ASSERT
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100,
                $"Mesh generation should complete in under 100ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.IsNotNull(mesh, "Mesh should be generated within time limit");
        }

        [TestMethod]
        public void MeshSystem_MemoryUsage_RemainsWithinBounds()
        {
            // ARRANGE
            var initialMemory = GC.GetTotalMemory(true);

            // ACT
            for (int i = 0; i < 100; i++)
            {
                var config = new AsteroidMeshConfig
                {
                    Size = AsteroidSize.Medium,
                    Seed = i,
                    LODLevel = 0,
                    VertexCount = 500
                };
                
                _meshSystem.GenerateAsteroidMesh(config);
                
                if (i % 10 == 0)
                {
                    GC.Collect();
                }
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;

            // ASSERT
            Assert.IsTrue(memoryGrowth < 50 * 1024 * 1024,
                $"Memory growth should be under 50MB, actual: {memoryGrowth / 1024 / 1024}MB");
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateAsteroidMesh_InvalidConfig_ThrowsException()
        {
            // ARRANGE
            var invalidConfig = new AsteroidMeshConfig
            {
                Size = (AsteroidSize)999, // Invalid size
                Seed = -1,
                LODLevel = -1,
                VertexCount = -100
            };

            // ACT & ASSERT
            _meshSystem.GenerateAsteroidMesh(invalidConfig);
        }

        [TestMethod]
        public void MeshSystem_NullInput_HandlesGracefully()
        {
            // ACT & ASSERT
            try
            {
                var result = _meshSystem.GeneratePlayerMesh(null);
                Assert.IsNull(result, "Should return null for null input");
            }
            catch (Exception)
            {
                Assert.Fail("Should handle null input gracefully");
            }
        }

        [TestMethod]
        public void OptimizeMesh_NullMesh_HandlesGracefully()
        {
            // ARRANGE
            EnhancedMesh nullMesh = null;

            // ACT & ASSERT
            try
            {
                _meshSystem.OptimizeMesh(ref nullMesh, 100);
                Assert.IsNull(nullMesh, "Null mesh should remain null");
            }
            catch (Exception)
            {
                Assert.Fail("Should handle null mesh gracefully");
            }
        }

        #endregion

        #region Cache Management Tests

        [TestMethod]
        public void ClearCache_RemovesAllEntries()
        {
            // ARRANGE
            for (int i = 0; i < 10; i++)
            {
                var config = new AsteroidMeshConfig { Size = AsteroidSize.Medium, Seed = i, LODLevel = 0 };
                _meshSystem.GenerateAsteroidMesh(config);
            }
            
            var statsBefore = _meshSystem.GetCacheStatistics();

            // ACT
            _meshSystem.ClearCache();
            var statsAfter = _meshSystem.GetCacheStatistics();

            // ASSERT
            Assert.IsTrue(statsBefore.CachedMeshCount > 0, "Cache should have entries before clearing");
            Assert.AreEqual(0, statsAfter.CachedMeshCount, "Cache should be empty after clearing");
            Assert.AreEqual(0, statsAfter.CurrentMemoryUsage, "Memory usage should be zero after clearing");
        }

        [TestMethod]
        public void ClearExpiredCache_RemovesOnlyOldEntries()
        {
            // ARRANGE
            var config1 = new AsteroidMeshConfig { Size = AsteroidSize.Medium, Seed = 1, LODLevel = 0 };
            var config2 = new AsteroidMeshConfig { Size = AsteroidSize.Medium, Seed = 2, LODLevel = 0 };
            
            _meshSystem.GenerateAsteroidMesh(config1);
            System.Threading.Thread.Sleep(100); // Ensure time difference
            _meshSystem.GenerateAsteroidMesh(config2);

            // ACT
            _meshSystem.ClearExpiredCache(TimeSpan.FromMilliseconds(50));
            
            // Try to access meshes again
            var mesh1 = _meshSystem.GenerateAsteroidMesh(config1); // Should be regenerated
            var mesh2 = _meshSystem.GenerateAsteroidMesh(config2); // Should be cached

            var stats = _meshSystem.GetCacheStatistics();

            // ASSERT
            Assert.IsTrue(stats.MissCount > 0, "Should have cache misses for expired entries");
        }

        [TestMethod]
        public void SetCacheLimit_AdjustsCacheSize()
        {
            // ARRANGE
            var originalLimit = _meshSystem.GetCurrentSettings().MaxCacheSize;
            var newLimit = originalLimit / 2;

            // ACT
            _meshSystem.SetCacheLimit(newLimit);
            var updatedSettings = _meshSystem.GetCurrentSettings();

            // ASSERT
            Assert.AreEqual(newLimit, updatedSettings.MaxCacheSize, "Cache limit should be updated");
        }

        #endregion

        #region Specialized Mesh Generation Tests

        [TestMethod]
        public void GeneratePlayerMesh_CreatesValidPlayerGeometry()
        {
            // ARRANGE
            var config = new PlayerMeshConfig
            {
                ShipType = PlayerShipType.Fighter,
                DetailLevel = 1,
                IncludeEngineTrails = true
            };

            // ACT
            var mesh = _meshSystem.GeneratePlayerMesh(config);

            // ASSERT
            Assert.IsNotNull(mesh, "Player mesh should be generated");
            Assert.IsTrue(mesh.Vertices.Length > 0, "Player mesh should have vertices");
            Assert.IsTrue(mesh.BoundingBox.Size.Length() > 0, "Player mesh should have valid bounding box");
        }

        [TestMethod]
        public void GenerateBulletMesh_CreatesSimpleGeometry()
        {
            // ARRANGE
            var config = new BulletMeshConfig
            {
                BulletType = BulletType.Standard,
                Length = 2.0f,
                Width = 0.5f
            };

            // ACT
            var mesh = _meshSystem.GenerateBulletMesh(config);

            // ASSERT
            Assert.IsNotNull(mesh, "Bullet mesh should be generated");
            Assert.IsTrue(mesh.Vertices.Length >= 8, "Bullet mesh should have at least 8 vertices for a simple shape");
        }

        [TestMethod]
        public void GenerateEffectMesh_CreatesParticleGeometry()
        {
            // ARRANGE
            var config = new EffectMeshConfig
            {
                EffectType = EffectType.Explosion,
                ParticleCount = 100,
                Duration = 2.0f,
                IntensityScale = 1.5f
            };

            // ACT
            var mesh = _meshSystem.GenerateEffectMesh(config);

            // ASSERT
            Assert.IsNotNull(mesh, "Effect mesh should be generated");
            Assert.IsTrue(mesh.Vertices.Length > 0, "Effect mesh should have vertices");
        }

        #endregion
    }

    #region Test Support Classes

    /// <summary>
    /// Mock performance metrics for testing LOD adjustment
    /// </summary>
    public class PerformanceMetrics
    {
        public float AverageFrameRate { get; set; }
        public long MemoryUsage { get; set; }
        public float CPUUsage { get; set; }
        public float GPUUsage { get; set; }
    }

    /// <summary>
    /// Mock mesh generation settings for testing
    /// </summary>
    public class MeshGenerationSettings
    {
        public int DefaultVertexCount { get; set; } = 1000;
        public long MaxCacheSize { get; set; } = 200 * 1024 * 1024; // 200MB
        public bool EnableOptimization { get; set; } = true;
        public int DefaultLODLevels { get; set; } = 3;
        public bool EnableBatching { get; set; } = true;
        public float QualityScaling { get; set; } = 1.0f;
    }

    #endregion
}