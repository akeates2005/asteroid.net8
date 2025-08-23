using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Environment
{
    /// <summary>
    /// Manages distant background elements including planets, space stations,
    /// galaxies, and other large-scale cosmic structures
    /// </summary>
    public class BackgroundManager : IEnvironmentSystem
    {
        private readonly BackgroundSettings _settings;
        private readonly List<BackgroundElement> _backgroundElements;
        private readonly Random _random;
        
        private bool _isInitialized;
        private LODLevel _currentLOD = LODLevel.High;
        private float _parallaxTimer;
        
        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public int ElementCount => _backgroundElements.Count;
        public string SystemName => "Background";
        
        public event Action<PerformanceImpact> OnPerformanceImpact;
        
        public BackgroundManager(BackgroundSettings settings)
        {
            _settings = settings ?? BackgroundSettings.CreateDefault();
            _backgroundElements = new List<BackgroundElement>();
            _random = new Random();
        }
        
        public void Initialize()
        {
            try
            {
                CreateBackgroundElements();
                
                _isInitialized = true;
                ErrorManager.LogInfo($"Background Manager initialized with {_backgroundElements.Count} elements");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize Background Manager", ex);
                throw;
            }
        }
        
        private void CreateBackgroundElements()
        {
            int elementCount = Math.Min(_settings.MaxBackgroundElements, CalculateOptimalElementCount());
            
            for (int i = 0; i < elementCount; i++)
            {
                var elementType = DetermineElementType();
                var element = CreateBackgroundElement(elementType, i);
                
                if (element != null)
                {
                    _backgroundElements.Add(element);
                }
            }
        }
        
        private int CalculateOptimalElementCount()
        {
            return _settings.MaxBackgroundElements;
        }
        
        private BackgroundElementType DetermineElementType()
        {
            var roll = _random.NextSingle();
            
            if (roll < 0.3f && _settings.EnableDistantPlanets)
                return BackgroundElementType.DistantPlanet;
            if (roll < 0.5f && _settings.EnableSpaceStations)
                return BackgroundElementType.SpaceStation;
            if (roll < 0.7f && _settings.EnableNebulae)
                return BackgroundElementType.DistantNebula;
            if (roll < 0.85f && _settings.EnableGalaxies)
                return BackgroundElementType.Galaxy;
            if (roll < 0.92f && _settings.EnableDistantPlanets)
                return BackgroundElementType.GasGiant;
            if (roll < 0.96f && _settings.EnableSpaceStations)
                return BackgroundElementType.DerelictShip;
            if (roll < 0.98f && _settings.EnableGalaxies)
                return BackgroundElementType.Quasar;
            
            return BackgroundElementType.AsteroidCluster;
        }
        
        private BackgroundElement CreateBackgroundElement(BackgroundElementType type, int index)
        {
            var position = GenerateBackgroundPosition(index);
            var distance = Vector3.Distance(position, Vector3.Zero);
            
            return type switch
            {
                BackgroundElementType.DistantPlanet => new DistantPlanet(position, distance, _random),
                BackgroundElementType.SpaceStation => new SpaceStation(position, distance, _random),
                BackgroundElementType.DistantNebula => new DistantNebula(position, distance, _random),
                BackgroundElementType.Galaxy => new Galaxy(position, distance, _random),
                BackgroundElementType.GasGiant => new GasGiant(position, distance, _random),
                BackgroundElementType.DerelictShip => new DerelictShip(position, distance, _random),
                BackgroundElementType.Quasar => new Quasar(position, distance, _random),
                BackgroundElementType.AsteroidCluster => new AsteroidCluster(position, distance, _random),
                _ => null
            };
        }
        
        private Vector3 GenerateBackgroundPosition(int index)
        {
            // Generate positions on a sphere around the scene with varying distances
            var phi = _random.NextSingle() * MathF.PI * 2f;
            var theta = MathF.Acos(1f - 2f * _random.NextSingle());
            var distance = _random.NextSingle() * (_settings.BackgroundCullingDistance - 500f) + 500f;
            
            return new Vector3(
                distance * MathF.Sin(theta) * MathF.Cos(phi),
                distance * MathF.Sin(theta) * MathF.Sin(phi),
                distance * MathF.Cos(theta)
            );
        }
        
        public void Update(float deltaTime, Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            _parallaxTimer += deltaTime;
            
            // Update background elements with parallax
            foreach (var element in _backgroundElements)
            {
                element.Update(deltaTime, camera, playerPosition, _parallaxTimer);
            }
            
            var updateTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(updateTime, 0f);
        }
        
        public void Render(Camera3D camera, Vector3 playerPosition)
        {
            if (!IsActive || !IsVisible || !_isInitialized) return;
            
            var startTime = DateTime.UtcNow;
            
            // Sort elements by distance (render far to near)
            var sortedElements = new List<BackgroundElement>(_backgroundElements);
            sortedElements.Sort((a, b) => b.Distance.CompareTo(a.Distance));
            
            // Render elements based on LOD and distance
            foreach (var element in sortedElements)
            {
                var distance = Vector3.Distance(element.Position, camera.Position);
                if (distance <= _settings.BackgroundCullingDistance && ShouldRenderElement(element))
                {
                    element.Render(camera, _currentLOD);
                }
            }
            
            var renderTime = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            ReportPerformanceImpact(0f, renderTime);
        }
        
        private bool ShouldRenderElement(BackgroundElement element)
        {
            // Skip certain elements based on LOD
            switch (_currentLOD)
            {
                case LODLevel.VeryLow:
                    return element.Type == BackgroundElementType.DistantPlanet || 
                           element.Type == BackgroundElementType.Galaxy;
                case LODLevel.Low:
                    return element.Type != BackgroundElementType.DerelictShip && 
                           element.Type != BackgroundElementType.AsteroidCluster;
                default:
                    return true;
            }
        }
        
        public void EnablePlanets(bool enable)
        {
            _settings.EnableDistantPlanets = enable;
        }
        
        public void SetNebulaVisibility(bool visible)
        {
            _settings.EnableNebulae = visible;
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
                    _settings.EnableDistantPlanets = true;
                    _settings.EnableSpaceStations = false;
                    _settings.EnableNebulae = false;
                    _settings.EnableGalaxies = false;
                    _settings.MaxBackgroundElements = 3;
                    break;
                case EnvironmentQuality.Low:
                    _settings.EnableDistantPlanets = true;
                    _settings.EnableSpaceStations = false;
                    _settings.EnableNebulae = false;
                    _settings.EnableGalaxies = true;
                    _settings.MaxBackgroundElements = 8;
                    break;
                case EnvironmentQuality.Medium:
                    _settings.EnableDistantPlanets = true;
                    _settings.EnableSpaceStations = true;
                    _settings.EnableNebulae = true;
                    _settings.EnableGalaxies = true;
                    _settings.MaxBackgroundElements = 15;
                    break;
                case EnvironmentQuality.High:
                case EnvironmentQuality.Ultra:
                case EnvironmentQuality.Extreme:
                    _settings.EnableDistantPlanets = true;
                    _settings.EnableSpaceStations = true;
                    _settings.EnableNebulae = true;
                    _settings.EnableGalaxies = true;
                    _settings.MaxBackgroundElements = 25;
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
            return (_backgroundElements.Count * 128f) / (1024f * 1024f);
        }
        
        private float CalculateSeverity(float updateTime, float renderTime)
        {
            var totalTime = updateTime + renderTime;
            if (totalTime < 1f) return 0f;
            if (totalTime < 2f) return 0.2f;
            if (totalTime < 4f) return 0.5f;
            return 0.8f;
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
                    ["PlanetsEnabled"] = _settings.EnableDistantPlanets,
                    ["StationsEnabled"] = _settings.EnableSpaceStations,
                    ["NebulaeEnabled"] = _settings.EnableNebulae,
                    ["GalaxiesEnabled"] = _settings.EnableGalaxies,
                    ["MaxElements"] = _settings.MaxBackgroundElements
                }
            };
        }
        
        public List<EnvironmentEvent> GetEnvironmentalEvents(Vector3 playerPosition)
        {
            // Background elements typically don't generate events
            return new List<EnvironmentEvent>();
        }
        
        public void Dispose()
        {
            _backgroundElements?.Clear();
        }
    }
    
    // Supporting enums and classes
    public enum BackgroundElementType
    {
        DistantPlanet,
        SpaceStation,
        DistantNebula,
        Galaxy,
        GasGiant,
        DerelictShip,
        Quasar,
        AsteroidCluster
    }
    
    public abstract class BackgroundElement
    {
        public Vector3 Position { get; protected set; }
        public float Distance { get; protected set; }
        public BackgroundElementType Type { get; protected set; }
        public float ParallaxFactor { get; protected set; }
        
        protected Random _random;
        protected float _animationTimer;
        protected Vector3 _basePosition;
        
        public BackgroundElement(Vector3 position, float distance, Random random)
        {
            Position = position;
            _basePosition = position;
            Distance = distance;
            _random = random;
            ParallaxFactor = CalculateParallaxFactor(distance);
        }
        
        protected float CalculateParallaxFactor(float distance)
        {
            // More distant objects have less parallax
            return Math.Clamp(1000f / distance, 0.01f, 1f);
        }
        
        public virtual void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            _animationTimer += deltaTime;
            
            // Apply parallax movement
            var parallaxOffset = (camera.Position - playerPosition) * ParallaxFactor;
            Position = _basePosition - parallaxOffset;
        }
        
        public abstract void Render(Camera3D camera, LODLevel lodLevel);
    }
    
    public class DistantPlanet : BackgroundElement
    {
        private Color _planetColor;
        private float _size;
        private bool _hasRings;
        private Color _ringColor;
        private List<Vector3> _moons;
        private PlanetType _planetType;
        
        public DistantPlanet(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.DistantPlanet;
            _size = random.NextSingle() * 20f + 5f;
            _planetType = (PlanetType)random.Next(0, 6);
            _planetColor = GeneratePlanetColor(_planetType);
            _hasRings = random.NextSingle() < 0.3f;
            _ringColor = GenerateRingColor();
            _moons = GenerateMoons();
        }
        
        private Color GeneratePlanetColor(PlanetType type)
        {
            return type switch
            {
                PlanetType.Rocky => new Color(150, 100, 80, 255),
                PlanetType.Desert => new Color(200, 150, 100, 255),
                PlanetType.Ocean => new Color(50, 150, 255, 255),
                PlanetType.Ice => new Color(200, 220, 255, 255),
                PlanetType.Gas => new Color(255, 200, 150, 255),
                PlanetType.Lava => new Color(255, 100, 50, 255),
                _ => new Color(128, 128, 128, 255)
            };
        }
        
        private Color GenerateRingColor()
        {
            var colors = new[]
            {
                new Color(200, 180, 160, 100),
                new Color(150, 150, 200, 100),
                new Color(255, 200, 100, 100)
            };
            return colors[_random.Next(colors.Length)];
        }
        
        private List<Vector3> GenerateMoons()
        {
            var moons = new List<Vector3>();
            int moonCount = _random.Next(0, 4);
            
            for (int i = 0; i < moonCount; i++)
            {
                var angle = _random.NextSingle() * MathF.PI * 2f;
                var distance = _size * (2f + i);
                var moonPos = Position + new Vector3(
                    MathF.Cos(angle) * distance,
                    (_random.NextSingle() - 0.5f) * distance * 0.2f,
                    MathF.Sin(angle) * distance
                );
                moons.Add(moonPos);
            }
            
            return moons;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Update moon positions (orbital motion)
            for (int i = 0; i < _moons.Count; i++)
            {
                var angle = globalTimer * 0.1f * (i + 1);
                var distance = _size * (2f + i);
                _moons[i] = Position + new Vector3(
                    MathF.Cos(angle) * distance,
                    (_random.NextSingle() - 0.5f) * distance * 0.2f,
                    MathF.Sin(angle) * distance
                );
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            // Planet
            Raylib.DrawSphere(Position, _size, _planetColor);
            
            // Rings
            if (_hasRings && lodLevel >= LODLevel.Medium)
            {
                var ringRadius = _size * 1.8f;
                Raylib.DrawCircle3D(Position, ringRadius, Vector3.UnitY, 0f, _ringColor);
            }
            
            // Moons
            if (lodLevel >= LODLevel.High)
            {
                foreach (var moon in _moons)
                {
                    var moonSize = _size * 0.2f;
                    Raylib.DrawSphere(moon, moonSize, Color.Gray);
                }
            }
            
            // Atmosphere glow
            if (lodLevel >= LODLevel.High && (_planetType == PlanetType.Ocean || _planetType == PlanetType.Gas))
            {
                var glowColor = new Color(_planetColor.R, _planetColor.G, _planetColor.B, 30);
                Raylib.DrawSphere(Position, _size * 1.1f, glowColor);
            }
        }
    }
    
    public class SpaceStation : BackgroundElement
    {
        private StationType _stationType;
        private Color _hullColor;
        private List<Vector3> _stationModules;
        private bool _isOperational;
        private float _rotationSpeed;
        
        public SpaceStation(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.SpaceStation;
            _stationType = (StationType)random.Next(0, 4);
            _hullColor = GenerateHullColor();
            _isOperational = random.NextSingle() < 0.7f;
            _rotationSpeed = random.NextSingle() * 0.5f + 0.1f;
            _stationModules = GenerateStationModules();
        }
        
        private Color GenerateHullColor()
        {
            var colors = new[]
            {
                new Color(150, 150, 150, 255), // Standard gray
                new Color(100, 100, 120, 255), // Dark blue-gray
                new Color(120, 100, 100, 255), // Reddish
                new Color(100, 120, 100, 255)  // Greenish
            };
            return colors[_random.Next(colors.Length)];
        }
        
        private List<Vector3> GenerateStationModules()
        {
            var modules = new List<Vector3>();
            int moduleCount = _stationType switch
            {
                StationType.Research => 3 + _random.Next(3),
                StationType.Mining => 2 + _random.Next(2),
                StationType.Military => 4 + _random.Next(4),
                StationType.Trading => 5 + _random.Next(5),
                _ => 3
            };
            
            for (int i = 0; i < moduleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * 20f,
                    (_random.NextSingle() - 0.5f) * 20f,
                    (_random.NextSingle() - 0.5f) * 20f
                );
                modules.Add(Position + offset);
            }
            
            return modules;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Rotate station modules
            var rotation = _rotationSpeed * globalTimer;
            for (int i = 0; i < _stationModules.Count; i++)
            {
                var relative = _stationModules[i] - Position;
                var rotated = Vector3.Transform(relative, Matrix4x4.CreateRotationY(rotation));
                _stationModules[i] = Position + rotated;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            // Main station hub
            Raylib.DrawSphere(Position, 3f, _hullColor);
            
            // Station modules
            foreach (var module in _stationModules)
            {
                Raylib.DrawCube(module, 2f, 2f, 4f, _hullColor);
                
                // Connecting struts
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawLine3D(Position, module, Color.DarkGray);
                }
            }
            
            // Operational lights
            if (_isOperational && lodLevel >= LODLevel.High)
            {
                var lightColor = Color.Green;
                foreach (var module in _stationModules)
                {
                    if (_random.NextSingle() < 0.3f) // Not all modules have lights
                    {
                        Raylib.DrawSphere(module + Vector3.UnitY * 1.5f, 0.2f, lightColor);
                    }
                }
            }
        }
    }
    
    public class DistantNebula : BackgroundElement
    {
        private Color _nebulaColor;
        private float _size;
        private List<Vector3> _nebulaParticles;
        private float _density;
        
        public DistantNebula(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.DistantNebula;
            _size = random.NextSingle() * 100f + 50f;
            _nebulaColor = GenerateNebulaColor();
            _density = random.NextSingle() * 0.3f + 0.1f;
            _nebulaParticles = GenerateNebulaParticles();
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
        
        private List<Vector3> GenerateNebulaParticles()
        {
            var particles = new List<Vector3>();
            int particleCount = (int)(_size * _density);
            
            for (int i = 0; i < particleCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * _size,
                    (_random.NextSingle() - 0.5f) * _size,
                    (_random.NextSingle() - 0.5f) * _size
                );
                particles.Add(Position + offset);
            }
            
            return particles;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Subtle nebula movement
            for (int i = 0; i < _nebulaParticles.Count; i++)
            {
                var drift = new Vector3(
                    MathF.Sin(globalTimer * 0.1f + i) * deltaTime * 0.5f,
                    MathF.Cos(globalTimer * 0.15f + i) * deltaTime * 0.5f,
                    MathF.Sin(globalTimer * 0.08f + i) * deltaTime * 0.5f
                );
                _nebulaParticles[i] += drift;
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            // Render nebula as collection of particles
            foreach (var particle in _nebulaParticles)
            {
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawSphere(particle, 1f, _nebulaColor);
                }
                else
                {
                    Raylib.DrawPoint3D(particle, _nebulaColor);
                }
            }
            
            // Nebula glow effect
            if (lodLevel >= LODLevel.High)
            {
                var glowColor = new Color(_nebulaColor.R, _nebulaColor.G, _nebulaColor.B, 20);
                Raylib.DrawSphere(Position, _size * 0.8f, glowColor);
            }
        }
    }
    
    public class Galaxy : BackgroundElement
    {
        private GalaxyType _galaxyType;
        private Color _coreColor;
        private List<Vector3> _spiralArms;
        private float _armCount;
        
        public Galaxy(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.Galaxy;
            _galaxyType = (GalaxyType)random.Next(0, 3);
            _coreColor = GenerateGalaxyCoreColor();
            _armCount = _galaxyType == GalaxyType.Spiral ? random.Next(2, 5) : 0;
            _spiralArms = GenerateSpiralArms();
        }
        
        private Color GenerateGalaxyCoreColor()
        {
            var colors = new[]
            {
                new Color(255, 200, 100, 150), // Yellow core
                new Color(200, 150, 255, 150), // Blue core
                new Color(255, 150, 150, 150)  // Red core
            };
            return colors[_random.Next(colors.Length)];
        }
        
        private List<Vector3> GenerateSpiralArms()
        {
            var arms = new List<Vector3>();
            
            if (_galaxyType == GalaxyType.Spiral)
            {
                for (int arm = 0; arm < _armCount; arm++)
                {
                    float armAngle = (float)arm / _armCount * MathF.PI * 2f;
                    
                    for (int i = 0; i < 20; i++)
                    {
                        float t = (float)i / 19f;
                        float radius = t * 30f;
                        float angle = armAngle + t * MathF.PI * 4f; // Spiral effect
                        
                        var armPoint = Position + new Vector3(
                            MathF.Cos(angle) * radius,
                            (_random.NextSingle() - 0.5f) * radius * 0.1f,
                            MathF.Sin(angle) * radius
                        );
                        arms.Add(armPoint);
                    }
                }
            }
            
            return arms;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Slowly rotate galaxy
            if (_galaxyType == GalaxyType.Spiral)
            {
                var rotation = globalTimer * 0.01f;
                for (int i = 0; i < _spiralArms.Count; i++)
                {
                    var relative = _spiralArms[i] - Position;
                    var rotated = Vector3.Transform(relative, Matrix4x4.CreateRotationY(rotation));
                    _spiralArms[i] = Position + rotated;
                }
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            // Galaxy core
            Raylib.DrawSphere(Position, 5f, _coreColor);
            
            // Galaxy structure
            switch (_galaxyType)
            {
                case GalaxyType.Spiral:
                    if (lodLevel >= LODLevel.High)
                    {
                        foreach (var armPoint in _spiralArms)
                        {
                            Raylib.DrawSphere(armPoint, 0.5f, 
                                new Color(_coreColor.R, _coreColor.G, _coreColor.B, 100));
                        }
                    }
                    break;
                    
                case GalaxyType.Elliptical:
                    Raylib.DrawSphere(Position, 15f, 
                        new Color(_coreColor.R, _coreColor.G, _coreColor.B, 50));
                    break;
                    
                case GalaxyType.Irregular:
                    for (int i = 0; i < 10; i++)
                    {
                        var offset = new Vector3(
                            (_random.NextSingle() - 0.5f) * 20f,
                            (_random.NextSingle() - 0.5f) * 20f,
                            (_random.NextSingle() - 0.5f) * 20f
                        );
                        Raylib.DrawSphere(Position + offset, 2f, 
                            new Color(_coreColor.R, _coreColor.G, _coreColor.B, 80));
                    }
                    break;
            }
        }
    }
    
    public class GasGiant : BackgroundElement
    {
        private Color _atmosphereColor;
        private List<Color> _bands;
        private float _size;
        private bool _hasStorm;
        private Vector3 _stormPosition;
        
        public GasGiant(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.GasGiant;
            _size = random.NextSingle() * 25f + 10f;
            _atmosphereColor = GenerateAtmosphereColor();
            _bands = GenerateBands();
            _hasStorm = random.NextSingle() < 0.4f;
            if (_hasStorm)
            {
                _stormPosition = Position + new Vector3(
                    (_random.NextSingle() - 0.5f) * _size,
                    (_random.NextSingle() - 0.5f) * _size,
                    0
                );
            }
        }
        
        private Color GenerateAtmosphereColor()
        {
            var colors = new[]
            {
                new Color(255, 200, 150, 255), // Jupiter-like
                new Color(200, 220, 255, 255), // Neptune-like
                new Color(255, 180, 120, 255), // Saturn-like
                new Color(180, 255, 180, 255)  // Exotic green
            };
            return colors[_random.Next(colors.Length)];
        }
        
        private List<Color> GenerateBands()
        {
            var bands = new List<Color>();
            int bandCount = _random.Next(3, 7);
            
            for (int i = 0; i < bandCount; i++)
            {
                var variation = 1f + (_random.NextSingle() - 0.5f) * 0.4f;
                bands.Add(new Color(
                    (byte)Math.Clamp(_atmosphereColor.R * variation, 0, 255),
                    (byte)Math.Clamp(_atmosphereColor.G * variation, 0, 255),
                    (byte)Math.Clamp(_atmosphereColor.B * variation, 0, 255),
                    _atmosphereColor.A
                ));
            }
            
            return bands;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Animate storm
            if (_hasStorm)
            {
                var stormMovement = new Vector3(
                    MathF.Sin(globalTimer * 0.1f) * deltaTime,
                    MathF.Cos(globalTimer * 0.1f) * deltaTime,
                    0
                );
                _stormPosition += stormMovement;
                
                // Keep storm on planet surface
                var distanceFromCenter = Vector3.Distance(_stormPosition, Position);
                if (distanceFromCenter > _size * 0.8f)
                {
                    _stormPosition = Position + Vector3.Normalize(_stormPosition - Position) * _size * 0.8f;
                }
            }
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            // Main planet
            Raylib.DrawSphere(Position, _size, _atmosphereColor);
            
            // Atmospheric bands
            if (lodLevel >= LODLevel.High)
            {
                for (int i = 0; i < _bands.Count; i++)
                {
                    var bandY = Position.Y + (_size * 2f / _bands.Count) * (i - _bands.Count / 2f);
                    var bandPosition = new Vector3(Position.X, bandY, Position.Z);
                    Raylib.DrawSphere(bandPosition, _size * 1.01f, _bands[i]);
                }
            }
            
            // Great red spot or storm
            if (_hasStorm && lodLevel >= LODLevel.Medium)
            {
                var stormColor = new Color(255, 100, 100, 200);
                Raylib.DrawSphere(_stormPosition, _size * 0.2f, stormColor);
            }
        }
    }
    
    public class DerelictShip : BackgroundElement
    {
        private ShipSize _shipSize;
        private Color _hullColor;
        private bool _isDamaged;
        private List<Vector3> _debrisField;
        
        public DerelictShip(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.DerelictShip;
            _shipSize = (ShipSize)random.Next(0, 3);
            _hullColor = new Color(80, 80, 80, 255); // Weathered gray
            _isDamaged = random.NextSingle() < 0.8f;
            _debrisField = GenerateDebrisField();
        }
        
        private List<Vector3> GenerateDebrisField()
        {
            var debris = new List<Vector3>();
            if (!_isDamaged) return debris;
            
            int debrisCount = (int)_shipSize + _random.Next(5);
            
            for (int i = 0; i < debrisCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * 20f,
                    (_random.NextSingle() - 0.5f) * 20f,
                    (_random.NextSingle() - 0.5f) * 20f
                );
                debris.Add(Position + offset);
            }
            
            return debris;
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            var shipSize = _shipSize switch
            {
                ShipSize.Fighter => new Vector3(2f, 1f, 4f),
                ShipSize.Cruiser => new Vector3(4f, 2f, 8f),
                ShipSize.Battleship => new Vector3(6f, 3f, 12f),
                _ => new Vector3(3f, 2f, 6f)
            };
            
            // Main hull
            Raylib.DrawCube(Position, shipSize.X, shipSize.Y, shipSize.Z, _hullColor);
            
            // Damage effects
            if (_isDamaged && lodLevel >= LODLevel.High)
            {
                // Hull breach
                var breachColor = new Color(255, 100, 0, 150);
                Raylib.DrawSphere(Position + Vector3.UnitX * shipSize.X * 0.3f, 
                    shipSize.X * 0.2f, breachColor);
                
                // Debris field
                foreach (var debris in _debrisField)
                {
                    Raylib.DrawCube(debris, 0.5f, 0.5f, 0.5f, Color.DarkGray);
                }
            }
        }
    }
    
    public class Quasar : BackgroundElement
    {
        private Color _jetColor;
        private float _intensity;
        private List<Vector3> _jets;
        
        public Quasar(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.Quasar;
            _intensity = random.NextSingle() * 0.8f + 0.2f;
            _jetColor = new Color(100, 200, 255, (byte)(255 * _intensity));
            _jets = GenerateJets();
        }
        
        private List<Vector3> GenerateJets()
        {
            var jets = new List<Vector3>
            {
                Position + Vector3.UnitY * 200f,
                Position - Vector3.UnitY * 200f
            };
            return jets;
        }
        
        public override void Update(float deltaTime, Camera3D camera, Vector3 playerPosition, float globalTimer)
        {
            base.Update(deltaTime, camera, playerPosition, globalTimer);
            
            // Pulsing effect
            var pulse = MathF.Sin(globalTimer * 2f) * 0.3f + 0.7f;
            _jetColor = new Color(
                (byte)(100 * pulse),
                (byte)(200 * pulse),
                (byte)(255 * pulse),
                (byte)(255 * _intensity * pulse)
            );
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.High) return;
            
            // Central black hole
            Raylib.DrawSphere(Position, 2f, Color.Black);
            
            // Accretion disk
            Raylib.DrawCircle3D(Position, 8f, Vector3.UnitY, 0f, 
                new Color(255, 150, 50, 100));
            
            // Jets
            foreach (var jet in _jets)
            {
                Raylib.DrawLine3D(Position, jet, _jetColor);
            }
        }
    }
    
    public class AsteroidCluster : BackgroundElement
    {
        private List<Vector3> _asteroids;
        private List<float> _asteroidSizes;
        
        public AsteroidCluster(Vector3 position, float distance, Random random) : base(position, distance, random)
        {
            Type = BackgroundElementType.AsteroidCluster;
            (_asteroids, _asteroidSizes) = GenerateAsteroids();
        }
        
        private (List<Vector3>, List<float>) GenerateAsteroids()
        {
            var asteroids = new List<Vector3>();
            var sizes = new List<float>();
            int asteroidCount = _random.Next(5, 20);
            
            for (int i = 0; i < asteroidCount; i++)
            {
                var offset = new Vector3(
                    (_random.NextSingle() - 0.5f) * 50f,
                    (_random.NextSingle() - 0.5f) * 50f,
                    (_random.NextSingle() - 0.5f) * 50f
                );
                asteroids.Add(Position + offset);
                sizes.Add(_random.NextSingle() * 2f + 0.5f);
            }
            
            return (asteroids, sizes);
        }
        
        public override void Render(Camera3D camera, LODLevel lodLevel)
        {
            if (lodLevel < LODLevel.Medium) return;
            
            var asteroidColor = new Color(120, 100, 80, 255);
            
            for (int i = 0; i < _asteroids.Count; i++)
            {
                if (lodLevel >= LODLevel.High)
                {
                    Raylib.DrawCube(_asteroids[i], _asteroidSizes[i], _asteroidSizes[i], _asteroidSizes[i], asteroidColor);
                }
                else
                {
                    Raylib.DrawPoint3D(_asteroids[i], asteroidColor);
                }
            }
        }
    }
    
    // Supporting enums
    public enum PlanetType
    {
        Rocky,
        Desert,
        Ocean,
        Ice,
        Gas,
        Lava
    }
    
    public enum StationType
    {
        Research,
        Mining,
        Military,
        Trading
    }
    
    public enum GalaxyType
    {
        Spiral,
        Elliptical,
        Irregular
    }
    
    public enum ShipSize
    {
        Fighter,
        Cruiser,
        Battleship
    }
}