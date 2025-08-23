using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Interface for objects that can participate in collision detection
    /// </summary>
    public interface ICollidable
    {
        Vector2 Position { get; }
        float Radius { get; }
        bool Active { get; }
        void OnCollision(ICollidable other);
    }
}