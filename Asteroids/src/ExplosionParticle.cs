using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Represents an individual particle in an explosion effect with physics-based movement.
    /// Provides visual feedback for destruction events with realistic particle behavior and lifespan management.
    /// </summary>
    public class ExplosionParticle
    {
        /// <summary>
        /// Current screen position of the explosion particle
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Movement velocity vector determining the particle's trajectory and speed
        /// </summary>
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// Remaining lifespan of the particle in game frames (decreases over time)
        /// </summary>
        public float Lifespan { get; set; }
        /// <summary>
        /// Color of the particle for rendering explosion effects
        /// </summary>
        public Color Color { get; set; }
        /// <summary>
        /// Whether the particle is still active and should be processed and rendered
        /// </summary>
        public bool IsActive => Lifespan > 0;

        /// <summary>
        /// Creates a new explosion particle with specified initial conditions
        /// </summary>
        /// <param name="position">Initial position of the particle</param>
        /// <param name="velocity">Initial movement velocity and direction</param>
        /// <param name="lifespan">Duration the particle will remain active (in frames)</param>
        /// <param name="color">Color for rendering the particle</param>
        public ExplosionParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            Color = color;
        }

        /// <summary>
        /// Updates particle position based on velocity and decreases lifespan using delta time.
        /// Applies frame-time compensation for smooth animation regardless of frame rate.
        /// </summary>
        public void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Lifespan -= deltaTime * 60.0f; // Convert to delta-time based countdown
        }

        /// <summary>
        /// Renders the explosion particle as a single pixel if it has sufficient lifespan and visibility.
        /// Optimizes performance by only drawing particles that are significantly visible.
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
