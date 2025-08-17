using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    class Player
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float Size;
        public bool IsShieldActive;
        public float ShieldDuration;
        public float ShieldCooldown;

        private const float MaxShieldDuration = 180; // 3 seconds at 60 FPS
        private const float MaxShieldCooldown = 300; // 5 seconds at 60 FPS

        private List<EngineParticle> _engineParticles = new List<EngineParticle>();
        private Random _random = new Random();

        public Player(Vector2 position, float size)
        {
            Position = position;
            Size = size;
            Velocity = Vector2.Zero;
            Rotation = 0;
            IsShieldActive = false;
            ShieldDuration = 0;
            ShieldCooldown = 0;
        }

        public void Update()
        {
            // Handle input
            if (Raylib.IsKeyDown(KeyboardKey.Right))
            {
                Rotation += 5;
            }
            if (Raylib.IsKeyDown(KeyboardKey.Left))
            {
                Rotation -= 5;
            }
            if (Raylib.IsKeyDown(KeyboardKey.Up))
            {
                Velocity += Vector2.Transform(new Vector2(0, -0.1f), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));

                // Create engine particles
                Vector2 particlePosition = Position + Vector2.Transform(new Vector2(0, Size / 2), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));
                Vector2 particleVelocity = Vector2.Transform(new Vector2((float)(_random.NextDouble() * 2 - 1), 2), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));
                _engineParticles.Add(new EngineParticle(particlePosition, particleVelocity, 20, Theme.EngineColor));
            }

            // Update position
            Position += Velocity;

            // Screen wrapping
            if (Position.X < 0) Position.X = Raylib.GetScreenWidth();
            if (Position.X > Raylib.GetScreenWidth()) Position.X = 0;
            if (Position.Y < 0) Position.Y = Raylib.GetScreenHeight();
            if (Position.Y > Raylib.GetScreenHeight()) Position.Y = 0;

            // Update engine particles
            foreach (var particle in _engineParticles)
            {
                particle.Update();
            }
            _engineParticles.RemoveAll(p => p.Lifespan <= 0);

            // Update shield duration and cooldown
            if (IsShieldActive)
            {
                ShieldDuration--;
                if (ShieldDuration <= 0)
                {
                    IsShieldActive = false;
                    ShieldCooldown = MaxShieldCooldown;
                }
            }
            else if (ShieldCooldown > 0)
            {
                ShieldCooldown--;
            }
        }

        public void Draw()
        {
            // Draw engine particles
            foreach (var particle in _engineParticles)
            {
                particle.Draw();
            }

            Vector2 v1 = Position + Vector2.Transform(new Vector2(0, -Size), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));
            Vector2 v2 = Position + Vector2.Transform(new Vector2(-Size / 2, Size / 2), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));
            Vector2 v3 = Position + Vector2.Transform(new Vector2(Size / 2, Size / 2), Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation));
            Raylib.DrawTriangleLines(v1, v2, v3, Theme.PlayerColor);

            // Draw shield if active
            if (IsShieldActive)
            {
                Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, Size * 1.5f, Theme.ShieldColor);
            }
        }
    }
}
