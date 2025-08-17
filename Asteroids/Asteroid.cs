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

            Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, Radius, Color.White);
        }
    }
}