using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Enhanced explosion particle with pooling support
    /// </summary>
    public class EnhancedExplosionParticle : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Lifespan { get; set; }
        public Color Color { get; set; }
        public bool IsActive { get; set; }

        public EnhancedExplosionParticle()
        {
            Reset();
        }

        public EnhancedExplosionParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            Color = color;
            IsActive = true;
        }

        public void Update()
        {
            if (Lifespan <= 0)
            {
                IsActive = false;
                return;
            }

            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Velocity *= GameConstants.VELOCITY_DAMPING; // Slow down over time
            Lifespan -= deltaTime * 60.0f; // Convert to delta-time based countdown
        }

        public void Draw()
        {
            if (Lifespan <= 5.0f || !IsActive) return; // Much more aggressive - only draw if significant life left

            // Fade out over time
            float alpha = (float)Lifespan / GameConstants.EXPLOSION_PARTICLE_LIFESPAN;
            Color fadeColor = new Color(
                Math.Clamp((int)Color.R, 0, 255),
                Math.Clamp((int)Color.G, 0, 255),
                Math.Clamp((int)Color.B, 0, 255),
                Math.Clamp((int)(alpha * 255), 0, 255)
            );
            
            // Only draw if alpha is significant
            if (fadeColor.A > 30)
            {
                Raylib.DrawCircle((int)Position.X, (int)Position.Y, 2, fadeColor);
            }
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Lifespan = 0;
            Color = Color.White;
            IsActive = false;
        }
    }
}