using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Represents a single particle emitted from the player's ship engine to create thrust visual effects.
    /// Provides realistic particle physics with position, velocity, lifespan, and delta-time based updates.
    /// </summary>
    public class EngineParticle
    {
        /// <summary>
        /// Current screen position of the particle
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Movement velocity vector determining the particle's direction and speed
        /// </summary>
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// Remaining lifespan of the particle in game frames (decreases over time)
        /// </summary>
        public float Lifespan { get; set; }
        /// <summary>
        /// Color of the particle for rendering
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Creates a new engine particle with specified properties
        /// </summary>
        /// <param name="position">Initial position of the particle</param>
        /// <param name="velocity">Initial movement velocity</param>
        /// <param name="lifespan">Duration the particle will remain visible (in frames)</param>
        /// <param name="color">Color for rendering the particle</param>
        public EngineParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            Color = color;
        }

        /// <summary>
        /// Updates particle position based on velocity and decreases lifespan using delta time.
        /// Uses frame-time compensation for smooth movement regardless of frame rate.
        /// </summary>
        public void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Lifespan -= deltaTime * 60.0f; // Convert to delta-time based countdown
        }

        /// <summary>
        /// Renders the particle as a single pixel if it has sufficient lifespan and visibility.
        /// Only draws particles that are significantly visible to optimize performance.
        /// </summary>
        public void Draw()
        {
            if (Lifespan > 2.0f && Color.A > 20) // Only draw if significant lifespan and visible
            {
                Raylib.DrawPixelV(Position, Color);
            }
        }
    }
}
