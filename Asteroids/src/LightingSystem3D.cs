using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Advanced 3D lighting system with multiple light types, shadows, and real-time calculation
    /// </summary>
    public class LightingSystem3D
    {
        private readonly List<Light3D> _lights;
        private readonly List<ShadowCaster3D> _shadowCasters;
        private Color _ambientLight;
        private float _ambientIntensity;
        private bool _shadowsEnabled;
        private bool _isInitialized;
        private readonly Dictionary<Light3D, Shader> _lightShaders;
        
        // Performance optimization
        private readonly Dictionary<Vector3, LightingResult> _lightingCache;
        private float _cacheTimeout = 0.1f; // Cache lighting for 100ms
        private readonly Dictionary<Vector3, DateTime> _cacheTimestamps;
        
        public LightingSystem3D()
        {
            _lights = new List<Light3D>();
            _shadowCasters = new List<ShadowCaster3D>();
            _ambientLight = new Color(30, 30, 50, 255);
            _ambientIntensity = 0.2f;
            _shadowsEnabled = false;
            _lightShaders = new Dictionary<Light3D, Shader>();
            _lightingCache = new Dictionary<Vector3, LightingResult>();
            _cacheTimestamps = new Dictionary<Vector3, DateTime>();
        }
        
        public bool Initialize()
        {
            try
            {
                // Initialize lighting system
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize LightingSystem3D", ex);
                return false;
            }
        }
        
        public void AddLight(Light3D light)
        {
            if (!_lights.Contains(light))
            {
                _lights.Add(light);
                InvalidateLightingCache();
            }
        }
        
        public void RemoveLight(Light3D light)
        {
            if (_lights.Remove(light))
            {
                if (_lightShaders.ContainsKey(light))
                {
                    _lightShaders.Remove(light);
                }
                InvalidateLightingCache();
            }
        }
        
        public void UpdateLight(Light3D light)
        {
            // Light properties changed, invalidate cache
            InvalidateLightingCache();
        }
        
        public void AddShadowCaster(ShadowCaster3D caster)
        {
            if (!_shadowCasters.Contains(caster))
            {
                _shadowCasters.Add(caster);
                InvalidateLightingCache();
            }
        }
        
        public void RemoveShadowCaster(ShadowCaster3D caster)
        {
            if (_shadowCasters.Remove(caster))
            {
                InvalidateLightingCache();
            }
        }
        
        public void SetAmbientLight(Color color, float intensity)
        {
            _ambientLight = color;
            _ambientIntensity = intensity;
            InvalidateLightingCache();
        }
        
        public void EnableShadows(bool enabled)
        {
            _shadowsEnabled = enabled;
            InvalidateLightingCache();
        }
        
        public LightingResult CalculateLighting(Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            if (!_isInitialized) 
            {
                return new LightingResult 
                { 
                    Color = _ambientLight, 
                    Intensity = _ambientIntensity,
                    IsShadowed = false
                };
            }
            
            // Check cache first
            var cacheKey = new Vector3(
                MathF.Round(surfacePoint.X, 1),
                MathF.Round(surfacePoint.Y, 1), 
                MathF.Round(surfacePoint.Z, 1)
            ); // Round to 1 decimal for caching
            if (_lightingCache.ContainsKey(cacheKey) && 
                _cacheTimestamps.ContainsKey(cacheKey) &&
                DateTime.Now - _cacheTimestamps[cacheKey] < TimeSpan.FromSeconds(_cacheTimeout))
            {
                return _lightingCache[cacheKey];
            }
            
            var result = CalculateLightingInternal(surfacePoint, surfaceNormal);
            
            // Update cache
            _lightingCache[cacheKey] = result;
            _cacheTimestamps[cacheKey] = DateTime.Now;
            
            return result;
        }
        
        private LightingResult CalculateLightingInternal(Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            // Start with ambient lighting
            Vector3 totalLight = new Vector3(_ambientLight.R, _ambientLight.G, _ambientLight.B) * _ambientIntensity;
            float totalIntensity = _ambientIntensity;
            bool isShadowed = false;
            
            foreach (var light in _lights)
            {
                if (!light.IsEnabled) continue;
                
                var lightContribution = CalculateLightContribution(light, surfacePoint, surfaceNormal);
                
                if (lightContribution.Intensity > 0)
                {
                    // Check for shadows if enabled
                    bool lightShadowed = false;
                    if (_shadowsEnabled)
                    {
                        lightShadowed = IsInShadow(surfacePoint, light);
                    }
                    
                    if (lightShadowed)
                    {
                        lightContribution.Intensity *= 0.1f; // Heavily attenuate shadowed areas
                        isShadowed = true;
                    }
                    
                    // Combine light contributions
                    Vector3 lightColor = new Vector3(light.Color.R, light.Color.G, light.Color.B) / 255.0f;
                    totalLight += lightColor * lightContribution.Intensity;
                    totalIntensity += lightContribution.Intensity;
                }
            }
            
            // Clamp values
            totalLight = Vector3.Clamp(totalLight, Vector3.Zero, Vector3.One * 255.0f);
            totalIntensity = Math.Clamp(totalIntensity, 0.0f, 1.0f);
            
            return new LightingResult
            {
                Color = new Color((int)totalLight.X, (int)totalLight.Y, (int)totalLight.Z, 255),
                Intensity = totalIntensity,
                IsShadowed = isShadowed
            };
        }
        
        private LightContribution CalculateLightContribution(Light3D light, Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            switch (light.Type)
            {
                case LightType.Directional:
                    return CalculateDirectionalLight(light, surfacePoint, surfaceNormal);
                    
                case LightType.Point:
                    return CalculatePointLight(light, surfacePoint, surfaceNormal);
                    
                case LightType.Spot:
                    return CalculateSpotLight(light, surfacePoint, surfaceNormal);
                    
                default:
                    return new LightContribution { Intensity = 0.0f };
            }
        }
        
        private LightContribution CalculateDirectionalLight(Light3D light, Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            Vector3 lightDirection = Vector3.Normalize(-light.Direction);
            float dotProduct = Vector3.Dot(surfaceNormal, lightDirection);
            float intensity = Math.Max(0.0f, dotProduct) * light.Intensity;
            
            return new LightContribution { Intensity = intensity };
        }
        
        private LightContribution CalculatePointLight(Light3D light, Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            Vector3 lightDirection = Vector3.Normalize(light.Position - surfacePoint);
            float distance = Vector3.Distance(light.Position, surfacePoint);
            
            // Early exit if beyond range
            if (distance > light.Range) return new LightContribution { Intensity = 0.0f };
            
            float dotProduct = Vector3.Dot(surfaceNormal, lightDirection);
            if (dotProduct <= 0.0f) return new LightContribution { Intensity = 0.0f };
            
            // Quadratic attenuation
            float attenuation = 1.0f / (1.0f + 0.045f * distance + 0.0075f * distance * distance);
            
            // Range falloff
            float rangeFalloff = Math.Max(0.0f, 1.0f - (distance / light.Range));
            rangeFalloff = rangeFalloff * rangeFalloff; // Quadratic falloff
            
            float intensity = dotProduct * light.Intensity * attenuation * rangeFalloff;
            
            return new LightContribution { Intensity = intensity };
        }
        
        private LightContribution CalculateSpotLight(Light3D light, Vector3 surfacePoint, Vector3 surfaceNormal)
        {
            Vector3 lightToSurface = Vector3.Normalize(surfacePoint - light.Position);
            Vector3 spotDirection = Vector3.Normalize(light.Direction);
            
            float spotAngle = Vector3.Dot(lightToSurface, spotDirection);
            float coneAngleRad = light.ConeAngle * MathF.PI / 180.0f;
            float minCos = MathF.Cos(coneAngleRad * 0.5f);
            
            // Early exit if outside cone
            if (spotAngle < minCos) return new LightContribution { Intensity = 0.0f };
            
            // Calculate base point light contribution
            var baseContribution = CalculatePointLight(light, surfacePoint, surfaceNormal);
            
            // Apply cone falloff
            float coneFalloff = (spotAngle - minCos) / (1.0f - minCos);
            coneFalloff = MathF.Pow(coneFalloff, light.SpotFalloff);
            
            return new LightContribution 
            { 
                Intensity = baseContribution.Intensity * coneFalloff 
            };
        }
        
        private bool IsInShadow(Vector3 surfacePoint, Light3D light)
        {
            Vector3 lightDirection;
            float maxDistance;
            
            switch (light.Type)
            {
                case LightType.Directional:
                    lightDirection = Vector3.Normalize(-light.Direction);
                    maxDistance = 1000.0f; // Large distance for directional lights
                    break;
                    
                case LightType.Point:
                case LightType.Spot:
                    lightDirection = Vector3.Normalize(light.Position - surfacePoint);
                    maxDistance = Vector3.Distance(light.Position, surfacePoint);
                    break;
                    
                default:
                    return false;
            }
            
            // Simple shadow ray casting
            foreach (var caster in _shadowCasters)
            {
                if (RayIntersectsSphere(surfacePoint, lightDirection, caster.Position, caster.Radius, maxDistance))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool RayIntersectsSphere(Vector3 rayOrigin, Vector3 rayDirection, Vector3 sphereCenter, float sphereRadius, float maxDistance)
        {
            Vector3 toSphere = sphereCenter - rayOrigin;
            float projectionLength = Vector3.Dot(toSphere, rayDirection);
            
            // Sphere is behind ray
            if (projectionLength < 0) return false;
            
            // Beyond max distance
            if (projectionLength > maxDistance) return false;
            
            Vector3 closestPoint = rayOrigin + rayDirection * projectionLength;
            float distanceToCenter = Vector3.Distance(closestPoint, sphereCenter);
            
            return distanceToCenter <= sphereRadius;
        }
        
        private void InvalidateLightingCache()
        {
            _lightingCache.Clear();
            _cacheTimestamps.Clear();
        }
        
        public Color GetAmbientLight() => _ambientLight;
        public int GetLightCount() => _lights.Count;
        public bool IsInitialized => _isInitialized;
        
        public void Update(float deltaTime)
        {
            // Clean up old cache entries
            var now = DateTime.Now;
            var expiredKeys = new List<Vector3>();
            
            foreach (var kvp in _cacheTimestamps)
            {
                if (now - kvp.Value > TimeSpan.FromSeconds(_cacheTimeout * 2))
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                _lightingCache.Remove(key);
                _cacheTimestamps.Remove(key);
            }
        }
        
        public void Cleanup()
        {
            _lights.Clear();
            _shadowCasters.Clear();
            _lightShaders.Clear();
            _lightingCache.Clear();
            _cacheTimestamps.Clear();
            _isInitialized = false;
        }
    }
    
    /// <summary>
    /// 3D light definition with multiple types and properties
    /// </summary>
    public class Light3D
    {
        public LightType Type { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Color Color { get; set; } = Color.White;
        public float Intensity { get; set; } = 1.0f;
        public float Range { get; set; } = 10.0f;
        public float ConeAngle { get; set; } = 45.0f; // For spot lights
        public float SpotFalloff { get; set; } = 1.0f; // For spot lights
        public bool IsEnabled { get; set; } = true;
        public bool CastShadows { get; set; } = true;
        
        public override bool Equals(object obj)
        {
            return obj is Light3D other && 
                   Type == other.Type &&
                   Position == other.Position &&
                   Direction == other.Direction &&
                   Intensity == other.Intensity &&
                   Range == other.Range;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Position, Direction, Intensity, Range);
        }
    }
    
    /// <summary>
    /// Types of 3D lights
    /// </summary>
    public enum LightType
    {
        Directional,
        Point,
        Spot
    }
    
    /// <summary>
    /// Result of lighting calculation
    /// </summary>
    public struct LightingResult
    {
        public Color Color;
        public float Intensity;
        public bool IsShadowed;
    }
    
    /// <summary>
    /// Individual light contribution calculation result
    /// </summary>
    public struct LightContribution
    {
        public float Intensity;
    }
    
    /// <summary>
    /// Shadow casting object
    /// </summary>
    public struct ShadowCaster3D
    {
        public Vector3 Position;
        public float Radius;
        
        public override bool Equals(object obj)
        {
            return obj is ShadowCaster3D other &&
                   Position == other.Position &&
                   Radius == other.Radius;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Radius);
        }
    }
}