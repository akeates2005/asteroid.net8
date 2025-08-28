using System;
using System.Collections.Generic;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Centralized input handling with configurable key bindings and input validation.
    /// Provides consistent input processing and prevents key repeat issues.
    /// </summary>
    public class InputManager
    {
        private readonly Dictionary<string, KeyboardKey> _keyBindings;
        private readonly Dictionary<KeyboardKey, float> _keyTimers;
        private readonly Dictionary<string, Action> _actionHandlers;
        
        private const float KEY_REPEAT_DELAY = 0.2f; // 200ms delay for key repeats

        /// <summary>
        /// Event fired when movement input is detected
        /// </summary>
        public event Action<InputState>? MovementInput;
        
        /// <summary>
        /// Event fired when action input is detected
        /// </summary>
        public event Action<string>? ActionInput;

        public InputManager()
        {
            _keyBindings = new Dictionary<string, KeyboardKey>();
            _keyTimers = new Dictionary<KeyboardKey, float>();
            _actionHandlers = new Dictionary<string, Action>();
            
            InitializeDefaultBindings();
        }

        /// <summary>
        /// Update input state (should be called every frame)
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(float deltaTime)
        {
            UpdateKeyTimers(deltaTime);
            ProcessMovementInput();
            ProcessActionInput();
        }

        /// <summary>
        /// Bind a key to an action
        /// </summary>
        /// <param name="action">Action name</param>
        /// <param name="key">Keyboard key</param>
        public void BindKey(string action, KeyboardKey key)
        {
            _keyBindings[action] = key;
            ErrorManager.LogInfo($"Bound key {key} to action '{action}'");
        }

        /// <summary>
        /// Register an action handler
        /// </summary>
        /// <param name="action">Action name</param>
        /// <param name="handler">Action handler</param>
        public void RegisterActionHandler(string action, Action handler)
        {
            _actionHandlers[action] = handler;
        }

        /// <summary>
        /// Check if an action key is currently pressed
        /// </summary>
        /// <param name="action">Action name</param>
        /// <returns>True if key is pressed</returns>
        public bool IsActionPressed(string action)
        {
            return _keyBindings.TryGetValue(action, out var key) && Raylib.IsKeyDown(key);
        }

        /// <summary>
        /// Check if an action key was just pressed (with repeat prevention)
        /// </summary>
        /// <param name="action">Action name</param>
        /// <returns>True if key was just pressed</returns>
        public bool IsActionJustPressed(string action)
        {
            if (!_keyBindings.TryGetValue(action, out var key)) return false;
            
            if (Raylib.IsKeyPressed(key))
            {
                if (!_keyTimers.ContainsKey(key) || _keyTimers[key] <= 0)
                {
                    _keyTimers[key] = KEY_REPEAT_DELAY;
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Get current movement input state
        /// </summary>
        /// <returns>Current input state</returns>
        public InputState GetInputState()
        {
            return new InputState
            {
                ThrustInput = IsActionPressed("thrust"),
                LeftInput = IsActionPressed("turn_left"),
                RightInput = IsActionPressed("turn_right"),
                ShootInput = IsActionJustPressed("shoot"),
                ShieldInput = IsActionJustPressed("shield"),
                PauseInput = IsActionJustPressed("pause"),
                Toggle3DInput = IsActionJustPressed("toggle_3d")
            };
        }

        /// <summary>
        /// Get configured key binding for an action
        /// </summary>
        /// <param name="action">Action name</param>
        /// <returns>Bound key or null if not bound</returns>
        public KeyboardKey? GetKeyBinding(string action)
        {
            return _keyBindings.TryGetValue(action, out var key) ? key : null;
        }

        /// <summary>
        /// Reset all key timers
        /// </summary>
        public void Reset()
        {
            _keyTimers.Clear();
            ErrorManager.LogInfo("Input manager reset");
        }

        private void InitializeDefaultBindings()
        {
            // Default key bindings
            BindKey("thrust", KeyboardKey.Up);
            BindKey("turn_left", KeyboardKey.Left);
            BindKey("turn_right", KeyboardKey.Right);
            BindKey("shoot", KeyboardKey.Space);
            BindKey("shield", KeyboardKey.X);
            BindKey("pause", KeyboardKey.P);
            BindKey("toggle_3d", KeyboardKey.F3);
            
            ErrorManager.LogInfo("Default input bindings initialized");
        }

        private void UpdateKeyTimers(float deltaTime)
        {
            var keys = new List<KeyboardKey>(_keyTimers.Keys);
            foreach (var key in keys)
            {
                if (_keyTimers[key] > 0)
                {
                    _keyTimers[key] -= deltaTime;
                    if (_keyTimers[key] <= 0)
                    {
                        _keyTimers[key] = 0;
                    }
                }
            }
        }

        private void ProcessMovementInput()
        {
            var inputState = GetInputState();
            MovementInput?.Invoke(inputState);
        }

        private void ProcessActionInput()
        {
            foreach (var binding in _keyBindings)
            {
                if (IsActionJustPressed(binding.Key) && _actionHandlers.TryGetValue(binding.Key, out var handler))
                {
                    try
                    {
                        handler.Invoke();
                        ActionInput?.Invoke(binding.Key);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.LogError($"Error executing action handler for '{binding.Key}'", ex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Current input state snapshot
    /// </summary>
    public struct InputState
    {
        public bool ThrustInput { get; init; }
        public bool LeftInput { get; init; }
        public bool RightInput { get; init; }
        public bool ShootInput { get; init; }
        public bool ShieldInput { get; init; }
        public bool PauseInput { get; init; }
        public bool Toggle3DInput { get; init; }
    }
}