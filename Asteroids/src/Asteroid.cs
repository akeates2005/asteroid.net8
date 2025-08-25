using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Represents an asteroid game object with random movement, size-based physics, and level-scaled difficulty.
    /// Handles collision detection, screen wrapping, and procedural shape generation for visual variety.
    /// </summary>
    public class Asteroid
    {
        /// <summary>
        /// Current position of the asteroid on the screen
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Current movement velocity vector determining direction and speed
        /// </summary>
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// Size category of the asteroid (Small, Medium, or Large) affecting collision radius and visual appearance
        /// </summary>
        public AsteroidSize AsteroidSize { get; set; }
        /// <summary>
        /// Collision detection radius based on asteroid size
        /// </summary>
        public float Radius { get; set; }
        /// <summary>
        /// Whether the asteroid is active and should be updated and rendered
        /// </summary>
        public bool Active { get; set; }

        private Random _random;
        private float _timer;
        private float _changeInterval;
        private AsteroidShape _shape;

        /// <summary>
        /// Initializes a new asteroid with position, velocity, size, and level-based difficulty scaling
        /// </summary>
        /// <param name="position">Starting position on the screen</param>
        /// <param name="velocity">Initial movement velocity</param>
        /// <param name="asteroidSize">Size category determining radius and visual scale</param>
        /// <param name="random">Random number generator for procedural shape and behavior</param>
        /// <param name="level">Current game level for difficulty scaling (higher = faster movement)</param>
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

        /// <summary>
        /// Updates asteroid movement with periodic velocity changes and screen wrapping.
        /// Implements random direction changes at intervals for unpredictable movement patterns.
        /// </summary>
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
            if (Position.X < 0) Position = new Vector2(Raylib.GetScreenWidth(), Position.Y);
            if (Position.X > Raylib.GetScreenWidth()) Position = new Vector2(0, Position.Y);
            if (Position.Y < 0) Position = new Vector2(Position.X, Raylib.GetScreenHeight());
            if (Position.Y > Raylib.GetScreenHeight()) Position = new Vector2(Position.X, 0);
        }

        /// <summary>
        /// Renders the asteroid as a polygonal shape using its procedurally generated vertices.
        /// Draws connected line segments to create a jagged, natural-looking asteroid outline.
        /// </summary>
        public void Draw()
        {
            if (!Active) return;

            // Draw the asteroid as a polygon
            for (int i = 0; i < _shape.Points.Length; i++)
            {
                Vector2 p1 = _shape.Points[i] + Position;
                Vector2 p2 = _shape.Points[(i + 1) % _shape.Points.Length] + Position;
                Raylib.DrawLineV(p1, p2, Theme.AsteroidColor);
            }
        }
    }
}