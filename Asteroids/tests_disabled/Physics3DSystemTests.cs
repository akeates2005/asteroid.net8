using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asteroids;

namespace Asteroids.Tests.Unit
{
    [TestClass]
    public class Physics3DSystemTests
    {
        private Physics3DSystem _physics;
        private MockPhysicsBody3D _testBody;
        
        [TestInitialize]
        public void Setup()
        {
            _physics = new Physics3DSystem();
            _physics.Initialize();
            
            _testBody = new MockPhysicsBody3D
            {
                Position = Vector3.Zero,
                Velocity = Vector3.Zero,
                Mass = 1.0f,
                Radius = 1.0f
            };
        }
        
        [TestMethod]
        public void Initialize_SetsUpPhysicsWorld()
        {
            // ARRANGE
            var newPhysics = new Physics3DSystem();
            
            // ACT
            var result = newPhysics.Initialize();
            
            // ASSERT
            Assert.IsTrue(result);
            Assert.IsTrue(newPhysics.IsInitialized);
        }
        
        [TestMethod]
        public void AddBody_RegistersBodyInSystem()
        {
            // ARRANGE
            var bodyCount = _physics.GetBodyCount();
            
            // ACT
            _physics.AddBody(_testBody);
            
            // ASSERT
            Assert.AreEqual(bodyCount + 1, _physics.GetBodyCount());
        }
        
        [TestMethod]
        public void RemoveBody_UnregistersBodyFromSystem()
        {
            // ARRANGE
            _physics.AddBody(_testBody);
            var bodyCount = _physics.GetBodyCount();
            
            // ACT
            _physics.RemoveBody(_testBody);
            
            // ASSERT
            Assert.AreEqual(bodyCount - 1, _physics.GetBodyCount());
        }
        
        [TestMethod]
        public void Update_AppliesGravityToBody()
        {
            // ARRANGE
            _physics.AddBody(_testBody);
            _physics.SetGravity(new Vector3(0, -9.81f, 0));
            var initialVelocity = _testBody.Velocity;
            
            // ACT
            _physics.Update(1.0f); // 1 second
            
            // ASSERT
            var expectedVelocity = initialVelocity + new Vector3(0, -9.81f, 0);
            Assert.AreEqual(expectedVelocity.Y, _testBody.Velocity.Y, 0.1f);
        }
        
        [TestMethod]
        public void Update_AppliesVelocityToPosition()
        {
            // ARRANGE
            _testBody.Velocity = new Vector3(5, 0, 3);
            _physics.AddBody(_testBody);
            var initialPosition = _testBody.Position;
            
            // ACT
            _physics.Update(2.0f); // 2 seconds
            
            // ASSERT
            var expectedPosition = initialPosition + _testBody.Velocity * 2.0f;
            Assert.AreEqual(expectedPosition, _testBody.Position);
        }
        
        [TestMethod]
        public void CollisionDetection_DetectsSphereSphereCollision()
        {
            // ARRANGE
            var body1 = new MockPhysicsBody3D
            {
                Position = new Vector3(0, 0, 0),
                Radius = 1.0f,
                Mass = 1.0f
            };
            
            var body2 = new MockPhysicsBody3D
            {
                Position = new Vector3(1.5f, 0, 0), // Overlapping
                Radius = 1.0f,
                Mass = 1.0f
            };
            
            _physics.AddBody(body1);
            _physics.AddBody(body2);
            
            // ACT
            _physics.Update(0.016f);
            
            // ASSERT
            Assert.IsTrue(_physics.AreColliding(body1, body2));
        }
        
        [TestMethod]
        public void CollisionResponse_SeparatesBodies()
        {
            // ARRANGE
            var body1 = new MockPhysicsBody3D
            {
                Position = new Vector3(0, 0, 0),
                Velocity = new Vector3(1, 0, 0),
                Radius = 1.0f,
                Mass = 1.0f
            };
            
            var body2 = new MockPhysicsBody3D
            {
                Position = new Vector3(1.0f, 0, 0), // Touching
                Velocity = Vector3.Zero,
                Radius = 1.0f,
                Mass = 1.0f
            };
            
            _physics.AddBody(body1);
            _physics.AddBody(body2);
            
            var initialDistance = Vector3.Distance(body1.Position, body2.Position);
            
            // ACT
            _physics.Update(0.016f);
            
            // ASSERT
            var finalDistance = Vector3.Distance(body1.Position, body2.Position);
            Assert.IsTrue(finalDistance > initialDistance); // Bodies should separate
        }
        
        [TestMethod]
        public void ApplyForce_ChangesBodyVelocity()
        {
            // ARRANGE
            _physics.AddBody(_testBody);
            var initialVelocity = _testBody.Velocity;
            var force = new Vector3(10, 0, 5);
            
            // ACT
            _physics.ApplyForce(_testBody, force);
            _physics.Update(1.0f);
            
            // ASSERT
            var expectedAcceleration = force / _testBody.Mass;
            var expectedVelocity = initialVelocity + expectedAcceleration;
            
            var tolerance = 0.1f;
            Assert.IsTrue(Vector3.Distance(expectedVelocity, _testBody.Velocity) <= tolerance);
        }
        
        [TestMethod]
        public void ApplyImpulse_InstantlyChangesVelocity()
        {
            // ARRANGE
            _physics.AddBody(_testBody);
            var initialVelocity = _testBody.Velocity;
            var impulse = new Vector3(5, 2, -3);
            
            // ACT
            _physics.ApplyImpulse(_testBody, impulse);
            
            // ASSERT
            var expectedVelocity = initialVelocity + (impulse / _testBody.Mass);
            Assert.AreEqual(expectedVelocity, _testBody.Velocity);
        }
        
        [TestMethod]
        public void Raycast_DetectsIntersection()
        {
            // ARRANGE
            _testBody.Position = new Vector3(0, 0, 5);
            _testBody.Radius = 2.0f;
            _physics.AddBody(_testBody);
            
            var rayOrigin = new Vector3(0, 0, 0);
            var rayDirection = Vector3.UnitZ;
            var maxDistance = 10.0f;
            
            // ACT
            var hit = _physics.Raycast(rayOrigin, rayDirection, maxDistance);
            
            // ASSERT
            Assert.IsTrue(hit.HasValue);
            Assert.AreEqual(_testBody, hit.Value.Body);
            Assert.IsTrue(hit.Value.Distance <= 5.0f);
        }
        
        [TestMethod]
        public void Raycast_MissesWhenNoIntersection()
        {
            // ARRANGE
            _testBody.Position = new Vector3(10, 0, 0);
            _physics.AddBody(_testBody);
            
            var rayOrigin = new Vector3(0, 0, 0);
            var rayDirection = Vector3.UnitZ;
            var maxDistance = 5.0f;
            
            // ACT
            var hit = _physics.Raycast(rayOrigin, rayDirection, maxDistance);
            
            // ASSERT
            Assert.IsFalse(hit.HasValue);
        }
        
        [TestMethod]
        public void SetWorldBounds_LimitsBodyMovement()
        {
            // ARRANGE
            var bounds = new BoundingBox3D
            {
                Min = new Vector3(-10, -10, -10),
                Max = new Vector3(10, 10, 10)
            };
            
            _physics.SetWorldBounds(bounds);
            _testBody.Position = new Vector3(0, 0, 0);
            _testBody.Velocity = new Vector3(100, 0, 0); // High velocity toward boundary
            _physics.AddBody(_testBody);
            
            // ACT
            _physics.Update(1.0f); // Should hit boundary
            
            // ASSERT
            Assert.IsTrue(_testBody.Position.X <= bounds.Max.X);
            Assert.IsTrue(_testBody.Position.X >= bounds.Min.X);
        }
        
        [TestMethod]
        public void PerformanceTest_HandlesMany Bodies()
        {
            // ARRANGE
            var bodies = new MockPhysicsBody3D[1000];
            var random = new Random(12345);
            
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i] = new MockPhysicsBody3D
                {
                    Position = new Vector3(
                        (float)(random.NextDouble() - 0.5) * 200,
                        (float)(random.NextDouble() - 0.5) * 200,
                        (float)(random.NextDouble() - 0.5) * 200
                    ),
                    Velocity = Vector3.Zero,
                    Mass = 1.0f,
                    Radius = 1.0f
                };
                _physics.AddBody(bodies[i]);
            }
            
            // ACT
            var startTime = DateTime.Now;
            _physics.Update(0.016f); // Single frame update
            var endTime = DateTime.Now;
            
            // ASSERT
            var updateTime = (endTime - startTime).TotalMilliseconds;
            Assert.IsTrue(updateTime < 16.0); // Should complete within frame budget
        }
    }
    
    // Mock physics body for testing
    public class MockPhysicsBody3D : IPhysicsBody3D
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        public float Mass { get; set; } = 1.0f;
        public float Radius { get; set; } = 1.0f;
        public bool IsStatic { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public object UserData { get; set; }
        
        public BoundingBox3D GetBoundingBox()
        {
            return new BoundingBox3D
            {
                Min = Position - Vector3.One * Radius,
                Max = Position + Vector3.One * Radius
            };
        }
        
        public void OnCollision(IPhysicsBody3D other, Vector3 contactPoint, Vector3 contactNormal)
        {
            // Mock collision response
        }
    }
}