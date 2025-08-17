using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool Active;

        public Bullet(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
            Active = true;
        }

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

        public void Draw()
        {
            if (!Active) return;

            Raylib.DrawCircle((int)Position.X, (int)Position.Y, 2, Theme.BulletColor);
        }
    }
}