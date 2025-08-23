using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Base interface for all environmental systems
    /// </summary>
    public interface IEnvironmentSystem : IDisposable
    {
        bool IsActive { get; set; }
        bool IsVisible { get; set; }
        int ElementCount { get; }
        string SystemName { get; }
        
        event Action<PerformanceImpact> OnPerformanceImpact;
        
        void Initialize();
        void Update(float deltaTime, Camera3D camera, Vector3 playerPosition);
        void Render(Camera3D camera, Vector3 playerPosition);
        void UpdateLOD(LODLevel lodLevel);
        void SetQuality(EnvironmentQuality quality);
        
        SystemStatistics GetStatistics();
        List<EnvironmentEvent> GetEnvironmentalEvents(Vector3 playerPosition);
    }
    
    /// <summary>
    /// Represents different environment quality levels
    /// </summary>
    public enum EnvironmentQuality
    {
        Potato = 0,    // Minimal effects
        Low = 1,       // Basic effects
        Medium = 2,    // Balanced effects
        High = 3,      // Rich effects
        Ultra = 4,     // Maximum effects
        Extreme = 5    // Experimental/stress test
    }
    
    /// <summary>
    /// Level of detail for environmental rendering
    /// </summary>
    public enum LODLevel
    {
        VeryLow = 0,   // Distance culling aggressive
        Low = 1,       // Reduced detail
        Medium = 2,    // Standard detail
        High = 3,      // Full detail
        Maximum = 4    // No culling
    }
    
    /// <summary>
    /// Performance impact data for system monitoring
    /// </summary>
    public struct PerformanceImpact
    {
        public string SystemName;
        public float UpdateTime;     // ms
        public float RenderTime;     // ms
        public int ElementCount;
        public float MemoryUsage;    // MB
        public float Severity;       // 0-1, how much it impacts performance
        
        public static PerformanceImpact None => new PerformanceImpact
        {
            SystemName = "None",
            UpdateTime = 0,
            RenderTime = 0,
            ElementCount = 0,
            MemoryUsage = 0,
            Severity = 0
        };
    }
    
    /// <summary>
    /// Environmental events that can affect gameplay
    /// </summary>
    public struct EnvironmentEvent
    {
        public EnvironmentEventType Type;
        public Vector3 Position;
        public float Radius;
        public float Intensity;
        public float Duration;
        public Dictionary<string, object> Parameters;
    }
    
    public enum EnvironmentEventType
    {
        // Hazards
        GravityWellEntered,
        GravityWellExited,
        BlackHoleNearby,
        AsteroidStormApproaching,
        EnergyFieldEntered,
        EnergyFieldExited,
        RadiationZoneEntered,
        
        // Weather
        SolarFlareDetected,
        ElectromagneticStorm,
        SpaceDustCloud,
        CosmicRayBurst,
        
        // Interactive
        EnergyBoostAvailable,
        RepairStationNearby,
        WormholeDetected,
        
        // Lighting
        PulsarPulse,
        SupernovaShockwave,
        DayNightTransition
    }
    
    /// <summary>
    /// System-specific statistics
    /// </summary>
    public struct SystemStatistics
    {
        public string SystemName;
        public bool IsActive;
        public int ElementCount;
        public float UpdateTime;
        public float RenderTime;
        public float MemoryUsage;
        public LODLevel CurrentLOD;
        public EnvironmentQuality Quality;
        public Dictionary<string, object> CustomData;
    }
    
    /// <summary>
    /// Overall performance metrics
    /// </summary>
    public struct PerformanceMetrics
    {
        public float UpdateTime;      // Total update time
        public float RenderTime;      // Total render time
        public int TotalElements;     // All environmental elements
        public int ActiveSystems;     // Currently active systems
        public float CurrentFPS;      // Current frame rate
        public float MemoryUsage;     // Total memory usage
        public LODLevel LODLevel;     // Current LOD level
    }
    
    /// <summary>
    /// Cosmic event types for dramatic environmental changes
    /// </summary>
    public enum CosmicEventType
    {
        Supernova,
        Pulsar,
        BlackHoleFormation,
        WormholeOpen,
        WormholeClose,
        AsteroidStorm,
        EnergyStorm,
        SolarFlare,
        GammaRayBurst,
        NeutronStarCollision
    }
    
    /// <summary>
    /// Environmental presets for quick setup
    /// </summary>
    public enum EnvironmentPreset
    {
        DeepSpace,      // Minimal, empty space
        Nebula,         // Colorful gas clouds
        AsteroidField,  // Dense asteroid field
        PlanetarySystem,// Near planets with day/night
        GalacticCore,   // Dense with many hazards
        BlackHoleRegion,// Extreme gravity effects
        Wormhole,       // Dimensional effects
        Battlefield     // Post-battle debris
    }
}