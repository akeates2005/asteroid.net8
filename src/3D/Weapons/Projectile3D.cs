using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Represents a 3D projectile with advanced physics and effects
    /// </summary>
    public class Projectile3D
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool IsActive { get; private set; }
        public WeaponStats WeaponStats { get; private set; }
        public WeaponType WeaponType { get; private set; }
        public float Damage { get; private set; }
        public float Lifespan { get; private set; }
        public bool HasHitTarget { get; private set; }

        private float _maxLifespan;
        private float _creationTime;
        private float _delayTime;
        private bool _isDelayed;
        private List<Vector3> _trailPositions;
        private const int MaxTrailPositions = 10;
        private Random _random;
        
        // Tracking for missiles
        private Vector3? _targetPosition;
        private float _trackingStrength = 2f;
        
        // Chain lightning tracking
        private List<Vector3> _hitTargets;
        private int _maxChainTargets = 3;
        private float _chainRange = 20f;
        
        // Beam/laser tracking
        private Vector3 _beamEnd;
        private bool _isBeam;
        
        // Particle effects
        private List<ParticleEffect> _particles;

        public Projectile3D(Vector3 position, Vector3 velocity, WeaponStats weaponStats, WeaponType weaponType, float damage)
        {
            Position = position;
            Velocity = velocity;
            WeaponStats = weaponStats;
            WeaponType = weaponType;
            Damage = damage;
            IsActive = true;
            _maxLifespan = CalculateLifespan();
            Lifespan = _maxLifespan;
            _creationTime = GetTime();
            _trailPositions = new List<Vector3>();
            _random = new Random();
            _hitTargets = new List<Vector3>();
            _particles = new List<ParticleEffect>();
            
            InitializeSpecialProperties();
        }

        private void InitializeSpecialProperties()
        {
            switch (WeaponType)
            {
                case WeaponType.Laser:
                case WeaponType.Beam:
                    _isBeam = true;
                    _beamEnd = Position + Vector3.Normalize(Velocity) * WeaponStats.Range;
                    break;
                    
                case WeaponType.Flamethrower:
                    CreateFlameParticles();
                    break;
                    
                case WeaponType.Plasma:
                    CreatePlasmaTrail();
                    break;
            }
        }

        public void SetDelay(float delayTime)
        {
            _delayTime = delayTime;
            _isDelayed = delayTime > 0;
        }

        public void SetTarget(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
        }

        private float CalculateLifespan()
        {
            if (WeaponStats.Speed == float.MaxValue) // Instant weapons
                return 0.1f;
                
            return WeaponStats.Range / WeaponStats.Speed;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            // Handle delay
            if (_isDelayed)
            {
                _delayTime -= deltaTime;
                if (_delayTime > 0) return;
                _isDelayed = false;
            }

            float currentTime = GetTime();
            float age = currentTime - _creationTime;

            // Update lifespan
            Lifespan -= deltaTime;
            if (Lifespan <= 0)
            {
                IsActive = false;
                return;
            }

            UpdateMovement(deltaTime);
            UpdateTrail();
            UpdateParticles(deltaTime);
            CheckBoundaries();
        }

        private void UpdateMovement(float deltaTime)
        {
            switch (WeaponType)
            {
                case WeaponType.Missile:
                    UpdateMissileTracking(deltaTime);
                    break;
                    
                case WeaponType.Laser:
                case WeaponType.Beam:
                    // Beams don't move, they're instant
                    return;
                    
                case WeaponType.Flamethrower:
                    UpdateFlamethrowerMovement(deltaTime);
                    break;
                    
                default:
                    Position += Velocity * deltaTime;
                    break;
            }
        }

        private void UpdateMissileTracking(float deltaTime)
        {
            if (_targetPosition.HasValue)
            {
                Vector3 directionToTarget = Vector3.Normalize(_targetPosition.Value - Position);
                Vector3 currentDirection = Vector3.Normalize(Velocity);
                
                // Gradually turn towards target
                Vector3 newDirection = Vector3.Lerp(currentDirection, directionToTarget, _trackingStrength * deltaTime);
                Velocity = newDirection * Velocity.Length();
            }
            
            Position += Velocity * deltaTime;
        }

        private void UpdateFlamethrowerMovement(float deltaTime)
        {
            // Flame projectiles spread out and slow down over time
            float ageRatio = 1f - (Lifespan / _maxLifespan);
            Vector3 spread = new Vector3(
                (float)(_random.NextDouble() - 0.5) * ageRatio,
                (float)(_random.NextDouble() - 0.5) * ageRatio,
                (float)(_random.NextDouble() - 0.5) * ageRatio
            );
            
            Velocity = Velocity * (1f - deltaTime * 2f) + spread * deltaTime * 5f;
            Position += Velocity * deltaTime;
        }

        private void UpdateTrail()
        {
            if (WeaponType == WeaponType.Standard || WeaponType == WeaponType.Laser || WeaponType == WeaponType.Beam)
                return;

            _trailPositions.Insert(0, Position);
            if (_trailPositions.Count > MaxTrailPositions)
            {
                _trailPositions.RemoveAt(_trailPositions.Count - 1);
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(deltaTime);
                if (!_particles[i].IsActive)
                {
                    _particles.RemoveAt(i);
                }
            }

            // Create new particles for certain weapon types
            if (WeaponType == WeaponType.Flamethrower && _random.NextSingle() < 0.3f)
            {
                CreateFlameParticles();
            }
            else if (WeaponType == WeaponType.Plasma && _random.NextSingle() < 0.2f)
            {
                CreatePlasmaTrail();
            }
        }

        private void CreateFlameParticles()
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 particleVel = Velocity * 0.5f + new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 2f,
                    (float)(_random.NextDouble() - 0.5) * 2f,
                    (float)(_random.NextDouble() - 0.5) * 2f
                );
                
                _particles.Add(new ParticleEffect(Position, particleVel, Color.Orange, 0.8f));
            }
        }

        private void CreatePlasmaTrail()
        {
            Vector3 particleVel = -Velocity * 0.2f + new Vector3(
                (float)(_random.NextDouble() - 0.5) * 1f,
                (float)(_random.NextDouble() - 0.5) * 1f,
                (float)(_random.NextDouble() - 0.5) * 1f
            );
            
            _particles.Add(new ParticleEffect(Position, particleVel, Color.Cyan, 1.2f));
        }

        private void CheckBoundaries()
        {
            // Simple boundary check - can be enhanced with proper world bounds
            float maxDistance = 500f;
            if (Vector3.Distance(Vector3.Zero, Position) > maxDistance)
            {
                IsActive = false;
            }
        }

        public bool CheckCollision(Vector3 targetPosition, float targetRadius)
        {
            if (!IsActive || HasHitTarget) return false;

            switch (WeaponType)
            {
                case WeaponType.Laser:
                case WeaponType.Beam:
                    return CheckBeamCollision(targetPosition, targetRadius);
                    
                case WeaponType.Flamethrower:
                    return CheckFlameCollision(targetPosition, targetRadius);
                    
                default:
                    return CheckStandardCollision(targetPosition, targetRadius);
            }
        }

        private bool CheckStandardCollision(Vector3 targetPosition, float targetRadius)
        {
            float projectileRadius = WeaponStats.Type switch
            {
                WeaponType.Missile => 1.5f,
                WeaponType.Plasma => 1.2f,
                _ => 0.5f
            };

            float distance = Vector3.Distance(Position, targetPosition);
            return distance < (projectileRadius + targetRadius);
        }

        private bool CheckBeamCollision(Vector3 targetPosition, float targetRadius)
        {
            // Check if target intersects with beam line
            Vector3 beamDirection = Vector3.Normalize(_beamEnd - Position);
            Vector3 toTarget = targetPosition - Position;
            
            float projectionLength = Vector3.Dot(toTarget, beamDirection);
            projectionLength = Math.Max(0, Math.Min(projectionLength, Vector3.Distance(Position, _beamEnd)));
            
            Vector3 closestPoint = Position + beamDirection * projectionLength;
            float distance = Vector3.Distance(closestPoint, targetPosition);
            
            return distance < targetRadius + 0.5f;
        }

        private bool CheckFlameCollision(Vector3 targetPosition, float targetRadius)
        {
            // Flame has larger collision area and affects multiple times
            float flameRadius = 3f * (1f - Lifespan / _maxLifespan); // Expands over time
            float distance = Vector3.Distance(Position, targetPosition);
            return distance < (flameRadius + targetRadius);
        }

        public DamageInfo CalculateDamageInfo(Vector3 targetPosition)
        {
            float finalDamage = Damage;
            bool isCritical = false;
            bool hasSplash = WeaponStats.HasSplashDamage;
            float splashDamage = 0f;

            // Apply distance-based damage falloff for some weapons
            if (WeaponType == WeaponType.Shotgun || WeaponType == WeaponType.Flamethrower)
            {
                float distance = Vector3.Distance(Position, targetPosition);
                float maxRange = WeaponStats.Range;
                float falloff = Math.Max(0.3f, 1f - (distance / maxRange));
                finalDamage *= falloff;
            }

            // Calculate splash damage
            if (hasSplash)
            {
                float splashDistance = Vector3.Distance(Position, targetPosition);
                if (splashDistance <= WeaponStats.SplashRadius)
                {
                    float splashFalloff = 1f - (splashDistance / WeaponStats.SplashRadius);
                    splashDamage = WeaponStats.SplashDamage * splashFalloff;
                }
            }

            return new DamageInfo
            {
                DirectDamage = finalDamage,
                SplashDamage = splashDamage,
                DamageType = WeaponStats.DamageType,
                IsCritical = isCritical,
                HasPenetration = WeaponStats.PenetrationPower > 0,
                Knockback = WeaponStats.Knockback,
                SourcePosition = Position,
                WeaponType = WeaponType
            };
        }

        public void OnHitTarget()
        {
            HasHitTarget = true;
            
            // Handle penetration
            if (WeaponStats.PenetrationPower <= 0)
            {
                IsActive = false;
            }
            else
            {
                // Reduce damage and speed for penetrating weapons
                Damage *= WeaponStats.PenetrationPower;
                Velocity *= 0.8f;
            }

            // Handle chain lightning
            if (WeaponType == WeaponType.Lightning)
            {
                // Implementation for chaining to nearby targets would go here
                // This would require access to the game's target list
            }

            // Handle explosive weapons
            if (WeaponStats.HasSplashDamage)
            {
                CreateExplosionEffect();
            }
        }

        private void CreateExplosionEffect()
        {
            // Create explosion particles
            for (int i = 0; i < 15; i++)
            {
                Vector3 explosionVel = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 10f,
                    (float)(_random.NextDouble() - 0.5) * 10f,
                    (float)(_random.NextDouble() - 0.5) * 10f
                );
                
                Color explosionColor = WeaponType switch
                {
                    WeaponType.Missile => Color.Orange,
                    WeaponType.Plasma => Color.Cyan,
                    _ => Color.Yellow
                };
                
                _particles.Add(new ParticleEffect(Position, explosionVel, explosionColor, 1.5f));
            }
        }

        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;

            // Draw particles first
            foreach (var particle in _particles)
            {
                particle.Draw(camera);
            }

            // Draw weapon-specific visuals
            switch (WeaponType)
            {
                case WeaponType.Standard:
                    DrawStandardProjectile(camera);
                    break;
                    
                case WeaponType.Plasma:
                    DrawPlasmaProjectile(camera);
                    break;
                    
                case WeaponType.Laser:
                case WeaponType.Beam:
                    DrawBeamProjectile(camera);
                    break;
                    
                case WeaponType.Missile:
                    DrawMissileProjectile(camera);
                    break;
                    
                case WeaponType.Shotgun:
                    DrawShotgunPellet(camera);
                    break;
                    
                case WeaponType.Flamethrower:
                    DrawFlameProjectile(camera);
                    break;
                    
                case WeaponType.Lightning:
                    DrawLightningProjectile(camera);
                    break;
                    
                default:
                    DrawStandardProjectile(camera);
                    break;
            }

            DrawTrail(camera);
        }

        private void DrawStandardProjectile(Camera3D camera)
        {
            Raylib.DrawSphere(Position, 0.3f, Color.Yellow);
        }

        private void DrawPlasmaProjectile(Camera3D camera)
        {
            // Draw glowing plasma ball
            Raylib.DrawSphere(Position, 0.5f, Color.Cyan);
            Raylib.DrawSphereWires(Position, 0.7f, 8, 8, Color.Blue);
        }

        private void DrawBeamProjectile(Camera3D camera)
        {
            // Draw continuous beam
            Raylib.DrawLine3D(Position, _beamEnd, Color.Red);
            
            // Draw beam segments for visual effect
            Vector3 direction = Vector3.Normalize(_beamEnd - Position);
            float distance = Vector3.Distance(Position, _beamEnd);
            int segments = (int)(distance / 2f);
            
            for (int i = 0; i < segments; i++)
            {
                Vector3 segmentPos = Position + direction * (i * 2f);
                Raylib.DrawSphere(segmentPos, 0.2f, Color.White);
            }
        }

        private void DrawMissileProjectile(Camera3D camera)
        {
            // Draw missile body
            Raylib.DrawSphere(Position, 0.4f, Color.Gray);
            
            // Draw missile trail
            Vector3 trailStart = Position - Vector3.Normalize(Velocity) * 2f;
            Raylib.DrawLine3D(Position, trailStart, Color.Orange);
        }

        private void DrawShotgunPellet(Camera3D camera)
        {
            Raylib.DrawSphere(Position, 0.2f, Color.Yellow);
        }

        private void DrawFlameProjectile(Camera3D camera)
        {
            // Draw flame as expanding sphere with decreasing alpha
            float ageRatio = 1f - (Lifespan / _maxLifespan);
            float radius = 0.5f + ageRatio * 2f;
            
            Color flameColor = Color.Orange;
            flameColor.A = (byte)(255 * (1f - ageRatio));
            
            Raylib.DrawSphere(Position, radius, flameColor);
        }

        private void DrawLightningProjectile(Camera3D camera)
        {
            // Draw jagged lightning bolt
            Vector3 start = Position - Vector3.Normalize(Velocity) * 1f;
            Vector3 end = Position;
            
            // Create jagged line effect
            int segments = 5;
            Vector3 direction = (end - start) / segments;
            Vector3 currentPos = start;
            
            for (int i = 0; i < segments; i++)
            {
                Vector3 nextPos = currentPos + direction;
                
                // Add random jitter
                Vector3 jitter = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 0.5f,
                    (float)(_random.NextDouble() - 0.5) * 0.5f,
                    (float)(_random.NextDouble() - 0.5) * 0.5f
                );
                nextPos += jitter;
                
                Raylib.DrawLine3D(currentPos, nextPos, Color.White);
                currentPos = nextPos;
            }
        }

        private void DrawTrail(Camera3D camera)
        {
            if (_trailPositions.Count < 2) return;

            Color trailColor = WeaponType switch
            {
                WeaponType.Plasma => Color.Cyan,
                WeaponType.Missile => Color.Orange,
                WeaponType.Lightning => Color.White,
                _ => Color.Yellow
            };

            for (int i = 0; i < _trailPositions.Count - 1; i++)
            {
                float alpha = 1f - ((float)i / _trailPositions.Count);
                trailColor.A = (byte)(255 * alpha);
                
                Raylib.DrawLine3D(_trailPositions[i], _trailPositions[i + 1], trailColor);
            }
        }

        private float GetTime() => (float)Raylib.GetTime();
    }

    /// <summary>
    /// Damage information for hit calculations
    /// </summary>
    public struct DamageInfo
    {
        public float DirectDamage;
        public float SplashDamage;
        public DamageType DamageType;
        public bool IsCritical;
        public bool HasPenetration;
        public float Knockback;
        public Vector3 SourcePosition;
        public WeaponType WeaponType;
    }

    /// <summary>
    /// Simple particle effect for weapon visuals
    /// </summary>
    public class ParticleEffect
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Color Color { get; private set; }
        public bool IsActive { get; private set; }
        
        private float _lifespan;
        private float _maxLifespan;

        public ParticleEffect(Vector3 position, Vector3 velocity, Color color, float lifespan)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            _lifespan = lifespan;
            _maxLifespan = lifespan;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            Position += Velocity * deltaTime;
            Velocity *= 0.98f; // Slight damping
            _lifespan -= deltaTime;

            if (_lifespan <= 0)
            {
                IsActive = false;
            }
            else
            {
                // Fade alpha over time
                float alpha = _lifespan / _maxLifespan;
                Color.A = (byte)(255 * alpha);
            }
        }

        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            float size = (_lifespan / _maxLifespan) * 0.3f;
            Raylib.DrawSphere(Position, size, Color);
        }
    }
}