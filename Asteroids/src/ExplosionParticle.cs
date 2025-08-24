using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public class ExplosionParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifespan;
        public Color Color;
        public bool IsActive => Lifespan > 0;

        public ExplosionParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            Color = color;
        }

        public void Update()
        {
            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Lifespan -= deltaTime * 60.0f; // Convert to delta-time based countdown
        }

        public void Draw()
        {
            if (Lifespan > 2.0f && Color.A > 20) // Only draw if significant lifespan and visible
            {
                Raylib.DrawPixelV(Position, Color);
            }
        }
    }
}
