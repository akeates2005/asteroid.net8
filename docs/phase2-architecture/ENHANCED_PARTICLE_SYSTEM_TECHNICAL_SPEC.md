# Enhanced Particle System - Technical Specification

## Overview

The Enhanced Particle System provides a high-performance, flexible particle rendering and simulation framework for the Asteroids game. It supports multiple particle types, GPU acceleration, Level-of-Detail (LOD) optimization, and advanced visual effects while maintaining consistent performance across different hardware configurations.

## System Architecture

```
ParticleSystemManager
├── Core Components
│   ├── ParticlePool<T> (Memory management)
│   ├── ParticleRenderer (Rendering pipeline)
│   ├── ParticlePhysics (Simulation engine)
│   └── ParticleDataManager (Data organization)
├── Effect Systems
│   ├── ExplosionSystem (Volumetric explosions)
│   ├── EngineTrailSystem (Ship thrust effects)
│   ├── WeaponEffectSystem (Weapon trails and impacts)
│   ├── EnvironmentalSystem (Ambient particles)
│   └── ImpactEffectSystem (Collision effects)
├── Optimization
│   ├── ParticleLODSystem (Distance-based optimization)
│   ├── ParticleCulling (Frustum and occlusion culling)
│   ├── BatchRenderer (Instanced rendering)
│   └── GPUSimulation (Compute shader acceleration)
└── Management
    ├── EffectManager (Effect lifecycle)
    ├── EmissionControl (Particle emission)
    └── PerformanceMonitor (System metrics)
```

## Core Interfaces

### 1. Particle Interface

```csharp
public interface IParticle : IPoolable
{
    /// <summary>
    /// Current position in world space
    /// </summary>
    Vector2 Position { get; set; }
    
    /// <summary>
    /// Current velocity vector
    /// </summary>
    Vector2 Velocity { get; set; }
    
    /// <summary>
    /// Acceleration vector
    /// </summary>
    Vector2 Acceleration { get; set; }
    
    /// <summary>
    /// Current age of particle in seconds
    /// </summary>
    float Age { get; set; }
    
    /// <summary>
    /// Total lifetime of particle in seconds
    /// </summary>
    float Lifetime { get; set; }
    
    /// <summary>
    /// Normalized age (0.0 to 1.0)
    /// </summary>
    float NormalizedAge { get; }
    
    /// <summary>
    /// Current scale multiplier
    /// </summary>
    float Scale { get; set; }
    
    /// <summary>
    /// Current rotation in radians
    /// </summary>
    float Rotation { get; set; }
    
    /// <summary>
    /// Angular velocity in radians per second
    /// </summary>
    float AngularVelocity { get; set; }
    
    /// <summary>
    /// Current color with alpha
    /// </summary>
    Color Color { get; set; }
    
    /// <summary>
    /// Particle blend mode
    /// </summary>
    ParticleBlendMode BlendMode { get; }
    
    /// <summary>
    /// Particle layer for sorting
    /// </summary>
    ParticleLayer Layer { get; }
    
    /// <summary>
    /// Custom user data
    /// </summary>
    object UserData { get; set; }
    
    /// <summary>
    /// Update particle state
    /// </summary>
    void Update(float deltaTime, ParticleUpdateContext context);
    
    /// <summary>
    /// Get rendering data for batching
    /// </summary>
    ParticleRenderData GetRenderData();
    
    /// <summary>
    /// Check if particle should be removed
    /// </summary>
    bool ShouldDestroy { get; }
}

public enum ParticleBlendMode
{
    Alpha,          // Standard alpha blending
    Additive,       // Additive blending for glowing effects
    Multiply,       // Multiplicative blending for shadows
    Screen,         // Screen blending for highlights
    Overlay         // Overlay blending for complex effects
}

public enum ParticleLayer
{
    Background = 0,     // Behind game objects
    GameWorld = 1,      // Mixed with game objects
    Effects = 2,        // Above game objects
    UI = 3             // UI level effects
}
```

### 2. Particle System Interface

```csharp
public interface IParticleSystem : IDisposable
{
    /// <summary>
    /// System name identifier
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Whether system is currently active
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Maximum number of particles this system can handle
    /// </summary>
    int MaxParticles { get; }
    
    /// <summary>
    /// Current number of active particles
    /// </summary>
    int ActiveParticleCount { get; }
    
    /// <summary>
    /// Emission rate (particles per second)
    /// </summary>
    float EmissionRate { get; set; }
    
    /// <summary>
    /// System world position
    /// </summary>
    Vector2 Position { get; set; }
    
    /// <summary>
    /// Emit particles
    /// </summary>
    void Emit(int count, EmissionParameters parameters);
    
    /// <summary>
    /// Update all particles in system
    /// </summary>
    void Update(float deltaTime);
    
    /// <summary>
    /// Render all particles
    /// </summary>
    void Render(IParticleRenderer renderer);
    
    /// <summary>
    /// Clear all particles
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Get system performance statistics
    /// </summary>
    ParticleSystemStats GetStatistics();
}
```

### 3. Particle Renderer Interface

```csharp
public interface IParticleRenderer
{
    /// <summary>
    /// Begin particle rendering batch
    /// </summary>
    void BeginBatch();
    
    /// <summary>
    /// Add particle to current batch
    /// </summary>
    void DrawParticle(ParticleRenderData particle);
    
    /// <summary>
    /// Draw batch of particles with same properties
    /// </summary>
    void DrawBatch(ParticleRenderData[] particles, ParticleMaterial material);
    
    /// <summary>
    /// End and flush current batch
    /// </summary>
    void EndBatch();
    
    /// <summary>
    /// Set current camera matrix
    /// </summary>
    void SetViewMatrix(Matrix4x4 viewMatrix);
    
    /// <summary>
    /// Set current projection matrix
    /// </summary>
    void SetProjectionMatrix(Matrix4x4 projectionMatrix);
    
    /// <summary>
    /// Get rendering capabilities
    /// </summary>
    ParticleRenderingCapabilities GetCapabilities();
}
```

## Advanced Particle Types

### 1. Base Particle Implementation

```csharp
public class AdvancedParticle : IParticle
{
    // IPoolable implementation
    public uint PoolId { get; set; }
    public PoolableState State { get; set; }
    public bool IsActive { get; set; }
    
    // Core properties
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 Acceleration { get; set; }
    public float Age { get; set; }
    public float Lifetime { get; set; }
    public float Scale { get; set; }
    public float Rotation { get; set; }
    public float AngularVelocity { get; set; }
    public Color Color { get; set; }
    public ParticleBlendMode BlendMode { get; protected set; }
    public ParticleLayer Layer { get; protected set; }
    public object UserData { get; set; }
    
    // Derived properties
    public float NormalizedAge => Lifetime > 0 ? Math.Clamp(Age / Lifetime, 0f, 1f) : 1f;
    public bool ShouldDestroy => Age >= Lifetime || !IsActive;
    
    // Physics properties
    public float Drag { get; set; } = 0.98f;
    public float Mass { get; set; } = 1.0f;
    public Vector2 Force { get; set; }
    
    // Visual properties
    public float InitialScale { get; set; }
    public Color InitialColor { get; set; }
    public AnimationCurve ScaleCurve { get; set; }
    public ColorGradient ColorGradient { get; set; }
    public TextureRegion TextureRegion { get; set; }
    
    public virtual void Update(float deltaTime, ParticleUpdateContext context)
    {
        if (!IsActive || ShouldDestroy) return;
        
        Age += deltaTime;
        
        // Physics update
        UpdatePhysics(deltaTime, context);
        
        // Visual update
        UpdateVisuals(deltaTime, context);
    }
    
    protected virtual void UpdatePhysics(float deltaTime, ParticleUpdateContext context)
    {
        // Apply forces
        var totalAcceleration = Acceleration + (Force / Mass);
        
        // Apply environmental forces
        if (context.HasWind)
        {
            totalAcceleration += context.WindForce * context.WindStrength;
        }
        
        if (context.HasGravity)
        {
            totalAcceleration += context.GravityForce;
        }
        
        // Update velocity
        Velocity += totalAcceleration * deltaTime;
        
        // Apply drag
        Velocity *= MathF.Pow(Drag, deltaTime);
        
        // Update position
        Position += Velocity * deltaTime;
        
        // Update rotation
        Rotation += AngularVelocity * deltaTime;
        
        // Reset force accumulator
        Force = Vector2.Zero;
    }
    
    protected virtual void UpdateVisuals(float deltaTime, ParticleUpdateContext context)
    {
        var t = NormalizedAge;
        
        // Update scale using curve
        if (ScaleCurve != null)
        {
            Scale = InitialScale * ScaleCurve.Evaluate(t);
        }
        
        // Update color using gradient
        if (ColorGradient != null)
        {
            Color = ColorGradient.Evaluate(t);
        }
        else
        {
            // Simple alpha fade
            var alpha = (byte)(255 * (1.0f - t));
            Color = new Color(InitialColor.R, InitialColor.G, InitialColor.B, alpha);
        }
    }
    
    public virtual ParticleRenderData GetRenderData()
    {
        return new ParticleRenderData
        {
            Position = Position,
            Scale = Scale,
            Rotation = Rotation,
            Color = Color,
            TextureRegion = TextureRegion,
            Layer = (int)Layer,
            BlendMode = BlendMode
        };
    }
    
    public virtual void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Acceleration = Vector2.Zero;
        Age = 0f;
        Lifetime = 1f;
        Scale = 1f;
        Rotation = 0f;
        AngularVelocity = 0f;
        Color = Color.White;
        Force = Vector2.Zero;
        UserData = null;
        IsActive = false;
        State = PoolableState.InPool;
    }
    
    public virtual void Initialize()
    {
        IsActive = true;
        State = PoolableState.InUse;
    }
    
    public virtual void Dispose()
    {
        State = PoolableState.Disposed;
    }
    
    public virtual bool IsValid()
    {
        return !float.IsNaN(Position.X) && !float.IsNaN(Position.Y) &&
               !float.IsNaN(Velocity.X) && !float.IsNaN(Velocity.Y) &&
               Lifetime > 0;
    }
}
```

### 2. Specialized Particle Types

#### Explosion Particle

```csharp
public class ExplosionParticle : AdvancedParticle
{
    public float ExplosionForce { get; set; }
    public Vector2 ExplosionCenter { get; set; }
    public float ShockwaveRadius { get; set; }
    public float ShockwaveSpeed { get; set; }
    
    public ExplosionParticle()
    {
        BlendMode = ParticleBlendMode.Additive;
        Layer = ParticleLayer.Effects;
        Drag = 0.95f;
    }
    
    protected override void UpdatePhysics(float deltaTime, ParticleUpdateContext context)
    {
        // Apply explosion force that diminishes over time
        var directionFromExplosion = Vector2.Normalize(Position - ExplosionCenter);
        var distanceFromExplosion = Vector2.Distance(Position, ExplosionCenter);
        
        // Shockwave effect
        var shockwaveDistance = ShockwaveSpeed * Age;
        if (distanceFromExplosion < shockwaveDistance + ShockwaveRadius && 
            distanceFromExplosion > shockwaveDistance - ShockwaveRadius)
        {
            var shockwaveForce = ExplosionForce * (1.0f - NormalizedAge) * 
                (1.0f - Math.Abs(distanceFromExplosion - shockwaveDistance) / ShockwaveRadius);
            Force += directionFromExplosion * shockwaveForce;
        }
        
        base.UpdatePhysics(deltaTime, context);
    }
    
    protected override void UpdateVisuals(float deltaTime, ParticleUpdateContext context)
    {
        base.UpdateVisuals(deltaTime, context);
        
        // Add heat distortion effect for close particles
        var distanceFromExplosion = Vector2.Distance(Position, ExplosionCenter);
        if (distanceFromExplosion < 50f && NormalizedAge < 0.3f)
        {
            // Modify color to appear hotter
            var heatIntensity = 1.0f - (distanceFromExplosion / 50f);
            Color = Color.Lerp(Color, Color.Orange, heatIntensity * 0.5f);
        }
    }
}
```

#### Engine Trail Particle

```csharp
public class EngineTrailParticle : AdvancedParticle
{
    public Vector2 EnginePosition { get; set; }
    public Vector2 EngineVelocity { get; set; }
    public float TurbulenceStrength { get; set; }
    public float HeatIntensity { get; set; }
    
    private float _turbulenceOffset;
    private Random _random = new Random();
    
    public EngineTrailParticle()
    {
        BlendMode = ParticleBlendMode.Additive;
        Layer = ParticleLayer.Effects;
        Drag = 0.92f;
        TurbulenceStrength = 1.0f;
        _turbulenceOffset = (float)_random.NextDouble() * MathF.PI * 2;
    }
    
    protected override void UpdatePhysics(float deltaTime, ParticleUpdateContext context)
    {
        // Apply turbulence for realistic engine exhaust
        var turbulenceX = MathF.Sin(Age * 10f + _turbulenceOffset) * TurbulenceStrength;
        var turbulenceY = MathF.Cos(Age * 12f + _turbulenceOffset * 1.3f) * TurbulenceStrength;
        var turbulence = new Vector2(turbulenceX, turbulenceY) * (1.0f - NormalizedAge);
        
        Force += turbulence;
        
        base.UpdatePhysics(deltaTime, context);
    }
    
    protected override void UpdateVisuals(float deltaTime, ParticleUpdateContext context)
    {
        // Engine trail color progression: White -> Yellow -> Orange -> Red -> Fade
        var t = NormalizedAge;
        
        if (t < 0.2f)
        {
            Color = Color.Lerp(Color.White, Color.Yellow, t / 0.2f);
        }
        else if (t < 0.4f)
        {
            Color = Color.Lerp(Color.Yellow, Color.Orange, (t - 0.2f) / 0.2f);
        }
        else if (t < 0.6f)
        {
            Color = Color.Lerp(Color.Orange, Color.Red, (t - 0.4f) / 0.2f);
        }
        else
        {
            var alpha = (byte)(255 * (1.0f - ((t - 0.6f) / 0.4f)));
            Color = new Color(Color.Red.R, Color.Red.G, Color.Red.B, alpha);
        }
        
        // Scale decreases over time
        Scale = InitialScale * (1.0f - t * 0.7f);
    }
}
```

## Particle Systems

### 1. Volumetric Explosion System

```csharp
public class VolumetricExplosionSystem : IParticleSystem
{
    private readonly ParticlePool<ExplosionParticle> _particlePool;
    private readonly List<ExplosionParticle> _activeParticles;
    private readonly Random _random;
    
    public string Name => "VolumetricExplosionSystem";
    public bool IsActive { get; set; } = true;
    public int MaxParticles { get; }
    public int ActiveParticleCount => _activeParticles.Count;
    public float EmissionRate { get; set; } = 0f; // Explosions are triggered, not continuous
    public Vector2 Position { get; set; }
    
    public VolumetricExplosionSystem(int maxParticles = 1000)
    {
        MaxParticles = maxParticles;
        _particlePool = new ParticlePool<ExplosionParticle>(maxParticles);
        _activeParticles = new List<ExplosionParticle>(maxParticles);
        _random = new Random();
    }
    
    public void CreateExplosion(Vector2 center, float intensity, ExplosionType type = ExplosionType.Standard)
    {
        var particleCount = (int)(intensity * 100f);
        particleCount = Math.Clamp(particleCount, 10, MaxParticles / 4);
        
        var parameters = new EmissionParameters
        {
            Position = center,
            Count = particleCount,
            UserData = new ExplosionData { Center = center, Intensity = intensity, Type = type }
        };
        
        Emit(particleCount, parameters);
    }
    
    public void Emit(int count, EmissionParameters parameters)
    {
        if (!IsActive) return;
        
        var explosionData = parameters.UserData as ExplosionData;
        var center = explosionData?.Center ?? parameters.Position;
        var intensity = explosionData?.Intensity ?? 1.0f;
        var type = explosionData?.Type ?? ExplosionType.Standard;
        
        for (int i = 0; i < count && _activeParticles.Count < MaxParticles; i++)
        {
            var particle = _particlePool.Get();
            if (particle == null) break;
            
            ConfigureExplosionParticle(particle, center, intensity, type);
            _activeParticles.Add(particle);
        }
    }
    
    private void ConfigureExplosionParticle(ExplosionParticle particle, Vector2 center, float intensity, ExplosionType type)
    {
        // Random direction
        var angle = (float)_random.NextDouble() * MathF.PI * 2;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        
        // Distance from center
        var distance = (float)Math.Pow(_random.NextDouble(), 0.5) * intensity * 50f;
        
        // Configure particle
        particle.Position = center + direction * distance * 0.1f; // Start near center
        particle.Velocity = direction * intensity * (100f + (float)_random.NextDouble() * 100f);
        particle.ExplosionCenter = center;
        particle.ExplosionForce = intensity * 500f;
        particle.ShockwaveRadius = 20f;
        particle.ShockwaveSpeed = 200f;
        
        // Configure based on explosion type
        switch (type)
        {
            case ExplosionType.Standard:
                particle.Lifetime = 0.5f + (float)_random.NextDouble() * 0.5f;
                particle.InitialScale = 1.0f + (float)_random.NextDouble() * 2.0f;
                particle.InitialColor = Color.Orange;
                break;
                
            case ExplosionType.Nuclear:
                particle.Lifetime = 1.0f + (float)_random.NextDouble() * 1.0f;
                particle.InitialScale = 2.0f + (float)_random.NextDouble() * 4.0f;
                particle.InitialColor = Color.White;
                particle.ShockwaveSpeed = 400f;
                break;
                
            case ExplosionType.Plasma:
                particle.Lifetime = 0.3f + (float)_random.NextDouble() * 0.3f;
                particle.InitialScale = 0.5f + (float)_random.NextDouble() * 1.0f;
                particle.InitialColor = Color.Cyan;
                particle.BlendMode = ParticleBlendMode.Additive;
                break;
        }
        
        particle.Scale = particle.InitialScale;
        particle.Color = particle.InitialColor;
        particle.Drag = 0.92f + (float)_random.NextDouble() * 0.06f;
        particle.AngularVelocity = ((float)_random.NextDouble() - 0.5f) * 10f;
    }
    
    public void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        var context = new ParticleUpdateContext
        {
            DeltaTime = deltaTime,
            HasGravity = false, // Explosions are not affected by gravity initially
            HasWind = true,
            WindForce = new Vector2(10f, 0f), // Slight horizontal wind
            WindStrength = 0.1f
        };
        
        // Update all particles
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime, context);
            
            if (particle.ShouldDestroy)
            {
                _activeParticles.RemoveAt(i);
                _particlePool.Return(particle);
            }
        }
    }
    
    public void Render(IParticleRenderer renderer)
    {
        if (!IsActive || _activeParticles.Count == 0) return;
        
        renderer.BeginBatch();
        
        // Sort particles by distance for proper blending
        var sortedParticles = _activeParticles
            .OrderBy(p => Vector2.DistanceSquared(p.Position, renderer.CameraPosition))
            .ToList();
        
        foreach (var particle in sortedParticles)
        {
            renderer.DrawParticle(particle.GetRenderData());
        }
        
        renderer.EndBatch();
    }
    
    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            _particlePool.Return(particle);
        }
        _activeParticles.Clear();
    }
    
    public ParticleSystemStats GetStatistics()
    {
        return new ParticleSystemStats
        {
            SystemName = Name,
            ActiveParticles = ActiveParticleCount,
            MaxParticles = MaxParticles,
            UtilizationRate = (float)ActiveParticleCount / MaxParticles,
            MemoryUsage = ActiveParticleCount * Marshal.SizeOf<ExplosionParticle>(),
            FrameTime = 0f // Would be calculated by performance monitor
        };
    }
    
    public void Dispose()
    {
        Clear();
        _particlePool?.Dispose();
    }
    
    private class ExplosionData
    {
        public Vector2 Center;
        public float Intensity;
        public ExplosionType Type;
    }
}

public enum ExplosionType
{
    Standard,    // Orange/red explosion
    Nuclear,     // White/yellow with large shockwave
    Plasma,      // Blue/cyan energy explosion
    Electric,    // Purple/blue with sparks
    Chemical     // Green/toxic explosion
}
```

### 2. Advanced Engine Trail System

```csharp
public class AdvancedEngineSystem : IParticleSystem
{
    private readonly ParticlePool<EngineTrailParticle> _particlePool;
    private readonly List<EngineTrailParticle> _activeParticles;
    private readonly Dictionary<uint, EngineEmitter> _emitters;
    private readonly Random _random;
    
    public string Name => "AdvancedEngineSystem";
    public bool IsActive { get; set; } = true;
    public int MaxParticles { get; }
    public int ActiveParticleCount => _activeParticles.Count;
    public float EmissionRate { get; set; } = 60f; // 60 particles per second
    public Vector2 Position { get; set; }
    
    private float _emissionTimer;
    
    public AdvancedEngineSystem(int maxParticles = 2000)
    {
        MaxParticles = maxParticles;
        _particlePool = new ParticlePool<EngineTrailParticle>(maxParticles);
        _activeParticles = new List<EngineTrailParticle>(maxParticles);
        _emitters = new Dictionary<uint, EngineEmitter>();
        _random = new Random();
    }
    
    public void RegisterEngine(uint engineId, Vector2 position, Vector2 thrustDirection, EngineType type)
    {
        _emitters[engineId] = new EngineEmitter
        {
            Id = engineId,
            Position = position,
            ThrustDirection = thrustDirection,
            Type = type,
            IsActive = true,
            ThrustIntensity = 1.0f
        };
    }
    
    public void UpdateEngine(uint engineId, Vector2 position, Vector2 velocity, float thrustIntensity)
    {
        if (_emitters.TryGetValue(engineId, out var emitter))
        {
            emitter.Position = position;
            emitter.Velocity = velocity;
            emitter.ThrustIntensity = thrustIntensity;
            emitter.IsActive = thrustIntensity > 0.01f;
        }
    }
    
    public void UnregisterEngine(uint engineId)
    {
        _emitters.Remove(engineId);
    }
    
    public void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        // Update emission timer
        _emissionTimer += deltaTime;
        
        // Emit particles from active engines
        if (_emissionTimer >= 1.0f / EmissionRate)
        {
            EmitFromEngines();
            _emissionTimer = 0f;
        }
        
        // Update existing particles
        var context = new ParticleUpdateContext
        {
            DeltaTime = deltaTime,
            HasGravity = false,
            HasWind = true,
            WindForce = new Vector2(0f, -5f), // Slight upward draft
            WindStrength = 0.05f
        };
        
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime, context);
            
            if (particle.ShouldDestroy)
            {
                _activeParticles.RemoveAt(i);
                _particlePool.Return(particle);
            }
        }
    }
    
    private void EmitFromEngines()
    {
        foreach (var emitter in _emitters.Values)
        {
            if (!emitter.IsActive || emitter.ThrustIntensity <= 0.01f) continue;
            
            var particlesThisFrame = (int)(emitter.ThrustIntensity * 3f); // 0-3 particles per frame
            
            for (int i = 0; i < particlesThisFrame; i++)
            {
                if (_activeParticles.Count >= MaxParticles) break;
                
                var particle = _particlePool.Get();
                if (particle == null) break;
                
                ConfigureEngineParticle(particle, emitter);
                _activeParticles.Add(particle);
            }
        }
    }
    
    private void ConfigureEngineParticle(EngineTrailParticle particle, EngineEmitter emitter)
    {
        // Position with slight random offset
        var offsetRadius = 2f;
        var offsetAngle = (float)_random.NextDouble() * MathF.PI * 2;
        var offset = new Vector2(MathF.Cos(offsetAngle), MathF.Sin(offsetAngle)) * 
                     (float)_random.NextDouble() * offsetRadius;
        
        particle.Position = emitter.Position + offset;
        particle.EnginePosition = emitter.Position;
        particle.EngineVelocity = emitter.Velocity;
        
        // Velocity opposite to thrust direction with random spread
        var baseDirection = -Vector2.Normalize(emitter.ThrustDirection);
        var spreadAngle = ((float)_random.NextDouble() - 0.5f) * MathF.PI * 0.3f; // 30 degree spread
        var rotatedDirection = Vector2.Transform(baseDirection, Matrix3x2.CreateRotation(spreadAngle));
        
        var speed = emitter.ThrustIntensity * (50f + (float)_random.NextDouble() * 50f);
        particle.Velocity = emitter.Velocity + rotatedDirection * speed;
        
        // Configure visual properties based on engine type
        switch (emitter.Type)
        {
            case EngineType.Chemical:
                particle.Lifetime = 0.3f + (float)_random.NextDouble() * 0.2f;
                particle.InitialColor = Color.Orange;
                particle.TurbulenceStrength = 15f;
                particle.HeatIntensity = 1.0f;
                break;
                
            case EngineType.Ion:
                particle.Lifetime = 0.8f + (float)_random.NextDouble() * 0.4f;
                particle.InitialColor = Color.Cyan;
                particle.TurbulenceStrength = 5f;
                particle.HeatIntensity = 0.3f;
                particle.BlendMode = ParticleBlendMode.Additive;
                break;
                
            case EngineType.Nuclear:
                particle.Lifetime = 0.5f + (float)_random.NextDouble() * 0.3f;
                particle.InitialColor = Color.White;
                particle.TurbulenceStrength = 20f;
                particle.HeatIntensity = 1.5f;
                break;
        }
        
        particle.InitialScale = 0.5f + (float)_random.NextDouble() * 1.0f;
        particle.Scale = particle.InitialScale;
        particle.Color = particle.InitialColor;
        particle.AngularVelocity = ((float)_random.NextDouble() - 0.5f) * 5f;
    }
    
    public void Emit(int count, EmissionParameters parameters)
    {
        // Not typically used directly - engines are managed through RegisterEngine/UpdateEngine
    }
    
    public void Render(IParticleRenderer renderer)
    {
        if (!IsActive || _activeParticles.Count == 0) return;
        
        renderer.BeginBatch();
        
        // Group particles by blend mode for efficient rendering
        var additiveParticles = new List<ParticleRenderData>();
        var alphaParticles = new List<ParticleRenderData>();
        
        foreach (var particle in _activeParticles)
        {
            var renderData = particle.GetRenderData();
            
            if (particle.BlendMode == ParticleBlendMode.Additive)
                additiveParticles.Add(renderData);
            else
                alphaParticles.Add(renderData);
        }
        
        // Render additive particles first (they don't need depth sorting)
        if (additiveParticles.Count > 0)
        {
            var additiveMaterial = new ParticleMaterial { BlendMode = ParticleBlendMode.Additive };
            renderer.DrawBatch(additiveParticles.ToArray(), additiveMaterial);
        }
        
        // Render alpha particles (sorted by distance)
        if (alphaParticles.Count > 0)
        {
            var sortedAlpha = alphaParticles
                .OrderBy(p => Vector2.DistanceSquared(p.Position, renderer.CameraPosition))
                .ToArray();
            
            var alphaMaterial = new ParticleMaterial { BlendMode = ParticleBlendMode.Alpha };
            renderer.DrawBatch(sortedAlpha, alphaMaterial);
        }
        
        renderer.EndBatch();
    }
    
    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            _particlePool.Return(particle);
        }
        _activeParticles.Clear();
        _emitters.Clear();
    }
    
    public ParticleSystemStats GetStatistics()
    {
        return new ParticleSystemStats
        {
            SystemName = Name,
            ActiveParticles = ActiveParticleCount,
            MaxParticles = MaxParticles,
            UtilizationRate = (float)ActiveParticleCount / MaxParticles,
            MemoryUsage = ActiveParticleCount * Marshal.SizeOf<EngineTrailParticle>(),
            ActiveEmitters = _emitters.Count(e => e.Value.IsActive)
        };
    }
    
    public void Dispose()
    {
        Clear();
        _particlePool?.Dispose();
    }
    
    private class EngineEmitter
    {
        public uint Id;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 ThrustDirection;
        public EngineType Type;
        public bool IsActive;
        public float ThrustIntensity;
    }
}

public enum EngineType
{
    Chemical,   // Orange/red flames
    Ion,        // Blue/cyan energy
    Nuclear,    // White/bright energy
    Plasma      // Purple/magenta energy
}
```

## Performance Optimization Systems

### 1. Particle LOD System

```csharp
public class ParticleLODSystem
{
    private readonly Dictionary<ParticleLayer, LODConfiguration> _lodConfigs;
    private readonly PerformanceMetrics _metrics;
    private Vector2 _cameraPosition;
    private float _cameraZoom;
    
    public ParticleLODSystem()
    {
        _lodConfigs = new Dictionary<ParticleLayer, LODConfiguration>
        {
            [ParticleLayer.Effects] = new LODConfiguration
            {
                NearDistance = 100f,
                MidDistance = 300f,
                FarDistance = 500f,
                NearQuality = 1.0f,
                MidQuality = 0.6f,
                FarQuality = 0.3f,
                CullDistance = 800f
            },
            [ParticleLayer.GameWorld] = new LODConfiguration
            {
                NearDistance = 150f,
                MidDistance = 400f,
                FarDistance = 600f,
                NearQuality = 1.0f,
                MidQuality = 0.8f,
                FarQuality = 0.5f,
                CullDistance = 1000f
            }
        };
        
        _metrics = new PerformanceMetrics();
    }
    
    public LODLevel GetLODLevel(IParticle particle)
    {
        var distance = Vector2.Distance(particle.Position, _cameraPosition) / _cameraZoom;
        var config = _lodConfigs.GetValueOrDefault(particle.Layer, _lodConfigs[ParticleLayer.Effects]);
        
        if (distance >= config.CullDistance)
            return LODLevel.Culled;
        else if (distance >= config.FarDistance)
            return LODLevel.Far;
        else if (distance >= config.MidDistance)
            return LODLevel.Mid;
        else
            return LODLevel.Near;
    }
    
    public bool ShouldRenderParticle(IParticle particle, out float qualityMultiplier)
    {
        var lodLevel = GetLODLevel(particle);
        var config = _lodConfigs.GetValueOrDefault(particle.Layer, _lodConfigs[ParticleLayer.Effects]);
        
        switch (lodLevel)
        {
            case LODLevel.Near:
                qualityMultiplier = config.NearQuality;
                return true;
                
            case LODLevel.Mid:
                qualityMultiplier = config.MidQuality;
                // Skip some particles at mid distance
                return (particle.PoolId % 2) == 0;
                
            case LODLevel.Far:
                qualityMultiplier = config.FarQuality;
                // Skip most particles at far distance
                return (particle.PoolId % 4) == 0;
                
            case LODLevel.Culled:
                qualityMultiplier = 0f;
                return false;
                
            default:
                qualityMultiplier = 1f;
                return true;
        }
    }
    
    public void UpdateCamera(Vector2 position, float zoom)
    {
        _cameraPosition = position;
        _cameraZoom = zoom;
    }
    
    private class LODConfiguration
    {
        public float NearDistance;
        public float MidDistance;
        public float FarDistance;
        public float CullDistance;
        public float NearQuality;
        public float MidQuality;
        public float FarQuality;
    }
}

public enum LODLevel
{
    Near,
    Mid,
    Far,
    Culled
}
```

## Master Particle System

### 1. Particle System Manager

```csharp
public class MasterParticleSystem3D : IDisposable
{
    private readonly Dictionary<string, IParticleSystem> _particleSystems;
    private readonly ParticleLODSystem _lodSystem;
    private readonly IParticleRenderer _renderer;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ParticleSystemConfiguration _config;
    
    // Built-in systems
    public VolumetricExplosionSystem ExplosionSystem { get; private set; }
    public AdvancedEngineSystem EngineSystem { get; private set; }
    public WeaponEffectSystem WeaponSystem { get; private set; }
    public EnvironmentalParticleSystem EnvironmentalSystem { get; private set; }
    
    // Performance tracking
    public int TotalActiveParticles => _particleSystems.Values.Sum(s => s.ActiveParticleCount);
    public float FrameTime { get; private set; }
    public float MemoryUsage { get; private set; }
    
    public MasterParticleSystem3D(IParticleRenderer renderer, ParticleSystemConfiguration config = null)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _config = config ?? ParticleSystemConfiguration.Default;
        
        _particleSystems = new Dictionary<string, IParticleSystem>();
        _lodSystem = new ParticleLODSystem();
        _performanceMonitor = new PerformanceMonitor("ParticleSystem");
        
        InitializeBuiltInSystems();
    }
    
    private void InitializeBuiltInSystems()
    {
        ExplosionSystem = new VolumetricExplosionSystem(_config.MaxExplosionParticles);
        EngineSystem = new AdvancedEngineSystem(_config.MaxEngineParticles);
        WeaponSystem = new WeaponEffectSystem(_config.MaxWeaponParticles);
        EnvironmentalSystem = new EnvironmentalParticleSystem(_config.MaxEnvironmentalParticles);
        
        RegisterSystem(ExplosionSystem);
        RegisterSystem(EngineSystem);
        RegisterSystem(WeaponSystem);
        RegisterSystem(EnvironmentalSystem);
    }
    
    public void RegisterSystem(IParticleSystem system)
    {
        if (system == null) throw new ArgumentNullException(nameof(system));
        
        _particleSystems[system.Name] = system;
    }
    
    public void UnregisterSystem(string systemName)
    {
        if (_particleSystems.TryGetValue(systemName, out var system))
        {
            system.Dispose();
            _particleSystems.Remove(systemName);
        }
    }
    
    public T GetSystem<T>() where T : class, IParticleSystem
    {
        return _particleSystems.Values.OfType<T>().FirstOrDefault();
    }
    
    public void Update(float deltaTime)
    {
        _performanceMonitor.BeginFrame();
        
        // Update camera information for LOD system
        _lodSystem.UpdateCamera(_renderer.CameraPosition, _renderer.CameraZoom);
        
        // Update all particle systems
        foreach (var system in _particleSystems.Values)
        {
            if (system.IsActive)
            {
                system.Update(deltaTime);
            }
        }
        
        // Update performance metrics
        UpdatePerformanceMetrics();
        
        _performanceMonitor.EndFrame();
        FrameTime = _performanceMonitor.LastFrameTime;
    }
    
    public void Render()
    {
        _performanceMonitor.BeginRender();
        
        _renderer.BeginBatch();
        
        // Render systems in layer order
        var orderedSystems = _particleSystems.Values
            .OrderBy(s => GetSystemRenderPriority(s))
            .ToList();
        
        foreach (var system in orderedSystems)
        {
            if (system.IsActive)
            {
                system.Render(_renderer);
            }
        }
        
        _renderer.EndBatch();
        
        _performanceMonitor.EndRender();
    }
    
    private int GetSystemRenderPriority(IParticleSystem system)
    {
        return system switch
        {
            EnvironmentalParticleSystem => 0,
            EngineSystem => 1,
            WeaponEffectSystem => 2,
            VolumetricExplosionSystem => 3,
            _ => 10
        };
    }
    
    private void UpdatePerformanceMetrics()
    {
        long totalMemory = 0;
        
        foreach (var system in _particleSystems.Values)
        {
            var stats = system.GetStatistics();
            totalMemory += stats.MemoryUsage;
        }
        
        MemoryUsage = totalMemory / (1024f * 1024f); // Convert to MB
    }
    
    public ParticleSystemManagerStats GetGlobalStatistics()
    {
        var systemStats = _particleSystems.Values.Select(s => s.GetStatistics()).ToList();
        
        return new ParticleSystemManagerStats
        {
            TotalSystems = _particleSystems.Count,
            TotalActiveParticles = TotalActiveParticles,
            TotalMemoryUsage = MemoryUsage,
            FrameTime = FrameTime,
            SystemStatistics = systemStats,
            PerformanceMetrics = _performanceMonitor.GetSnapshot()
        };
    }
    
    public void Clear()
    {
        foreach (var system in _particleSystems.Values)
        {
            system.Clear();
        }
    }
    
    public void Dispose()
    {
        foreach (var system in _particleSystems.Values)
        {
            system.Dispose();
        }
        
        _particleSystems.Clear();
        _performanceMonitor?.Dispose();
    }
}
```

This enhanced particle system provides high-performance, flexible particle rendering with advanced visual effects, LOD optimization, and comprehensive performance monitoring for the Asteroids game.