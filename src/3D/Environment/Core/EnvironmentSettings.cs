using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Comprehensive settings for all environmental systems
    /// </summary>
    public class EnvironmentSettings
    {
        public StarfieldSettings Starfield { get; set; }
        public WeatherSettings Weather { get; set; }
        public HazardSettings Hazards { get; set; }
        public BackgroundSettings Background { get; set; }
        public AtmosphereSettings Atmosphere { get; set; }
        public InteractiveSettings Interactive { get; set; }
        public DynamicLightingSettings Lighting { get; set; }
        public PerformanceSettings Performance { get; set; }
        public EnvironmentQuality Quality { get; set; }
        
        public static EnvironmentSettings CreateDefault()
        {
            return new EnvironmentSettings
            {
                Quality = EnvironmentQuality.High,
                Starfield = StarfieldSettings.CreateDefault(),
                Weather = WeatherSettings.CreateDefault(),
                Hazards = HazardSettings.CreateDefault(),
                Background = BackgroundSettings.CreateDefault(),
                Atmosphere = AtmosphereSettings.CreateDefault(),
                Interactive = InteractiveSettings.CreateDefault(),
                Lighting = DynamicLightingSettings.CreateDefault(),
                Performance = PerformanceSettings.CreateDefault()
            };
        }
        
        public static EnvironmentSettings CreatePerformanceOptimized()
        {
            return new EnvironmentSettings
            {
                Quality = EnvironmentQuality.Medium,
                Starfield = StarfieldSettings.CreatePerformanceOptimized(),
                Weather = WeatherSettings.CreatePerformanceOptimized(),
                Hazards = HazardSettings.CreatePerformanceOptimized(),
                Background = BackgroundSettings.CreatePerformanceOptimized(),
                Atmosphere = AtmosphereSettings.CreatePerformanceOptimized(),
                Interactive = InteractiveSettings.CreatePerformanceOptimized(),
                Lighting = DynamicLightingSettings.CreatePerformanceOptimized(),
                Performance = PerformanceSettings.CreateOptimized()
            };
        }
        
        public static EnvironmentSettings CreateMaximumQuality()
        {
            return new EnvironmentSettings
            {
                Quality = EnvironmentQuality.Ultra,
                Starfield = StarfieldSettings.CreateMaximumQuality(),
                Weather = WeatherSettings.CreateMaximumQuality(),
                Hazards = HazardSettings.CreateMaximumQuality(),
                Background = BackgroundSettings.CreateMaximumQuality(),
                Atmosphere = AtmosphereSettings.CreateMaximumQuality(),
                Interactive = InteractiveSettings.CreateMaximumQuality(),
                Lighting = DynamicLightingSettings.CreateMaximumQuality(),
                Performance = PerformanceSettings.CreateUnlimited()
            };
        }
    }
    
    /// <summary>
    /// Starfield configuration
    /// </summary>
    public class StarfieldSettings
    {
        public StarfieldDensity Density { get; set; } = StarfieldDensity.Medium;
        public int BaseStarCount { get; set; } = 2000;
        public float CullingDistance { get; set; } = 1000f;
        public bool EnableTwinkle { get; set; } = true;
        public bool EnableColors { get; set; } = true;
        public bool EnableNebulae { get; set; } = true;
        public float AnimationSpeed { get; set; } = 1.0f;
        public Vector3 DriftVelocity { get; set; } = new Vector3(0.1f, 0, 0.05f);
        
        public static StarfieldSettings CreateDefault() => new StarfieldSettings();
        public static StarfieldSettings CreatePerformanceOptimized() => new StarfieldSettings
        {
            Density = StarfieldDensity.Low,
            BaseStarCount = 1000,
            CullingDistance = 500f,
            EnableTwinkle = false,
            EnableColors = false,
            EnableNebulae = false
        };
        public static StarfieldSettings CreateMaximumQuality() => new StarfieldSettings
        {
            Density = StarfieldDensity.Ultra,
            BaseStarCount = 5000,
            CullingDistance = 2000f,
            EnableTwinkle = true,
            EnableColors = true,
            EnableNebulae = true
        };
    }
    
    /// <summary>
    /// Weather system configuration
    /// </summary>
    public class WeatherSettings
    {
        public bool EnableSpaceStorms { get; set; } = true;
        public bool EnableEMInterference { get; set; } = true;
        public bool EnableSpaceDust { get; set; } = true;
        public bool EnableSolarWind { get; set; } = true;
        public float BaseIntensity { get; set; } = 1.0f;
        public float WeatherChangeRate { get; set; } = 0.1f;
        public int MaxSimultaneousWeather { get; set; } = 3;
        
        public static WeatherSettings CreateDefault() => new WeatherSettings();
        public static WeatherSettings CreatePerformanceOptimized() => new WeatherSettings
        {
            EnableSpaceStorms = true,
            EnableEMInterference = false,
            EnableSpaceDust = false,
            EnableSolarWind = false,
            MaxSimultaneousWeather = 1
        };
        public static WeatherSettings CreateMaximumQuality() => new WeatherSettings
        {
            EnableSpaceStorms = true,
            EnableEMInterference = true,
            EnableSpaceDust = true,
            EnableSolarWind = true,
            MaxSimultaneousWeather = 5
        };
    }
    
    /// <summary>
    /// Environmental hazards configuration
    /// </summary>
    public class HazardSettings
    {
        public bool EnableBlackHoles { get; set; } = true;
        public bool EnableAsteroidStorms { get; set; } = true;
        public bool EnableEnergyFields { get; set; } = true;
        public bool EnableRadiationZones { get; set; } = true;
        public HazardLevel BaseHazardLevel { get; set; } = HazardLevel.Medium;
        public float HazardSpawnRate { get; set; } = 0.05f;
        public float MaxHazardDistance { get; set; } = 500f;
        
        public static HazardSettings CreateDefault() => new HazardSettings();
        public static HazardSettings CreatePerformanceOptimized() => new HazardSettings
        {
            EnableBlackHoles = false,
            EnableAsteroidStorms = true,
            EnableEnergyFields = false,
            EnableRadiationZones = false,
            BaseHazardLevel = HazardLevel.Low
        };
        public static HazardSettings CreateMaximumQuality() => new HazardSettings
        {
            EnableBlackHoles = true,
            EnableAsteroidStorms = true,
            EnableEnergyFields = true,
            EnableRadiationZones = true,
            BaseHazardLevel = HazardLevel.High
        };
    }
    
    /// <summary>
    /// Background elements configuration
    /// </summary>
    public class BackgroundSettings
    {
        public bool EnableDistantPlanets { get; set; } = true;
        public bool EnableSpaceStations { get; set; } = true;
        public bool EnableNebulae { get; set; } = true;
        public bool EnableGalaxies { get; set; } = true;
        public int MaxBackgroundElements { get; set; } = 20;
        public float BackgroundCullingDistance { get; set; } = 2000f;
        
        public static BackgroundSettings CreateDefault() => new BackgroundSettings();
        public static BackgroundSettings CreatePerformanceOptimized() => new BackgroundSettings
        {
            EnableDistantPlanets = true,
            EnableSpaceStations = false,
            EnableNebulae = false,
            EnableGalaxies = false,
            MaxBackgroundElements = 5
        };
        public static BackgroundSettings CreateMaximumQuality() => new BackgroundSettings
        {
            EnableDistantPlanets = true,
            EnableSpaceStations = true,
            EnableNebulae = true,
            EnableGalaxies = true,
            MaxBackgroundElements = 50
        };
    }
    
    /// <summary>
    /// Atmospheric effects configuration
    /// </summary>
    public class AtmosphereSettings
    {
        public bool EnableParticleStreams { get; set; } = true;
        public bool EnableEnergyCascades { get; set; } = true;
        public bool EnableDimensionalRifts { get; set; } = true;
        public bool EnableVolumetricEffects { get; set; } = true;
        public float AtmosphericDensity { get; set; } = 0.3f;
        public int MaxAtmosphericParticles { get; set; } = 1000;
        
        public static AtmosphereSettings CreateDefault() => new AtmosphereSettings();
        public static AtmosphereSettings CreatePerformanceOptimized() => new AtmosphereSettings
        {
            EnableParticleStreams = true,
            EnableEnergyCascades = false,
            EnableDimensionalRifts = false,
            EnableVolumetricEffects = false,
            MaxAtmosphericParticles = 200
        };
        public static AtmosphereSettings CreateMaximumQuality() => new AtmosphereSettings
        {
            EnableParticleStreams = true,
            EnableEnergyCascades = true,
            EnableDimensionalRifts = true,
            EnableVolumetricEffects = true,
            MaxAtmosphericParticles = 3000
        };
    }
    
    /// <summary>
    /// Interactive environment configuration
    /// </summary>
    public class InteractiveSettings
    {
        public bool EnableGravityWells { get; set; } = true;
        public bool EnableEnergyBoosts { get; set; } = true;
        public bool EnableRepairStations { get; set; } = true;
        public bool EnableWormholes { get; set; } = true;
        public float InteractionRange { get; set; } = 50f;
        public int MaxInteractiveElements { get; set; } = 10;
        
        public static InteractiveSettings CreateDefault() => new InteractiveSettings();
        public static InteractiveSettings CreatePerformanceOptimized() => new InteractiveSettings
        {
            EnableGravityWells = true,
            EnableEnergyBoosts = true,
            EnableRepairStations = false,
            EnableWormholes = false,
            MaxInteractiveElements = 5
        };
        public static InteractiveSettings CreateMaximumQuality() => new InteractiveSettings
        {
            EnableGravityWells = true,
            EnableEnergyBoosts = true,
            EnableRepairStations = true,
            EnableWormholes = true,
            MaxInteractiveElements = 20
        };
    }
    
    /// <summary>
    /// Dynamic lighting configuration
    /// </summary>
    public class DynamicLightingSettings
    {
        public bool EnableDayNightCycle { get; set; } = true;
        public bool EnableSolarFlares { get; set; } = true;
        public bool EnableCosmicEvents { get; set; } = true;
        public bool EnableDynamicShadows { get; set; } = true;
        public float AmbientIntensity { get; set; } = 0.2f;
        public float ColorTemperature { get; set; } = 5500f;
        
        public static DynamicLightingSettings CreateDefault() => new DynamicLightingSettings();
        public static DynamicLightingSettings CreatePerformanceOptimized() => new DynamicLightingSettings
        {
            EnableDayNightCycle = false,
            EnableSolarFlares = false,
            EnableCosmicEvents = false,
            EnableDynamicShadows = false
        };
        public static DynamicLightingSettings CreateMaximumQuality() => new DynamicLightingSettings
        {
            EnableDayNightCycle = true,
            EnableSolarFlares = true,
            EnableCosmicEvents = true,
            EnableDynamicShadows = true
        };
    }
    
    /// <summary>
    /// Performance management configuration
    /// </summary>
    public class PerformanceSettings
    {
        public int TargetFPS { get; set; } = 60;
        public float MaxUpdateTime { get; set; } = 5.0f;   // ms
        public float MaxRenderTime { get; set; } = 10.0f;  // ms
        public bool EnableAutoLOD { get; set; } = true;
        public bool EnableAdaptiveQuality { get; set; } = true;
        public float MemoryLimit { get; set; } = 256f;      // MB
        
        public static PerformanceSettings CreateDefault() => new PerformanceSettings();
        public static PerformanceSettings CreateOptimized() => new PerformanceSettings
        {
            TargetFPS = 60,
            MaxUpdateTime = 3.0f,
            MaxRenderTime = 6.0f,
            MemoryLimit = 128f
        };
        public static PerformanceSettings CreateUnlimited() => new PerformanceSettings
        {
            TargetFPS = 120,
            MaxUpdateTime = 20.0f,
            MaxRenderTime = 30.0f,
            EnableAutoLOD = false,
            MemoryLimit = 1024f
        };
    }
    
    /// <summary>
    /// Starfield density levels
    /// </summary>
    public enum StarfieldDensity
    {
        Minimal = 0,    // 500 stars
        Sparse = 1,     // 1000 stars
        Low = 2,        // 1500 stars
        Medium = 3,     // 2000 stars
        Dense = 4,      // 3000 stars
        Ultra = 5       // 5000+ stars
    }
    
    /// <summary>
    /// Environmental hazard levels
    /// </summary>
    public enum HazardLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Extreme = 4,
        Apocalyptic = 5
    }
}