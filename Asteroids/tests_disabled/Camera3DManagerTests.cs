using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asteroids;

namespace Asteroids.Tests.Unit
{
    [TestClass]
    public class Camera3DManagerTests
    {
        private Camera3DManager _cameraManager;
        private MockGameState _mockGameState;
        
        [TestInitialize]
        public void Setup()
        {
            _cameraManager = new Camera3DManager();
            _mockGameState = new MockGameState();
        }
        
        [TestCleanup]
        public void Cleanup()
        {
            _cameraManager?.Cleanup();
        }
        
        [TestMethod]
        public void Initialize_SetsDefaultCameraPosition()
        {
            // ARRANGE
            var expectedPosition = new Vector3(0, 20, 20);
            var expectedTarget = Vector3.Zero;
            
            // ACT
            var result = _cameraManager.Initialize();
            var state = _cameraManager.GetCurrentState();
            
            // ASSERT
            Assert.IsTrue(result);
            Assert.AreEqual(expectedPosition, state.Position);
            Assert.AreEqual(expectedTarget, state.Target);
            Assert.AreEqual(CameraMode.FollowPlayer, state.Mode);
            Assert.IsTrue(state.IsActive);
        }
        
        [TestMethod]
        public void SwitchMode_TransitionsFromFollowToOrbital()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);
            
            // ACT
            _cameraManager.SwitchMode(CameraMode.Orbital, 1.0f);
            
            // ASSERT
            Assert.AreEqual(CameraMode.Orbital, _cameraManager.CurrentMode);
            Assert.IsTrue(_cameraManager.IsTransitioning);
        }
        
        [TestMethod]
        public void InterpolateTo_SmoothlyMovesCamera()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var targetPosition = new Vector3(10, 15, 25);
            var initialPosition = _cameraManager.GetCurrentState().Position;
            
            // ACT
            _cameraManager.InterpolateTo(targetPosition, 2.0f);
            
            // Simulate time passage
            for (float t = 0; t < 2.0f; t += 0.016f) // 60 FPS
            {
                _cameraManager.Update(_mockGameState, 0.016f);
            }
            
            var finalPosition = _cameraManager.GetCurrentState().Position;
            
            // ASSERT
            var tolerance = new Vector3(0.1f);
            Assert.IsTrue(Vector3.Distance(targetPosition, finalPosition) <= tolerance.X);
            Assert.IsFalse(_cameraManager.IsTransitioning);
        }
        
        [TestMethod]
        public void FollowPlayerMode_TracksPlayerPosition()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);
            _mockGameState.Player.Position = new Vector2(100, 200);
            
            // ACT
            _cameraManager.Update(_mockGameState, 0.016f);
            var cameraState = _cameraManager.GetCurrentState();
            
            // ASSERT
            var expectedPosition = new Vector3(100 - 400, 20, 200 - 300 + 50);
            var tolerance = 2.0f;
            Assert.IsTrue(Vector3.Distance(expectedPosition, cameraState.Position) <= tolerance);
        }
        
        [TestMethod]
        public void OrbitalMode_RotatesAroundPlayer()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.Orbital);
            _cameraManager.SetOrbitalRadius(30.0f);
            _cameraManager.SetOrbitalSpeed(45.0f); // degrees per second
            
            _mockGameState.Player.Position = Vector2.Zero;
            
            // ACT
            var initialAngle = _cameraManager.GetOrbitalAngle();
            _cameraManager.Update(_mockGameState, 1.0f); // 1 second
            var finalAngle = _cameraManager.GetOrbitalAngle();
            
            // ASSERT
            var expectedAngleDelta = 45.0f;
            var actualAngleDelta = Math.Abs(finalAngle - initialAngle);
            Assert.AreEqual(expectedAngleDelta, actualAngleDelta, 0.5f);
        }
        
        [TestMethod]
        public void FreeCameraMode_RespondsToInput()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.Free);
            var initialPosition = _cameraManager.GetCurrentState().Position;
            
            var input = new CameraInput
            {
                MoveForward = 1.0f,
                MoveRight = 0.5f,
                MoveUp = 0.0f,
                RotateX = 0.1f,
                RotateY = 0.2f
            };
            
            // ACT
            _cameraManager.ProcessInput(input, 0.016f);
            _cameraManager.Update(_mockGameState, 0.016f);
            var finalPosition = _cameraManager.GetCurrentState().Position;
            
            // ASSERT
            Assert.AreNotEqual(initialPosition, finalPosition);
        }
        
        [TestMethod]
        public void CinematicMode_FollowsPresetPath()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var waypoints = new Vector3[]
            {
                new Vector3(0, 20, 20),
                new Vector3(10, 25, 15),
                new Vector3(20, 30, 10)
            };
            
            _cameraManager.SetCinematicPath(waypoints, 3.0f);
            _cameraManager.SetMode(CameraMode.Cinematic);
            
            // ACT
            var startPosition = _cameraManager.GetCurrentState().Position;
            
            // Simulate halfway through the cinematic
            for (int i = 0; i < 90; i++) // 1.5 seconds at 60 FPS
            {
                _cameraManager.Update(_mockGameState, 0.016f);
            }
            
            var midPosition = _cameraManager.GetCurrentState().Position;
            
            // ASSERT
            Assert.AreNotEqual(startPosition, midPosition);
            // Should be somewhere between first and second waypoint
            var distanceFromStart = Vector3.Distance(waypoints[0], midPosition);
            var distanceFromMid = Vector3.Distance(waypoints[1], midPosition);
            Assert.IsTrue(distanceFromStart > 0 && distanceFromMid > 0);
        }
        
        [TestMethod]
        public void ShakeEffect_AppliesTemporaryDisplacement()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var originalPosition = _cameraManager.GetCurrentState().Position;
            
            // ACT
            _cameraManager.ApplyShake(1.0f, 0.5f); // High intensity, 0.5 second duration
            _cameraManager.Update(_mockGameState, 0.016f);
            var shakenPosition = _cameraManager.GetCurrentState().Position;
            
            // Wait for shake to end
            for (int i = 0; i < 35; i++) // Just over 0.5 seconds
            {
                _cameraManager.Update(_mockGameState, 0.016f);
            }
            var finalPosition = _cameraManager.GetCurrentState().Position;
            
            // ASSERT
            Assert.AreNotEqual(originalPosition, shakenPosition); // Should be displaced during shake
            var tolerance = 0.1f;
            Assert.IsTrue(Vector3.Distance(originalPosition, finalPosition) <= tolerance); // Should return to original
        }
    }
    
    // Mock classes for testing
    public class MockGameState
    {
        public Player Player { get; set; } = new Player();
    }
    
    public struct CameraInput
    {
        public float MoveForward;
        public float MoveRight;
        public float MoveUp;
        public float RotateX;
        public float RotateY;
    }
    
    public enum CameraMode
    {
        FollowPlayer,
        Orbital,
        Free,
        Cinematic,
        FirstPerson
    }
}