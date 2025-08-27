using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public enum PowerUpType
    {
        Shield,      // Temporary invincibility
        RapidFire,   // Increased firing rate
        MultiShot,   // Spread shot pattern
        Health,      // Extra life/health restoration
        Speed        // Enhanced movement speed
    }

    public class PowerUp
    {
        public Vector2 Position { get; set; }
        public PowerUpType Type { get; set; }
        public float Rotation { get; set; }
        public bool Active { get; set; }
        public float LifeTime { get; set; }        // Auto-despawn timer
        public float PulseAnimation { get; set; }  // Visual animation
        public float MaxLifeTime { get; private set; } = 15.0f; // 15 seconds lifetime

        public PowerUp(Vector2 position, PowerUpType type)
        {
            Position = position;
            Type = type;
            Rotation = 0;
            Active = true;
            LifeTime = MaxLifeTime;
            PulseAnimation = 0;
        }

        public void Update(float deltaTime)
        {
            if (!Active) return;

            // Update lifetime
            LifeTime -= deltaTime;
            if (LifeTime <= 0)
            {
                Active = false;
                return;
            }

            // Update rotation for visual effect
            Rotation += 90.0f * deltaTime; // Rotate 90 degrees per second
            if (Rotation >= 360.0f) Rotation -= 360.0f;

            // Update pulse animation
            PulseAnimation += 3.0f * deltaTime; // Pulse cycle every ~2 seconds
            if (PulseAnimation >= MathF.PI * 2) PulseAnimation -= MathF.PI * 2;
        }

        public float GetRadius()
        {
            return GameConstants.POWERUP_RADIUS;
        }

        public Color GetColor()
        {
            return Type switch
            {
                PowerUpType.Shield => Color.Blue,
                PowerUpType.RapidFire => Color.Red,
                PowerUpType.MultiShot => Color.Orange,
                PowerUpType.Health => Color.Green,
                PowerUpType.Speed => Color.Yellow,
                _ => Color.White
            };
        }

        public float GetPulseScale()
        {
            // Creates a pulsing effect between 0.8 and 1.2 scale
            return 1.0f + 0.2f * MathF.Sin(PulseAnimation);
        }
    }
}