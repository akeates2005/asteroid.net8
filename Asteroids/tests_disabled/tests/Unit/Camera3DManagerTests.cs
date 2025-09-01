using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Asteroids;

namespace Asteroids.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for Camera3DManager following TDD methodology.
    /// Tests camera initialization, mode switching, interpolation, and state management.
    /// </summary>
    [TestClass]
    public class Camera3DManagerTests
    {
        private Camera3DManager _cameraManager;
        private MockGameState _gameState;

        [TestInitialize]
        public void Setup()
        {
            _cameraManager = new Camera3DManager();
            _gameState = new MockGameState();
            _gameState.Player.Position = Vector2.Zero;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cameraManager?.Cleanup();
        }

        #region Initialization Tests

        [TestMethod]
        public void Initialize_SetsDefaultCameraPosition()
        {
            // ARRANGE
            var expectedPosition = new Vector3(0, 20, 20);
            var expectedTarget = Vector3.Zero;

            // ACT
            _cameraManager.Initialize();
            var state = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.AreEqual(expectedPosition, state.Position, "Camera should initialize at default position");
            Assert.AreEqual(expectedTarget, state.Target, "Camera should target origin");
            Assert.AreEqual(CameraMode.FollowPlayer, state.Mode, "Should start in FollowPlayer mode");
            Assert.IsTrue(state.IsActive, "Camera should be active after initialization");
            Assert.IsFalse(state.IsTransitioning, "Camera should not be transitioning initially");
        }

        [TestMethod]
        public void Initialize_WithCustomSettings_AppliesCorrectValues()
        {
            // ARRANGE
            var customSettings = new Camera3DSettings
            {
                FOV = 90.0f,
                NearPlane = 0.5f,
                FarPlane = 500.0f,
                DefaultMode = CameraMode.Orbital,
                SmoothingSpeed = 8.0f
            };

            // ACT
            _cameraManager.Initialize(customSettings);
            var state = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.AreEqual(90.0f, state.FOV, 0.01f, "FOV should match settings");
            Assert.AreEqual(0.5f, state.NearPlane, 0.01f, "Near plane should match settings");
            Assert.AreEqual(500.0f, state.FarPlane, 0.01f, "Far plane should match settings");
            Assert.AreEqual(CameraMode.Orbital, state.Mode, "Should start in specified mode");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetCurrentState_BeforeInitialization_ThrowsException()
        {
            // ACT & ASSERT
            _cameraManager.GetCurrentState();
        }

        #endregion

        #region Mode Switching Tests

        [TestMethod]
        public void SwitchMode_FromFollowToOrbital_TransitionsCorrectly()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);
            Assert.AreEqual(CameraMode.FollowPlayer, _cameraManager.CurrentMode);

            // ACT
            _cameraManager.SwitchMode(CameraMode.Orbital, 1.0f);

            // ASSERT
            Assert.AreEqual(CameraMode.Orbital, _cameraManager.CurrentMode, "Mode should change immediately");
            Assert.IsTrue(_cameraManager.IsTransitioning, "Should be transitioning");
            Assert.AreEqual(1.0f, _cameraManager.GetTransitionDuration(), 0.01f, "Transition duration should match");
        }

        [TestMethod]
        public void SwitchMode_ToSameMode_NoTransition()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);

            // ACT
            _cameraManager.SwitchMode(CameraMode.FollowPlayer, 2.0f);

            // ASSERT
            Assert.AreEqual(CameraMode.FollowPlayer, _cameraManager.CurrentMode);
            Assert.IsFalse(_cameraManager.IsTransitioning, "Should not transition to same mode");
        }

        [TestMethod]
        public void SwitchMode_WithZeroTransitionTime_InstantSwitch()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);

            // ACT
            _cameraManager.SwitchMode(CameraMode.FreeRoam, 0.0f);

            // ASSERT
            Assert.AreEqual(CameraMode.FreeRoam, _cameraManager.CurrentMode);
            Assert.IsFalse(_cameraManager.IsTransitioning, "Should not be transitioning with zero time");
        }

        [TestMethod]
        public void SwitchMode_AllSupportedModes_CompletesSuccessfully()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var modes = new[] { CameraMode.FollowPlayer, CameraMode.Orbital, CameraMode.FreeRoam, CameraMode.Cinematic };

            // ACT & ASSERT
            foreach (var mode in modes)
            {
                _cameraManager.SwitchMode(mode, 0.1f);
                Assert.AreEqual(mode, _cameraManager.CurrentMode, $"Should switch to {mode}");

                // Complete transition
                _cameraManager.Update(_gameState, 0.2f);
                Assert.IsFalse(_cameraManager.IsTransitioning, $"Transition to {mode} should complete");
            }
        }

        #endregion

        #region Interpolation Tests

        [TestMethod]
        public void InterpolateTo_SmoothlyMovesCamera()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var targetPosition = new Vector3(10, 15, 25);
            var targetTarget = new Vector3(5, 0, 5);
            var initialState = _cameraManager.GetCurrentState();

            // ACT
            _cameraManager.InterpolateTo(targetTarget, targetPosition, 2.0f);

            // Simulate time passage at 60 FPS
            var totalFrames = 120; // 2 seconds
            for (int i = 0; i < totalFrames; i++)
            {
                _cameraManager.Update(_gameState, 1.0f / 60.0f);
            }

            var finalState = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.IsTrue(Vector3.Distance(targetPosition, finalState.Position) < 0.1f, 
                "Camera should reach target position");
            Assert.IsTrue(Vector3.Distance(targetTarget, finalState.Target) < 0.1f, 
                "Camera should reach target focus");
            Assert.IsFalse(finalState.IsTransitioning, "Interpolation should be complete");
        }

        [TestMethod]
        public void InterpolateTo_WithZeroDuration_InstantMove()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var targetPosition = new Vector3(10, 15, 25);
            var targetTarget = new Vector3(5, 0, 5);

            // ACT
            _cameraManager.InterpolateTo(targetTarget, targetPosition, 0.0f);
            var state = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.AreEqual(targetPosition, state.Position, "Should move instantly to target position");
            Assert.AreEqual(targetTarget, state.Target, "Should focus instantly on target");
            Assert.IsFalse(state.IsTransitioning, "Should not be transitioning with zero duration");
        }

        [TestMethod]
        public void InterpolateTo_InterruptPreviousInterpolation_StartsNewOne()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var firstTarget = new Vector3(5, 5, 5);
            var secondTarget = new Vector3(10, 10, 10);

            // ACT
            _cameraManager.InterpolateTo(Vector3.Zero, firstTarget, 2.0f);
            _cameraManager.Update(_gameState, 0.5f); // Partial progress

            var midState = _cameraManager.GetCurrentState();
            Assert.IsTrue(midState.IsTransitioning, "Should be transitioning");

            _cameraManager.InterpolateTo(Vector3.Zero, secondTarget, 1.0f);

            // Complete new interpolation
            for (int i = 0; i < 60; i++)
            {
                _cameraManager.Update(_gameState, 1.0f / 60.0f);
            }

            var finalState = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.IsTrue(Vector3.Distance(secondTarget, finalState.Position) < 0.1f, 
                "Should reach second target, not first");
            Assert.IsFalse(finalState.IsTransitioning, "New interpolation should complete");
        }

        #endregion

        #region Camera Mode Controllers Tests

        [TestMethod]
        public void FollowPlayerMode_TracksPlayerPosition()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FollowPlayer);
            _gameState.Player.Position = new Vector2(100, 200);

            // ACT
            _cameraManager.Update(_gameState, 0.016f);
            var cameraState = _cameraManager.GetCurrentState();

            // ASSERT
            var expectedPosition = new Vector3(100 - 400, 20, 200 - 300 + 50); // Follow offset
            Assert.IsTrue(Vector3.Distance(expectedPosition, cameraState.Position) < 2.0f, 
                "Camera should follow player with appropriate offset");
        }

        [TestMethod]
        public void OrbitalMode_RotatesAroundPlayer()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.Orbital);
            _gameState.Player.Position = Vector2.Zero;

            var initialState = _cameraManager.GetCurrentState();
            var initialAngle = Math.Atan2(initialState.Position.Z, initialState.Position.X);

            // ACT
            _cameraManager.Update(_gameState, 2.0f); // 2 seconds
            var finalState = _cameraManager.GetCurrentState();
            var finalAngle = Math.Atan2(finalState.Position.Z, finalState.Position.X);

            // ASSERT
            var angleDifference = Math.Abs(finalAngle - initialAngle);
            Assert.IsTrue(angleDifference > 0.5, "Camera should have rotated around player");
            
            // Camera should maintain distance from player
            var playerPos3D = new Vector3(_gameState.Player.Position.X, 0, _gameState.Player.Position.Y);
            var initialDistance = Vector3.Distance(initialState.Position, playerPos3D);
            var finalDistance = Vector3.Distance(finalState.Position, playerPos3D);
            Assert.AreEqual(initialDistance, finalDistance, 5.0f, "Orbital distance should remain consistent");
        }

        [TestMethod]
        public void FreeRoamMode_AllowsManualControl()
        {
            // ARRANGE
            _cameraManager.Initialize();
            _cameraManager.SetMode(CameraMode.FreeRoam);
            var initialState = _cameraManager.GetCurrentState();

            // ACT
            _cameraManager.HandleInput(CameraInput.MoveForward, 1.0f);
            _cameraManager.Update(_gameState, 0.016f);
            var newState = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.AreNotEqual(initialState.Position, newState.Position, 
                "Free roam should allow position changes via input");
        }

        #endregion

        #region Performance and Stability Tests

        [TestMethod]
        public void Update_HighFrequencyUpdates_RemainsStable()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var initialState = _cameraManager.GetCurrentState();

            // ACT
            for (int i = 0; i < 1000; i++)
            {
                _gameState.Player.Position = new Vector2(
                    (float)Math.Sin(i * 0.1f) * 100,
                    (float)Math.Cos(i * 0.1f) * 100
                );
                _cameraManager.Update(_gameState, 0.001f); // 1000 FPS simulation
            }

            var finalState = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.IsTrue(finalState.IsActive, "Camera should remain active");
            Assert.IsFalse(float.IsNaN(finalState.Position.X), "Position should not become NaN");
            Assert.IsFalse(float.IsNaN(finalState.Position.Y), "Position should not become NaN");
            Assert.IsFalse(float.IsNaN(finalState.Position.Z), "Position should not become NaN");
        }

        [TestMethod]
        public void GetRenderMatrix_ConsistentResults()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var state = _cameraManager.GetCurrentState();

            // ACT
            var matrix1 = _cameraManager.GetViewMatrix();
            var matrix2 = _cameraManager.GetViewMatrix();
            var projMatrix1 = _cameraManager.GetProjectionMatrix(800, 600);
            var projMatrix2 = _cameraManager.GetProjectionMatrix(800, 600);

            // ASSERT
            Assert.AreEqual(matrix1, matrix2, "View matrix should be consistent");
            Assert.AreEqual(projMatrix1, projMatrix2, "Projection matrix should be consistent");
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void Update_WithNullGameState_HandlesGracefully()
        {
            // ARRANGE
            _cameraManager.Initialize();

            // ACT & ASSERT
            try
            {
                _cameraManager.Update(null, 0.016f);
                Assert.IsTrue(_cameraManager.GetCurrentState().IsActive, "Camera should remain active");
            }
            catch
            {
                Assert.Fail("Should handle null game state gracefully");
            }
        }

        [TestMethod]
        public void Update_WithNegativeDeltaTime_HandlesGracefully()
        {
            // ARRANGE
            _cameraManager.Initialize();
            var initialState = _cameraManager.GetCurrentState();

            // ACT
            _cameraManager.Update(_gameState, -0.016f);
            var finalState = _cameraManager.GetCurrentState();

            // ASSERT
            Assert.AreEqual(initialState.Position, finalState.Position, "Position should not change with negative delta");
        }

        [TestMethod]
        public void SwitchMode_WithNegativeTransitionTime_UsesZero()
        {
            // ARRANGE
            _cameraManager.Initialize();

            // ACT
            _cameraManager.SwitchMode(CameraMode.Orbital, -1.0f);

            // ASSERT
            Assert.IsFalse(_cameraManager.IsTransitioning, "Should not transition with negative time");
            Assert.AreEqual(CameraMode.Orbital, _cameraManager.CurrentMode, "Mode should still change");
        }

        #endregion
    }

    #region Test Support Classes

    /// <summary>
    /// Mock game state for testing camera functionality
    /// </summary>
    public class MockGameState
    {
        public Player Player { get; set; } = new Player();
        public List<Asteroid> Asteroids { get; set; } = new List<Asteroid>();
        public List<EnemyShip> Enemies { get; set; } = new List<EnemyShip>();
        public float GameTime { get; set; } = 0.0f;
    }

    #endregion
}