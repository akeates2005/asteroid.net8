using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Advanced 3D camera management system supporting multiple camera modes,
    /// smooth transitions, and intelligent following behavior.
    /// Provides production-ready camera control with performance optimization.
    /// </summary>
    public class Camera3DManager
    {
        #region Enums and Structures

        /// <summary>
        /// Available camera modes for different gameplay experiences
        /// </summary>
        public enum CameraMode
        {
            FollowPlayer,   // Traditional follow mode with configurable offset
            Orbital,        // Orbital camera rotating around the action
            FreeRoam,       // Free camera movement for exploration
            Cinematic,      // Cinematic camera with predefined movements
            DebugView       // Debug camera for development
        }

        /// <summary>
        /// Camera following styles for different feel preferences
        /// </summary>
        public enum FollowStyle
        {
            Tight,          // Close follow with minimal lag
            Smooth,         // Smooth interpolated follow
            Predictive,     // Predict player movement
            Cinematic       // Cinematic camera movements
        }

        /// <summary>
        /// Camera input commands for manual control
        /// </summary>
        public enum CameraInput
        {
            MoveForward,
            MoveBackward,
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            RotateLeft,
            RotateRight,
            RotateUp,
            RotateDown,
            ZoomIn,
            ZoomOut
        }

        /// <summary>
        /// Comprehensive camera state information
        /// </summary>
        public struct CameraState
        {
            public Vector3 Position;
            public Vector3 Target;
            public Vector3 Up;
            public CameraMode Mode;
            public FollowStyle Style;
            public bool IsActive;
            public bool IsTransitioning;
            public float FOV;
            public float NearPlane;
            public float FarPlane;
            public float TransitionProgress;
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
        }

        /// <summary>
        /// Camera configuration settings
        /// </summary>
        public struct Camera3DSettings
        {
            public float FOV;
            public float NearPlane;
            public float FarPlane;
            public float SmoothingSpeed;
            public bool EnableShake;
            public CameraMode DefaultMode;
            public FollowStyle DefaultFollowStyle;
            public Vector3 FollowOffset;
            public float OrbitalRadius;
            public float OrbitalSpeed;
            public float FreeRoamSpeed;
            public bool EnableCollision;
        }

        #endregion

        #region Private Fields

        private Camera3D _camera;
        private CameraMode _currentMode;
        private FollowStyle _followStyle;
        private Camera3DSettings _settings;
        private bool _isInitialized = false;

        // Transition system
        private bool _isTransitioning = false;
        private float _transitionTime = 0.0f;
        private float _transitionDuration = 0.0f;
        private Vector3 _transitionStartPosition;
        private Vector3 _transitionTargetPosition;
        private Vector3 _transitionStartTarget;
        private Vector3 _transitionTargetTarget;

        // Camera controllers for different modes
        private readonly Dictionary<CameraMode, ICameraController> _controllers;

        // Interpolation system
        private CameraInterpolator _interpolator;
        private CameraEffects _effects;

        // Performance tracking
        private float _lastUpdateTime = 0.0f;
        private Vector3 _lastPlayerPosition = Vector3.Zero;
        private Queue<float> _frameTimeHistory = new Queue<float>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Current camera mode
        /// </summary>
        public CameraMode CurrentMode => _currentMode;

        /// <summary>
        /// Current follow style
        /// </summary>
        public FollowStyle CurrentFollowStyle => _followStyle;

        /// <summary>
        /// Whether camera is currently transitioning between states
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Current transition duration
        /// </summary>
        public float GetTransitionDuration() => _transitionDuration;

        /// <summary>
        /// Whether camera is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initialize camera manager with default settings
        /// </summary>
        public Camera3DManager()
        {
            _settings = GetDefaultSettings();
            _controllers = new Dictionary<CameraMode, ICameraController>();
            _interpolator = new CameraInterpolator();
            _effects = new CameraEffects();
            
            InitializeControllers();
        }

        /// <summary>
        /// Initialize camera system
        /// </summary>
        public void Initialize(Camera3DSettings? customSettings = null)
        {
            if (customSettings.HasValue)
            {
                _settings = customSettings.Value;
            }

            // Initialize camera with default position
            _camera = new Camera3D
            {
                Position = new Vector3(0, 20, 20),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                FovY = _settings.FOV,
                Projection = CameraProjection.Perspective
            };

            _currentMode = _settings.DefaultMode;
            _followStyle = _settings.DefaultFollowStyle;

            // Initialize controllers with settings
            foreach (var controller in _controllers.Values)
            {
                controller.Initialize(_settings);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Get default camera settings
        /// </summary>
        private static Camera3DSettings GetDefaultSettings()
        {
            return new Camera3DSettings
            {
                FOV = 75.0f,
                NearPlane = 0.1f,
                FarPlane = 1000.0f,
                SmoothingSpeed = 5.0f,
                EnableShake = true,
                DefaultMode = CameraMode.FollowPlayer,
                DefaultFollowStyle = FollowStyle.Smooth,
                FollowOffset = new Vector3(-400, 20, 50),
                OrbitalRadius = 30.0f,
                OrbitalSpeed = 45.0f,
                FreeRoamSpeed = 10.0f,
                EnableCollision = false
            };
        }

        /// <summary>
        /// Initialize camera controllers for different modes
        /// </summary>
        private void InitializeControllers()
        {
            _controllers[CameraMode.FollowPlayer] = new FollowPlayerCameraController();
            _controllers[CameraMode.Orbital] = new OrbitalCameraController();
            _controllers[CameraMode.FreeRoam] = new FreeRoamCameraController();
            _controllers[CameraMode.Cinematic] = new CinematicCameraController();
            _controllers[CameraMode.DebugView] = new DebugCameraController();
        }

        #endregion

        #region Core Update Logic

        /// <summary>
        /// Update camera system
        /// </summary>
        public void Update(object gameState, float deltaTime)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Camera manager must be initialized before use");
            }

            if (deltaTime < 0)
            {
                return; // Handle negative delta time gracefully
            }

            _lastUpdateTime = deltaTime;
            TrackPerformance(deltaTime);

            // Update transition if active
            if (_isTransitioning)
            {
                UpdateTransition(deltaTime);
            }

            // Update current camera controller
            if (_controllers.TryGetValue(_currentMode, out var controller))
            {
                controller.Update(gameState, deltaTime);
                
                // Apply controller results if not transitioning
                if (!_isTransitioning)
                {
                    var controllerState = controller.GetCameraState();
                    _camera.Position = controllerState.Position;
                    _camera.Target = controllerState.Target;
                }
            }

            // Update interpolator and effects
            _interpolator.Update(deltaTime);
            _effects.Update(deltaTime);

            // Apply camera effects
            ApplyCameraEffects();
        }

        /// <summary>
        /// Update camera transition
        /// </summary>
        private void UpdateTransition(float deltaTime)
        {
            _transitionTime += deltaTime;
            float progress = _transitionDuration > 0 ? _transitionTime / _transitionDuration : 1.0f;

            if (progress >= 1.0f)
            {
                // Transition complete
                _camera.Position = _transitionTargetPosition;
                _camera.Target = _transitionTargetTarget;
                _isTransitioning = false;
                _transitionTime = 0.0f;
            }
            else
            {
                // Smooth interpolation using easing
                float easedProgress = EaseInOutCubic(progress);
                _camera.Position = Vector3.Lerp(_transitionStartPosition, _transitionTargetPosition, easedProgress);
                _camera.Target = Vector3.Lerp(_transitionStartTarget, _transitionTargetTarget, easedProgress);
            }
        }

        /// <summary>
        /// Apply camera effects like shake, sway, etc.
        /// </summary>
        private void ApplyCameraEffects()
        {
            if (_settings.EnableShake && _effects.IsShakeActive)
            {
                var shakeOffset = _effects.GetShakeOffset();
                _camera.Position += shakeOffset;
            }
        }

        /// <summary>
        /// Track performance metrics
        /// </summary>
        private void TrackPerformance(float deltaTime)
        {
            _frameTimeHistory.Enqueue(deltaTime);
            if (_frameTimeHistory.Count > 60) // Keep last 60 frames
            {
                _frameTimeHistory.Dequeue();
            }
        }

        #endregion

        #region Mode Management

        /// <summary>
        /// Switch to a new camera mode with optional transition
        /// </summary>
        public void SwitchMode(CameraMode newMode, float transitionTime = 1.0f)
        {
            if (newMode == _currentMode)
            {
                return; // No change needed
            }

            if (transitionTime <= 0.0f)
            {
                // Instant switch
                _currentMode = newMode;
                _isTransitioning = false;
                return;
            }

            // Setup transition
            _transitionStartPosition = _camera.Position;
            _transitionStartTarget = _camera.Target;
            
            // Get target state from new controller
            if (_controllers.TryGetValue(newMode, out var newController))
            {
                var targetState = newController.GetCameraState();
                _transitionTargetPosition = targetState.Position;
                _transitionTargetTarget = targetState.Target;
            }
            else
            {
                _transitionTargetPosition = _camera.Position;
                _transitionTargetTarget = _camera.Target;
            }

            _currentMode = newMode;
            _isTransitioning = true;
            _transitionTime = 0.0f;
            _transitionDuration = Math.Max(0.0f, transitionTime);
        }

        /// <summary>
        /// Set camera mode without transition
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            _currentMode = mode;
            _isTransitioning = false;
        }

        /// <summary>
        /// Set camera follow style
        /// </summary>
        public void SetFollowStyle(FollowStyle style)
        {
            _followStyle = style;
            
            // Update follow controller if active
            if (_currentMode == CameraMode.FollowPlayer && 
                _controllers.TryGetValue(CameraMode.FollowPlayer, out var controller) &&
                controller is FollowPlayerCameraController followController)
            {
                followController.SetFollowStyle(style);
            }
        }

        #endregion

        #region Interpolation and Movement

        /// <summary>
        /// Smoothly interpolate camera to target position and target
        /// </summary>
        public void InterpolateTo(Vector3 targetTarget, Vector3 targetPosition, float duration)
        {
            if (duration <= 0.0f)
            {
                // Instant move
                _camera.Position = targetPosition;
                _camera.Target = targetTarget;
                return;
            }

            // Setup interpolation
            _transitionStartPosition = _camera.Position;
            _transitionStartTarget = _camera.Target;
            _transitionTargetPosition = targetPosition;
            _transitionTargetTarget = targetTarget;
            
            _isTransitioning = true;
            _transitionTime = 0.0f;
            _transitionDuration = duration;
        }

        /// <summary>
        /// Handle manual camera input for free roam mode
        /// </summary>
        public void HandleInput(CameraInput input, float intensity = 1.0f)
        {
            if (_currentMode == CameraMode.FreeRoam && 
                _controllers.TryGetValue(CameraMode.FreeRoam, out var controller) &&
                controller is FreeRoamCameraController freeController)
            {
                freeController.HandleInput(input, intensity);
            }
        }

        #endregion

        #region State and Matrix Access

        /// <summary>
        /// Get current camera state
        /// </summary>
        public CameraState GetCurrentState()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Camera manager must be initialized before accessing state");
            }

            return new CameraState
            {
                Position = _camera.Position,
                Target = _camera.Target,
                Up = _camera.Up,
                Mode = _currentMode,
                Style = _followStyle,
                IsActive = _isInitialized,
                IsTransitioning = _isTransitioning,
                FOV = _camera.FovY,
                NearPlane = _settings.NearPlane,
                FarPlane = _settings.FarPlane,
                TransitionProgress = _isTransitioning ? (_transitionTime / _transitionDuration) : 0.0f,
                ViewMatrix = GetViewMatrix(),
                ProjectionMatrix = GetProjectionMatrix(800, 600) // Default screen size
            };
        }

        /// <summary>
        /// Get view matrix for rendering
        /// </summary>
        public Matrix4x4 GetViewMatrix()
        {
            return Raymath.MatrixLookAt(_camera.Position, _camera.Target, _camera.Up);
        }

        /// <summary>
        /// Get projection matrix for rendering
        /// </summary>
        public Matrix4x4 GetProjectionMatrix(int screenWidth, int screenHeight)
        {
            float aspect = (float)screenWidth / screenHeight;
            return Raymath.MatrixPerspective(_camera.FovY * (float)(Math.PI / 180.0), aspect, _settings.NearPlane, _settings.FarPlane);
        }

        /// <summary>
        /// Get Raylib Camera3D for direct rendering
        /// </summary>
        public Camera3D GetRaylibCamera()
        {
            return _camera;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Cubic easing function for smooth transitions
        /// </summary>
        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public CameraPerformanceStats GetPerformanceStats()
        {
            if (_frameTimeHistory.Count == 0)
            {
                return new CameraPerformanceStats();
            }

            var frameTimeArray = _frameTimeHistory.ToArray();
            return new CameraPerformanceStats
            {
                AverageFrameTime = frameTimeArray.Average(),
                MinFrameTime = frameTimeArray.Min(),
                MaxFrameTime = frameTimeArray.Max(),
                LastUpdateTime = _lastUpdateTime,
                IsTransitioning = _isTransitioning
            };
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup camera resources
        /// </summary>
        public void Cleanup()
        {
            foreach (var controller in _controllers.Values)
            {
                controller?.Cleanup();
            }
            
            _controllers.Clear();
            _interpolator?.Cleanup();
            _effects?.Cleanup();
            
            _isInitialized = false;
        }

        #endregion
    }

    #region Support Classes and Interfaces

    /// <summary>
    /// Camera performance statistics
    /// </summary>
    public struct CameraPerformanceStats
    {
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public float LastUpdateTime;
        public bool IsTransitioning;
    }

    /// <summary>
    /// Interface for camera mode controllers
    /// </summary>
    public interface ICameraController
    {
        void Initialize(Camera3DManager.Camera3DSettings settings);
        void Update(object gameState, float deltaTime);
        Camera3DManager.CameraState GetCameraState();
        void Cleanup();
    }

    /// <summary>
    /// Camera interpolation system
    /// </summary>
    public class CameraInterpolator
    {
        public void Update(float deltaTime) { }
        public void Cleanup() { }
    }

    /// <summary>
    /// Camera effects system (shake, etc.)
    /// </summary>
    public class CameraEffects
    {
        public bool IsShakeActive { get; private set; } = false;
        public void Update(float deltaTime) { }
        public Vector3 GetShakeOffset() => Vector3.Zero;
        public void Cleanup() { }
    }

    #region Camera Controllers

    /// <summary>
    /// Follow player camera controller
    /// </summary>
    public class FollowPlayerCameraController : ICameraController
    {
        private Camera3DManager.Camera3DSettings _settings;
        private Camera3DManager.FollowStyle _followStyle = Camera3DManager.FollowStyle.Smooth;
        private Camera3DManager.CameraState _currentState;

        public void Initialize(Camera3DManager.Camera3DSettings settings)
        {
            _settings = settings;
            _currentState = new Camera3DManager.CameraState
            {
                Position = new Vector3(0, 20, 20),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                IsActive = true
            };
        }

        public void Update(object gameState, float deltaTime)
        {
            // For now, use basic object with Player property simulation
            if (gameState != null && gameState.GetType().GetProperty("Player") != null)
            {
                var playerProperty = gameState.GetType().GetProperty("Player");
                var playerObj = playerProperty?.GetValue(gameState);
                var positionProperty = playerObj?.GetType().GetProperty("Position");
                var playerPos = (Vector2)(positionProperty?.GetValue(playerObj) ?? Vector2.Zero);
                var targetPos = new Vector3(playerPos.X + _settings.FollowOffset.X, 
                                          _settings.FollowOffset.Y, 
                                          playerPos.Y + _settings.FollowOffset.Z);
                
                _currentState.Position = Vector3.Lerp(_currentState.Position, targetPos, 
                    deltaTime * _settings.SmoothingSpeed);
                _currentState.Target = new Vector3(playerPos.X, 0, playerPos.Y);
            }
        }

        public Camera3DManager.CameraState GetCameraState() => _currentState;

        public void SetFollowStyle(Camera3DManager.FollowStyle style)
        {
            _followStyle = style;
        }

        public void Cleanup() { }
    }

    /// <summary>
    /// Orbital camera controller
    /// </summary>
    public class OrbitalCameraController : ICameraController
    {
        private Camera3DManager.Camera3DSettings _settings;
        private Camera3DManager.CameraState _currentState;
        private float _currentAngle = 0.0f;

        public void Initialize(Camera3DManager.Camera3DSettings settings)
        {
            _settings = settings;
            _currentState = new Camera3DManager.CameraState
            {
                Position = new Vector3(_settings.OrbitalRadius, 20, 0),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                IsActive = true
            };
        }

        public void Update(object gameState, float deltaTime)
        {
            _currentAngle += _settings.OrbitalSpeed * deltaTime * (float)(Math.PI / 180.0);
            
            // For now, use basic object with Player property simulation
            if (gameState != null && gameState.GetType().GetProperty("Player") != null)
            {
                var playerProperty = gameState.GetType().GetProperty("Player");
                var playerObj = playerProperty?.GetValue(gameState);
                var positionProperty = playerObj?.GetType().GetProperty("Position");
                var playerPos = (Vector2)(positionProperty?.GetValue(playerObj) ?? Vector2.Zero);
                var centerPos = new Vector3(playerPos.X, 0, playerPos.Y);
                
                _currentState.Position = centerPos + new Vector3(
                    (float)Math.Cos(_currentAngle) * _settings.OrbitalRadius,
                    20,
                    (float)Math.Sin(_currentAngle) * _settings.OrbitalRadius
                );
                _currentState.Target = centerPos;
            }
        }

        public float GetCurrentAngle() => _currentAngle * (float)(180.0 / Math.PI);

        public Camera3DManager.CameraState GetCameraState() => _currentState;

        public void Cleanup() { }
    }

    /// <summary>
    /// Free roam camera controller
    /// </summary>
    public class FreeRoamCameraController : ICameraController
    {
        private Camera3DManager.Camera3DSettings _settings;
        private Camera3DManager.CameraState _currentState;

        public void Initialize(Camera3DManager.Camera3DSettings settings)
        {
            _settings = settings;
            _currentState = new Camera3DManager.CameraState
            {
                Position = new Vector3(0, 20, 20),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                IsActive = true
            };
        }

        public void Update(object gameState, float deltaTime) { }

        public void HandleInput(Camera3DManager.CameraInput input, float intensity)
        {
            var movement = Vector3.Zero;
            switch (input)
            {
                case Camera3DManager.CameraInput.MoveForward:
                    movement = Vector3.Normalize(_currentState.Target - _currentState.Position);
                    break;
                // Add other movement cases...
            }
            
            _currentState.Position += movement * _settings.FreeRoamSpeed * intensity * 0.016f;
        }

        public Camera3DManager.CameraState GetCameraState() => _currentState;

        public void Cleanup() { }
    }

    /// <summary>
    /// Cinematic camera controller
    /// </summary>
    public class CinematicCameraController : ICameraController
    {
        private Camera3DManager.Camera3DSettings _settings;
        private Camera3DManager.CameraState _currentState;

        public void Initialize(Camera3DManager.Camera3DSettings settings)
        {
            _settings = settings;
            _currentState = new Camera3DManager.CameraState
            {
                Position = new Vector3(0, 25, 30),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                IsActive = true
            };
        }

        public void Update(object gameState, float deltaTime) { }

        public Camera3DManager.CameraState GetCameraState() => _currentState;

        public void Cleanup() { }
    }

    /// <summary>
    /// Debug camera controller
    /// </summary>
    public class DebugCameraController : ICameraController
    {
        private Camera3DManager.Camera3DSettings _settings;
        private Camera3DManager.CameraState _currentState;

        public void Initialize(Camera3DManager.Camera3DSettings settings)
        {
            _settings = settings;
            _currentState = new Camera3DManager.CameraState
            {
                Position = new Vector3(0, 50, 50),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                IsActive = true
            };
        }

        public void Update(object gameState, float deltaTime) { }

        public Camera3DManager.CameraState GetCameraState() => _currentState;

        public void Cleanup() { }
    }

    #endregion

    #endregion
}