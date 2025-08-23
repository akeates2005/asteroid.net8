using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Manages environmental hazards including black holes, asteroid storms,
    /// energy fields, radiation zones, and other dangerous space phenomena
    /// </summary>
    public class HazardManager : IEnvironmentSystem
    {
        private readonly HazardSettings _settings;
        private readonly List<EnvironmentalHazard> _activeHazards;
        private readonly Random _random;
        
        private float _hazardSpawnTimer;
        private LODLevel _currentLOD = LODLevel.High;
        private bool _isInitialized;
        
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public int ElementCount => _activeHazards.Count;
        public string SystemName => "Hazards";
        
        public event Action<PerformanceImpact> OnPerformanceImpact;
        public event Action<HazardEvent> OnHazardDetected;
        
        public HazardManager(HazardSettings settings)
        {
            _settings = settings ?? HazardSettings.CreateDefault();
            _activeHazards = new List<EnvironmentalHazard>();
            _random = new Random();
        }
        
        public void Initialize()
        {
            try
            {
                // Initialize with some base hazards based on settings
                CreateInitialHazards();
                
                _isInitialized = true;
                ErrorManager.LogInfo($"Hazard Manager initialized with {_activeHazards.Count} initial hazards");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize Hazard Manager", ex);
                throw;
            }
        }
        
        private void CreateInitialHazards()
        {
            if (_settings.EnableBlackHoles && _settings.BaseHazardLevel >= HazardLevel.Medium)
            {
                CreateBlackHole(GenerateRandomPosition(Vector3.Zero, 500f), 1.0f);
            }
            
            if (_settings.EnableEnergyFields && _settings.BaseHazardLevel >= HazardLevel.Low)
            {
                for (int i = 0; i < (int)_settings.BaseHazardLevel; i++)
                {
                    CreateEnergyField(GenerateRandomPosition(Vector3.Zero, 300f), 0.8f);
                }
            }
            
            if (_settings.EnableRadiationZones && _settings.BaseHazardLevel >= HazardLevel.High)
            {
                CreateRadiationZone(GenerateRandomPosition(Vector3.Zero, 400f), 0.6f);
            }
        }
        
        private Vector3 GenerateRandomPosition(Vector3 center, float maxDistance)
        {
            var angle = _random.NextSingle() * MathF.PI * 2f;
            var elevation = (_random.NextSingle() - 0.5f) * MathF.PI * 0.5f;
            var distance = _random.NextSingle() * maxDistance;
            
            return center + new Vector3(
                distance * MathF.Cos(elevation) * MathF.Cos(angle),
                distance * MathF.Sin(elevation),
                distance * MathF.Cos(elevation) * MathF.Sin(angle)
            );
        }
        
        public void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            _hazardSpawnTimer += deltaTime;
            
            // Update existing hazards
            UpdateHazards(deltaTime, camera, playerPosition);
            
            // Remove expired hazards
            RemoveExpiredHazards();
            
            // Spawn new hazards
            if (_hazardSpawnTimer >= GetHazardSpawnInterval())
            {
                TrySpawnHazard(playerPosition);
                _hazardSpawnTimer = 0f;
            }
            
            var updateTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(updateTime, 0f);
        }
        
        private void UpdateHazards(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            foreach (var hazard in _activeHazards)
            {
                hazard.Update(deltaTime, camera, playerPosition);
                
                // Check for player proximity and trigger events
                var distance = Vector3.Distance(hazard.Position, playerPosition);
                if (distance <= hazard.DetectionRadius)
                {
                    TriggerHazardEvent(hazard, playerPosition, distance);
                }
            }
        }
        
        private void RemoveExpiredHazards()
        {
            for (int i = _activeHazards.Count - 1; i >= 0; i--)
            {
                if (_activeHazards[i].IsExpired)
                {
                    _activeHazards.RemoveAt(i);
                }
            }
        }
        
        private float GetHazardSpawnInterval()
        {
            return (1f / _settings.HazardSpawnRate) * (1f / (float)(_settings.BaseHazardLevel + 1));
        }
        
        private void TrySpawnHazard(Vector3 playerPosition)
        {
            if (_activeHazards.Count >= GetMaxHazards()) return;
            
            var hazardType = DetermineHazardType();
            if (hazardType != HazardType.None)
            {
                var position = GenerateHazardPosition(playerPosition);
                var intensity = CalculateHazardIntensity();
                
                CreateHazard(hazardType, position, intensity);
            }
        }
        
        private int GetMaxHazards()
        {
            return _settings.BaseHazardLevel switch
            {
                HazardLevel.None => 0,
                HazardLevel.Low => 3,
                HazardLevel.Medium => 6,
                HazardLevel.High => 10,
                HazardLevel.Extreme => 15,
                HazardLevel.Apocalyptic => 25,
                _ => 6
            };
        }
        
        private HazardType DetermineHazardType()
        {
            var roll = _random.NextSingle();
            
            if (roll < 0.1f && _settings.EnableBlackHoles)
                return HazardType.BlackHole;
            if (roll < 0.3f && _settings.EnableAsteroidStorms)
                return HazardType.AsteroidStorm;
            if (roll < 0.6f && _settings.EnableEnergyFields)
                return HazardType.EnergyField;
            if (roll < 0.8f && _settings.EnableRadiationZones)
                return HazardType.RadiationZone;
            if (roll < 0.9f && _settings.EnableEnergyFields)
                return HazardType.GravityWell;
            if (roll < 0.95f && _settings.EnableBlackHoles)
                return HazardType.SingularityRift;
            
            return HazardType.None;
        }
        
        private Vector3 GenerateHazardPosition(Vector3 playerPosition)
        {
            var distance = _random.NextSingle() * _settings.MaxHazardDistance + 100f;
            var angle = _random.NextSingle() * MathF.PI * 2f;
            var elevation = (_random.NextSingle() - 0.5f) * MathF.PI * 0.3f;
            
            return playerPosition + new Vector3(
                distance * MathF.Cos(elevation) * MathF.Cos(angle),
                distance * MathF.Sin(elevation),
                distance * MathF.Cos(elevation) * MathF.Sin(angle)
            );
        }
        
        private float CalculateHazardIntensity()
        {
            var baseIntensity = (float)_settings.BaseHazardLevel / 5f;
            var randomVariation = (_random.NextSingle() - 0.5f) * 0.4f;
            return Math.Clamp(baseIntensity + randomVariation, 0.1f, 2.0f);
        }
        
        private void CreateHazard(HazardType type, Vector3 position, float intensity)
        {
            EnvironmentalHazard hazard = type switch
            {
                HazardType.BlackHole => new BlackHole(position, intensity),
                HazardType.AsteroidStorm => new AsteroidStorm(position, intensity),
                HazardType.EnergyField => new EnergyField(position, intensity),
                HazardType.RadiationZone => new RadiationZone(position, intensity),
                HazardType.GravityWell => new GravityWell(position, intensity),
                HazardType.SingularityRift => new SingularityRift(position, intensity),
                HazardType.TemporalAnomaly => new TemporalAnomaly(position, intensity),
                HazardType.DarkMatterCloud => new DarkMatterCloud(position, intensity),
                _ => null
            };
            
            if (hazard != null)
            {
                _activeHazards.Add(hazard);
            }
        }
        
        private void TriggerHazardEvent(EnvironmentalHazard hazard, Vector3 playerPosition, float distance)
        {
            var hazardEvent = new HazardEvent
            {
                HazardType = hazard.Type,
                Position = hazard.Position,
                Intensity = hazard.Intensity * (1f - distance / hazard.DetectionRadius),
                Distance = distance,
                Radius = hazard.EffectRadius,
                IsLethal = hazard.IsLethal && distance <= hazard.LethalRadius
            };
            
            OnHazardDetected?.Invoke(hazardEvent);
        }
        
        public void Render(Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !IsVisible || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            foreach (var hazard in _activeHazards)
            {
                var distance = Vector3.Distance(hazard.Position, camera.Position);
                if (distance <= hazard.RenderDistance)
                {
                    hazard.Render(camera, _currentLOD);
                }
            }
            
            var renderTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(0f, renderTime);
        }
        
        public void CreateBlackHole(Vector3 position, float intensity)
        {
            var blackHole = new BlackHole(position, intensity);
            _activeHazards.Add(blackHole);
        }
        
        public void CreateAsteroidStorm(Vector3 position, float intensity)
        {
            var storm = new AsteroidStorm(position, intensity);
            _activeHazards.Add(storm);
        }
        
        public void EnableAsteroidField(bool enable)
        {
            _settings.EnableAsteroidStorms = enable;
        }
        
        public void SetHazardLevel(HazardLevel level)
        {
            _settings.BaseHazardLevel = level;
        }
        
        public List<HazardInfo> GetActiveHazards()
        {
            var hazards = new List<HazardInfo>();
            foreach (var hazard in _activeHazards)
            {
                hazards.Add(new HazardInfo
                {
                    Type = hazard.Type,
                    Position = hazard.Position,
                    Intensity = hazard.Intensity,
                    Radius = hazard.EffectRadius,
                    IsLethal = hazard.IsLethal
                });
            }
            return hazards;
        }
        
        public void UpdateLOD(LODLevel lodLevel)
        {
            _currentLOD = lodLevel;
        }
        
        public void SetQuality(EnvironmentQuality quality)
        {
            switch (quality)
            {
                case EnvironmentQuality.Potato:
                    _settings.EnableBlackHoles = false;
                    _settings.EnableEnergyFields = false;
                    _settings.EnableRadiationZones = false;
                    _settings.EnableAsteroidStorms = true;
                    break;
                case EnvironmentQuality.Low:
                    _settings.EnableBlackHoles = false;
                    _settings.EnableEnergyFields = true;
                    _settings.EnableRadiationZones = false;
                    _settings.EnableAsteroidStorms = true;
                    break;
                case EnvironmentQuality.Medium:
                case EnvironmentQuality.High:
                case EnvironmentQuality.Ultra:
                case EnvironmentQuality.Extreme:
                    _settings.EnableBlackHoles = true;
                    _settings.EnableEnergyFields = true;
                    _settings.EnableRadiationZones = true;
                    _settings.EnableAsteroidStorms = true;
                    break;
            }
        }
        
        private void ReportPerformanceImpact(float updateTime, float renderTime)
        {
            var impact = new PerformanceImpact
            {
                SystemName = SystemName,
                UpdateTime = updateTime,
                RenderTime = renderTime,
                ElementCount = ElementCount,
                MemoryUsage = EstimateMemoryUsage(),
                Severity = CalculateSeverity(updateTime, renderTime)
            };
            
            OnPerformanceImpact?.Invoke(impact);
        }
        
        private float EstimateMemoryUsage()
        {
            return (_activeHazards.Count * 256f) / (1024f * 1024f);
        }
        
        private float CalculateSeverity(float updateTime, float renderTime)
        {
            var totalTime = updateTime + renderTime;
            if (totalTime < 1f) return 0f;
            if (totalTime < 3f) return 0.4f;
            if (totalTime < 6f) return 0.7f;
            return 1f;
        }
        
        public SystemStatistics GetStatistics()
        {
            return new SystemStatistics
            {
                SystemName = SystemName,
                IsActive = IsActive,
                ElementCount = ElementCount,
                UpdateTime = 0f,
                RenderTime = 0f,
                MemoryUsage = EstimateMemoryUsage(),
                CurrentLOD = _currentLOD,
                Quality = EnvironmentQuality.High,
                CustomData = new Dictionary<string, object>
                {
                    ["ActiveHazards"] = _activeHazards.Count,
                    ["HazardLevel"] = _settings.BaseHazardLevel,
                    ["BlackHolesEnabled"] = _settings.EnableBlackHoles,
                    ["AsteroidStormsEnabled"] = _settings.EnableAsteroidStorms
                }
            };
        }
        
        public List<EnvironmentEvent> GetEnvironmentalEvents(Vector3 playerPosition)
        {
            var events = new List<EnvironmentEvent>();
            
            foreach (var hazard in _activeHazards)
            {
                var distance = Vector3.Distance(hazard.Position, playerPosition);
                if (distance <= hazard.DetectionRadius)
                {
                    var environmentEvent = hazard.GetEnvironmentEvent(playerPosition, distance);
                    if (environmentEvent.HasValue)
                    {
                        events.Add(environmentEvent.Value);
                    }
                }
            }
            
            return events;
        }
        
        public void Dispose()
        {
            _activeHazards?.Clear();
        }
    }
    
    // Supporting data structures and hazard implementations
    public struct HazardEvent
    {
        public HazardType HazardType;
        public Vector3 Position;
        public float Intensity;
        public float Distance;
        public float Radius;
        public bool IsLethal;
    }
    
    public struct HazardInfo
    {
        public HazardType Type;
        public Vector3 Position;
        public float Intensity;
        public float Radius;
        public bool IsLethal;
    }
    
    public enum HazardType
    {
        None,
        BlackHole,
        AsteroidStorm,
        EnergyField,
        RadiationZone,
        GravityWell,
        SingularityRift,
        TemporalAnomaly,
        DarkMatterCloud
    }
    
    public abstract class EnvironmentalHazard
    {
        public Vector3 Position { get; protected set; }
        public float Intensity { get; protected set; }
        public float EffectRadius { get; protected set; }
        public float DetectionRadius { get; protected set; }
        public float RenderDistance { get; protected set; }
        public float LethalRadius { get; protected set; }
        public HazardType Type { get; protected set; }
        public bool IsLethal { get; protected set; }
        public bool IsExpired { get; protected set; }
        
        protected float _lifetime;
        protected float _maxLifetime;
        protected Random _random = new Random();
        
        public abstract void Update(float deltaTime, Camera3D camera, Vector3 playerPosition);
        public abstract void Render(Camera3D camera, LODLevel lodLevel);
        public abstract EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance);
    }
    
    public class BlackHole : EnvironmentalHazard
    {
        private float _rotationSpeed;
        private float _accretionDiskRadius;
        private List<Vector3> _accretionParticles;
        private float _particleTimer;
        
        public BlackHole(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.BlackHole;
            
            EffectRadius = 100f + intensity * 200f;
            DetectionRadius = EffectRadius * 2f;
            RenderDistance = DetectionRadius * 2f;
            LethalRadius = EffectRadius * 0.3f;
            IsLethal = true;
            
            _maxLifetime = float.PositiveInfinity; // Black holes are permanent
            _rotationSpeed = intensity * 2f;
            _accretionDiskRadius = EffectRadius * 1.5f;
            _accretionParticles = new List<Vector3>();
            
            GenerateAccretionDisk();
        }
        
        private void GenerateAccretionDisk()
        {
            int particleCount = (int)(50 + Intensity * 100);
            for (int i = 0; i < particleCount; i++)
            {
                var angle = _random.NextSingle() * MathF.PI * 2f;
                var radius = _random.NextSingle() * _accretionDiskRadius + EffectRadius;
                var height = (_random.NextSingle() - 0.5f) * radius * 0.1f;
                
                _accretionParticles.Add(Position + new Vector3(
                    MathF.Cos(angle) * radius,
                    height,
                    MathF.Sin(angle) * radius
                ));
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            _particleTimer += deltaTime;
            
            // Rotate accretion disk
            var rotationAngle = _rotationSpeed * deltaTime;
            for (int i = 0; i < _accretionParticles.Count; i++)
            {
                var relative = _accretionParticles[i] - Position;
                var distance = new Vector2(relative.X, relative.Z).Length();
                var currentAngle = MathF.Atan2(relative.Z, relative.X);
                var newAngle = currentAngle + rotationAngle / (distance * 0.01f); // Slower at greater distances
                
                _accretionParticles[i] = Position + new Vector3(
                    MathF.Cos(newAngle) * distance,
                    relative.Y,
                    MathF.Sin(newAngle) * distance
                );
            }
            
            // Add new particles periodically
            if (_particleTimer >= 1f)
            {
                AddAccretionParticle();
                _particleTimer = 0f;
            }
        }
        
        private void AddAccretionParticle()
        {
            var angle = _random.NextSingle() * MathF.PI * 2f;
            var radius = _accretionDiskRadius + _random.NextSingle() * 50f;
            var height = (_random.NextSingle() - 0.5f) * 5f;
            
            var newParticle = Position + new Vector3(
                MathF.Cos(angle) * radius,
                height,
                MathF.Sin(angle) * radius
            );
            
            _accretionParticles.Add(newParticle);
            
            // Remove oldest particles to maintain count
            if (_accretionParticles.Count > 200)
            {
                _accretionParticles.RemoveAt(0);
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            // Event horizon (black sphere)
            Raylib.DrawSphere(Position, LethalRadius, Color.Black);
            
            // Gravitational lensing effect
            if (lodLevel >= LODLevel.Medium)
            {
                var lensColor = new Color(50, 50, 100, 100);
                for (int i = 1; i <= 3; i++)
                {
                    var ringRadius = EffectRadius * i * 0.4f;
                    Raylib.DrawSphereWires(Position, ringRadius, 16, 16,
                        new Color(lensColor.R, lensColor.G, lensColor.B, lensColor.A / i));
                }
            }
            
            // Accretion disk
            if (lodLevel >= LODLevel.Low)
            {
                var diskColor = new Color(255, 150, 50, 150);
                foreach (var particle in _accretionParticles)
                {
                    if (lodLevel >= LODLevel.High)
                    {
                        var distance = Vector3.Distance(particle, Position);
                        var heat = Math.Clamp(1f - distance / _accretionDiskRadius, 0f, 1f);
                        var particleColor = new Color(
                            (byte)(255 * heat),
                            (byte)(150 * heat),
                            (byte)(50 * (1f - heat)),
                            150
                        );
                        Raylib.DrawSphere(particle, 0.5f, particleColor);
                    }
                    else
                    {
                        Raylib.DrawPoint3D(particle, diskColor);
                    }
                }
            }
            
            // Jets (for rotating black holes)
            if (lodLevel >= LODLevel.High && Intensity > 0.7f)
            {
                var jetColor = new Color(100, 200, 255, 100);
                var jetLength = EffectRadius * 3f;
                
                Raylib.DrawLine3D(
                    Position + new Vector3(0, jetLength, 0),
                    Position - new Vector3(0, jetLength, 0),
                    jetColor
                );
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            var eventType = distance <= LethalRadius ? 
                EnvironmentEventType.BlackHoleNearby : 
                EnvironmentEventType.GravityWellEntered;
            
            return new EnvironmentEvent
            {
                Type = eventType,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = float.PositiveInfinity,
                Parameters = new Dictionary<string, object>
                {
                    ["GravityStrength"] = Intensity * 50f * (LethalRadius / Math.Max(distance, 1f)),
                    ["TimeDilation"] = Intensity * 0.5f,
                    ["IsLethal"] = distance <= LethalRadius
                }
            };
        }
    }
    
    public class AsteroidStorm : EnvironmentalHazard
    {
        private List<Vector3> _asteroidPositions;
        private List<Vector3> _asteroidVelocities;
        private List<float> _asteroidSizes;
        private Vector3 _stormDirection;
        
        public AsteroidStorm(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.AsteroidStorm;
            
            EffectRadius = 150f + intensity * 250f;
            DetectionRadius = EffectRadius * 1.5f;
            RenderDistance = DetectionRadius * 2f;
            LethalRadius = EffectRadius * 0.8f;
            IsLethal = false; // Damaging but not instantly lethal
            
            _maxLifetime = 60f + intensity * 120f;
            _stormDirection = Vector3.Normalize(new Vector3(
                _random.NextSingle() - 0.5f,
                (_random.NextSingle() - 0.5f) * 0.2f,
                _random.NextSingle() - 0.5f
            ));
            
            _asteroidPositions = new List<Vector3>();
            _asteroidVelocities = new List<Vector3>();
            _asteroidSizes = new List<float>();
            
            GenerateAsteroids();
        }
        
        private void GenerateAsteroids()
        {
            int asteroidCount = (int)(20 + Intensity * 80);
            
            for (int i = 0; i < asteroidCount; i++)
            {
                // Generate positions in a cylindrical volume
                var angle = _random.NextSingle() * MathF.PI * 2f;
                var radius = _random.NextSingle() * EffectRadius;
                var height = (_random.NextSingle() - 0.5f) * EffectRadius * 0.3f;
                
                _asteroidPositions.Add(Position + new Vector3(
                    MathF.Cos(angle) * radius,
                    height,
                    MathF.Sin(angle) * radius
                ));
                
                // Velocities aligned with storm direction with variation
                var baseVelocity = _stormDirection * (10f + Intensity * 30f);
                var variation = new Vector3(
                    (_random.NextSingle() - 0.5f) * 10f,
                    (_random.NextSingle() - 0.5f) * 5f,
                    (_random.NextSingle() - 0.5f) * 10f
                );
                _asteroidVelocities.Add(baseVelocity + variation);
                
                _asteroidSizes.Add(_random.NextSingle() * 3f + 0.5f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            
            // Move asteroids
            for (int i = 0; i < _asteroidPositions.Count; i++)
            {
                _asteroidPositions[i] += _asteroidVelocities[i] * deltaTime;
            }
            
            // Move storm center
            Position += _stormDirection * Intensity * 5f * deltaTime;
            
            // Check expiration
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var stormColor = new Color(150, 100, 50, 100);
            
            // Render storm boundary
            if (lodLevel >= LODLevel.Medium)
            {
                Raylib.DrawSphereWires(Position, EffectRadius, 12, 12, 
                    new Color(stormColor.R, stormColor.G, stormColor.B, 50));
            }
            
            // Render asteroids
            for (int i = 0; i < _asteroidPositions.Count; i++)
            {
                var asteroidColor = new Color(120, 80, 60, 200);
                
                if (lodLevel >= LODLevel.High)
                {
                    // Detailed asteroid rendering
                    Raylib.DrawCube(_asteroidPositions[i], _asteroidSizes[i], _asteroidSizes[i], _asteroidSizes[i], asteroidColor);
                    
                    // Motion trail
                    var trailEnd = _asteroidPositions[i] - _asteroidVelocities[i] * 0.1f;
                    Raylib.DrawLine3D(_asteroidPositions[i], trailEnd, new Color(asteroidColor.R, asteroidColor.G, asteroidColor.B, 100));
                }
                else if (lodLevel >= LODLevel.Medium)
                {
                    Raylib.DrawSphere(_asteroidPositions[i], _asteroidSizes[i], asteroidColor);
                }
                else
                {
                    Raylib.DrawPoint3D(_asteroidPositions[i], asteroidColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.AsteroidStormApproaching,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["AsteroidCount"] = _asteroidPositions.Count,
                    ["StormVelocity"] = _stormDirection * Intensity * 5f,
                    ["CollisionRisk"] = Intensity * (1f - distance / EffectRadius)
                }
            };
        }
    }
    
    public class EnergyField : EnvironmentalHazard
    {
        private float _pulsePhase;
        private Color _fieldColor;
        private List<Vector3> _energyNodes;
        
        public EnergyField(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.EnergyField;
            
            EffectRadius = 50f + intensity * 100f;
            DetectionRadius = EffectRadius * 1.2f;
            RenderDistance = DetectionRadius * 1.5f;
            LethalRadius = 0f; // Not lethal, but can damage
            IsLethal = false;
            
            _maxLifetime = 45f + intensity * 90f;
            _fieldColor = GenerateEnergyColor();
            _energyNodes = new List<Vector3>();
            
            GenerateEnergyNodes();
        }
        
        private Color GenerateEnergyColor()
        {
            var colors = new[]
            {
                new Color(0, 255, 255, 150),   // Cyan
                new Color(255, 0, 255, 150),   // Magenta
                new Color(255, 255, 0, 150),   // Yellow
                new Color(0, 255, 0, 150),     // Green
                new Color(255, 100, 0, 150)    // Orange
            };
            
            return colors[_random.Next(colors.Length)];
        }
        
        private void GenerateEnergyNodes()
        {
            int nodeCount = (int)(5 + Intensity * 15);
            
            for (int i = 0; i < nodeCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius
                );
                _energyNodes.Add(Position + offset);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            _pulsePhase += deltaTime * Intensity * 3f;
            
            if (_pulsePhase > MathF.PI * 2f)
                _pulsePhase -= MathF.PI * 2f;
            
            // Animate energy nodes
            for (int i = 0; i < _energyNodes.Count; i++)
            {
                var offset = new Vector3(
                    MathF.Sin(_pulsePhase + i) * 2f,
                    MathF.Cos(_pulsePhase + i * 1.3f) * 2f,
                    MathF.Sin(_pulsePhase + i * 0.7f) * 2f
                );
                _energyNodes[i] = Position + offset;
            }
            
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var pulseBrightness = 0.7f + MathF.Sin(_pulsePhase) * 0.3f;
            var currentColor = new Color(
                (byte)(_fieldColor.R * pulseBrightness),
                (byte)(_fieldColor.G * pulseBrightness),
                (byte)(_fieldColor.B * pulseBrightness),
                _fieldColor.A
            );
            
            // Energy field boundary
            if (lodLevel >= LODLevel.Medium)
            {
                Raylib.DrawSphereWires(Position, EffectRadius, 16, 16, 
                    new Color(currentColor.R, currentColor.G, currentColor.B, 50));
            }
            
            // Energy nodes
            foreach (var node in _energyNodes)
            {
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawSphere(node, 2f + MathF.Sin(_pulsePhase) * 0.5f, currentColor);
                    
                    // Energy connections
                    foreach (var otherNode in _energyNodes)
                    {
                        if (node != otherNode && Vector3.Distance(node, otherNode) < EffectRadius * 0.6f)
                        {
                            Raylib.DrawLine3D(node, otherNode, 
                                new Color(currentColor.R, currentColor.G, currentColor.B, 30));
                        }
                    }
                }
                else
                {
                    Raylib.DrawPoint3D(node, currentColor);
                }
            }
            
            // Central energy core
            Raylib.DrawSphere(Position, 3f, currentColor);
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.EnergyFieldEntered,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["EnergyType"] = _fieldColor.ToString(),
                    ["PowerBoost"] = Intensity * 0.5f,
                    ["ShieldDrain"] = Intensity * 0.3f
                }
            };
        }
    }
    
    public class RadiationZone : EnvironmentalHazard
    {
        private float _radiationLevel;
        private List<Vector3> _radiationParticles;
        private float _particleTimer;
        
        public RadiationZone(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.RadiationZone;
            
            EffectRadius = 80f + intensity * 160f;
            DetectionRadius = EffectRadius * 1.3f;
            RenderDistance = DetectionRadius * 1.5f;
            LethalRadius = EffectRadius * 0.4f;
            IsLethal = intensity > 0.8f;
            
            _maxLifetime = 120f + intensity * 180f;
            _radiationLevel = intensity;
            _radiationParticles = new List<Vector3>();
            
            GenerateRadiationParticles();
        }
        
        private void GenerateRadiationParticles()
        {
            int particleCount = (int)(30 + Intensity * 100);
            
            for (int i = 0; i < particleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius
                );
                _radiationParticles.Add(Position + offset);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            _particleTimer += deltaTime;
            
            // Drift particles
            for (int i = 0; i < _radiationParticles.Count; i++)
            {
                var drift = new Vector3(
                    (_random.NextSingle() - 0.5f) * deltaTime * 5f,
                    (_random.NextSingle() - 0.5f) * deltaTime * 5f,
                    (_random.NextSingle() - 0.5f) * deltaTime * 5f
                );
                _radiationParticles[i] += drift;
            }
            
            // Add new particles
            if (_particleTimer >= 1f)
            {
                AddRadiationParticle();
                _particleTimer = 0f;
            }
            
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        private void AddRadiationParticle()
        {
            var offset = new Vector3(
                (_random.NextSingle() - 0.5f) * EffectRadius,
                (_random.NextSingle() - 0.5f) * EffectRadius,
                (_random.NextSingle() - 0.5f) * EffectRadius
            );
            _radiationParticles.Add(Position + offset);
            
            if (_radiationParticles.Count > 200)
            {
                _radiationParticles.RemoveAt(0);
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var radiationColor = new Color(255, 255, 0, (byte)(100 * _radiationLevel));
            
            // Radiation zone boundary
            if (lodLevel >= LODLevel.Medium)
            {
                Raylib.DrawSphereWires(Position, EffectRadius, 20, 20, 
                    new Color(255, 0, 0, 50));
                
                if (IsLethal)
                {
                    Raylib.DrawSphereWires(Position, LethalRadius, 12, 12, 
                        new Color(255, 0, 0, 100));
                }
            }
            
            // Radiation particles
            foreach (var particle in _radiationParticles)
            {
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawSphere(particle, 0.3f, radiationColor);
                }
                else
                {
                    Raylib.DrawPoint3D(particle, radiationColor);
                }
            }
            
            // Central radiation source
            var coreColor = new Color(255, 150, 0, 200);
            Raylib.DrawSphere(Position, 2f, coreColor);
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.RadiationZoneEntered,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["RadiationLevel"] = _radiationLevel * 10f,
                    ["HullDamageRate"] = _radiationLevel * 0.1f,
                    ["IsLethal"] = IsLethal && distance <= LethalRadius
                }
            };
        }
    }
    
    public class GravityWell : EnvironmentalHazard
    {
        private float _gravityStrength;
        private List<Vector3> _orbitingDebris;
        private List<float> _orbitSpeeds;
        
        public GravityWell(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.GravityWell;
            
            EffectRadius = 75f + intensity * 150f;
            DetectionRadius = EffectRadius * 1.5f;
            RenderDistance = DetectionRadius * 2f;
            LethalRadius = EffectRadius * 0.2f;
            IsLethal = false;
            
            _maxLifetime = float.PositiveInfinity; // Gravity wells are permanent
            _gravityStrength = intensity * 20f;
            _orbitingDebris = new List<Vector3>();
            _orbitSpeeds = new List<float>();
            
            GenerateOrbitingDebris();
        }
        
        private void GenerateOrbitingDebris()
        {
            int debrisCount = (int)(10 + Intensity * 30);
            
            for (int i = 0; i < debrisCount; i++)
            {
                var angle = _random.NextSingle() * MathF.PI * 2f;
                var radius = _random.NextSingle() * EffectRadius + LethalRadius;
                var height = (_random.NextSingle() - 0.5f) * radius * 0.1f;
                
                _orbitingDebris.Add(Position + new Vector3(
                    MathF.Cos(angle) * radius,
                    height,
                    MathF.Sin(angle) * radius
                ));
                
                // Orbital speed inversely proportional to distance
                _orbitSpeeds.Add(1f / radius * _gravityStrength);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            
            // Update orbiting debris
            for (int i = 0; i < _orbitingDebris.Count; i++)
            {
                var relative = _orbitingDebris[i] - Position;
                var distance = new Vector2(relative.X, relative.Z).Length();
                var currentAngle = MathF.Atan2(relative.Z, relative.X);
                var newAngle = currentAngle + _orbitSpeeds[i] * deltaTime;
                
                _orbitingDebris[i] = Position + new Vector3(
                    MathF.Cos(newAngle) * distance,
                    relative.Y,
                    MathF.Sin(newAngle) * distance
                );
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var gravityColor = new Color(100, 0, 200, 100);
            
            // Gravity well visualization
            if (lodLevel >= LODLevel.Medium)
            {
                for (int i = 1; i <= 4; i++)
                {
                    var ringRadius = EffectRadius * i * 0.25f;
                    Raylib.DrawSphereWires(Position, ringRadius, 12, 12, 
                        new Color(gravityColor.R, gravityColor.G, gravityColor.B, gravityColor.A / i));
                }
            }
            
            // Orbiting debris
            foreach (var debris in _orbitingDebris)
            {
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawCube(debris, 1f, 1f, 1f, new Color(100, 100, 100, 200));
                }
                else
                {
                    Raylib.DrawPoint3D(debris, new Color(100, 100, 100, 200));
                }
            }
            
            // Central mass
            Raylib.DrawSphere(Position, 3f, new Color(50, 0, 100, 255));
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.GravityWellEntered,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = float.PositiveInfinity,
                Parameters = new Dictionary<string, object>
                {
                    ["GravityStrength"] = _gravityStrength * (EffectRadius / Math.Max(distance, 1f)),
                    ["PullDirection"] = Vector3.Normalize(Position - playerPosition),
                    ["OrbitVelocity"] = _gravityStrength / Math.Max(distance, 1f)
                }
            };
        }
    }
    
    public class SingularityRift : EnvironmentalHazard
    {
        private Vector3 _riftDirection;
        private float _riftLength;
        private List<Vector3> _riftParticles;
        private float _energyFluctuation;
        
        public SingularityRift(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.SingularityRift;
            
            EffectRadius = 30f + intensity * 60f;
            DetectionRadius = EffectRadius * 2f;
            RenderDistance = DetectionRadius * 3f;
            LethalRadius = EffectRadius * 0.5f;
            IsLethal = true;
            
            _maxLifetime = 30f + intensity * 60f;
            _riftDirection = Vector3.Normalize(new Vector3(
                _random.NextSingle() - 0.5f,
                _random.NextSingle() - 0.5f,
                _random.NextSingle() - 0.5f
            ));
            _riftLength = EffectRadius * 2f;
            _riftParticles = new List<Vector3>();
            
            GenerateRiftParticles();
        }
        
        private void GenerateRiftParticles()
        {
            int particleCount = (int)(20 + Intensity * 50);
            
            for (int i = 0; i < particleCount; i++)
            {
                var alongRift = _random.NextSingle() * _riftLength - _riftLength * 0.5f;
                var perpendicular = new Vector3(
                    (_random.NextSingle() - 0.5f) * EffectRadius * 0.2f,
                    (_random.NextSingle() - 0.5f) * EffectRadius * 0.2f,
                    (_random.NextSingle() - 0.5f) * EffectRadius * 0.2f
                );
                
                _riftParticles.Add(Position + _riftDirection * alongRift + perpendicular);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            _energyFluctuation += deltaTime * 5f;
            
            // Chaotic particle movement
            for (int i = 0; i < _riftParticles.Count; i++)
            {
                var chaos = new Vector3(
                    MathF.Sin(_energyFluctuation + i) * deltaTime * 10f,
                    MathF.Cos(_energyFluctuation + i * 1.3f) * deltaTime * 10f,
                    MathF.Sin(_energyFluctuation + i * 0.7f) * deltaTime * 10f
                );
                _riftParticles[i] += chaos;
            }
            
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var riftColor = new Color(255, 0, 255, 200);
            var fluctuation = MathF.Sin(_energyFluctuation) * 0.3f + 0.7f;
            var currentColor = new Color(
                (byte)(riftColor.R * fluctuation),
                (byte)(riftColor.G * fluctuation),
                (byte)(riftColor.B * fluctuation),
                riftColor.A
            );
            
            // Rift line
            var riftStart = Position - _riftDirection * _riftLength * 0.5f;
            var riftEnd = Position + _riftDirection * _riftLength * 0.5f;
            Raylib.DrawLine3D(riftStart, riftEnd, currentColor);
            
            // Rift particles
            foreach (var particle in _riftParticles)
            {
                if (lodLevel >= LODLevel.High)
                {
                    var size = 0.5f + MathF.Sin(_energyFluctuation) * 0.3f;
                    Raylib.DrawSphere(particle, size, currentColor);
                }
                else
                {
                    Raylib.DrawPoint3D(particle, currentColor);
                }
            }
            
            // Distortion effect
            if (lodLevel >= LODLevel.High)
            {
                Raylib.DrawSphereWires(Position, EffectRadius, 8, 8, 
                    new Color(currentColor.R, currentColor.G, currentColor.B, 50));
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.WormholeDetected,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["RiftDirection"] = _riftDirection,
                    ["SpaceTimeDistortion"] = Intensity * 2f,
                    ["IsUnstable"] = true,
                    ["EnergyLevel"] = fluctuation
                }
            };
        }
        
        private float fluctuation => MathF.Sin(_energyFluctuation) * 0.3f + 0.7f;
    }
    
    public class TemporalAnomaly : EnvironmentalHazard
    {
        private float _timeDistortion;
        private List<Vector3> _chronoParticles;
        private List<float> _particleAges;
        
        public TemporalAnomaly(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.TemporalAnomaly;
            
            EffectRadius = 40f + intensity * 80f;
            DetectionRadius = EffectRadius * 1.5f;
            RenderDistance = DetectionRadius * 2f;
            LethalRadius = 0f;
            IsLethal = false;
            
            _maxLifetime = 60f + intensity * 120f;
            _timeDistortion = intensity;
            _chronoParticles = new List<Vector3>();
            _particleAges = new List<float>();
            
            GenerateChronoParticles();
        }
        
        private void GenerateChronoParticles()
        {
            int particleCount = (int)(15 + Intensity * 35);
            
            for (int i = 0; i < particleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius
                );
                _chronoParticles.Add(Position + offset);
                _particleAges.Add(_random.NextSingle() * 10f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            
            // Update chrono particles with time distortion
            for (int i = 0; i < _particleAges.Count; i++)
            {
                var distortedTime = deltaTime * (1f + _timeDistortion * MathF.Sin(_lifetime + i));
                _particleAges[i] += distortedTime;
                
                // Particles move in temporal spirals
                var age = _particleAges[i];
                var spiral = new Vector3(
                    MathF.Cos(age) * 2f,
                    MathF.Sin(age * 2f) * 1f,
                    MathF.Sin(age) * 2f
                );
                _chronoParticles[i] = Position + spiral;
            }
            
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var temporalColor = new Color(100, 255, 200, 150);
            
            // Temporal field visualization
            if (lodLevel >= LODLevel.Medium)
            {
                var fieldAlpha = (byte)(50 + MathF.Sin(_lifetime * 2f) * 30);
                Raylib.DrawSphereWires(Position, EffectRadius, 12, 12, 
                    new Color(temporalColor.R, temporalColor.G, temporalColor.B, fieldAlpha));
            }
            
            // Chrono particles
            for (int i = 0; i < _chronoParticles.Count; i++)
            {
                var age = _particleAges[i];
                var alpha = (byte)(100 + MathF.Sin(age) * 100);
                var particleColor = new Color(temporalColor.R, temporalColor.G, temporalColor.B, alpha);
                
                if (lodLevel >= LODLevel.High)
                {
                    var size = 1f + MathF.Sin(age * 3f) * 0.5f;
                    Raylib.DrawSphere(_chronoParticles[i], size, particleColor);
                    
                    // Temporal trail
                    var trail = _chronoParticles[i] - new Vector3(
                        MathF.Cos(age - 0.1f) * 2f,
                        MathF.Sin((age - 0.1f) * 2f) * 1f,
                        MathF.Sin(age - 0.1f) * 2f
                    );
                    Raylib.DrawLine3D(_chronoParticles[i], Position + trail, 
                        new Color(particleColor.R, particleColor.G, particleColor.B, 50));
                }
                else
                {
                    Raylib.DrawPoint3D(_chronoParticles[i], particleColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.WormholeDetected, // Closest matching type
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius),
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["TimeDistortion"] = _timeDistortion,
                    ["TimeMultiplier"] = 1f + _timeDistortion * MathF.Sin(_lifetime),
                    ["ChronoField"] = true
                }
            };
        }
    }
    
    public class DarkMatterCloud : EnvironmentalHazard
    {
        private List<Vector3> _darkMatterParticles;
        private List<float> _particleIntensities;
        private float _cloudDensity;
        
        public DarkMatterCloud(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Type = HazardType.DarkMatterCloud;
            
            EffectRadius = 100f + intensity * 200f;
            DetectionRadius = EffectRadius * 1.2f;
            RenderDistance = DetectionRadius * 1.5f;
            LethalRadius = 0f;
            IsLethal = false;
            
            _maxLifetime = 180f + intensity * 300f;
            _cloudDensity = intensity;
            _darkMatterParticles = new List<Vector3>();
            _particleIntensities = new List<float>();
            
            GenerateDarkMatterParticles();
        }
        
        private void GenerateDarkMatterParticles()
        {
            int particleCount = (int)(50 + Intensity * 150);
            
            for (int i = 0; i < particleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius,
                    (_random.NextSingle() - 0.5f) * EffectRadius
                );
                _darkMatterParticles.Add(Position + offset);
                _particleIntensities.Add(_random.NextSingle() * _cloudDensity);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _lifetime += deltaTime;
            
            // Dark matter particles move slowly and unpredictably
            for (int i = 0; i < _darkMatterParticles.Count; i++)
            {
                var drift = new Vector3(
                    MathF.Sin(_lifetime + i) * deltaTime * 0.5f,
                    MathF.Cos(_lifetime + i * 1.3f) * deltaTime * 0.5f,
                    MathF.Sin(_lifetime + i * 0.7f) * deltaTime * 0.5f
                );
                _darkMatterParticles[i] += drift;
                
                // Fluctuate particle intensity
                _particleIntensities[i] += (_random.NextSingle() - 0.5f) * deltaTime * 0.1f;
                _particleIntensities[i] = Math.Clamp(_particleIntensities[i], 0f, 1f);
            }
            
            if (_lifetime >= _maxLifetime)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            // Dark matter is barely visible
            var darkMatterColor = new Color(50, 0, 100, 30);
            
            // Cloud boundary (very faint)
            Raylib.DrawSphereWires(Position, EffectRadius, 8, 8, 
                new Color(darkMatterColor.R, darkMatterColor.G, darkMatterColor.B, 20));
            
            // Dark matter particles (subtle)
            for (int i = 0; i < _darkMatterParticles.Count; i++)
            {
                var intensity = _particleIntensities[i];
                var alpha = (byte)(darkMatterColor.A * intensity);
                var particleColor = new Color(darkMatterColor.R, darkMatterColor.G, darkMatterColor.B, alpha);
                
                if (lodLevel >= LODLevel.High && intensity > 0.3f)
                {
                    Raylib.DrawSphere(_darkMatterParticles[i], 0.5f, particleColor);
                }
                else if (intensity > 0.5f)
                {
                    Raylib.DrawPoint3D(_darkMatterParticles[i], particleColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.GravityWellEntered,
                Position = Position,
                Radius = EffectRadius,
                Intensity = Intensity * (1f - distance / EffectRadius) * 0.3f, // Weak effect
                Duration = _maxLifetime - _lifetime,
                Parameters = new Dictionary<string, object>
                {
                    ["DarkMatterDensity"] = _cloudDensity,
                    ["GravitationalLensing"] = _cloudDensity * 0.1f,
                    ["NavigationInterference"] = _cloudDensity * 0.2f
                }
            };
        }
    }
}