using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raylib_cs;
using Asteroids;

namespace Asteroids.Tests.Unit
{
    [TestClass]
    public class LightingSystem3DTests
    {
        private LightingSystem3D _lighting;
        
        [TestInitialize]
        public void Setup()
        {
            _lighting = new LightingSystem3D();
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            _lighting?.Cleanup();
        }
        
        [TestMethod]
        public void Initialize_SetsUpLightingSystem()
        {
            // ACT
            var result = _lighting.Initialize();
            
            // ASSERT
            Assert.IsTrue(result);
            Assert.IsTrue(_lighting.IsInitialized);
        }
        
        [TestMethod]
        public void AddLight_IncreasesLightCount()
        {
            // ARRANGE
            _lighting.Initialize();
            var initialCount = _lighting.GetLightCount();
            
            var light = new Light3D
            {
                Type = LightType.Point,
                Position = Vector3.Zero,
                Color = Color.White,
                Intensity = 1.0f,
                Range = 10.0f
            };
            
            // ACT
            _lighting.AddLight(light);
            
            // ASSERT
            Assert.AreEqual(initialCount + 1, _lighting.GetLightCount());
        }
        
        [TestMethod]
        public void RemoveLight_DecreasesLightCount()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var light = new Light3D
            {
                Type = LightType.Directional,
                Direction = Vector3.UnitY,
                Color = Color.Yellow,
                Intensity = 0.8f
            };
            
            _lighting.AddLight(light);
            var countWithLight = _lighting.GetLightCount();
            
            // ACT
            _lighting.RemoveLight(light);
            
            // ASSERT
            Assert.AreEqual(countWithLight - 1, _lighting.GetLightCount());
        }
        
        [TestMethod]
        public void SetAmbientLight_UpdatesAmbientColor()
        {
            // ARRANGE
            _lighting.Initialize();
            var ambientColor = new Color(50, 50, 100, 255);
            
            // ACT
            _lighting.SetAmbientLight(ambientColor, 0.3f);
            var retrievedAmbient = _lighting.GetAmbientLight();
            
            // ASSERT
            Assert.AreEqual(ambientColor.R, retrievedAmbient.R);
            Assert.AreEqual(ambientColor.G, retrievedAmbient.G);
            Assert.AreEqual(ambientColor.B, retrievedAmbient.B);
        }
        
        [TestMethod]
        public void CalculateLighting_ReturnsCorrectIntensity()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var light = new Light3D
            {
                Type = LightType.Point,
                Position = new Vector3(0, 5, 0),
                Color = Color.White,
                Intensity = 1.0f,
                Range = 10.0f
            };
            
            _lighting.AddLight(light);
            
            var surfacePoint = Vector3.Zero;
            var surfaceNormal = Vector3.UnitY;
            
            // ACT
            var lighting = _lighting.CalculateLighting(surfacePoint, surfaceNormal);
            
            // ASSERT
            Assert.IsTrue(lighting.Intensity > 0);
            Assert.IsTrue(lighting.Intensity <= 1.0f);
        }
        
        [TestMethod]
        public void DirectionalLight_AffectsAllSurfaces()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var directionalLight = new Light3D
            {
                Type = LightType.Directional,
                Direction = new Vector3(0, -1, 0), // Straight down
                Color = Color.White,
                Intensity = 1.0f
            };
            
            _lighting.AddLight(directionalLight);
            
            var point1 = new Vector3(0, 0, 0);
            var point2 = new Vector3(100, 0, 100);
            var normal = Vector3.UnitY;
            
            // ACT
            var lighting1 = _lighting.CalculateLighting(point1, normal);
            var lighting2 = _lighting.CalculateLighting(point2, normal);
            
            // ASSERT
            // Directional lights should affect all surfaces equally (ignoring distance)
            Assert.AreEqual(lighting1.Intensity, lighting2.Intensity, 0.01f);
        }
        
        [TestMethod]
        public void PointLight_AttenuatesWithDistance()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var pointLight = new Light3D
            {
                Type = LightType.Point,
                Position = Vector3.Zero,
                Color = Color.White,
                Intensity = 1.0f,
                Range = 20.0f
            };
            
            _lighting.AddLight(pointLight);
            
            var closePoint = new Vector3(0, 1, 0);
            var farPoint = new Vector3(0, 10, 0);
            var normal = Vector3.UnitY;
            
            // ACT
            var closeLighting = _lighting.CalculateLighting(closePoint, normal);
            var farLighting = _lighting.CalculateLighting(farPoint, normal);
            
            // ASSERT
            Assert.IsTrue(closeLighting.Intensity > farLighting.Intensity);
        }
        
        [TestMethod]
        public void SpotLight_RespectsConeAngle()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var spotLight = new Light3D
            {
                Type = LightType.Spot,
                Position = new Vector3(0, 5, 0),
                Direction = new Vector3(0, -1, 0), // Pointing down
                Color = Color.White,
                Intensity = 1.0f,
                Range = 10.0f,
                ConeAngle = 30.0f // 30 degree cone
            };
            
            _lighting.AddLight(spotLight);
            
            var insidePoint = new Vector3(0, 0, 0); // Directly below
            var outsidePoint = new Vector3(5, 0, 0); // Outside cone
            var normal = Vector3.UnitY;
            
            // ACT
            var insideLighting = _lighting.CalculateLighting(insidePoint, normal);
            var outsideLighting = _lighting.CalculateLighting(outsidePoint, normal);
            
            // ASSERT
            Assert.IsTrue(insideLighting.Intensity > outsideLighting.Intensity);
        }
        
        [TestMethod]
        public void UpdateLightPosition_ChangesLightingCalculation()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var light = new Light3D
            {
                Type = LightType.Point,
                Position = new Vector3(0, 5, 0),
                Color = Color.White,
                Intensity = 1.0f,
                Range = 10.0f
            };
            
            _lighting.AddLight(light);
            
            var testPoint = new Vector3(3, 0, 0);
            var normal = Vector3.UnitY;
            
            // ACT
            var lighting1 = _lighting.CalculateLighting(testPoint, normal);
            
            // Move light closer
            light.Position = new Vector3(3, 2, 0);
            _lighting.UpdateLight(light);
            
            var lighting2 = _lighting.CalculateLighting(testPoint, normal);
            
            // ASSERT
            Assert.AreNotEqual(lighting1.Intensity, lighting2.Intensity);
            Assert.IsTrue(lighting2.Intensity > lighting1.Intensity); // Should be brighter when closer
        }
        
        [TestMethod]
        public void ShadowCasting_BlocksLightCorrectly()
        {
            // ARRANGE
            _lighting.Initialize();
            _lighting.EnableShadows(true);
            
            var light = new Light3D
            {
                Type = LightType.Point,
                Position = new Vector3(0, 10, 0),
                Color = Color.White,
                Intensity = 1.0f,
                Range = 20.0f
            };
            
            _lighting.AddLight(light);
            
            // Add shadow caster
            var shadowCaster = new Asteroids.ShadowCaster3D
            {
                Position = new Vector3(0, 5, 0),
                Radius = 2.0f
            };
            
            _lighting.AddShadowCaster(shadowCaster);
            
            var surfacePoint = new Vector3(0, 0, 0); // Directly below light and caster
            var normal = Vector3.UnitY;
            
            // ACT
            var lighting = _lighting.CalculateLighting(surfacePoint, normal);
            
            // ASSERT
            Assert.IsTrue(lighting.IsShadowed);
            Assert.IsTrue(lighting.Intensity < 0.5f); // Should be significantly dimmed
        }
        
        [TestMethod]
        public void MultipleLight_CombinesCorrectly()
        {
            // ARRANGE
            _lighting.Initialize();
            
            var light1 = new Light3D
            {
                Type = LightType.Point,
                Position = new Vector3(-5, 5, 0),
                Color = Color.Red,
                Intensity = 0.5f,
                Range = 10.0f
            };
            
            var light2 = new Light3D
            {
                Type = LightType.Point,
                Position = new Vector3(5, 5, 0),
                Color = Color.Blue,
                Intensity = 0.5f,
                Range = 10.0f
            };
            
            _lighting.AddLight(light1);
            _lighting.AddLight(light2);
            
            var testPoint = Vector3.Zero;
            var normal = Vector3.UnitY;
            
            // ACT
            var combinedLighting = _lighting.CalculateLighting(testPoint, normal);
            
            // ASSERT
            Assert.IsTrue(combinedLighting.Intensity > 0.5f); // Should be brighter than either light alone
        }
        
        [TestMethod]
        public void PerformanceTest_HandlesManyLights()
        {
            // ARRANGE
            _lighting.Initialize();
            var random = new Random(12345);
            
            // Add many lights
            for (int i = 0; i < 100; i++)
            {
                var light = new Light3D
                {
                    Type = LightType.Point,
                    Position = new Vector3(
                        (float)(random.NextDouble() - 0.5) * 200,
                        (float)(random.NextDouble() - 0.5) * 200,
                        (float)(random.NextDouble() - 0.5) * 200
                    ),
                    Color = Color.White,
                    Intensity = 0.5f,
                    Range = 10.0f
                };
                
                _lighting.AddLight(light);
            }
            
            var testPoint = Vector3.Zero;
            var normal = Vector3.UnitY;
            
            // ACT
            var startTime = DateTime.Now;
            
            for (int i = 0; i < 100; i++)
            {
                _lighting.CalculateLighting(testPoint, normal);
            }
            
            var endTime = DateTime.Now;
            
            // ASSERT
            var totalTime = (endTime - startTime).TotalMilliseconds;
            Assert.IsTrue(totalTime < 50.0); // Should complete 100 calculations in under 50ms
        }
    }
    
    // Test helper structures
    public struct ShadowCaster3D
    {
        public Vector3 Position;
        public float Radius;
    }
}