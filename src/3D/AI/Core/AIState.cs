using System;

namespace Asteroids.AI.Core
{
    /// <summary>
    /// Base class for AI state machine states
    /// </summary>
    public abstract class AIState
    {
        public abstract string StateName { get; }
        
        /// <summary>
        /// Called when entering this state
        /// </summary>
        public virtual void OnEnter(AIEnemyShip ship) { }
        
        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        public abstract void Update(AIEnemyShip ship, float deltaTime);
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public virtual void OnExit(AIEnemyShip ship) { }
        
        /// <summary>
        /// Check if we should transition to another state
        /// </summary>
        public virtual AIState CheckTransitions(AIEnemyShip ship)
        {
            return null; // Stay in current state
        }
    }
    
    /// <summary>
    /// AI state machine for managing enemy behaviors
    /// </summary>
    public class AIStateMachine
    {
        private AIState currentState;
        private AIState previousState;
        
        public AIState CurrentState => currentState;
        public AIState PreviousState => previousState;
        
        public void Initialize(AIState initialState, AIEnemyShip ship)
        {
            currentState = initialState;
            currentState?.OnEnter(ship);
        }
        
        public void Update(AIEnemyShip ship, float deltaTime)
        {
            if (currentState == null) return;
            
            // Check for state transitions
            var newState = currentState.CheckTransitions(ship);
            if (newState != null && newState != currentState)
            {
                ChangeState(newState, ship);
            }
            
            // Update current state
            currentState.Update(ship, deltaTime);
        }
        
        public void ChangeState(AIState newState, AIEnemyShip ship)
        {
            if (newState == currentState) return;
            
            previousState = currentState;
            currentState?.OnExit(ship);
            
            currentState = newState;
            currentState?.OnEnter(ship);
        }
        
        public bool IsInState<T>() where T : AIState
        {
            return currentState is T;
        }
        
        public T GetCurrentState<T>() where T : AIState
        {
            return currentState as T;
        }
    }
}