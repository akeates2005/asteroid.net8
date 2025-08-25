using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Interface for objects that can participate in collision detection
    /// </summary>
    public interface ICollidable
    {
        /// <summary>
        /// Current position of the object used for collision detection calculations
        /// </summary>
        Vector2 Position { get; }
        /// <summary>
        /// Collision detection radius for circular collision bounds
        /// </summary>
        float Radius { get; }
        /// <summary>
        /// Whether the object is currently active and should participate in collision detection
        /// </summary>
        bool Active { get; }
        /// <summary>
        /// Called when this object collides with another collidable object
        /// </summary>
        /// <param name="other">The other object involved in the collision</param>
        void OnCollision(ICollidable other);
    }
}