using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Represents the player's spaceship with movement, shield mechanics, and engine particle effects.
    /// Handles input processing, screen wrapping, and collision detection for the main game entity.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Current position of the player on the screen
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// Current velocity vector determining movement direction and speed
        /// </summary>
        public Vector2 Velocity { get; set; }
        /// <summary>
        /// Current rotation angle in degrees (0 = pointing up)
        /// </summary>
        public float Rotation { get; set; }
        /// <summary>
        /// Visual size of the player ship for drawing and collision detection
        /// </summary>
        public float Size { get; set; }
        /// <summary>
        /// Whether the protective shield is currently active
        /// </summary>
        public bool IsShieldActive { get; set; }
        /// <summary>
        /// Remaining shield duration in frames (decrements while shield is active)
        /// </summary>
        public float ShieldDuration { get; set; }
        /// <summary>
        /// Remaining cooldown time in frames before shield can be used again
        /// </summary>
        public float ShieldCooldown { get; set; }

        private const float MaxShieldDuration = 180; // 3 seconds at 60 FPS
        private const float MaxShieldCooldown = 300; // 5 seconds at 60 FPS

        private List<EngineParticle> _engineParticles = new List<EngineParticle>();
        private Random _random = new Random();

        /// <summary>
        /// Initializes a new player instance at the specified position with the given size
        /// </summary>
        /// <param name="position">Starting position on the screen</param>
        /// <param name="size">Visual size of the player ship</param>
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

        /// <summary>
        /// Updates the player state including input handling, movement, shield management, and engine particles.
        /// Processes keyboard input for rotation and thrust, applies screen wrapping, and manages shield timers.
        /// </summary>
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
                _engineParticles.Add(new EngineParticle(particlePosition, particleVelocity, 20, DynamicTheme.GetEngineColor()));
            }

            // Update position
            Position += Velocity;

            // Screen wrapping
            if (Position.X < 0) Position = new Vector2(Raylib.GetScreenWidth(), Position.Y);
            if (Position.X > Raylib.GetScreenWidth()) Position = new Vector2(0, Position.Y);
            if (Position.Y < 0) Position = new Vector2(Position.X, Raylib.GetScreenHeight());
            if (Position.Y > Raylib.GetScreenHeight()) Position = new Vector2(Position.X, 0);

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

        /// <summary>
        /// Renders the player ship as a triangle with engine particles and shield effects.
        /// Draws engine particles, the triangular ship with dynamic coloring, and shield visual effects when active.
        /// </summary>
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
            // Get player health percentage for color calculation
            float healthPercent = 1.0f; // Simplified - could be based on shield/damage state
            Color playerColor = DynamicTheme.GetPlayerColor(healthPercent);
            Raylib.DrawTriangleLines(v1, v2, v3, playerColor);

            // Draw shield if active with pulsing effect
            if (IsShieldActive)
            {
                float shieldAlpha = 0.3f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 8); // Pulsing shield
                Color shieldColor = DynamicTheme.GetShieldColor(shieldAlpha);
                Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, Size * 1.5f, shieldColor);
                
                // Add inner shield glow
                Color innerGlow = new Color(shieldColor.R, shieldColor.G, shieldColor.B, (byte)30);
                Raylib.DrawCircle((int)Position.X, (int)Position.Y, Size * 1.2f, innerGlow);
            }
        }

        /// <summary>
        /// Draw only the engine particles (thrust trail) without drawing the player ship
        /// </summary>
        public void DrawEngineParticles()
        {
            // Draw engine particles
            foreach (var particle in _engineParticles)
            {
                particle.Draw();
            }
        }
        
        /// <summary>
        /// Nuclear option - clear all engine particles immediately
        /// </summary>
        public void ClearEngineParticles()
        {
            _engineParticles.Clear();
            ErrorManager.LogInfo($"Player: Cleared {_engineParticles.Count} engine particles");
        }
    }
}
