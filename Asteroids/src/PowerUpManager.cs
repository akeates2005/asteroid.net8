using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public class PowerUpEffect
    {
        public PowerUpType Type { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; set; }
        public float Intensity { get; set; } // For effects like RapidFire multiplier, Speed multiplier

        public PowerUpEffect(PowerUpType type, float duration, float intensity = 1.0f)
        {
            Type = type;
            Duration = duration;
            RemainingTime = duration;
            Intensity = intensity;
        }

        public bool IsActive => RemainingTime > 0;

        public void Update(float deltaTime)
        {
            if (RemainingTime > 0)
            {
                RemainingTime -= deltaTime;
            }
        }
    }

    public class PowerUpManager
    {
        private List<PowerUp> _activePowerUps;
        private Dictionary<PowerUpType, PowerUpEffect> _activeEffects;
        private AdvancedParticlePool? _particlePool;
        private AudioManager? _audioManager;
        private Random _random;

        // Power-up effect durations (in seconds)
        private const float SHIELD_DURATION = 8.0f;
        private const float RAPID_FIRE_DURATION = 10.0f;
        private const float MULTI_SHOT_DURATION = 12.0f;
        private const float SPEED_DURATION = 15.0f;

        // Power-up effect intensities
        private const float RAPID_FIRE_MULTIPLIER = 2.5f;
        private const float SPEED_MULTIPLIER = 1.8f;
        private const int MULTI_SHOT_BULLETS = 3;

        public PowerUpManager(AdvancedParticlePool? particlePool, AudioManager? audioManager)
        {
            _activePowerUps = new List<PowerUp>();
            _activeEffects = new Dictionary<PowerUpType, PowerUpEffect>();
            _particlePool = particlePool;
            _audioManager = audioManager;
            _random = new Random();
        }

        public void SpawnPowerUp(Vector2 position, PowerUpType type)
        {
            var powerUp = new PowerUp(position, type);
            _activePowerUps.Add(powerUp);

            // Create spawn particle effect
            _particlePool?.CreatePowerUpSpawnEffect(position, powerUp.GetColor());

            ErrorManager.LogInfo($"Spawned {type} power-up at {position}");
        }

        public void UpdatePowerUps(float deltaTime)
        {
            // Update active power-ups
            for (int i = _activePowerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = _activePowerUps[i];
                powerUp.Update(deltaTime);

                if (!powerUp.Active)
                {
                    // Create despawn effect
                    _particlePool?.CreatePowerUpDespawnEffect(powerUp.Position, powerUp.GetColor());
                    _activePowerUps.RemoveAt(i);
                }
            }

            // Update active effects
            var effectsToRemove = new List<PowerUpType>();
            foreach (var kvp in _activeEffects.ToList())
            {
                kvp.Value.Update(deltaTime);
                if (!kvp.Value.IsActive)
                {
                    effectsToRemove.Add(kvp.Key);
                }
            }

            // Remove expired effects
            foreach (var type in effectsToRemove)
            {
                _activeEffects.Remove(type);
                ErrorManager.LogInfo($"Power-up effect {type} expired");
            }
        }

        public bool CheckCollision(Player player)
        {
            for (int i = _activePowerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = _activePowerUps[i];
                if (!powerUp.Active) continue;

                float distance = Vector2.Distance(player.Position, powerUp.Position);
                if (distance <= player.Size / 2 + powerUp.GetRadius())
                {
                    // Collision detected - collect power-up
                    ApplyPowerUpEffect(player, powerUp.Type);
                    
                    // Create collection effect
                    _particlePool?.CreatePowerUpCollectionEffect(powerUp.Position, powerUp.GetColor());
                    
                    // Play collection sound
                    _audioManager?.PlaySound("powerup", 0.7f);
                    
                    // Remove power-up
                    _activePowerUps.RemoveAt(i);
                    
                    ErrorManager.LogInfo($"Player collected {powerUp.Type} power-up");
                    return true;
                }
            }
            return false;
        }

        public void ApplyPowerUpEffect(Player player, PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Shield:
                    player.IsShieldActive = true;
                    player.ShieldDuration = SHIELD_DURATION * GameConstants.TARGET_FPS;
                    player.ShieldCooldown = 0; // Reset cooldown
                    _activeEffects[type] = new PowerUpEffect(type, SHIELD_DURATION);
                    break;

                case PowerUpType.RapidFire:
                    _activeEffects[type] = new PowerUpEffect(type, RAPID_FIRE_DURATION, RAPID_FIRE_MULTIPLIER);
                    break;

                case PowerUpType.MultiShot:
                    _activeEffects[type] = new PowerUpEffect(type, MULTI_SHOT_DURATION, MULTI_SHOT_BULLETS);
                    break;

                case PowerUpType.Health:
                    // Instant effect - could extend to lives system later
                    player.IsShieldActive = true;
                    player.ShieldDuration = 60f; // Brief invincibility
                    break;

                case PowerUpType.Speed:
                    _activeEffects[type] = new PowerUpEffect(type, SPEED_DURATION, SPEED_MULTIPLIER);
                    break;
            }

            ErrorManager.LogInfo($"Applied {type} power-up effect to player");
        }

        public void RenderPowerUps2D()
        {
            foreach (var powerUp in _activePowerUps)
            {
                if (!powerUp.Active) continue;

                Color color = powerUp.GetColor();
                float pulseScale = powerUp.GetPulseScale();
                float radius = powerUp.GetRadius() * pulseScale;

                // Draw outer glow
                Color glowColor = new Color(color.R, color.G, color.B, (byte)100);
                Raylib.DrawCircleV(powerUp.Position, radius * 1.3f, glowColor);

                // Draw main power-up circle
                Raylib.DrawCircleV(powerUp.Position, radius, color);

                // Draw inner highlight
                Color highlightColor = new Color((byte)255, (byte)255, (byte)255, (byte)150);
                Raylib.DrawCircleV(powerUp.Position, radius * 0.6f, highlightColor);

                // Draw type indicator symbol
                DrawPowerUpSymbol(powerUp.Position, powerUp.Type, radius * 0.8f);

                // Draw lifetime indicator (fading out)
                float lifePercent = powerUp.LifeTime / powerUp.MaxLifeTime;
                if (lifePercent < 0.3f) // Start blinking when < 30% lifetime remaining
                {
                    float blinkAlpha = (MathF.Sin(powerUp.PulseAnimation * 8) + 1) * 0.5f;
                    Color warningColor = new Color((byte)255, (byte)255, (byte)255, (byte)(255 * blinkAlpha));
                    Raylib.DrawCircleLinesV(powerUp.Position, radius * 1.5f, warningColor);
                }
            }
        }

        public void RenderPowerUps3D(IRenderer renderer)
        {
            foreach (var powerUp in _activePowerUps)
            {
                if (!powerUp.Active) continue;

                // Use renderer abstraction for 3D power-up rendering
                // This would be implemented in the renderer to draw 3D power-up models
                renderer.RenderPowerUp3D(powerUp.Position, powerUp.Type, powerUp.GetPulseScale(), powerUp.Rotation);
            }
        }

        private void DrawPowerUpSymbol(Vector2 position, PowerUpType type, float size)
        {
            Color symbolColor = Color.White;
            
            switch (type)
            {
                case PowerUpType.Shield:
                    // Draw shield symbol (hexagon)
                    DrawHexagon(position, size * 0.4f, symbolColor);
                    break;

                case PowerUpType.RapidFire:
                    // Draw crosshair symbol
                    float lineLength = size * 0.6f;
                    Raylib.DrawLineEx(
                        new Vector2(position.X - lineLength / 2, position.Y), 
                        new Vector2(position.X + lineLength / 2, position.Y), 
                        2, symbolColor);
                    Raylib.DrawLineEx(
                        new Vector2(position.X, position.Y - lineLength / 2), 
                        new Vector2(position.X, position.Y + lineLength / 2), 
                        2, symbolColor);
                    break;

                case PowerUpType.MultiShot:
                    // Draw triple arrow symbol
                    DrawTripleArrow(position, size * 0.3f, symbolColor);
                    break;

                case PowerUpType.Health:
                    // Draw plus symbol
                    float plusSize = size * 0.4f;
                    Raylib.DrawLineEx(
                        new Vector2(position.X - plusSize, position.Y), 
                        new Vector2(position.X + plusSize, position.Y), 
                        3, symbolColor);
                    Raylib.DrawLineEx(
                        new Vector2(position.X, position.Y - plusSize), 
                        new Vector2(position.X, position.Y + plusSize), 
                        3, symbolColor);
                    break;

                case PowerUpType.Speed:
                    // Draw lightning bolt symbol
                    DrawLightningBolt(position, size * 0.5f, symbolColor);
                    break;
            }
        }

        private void DrawHexagon(Vector2 center, float radius, Color color)
        {
            const int sides = 6;
            for (int i = 0; i < sides; i++)
            {
                float angle1 = (i * MathF.PI * 2) / sides;
                float angle2 = ((i + 1) * MathF.PI * 2) / sides;
                
                Vector2 point1 = new Vector2(
                    center.X + MathF.Cos(angle1) * radius,
                    center.Y + MathF.Sin(angle1) * radius);
                Vector2 point2 = new Vector2(
                    center.X + MathF.Cos(angle2) * radius,
                    center.Y + MathF.Sin(angle2) * radius);
                
                Raylib.DrawLineEx(point1, point2, 2, color);
            }
        }

        private void DrawTripleArrow(Vector2 center, float size, Color color)
        {
            // Draw three small arrows pointing up
            for (int i = -1; i <= 1; i++)
            {
                Vector2 arrowBase = new Vector2(center.X + i * size * 0.8f, center.Y + size);
                Vector2 arrowTip = new Vector2(center.X + i * size * 0.8f, center.Y - size);
                Vector2 arrowLeft = new Vector2(center.X + i * size * 0.8f - size * 0.3f, center.Y - size * 0.3f);
                Vector2 arrowRight = new Vector2(center.X + i * size * 0.8f + size * 0.3f, center.Y - size * 0.3f);
                
                Raylib.DrawLineEx(arrowBase, arrowTip, 2, color);
                Raylib.DrawLineEx(arrowTip, arrowLeft, 2, color);
                Raylib.DrawLineEx(arrowTip, arrowRight, 2, color);
            }
        }

        private void DrawLightningBolt(Vector2 center, float size, Color color)
        {
            // Simple lightning bolt shape
            Vector2[] points = new Vector2[]
            {
                new Vector2(center.X - size * 0.3f, center.Y - size),
                new Vector2(center.X + size * 0.1f, center.Y - size * 0.2f),
                new Vector2(center.X - size * 0.1f, center.Y - size * 0.2f),
                new Vector2(center.X + size * 0.3f, center.Y + size)
            };

            for (int i = 0; i < points.Length - 1; i++)
            {
                Raylib.DrawLineEx(points[i], points[i + 1], 2, color);
            }
        }

        // Public methods for checking active effects
        public bool IsEffectActive(PowerUpType type)
        {
            return _activeEffects.ContainsKey(type) && _activeEffects[type].IsActive;
        }

        public float GetEffectIntensity(PowerUpType type)
        {
            return _activeEffects.ContainsKey(type) && _activeEffects[type].IsActive 
                ? _activeEffects[type].Intensity : 1.0f;
        }

        public float GetEffectRemainingTime(PowerUpType type)
        {
            return _activeEffects.ContainsKey(type) && _activeEffects[type].IsActive 
                ? _activeEffects[type].RemainingTime : 0.0f;
        }

        public int GetActivePowerUpCount()
        {
            return _activePowerUps.Count(p => p.Active);
        }

        public List<PowerUpType> GetActiveEffectTypes()
        {
            return _activeEffects.Where(kvp => kvp.Value.IsActive).Select(kvp => kvp.Key).ToList();
        }

        public void Clear()
        {
            _activePowerUps.Clear();
            _activeEffects.Clear();
        }
    }
}