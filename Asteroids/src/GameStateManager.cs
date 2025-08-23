using System;
using System.Collections.Generic;

namespace Asteroids
{
    /// <summary>
    /// Manages game state transitions and lifecycle
    /// </summary>
    public class GameStateManager
    {
        private readonly Dictionary<Type, IGameState> _states;
        private IGameState _currentState;
        private IGameState _pendingState;

        public IGameState CurrentState => _currentState;
        public bool HasPendingTransition => _pendingState != null;

        public GameStateManager()
        {
            _states = new Dictionary<Type, IGameState>();
        }

        /// <summary>
        /// Register a game state
        /// </summary>
        public void RegisterState<T>(T state) where T : class, IGameState
        {
            _states[typeof(T)] = state;
        }

        /// <summary>
        /// Transition to a new state
        /// </summary>
        public void TransitionTo<T>() where T : class, IGameState
        {
            if (!_states.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"State {typeof(T).Name} is not registered");
            }

            _pendingState = _states[typeof(T)];
        }

        /// <summary>
        /// Process pending state transition
        /// </summary>
        public void ProcessTransition()
        {
            if (_pendingState == null) return;

            // Exit current state
            _currentState?.Exit();

            // Enter new state
            _currentState = _pendingState;
            _currentState.Enter();

            _pendingState = null;
        }

        /// <summary>
        /// Update current state
        /// </summary>
        public void Update(float gameTime)
        {
            ProcessTransition();
            _currentState?.Update(gameTime);
        }

        /// <summary>
        /// Draw current state
        /// </summary>
        public void Draw()
        {
            _currentState?.Draw();
        }

        /// <summary>
        /// Handle input for current state
        /// </summary>
        public void HandleInput()
        {
            _currentState?.HandleInput();
        }

        /// <summary>
        /// Get state by type
        /// </summary>
        public T GetState<T>() where T : class, IGameState
        {
            return _states.TryGetValue(typeof(T), out var state) ? state as T : null;
        }
    }
}