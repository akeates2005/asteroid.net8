using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Manages dynamic space weather systems including storms, electromagnetic interference,
    /// solar winds, and cosmic phenomena that affect gameplay
    /// </summary>
    public class WeatherManager : IEnvironmentSystem
    {
        private readonly WeatherSettings _settings;
        private readonly List<WeatherSystem> _activeSystems;
        private readonly Random _random;
        
        private float _weatherTimer;
        private float _intensityModifier = 1.0f;
        private LODLevel _currentLOD = LODLevel.High;
        private bool _isInitialized;
        
        // Current weather state
        private WeatherConditions _currentConditions;
        private List<WeatherEvent> _pendingEvents;
        
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public int ElementCount => GetTotalWeatherElements();
        public string SystemName => "Weather";
        
        public event Action<PerformanceImpact> OnPerformanceImpact;
        public event Action<WeatherEvent> OnWeatherEvent;
        
        public WeatherManager(WeatherSettings settings)
        {
            _settings = settings ?? WeatherSettings.CreateDefault();
            _activeSystems = new List<WeatherSystem>();
            _pendingEvents = new List<WeatherEvent>();
            _random = new Random();
        }
        
        public void Initialize()
        {
            try
            {
                _currentConditions = new WeatherConditions
                {
                    Temperature = 2.7f, // Cosmic background temperature
                    EMField = 0.1f,
                    SolarWindIntensity = 0.3f,
                    CosmicRayLevel = 0.2f,
                    SpaceDustDensity = 0.1f
                };
                
                _isInitialized = true;
                ErrorManager.LogInfo("Weather Manager initialized successfully");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize Weather Manager", ex);
                throw;
            }
        }
        
        public void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            _weatherTimer += deltaTime;
            
            // Update existing weather systems
            UpdateWeatherSystems(deltaTime, camera, playerPosition);
            
            // Process weather changes
            ProcessWeatherChanges(deltaTime);
            
            // Spawn new weather events
            if (_weatherTimer >= GetWeatherSpawnInterval())
            {
                TrySpawnWeatherEvent(playerPosition);
                _weatherTimer = 0f;
            }
            
            // Update global conditions
            UpdateGlobalConditions(deltaTime);
            
            // Process pending events
            ProcessPendingEvents();
            
            var updateTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(updateTime, 0f);
        }
        
        private void UpdateWeatherSystems(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            for (int i = _activeSystems.Count - 1; i >= 0; i--)
            {
                var system = _activeSystems[i];
                system.Update(deltaTime, camera, playerPosition);
                
                if (system.IsExpired)
                {
                    _activeSystems.RemoveAt(i);
                }
            }
        }
        
        private void ProcessWeatherChanges(float deltaTime)
        {
            var changeRate = _settings.WeatherChangeRate * deltaTime;
            
            // Gradual weather transitions
            _currentConditions.EMField += (_random.NextSingle() - 0.5f) * changeRate;
            _currentConditions.SolarWindIntensity += (_random.NextSingle() - 0.5f) * changeRate;
            _currentConditions.CosmicRayLevel += (_random.NextSingle() - 0.5f) * changeRate;
            _currentConditions.SpaceDustDensity += (_random.NextSingle() - 0.5f) * changeRate;
            
            // Clamp values
            _currentConditions = ClampWeatherConditions(_currentConditions);
        }
        
        private WeatherConditions ClampWeatherConditions(WeatherConditions conditions)
        {
            return new WeatherConditions
            {
                Temperature = Math.Clamp(conditions.Temperature, 1f, 10f),
                EMField = Math.Clamp(conditions.EMField, 0f, 2f),
                SolarWindIntensity = Math.Clamp(conditions.SolarWindIntensity, 0f, 2f),
                CosmicRayLevel = Math.Clamp(conditions.CosmicRayLevel, 0f, 2f),
                SpaceDustDensity = Math.Clamp(conditions.SpaceDustDensity, 0f, 1f)
            };
        }
        
        private float GetWeatherSpawnInterval()
        {
            return 10f / _settings.WeatherChangeRate; // Base 10 seconds, modified by change rate
        }
        
        private void TrySpawnWeatherEvent(Vector3 playerPosition)
        {
            if (_activeSystems.Count >= _settings.MaxSimultaneousWeather) return;
            
            var eventChance = _random.NextSingle();
            var weatherType = DetermineWeatherType(eventChance);
            
            if (weatherType != WeatherType.None)
            {
                SpawnWeatherEvent(weatherType, playerPosition);
            }
        }
        
        private WeatherType DetermineWeatherType(float chance)
        {
            // Probabilities based on current conditions and settings
            if (!_settings.EnableSpaceStorms && !_settings.EnableEMInterference && 
                !_settings.EnableSpaceDust && !_settings.EnableSolarWind)
                return WeatherType.None;
            
            if (chance < 0.1f && _settings.EnableSpaceStorms)
                return WeatherType.ElectromagneticStorm;
            if (chance < 0.3f && _settings.EnableSolarWind)
                return WeatherType.SolarWindBurst;
            if (chance < 0.5f && _settings.EnableSpaceDust)
                return WeatherType.SpaceDustCloud;
            if (chance < 0.7f && _settings.EnableEMInterference)
                return WeatherType.CosmicRayBurst;
            if (chance < 0.85f && _settings.EnableSpaceStorms)
                return WeatherType.IonStorm;
            if (chance < 0.95f && _settings.EnableSpaceDust)
                return WeatherType.MicroMeteorShower;
            
            return WeatherType.None;
        }
        
        private void SpawnWeatherEvent(WeatherType type, Vector3 playerPosition)
        {
            var position = GenerateWeatherPosition(playerPosition);
            var system = CreateWeatherSystem(type, position);
            
            if (system != null)
            {
                _activeSystems.Add(system);
                
                // Create weather event for gameplay systems
                var weatherEvent = new WeatherEvent
                {
                    Type = type,
                    Position = position,
                    Intensity = system.Intensity,
                    Duration = system.Duration,
                    Radius = system.Radius,
                    StartTime = DateTime.UtcNow
                };
                
                _pendingEvents.Add(weatherEvent);
                OnWeatherEvent?.Invoke(weatherEvent);
            }
        }
        
        private Vector3 GenerateWeatherPosition(Vector3 playerPosition)
        {
            // Generate position within reasonable distance of player
            var distance = _random.NextSingle() * 300f + 100f;
            var angle = _random.NextSingle() * MathF.PI * 2f;
            var elevation = (_random.NextSingle() - 0.5f) * MathF.PI * 0.5f;
            
            return playerPosition + new Vector3(
                distance * MathF.Cos(elevation) * MathF.Cos(angle),
                distance * MathF.Sin(elevation),
                distance * MathF.Cos(elevation) * MathF.Sin(angle)
            );
        }
        
        private WeatherSystem CreateWeatherSystem(WeatherType type, Vector3 position)
        {
            return type switch
            {
                WeatherType.ElectromagneticStorm => new ElectromagneticStorm(position, _settings.BaseIntensity),
                WeatherType.SolarWindBurst => new SolarWindBurst(position, _settings.BaseIntensity),
                WeatherType.SpaceDustCloud => new SpaceDustCloud(position, _settings.BaseIntensity),
                WeatherType.CosmicRayBurst => new CosmicRayBurst(position, _settings.BaseIntensity),
                WeatherType.IonStorm => new IonStorm(position, _settings.BaseIntensity),
                WeatherType.MicroMeteorShower => new MicroMeteorShower(position, _settings.BaseIntensity),
                WeatherType.GravitationalAnomaly => new GravitationalAnomaly(position, _settings.BaseIntensity),
                WeatherType.QuantumFluctuation => new QuantumFluctuation(position, _settings.BaseIntensity),
                _ => null
            };
        }
        
        private void UpdateGlobalConditions(float deltaTime)
        {
            // Apply weather system effects to global conditions
            foreach (var system in _activeSystems)
            {
                ApplySystemEffects(system, deltaTime);
            }
        }
        
        private void ApplySystemEffects(WeatherSystem system, float deltaTime)
        {
            var effectStrength = system.Intensity * deltaTime * 0.1f;
            
            switch (system.Type)
            {
                case WeatherType.ElectromagneticStorm:
                    _currentConditions.EMField += effectStrength;
                    break;
                case WeatherType.SolarWindBurst:
                    _currentConditions.SolarWindIntensity += effectStrength;
                    break;
                case WeatherType.SpaceDustCloud:
                    _currentConditions.SpaceDustDensity += effectStrength;
                    break;
                case WeatherType.CosmicRayBurst:
                    _currentConditions.CosmicRayLevel += effectStrength;
                    break;
            }
        }
        
        private void ProcessPendingEvents()
        {
            for (int i = _pendingEvents.Count - 1; i >= 0; i--)
            {
                var weatherEvent = _pendingEvents[i];
                var elapsed = (DateTime.UtcNow - weatherEvent.StartTime).TotalSeconds;
                
                if (elapsed >= weatherEvent.Duration)
                {
                    _pendingEvents.RemoveAt(i);
                }
            }
        }
        
        public void Render(Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !IsVisible || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            // Render all active weather systems
            foreach (var system in _activeSystems)
            {
                if (ShouldRenderSystem(system, camera))
                {
                    system.Render(camera, _currentLOD);
                }
            }
            
            // Render global atmospheric effects
            RenderGlobalEffects(camera, playerPosition);
            
            var renderTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(0f, renderTime);
        }
        
        private bool ShouldRenderSystem(WeatherSystem system, Camera3D camera)
        {
            var distance = Vector3.Distance(system.Position, camera.Position);
            return distance <= system.RenderDistance;
        }
        
        private void RenderGlobalEffects(Camera3D camera, Vector3 playerPosition)
        {
            // Render background space dust
            if (_currentConditions.SpaceDustDensity > 0.1f && _currentLOD >= LODLevel.Medium)
            {
                RenderSpaceDust(camera, playerPosition);
            }
            
            // Render EM field visualization
            if (_currentConditions.EMField > 0.5f && _currentLOD >= LODLevel.High)
            {
                RenderEMFieldEffect(camera, playerPosition);
            }
        }
        
        private void RenderSpaceDust(Camera3D camera, Vector3 playerPosition)
        {
            var dustCount = (int)(_currentConditions.SpaceDustDensity * 100f);
            var dustColor = new Color(200, 200, 200, (byte)(30 * _currentConditions.SpaceDustDensity));
            
            for (int i = 0; i < dustCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * 1000f,
                    (_random.NextSingle() - 0.5f) * 1000f,
                    (_random.NextSingle() - 0.5f) * 1000f
                );
                
                var position = playerPosition + offset;
                Raylib.DrawPoint3D(position, dustColor);
            }
        }
        
        private void RenderEMFieldEffect(Camera3D camera, Vector3 playerPosition)
        {
            var effectColor = new Color(100, 100, 255, (byte)(50 * _currentConditions.EMField));
            var radius = 200f * _currentConditions.EMField;
            
            // Draw electromagnetic field lines
            for (int i = 0; i < 8; i++)
            {
                var angle = (float)i / 8f * MathF.PI * 2f;
                var start = playerPosition + new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
                var end = playerPosition + new Vector3(MathF.Cos(angle) * radius * 0.5f, radius * 0.3f, MathF.Sin(angle) * radius * 0.5f);
                
                Raylib.DrawLine3D(start, end, effectColor);
            }
        }
        
        public void CreateEnergyStorm(Vector3 position, float intensity)
        {
            var storm = new ElectromagneticStorm(position, intensity);
            _activeSystems.Add(storm);
            
            var weatherEvent = new WeatherEvent
            {
                Type = WeatherType.ElectromagneticStorm,
                Position = position,
                Intensity = intensity,
                Duration = storm.Duration,
                Radius = storm.Radius,
                StartTime = DateTime.UtcNow
            };
            
            _pendingEvents.Add(weatherEvent);
            OnWeatherEvent?.Invoke(weatherEvent);
        }
        
        public void EnableSpaceDust(bool enable)
        {
            _settings.EnableSpaceDust = enable;
        }
        
        public WeatherConditions GetCurrentConditions()
        {
            return _currentConditions;
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
                    _settings.EnableSpaceStorms = false;
                    _settings.EnableEMInterference = false;
                    _settings.EnableSpaceDust = false;
                    _settings.EnableSolarWind = false;
                    break;
                case EnvironmentQuality.Low:
                    _settings.EnableSpaceStorms = true;
                    _settings.EnableEMInterference = false;
                    _settings.EnableSpaceDust = false;
                    _settings.EnableSolarWind = false;
                    break;
                case EnvironmentQuality.Medium:
                    _settings.EnableSpaceStorms = true;
                    _settings.EnableEMInterference = true;
                    _settings.EnableSpaceDust = true;
                    _settings.EnableSolarWind = false;
                    break;
                case EnvironmentQuality.High:
                case EnvironmentQuality.Ultra:
                case EnvironmentQuality.Extreme:
                    _settings.EnableSpaceStorms = true;
                    _settings.EnableEMInterference = true;
                    _settings.EnableSpaceDust = true;
                    _settings.EnableSolarWind = true;
                    break;
            }
        }
        
        private int GetTotalWeatherElements()
        {
            int total = 0;
            foreach (var system in _activeSystems)
            {
                total += system.ParticleCount;
            }
            return total;
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
            return (_activeSystems.Count * 512f + _pendingEvents.Count * 128f) / (1024f * 1024f);
        }
        
        private float CalculateSeverity(float updateTime, float renderTime)
        {
            var totalTime = updateTime + renderTime;
            if (totalTime < 2f) return 0f;
            if (totalTime < 5f) return 0.4f;
            if (totalTime < 8f) return 0.7f;
            return 1f;
        }
        
        public SystemStatistics GetStatistics()
        {
            return new SystemStatistics
            {
                SystemName = SystemName,
                IsActive = IsActive,
                ElementCount = ElementCount,
                UpdateTime = 0f, // Updated in ReportPerformanceImpact
                RenderTime = 0f, // Updated in ReportPerformanceImpact
                MemoryUsage = EstimateMemoryUsage(),
                CurrentLOD = _currentLOD,
                Quality = EnvironmentQuality.High, // Based on current settings
                CustomData = new Dictionary<string, object>
                {
                    ["ActiveSystems"] = _activeSystems.Count,
                    ["PendingEvents"] = _pendingEvents.Count,
                    ["EMField"] = _currentConditions.EMField,
                    ["SolarWind"] = _currentConditions.SolarWindIntensity,
                    ["SpaceDust"] = _currentConditions.SpaceDustDensity
                }
            };
        }
        
        public List<EnvironmentEvent> GetEnvironmentalEvents(Vector3 playerPosition)
        {
            var events = new List<EnvironmentEvent>();
            
            foreach (var system in _activeSystems)
            {
                var distance = Vector3.Distance(system.Position, playerPosition);
                if (distance <= system.Radius)
                {
                    var environmentEvent = system.GetEnvironmentEvent(playerPosition, distance);
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
            _activeSystems?.Clear();
            _pendingEvents?.Clear();
        }
    }
    
    // Supporting data structures and weather system implementations
    public struct WeatherConditions
    {
        public float Temperature;           // Kelvin
        public float EMField;              // Electromagnetic field strength
        public float SolarWindIntensity;   // Solar wind particle density
        public float CosmicRayLevel;       // Cosmic radiation level
        public float SpaceDustDensity;     // Space dust particle density
    }
    
    public struct WeatherEvent
    {
        public WeatherType Type;
        public Vector3 Position;
        public float Intensity;
        public float Duration;
        public float Radius;
        public DateTime StartTime;
    }
    
    public enum WeatherType
    {
        None,
        ElectromagneticStorm,
        SolarWindBurst,
        SpaceDustCloud,
        CosmicRayBurst,
        IonStorm,
        MicroMeteorShower,
        GravitationalAnomaly,
        QuantumFluctuation
    }
    
    public abstract class WeatherSystem
    {
        public Vector3 Position { get; protected set; }
        public float Intensity { get; protected set; }
        public float Duration { get; protected set; }
        public float Radius { get; protected set; }
        public float RenderDistance { get; protected set; }
        public WeatherType Type { get; protected set; }
        public int ParticleCount { get; protected set; }
        public bool IsExpired { get; protected set; }
        
        protected float _timer;
        protected Random _random = new Random();
        
        public abstract void Update(float deltaTime, Camera3D camera, Vector3 playerPosition);
        public abstract void Render(Camera3D camera, LODLevel lodLevel);
        public abstract EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance);
    }
    
    // Specific weather system implementations
    public class ElectromagneticStorm : WeatherSystem
    {
        private List<Vector3> _lightningBolts;
        private float _boltTimer;
        
        public ElectromagneticStorm(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 30f + intensity * 20f;
            Radius = 50f + intensity * 100f;
            RenderDistance = Radius * 2f;
            Type = WeatherType.ElectromagneticStorm;
            ParticleCount = (int)(20 + intensity * 50);
            _lightningBolts = new List<Vector3>();
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            _boltTimer += deltaTime;
            
            if (_boltTimer >= 0.5f / Intensity)
            {
                GenerateLightningBolt();
                _boltTimer = 0f;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        private void GenerateLightningBolt()
        {
            var offset = new Vector3(
                (_random.NextSingle() - 0.5f) * Radius,
                (_random.NextSingle() - 0.5f) * Radius,
                (_random.NextSingle() - 0.5f) * Radius
            );
            
            _lightningBolts.Add(Position + offset);
            
            // Remove old bolts
            if (_lightningBolts.Count > 10)
            {
                _lightningBolts.RemoveAt(0);
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var stormColor = new Color(100, 100, 255, 150);
            
            // Render storm center
            Raylib.DrawSphere(Position, Radius * 0.3f, new Color(stormColor.R, stormColor.G, stormColor.B, 50));
            
            // Render lightning bolts
            foreach (var bolt in _lightningBolts)
            {
                Raylib.DrawLine3D(Position, bolt, Color.White);
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawSphere(bolt, 2f, stormColor);
                }
            }
            
            // Render storm boundary
            if (lodLevel >= LODLevel.Medium)
            {
                Raylib.DrawSphereWires(Position, Radius, 12, 12, new Color(stormColor.R, stormColor.G, stormColor.B, 30));
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.ElectromagneticStorm,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["EMFieldStrength"] = Intensity * 2f,
                    ["LightningFrequency"] = Intensity * 10f
                }
            };
        }
    }
    
    public class SolarWindBurst : WeatherSystem
    {
        private Vector3 _direction;
        private List<Vector3> _particles;
        
        public SolarWindBurst(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 15f + intensity * 10f;
            Radius = 100f + intensity * 200f;
            RenderDistance = Radius * 1.5f;
            Type = WeatherType.SolarWindBurst;
            ParticleCount = (int)(50 + intensity * 100);
            
            _direction = Vector3.Normalize(new Vector3(_random.NextSingle() - 0.5f, 0, _random.NextSingle() - 0.5f));
            _particles = new List<Vector3>();
            GenerateParticles();
        }
        
        private void GenerateParticles()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius * 0.2f,
                    (_random.NextSingle() - 0.5f) * Radius
                );
                _particles.Add(Position + offset);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            // Move particles
            var velocity = _direction * Intensity * 50f * deltaTime;
            for (int i = 0; i < _particles.Count; i++)
            {
                _particles[i] += velocity;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var windColor = new Color(255, 200, 100, 100);
            
            foreach (var particle in _particles)
            {
                if (lodLevel >= LODLevel.Medium)
                {
                    var tail = particle - _direction * 10f;
                    Raylib.DrawLine3D(particle, tail, windColor);
                }
                else
                {
                    Raylib.DrawPoint3D(particle, windColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.SolarFlareDetected,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["WindDirection"] = _direction,
                    ["ParticleVelocity"] = Intensity * 50f
                }
            };
        }
    }
    
    public class SpaceDustCloud : WeatherSystem
    {
        private List<Vector3> _dustParticles;
        private List<float> _particleSizes;
        
        public SpaceDustCloud(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 60f + intensity * 40f;
            Radius = 75f + intensity * 150f;
            RenderDistance = Radius * 1.2f;
            Type = WeatherType.SpaceDustCloud;
            ParticleCount = (int)(100 + intensity * 200);
            
            _dustParticles = new List<Vector3>();
            _particleSizes = new List<float>();
            GenerateDustParticles();
        }
        
        private void GenerateDustParticles()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius
                );
                _dustParticles.Add(Position + offset);
                _particleSizes.Add(_random.NextSingle() * 0.5f + 0.1f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            // Gentle drift
            var drift = new Vector3(
                MathF.Sin(_timer * 0.1f) * deltaTime,
                MathF.Cos(_timer * 0.15f) * deltaTime,
                MathF.Sin(_timer * 0.08f) * deltaTime
            );
            
            for (int i = 0; i < _dustParticles.Count; i++)
            {
                _dustParticles[i] += drift;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var dustColor = new Color(150, 120, 100, 80);
            
            for (int i = 0; i < _dustParticles.Count; i++)
            {
                if (lodLevel >= LODLevel.Medium)
                {
                    Raylib.DrawSphere(_dustParticles[i], _particleSizes[i], dustColor);
                }
                else
                {
                    Raylib.DrawPoint3D(_dustParticles[i], dustColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.SpaceDustCloud,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["DustDensity"] = Intensity,
                    ["VisibilityReduction"] = Intensity * 0.5f
                }
            };
        }
    }
    
    public class CosmicRayBurst : WeatherSystem
    {
        private List<Vector3> _rayDirections;
        private List<float> _rayIntensities;
        
        public CosmicRayBurst(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 5f + intensity * 10f;
            Radius = 30f + intensity * 70f;
            RenderDistance = Radius * 3f;
            Type = WeatherType.CosmicRayBurst;
            ParticleCount = (int)(10 + intensity * 30);
            
            _rayDirections = new List<Vector3>();
            _rayIntensities = new List<float>();
            GenerateRays();
        }
        
        private void GenerateRays()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                var direction = Vector3.Normalize(new Vector3(
                    _random.NextSingle() - 0.5f,
                    _random.NextSingle() - 0.5f,
                    _random.NextSingle() - 0.5f
                ));
                _rayDirections.Add(direction);
                _rayIntensities.Add(_random.NextSingle() * Intensity + 0.5f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var rayColor = new Color(255, 255, 255, 200);
            
            for (int i = 0; i < _rayDirections.Count; i++)
            {
                var rayLength = _rayIntensities[i] * 50f;
                var endPoint = Position + _rayDirections[i] * rayLength;
                
                Raylib.DrawLine3D(Position, endPoint, rayColor);
                
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawSphere(endPoint, 1f, new Color(255, 255, 0, 100));
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.CosmicRayBurst,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["RadiationLevel"] = Intensity * 5f,
                    ["EnergyType"] = "Gamma"
                }
            };
        }
    }
    
    public class IonStorm : WeatherSystem
    {
        private List<Vector3> _ionClouds;
        private List<Color> _ionColors;
        
        public IonStorm(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 45f + intensity * 30f;
            Radius = 80f + intensity * 120f;
            RenderDistance = Radius * 1.5f;
            Type = WeatherType.IonStorm;
            ParticleCount = (int)(30 + intensity * 70);
            
            _ionClouds = new List<Vector3>();
            _ionColors = new List<Color>();
            GenerateIonClouds();
        }
        
        private void GenerateIonClouds()
        {
            var colors = new[]
            {
                new Color(0, 255, 255, 100),   // Cyan
                new Color(255, 0, 255, 100),   // Magenta  
                new Color(255, 255, 0, 100),   // Yellow
                new Color(0, 255, 0, 100)      // Green
            };
            
            for (int i = 0; i < ParticleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius
                );
                _ionClouds.Add(Position + offset);
                _ionColors.Add(colors[_random.Next(colors.Length)]);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            // Swirling motion
            for (int i = 0; i < _ionClouds.Count; i++)
            {
                var angle = _timer * 0.5f + i * 0.1f;
                var swirl = new Vector3(
                    MathF.Cos(angle) * deltaTime,
                    MathF.Sin(angle * 1.3f) * deltaTime,
                    MathF.Sin(angle * 0.7f) * deltaTime
                );
                _ionClouds[i] += swirl;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            for (int i = 0; i < _ionClouds.Count; i++)
            {
                var size = 3f + MathF.Sin(_timer * 2f + i) * 1f;
                
                if (lodLevel >= LODLevel.Medium)
                {
                    Raylib.DrawSphere(_ionClouds[i], size, _ionColors[i]);
                }
                else
                {
                    Raylib.DrawPoint3D(_ionClouds[i], _ionColors[i]);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.ElectromagneticStorm,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["IonDensity"] = Intensity,
                    ["EMInterference"] = Intensity * 3f
                }
            };
        }
    }
    
    public class MicroMeteorShower : WeatherSystem
    {
        private List<Vector3> _meteorPositions;
        private List<Vector3> _meteorVelocities;
        private List<float> _meteorSizes;
        
        public MicroMeteorShower(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 20f + intensity * 15f;
            Radius = 100f + intensity * 200f;
            RenderDistance = Radius * 2f;
            Type = WeatherType.MicroMeteorShower;
            ParticleCount = (int)(20 + intensity * 80);
            
            _meteorPositions = new List<Vector3>();
            _meteorVelocities = new List<Vector3>();
            _meteorSizes = new List<float>();
            GenerateMeteors();
        }
        
        private void GenerateMeteors()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius
                );
                _meteorPositions.Add(Position + offset);
                
                var velocity = Vector3.Normalize(new Vector3(
                    _random.NextSingle() - 0.5f,
                    _random.NextSingle() - 0.5f,
                    _random.NextSingle() - 0.5f
                )) * (20f + _random.NextSingle() * 30f);
                _meteorVelocities.Add(velocity);
                
                _meteorSizes.Add(_random.NextSingle() * 0.5f + 0.1f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            // Move meteors
            for (int i = 0; i < _meteorPositions.Count; i++)
            {
                _meteorPositions[i] += _meteorVelocities[i] * deltaTime;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            var meteorColor = new Color(255, 150, 50, 200);
            
            for (int i = 0; i < _meteorPositions.Count; i++)
            {
                if (lodLevel >= LODLevel.Medium)
                {
                    // Draw meteor with trail
                    var tail = _meteorPositions[i] - _meteorVelocities[i] * 0.1f;
                    Raylib.DrawLine3D(_meteorPositions[i], tail, meteorColor);
                    Raylib.DrawSphere(_meteorPositions[i], _meteorSizes[i], meteorColor);
                }
                else
                {
                    Raylib.DrawPoint3D(_meteorPositions[i], meteorColor);
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.AsteroidStormApproaching,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["MeteorCount"] = ParticleCount,
                    ["ImpactRisk"] = Intensity * 0.3f
                }
            };
        }
    }
    
    public class GravitationalAnomaly : WeatherSystem
    {
        public GravitationalAnomaly(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 90f + intensity * 60f;
            Radius = 50f + intensity * 100f;
            RenderDistance = Radius * 3f;
            Type = WeatherType.GravitationalAnomaly;
            ParticleCount = 0; // No visible particles, just effects
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            // Gravitational lensing effect visualization
            var anomalyColor = new Color(100, 0, 100, 100);
            
            // Draw concentric rings to show gravity wells
            for (int i = 1; i <= 3; i++)
            {
                var ringRadius = Radius * i * 0.3f;
                Raylib.DrawSphereWires(Position, ringRadius, 12, 12, 
                    new Color(anomalyColor.R, anomalyColor.G, anomalyColor.B, anomalyColor.A / i));
            }
            
            // Central singularity
            Raylib.DrawSphere(Position, 2f, Color.Black);
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.GravityWellEntered,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["GravityStrength"] = Intensity * 10f,
                    ["TimeDilation"] = Intensity * 0.1f
                }
            };
        }
    }
    
    public class QuantumFluctuation : WeatherSystem
    {
        private List<Vector3> _fluctuationPoints;
        private List<float> _fluctuationPhases;
        
        public QuantumFluctuation(Vector3 position, float intensity)
        {
            Position = position;
            Intensity = intensity;
            Duration = 10f + intensity * 20f;
            Radius = 20f + intensity * 40f;
            RenderDistance = Radius * 2f;
            Type = WeatherType.QuantumFluctuation;
            ParticleCount = (int)(5 + intensity * 15);
            
            _fluctuationPoints = new List<Vector3>();
            _fluctuationPhases = new List<float>();
            GenerateFluctuations();
        }
        
        private void GenerateFluctuations()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius,
                    (_random.NextSingle() - 0.5f) * Radius
                );
                _fluctuationPoints.Add(Position + offset);
                _fluctuationPhases.Add(_random.NextSingle() * MathF.PI * 2f);
            }
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            _timer += deltaTime;
            
            // Update quantum fluctuation phases
            for (int i = 0; i < _fluctuationPhases.Count; i++)
            {
                _fluctuationPhases[i] += deltaTime * 5f * Intensity;
                if (_fluctuationPhases[i] > MathF.PI * 2f)
                    _fluctuationPhases[i] -= MathF.PI * 2f;
            }
            
            if (_timer >= Duration)
            {
                IsExpired = true;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.High) return;
            
            for (int i = 0; i < _fluctuationPoints.Count; i++)
            {
                var phase = _fluctuationPhases[i];
                var alpha = (byte)(128 + MathF.Sin(phase) * 127);
                var size = 1f + MathF.Cos(phase) * 0.5f;
                
                var fluctColor = new Color(255, 0, 255, alpha);
                Raylib.DrawSphere(_fluctuationPoints[i], size, fluctColor);
                
                // Quantum uncertainty visualization
                if (MathF.Sin(phase) > 0.8f)
                {
                    var uncertainty = new Vector3(
                        (_random.NextSingle() - 0.5f) * 5f,
                        (_random.NextSingle() - 0.5f) * 5f,
                        (_random.NextSingle() - 0.5f) * 5f
                    );
                    Raylib.DrawSphere(_fluctuationPoints[i] + uncertainty, 0.5f, 
                        new Color(0, 255, 255, 100));
                }
            }
        }
        
        public override EnvironmentEvent? GetEnvironmentEvent(Vector3 playerPosition, float distance)
        {
            return new EnvironmentEvent
            {
                Type = EnvironmentEventType.EnergyFieldEntered,
                Position = Position,
                Radius = Radius,
                Intensity = Intensity * (1f - distance / Radius),
                Duration = Duration - _timer,
                Parameters = new Dictionary<string, object>
                {
                    ["QuantumField"] = true,
                    ["EnergyFluctuation"] = Intensity * 2f,
                    ["RealityStability"] = 1f - Intensity * 0.2f
                }
            };
        }
    }
}