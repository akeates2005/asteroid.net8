using System;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Interface for game state management pattern
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void Exit();

        /// <summary>
        /// Update game logic for this state
        /// </summary>
        /// <param name="gameTime">Frame time information</param>
        void Update(float gameTime);

        /// <summary>
        /// Render graphics for this state
        /// </summary>
        void Draw();

        /// <summary>
        /// Handle input for this state
        /// </summary>
        void HandleInput();
    }
}