using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Enemy ship types with distinct characteristics and behaviors
    /// </summary>
    public enum EnemyType
    {
        Scout,      // Fast, weak, erratic movement
        Hunter,     // Medium speed, pursues player
        Destroyer,  // Slow, powerful, formation flying
        Interceptor // Fast intercept trajectories
    }

    /// <summary>
    /// AI behavioral states for enemy ships
    /// </summary>
    public enum AIState
    {
        Idle,
        Pursuing,
        Retreating,
        Circling,
        Attacking,
        FormationFlying,
        Intercepting,
        Evading
    }

    /// <summary>
    /// Enemy ship entity with AI behavior, collision detection, and rendering support
    /// Integrates with existing spatial partitioning and rendering systems
    /// </summary>
    public class EnemyShip : IGameEntity, ICollidable
    {
        // Core properties
        public int Id { get; private set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public bool Active { get; set; }
        
        // ICollidable properties
        public float Radius => Size;

        // Enemy-specific properties
        public EnemyType Type { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; private set; }
        public AIState CurrentState { get; set; }
        public float StateTimer { get; set; }
        public Color Color { get; set; }

        // AI behavior properties
        public float Speed { get; private set; }
        public float TurnRate { get; private set; }
        public float AttackRange { get; private set; }
        public float RetreatDistance { get; private set; }
        public float LastShotTime { get; set; }
        public float ShotCooldown { get; private set; }
        
        // Formation flying support
        public Vector2 FormationPosition { get; set; }
        public int FormationIndex { get; set; } = -1;

        // Visual properties
        public float Size { get; private set; }
        public float PulseAnimation { get; set; }
        public float DamageFlashTimer { get; set; }

        private readonly Random _random;
        private static int _nextId = 1;

        public EnemyShip(EnemyType type, Vector2 position)
        {
            Id = _nextId++;
            Type = type;
            Position = position;
            Active = true;
            CurrentState = AIState.Idle;
            _random = new Random();
            
            InitializeByType();
        }

        /// <summary>
        /// Initialize enemy properties based on type
        /// </summary>
        private void InitializeByType()
        {
            switch (Type)
            {
                case EnemyType.Scout:
                    MaxHealth = 50f;
                    Speed = 120f;
                    TurnRate = 4f;
                    AttackRange = 150f;
                    RetreatDistance = 80f;
                    ShotCooldown = 1.5f;
                    Size = 12f;
                    Color = new Color(255, 255, 0, 255); // Yellow
                    break;

                case EnemyType.Hunter:
                    MaxHealth = 100f;
                    Speed = 80f;
                    TurnRate = 2.5f;
                    AttackRange = 200f;
                    RetreatDistance = 100f;
                    ShotCooldown = 1.0f;
                    Size = 16f;
                    Color = new Color(255, 0, 0, 255); // Red
                    break;

                case EnemyType.Destroyer:
                    MaxHealth = 200f;
                    Speed = 50f;
                    TurnRate = 1.5f;
                    AttackRange = 250f;
                    RetreatDistance = 120f;
                    ShotCooldown = 0.8f;
                    Size = 24f;
                    Color = new Color(128, 0, 128, 255); // Purple
                    break;

                case EnemyType.Interceptor:
                    MaxHealth = 75f;
                    Speed = 150f;
                    TurnRate = 3.5f;
                    AttackRange = 180f;
                    RetreatDistance = 90f;
                    ShotCooldown = 1.2f;
                    Size = 14f;
                    Color = new Color(255, 165, 0, 255); // Orange
                    break;
            }

            Health = MaxHealth;
        }

        /// <summary>
        /// Update enemy ship physics, animation, and state timers
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!Active) return;

            // Update state timer
            StateTimer += deltaTime;

            // Update animation properties
            PulseAnimation += deltaTime * 3f;
            
            // Update damage flash timer
            if (DamageFlashTimer > 0)
                DamageFlashTimer -= deltaTime;

            // Apply velocity with screen wrapping
            Position += Velocity * deltaTime;
            WrapAroundScreen();

            // Apply rotation to velocity direction for visual alignment
            if (Velocity.Length() > 0.1f)
            {
                float targetRotation = MathF.Atan2(Velocity.Y, Velocity.X) * (180f / MathF.PI);
                float rotationDiff = targetRotation - Rotation;
                
                // Normalize rotation difference to [-180, 180]
                while (rotationDiff > 180f) rotationDiff -= 360f;
                while (rotationDiff < -180f) rotationDiff += 360f;
                
                // Apply smooth rotation
                Rotation += rotationDiff * TurnRate * deltaTime;
            }

            // Apply velocity damping
            Velocity *= GameConstants.VELOCITY_DAMPING;
        }

        /// <summary>
        /// Apply damage to the enemy ship
        /// </summary>
        public void TakeDamage(float damage)
        {
            Health -= damage;
            DamageFlashTimer = 0.2f; // Flash for 200ms
            
            if (Health <= 0)
            {
                Active = false;
            }
        }

        /// <summary>
        /// Check if enemy can shoot based on cooldown
        /// </summary>
        public bool CanShoot()
        {
            return Raylib.GetTime() - LastShotTime >= ShotCooldown;
        }

        /// <summary>
        /// Record that enemy has shot (for cooldown tracking)
        /// </summary>
        public void RecordShot()
        {
            LastShotTime = (float)Raylib.GetTime();
        }

        /// <summary>
        /// Screen wrapping for enemy movement
        /// </summary>
        private void WrapAroundScreen()
        {
            if (Position.X < -Size)
                Position = new Vector2(GameConstants.SCREEN_WIDTH + Size, Position.Y);
            else if (Position.X > GameConstants.SCREEN_WIDTH + Size)
                Position = new Vector2(-Size, Position.Y);

            if (Position.Y < -Size)
                Position = new Vector2(Position.X, GameConstants.SCREEN_HEIGHT + Size);
            else if (Position.Y > GameConstants.SCREEN_HEIGHT + Size)
                Position = new Vector2(Position.X, -Size);
        }

        /// <summary>
        /// Get collision radius for spatial partitioning
        /// </summary>
        public float GetCollisionRadius()
        {
            return Size + GameConstants.COLLISION_DETECTION_MARGIN;
        }

        /// <summary>
        /// Check collision with another collidable object using position and radius
        /// </summary>
        public bool IsCollidingWith(Vector2 otherPosition, float otherRadius)
        {
            if (!Active) return false;

            float distance = Vector2.Distance(Position, otherPosition);
            float combinedRadius = GetCollisionRadius() + otherRadius;
            
            return distance <= combinedRadius;
        }

        /// <summary>
        /// Check collision with another collidable object
        /// </summary>
        public bool IsCollidingWith(ICollidable other)
        {
            if (!Active || other == null) return false;

            float distance = Vector2.Distance(Position, other.Position);
            float combinedRadius = GetCollisionRadius() + other.Radius;
            
            return distance <= combinedRadius;
        }

        /// <summary>
        /// Get current position for collision detection
        /// </summary>
        public Vector2 GetPosition()
        {
            return Position;
        }

        /// <summary>
        /// Get health percentage for rendering effects
        /// </summary>
        public float GetHealthPercentage()
        {
            return Health / MaxHealth;
        }

        /// <summary>
        /// Get rendering color with damage flash effect
        /// </summary>
        public Color GetRenderColor()
        {
            if (DamageFlashTimer > 0)
            {
                // Flash white when damaged
                return new Color(255, 255, 255, 255); // White
            }

            // Dim color based on health
            float healthRatio = GetHealthPercentage();
            return new Color(
                (byte)(Color.R * healthRatio),
                (byte)(Color.G * healthRatio),
                (byte)(Color.B * healthRatio),
                Color.A
            );
        }

        /// <summary>
        /// Get display size with pulse animation
        /// </summary>
        public float GetDisplaySize()
        {
            float pulseScale = 1f + MathF.Sin(PulseAnimation) * 0.1f;
            return Size * pulseScale;
        }

        /// <summary>
        /// Initialize entity (IGameEntity interface)
        /// </summary>
        public void Initialize()
        {
            // Initialization is handled in constructor
            // This method exists for interface compliance
        }

        /// <summary>
        /// Render entity using provided renderer (IGameEntity interface)
        /// </summary>
        public void Render(IRenderer renderer)
        {
            if (!Active || renderer == null) return;
            
            // Use renderer to render enemy in 3D mode
            renderer.RenderEnemy(Position, Rotation, Type, GetRenderColor(), GetDisplaySize(), GetHealthPercentage());
        }

        /// <summary>
        /// Handle collision with another collidable object (ICollidable interface)
        /// </summary>
        public void OnCollision(ICollidable other)
        {
            if (!Active || other == null) return;
            
            // Basic collision response - could be expanded for different collision types
            if (other is Player)
            {
                // Take damage from player collision
                TakeDamage(50f);
            }
            else if (other is Bullet)
            {
                // Take damage from bullet
                TakeDamage(25f);
            }
        }

        /// <summary>
        /// Cleanup entity resources (IGameEntity interface)
        /// </summary>
        public void Dispose()
        {
            Active = false;
            // No specific cleanup needed for EnemyShip
        }
    }
}