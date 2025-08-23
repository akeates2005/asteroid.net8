using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Manages all visual effects for weapons, projectiles, and abilities
    /// </summary>
    public class VisualEffectsManager
    {
        private List<VisualEffect> _activeEffects;
        private List<ParticleSystem> _particleSystems;
        private List<LightSource> _dynamicLights;
        private Dictionary<EffectType, EffectPool> _effectPools;
        private Random _random;
        
        // Performance settings
        private int _maxActiveEffects = 500;
        private int _maxParticleSystems = 50;
        private bool _enableAdvancedEffects = true;
        private float _effectsQuality = 1f; // 0.1 to 1.0
        
        public VisualEffectsManager()
        {
            _activeEffects = new List<VisualEffect>();
            _particleSystems = new List<ParticleSystem>();
            _dynamicLights = new List<LightSource>();
            _effectPools = new Dictionary<EffectType, EffectPool>();
            _random = new Random();
            
            InitializeEffectPools();
        }

        private void InitializeEffectPools()
        {
            foreach (EffectType effectType in Enum.GetValues<EffectType>())
            {
                _effectPools[effectType] = new EffectPool(effectType, 50);
            }
        }

        public void Update(float deltaTime)
        {
            UpdateActiveEffects(deltaTime);
            UpdateParticleSystems(deltaTime);
            UpdateDynamicLights(deltaTime);
            CleanupInactiveEffects();
        }

        private void UpdateActiveEffects(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Update(deltaTime);
                if (!_activeEffects[i].IsActive)
                {
                    ReturnEffectToPool(_activeEffects[i]);
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        private void UpdateParticleSystems(float deltaTime)
        {
            for (int i = _particleSystems.Count - 1; i >= 0; i--)
            {
                _particleSystems[i].Update(deltaTime);
                if (!_particleSystems[i].IsActive)
                {
                    _particleSystems.RemoveAt(i);
                }
            }
        }

        private void UpdateDynamicLights(float deltaTime)
        {
            for (int i = _dynamicLights.Count - 1; i >= 0; i--)
            {
                _dynamicLights[i].Update(deltaTime);
                if (!_dynamicLights[i].IsActive)
                {
                    _dynamicLights.RemoveAt(i);
                }
            }
        }

        private void CleanupInactiveEffects()
        {
            // Limit number of active effects for performance
            if (_activeEffects.Count > _maxActiveEffects)
            {
                int toRemove = _activeEffects.Count - _maxActiveEffects;
                for (int i = 0; i < toRemove; i++)
                {
                    ReturnEffectToPool(_activeEffects[0]);
                    _activeEffects.RemoveAt(0);
                }
            }
        }

        public void CreateMuzzleFlash(Vector3 position, Vector3 direction, WeaponType weaponType)
        {
            var effect = GetEffectFromPool(EffectType.MuzzleFlash);
            if (effect == null) return;

            effect.Initialize(position, direction, GetMuzzleFlashConfig(weaponType));
            _activeEffects.Add(effect);

            // Create dynamic light
            if (_enableAdvancedEffects)
            {
                var light = new LightSource(position, GetMuzzleFlashColor(weaponType), 2f, 0.2f);
                _dynamicLights.Add(light);
            }
        }

        public void CreateProjectileTrail(Vector3 position, Vector3 velocity, WeaponType weaponType)
        {
            if (!_enableAdvancedEffects) return;

            var effect = GetEffectFromPool(EffectType.ProjectileTrail);
            if (effect == null) return;

            effect.Initialize(position, velocity, GetTrailConfig(weaponType));
            _activeEffects.Add(effect);
        }

        public void CreateExplosion(Vector3 position, float radius, WeaponType weaponType)
        {
            // Main explosion effect
            var effect = GetEffectFromPool(EffectType.Explosion);
            if (effect != null)
            {
                effect.Initialize(position, Vector3.Zero, GetExplosionConfig(weaponType, radius));
                _activeEffects.Add(effect);
            }

            // Particle system
            var particleSystem = new ExplosionParticleSystem(position, radius, weaponType);
            _particleSystems.Add(particleSystem);

            // Dynamic light
            if (_enableAdvancedEffects)
            {
                var light = new LightSource(position, GetExplosionColor(weaponType), radius * 2f, 1f);
                _dynamicLights.Add(light);
            }

            // Screen shake effect would be handled here
            CreateScreenShake(0.3f, radius * 0.1f);
        }

        public void CreateImpactEffect(Vector3 position, Vector3 normal, WeaponType weaponType, float damage)
        {
            var effect = GetEffectFromPool(EffectType.Impact);
            if (effect == null) return;

            effect.Initialize(position, normal, GetImpactConfig(weaponType, damage));
            _activeEffects.Add(effect);

            // Sparks or debris particles
            if (_enableAdvancedEffects)
            {
                var sparks = new SparkParticleSystem(position, normal, damage);
                _particleSystems.Add(sparks);
            }
        }

        public void CreateBeamEffect(Vector3 start, Vector3 end, WeaponType weaponType, float intensity = 1f)
        {
            var effect = GetEffectFromPool(EffectType.Beam);
            if (effect == null) return;

            effect.Initialize(start, Vector3.Normalize(end - start), GetBeamConfig(weaponType, Vector3.Distance(start, end), intensity));
            _activeEffects.Add(effect);
        }

        public void CreateShieldEffect(Vector3 position, float radius, float intensity)
        {
            var effect = GetEffectFromPool(EffectType.Shield);
            if (effect == null) return;

            var config = new EffectConfig
            {
                Duration = 0.5f,
                Size = radius,
                Color = Color.Blue,
                Intensity = intensity,
                AnimationType = AnimationType.Pulse
            };

            effect.Initialize(position, Vector3.Zero, config);
            _activeEffects.Add(effect);
        }

        public void CreateAbilityEffect(Vector3 position, SpecialAbilityType abilityType, float duration)
        {
            var effect = GetEffectFromPool(EffectType.Ability);
            if (effect == null) return;

            effect.Initialize(position, Vector3.Zero, GetAbilityConfig(abilityType, duration));
            _activeEffects.Add(effect);

            // Special effects for certain abilities
            switch (abilityType)
            {
                case SpecialAbilityType.TimeDilation:
                    CreateTimeDistortionEffect(position, duration);
                    break;
                case SpecialAbilityType.MagneticField:
                    CreateMagneticFieldEffect(position, duration);
                    break;
                case SpecialAbilityType.EnergyOverload:
                    CreateEnergyOverloadEffect(position, duration);
                    break;
            }
        }

        private void CreateTimeDistortionEffect(Vector3 position, float duration)
        {
            var ripples = new RippleParticleSystem(position, 20f, duration);
            _particleSystems.Add(ripples);
        }

        private void CreateMagneticFieldEffect(Vector3 position, float duration)
        {
            var field = new MagneticFieldParticleSystem(position, 15f, duration);
            _particleSystems.Add(field);
        }

        private void CreateEnergyOverloadEffect(Vector3 position, float duration)
        {
            var lightning = new LightningParticleSystem(position, duration);
            _particleSystems.Add(lightning);
        }

        private void CreateScreenShake(float duration, float intensity)
        {
            // This would interface with the camera system
            // For now, just create a visual indicator
        }

        public void Draw(Camera3D camera)
        {
            // Draw all active effects
            foreach (var effect in _activeEffects)
            {
                effect.Draw(camera);
            }

            // Draw particle systems
            foreach (var system in _particleSystems)
            {
                system.Draw(camera);
            }

            // Dynamic lights would be applied to the lighting system here
        }

        private VisualEffect GetEffectFromPool(EffectType type)
        {
            if (_effectPools.ContainsKey(type))
            {
                return _effectPools[type].Get();
            }
            return null;
        }

        private void ReturnEffectToPool(VisualEffect effect)
        {
            if (_effectPools.ContainsKey(effect.Type))
            {
                _effectPools[effect.Type].Return(effect);
            }
        }

        private EffectConfig GetMuzzleFlashConfig(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Standard => new EffectConfig
                {
                    Duration = 0.1f,
                    Size = 1f,
                    Color = Color.Yellow,
                    Intensity = 1f,
                    AnimationType = AnimationType.FadeOut
                },
                WeaponType.Plasma => new EffectConfig
                {
                    Duration = 0.15f,
                    Size = 1.5f,
                    Color = Color.Cyan,
                    Intensity = 1.2f,
                    AnimationType = AnimationType.Pulse
                },
                WeaponType.Laser => new EffectConfig
                {
                    Duration = 0.05f,
                    Size = 0.8f,
                    Color = Color.Red,
                    Intensity = 1.5f,
                    AnimationType = AnimationType.Flash
                },
                WeaponType.Missile => new EffectConfig
                {
                    Duration = 0.2f,
                    Size = 2f,
                    Color = Color.Orange,
                    Intensity = 1.3f,
                    AnimationType = AnimationType.Expand
                },
                _ => new EffectConfig
                {
                    Duration = 0.1f,
                    Size = 1f,
                    Color = Color.White,
                    Intensity = 1f,
                    AnimationType = AnimationType.FadeOut
                }
            };
        }

        private EffectConfig GetTrailConfig(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Plasma => new EffectConfig
                {
                    Duration = 0.8f,
                    Size = 0.3f,
                    Color = Color.Cyan,
                    Intensity = 0.8f,
                    AnimationType = AnimationType.Trail
                },
                WeaponType.Missile => new EffectConfig
                {
                    Duration = 1.5f,
                    Size = 0.5f,
                    Color = Color.Orange,
                    Intensity = 1f,
                    AnimationType = AnimationType.Smoke
                },
                _ => new EffectConfig
                {
                    Duration = 0.3f,
                    Size = 0.2f,
                    Color = Color.Yellow,
                    Intensity = 0.6f,
                    AnimationType = AnimationType.Trail
                }
            };
        }

        private EffectConfig GetExplosionConfig(WeaponType weaponType, float radius)
        {
            return new EffectConfig
            {
                Duration = 0.8f,
                Size = radius,
                Color = GetExplosionColor(weaponType),
                Intensity = 1.5f,
                AnimationType = AnimationType.Explosion
            };
        }

        private EffectConfig GetImpactConfig(WeaponType weaponType, float damage)
        {
            float intensity = Math.Min(2f, damage / 50f);
            return new EffectConfig
            {
                Duration = 0.3f,
                Size = intensity,
                Color = GetMuzzleFlashColor(weaponType),
                Intensity = intensity,
                AnimationType = AnimationType.Impact
            };
        }

        private EffectConfig GetBeamConfig(WeaponType weaponType, float length, float intensity)
        {
            return new EffectConfig
            {
                Duration = 0.1f,
                Size = length,
                Color = GetMuzzleFlashColor(weaponType),
                Intensity = intensity,
                AnimationType = AnimationType.Beam
            };
        }

        private EffectConfig GetAbilityConfig(SpecialAbilityType abilityType, float duration)
        {
            return abilityType switch
            {
                SpecialAbilityType.TimeDilation => new EffectConfig
                {
                    Duration = duration,
                    Size = 10f,
                    Color = Color.Purple,
                    Intensity = 0.8f,
                    AnimationType = AnimationType.Ripple
                },
                SpecialAbilityType.MagneticField => new EffectConfig
                {
                    Duration = duration,
                    Size = 15f,
                    Color = Color.Blue,
                    Intensity = 0.6f,
                    AnimationType = AnimationType.Field
                },
                _ => new EffectConfig
                {
                    Duration = duration,
                    Size = 5f,
                    Color = Color.White,
                    Intensity = 1f,
                    AnimationType = AnimationType.Pulse
                }
            };
        }

        private Color GetMuzzleFlashColor(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Standard => Color.Yellow,
                WeaponType.Plasma => Color.Cyan,
                WeaponType.Laser => Color.Red,
                WeaponType.Missile => Color.Orange,
                WeaponType.Shotgun => Color.Yellow,
                WeaponType.Beam => Color.Purple,
                WeaponType.RailGun => Color.White,
                WeaponType.Pulse => Color.Green,
                WeaponType.Flamethrower => Color.Orange,
                WeaponType.Lightning => Color.White,
                _ => Color.White
            };
        }

        private Color GetExplosionColor(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Plasma => Color.Cyan,
                WeaponType.Missile => Color.Orange,
                WeaponType.Lightning => Color.White,
                _ => Color.Orange
            };
        }

        // Performance settings
        public void SetEffectsQuality(float quality)
        {
            _effectsQuality = Math.Clamp(quality, 0.1f, 1f);
            _maxActiveEffects = (int)(500 * _effectsQuality);
            _maxParticleSystems = (int)(50 * _effectsQuality);
        }

        public void SetAdvancedEffects(bool enabled)
        {
            _enableAdvancedEffects = enabled;
        }

        // Cleanup
        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                ReturnEffectToPool(effect);
            }
            _activeEffects.Clear();
            _particleSystems.Clear();
            _dynamicLights.Clear();
        }
    }

    /// <summary>
    /// Types of visual effects
    /// </summary>
    public enum EffectType
    {
        MuzzleFlash,
        ProjectileTrail,
        Explosion,
        Impact,
        Beam,
        Shield,
        Ability,
        Smoke,
        Sparks,
        Debris
    }

    /// <summary>
    /// Animation types for effects
    /// </summary>
    public enum AnimationType
    {
        FadeOut,
        Pulse,
        Flash,
        Expand,
        Trail,
        Smoke,
        Explosion,
        Impact,
        Beam,
        Ripple,
        Field
    }

    /// <summary>
    /// Configuration for visual effects
    /// </summary>
    public struct EffectConfig
    {
        public float Duration;
        public float Size;
        public Color Color;
        public float Intensity;
        public AnimationType AnimationType;
    }

    /// <summary>
    /// Base class for visual effects
    /// </summary>
    public abstract class VisualEffect
    {
        public EffectType Type { get; protected set; }
        public Vector3 Position { get; protected set; }
        public Vector3 Direction { get; protected set; }
        public bool IsActive { get; protected set; }
        public float Age { get; protected set; }
        
        protected EffectConfig _config;
        protected float _currentIntensity;
        protected float _currentSize;
        protected Color _currentColor;

        public virtual void Initialize(Vector3 position, Vector3 direction, EffectConfig config)
        {
            Position = position;
            Direction = direction;
            _config = config;
            IsActive = true;
            Age = 0f;
            _currentIntensity = config.Intensity;
            _currentSize = config.Size;
            _currentColor = config.Color;
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsActive) return;

            Age += deltaTime;
            if (Age >= _config.Duration)
            {
                IsActive = false;
                return;
            }

            UpdateAnimation(deltaTime);
        }

        protected virtual void UpdateAnimation(float deltaTime)
        {
            float progress = Age / _config.Duration;
            
            switch (_config.AnimationType)
            {
                case AnimationType.FadeOut:
                    _currentIntensity = _config.Intensity * (1f - progress);
                    break;
                    
                case AnimationType.Pulse:
                    _currentIntensity = _config.Intensity * (0.5f + 0.5f * MathF.Sin(Age * 10f));
                    break;
                    
                case AnimationType.Expand:
                    _currentSize = _config.Size * (1f + progress * 2f);
                    _currentIntensity = _config.Intensity * (1f - progress);
                    break;
            }

            // Update alpha based on intensity
            _currentColor.A = (byte)(255 * _currentIntensity);
        }

        public abstract void Draw(Camera3D camera);

        public virtual void Reset()
        {
            IsActive = false;
            Age = 0f;
        }
    }

    /// <summary>
    /// Object pool for visual effects
    /// </summary>
    public class EffectPool
    {
        private Stack<VisualEffect> _pool;
        private EffectType _type;

        public EffectPool(EffectType type, int initialSize)
        {
            _type = type;
            _pool = new Stack<VisualEffect>(initialSize);
            
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(CreateEffect(type));
            }
        }

        private VisualEffect CreateEffect(EffectType type)
        {
            return type switch
            {
                EffectType.MuzzleFlash => new MuzzleFlashEffect(),
                EffectType.Explosion => new ExplosionEffect(),
                EffectType.Impact => new ImpactEffect(),
                EffectType.Beam => new BeamEffect(),
                EffectType.Shield => new ShieldEffect(),
                _ => new GenericEffect(type)
            };
        }

        public VisualEffect Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return CreateEffect(_type);
        }

        public void Return(VisualEffect effect)
        {
            effect.Reset();
            _pool.Push(effect);
        }
    }

    // Specific effect implementations
    public class MuzzleFlashEffect : VisualEffect
    {
        public MuzzleFlashEffect()
        {
            Type = EffectType.MuzzleFlash;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            Raylib.DrawSphere(Position, _currentSize, _currentColor);
            
            // Draw additional flash lines
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * MathF.PI * 2f;
                Vector3 offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * _currentSize;
                Raylib.DrawLine3D(Position, Position + offset, _currentColor);
            }
        }
    }

    public class ExplosionEffect : VisualEffect
    {
        public ExplosionEffect()
        {
            Type = EffectType.Explosion;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            // Draw expanding sphere
            Raylib.DrawSphere(Position, _currentSize, _currentColor);
            
            // Draw outer ring
            Color ringColor = _currentColor;
            ringColor.A = (byte)(ringColor.A / 2);
            Raylib.DrawSphereWires(Position, _currentSize * 1.2f, 12, 12, ringColor);
        }
    }

    public class ImpactEffect : VisualEffect
    {
        public ImpactEffect()
        {
            Type = EffectType.Impact;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            // Draw impact flash
            Raylib.DrawSphere(Position, _currentSize * 0.5f, _currentColor);
            
            // Draw impact lines
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * MathF.PI * 2f;
                Vector3 lineEnd = Position + new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * _currentSize;
                Raylib.DrawLine3D(Position, lineEnd, _currentColor);
            }
        }
    }

    public class BeamEffect : VisualEffect
    {
        public BeamEffect()
        {
            Type = EffectType.Beam;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            Vector3 endPoint = Position + Direction * _config.Size;
            
            // Draw main beam
            Raylib.DrawLine3D(Position, endPoint, _currentColor);
            
            // Draw beam segments for thickness
            int segments = (int)(_config.Size / 2f);
            for (int i = 0; i < segments; i++)
            {
                Vector3 segmentPos = Vector3.Lerp(Position, endPoint, (float)i / segments);
                Raylib.DrawSphere(segmentPos, 0.1f * _currentIntensity, _currentColor);
            }
        }
    }

    public class ShieldEffect : VisualEffect
    {
        public ShieldEffect()
        {
            Type = EffectType.Shield;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            // Draw shield sphere
            Color shieldColor = _currentColor;
            shieldColor.A = (byte)(100 * _currentIntensity);
            Raylib.DrawSphere(Position, _currentSize, shieldColor);
            
            // Draw shield wireframe
            Raylib.DrawSphereWires(Position, _currentSize, 12, 12, _currentColor);
        }
    }

    public class GenericEffect : VisualEffect
    {
        public GenericEffect(EffectType type)
        {
            Type = type;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            Raylib.DrawSphere(Position, _currentSize, _currentColor);
        }
    }

    // Particle systems (simplified implementations)
    public abstract class ParticleSystem
    {
        public bool IsActive { get; protected set; }
        protected float _duration;
        protected float _age;

        public virtual void Update(float deltaTime)
        {
            _age += deltaTime;
            if (_age >= _duration)
            {
                IsActive = false;
            }
        }

        public abstract void Draw(Camera3D camera);
    }

    public class ExplosionParticleSystem : ParticleSystem
    {
        private Vector3 _position;
        private List<Particle> _particles;

        public ExplosionParticleSystem(Vector3 position, float radius, WeaponType weaponType)
        {
            _position = position;
            _duration = 2f;
            IsActive = true;
            _particles = new List<Particle>();
            
            // Create particles
            Random random = new Random();
            int particleCount = (int)(radius * 2);
            
            for (int i = 0; i < particleCount; i++)
            {
                Vector3 velocity = new Vector3(
                    (float)(random.NextDouble() - 0.5) * 10f,
                    (float)(random.NextDouble() - 0.5) * 10f,
                    (float)(random.NextDouble() - 0.5) * 10f
                );
                
                _particles.Add(new Particle(_position, velocity, Color.Orange, 1.5f));
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            foreach (var particle in _particles)
            {
                particle.Update(deltaTime);
            }
        }

        public override void Draw(Camera3D camera)
        {
            foreach (var particle in _particles)
            {
                if (particle.IsActive)
                {
                    particle.Draw(camera);
                }
            }
        }
    }

    public class SparkParticleSystem : ParticleSystem
    {
        private Vector3 _position;
        private Vector3 _normal;
        private List<Particle> _particles;

        public SparkParticleSystem(Vector3 position, Vector3 normal, float intensity)
        {
            _position = position;
            _normal = normal;
            _duration = 1f;
            IsActive = true;
            _particles = new List<Particle>();
            
            Random random = new Random();
            int particleCount = (int)(intensity / 10f);
            
            for (int i = 0; i < particleCount; i++)
            {
                Vector3 velocity = normal * 5f + new Vector3(
                    (float)(random.NextDouble() - 0.5) * 3f,
                    (float)(random.NextDouble() - 0.5) * 3f,
                    (float)(random.NextDouble() - 0.5) * 3f
                );
                
                _particles.Add(new Particle(_position, velocity, Color.Yellow, 0.5f));
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            foreach (var particle in _particles)
            {
                particle.Update(deltaTime);
            }
        }

        public override void Draw(Camera3D camera)
        {
            foreach (var particle in _particles)
            {
                if (particle.IsActive)
                {
                    particle.Draw(camera);
                }
            }
        }
    }

    public class RippleParticleSystem : ParticleSystem
    {
        private Vector3 _position;
        private float _maxRadius;

        public RippleParticleSystem(Vector3 position, float maxRadius, float duration)
        {
            _position = position;
            _maxRadius = maxRadius;
            _duration = duration;
            IsActive = true;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            float progress = _age / _duration;
            float currentRadius = _maxRadius * progress;
            float alpha = 1f - progress;
            
            Color rippleColor = new Color(150, 0, 255, (byte)(100 * alpha));
            Raylib.DrawSphereWires(_position, currentRadius, 16, 16, rippleColor);
        }
    }

    public class MagneticFieldParticleSystem : ParticleSystem
    {
        private Vector3 _position;
        private float _radius;

        public MagneticFieldParticleSystem(Vector3 position, float radius, float duration)
        {
            _position = position;
            _radius = radius;
            _duration = duration;
            IsActive = true;
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            float time = _age;
            Color fieldColor = new Color(100, 150, 255, 80);
            
            // Draw rotating field lines
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * MathF.PI * 2f + time;
                Vector3 point1 = _position + new Vector3(MathF.Cos(angle) * _radius, 0, MathF.Sin(angle) * _radius);
                Vector3 point2 = _position + new Vector3(MathF.Cos(angle + MathF.PI) * _radius, 0, MathF.Sin(angle + MathF.PI) * _radius);
                
                Raylib.DrawLine3D(point1, point2, fieldColor);
            }
        }
    }

    public class LightningParticleSystem : ParticleSystem
    {
        private Vector3 _position;
        private List<Vector3> _lightningPoints;
        private Random _random;

        public LightningParticleSystem(Vector3 position, float duration)
        {
            _position = position;
            _duration = duration;
            IsActive = true;
            _random = new Random();
            _lightningPoints = new List<Vector3>();
            
            GenerateLightningPoints();
        }

        private void GenerateLightningPoints()
        {
            _lightningPoints.Clear();
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * MathF.PI * 2f;
                Vector3 endPoint = _position + new Vector3(
                    MathF.Cos(angle) * 4f,
                    (float)(_random.NextDouble() - 0.5) * 2f,
                    MathF.Sin(angle) * 4f
                );
                _lightningPoints.Add(endPoint);
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            // Regenerate lightning points occasionally for flickering effect
            if (_random.NextSingle() < 0.3f)
            {
                GenerateLightningPoints();
            }
        }

        public override void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            Color lightningColor = Color.White;
            
            foreach (var point in _lightningPoints)
            {
                Raylib.DrawLine3D(_position, point, lightningColor);
            }
        }
    }

    public class Particle
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Color Color { get; private set; }
        public bool IsActive { get; private set; }
        
        private float _lifespan;
        private float _maxLifespan;

        public Particle(Vector3 position, Vector3 velocity, Color color, float lifespan)
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
            Velocity *= 0.98f; // Damping
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
            
            Raylib.DrawSphere(Position, 0.1f, Color);
        }
    }

    public class LightSource
    {
        public Vector3 Position { get; private set; }
        public Color Color { get; private set; }
        public float Radius { get; private set; }
        public bool IsActive { get; private set; }
        
        private float _duration;
        private float _maxDuration;

        public LightSource(Vector3 position, Color color, float radius, float duration)
        {
            Position = position;
            Color = color;
            Radius = radius;
            _duration = duration;
            _maxDuration = duration;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            _duration -= deltaTime;
            if (_duration <= 0)
            {
                IsActive = false;
            }
            else
            {
                // Fade over time
                float intensity = _duration / _maxDuration;
                Color.A = (byte)(255 * intensity);
            }
        }
    }
}