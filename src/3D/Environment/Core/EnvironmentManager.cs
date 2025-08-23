using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Central manager for all environmental effects and atmospheric systems
    /// Coordinates starfields, weather, lighting, hazards, and performance optimization
    /// </summary>
    public class EnvironmentManager : IDisposable
    {
        private readonly Dictionary<string, IEnvironmentSystem> _systems;
        private readonly EnvironmentSettings _settings;
        private readonly PerformanceManager _performanceManager;
        private readonly EnvironmentLODManager _lodManager;
        
        // Core environmental systems
        private StarfieldManager _starfieldManager;
        private WeatherManager _weatherManager;
        private HazardManager _hazardManager;
        private BackgroundManager _backgroundManager;
        private AtmosphereManager _atmosphereManager;
        private InteractiveEnvironmentManager _interactiveManager;
        private DynamicLightingManager _lightingManager;
        
        // Performance tracking
        private float _lastUpdateTime;
        private float _lastRenderTime;
        private int _totalElements;
        
        public bool IsInitialized { get; private set; }
        public EnvironmentSettings Settings => _settings;
        public PerformanceMetrics Performance { get; private set; }
        
        // Events
        public event Action<EnvironmentEvent> OnEnvironmentEvent;
        
        public EnvironmentManager(EnvironmentSettings settings = null)
        {
            _settings = settings ?? EnvironmentSettings.CreateDefault();
            _systems = new Dictionary<string, IEnvironmentSystem>();
            _performanceManager = new PerformanceManager(_settings.Performance);
            _lodManager = new EnvironmentLODManager(_settings.Quality);
            
            InitializeSystems();
        }
        
        private void InitializeSystems()
        {
            try
            {
                // Create core systems
                _starfieldManager = new StarfieldManager(_settings.Starfield);
                _weatherManager = new WeatherManager(_settings.Weather);
                _hazardManager = new HazardManager(_settings.Hazards);
                _backgroundManager = new BackgroundManager(_settings.Background);
                _atmosphereManager = new AtmosphereManager(_settings.Atmosphere);
                _interactiveManager = new InteractiveEnvironmentManager(_settings.Interactive);
                _lightingManager = new DynamicLightingManager(_settings.Lighting);
                
                // Register all systems
                RegisterSystem("starfield", _starfieldManager);
                RegisterSystem("weather", _weatherManager);
                RegisterSystem("hazards", _hazardManager);
                RegisterSystem("background", _backgroundManager);
                RegisterSystem("atmosphere", _atmosphereManager);
                RegisterSystem("interactive", _interactiveManager);
                RegisterSystem("lighting", _lightingManager);
                
                // Initialize systems
                foreach (var system in _systems.Values)
                {
                    system.Initialize();
                }
                
                IsInitialized = true;
                ErrorManager.LogInfo("Environment Manager initialized successfully with all systems");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize Environment Manager", ex);
                throw;
            }
        }
        
        private void RegisterSystem(string name, IEnvironmentSystem system)
        {
            _systems[name] = system;
            system.OnPerformanceImpact += OnSystemPerformanceImpact;
        }
        
        public void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            if (!IsInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            // Update performance manager
            _performanceManager.Update(deltaTime);
            
            // Get current LOD level
            var lodLevel = _lodManager.GetCurrentLODLevel(camera, _performanceManager.CurrentFPS);
            
            // Update environmental lighting first (affects other systems)
            _lightingManager.Update(deltaTime, camera, playerPosition);
            
            // Update all systems with LOD considerations
            _totalElements = 0;
            foreach (var kvp in _systems)
            {
                var system = kvp.Value;
                if (system.IsActive)
                {
                    system.UpdateLOD(lodLevel);
                    system.Update(deltaTime, camera, playerPosition);
                    _totalElements += system.ElementCount;
                }
            }
            
            // Check for environmental events
            ProcessEnvironmentalEvents(deltaTime, camera, playerPosition);
            
            // Update performance metrics
            _lastUpdateTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            UpdatePerformanceMetrics();
        }
        
        public void Render(Camera3D camera, Vector3 playerPosition)
        {
            if (!IsInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            // Render in specific order for optimal depth and blending
            RenderInOrder(camera, playerPosition);
            
            _lastRenderTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
        }
        
        private void RenderInOrder(Camera3D camera, Vector3 playerPosition)
        {
            // 1. Background elements (furthest)
            _backgroundManager?.Render(camera, playerPosition);
            
            // 2. Starfield
            _starfieldManager?.Render(camera, playerPosition);
            
            // 3. Atmospheric effects (distant)
            _atmosphereManager?.RenderDistant(camera, playerPosition);
            
            // 4. Environmental hazards
            _hazardManager?.Render(camera, playerPosition);
            
            // 5. Weather systems
            _weatherManager?.Render(camera, playerPosition);
            
            // 6. Interactive elements
            _interactiveManager?.Render(camera, playerPosition);
            
            // 7. Atmospheric effects (near)
            _atmosphereManager?.RenderNear(camera, playerPosition);
            
            // 8. Dynamic lighting effects (last for blending)
            _lightingManager?.RenderEffects(camera, playerPosition);
        }
        
        private void ProcessEnvironmentalEvents(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            // Check for environmental triggers
            foreach (var system in _systems.Values)
            {
                var events = system.GetEnvironmentalEvents(playerPosition);
                foreach (var envEvent in events)
                {
                    OnEnvironmentEvent?.Invoke(envEvent);
                    HandleEnvironmentEvent(envEvent);
                }
            }
        }
        
        private void HandleEnvironmentEvent(EnvironmentEvent envEvent)
        {
            switch (envEvent.Type)
            {
                case EnvironmentEventType.GravityWellEntered:
                    // Notify physics system
                    break;
                    
                case EnvironmentEventType.EnergyFieldEntered:
                    // Apply energy effects
                    break;
                    
                case EnvironmentEventType.AsteroidStormApproaching:
                    // Trigger warning systems
                    break;
                    
                case EnvironmentEventType.SolarFlareDetected:
                    // Affect communications/shields
                    break;
                    
                case EnvironmentEventType.BlackHoleNearby:
                    // Extreme gravity effects
                    break;
            }
        }
        
        public T GetSystem<T>(string systemName) where T : class, IEnvironmentSystem
        {
            return _systems.ContainsKey(systemName) ? _systems[systemName] as T : null;
        }
        
        public void SetQualityLevel(EnvironmentQuality quality)
        {
            _lodManager.SetQualityLevel(quality);
            foreach (var system in _systems.Values)
            {
                system.SetQuality(quality);
            }
        }
        
        public void EnableSystem(string systemName, bool enabled)
        {
            if (_systems.ContainsKey(systemName))
            {
                _systems[systemName].IsActive = enabled;
            }
        }
        
        public void CreateCosmicEvent(CosmicEventType eventType, Vector3 position, float intensity = 1.0f)
        {
            switch (eventType)
            {
                case CosmicEventType.Supernova:
                    _lightingManager.CreateSupernova(position, intensity);
                    _atmosphereManager.CreateShockwave(position, intensity * 100f);
                    break;
                    
                case CosmicEventType.Pulsar:
                    _lightingManager.CreatePulsarEffect(position, intensity);
                    break;
                    
                case CosmicEventType.WormholeOpen:
                    _atmosphereManager.CreateWormhole(position, intensity);
                    _interactiveManager.AddGravityWell(position, intensity * 50f);
                    break;
                    
                case CosmicEventType.AsteroidStorm:
                    _hazardManager.CreateAsteroidStorm(position, intensity);
                    break;
                    
                case CosmicEventType.EnergyStorm:
                    _weatherManager.CreateEnergyStorm(position, intensity);
                    break;
            }
        }
        
        private void OnSystemPerformanceImpact(PerformanceImpact impact)
        {
            _performanceManager.ReportSystemImpact(impact);
            
            // Auto-adjust quality if performance is poor
            if (impact.Severity > 0.8f)
            {
                _lodManager.ReduceQuality();
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            Performance = new PerformanceMetrics
            {
                UpdateTime = _lastUpdateTime,
                RenderTime = _lastRenderTime,
                TotalElements = _totalElements,
                ActiveSystems = CountActiveSystems(),
                CurrentFPS = _performanceManager.CurrentFPS,
                MemoryUsage = _performanceManager.EstimatedMemoryUsage,
                LODLevel = _lodManager.CurrentLODLevel
            };
        }
        
        private int CountActiveSystems()
        {
            int count = 0;
            foreach (var system in _systems.Values)
            {
                if (system.IsActive) count++;
            }
            return count;
        }
        
        public void CreateEnvironmentalPreset(EnvironmentPreset preset)
        {
            switch (preset)
            {
                case EnvironmentPreset.DeepSpace:
                    SetupDeepSpace();
                    break;
                case EnvironmentPreset.Nebula:
                    SetupNebula();
                    break;
                case EnvironmentPreset.AsteroidField:
                    SetupAsteroidField();
                    break;
                case EnvironmentPreset.PlanetarySystem:
                    SetupPlanetarySystem();
                    break;
                case EnvironmentPreset.GalacticCore:
                    SetupGalacticCore();
                    break;
            }
        }
        
        private void SetupDeepSpace()
        {
            _starfieldManager.SetDensity(StarfieldDensity.Sparse);
            _atmosphereManager.SetAmbientLevel(0.1f);
            _lightingManager.SetAmbientIntensity(0.05f);
            _hazardManager.SetHazardLevel(HazardLevel.Low);
        }
        
        private void SetupNebula()
        {
            _starfieldManager.SetDensity(StarfieldDensity.Dense);
            _atmosphereManager.EnableNebula(true);
            _lightingManager.SetColorTemperature(6500f);
            _backgroundManager.SetNebulaVisibility(true);
        }
        
        private void SetupAsteroidField()
        {
            _hazardManager.SetHazardLevel(HazardLevel.High);
            _hazardManager.EnableAsteroidField(true);
            _weatherManager.EnableSpaceDust(true);
            _lightingManager.SetAmbientIntensity(0.3f);
        }
        
        private void SetupPlanetarySystem()
        {
            _lightingManager.EnableDayNightCycle(true);
            _backgroundManager.EnablePlanets(true);
            _interactiveManager.EnableGravityWells(true);
            _atmosphereManager.SetAtmosphericDensity(0.5f);
        }
        
        private void SetupGalacticCore()
        {
            _starfieldManager.SetDensity(StarfieldDensity.Ultra);
            _lightingManager.SetAmbientIntensity(0.8f);
            _hazardManager.SetHazardLevel(HazardLevel.Extreme);
            _atmosphereManager.EnableRadiation(true);
        }
        
        public EnvironmentStatistics GetStatistics()
        {
            return new EnvironmentStatistics
            {
                Performance = Performance,
                SystemStatistics = GetSystemStatistics(),
                EnvironmentState = GetEnvironmentState()
            };
        }
        
        private Dictionary<string, SystemStatistics> GetSystemStatistics()
        {
            var stats = new Dictionary<string, SystemStatistics>();
            foreach (var kvp in _systems)
            {
                stats[kvp.Key] = kvp.Value.GetStatistics();
            }
            return stats;
        }
        
        private EnvironmentState GetEnvironmentState()
        {
            return new EnvironmentState
            {
                ActiveHazards = _hazardManager.GetActiveHazards(),
                WeatherConditions = _weatherManager.GetCurrentConditions(),
                LightingState = _lightingManager.GetCurrentState(),
                AtmosphericState = _atmosphereManager.GetCurrentState()
            };
        }
        
        public void Dispose()
        {
            if (!IsInitialized) return;
            
            try
            {
                foreach (var system in _systems.Values)
                {
                    system?.Dispose();
                }
                _systems.Clear();
                
                _performanceManager?.Dispose();
                _lodManager?.Dispose();
                
                IsInitialized = false;
                ErrorManager.LogInfo("Environment Manager disposed successfully");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error disposing Environment Manager", ex);
            }
        }
    }
}