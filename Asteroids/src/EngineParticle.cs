using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public class EngineParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifespan;
        public Color Color;

        public EngineParticle(Vector2 position, Vector2 velocity, float lifespan, Color color)
        {
            Position = position;
            Velocity = velocity;
            Lifespan = lifespan;
            Color = color;
        }

        public void Update()
        {
            Position += Velocity;
            Lifespan -= 1;
        }

        public void Draw()
        {
            if (Lifespan > 0)
            {
                Raylib.DrawPixelV(Position, Color);
            }
        }
    }
}
