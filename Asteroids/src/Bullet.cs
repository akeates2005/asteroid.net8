using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Represents a projectile fired by the player with linear movement and automatic deactivation.
    /// Handles collision detection and lifecycle management for ammunition in the game.
    /// </summary>
    public class Bullet
    {
        /// <summary>
        /// Current position of the bullet on the screen
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Movement velocity vector determining direction and speed of the bullet
        /// </summary>
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// Whether the bullet is active and should be updated, rendered, and participate in collision detection
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Initializes a new bullet with the specified position and velocity
        /// </summary>
        /// <param name="position">Starting position of the bullet</param>
        /// <param name="velocity">Movement velocity and direction</param>
        public Bullet(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
            Active = true;
        }

        /// <summary>
        /// Updates bullet position and checks for screen boundaries to deactivate off-screen bullets.
        /// Automatically deactivates bullets that travel beyond the visible screen area.
        /// </summary>
        public void Update()
        {
            if (!Active) return;

            Position += Velocity;

            // Deactivate if off-screen
            if (Position.X < 0 || Position.X > Raylib.GetScreenWidth() || Position.Y < 0 || Position.Y > Raylib.GetScreenHeight())
            {
                Active = false;
            }
        }

        /// <summary>
        /// Renders the bullet as a small colored circle on the screen.
        /// Only draws when the bullet is active.
        /// </summary>
        public void Draw()
        {
            if (!Active) return;

            Raylib.DrawCircle((int)Position.X, (int)Position.Y, 2, Theme.BulletColor);
        }
    }
}