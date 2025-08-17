using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    class Asteroid
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public AsteroidSize AsteroidSize;
        public float Radius;
        public bool Active;

        private Random _random;
        private float _timer;
        private float _changeInterval;
        private AsteroidShape _shape;

        public Asteroid(Vector2 position, Vector2 velocity, AsteroidSize asteroidSize, Random random, int level)
        {
            Position = position;
            AsteroidSize = asteroidSize;
            Active = true;
            _random = random;

            switch (asteroidSize)
            {
                case AsteroidSize.Large:
                    Radius = 40;
                    break;
                case AsteroidSize.Medium:
                    Radius = 20;
                    break;
                case AsteroidSize.Small:
                    Radius = 10;
                    break;
            }

            float speedMultiplier = 1 + (level - 1) * 0.2f;
            Velocity = Vector2.Multiply(velocity, speedMultiplier);

            float changeIntervalMultiplier = 1 - (level - 1) * 0.1f;
            _changeInterval = (float)(_random.NextDouble() * 5 + 2) * changeIntervalMultiplier;
            _timer = _changeInterval;

            _shape = new AsteroidShape(_random.Next(8, 13), Radius, _random);
        }

        public void Update()
        {
            if (!Active) return;

            _timer -= Raylib.GetFrameTime();

            if (_timer <= 0)
            {
                Velocity = new Vector2((float)(_random.NextDouble() * 4 - 2), (float)(_random.NextDouble() * 4 - 2));
                _timer = _changeInterval;
            }

            Position += Velocity;

            // Screen wrapping
            if (Position.X < 0) Position.X = Raylib.GetScreenWidth();
            if (Position.X > Raylib.GetScreenWidth()) Position.X = 0;
            if (Position.Y < 0) Position.Y = Raylib.GetScreenHeight();
            if (Position.Y > Raylib.GetScreenHeight()) Position.Y = 0;
        }

        public void Draw()
        {
            if (!Active) return;

            // Draw the asteroid as a polygon
            for (int i = 0; i < _shape.Points.Length; i++)
            {
                Vector2 p1 = _shape.Points[i] + Position;
                Vector2 p2 = _shape.Points[(i + 1) % _shape.Points.Length] + Position;
                Raylib.DrawLineV(p1, p2, Color.White);
            }
        }
    }
}