using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Advanced particle effect system for weapon visuals
    /// </summary>
    public class AdvancedParticleEffect
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 Acceleration { get; private set; }
        public Color Color { get; private set; }
        public bool IsActive { get; private set; }
        public ParticleEffectType EffectType { get; private set; }
        
        private float _lifespan;
        private float _maxLifespan;
        private float _size;
        private float _maxSize;
        private float _rotationSpeed;
        private float _rotation;
        private Random _random;
        
        // Effect-specific properties
        private float _sparkFrequency;
        private float _pulseSpeed;
        private float _distortionStrength;
        private Vector3 _originalPosition;
        private float _orbitRadius;
        private float _orbitSpeed;
        private float _orbitAngle;
        
        public AdvancedParticleEffect(Vector3 position, Vector3 velocity, Color color, 
                                    float lifespan, ParticleEffectType effectType)
        {
            Position = position;
            _originalPosition = position;
            Velocity = velocity;
            Acceleration = Vector3.Zero;
            Color = color;
            _lifespan = lifespan;
            _maxLifespan = lifespan;
            EffectType = effectType;
            IsActive = true;
            _random = new Random();
            
            InitializeEffectProperties();
        }
        
        private void InitializeEffectProperties()
        {
            switch (EffectType)
            {
                case ParticleEffectType.ElectricSparks:
                    _size = 0.1f;
                    _maxSize = 0.3f;
                    _sparkFrequency = 10f;
                    _rotationSpeed = 720f; // 2 rotations per second
                    Acceleration = new Vector3(0, -5f, 0); // Gravity effect
                    break;
                    
                case ParticleEffectType.QuantumDistortion:
                    _size = 0.2f;
                    _maxSize = 0.8f;
                    _distortionStrength = 2f;
                    _pulseSpeed = 4f;
                    _orbitRadius = (float)(_random.NextDouble() * 1f + 0.5f);
                    _orbitSpeed = (float)(_random.NextDouble() * 4f + 2f);
                    _orbitAngle = (float)(_random.NextDouble() * Math.PI * 2);
                    break;
                    
                case ParticleEffectType.NaniteCloud:
                    _size = 0.05f;
                    _maxSize = 0.1f;
                    _rotationSpeed = 180f;
                    // Nanites move in formation
                    break;
                    
                case ParticleEffectType.IceCrystals:
                    _size = 0.1f;
                    _maxSize = 0.4f;
                    _rotationSpeed = 90f;
                    Acceleration = new Vector3(0, -2f, 0);
                    break;
                    
                case ParticleEffectType.Explosion:
                    _size = 0.3f;
                    _maxSize = 1.2f;
                    _rotationSpeed = 360f;
                    break;
                    
                case ParticleEffectType.HealingSparkles:
                    _size = 0.08f;
                    _maxSize = 0.2f;
                    _pulseSpeed = 6f;
                    Acceleration = new Vector3(0, 3f, 0); // Float upward
                    break;
                    
                case ParticleEffectType.PlasmaTrail:
                    _size = 0.15f;
                    _maxSize = 0.5f;
                    _rotationSpeed = 480f;
                    break;
                    
                case ParticleEffectType.GravityWaves:
                    _size = 0.5f;
                    _maxSize = 2f;
                    _pulseSpeed = 2f;
                    break;
                    
                case ParticleEffectType.DimensionalRift:
                    _size = 0.2f;
                    _maxSize = 1f;
                    _distortionStrength = 3f;
                    _pulseSpeed = 8f;
                    break;
                    
                case ParticleEffectType.EnergyRipples:
                    _size = 0.1f;
                    _maxSize = 0.6f;
                    _pulseSpeed = 5f;
                    break;
                    
                case ParticleEffectType.FireBurst:
                    _size = 0.2f;
                    _maxSize = 0.8f;
                    _rotationSpeed = 300f;
                    Acceleration = new Vector3(0, 8f, 0); // Rise like flame
                    break;
                    
                case ParticleEffectType.SmokeCloud:
                    _size = 0.3f;
                    _maxSize = 1.5f;
                    _rotationSpeed = 45f;
                    Acceleration = new Vector3(0, 1f, 0); // Slowly rise
                    break;
                    
                case ParticleEffectType.MetallicShards:
                    _size = 0.08f;
                    _maxSize = 0.2f;
                    _rotationSpeed = 900f;
                    Acceleration = new Vector3(0, -10f, 0); // Heavy gravity
                    break;
                    
                case ParticleEffectType.PhotonBeam:
                    _size = 0.05f;
                    _maxSize = 0.15f;
                    // No gravity for light particles
                    break;
                    
                default:
                    _size = 0.1f;
                    _maxSize = 0.3f;
                    _rotationSpeed = 180f;
                    break;
            }
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            _lifespan -= deltaTime;
            if (_lifespan <= 0)
            {
                IsActive = false;
                return;
            }
            
            UpdateMovement(deltaTime);
            UpdateVisualProperties(deltaTime);
            UpdateSpecialEffects(deltaTime);
        }
        
        private void UpdateMovement(float deltaTime)
        {
            switch (EffectType)
            {
                case ParticleEffectType.QuantumDistortion:
                    UpdateQuantumMovement(deltaTime);
                    break;
                    
                case ParticleEffectType.GravityWaves:
                    UpdateGravityWaveMovement(deltaTime);
                    break;
                    
                case ParticleEffectType.DimensionalRift:
                    UpdateDimensionalMovement(deltaTime);
                    break;
                    
                default:
                    UpdateStandardMovement(deltaTime);
                    break;
            }
            
            // Update rotation
            _rotation += _rotationSpeed * deltaTime;
        }
        
        private void UpdateStandardMovement(float deltaTime)
        {
            Velocity += Acceleration * deltaTime;
            Position += Velocity * deltaTime;
            
            // Apply drag
            Velocity *= 0.98f;
        }
        
        private void UpdateQuantumMovement(float deltaTime)
        {
            _orbitAngle += _orbitSpeed * deltaTime;
            
            // Quantum particles orbit around original position with distortion
            Vector3 baseOrbitPosition = _originalPosition + new Vector3(
                MathF.Cos(_orbitAngle) * _orbitRadius,
                MathF.Sin(_orbitAngle * 0.7f) * _orbitRadius * 0.5f,
                MathF.Sin(_orbitAngle) * _orbitRadius
            );
            
            // Add quantum uncertainty
            Vector3 uncertainty = new Vector3(
                MathF.Sin(_orbitAngle * 3f) * _distortionStrength,
                MathF.Cos(_orbitAngle * 2.3f) * _distortionStrength,
                MathF.Sin(_orbitAngle * 1.7f) * _distortionStrength
            ) * (float)_random.NextDouble();
            
            Position = baseOrbitPosition + uncertainty + Velocity * deltaTime;
        }
        
        private void UpdateGravityWaveMovement(float deltaTime)
        {
            // Gravity waves expand outward in rings
            float age = _maxLifespan - _lifespan;
            float waveRadius = age * 10f; // Expand at 10 units per second
            
            Vector3 direction = Vector3.Normalize(Position - _originalPosition);
            Position = _originalPosition + direction * waveRadius;
        }
        
        private void UpdateDimensionalMovement(float deltaTime)
        {
            // Dimensional particles phase in and out of reality
            float phaseTime = (_maxLifespan - _lifespan) * _pulseSpeed;
            Vector3 dimensionalOffset = new Vector3(
                MathF.Sin(phaseTime) * _distortionStrength,
                MathF.Cos(phaseTime * 1.3f) * _distortionStrength,
                MathF.Sin(phaseTime * 0.7f) * _distortionStrength
            );
            
            Position += (Velocity + dimensionalOffset) * deltaTime;
        }
        
        private void UpdateVisualProperties(float deltaTime)
        {
            float ageRatio = _lifespan / _maxLifespan;
            
            // Update size based on effect type and age
            switch (EffectType)
            {
                case ParticleEffectType.Explosion:
                    // Explosion particles grow then shrink
                    if (ageRatio > 0.7f)
                        _size = _maxSize * ((1f - ageRatio) / 0.3f);
                    else
                        _size = _maxSize * (0.3f - ageRatio * 0.3f) / 0.7f + _maxSize * 0.7f;
                    break;
                    
                case ParticleEffectType.SmokeCloud:
                    // Smoke expands over time
                    _size = _maxSize * (1f - ageRatio * 0.5f);
                    break;
                    
                case ParticleEffectType.HealingSparkles:
                    // Healing sparkles pulse
                    float pulse = MathF.Sin((_maxLifespan - _lifespan) * _pulseSpeed) * 0.3f + 0.7f;
                    _size = _maxSize * ageRatio * pulse;
                    break;
                    
                default:
                    // Standard size decay
                    _size = _maxSize * ageRatio;
                    break;
            }
            
            // Update color alpha based on age
            Color originalColor = Color;
            originalColor.A = (byte)(255 * ageRatio);
            Color = originalColor;
        }
        
        private void UpdateSpecialEffects(float deltaTime)
        {
            float currentTime = (_maxLifespan - _lifespan);
            
            switch (EffectType)
            {
                case ParticleEffectType.ElectricSparks:
                    UpdateElectricEffects(deltaTime, currentTime);
                    break;
                    
                case ParticleEffectType.QuantumDistortion:
                    UpdateQuantumEffects(deltaTime, currentTime);
                    break;
                    
                case ParticleEffectType.IceCrystals:
                    UpdateIceEffects(deltaTime, currentTime);
                    break;
                    
                case ParticleEffectType.FireBurst:
                    UpdateFireEffects(deltaTime, currentTime);
                    break;
            }
        }
        
        private void UpdateElectricEffects(float deltaTime, float currentTime)
        {
            // Electric sparks change color rapidly
            if (currentTime % (1f / _sparkFrequency) < deltaTime)
            {
                Color = _random.NextSingle() < 0.5f ? Color.White : Color.Cyan;
            }
        }
        
        private void UpdateQuantumEffects(float deltaTime, float currentTime)
        {
            // Quantum particles shift between states
            float quantumPhase = currentTime * _pulseSpeed;
            Color baseColor = Color;
            
            // Color shifts between primary and quantum colors
            float colorShift = (MathF.Sin(quantumPhase) + 1f) / 2f;
            Color = ColorLerp(baseColor, Color.Purple, colorShift);
        }
        
        private void UpdateIceEffects(float deltaTime, float currentTime)
        {
            // Ice crystals reflect light
            float reflectionIntensity = MathF.Abs(MathF.Sin(_rotation * MathF.PI / 180f));
            Color iceColor = Color;
            iceColor = ColorLerp(iceColor, Color.White, reflectionIntensity * 0.5f);
            Color = iceColor;
        }
        
        private void UpdateFireEffects(float deltaTime, float currentTime)
        {
            // Fire particles change from yellow to red over time
            float fireAge = 1f - (_lifespan / _maxLifespan);
            Color = ColorLerp(Color.Yellow, Color.Red, fireAge);
        }
        
        public void Draw(Camera3D camera)
        {
            if (!IsActive || _size <= 0f) return;
            
            switch (EffectType)
            {
                case ParticleEffectType.ElectricSparks:
                    DrawElectricSpark(camera);
                    break;
                    
                case ParticleEffectType.QuantumDistortion:
                    DrawQuantumDistortion(camera);
                    break;
                    
                case ParticleEffectType.NaniteCloud:
                    DrawNaniteParticle(camera);
                    break;
                    
                case ParticleEffectType.IceCrystals:
                    DrawIceCrystal(camera);
                    break;
                    
                case ParticleEffectType.Explosion:
                    DrawExplosionParticle(camera);
                    break;
                    
                case ParticleEffectType.HealingSparkles:
                    DrawHealingSparkle(camera);
                    break;
                    
                case ParticleEffectType.PlasmaTrail:
                    DrawPlasmaParticle(camera);
                    break;
                    
                case ParticleEffectType.GravityWaves:
                    DrawGravityWave(camera);
                    break;
                    
                case ParticleEffectType.DimensionalRift:
                    DrawDimensionalRift(camera);
                    break;
                    
                case ParticleEffectType.EnergyRipples:
                    DrawEnergyRipple(camera);
                    break;
                    
                case ParticleEffectType.FireBurst:
                    DrawFireParticle(camera);
                    break;
                    
                case ParticleEffectType.SmokeCloud:
                    DrawSmokeParticle(camera);
                    break;
                    
                case ParticleEffectType.MetallicShards:
                    DrawMetallicShard(camera);
                    break;
                    
                case ParticleEffectType.PhotonBeam:
                    DrawPhotonParticle(camera);
                    break;
                    
                default:
                    DrawDefaultParticle(camera);
                    break;
            }
        }
        
        private void DrawElectricSpark(Camera3D camera)
        {
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw electric arcs
            for (int i = 0; i < 3; i++)
            {
                float angle = _rotation + i * 120f;
                Vector3 arcEnd = Position + new Vector3(
                    MathF.Cos(angle * MathF.PI / 180f) * _size * 3f,
                    MathF.Sin(angle * MathF.PI / 180f) * _size * 3f,
                    0f
                );
                Raylib.DrawLine3D(Position, arcEnd, Color.White);
            }
        }
        
        private void DrawQuantumDistortion(Camera3D camera)
        {
            // Draw multiple overlapping spheres with different phases
            for (int i = 0; i < 3; i++)
            {
                float phaseOffset = i * MathF.PI * 2f / 3f;
                Vector3 offset = new Vector3(
                    MathF.Sin(_rotation * MathF.PI / 180f + phaseOffset) * _size * 0.5f,
                    MathF.Cos(_rotation * MathF.PI / 180f + phaseOffset) * _size * 0.5f,
                    0f
                );
                
                Color phaseColor = Color;
                phaseColor.A = (byte)(phaseColor.A / (i + 1));
                
                Raylib.DrawSphere(Position + offset, _size * (1f - i * 0.2f), phaseColor);
            }
            
            // Draw connecting lines
            Raylib.DrawSphereWires(Position, _size * 1.5f, Color.Purple);
        }
        
        private void DrawNaniteParticle(Camera3D camera)
        {
            // Draw small cubic nanite
            Raylib.DrawCube(Position, _size, _size, _size, Color);
            Raylib.DrawCubeWires(Position, _size * 1.2f, _size * 1.2f, _size * 1.2f, Color.Green);
        }
        
        private void DrawIceCrystal(Camera3D camera)
        {
            // Draw crystalline structure
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw crystal facets
            for (int i = 0; i < 6; i++)
            {
                float angle = _rotation + i * 60f;
                Vector3 facetPos = Position + new Vector3(
                    MathF.Cos(angle * MathF.PI / 180f) * _size,
                    0f,
                    MathF.Sin(angle * MathF.PI / 180f) * _size
                );
                Raylib.DrawLine3D(Position, facetPos, Color.White);
            }
        }
        
        private void DrawExplosionParticle(Camera3D camera)
        {
            // Draw expanding explosion particle
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw shock wave
            if (_size > _maxSize * 0.5f)
            {
                Raylib.DrawSphereWires(Position, _size * 1.5f, Color.White);
            }
        }
        
        private void DrawHealingSparkle(Camera3D camera)
        {
            // Draw glowing healing particle
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw cross pattern
            float crossSize = _size * 2f;
            Raylib.DrawLine3D(
                Position + new Vector3(-crossSize, 0, 0),
                Position + new Vector3(crossSize, 0, 0),
                Color
            );
            Raylib.DrawLine3D(
                Position + new Vector3(0, -crossSize, 0),
                Position + new Vector3(0, crossSize, 0),
                Color
            );
        }
        
        private void DrawPlasmaParticle(Camera3D camera)
        {
            // Draw glowing plasma ball
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw plasma field
            Color fieldColor = Color;
            fieldColor.A = (byte)(fieldColor.A / 3);
            Raylib.DrawSphere(Position, _size * 2f, fieldColor);
        }
        
        private void DrawGravityWave(Camera3D camera)
        {
            // Draw expanding ring
            Raylib.DrawSphereWires(Position, _size, Color);
        }
        
        private void DrawDimensionalRift(Camera3D camera)
        {
            // Draw distorted reality effect
            Raylib.DrawSphere(Position, _size, Color);
            
            // Draw dimensional tears
            for (int i = 0; i < 4; i++)
            {
                float angle = _rotation + i * 90f;
                Vector3 tearStart = Position + new Vector3(
                    MathF.Cos(angle * MathF.PI / 180f) * _size,
                    0f,
                    MathF.Sin(angle * MathF.PI / 180f) * _size
                );
                Vector3 tearEnd = tearStart + new Vector3(
                    MathF.Sin(angle * MathF.PI / 180f) * _size * 2f,
                    (float)(_random.NextDouble() - 0.5) * _size,
                    MathF.Cos(angle * MathF.PI / 180f) * _size * 2f
                );
                Raylib.DrawLine3D(tearStart, tearEnd, Color.Purple);
            }
        }
        
        private void DrawEnergyRipple(Camera3D camera)
        {
            // Draw energy wave
            Raylib.DrawSphere(Position, _size, Color);
            Raylib.DrawSphereWires(Position, _size * 1.5f, Color);
        }
        
        private void DrawFireParticle(Camera3D camera)
        {
            // Draw flickering flame
            float flicker = 1f + MathF.Sin(_rotation * MathF.PI / 45f) * 0.2f;
            Raylib.DrawSphere(Position, _size * flicker, Color);
        }
        
        private void DrawSmokeParticle(Camera3D camera)
        {
            // Draw wispy smoke
            Color smokeColor = Color;
            smokeColor.A = (byte)(smokeColor.A / 2); // Semi-transparent
            Raylib.DrawSphere(Position, _size, smokeColor);
        }
        
        private void DrawMetallicShard(Camera3D camera)
        {
            // Draw spinning metal fragment
            Raylib.DrawCube(Position, _size, _size * 0.5f, _size * 2f, Color);
        }
        
        private void DrawPhotonParticle(Camera3D camera)
        {
            // Draw bright light particle
            Raylib.DrawSphere(Position, _size, Color.White);
            
            // Draw light rays
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Vector3 rayEnd = Position + new Vector3(
                    MathF.Cos(angle * MathF.PI / 180f) * _size * 4f,
                    0f,
                    MathF.Sin(angle * MathF.PI / 180f) * _size * 4f
                );
                Raylib.DrawLine3D(Position, rayEnd, Color);
            }
        }
        
        private void DrawDefaultParticle(Camera3D camera)
        {
            Raylib.DrawSphere(Position, _size, Color);
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
    }
}