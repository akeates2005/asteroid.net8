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
        public int Lifespan { get; set; }
        public Color Color { get; set; }
        public bool IsActive { get; set; }

        public EnhancedExplosionParticle()
        {
            Reset();
        }

        public EnhancedExplosionParticle(Vector2 position, Vector2 velocity, int lifespan, Color color)
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

            Position += Velocity;
            Velocity *= GameConstants.VELOCITY_DAMPING; // Slow down over time
            Lifespan--;
        }

        public void Draw()
        {
            if (Lifespan <= 0) return;

            // Fade out over time
            float alpha = (float)Lifespan / GameConstants.EXPLOSION_PARTICLE_LIFESPAN;
            Color fadeColor = new Color(Color.R, Color.G, Color.B, (byte)(alpha * 255));
            
            Raylib.DrawCircle((int)Position.X, (int)Position.Y, 2, fadeColor);
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