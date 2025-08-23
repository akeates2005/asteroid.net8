using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Manages dynamic starfields with multiple layers, twinkling, and parallax effects
    /// Creates immersive space backgrounds with performance optimization
    /// </summary>
    public class StarfieldManager : IEnvironmentSystem
    {
        private readonly StarfieldSettings _settings;
        private readonly List<StarLayer> _starLayers;
        private readonly List<NebulaCloud> _nebulae;
        private readonly Random _random;
        
        private float _time;
        private Vector3 _lastCameraPosition;
        private bool _isInitialized;
        private LODLevel _currentLOD = LODLevel.High;
        
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public int ElementCount => GetTotalStarCount();
        public string SystemName => "Starfield";
        
        public event Action<PerformanceImpact> OnPerformanceImpact;
        
        public StarfieldManager(StarfieldSettings settings)
        {
            _settings = settings ?? StarfieldSettings.CreateDefault();
            _starLayers = new List<StarLayer>();
            _nebulae = new List<NebulaCloud>();
            _random = new Random();
        }
        
        public void Initialize()
        {
            try
            {
                CreateStarLayers();
                CreateNebulae();
                
                _isInitialized = true;
                ErrorManager.LogInfo($"Starfield initialized with {ElementCount} total elements");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize starfield", ex);
                throw;
            }
        }
        
        private void CreateStarLayers()
        {
            // Create multiple layers for parallax effect
            var layerConfigs = GetLayerConfigurations();
            
            foreach (var config in layerConfigs)
            {
                var layer = new StarLayer
                {
                    Stars = GenerateStars(config.StarCount, config.DistanceRange),
                    ParallaxFactor = config.ParallaxFactor,
                    Brightness = config.Brightness,
                    TwinkleSpeed = config.TwinkleSpeed,
                    ColorVariation = config.ColorVariation
                };
                
                _starLayers.Add(layer);
            }
        }
        
        private List<LayerConfiguration> GetLayerConfigurations()
        {
            var densityMultiplier = GetDensityMultiplier();
            
            return new List<LayerConfiguration>
            {
                // Distant background stars
                new LayerConfiguration
                {
                    StarCount = (int)(300 * densityMultiplier),
                    DistanceRange = new Vector2(800f, 1000f),
                    ParallaxFactor = 0.1f,
                    Brightness = 0.3f,
                    TwinkleSpeed = 0.2f,
                    ColorVariation = 0.1f
                },
                // Mid-distance stars
                new LayerConfiguration
                {
                    StarCount = (int)(500 * densityMultiplier),
                    DistanceRange = new Vector2(400f, 800f),
                    ParallaxFactor = 0.3f,
                    Brightness = 0.6f,
                    TwinkleSpeed = 0.5f,
                    ColorVariation = 0.3f
                },
                // Near stars
                new LayerConfiguration
                {
                    StarCount = (int)(200 * densityMultiplier),
                    DistanceRange = new Vector2(200f, 400f),
                    ParallaxFactor = 0.7f,
                    Brightness = 0.9f,
                    TwinkleSpeed = 1.0f,
                    ColorVariation = 0.5f
                },
                // Bright foreground stars
                new LayerConfiguration
                {
                    StarCount = (int)(50 * densityMultiplier),
                    DistanceRange = new Vector2(100f, 200f),
                    ParallaxFactor = 1.0f,
                    Brightness = 1.2f,
                    TwinkleSpeed = 1.5f,
                    ColorVariation = 0.8f
                }
            };
        }
        
        private float GetDensityMultiplier()
        {
            return _settings.Density switch
            {
                StarfieldDensity.Minimal => 0.2f,
                StarfieldDensity.Sparse => 0.5f,
                StarfieldDensity.Low => 0.7f,
                StarfieldDensity.Medium => 1.0f,
                StarfieldDensity.Dense => 1.5f,
                StarfieldDensity.Ultra => 2.5f,
                _ => 1.0f
            };
        }
        
        private List<Star> GenerateStars(int count, Vector2 distanceRange)
        {
            var stars = new List<Star>();
            
            for (int i = 0; i < count; i++)
            {
                var star = new Star
                {
                    Position = GenerateRandomSpherePosition(distanceRange),
                    Color = GenerateStarColor(),
                    Size = _random.NextSingle() * 2f + 0.5f,
                    Brightness = _random.NextSingle() * 0.8f + 0.2f,
                    TwinklePhase = _random.NextSingle() * MathF.PI * 2f,
                    TwinkleSpeed = _random.NextSingle() * 0.5f + 0.5f,
                    Type = GetRandomStarType()
                };
                
                stars.Add(star);
            }
            
            return stars;
        }
        
        private Vector3 GenerateRandomSpherePosition(Vector2 distanceRange)
        {
            // Generate random position on sphere surface within distance range
            var theta = _random.NextSingle() * MathF.PI * 2f;
            var phi = MathF.Acos(1f - 2f * _random.NextSingle());
            var distance = _random.NextSingle() * (distanceRange.Y - distanceRange.X) + distanceRange.X;
            
            return new Vector3(
                distance * MathF.Sin(phi) * MathF.Cos(theta),
                distance * MathF.Sin(phi) * MathF.Sin(theta),
                distance * MathF.Cos(phi)
            );
        }
        
        private Color GenerateStarColor()
        {
            if (!_settings.EnableColors)
                return Color.White;
            
            // Realistic star colors based on temperature
            var colorType = _random.Next(0, 7);
            return colorType switch
            {
                0 => new Color(155, 176, 255, 255), // Blue giant
                1 => new Color(170, 191, 255, 255), // Blue-white
                2 => new Color(202, 215, 255, 255), // White
                3 => new Color(248, 247, 255, 255), // Yellow-white
                4 => new Color(255, 244, 234, 255), // Yellow (like Sun)
                5 => new Color(255, 210, 161, 255), // Orange
                6 => new Color(255, 181, 108, 255), // Red giant
                _ => Color.White
            };
        }
        
        private StarType GetRandomStarType()
        {
            var chance = _random.NextSingle();
            if (chance < 0.85f) return StarType.MainSequence;
            if (chance < 0.95f) return StarType.Giant;
            if (chance < 0.99f) return StarType.Supergiant;
            return StarType.Pulsar;
        }
        
        private void CreateNebulae()
        {
            if (!_settings.EnableNebulae) return;
            
            int nebulaCount = _settings.Density switch
            {
                StarfieldDensity.Minimal => 0,
                StarfieldDensity.Sparse => 1,
                StarfieldDensity.Low => 2,
                StarfieldDensity.Medium => 3,
                StarfieldDensity.Dense => 5,
                StarfieldDensity.Ultra => 8,
                _ => 3
            };
            
            for (int i = 0; i < nebulaCount; i++)
            {
                var nebula = new NebulaCloud
                {
                    Position = GenerateRandomSpherePosition(new Vector2(300f, 800f)),
                    Size = _random.NextSingle() * 200f + 100f,
                    Color = GenerateNebulaColor(),
                    Density = _random.NextSingle() * 0.3f + 0.1f,
                    AnimationSpeed = _random.NextSingle() * 0.2f + 0.05f,
                    Type = GetRandomNebulaType()
                };
                
                _nebulae.Add(nebula);
            }
        }
        
        private Color GenerateNebulaColor()
        {
            var colors = new[]
            {
                new Color(255, 100, 100, 50),  // Red
                new Color(100, 255, 100, 50),  // Green
                new Color(100, 100, 255, 50),  // Blue
                new Color(255, 255, 100, 50),  // Yellow
                new Color(255, 100, 255, 50),  // Magenta
                new Color(100, 255, 255, 50)   // Cyan
            };
            
            return colors[_random.Next(colors.Length)];
        }
        
        private NebulaType GetRandomNebulaType()
        {
            var types = Enum.GetValues<NebulaType>();
            return types[_random.Next(types.Length)];
        }
        
        public void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            _time += deltaTime * _settings.AnimationSpeed;
            
            // Update star twinkling
            if (_settings.EnableTwinkle)
            {
                UpdateStarTwinkling(deltaTime);
            }
            
            // Update nebula animations
            UpdateNebulae(deltaTime);
            
            // Apply drift effect
            ApplyDriftEffect(deltaTime);
            
            _lastCameraPosition = camera.Position;
            
            // Report performance impact
            var updateTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(updateTime, 0f);
        }
        
        private void UpdateStarTwinkling(float deltaTime)
        {
            foreach (var layer in _starLayers)
            {
                foreach (var star in layer.Stars)
                {
                    star.TwinklePhase += deltaTime * star.TwinkleSpeed * layer.TwinkleSpeed;
                    if (star.TwinklePhase > MathF.PI * 2f)
                        star.TwinklePhase -= MathF.PI * 2f;
                }
            }
        }
        
        private void UpdateNebulae(float deltaTime)
        {
            foreach (var nebula in _nebulae)
            {
                nebula.AnimationPhase += deltaTime * nebula.AnimationSpeed;
                if (nebula.AnimationPhase > MathF.PI * 2f)
                    nebula.AnimationPhase -= MathF.PI * 2f;
            }
        }
        
        private void ApplyDriftEffect(float deltaTime)
        {
            var drift = _settings.DriftVelocity * deltaTime;
            
            foreach (var layer in _starLayers)
            {
                foreach (var star in layer.Stars)
                {
                    star.Position += drift * layer.ParallaxFactor;
                }
            }
        }
        
        public void Render(Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !IsVisible || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            // Render in order: distant to near
            foreach (var layer in _starLayers)
            {
                RenderStarLayer(layer, camera);
            }
            
            // Render nebulae
            if (_settings.EnableNebulae && _currentLOD >= LODLevel.Medium)
            {
                RenderNebulae(camera);
            }
            
            var renderTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(0f, renderTime);
        }
        
        private void RenderStarLayer(StarLayer layer, Camera3D camera)
        {
            foreach (var star in layer.Stars)
            {
                // Calculate distance-based culling
                var distance = Vector3.Distance(star.Position, camera.Position);
                if (distance > _settings.CullingDistance) continue;
                
                // Calculate final position with parallax
                var parallaxOffset = (camera.Position - _lastCameraPosition) * layer.ParallaxFactor;
                var finalPosition = star.Position - parallaxOffset;
                
                // Calculate brightness with twinkling
                var brightness = star.Brightness * layer.Brightness;
                if (_settings.EnableTwinkle)
                {
                    var twinkle = MathF.Sin(star.TwinklePhase) * 0.3f + 0.7f;
                    brightness *= twinkle;
                }
                
                // Apply LOD-based size adjustment
                var finalSize = star.Size * GetLODSizeMultiplier();
                
                // Render star based on type and LOD
                RenderStar(finalPosition, star.Color, finalSize, brightness, star.Type);
            }
        }
        
        private void RenderStar(Vector3 position, Color color, float size, float brightness, StarType type)
        {
            var finalColor = new Color(
                (byte)(color.R * brightness),
                (byte)(color.G * brightness),
                (byte)(color.B * brightness),
                color.A
            );
            
            switch (_currentLOD)
            {
                case LODLevel.VeryLow:
                    Raylib.DrawPoint3D(position, finalColor);
                    break;
                    
                case LODLevel.Low:
                    if (type == StarType.Pulsar || type == StarType.Supergiant)
                        Raylib.DrawSphere(position, size * 0.5f, finalColor);
                    else
                        Raylib.DrawPoint3D(position, finalColor);
                    break;
                    
                case LODLevel.Medium:
                case LODLevel.High:
                case LODLevel.Maximum:
                    RenderDetailedStar(position, finalColor, size, type);
                    break;
            }
        }
        
        private void RenderDetailedStar(Vector3 position, Color color, float size, StarType type)
        {
            switch (type)
            {
                case StarType.MainSequence:
                    Raylib.DrawSphere(position, size, color);
                    break;
                    
                case StarType.Giant:
                    Raylib.DrawSphere(position, size * 1.5f, color);
                    if (_currentLOD >= LODLevel.High)
                        Raylib.DrawSphereWires(position, size * 2f, 8, 8, new Color(color.R, color.G, color.B, 50));
                    break;
                    
                case StarType.Supergiant:
                    Raylib.DrawSphere(position, size * 2f, color);
                    if (_currentLOD >= LODLevel.High)
                    {
                        Raylib.DrawSphereWires(position, size * 3f, 8, 8, new Color(color.R, color.G, color.B, 30));
                        Raylib.DrawSphereWires(position, size * 4f, 6, 6, new Color(color.R, color.G, color.B, 15));
                    }
                    break;
                    
                case StarType.Pulsar:
                    Raylib.DrawSphere(position, size, color);
                    if (_currentLOD >= LODLevel.Medium)
                    {
                        // Pulsing effect
                        var pulseSize = size * (1f + MathF.Sin(_time * 10f) * 0.5f);
                        Raylib.DrawSphereWires(position, pulseSize, 6, 6, new Color(255, 255, 255, 100));
                    }
                    break;
            }
        }
        
        private void RenderNebulae(Camera3D camera)
        {
            foreach (var nebula in _nebulae)
            {
                var distance = Vector3.Distance(nebula.Position, camera.Position);
                if (distance > _settings.CullingDistance * 1.5f) continue;
                
                RenderNebula(nebula);
            }
        }
        
        private void RenderNebula(NebulaCloud nebula)
        {
            // Animated nebula effect
            var animatedSize = nebula.Size * (1f + MathF.Sin(nebula.AnimationPhase) * 0.1f);
            var animatedDensity = nebula.Density * (1f + MathF.Cos(nebula.AnimationPhase * 0.7f) * 0.2f);
            
            var color = new Color(nebula.Color.R, nebula.Color.G, nebula.Color.B, 
                                 (byte)(nebula.Color.A * animatedDensity));
            
            switch (nebula.Type)
            {
                case NebulaType.Emission:
                    RenderEmissionNebula(nebula.Position, animatedSize, color);
                    break;
                case NebulaType.Reflection:
                    RenderReflectionNebula(nebula.Position, animatedSize, color);
                    break;
                case NebulaType.Planetary:
                    RenderPlanetaryNebula(nebula.Position, animatedSize, color);
                    break;
                case NebulaType.Supernova:
                    RenderSupernovaNebula(nebula.Position, animatedSize, color);
                    break;
            }
        }
        
        private void RenderEmissionNebula(Vector3 position, float size, Color color)
        {
            // Multiple spheres for layered effect
            Raylib.DrawSphere(position, size * 0.8f, new Color(color.R, color.G, color.B, color.A / 3));
            Raylib.DrawSphere(position, size * 0.6f, new Color(color.R, color.G, color.B, color.A / 2));
            Raylib.DrawSphere(position, size * 0.4f, color);
        }
        
        private void RenderReflectionNebula(Vector3 position, float size, Color color)
        {
            // Wispy, stretched appearance
            for (int i = 0; i < 3; i++)
            {
                var offset = new Vector3(
                    MathF.Sin(_time + i) * size * 0.1f,
                    MathF.Cos(_time + i) * size * 0.1f,
                    0
                );
                Raylib.DrawSphere(position + offset, size * (0.3f + i * 0.1f), 
                    new Color(color.R, color.G, color.B, color.A / (i + 1)));
            }
        }
        
        private void RenderPlanetaryNebula(Vector3 position, float size, Color color)
        {
            // Ring-like structure
            Raylib.DrawSphereWires(position, size * 0.8f, 12, 12, color);
            Raylib.DrawSphere(position, size * 0.2f, new Color(255, 255, 255, 150)); // Central star
        }
        
        private void RenderSupernovaNebula(Vector3 position, float size, Color color)
        {
            // Explosive, expanding appearance
            for (int i = 0; i < 5; i++)
            {
                var angle = (float)i / 5f * MathF.PI * 2f;
                var offset = new Vector3(
                    MathF.Cos(angle) * size * 0.3f,
                    MathF.Sin(angle) * size * 0.3f,
                    MathF.Sin(_time + i) * size * 0.1f
                );
                Raylib.DrawSphere(position + offset, size * 0.2f, 
                    new Color(color.R, color.G, color.B, color.A / 2));
            }
        }
        
        private float GetLODSizeMultiplier()
        {
            return _currentLOD switch
            {
                LODLevel.VeryLow => 0.5f,
                LODLevel.Low => 0.7f,
                LODLevel.Medium => 1.0f,
                LODLevel.High => 1.2f,
                LODLevel.Maximum => 1.5f,
                _ => 1.0f
            };
        }
        
        public void UpdateLOD(LODLevel lodLevel)
        {
            _currentLOD = lodLevel;
        }
        
        public void SetQuality(EnvironmentQuality quality)
        {
            // Adjust settings based on quality
            switch (quality)
            {
                case EnvironmentQuality.Potato:
                    _settings.EnableTwinkle = false;
                    _settings.EnableColors = false;
                    _settings.EnableNebulae = false;
                    break;
                case EnvironmentQuality.Low:
                    _settings.EnableTwinkle = false;
                    _settings.EnableColors = true;
                    _settings.EnableNebulae = false;
                    break;
                case EnvironmentQuality.Medium:
                    _settings.EnableTwinkle = true;
                    _settings.EnableColors = true;
                    _settings.EnableNebulae = true;
                    break;
                case EnvironmentQuality.High:
                case EnvironmentQuality.Ultra:
                case EnvironmentQuality.Extreme:
                    _settings.EnableTwinkle = true;
                    _settings.EnableColors = true;
                    _settings.EnableNebulae = true;
                    break;
            }
        }
        
        public void SetDensity(StarfieldDensity density)
        {
            if (_settings.Density != density)
            {
                _settings.Density = density;
                if (_isInitialized)
                {
                    RegenerateStarfield();
                }
            }
        }
        
        private void RegenerateStarfield()
        {
            _starLayers.Clear();
            _nebulae.Clear();
            CreateStarLayers();
            CreateNebulae();
        }
        
        private int GetTotalStarCount()
        {
            int total = 0;
            foreach (var layer in _starLayers)
            {
                total += layer.Stars.Count;
            }
            return total + _nebulae.Count;
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
            // Rough estimation: each star ~64 bytes, each nebula ~128 bytes
            return (GetTotalStarCount() * 64f + _nebulae.Count * 128f) / (1024f * 1024f);
        }
        
        private float CalculateSeverity(float updateTime, float renderTime)
        {
            var totalTime = updateTime + renderTime;
            if (totalTime < 1f) return 0f;
            if (totalTime < 3f) return 0.3f;
            if (totalTime < 5f) return 0.6f;
            return 1f;
        }
        
        public SystemStatistics GetStatistics()
        {
            return new SystemStatistics
            {
                SystemName = SystemName,
                IsActive = IsActive,
                ElementCount = ElementCount,
                UpdateTime = _lastUpdateTime,
                RenderTime = _lastRenderTime,
                MemoryUsage = EstimateMemoryUsage(),
                CurrentLOD = _currentLOD,
                Quality = _settings.Density switch
                {
                    StarfieldDensity.Minimal => EnvironmentQuality.Potato,
                    StarfieldDensity.Sparse => EnvironmentQuality.Low,
                    StarfieldDensity.Low => EnvironmentQuality.Medium,
                    StarfieldDensity.Medium => EnvironmentQuality.High,
                    StarfieldDensity.Dense => EnvironmentQuality.Ultra,
                    StarfieldDensity.Ultra => EnvironmentQuality.Extreme,
                    _ => EnvironmentQuality.High
                },
                CustomData = new Dictionary<string, object>
                {
                    ["StarLayers"] = _starLayers.Count,
                    ["Nebulae"] = _nebulae.Count,
                    ["TwinkleEnabled"] = _settings.EnableTwinkle,
                    ["ColorsEnabled"] = _settings.EnableColors
                }
            };
        }
        
        public List<EnvironmentEvent> GetEnvironmentalEvents(Vector3 playerPosition)
        {
            var events = new List<EnvironmentEvent>();
            
            // Check for pulsar proximity
            foreach (var layer in _starLayers)
            {
                foreach (var star in layer.Stars)
                {
                    if (star.Type == StarType.Pulsar)
                    {
                        var distance = Vector3.Distance(star.Position, playerPosition);
                        if (distance < 100f)
                        {
                            events.Add(new EnvironmentEvent
                            {
                                Type = EnvironmentEventType.PulsarPulse,
                                Position = star.Position,
                                Radius = 100f,
                                Intensity = 1f - (distance / 100f),
                                Duration = 1f,
                                Parameters = new Dictionary<string, object>
                                {
                                    ["StarColor"] = star.Color,
                                    ["PulsePhase"] = star.TwinklePhase
                                }
                            });
                        }
                    }
                }
            }
            
            return events;
        }
        
        private float _lastUpdateTime;
        private float _lastRenderTime;
        
        public void Dispose()
        {
            _starLayers?.Clear();
            _nebulae?.Clear();
        }
    }
    
    // Supporting data structures
    public class StarLayer
    {
        public List<Star> Stars { get; set; } = new List<Star>();
        public float ParallaxFactor { get; set; }
        public float Brightness { get; set; }
        public float TwinkleSpeed { get; set; }
        public float ColorVariation { get; set; }
    }
    
    public class Star
    {
        public Vector3 Position { get; set; }
        public Color Color { get; set; }
        public float Size { get; set; }
        public float Brightness { get; set; }
        public float TwinklePhase { get; set; }
        public float TwinkleSpeed { get; set; }
        public StarType Type { get; set; }
    }
    
    public class NebulaCloud
    {
        public Vector3 Position { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
        public float Density { get; set; }
        public float AnimationSpeed { get; set; }
        public float AnimationPhase { get; set; }
        public NebulaType Type { get; set; }
    }
    
    public class LayerConfiguration
    {
        public int StarCount { get; set; }
        public Vector2 DistanceRange { get; set; }
        public float ParallaxFactor { get; set; }
        public float Brightness { get; set; }
        public float TwinkleSpeed { get; set; }
        public float ColorVariation { get; set; }
    }
    
    public enum StarType
    {
        MainSequence,
        Giant,
        Supergiant,
        Pulsar
    }
    
    public enum NebulaType
    {
        Emission,
        Reflection,
        Planetary,
        Supernova
    }
}