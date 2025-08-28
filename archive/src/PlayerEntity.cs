using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Player entity implementation following the IGameEntity interface.
    /// Manages player movement, shield mechanics, and engine particle effects.
    /// </summary>
    public class PlayerEntity : IGameEntity
    {
        public int Id { get; private set; }
        public Vector2 Position { get; set; }
        public bool Active { get; set; }

        /// <summary>
        /// Current velocity vector
        /// </summary>
        public Vector2 Velocity { get; set; }
        
        /// <summary>
        /// Current rotation in degrees
        /// </summary>
        public float Rotation { get; set; }
        
        /// <summary>
        /// Visual size of the player ship
        /// </summary>
        public float Size { get; set; }
        
        /// <summary>
        /// Whether shield is currently active
        /// </summary>
        public bool IsShieldActive { get; set; }
        
        /// <summary>
        /// Remaining shield duration
        /// </summary>
        public float ShieldDuration { get; set; }
        
        /// <summary>
        /// Shield cooldown timer
        /// </summary>
        public float ShieldCooldown { get; set; }

        private readonly List<EngineParticle> _engineParticles;
        private readonly Random _random;
        private InputState _lastInputState;

        public PlayerEntity(int id, Vector2 position, float size)
        {
            Id = id;
            Position = position;
            Size = size;
            Active = true;
            Velocity = Vector2.Zero;
            Rotation = 0;
            IsShieldActive = false;
            ShieldDuration = 0;
            ShieldCooldown = 0;
            
            _engineParticles = new List<EngineParticle>();
            _random = new Random();
        }

        public void Initialize()
        {
            ErrorManager.LogInfo($"Player entity {Id} initialized at position {Position}");
        }

        public void Update(float deltaTime)
        {
            if (!Active) return;

            UpdateShield(deltaTime);
            UpdateEngineParticles(deltaTime);
            ApplyMovement();
            HandleScreenWrapping();
        }

        public void Render(IRenderer renderer)
        {
            if (!Active) return;

            float healthPercent = CalculateHealthPercent();
            Color playerColor = DynamicTheme.GetPlayerColor(healthPercent);
            float shieldAlpha = IsShieldActive ? CalculateShieldAlpha() : 0f;

            renderer.RenderPlayer(Position, Rotation, playerColor, IsShieldActive, shieldAlpha);
        }

        public float GetCollisionRadius()
        {
            return Size / 2f;
        }

        public void Dispose()
        {
            _engineParticles.Clear();
            ErrorManager.LogInfo($"Player entity {Id} disposed");
        }

        /// <summary>
        /// Process input state for movement
        /// </summary>
        /// <param name="inputState">Current input state</param>
        public void ProcessInput(InputState inputState)
        {
            if (!Active) return;

            // Handle rotation
            if (inputState.LeftInput)
            {
                Rotation -= GameConstants.PLAYER_ROTATION_SPEED;
            }
            if (inputState.RightInput)
            {
                Rotation += GameConstants.PLAYER_ROTATION_SPEED;
            }

            // Handle thrust
            if (inputState.ThrustInput)
            {
                ApplyThrust();
                CreateEngineParticles();
            }

            // Handle shield activation
            if (inputState.ShieldInput && !IsShieldActive && ShieldCooldown <= 0)
            {
                ActivateShield();
            }

            _lastInputState = inputState;
        }

        /// <summary>
        /// Activate player shield
        /// </summary>
        public void ActivateShield()
        {
            if (IsShieldActive) return;

            IsShieldActive = true;
            ShieldDuration = GameConstants.MAX_SHIELD_DURATION;
            ErrorManager.LogInfo($"Player {Id} shield activated");
        }

        /// <summary>
        /// Reset player to spawn state
        /// </summary>
        public void ResetToSpawn(Vector2 spawnPosition)
        {
            Position = spawnPosition;
            Velocity = Vector2.Zero;
            Rotation = 0;
            IsShieldActive = false;
            ShieldDuration = 0;
            ShieldCooldown = 0;
            _engineParticles.Clear();
            Active = true;
            
            ErrorManager.LogInfo($"Player {Id} reset to spawn position {spawnPosition}");
        }

        /// <summary>
        /// Clear all engine particles
        /// </summary>
        public void ClearEngineParticles()
        {
            int particleCount = _engineParticles.Count;
            _engineParticles.Clear();
            ErrorManager.LogInfo($"Player {Id}: Cleared {particleCount} engine particles");
        }

        /// <summary>
        /// Get current engine particle count for debugging
        /// </summary>
        public int GetEngineParticleCount()
        {
            return _engineParticles.Count;
        }

        private void UpdateShield(float deltaTime)
        {
            if (IsShieldActive)
            {
                ShieldDuration -= deltaTime * GameConstants.TARGET_FPS; // Convert to frame-based timing
                if (ShieldDuration <= 0)
                {
                    IsShieldActive = false;
                    ShieldCooldown = GameConstants.MAX_SHIELD_COOLDOWN;
                    ErrorManager.LogInfo($"Player {Id} shield deactivated, cooldown started");
                }
            }
            else if (ShieldCooldown > 0)
            {
                ShieldCooldown -= deltaTime * GameConstants.TARGET_FPS;
                if (ShieldCooldown <= 0)
                {
                    ShieldCooldown = 0;
                }
            }
        }

        private void UpdateEngineParticles(float deltaTime)
        {
            for (int i = _engineParticles.Count - 1; i >= 0; i--)
            {
                _engineParticles[i].Update();
                if (_engineParticles[i].Lifespan <= 0)
                {
                    _engineParticles.RemoveAt(i);
                }
            }
        }

        private void ApplyMovement()
        {
            Position += Velocity;
            
            // Apply friction
            Velocity *= GameConstants.PLAYER_FRICTION;
            
            // Clamp velocity to prevent excessive speeds
            float maxSpeed = 5f;
            if (Velocity.Length() > maxSpeed)
            {
                Velocity = Vector2.Normalize(Velocity) * maxSpeed;
            }
        }

        private void ApplyThrust()
        {
            Vector2 thrustVector = Vector2.Transform(
                new Vector2(0, -GameConstants.PLAYER_THRUST),
                Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation)
            );
            Velocity += thrustVector;
        }

        private void CreateEngineParticles()
        {
            if (_engineParticles.Count >= 50) return; // Limit particle count for performance

            Vector2 particlePosition = Position + Vector2.Transform(
                new Vector2(0, Size / 2),
                Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation)
            );

            Vector2 particleVelocity = Vector2.Transform(
                new Vector2((float)(_random.NextDouble() * 2 - 1), 2),
                Matrix3x2.CreateRotation(MathF.PI / 180 * Rotation)
            );

            _engineParticles.Add(new EngineParticle(
                particlePosition,
                particleVelocity,
                GameConstants.ENGINE_PARTICLE_LIFESPAN,
                DynamicTheme.GetEngineColor()
            ));
        }

        private void HandleScreenWrapping()
        {
            var screenBounds = new Vector2(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT);
            
            if (Position.X < 0) Position = new Vector2(screenBounds.X, Position.Y);
            if (Position.X > screenBounds.X) Position = new Vector2(0, Position.Y);
            if (Position.Y < 0) Position = new Vector2(Position.X, screenBounds.Y);
            if (Position.Y > screenBounds.Y) Position = new Vector2(Position.X, 0);
        }

        private float CalculateHealthPercent()
        {
            // For now, use shield state as health indicator
            // Could be extended with actual health system
            return IsShieldActive ? 1.0f : 0.8f;
        }

        private float CalculateShieldAlpha()
        {
            if (!IsShieldActive) return 0f;
            
            // Pulsing effect
            float basePulse = 0.3f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 8);
            
            // Fade out as duration decreases
            float durationFactor = Math.Max(0.2f, ShieldDuration / GameConstants.MAX_SHIELD_DURATION);
            
            return basePulse * durationFactor;
        }
    }
}