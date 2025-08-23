using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Advanced 3D projectile with enhanced physics, effects, and behaviors
    /// </summary>
    public class AdvancedProjectile3D
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Acceleration { get; private set; }
        public bool IsActive { get; private set; }
        public AdvancedWeaponStats WeaponStats { get; private set; }
        public float Damage { get; private set; }
        public float Lifespan { get; private set; }
        public int EvolutionLevel { get; private set; }
        public bool HasHitTarget { get; private set; }
        
        private float _maxLifespan;
        private float _creationTime;
        private List<Vector3> _trailPositions;
        private List<StatusEffect> _statusEffects;
        private List<AdvancedParticleEffect> _particles;
        private Random _random;
        
        // Advanced behaviors
        private Vector3? _homingTarget;
        private List<Vector3> _ricochetHistory;
        private int _remainingRicochets;
        private float _evolutionProgress;
        private bool _hasDelayedDetonation;
        private float _detonationTimer;
        private List<AdvancedProjectile3D> _childProjectiles;
        
        // Nanite swarm specific
        private int _naniteCount;
        private float _replicationTimer;
        private List<NaniteUnit> _nanites;
        
        // Quantum cannon specific
        private List<Vector3> _quantumPositions;
        private float _quantumPhase;
        private bool _isInSuperposition;
        
        // Cluster bomb specific
        private bool _hasDetonated;
        private int _clusterCount;
        
        // Visual effects
        private float _visualScale;
        private Color _currentColor;
        private float _glowIntensity;
        private TrailRenderer _trailRenderer;
        
        public AdvancedProjectile3D(Vector3 position, Vector3 velocity, AdvancedWeaponStats stats, float damage)
        {
            Position = position;
            Velocity = velocity;
            Acceleration = Vector3.Zero;
            WeaponStats = stats;
            Damage = damage;
            IsActive = true;
            _maxLifespan = CalculateLifespan();
            Lifespan = _maxLifespan;
            _creationTime = GetTime();
            EvolutionLevel = 1;
            
            _trailPositions = new List<Vector3>();
            _statusEffects = new List<StatusEffect>();
            _particles = new List<AdvancedParticleEffect>();
            _random = new Random();
            _ricochetHistory = new List<Vector3>();
            _childProjectiles = new List<AdvancedProjectile3D>();
            _nanites = new List<NaniteUnit>();
            _quantumPositions = new List<Vector3>();
            
            _remainingRicochets = stats.MaxRicochets;
            _evolutionProgress = 0f;
            _hasDelayedDetonation = stats.HasDelayedDetonation;
            _detonationTimer = stats.DetonationDelay;
            _naniteCount = 1;
            _quantumPhase = 0f;
            _clusterCount = 8; // Default cluster bomb count
            
            _visualScale = stats.VisualScale;
            _currentColor = stats.ProjectileColor;
            _glowIntensity = 1f;
            _trailRenderer = new TrailRenderer(stats.TrailEffect);
            
            InitializeSpecialBehaviors();
        }
        
        private void InitializeSpecialBehaviors()
        {
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.NaniteSwarm:
                    InitializeNaniteSwarm();
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    InitializeQuantumStates();
                    break;
                    
                case AdvancedWeaponType.ClusterBomb:
                    InitializeClusterBomb();
                    break;
                    
                case AdvancedWeaponType.GravityGun:
                    InitializeGravityField();
                    break;
                    
                case AdvancedWeaponType.Morphic:
                    InitializeMorphicProperties();
                    break;
            }
        }
        
        private void InitializeNaniteSwarm()
        {
            for (int i = 0; i < _naniteCount; i++)
            {
                var nanite = new NaniteUnit(Position, _random);
                _nanites.Add(nanite);
            }
        }
        
        private void InitializeQuantumStates()
        {
            _isInSuperposition = true;
            int stateCount = 3; // Exist in 3 quantum states simultaneously
            
            for (int i = 0; i < stateCount; i++)
            {
                Vector3 offset = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 5f,
                    (float)(_random.NextDouble() - 0.5) * 5f,
                    (float)(_random.NextDouble() - 0.5) * 5f
                );
                _quantumPositions.Add(Position + offset);
            }
        }
        
        private void InitializeClusterBomb()
        {
            // Cluster bombs start as single projectile, split on detonation
        }
        
        private void InitializeGravityField()
        {
            // Gravity guns create attraction/repulsion fields
        }
        
        private void InitializeMorphicProperties()
        {
            // Morphic weapons change properties during flight
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            float currentTime = GetTime();
            float age = currentTime - _creationTime;
            
            UpdateLifespan(deltaTime);
            UpdateMovement(deltaTime);
            UpdateSpecialBehaviors(deltaTime, age);
            UpdateStatusEffects(deltaTime);
            UpdateVisualEffects(deltaTime, age);
            UpdateParticles(deltaTime);
            UpdateTrail();
            CheckBoundaries();
        }
        
        private void UpdateLifespan(float deltaTime)
        {
            Lifespan -= deltaTime;
            if (Lifespan <= 0)
            {
                if (_hasDelayedDetonation && !_hasDetonated)
                {
                    TriggerDetonation();
                }
                else
                {
                    IsActive = false;
                }
            }
            
            if (_hasDelayedDetonation && _detonationTimer > 0)
            {
                _detonationTimer -= deltaTime;
                if (_detonationTimer <= 0 && !_hasDetonated)
                {
                    TriggerDetonation();
                }
            }
        }
        
        private void UpdateMovement(float deltaTime)
        {
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.GaussRifle:
                    UpdateLinearMovement(deltaTime);
                    break;
                    
                case AdvancedWeaponType.NaniteSwarm:
                    UpdateNaniteMovement(deltaTime);
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    UpdateQuantumMovement(deltaTime);
                    break;
                    
                case AdvancedWeaponType.GravityGun:
                    UpdateGravityMovement(deltaTime);
                    break;
                    
                default:
                    UpdateLinearMovement(deltaTime);
                    break;
            }
            
            // Apply homing if enabled
            if (WeaponStats.HasHomingCapability && _homingTarget.HasValue)
            {
                ApplyHomingBehavior(deltaTime);
            }
        }
        
        private void UpdateLinearMovement(float deltaTime)
        {
            Velocity += Acceleration * deltaTime;
            Position += Velocity * deltaTime;
        }
        
        private void UpdateNaniteMovement(float deltaTime)
        {
            // Update individual nanites
            foreach (var nanite in _nanites.Where(n => n.IsActive))
            {
                nanite.Update(deltaTime, Position);
            }
            
            // Update swarm center position
            Position += Velocity * deltaTime;
            
            // Nanite replication
            _replicationTimer += deltaTime;
            if (_replicationTimer >= 1f && _naniteCount < 20)
            {
                ReplicateNanites();
                _replicationTimer = 0f;
            }
        }
        
        private void UpdateQuantumMovement(float deltaTime)
        {
            _quantumPhase += deltaTime * 2f;
            
            // Update quantum state positions
            for (int i = 0; i < _quantumPositions.Count; i++)
            {
                Vector3 quantumVelocity = Velocity;
                float phaseOffset = (float)i / _quantumPositions.Count * MathF.PI * 2f;
                
                // Add quantum uncertainty
                Vector3 uncertainty = new Vector3(
                    MathF.Sin(_quantumPhase + phaseOffset) * 2f,
                    MathF.Cos(_quantumPhase + phaseOffset * 1.5f) * 2f,
                    MathF.Sin(_quantumPhase * 1.3f + phaseOffset) * 2f
                );
                
                _quantumPositions[i] += (quantumVelocity + uncertainty) * deltaTime;
            }
            
            // Main position is average of quantum states
            Position = _quantumPositions.Aggregate(Vector3.Zero, (sum, pos) => sum + pos) / _quantumPositions.Count;
        }
        
        private void UpdateGravityMovement(float deltaTime)
        {
            // Gravity projectiles create attraction/repulsion fields
            Position += Velocity * deltaTime;
            
            // Create gravity well effect around projectile
            CreateGravityField(Position, 10f, WeaponStats.ElementalDamage);
        }
        
        private void ApplyHomingBehavior(float deltaTime)
        {
            if (!_homingTarget.HasValue) return;
            
            Vector3 directionToTarget = Vector3.Normalize(_homingTarget.Value - Position);
            Vector3 currentDirection = Vector3.Normalize(Velocity);
            
            Vector3 newDirection = Vector3.Lerp(currentDirection, directionToTarget, 
                WeaponStats.HomingStrength * deltaTime);
            
            Velocity = newDirection * Velocity.Length();
        }
        
        private void UpdateSpecialBehaviors(float deltaTime, float age)
        {
            // Evolution for evolutionary weapons
            if (WeaponStats.CanEvolve)
            {
                _evolutionProgress += deltaTime * WeaponStats.EvolutionRate;
                if (_evolutionProgress >= 1f && EvolutionLevel < WeaponStats.MaxEvolutionLevel)
                {
                    EvolveProjectile();
                    _evolutionProgress = 0f;
                }
            }
            
            // Morphic behavior changes
            if (WeaponStats.Type == AdvancedWeaponType.Morphic)
            {
                UpdateMorphicBehavior(age);
            }
        }
        
        private void EvolveProjectile()
        {
            EvolutionLevel++;
            
            // Increase damage and other properties
            Damage *= 1.2f;
            _visualScale *= 1.1f;
            _glowIntensity *= 1.15f;
            
            // Add new status effects
            if (EvolutionLevel >= 3)
            {
                var newEffect = new StatusEffect(StatusEffectType.DamageBoost, 5f, 0.5f, Position);
                _statusEffects.Add(newEffect);
            }
        }
        
        private void UpdateMorphicBehavior(float age)
        {
            // Change properties based on flight time
            float morphPhase = age / 3f; // Complete cycle every 3 seconds
            
            if (morphPhase < 1f)
            {
                // Phase 1: High speed, low damage
                Velocity = Vector3.Normalize(Velocity) * WeaponStats.ProjectileSpeed * 1.5f;
                Damage = WeaponStats.BaseDamage * 0.7f;
                _currentColor = Color.Blue;
            }
            else if (morphPhase < 2f)
            {
                // Phase 2: Medium speed, medium damage, piercing
                Velocity = Vector3.Normalize(Velocity) * WeaponStats.ProjectileSpeed;
                Damage = WeaponStats.BaseDamage;
                _currentColor = Color.Purple;
            }
            else
            {
                // Phase 3: Low speed, high damage, area effect
                Velocity = Vector3.Normalize(Velocity) * WeaponStats.ProjectileSpeed * 0.6f;
                Damage = WeaponStats.BaseDamage * 1.8f;
                _currentColor = Color.Red;
            }
        }
        
        private void UpdateStatusEffects(float deltaTime)
        {
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                _statusEffects[i].Update(deltaTime);
                if (!_statusEffects[i].IsActive)
                {
                    _statusEffects.RemoveAt(i);
                }
            }
        }
        
        private void UpdateVisualEffects(float deltaTime, float age)
        {
            // Update glow intensity based on age and evolution
            float baseIntensity = 1f + (EvolutionLevel - 1) * 0.3f;
            _glowIntensity = baseIntensity * (1f + MathF.Sin(age * 5f) * 0.2f);
            
            // Update visual scale
            _visualScale = WeaponStats.VisualScale * (1f + (EvolutionLevel - 1) * 0.2f);
            
            // Special visual effects for different weapon types
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.QuantumCannon:
                    _currentColor = ColorLerp(WeaponStats.ProjectileColor, 
                        WeaponStats.SecondaryColor, 
                        (MathF.Sin(_quantumPhase) + 1f) / 2f);
                    break;
                    
                case AdvancedWeaponType.CryoLaser:
                    _glowIntensity *= 1.5f; // Brighter glow for laser
                    CreateIceParticles();
                    break;
                    
                case AdvancedWeaponType.NaniteSwarm:
                    CreateNaniteParticles();
                    break;
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
        }
        
        private void UpdateTrail()
        {
            if (WeaponStats.HasTrail)
            {
                _trailRenderer.AddPosition(Position);
                _trailPositions.Insert(0, Position);
                
                int maxTrailLength = 15;
                if (_trailPositions.Count > maxTrailLength)
                {
                    _trailPositions.RemoveAt(_trailPositions.Count - 1);
                }
            }
        }
        
        private void CheckBoundaries()
        {
            float maxDistance = 500f;
            if (Vector3.Distance(Vector3.Zero, Position) > maxDistance)
            {
                IsActive = false;
            }
        }
        
        public bool CheckCollision(Vector3 targetPosition, float targetRadius)
        {
            if (!IsActive || HasHitTarget) return false;
            
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.QuantumCannon:
                    return CheckQuantumCollision(targetPosition, targetRadius);
                    
                case AdvancedWeaponType.NaniteSwarm:
                    return CheckNaniteCollision(targetPosition, targetRadius);
                    
                case AdvancedWeaponType.CryoLaser:
                    return CheckBeamCollision(targetPosition, targetRadius);
                    
                default:
                    return CheckStandardCollision(targetPosition, targetRadius);
            }
        }
        
        private bool CheckQuantumCollision(Vector3 targetPosition, float targetRadius)
        {
            // Check collision with any quantum state
            foreach (var quantumPos in _quantumPositions)
            {
                float distance = Vector3.Distance(quantumPos, targetPosition);
                if (distance < targetRadius + _visualScale)
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool CheckNaniteCollision(Vector3 targetPosition, float targetRadius)
        {
            // Check collision with nanite swarm area
            float swarmRadius = _naniteCount * 0.5f;
            float distance = Vector3.Distance(Position, targetPosition);
            return distance < (targetRadius + swarmRadius);
        }
        
        private bool CheckBeamCollision(Vector3 targetPosition, float targetRadius)
        {
            // Beam weapons have instant hit along their path
            Vector3 beamStart = Position;
            Vector3 beamEnd = Position + Vector3.Normalize(Velocity) * WeaponStats.Range;
            
            return IsPointNearLine(targetPosition, beamStart, beamEnd, targetRadius);
        }
        
        private bool CheckStandardCollision(Vector3 targetPosition, float targetRadius)
        {
            float distance = Vector3.Distance(Position, targetPosition);
            return distance < (targetRadius + _visualScale);
        }
        
        private bool IsPointNearLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd, float threshold)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            Vector3 toPoint = point - lineStart;
            
            float projectionLength = Vector3.Dot(toPoint, Vector3.Normalize(lineDirection));
            projectionLength = Math.Max(0, Math.Min(projectionLength, lineDirection.Length()));
            
            Vector3 closestPoint = lineStart + Vector3.Normalize(lineDirection) * projectionLength;
            float distance = Vector3.Distance(point, closestPoint);
            
            return distance < threshold;
        }
        
        public AdvancedDamageInfo CalculateDamageInfo(Vector3 targetPosition)
        {
            float finalDamage = Damage;
            List<StatusEffectType> appliedEffects = new List<StatusEffectType>();
            
            // Apply evolution bonuses
            finalDamage *= (1f + (EvolutionLevel - 1) * 0.2f);
            
            // Apply elemental effects
            if (WeaponStats.ElementType != ElementalType.Kinetic)
            {
                finalDamage += WeaponStats.ElementalDamage;
                appliedEffects.AddRange(GetElementalEffects(WeaponStats.ElementType));
            }
            
            // Apply weapon-specific status effects
            if (WeaponStats.CreatesStatusEffects)
            {
                appliedEffects.AddRange(WeaponStats.StatusEffects);
            }
            
            // Distance-based falloff for certain weapons
            if (WeaponStats.Type == AdvancedWeaponType.NaniteSwarm ||
                WeaponStats.Type == AdvancedWeaponType.CryoLaser)
            {
                float distance = Vector3.Distance(Position, targetPosition);
                float falloff = Math.Max(0.3f, 1f - (distance / WeaponStats.Range));
                finalDamage *= falloff;
            }
            
            return new AdvancedDamageInfo
            {
                DirectDamage = finalDamage,
                ElementalDamage = WeaponStats.ElementalDamage,
                ElementType = WeaponStats.ElementType,
                StatusEffects = appliedEffects.ToArray(),
                HasAreaEffect = WeaponStats.HasAreaEffect,
                AreaRadius = WeaponStats.AreaRadius,
                AreaDamage = WeaponStats.AreaDamage,
                CanPierceShields = WeaponStats.CanPierceShields,
                ShieldPenetration = WeaponStats.ShieldPenetration,
                SourcePosition = Position,
                WeaponType = WeaponStats.Type,
                EvolutionLevel = EvolutionLevel
            };
        }
        
        private StatusEffectType[] GetElementalEffects(ElementalType elementType)
        {
            return elementType switch
            {
                ElementalType.Ice => new[] { StatusEffectType.Frozen, StatusEffectType.Slowed },
                ElementalType.Fire => new[] { StatusEffectType.Burning },
                ElementalType.Electromagnetic => new[] { StatusEffectType.Stunned, StatusEffectType.Drained },
                ElementalType.Quantum => new[] { StatusEffectType.Quantum, StatusEffectType.Confused },
                ElementalType.Technological => new[] { StatusEffectType.Corroded, StatusEffectType.Weakness },
                ElementalType.Restorative => new[] { StatusEffectType.Regeneration, StatusEffectType.ShieldBoost },
                _ => new StatusEffectType[0]
            };
        }
        
        public void OnHitTarget(Vector3 hitPosition)
        {
            HasHitTarget = true;
            
            // Handle special hit behaviors
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.ClusterBomb:
                    TriggerClusterDetonation(hitPosition);
                    break;
                    
                case AdvancedWeaponType.NaniteSwarm:
                    SpreadNanites(hitPosition);
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    CollapseQuantumStates(hitPosition);
                    break;
            }
            
            // Handle ricochets
            if (WeaponStats.CanRicochet && _remainingRicochets > 0)
            {
                PerformRicochet(hitPosition);
                return;
            }
            
            // Create impact effects
            CreateImpactEffects(hitPosition);
            
            // Deactivate if not piercing
            if (!WeaponStats.CanPierceShields)
            {
                IsActive = false;
            }
        }
        
        private void TriggerDetonation()
        {
            if (_hasDetonated) return;
            _hasDetonated = true;
            
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.ClusterBomb:
                    TriggerClusterDetonation(Position);
                    break;
                    
                default:
                    CreateExplosionEffect(Position, WeaponStats.AreaRadius, WeaponStats.AreaDamage);
                    break;
            }
            
            IsActive = false;
        }
        
        private void TriggerClusterDetonation(Vector3 center)
        {
            // Create multiple smaller explosions
            for (int i = 0; i < _clusterCount; i++)
            {
                float angle = (float)i / _clusterCount * MathF.PI * 2f;
                Vector3 clusterPosition = center + new Vector3(
                    MathF.Cos(angle) * 3f,
                    (float)(_random.NextDouble() - 0.5) * 2f,
                    MathF.Sin(angle) * 3f
                );
                
                CreateExplosionEffect(clusterPosition, WeaponStats.AreaRadius * 0.6f, WeaponStats.AreaDamage * 0.8f);
            }
        }
        
        private void SpreadNanites(Vector3 position)
        {
            // Nanites spread to nearby targets
            foreach (var nanite in _nanites.Where(n => n.IsActive))
            {
                nanite.SetTargetPosition(position);
            }
        }
        
        private void CollapseQuantumStates(Vector3 position)
        {
            // All quantum states collapse to hit position
            _quantumPositions.Clear();
            _quantumPositions.Add(position);
            _isInSuperposition = false;
            
            // Create quantum explosion
            CreateQuantumExplosion(position);
        }
        
        private void PerformRicochet(Vector3 hitPosition)
        {
            _remainingRicochets--;
            _ricochetHistory.Add(hitPosition);
            
            // Calculate new velocity based on surface normal (simplified)
            Vector3 reflection = Vector3.Reflect(Velocity, Vector3.UnitY);
            Velocity = reflection * WeaponStats.RicochetDamageRetention;
            Damage *= WeaponStats.RicochetDamageRetention;
            
            // Reset hit state
            HasHitTarget = false;
            
            // Create ricochet effect
            CreateRicochetEffect(hitPosition);
        }
        
        private void ReplicateNanites()
        {
            if (_naniteCount >= 20) return;
            
            int newNanites = Math.Min(2, 20 - _naniteCount);
            for (int i = 0; i < newNanites; i++)
            {
                var nanite = new NaniteUnit(Position, _random);
                _nanites.Add(nanite);
                _naniteCount++;
            }
            
            // Increase damage with more nanites
            Damage *= 1.1f;
        }
        
        private void CreateIceParticles()
        {
            var particle = new AdvancedParticleEffect(
                Position + GetRandomOffset(1f),
                Velocity * 0.3f + GetRandomVelocity(2f),
                Color.LightBlue,
                1f,
                ParticleEffectType.IceCrystals
            );
            _particles.Add(particle);
        }
        
        private void CreateNaniteParticles()
        {
            var particle = new AdvancedParticleEffect(
                Position + GetRandomOffset(0.5f),
                GetRandomVelocity(1f),
                Color.Gray,
                0.8f,
                ParticleEffectType.NaniteCloud
            );
            _particles.Add(particle);
        }
        
        private void CreateExplosionEffect(Vector3 position, float radius, float damage)
        {
            // Create explosion particles
            for (int i = 0; i < 20; i++)
            {
                var particle = new AdvancedParticleEffect(
                    position + GetRandomOffset(radius * 0.5f),
                    GetRandomVelocity(10f),
                    WeaponStats.ProjectileColor,
                    1.5f,
                    ParticleEffectType.Explosion
                );
                _particles.Add(particle);
            }
        }
        
        private void CreateQuantumExplosion(Vector3 position)
        {
            // Create quantum distortion effect
            for (int i = 0; i < 15; i++)
            {
                var particle = new AdvancedParticleEffect(
                    position + GetRandomOffset(3f),
                    GetRandomVelocity(8f),
                    Color.Purple,
                    2f,
                    ParticleEffectType.QuantumDistortion
                );
                _particles.Add(particle);
            }
        }
        
        private void CreateImpactEffects(Vector3 position)
        {
            // Create impact-specific particles based on weapon type
            ParticleEffectType effectType = WeaponStats.ParticleEffect;
            Color effectColor = _currentColor;
            
            for (int i = 0; i < 8; i++)
            {
                var particle = new AdvancedParticleEffect(
                    position + GetRandomOffset(1f),
                    GetRandomVelocity(5f),
                    effectColor,
                    1f,
                    effectType
                );
                _particles.Add(particle);
            }
        }
        
        private void CreateRicochetEffect(Vector3 position)
        {
            for (int i = 0; i < 5; i++)
            {
                var particle = new AdvancedParticleEffect(
                    position + GetRandomOffset(0.5f),
                    GetRandomVelocity(3f),
                    Color.White,
                    0.5f,
                    ParticleEffectType.MetallicShards
                );
                _particles.Add(particle);
            }
        }
        
        private void CreateGravityField(Vector3 center, float radius, float strength)
        {
            // Implementation would interact with game's physics system
            // to create attraction/repulsion effects on nearby objects
        }
        
        public void SetHomingTarget(Vector3 target)
        {
            _homingTarget = target;
        }
        
        public void ClearHomingTarget()
        {
            _homingTarget = null;
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
            DrawProjectileBody(camera);
            DrawGlowEffect(camera);
            DrawTrail(camera);
            DrawSpecialEffects(camera);
        }
        
        private void DrawProjectileBody(Camera3D camera)
        {
            switch (WeaponStats.Type)
            {
                case AdvancedWeaponType.GaussRifle:
                    DrawGaussRifleProjectile(camera);
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    DrawQuantumProjectile(camera);
                    break;
                    
                case AdvancedWeaponType.NaniteSwarm:
                    DrawNaniteSwarm(camera);
                    break;
                    
                case AdvancedWeaponType.CryoLaser:
                    DrawCryoLaser(camera);
                    break;
                    
                case AdvancedWeaponType.ClusterBomb:
                    DrawClusterBomb(camera);
                    break;
                    
                default:
                    DrawDefaultProjectile(camera);
                    break;
            }
        }
        
        private void DrawGaussRifleProjectile(Camera3D camera)
        {
            // Draw high-velocity electromagnetic projectile
            Raylib.DrawSphere(Position, _visualScale * 0.3f, _currentColor);
            
            // Draw electric field effect
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)i / 6 * MathF.PI * 2f;
                Vector3 sparkPos = Position + new Vector3(
                    MathF.Cos(angle) * _visualScale,
                    MathF.Sin(angle) * _visualScale,
                    0f
                );
                Raylib.DrawLine3D(Position, sparkPos, Color.White);
            }
        }
        
        private void DrawQuantumProjectile(Camera3D camera)
        {
            if (_isInSuperposition)
            {
                // Draw all quantum states with varying opacity
                for (int i = 0; i < _quantumPositions.Count; i++)
                {
                    Color stateColor = _currentColor;
                    stateColor.A = (byte)(255 / (_quantumPositions.Count - i));
                    
                    Raylib.DrawSphere(_quantumPositions[i], _visualScale, stateColor);
                    Raylib.DrawSphereWires(_quantumPositions[i], _visualScale * 1.2f, Color.Purple);
                }
                
                // Draw quantum field connections
                for (int i = 0; i < _quantumPositions.Count - 1; i++)
                {
                    Raylib.DrawLine3D(_quantumPositions[i], _quantumPositions[i + 1], Color.Magenta);
                }
            }
            else
            {
                // Collapsed state - single projectile
                Raylib.DrawSphere(Position, _visualScale, _currentColor);
                Raylib.DrawSphereWires(Position, _visualScale * 1.5f, Color.Purple);
            }
        }
        
        private void DrawNaniteSwarm(Camera3D camera)
        {
            // Draw individual nanites
            foreach (var nanite in _nanites.Where(n => n.IsActive))
            {
                nanite.Draw(camera);
            }
            
            // Draw swarm field
            float swarmRadius = _naniteCount * 0.3f;
            Color fieldColor = Color.Green;
            fieldColor.A = 50;
            Raylib.DrawSphere(Position, swarmRadius, fieldColor);
        }
        
        private void DrawCryoLaser(Camera3D camera)
        {
            // Draw continuous beam
            Vector3 beamEnd = Position + Vector3.Normalize(Velocity) * WeaponStats.Range;
            Raylib.DrawLine3D(Position, beamEnd, _currentColor);
            
            // Draw ice crystal effects along beam
            int crystalCount = (int)(WeaponStats.Range / 5f);
            for (int i = 0; i < crystalCount; i++)
            {
                float t = (float)i / crystalCount;
                Vector3 crystalPos = Vector3.Lerp(Position, beamEnd, t);
                Raylib.DrawSphere(crystalPos, 0.2f, Color.LightBlue);
            }
        }
        
        private void DrawClusterBomb(Camera3D camera)
        {
            // Draw main bomb body
            Raylib.DrawSphere(Position, _visualScale, _currentColor);
            Raylib.DrawSphereWires(Position, _visualScale * 1.2f, Color.Orange);
            
            // Draw cluster indicators
            for (int i = 0; i < 4; i++)
            {
                float angle = (float)i / 4 * MathF.PI * 2f;
                Vector3 indicatorPos = Position + new Vector3(
                    MathF.Cos(angle) * _visualScale * 0.8f,
                    0f,
                    MathF.Sin(angle) * _visualScale * 0.8f
                );
                Raylib.DrawSphere(indicatorPos, 0.2f, Color.Red);
            }
        }
        
        private void DrawDefaultProjectile(Camera3D camera)
        {
            Raylib.DrawSphere(Position, _visualScale, _currentColor);
        }
        
        private void DrawGlowEffect(Camera3D camera)
        {
            if (_glowIntensity <= 0) return;
            
            Color glowColor = _currentColor;
            for (int i = 1; i <= 3; i++)
            {
                float glowRadius = _visualScale * (1f + i * 0.4f);
                glowColor.A = (byte)(80 * _glowIntensity / i);
                Raylib.DrawSphere(Position, glowRadius, glowColor);
            }
        }
        
        private void DrawTrail(Camera3D camera)
        {
            if (!WeaponStats.HasTrail || _trailPositions.Count < 2) return;
            
            _trailRenderer.Draw(camera, _trailPositions, _currentColor);
        }
        
        private void DrawSpecialEffects(Camera3D camera)
        {
            // Draw evolution indicators
            if (EvolutionLevel > 1)
            {
                for (int i = 1; i < EvolutionLevel; i++)
                {
                    Vector3 starPos = Position + new Vector3(0, _visualScale * (1f + i * 0.3f), 0);
                    Raylib.DrawSphere(starPos, 0.1f, Color.Gold);
                }
            }
        }
        
        private Vector3 GetRandomOffset(float magnitude)
        {
            return new Vector3(
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude
            );
        }
        
        private Vector3 GetRandomVelocity(float magnitude)
        {
            return new Vector3(
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude
            );
        }
        
        private Color ColorLerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t),
                (byte)(a.A + (b.A - a.A) * t)
            );
        }
        
        private float CalculateLifespan()
        {
            if (WeaponStats.ProjectileSpeed == float.MaxValue)
                return 0.1f;
                
            return WeaponStats.Range / WeaponStats.ProjectileSpeed;
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    /// <summary>
    /// Advanced damage information with elemental and status effects
    /// </summary>
    public struct AdvancedDamageInfo
    {
        public float DirectDamage;
        public float ElementalDamage;
        public ElementalType ElementType;
        public StatusEffectType[] StatusEffects;
        public bool HasAreaEffect;
        public float AreaRadius;
        public float AreaDamage;
        public bool CanPierceShields;
        public float ShieldPenetration;
        public Vector3 SourcePosition;
        public AdvancedWeaponType WeaponType;
        public int EvolutionLevel;
    }
}