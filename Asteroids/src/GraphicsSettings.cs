using System;
using System.Numerics;
using System.Text.Json.Serialization;
using Raylib_cs;

namespace Asteroids
{
    public enum GraphicsQuality
    {
        Potato,    // 30 FPS target, minimal effects
        Low,       // 60 FPS, basic particles  
        Medium,    // 60 FPS, enhanced effects
        High,      // 60 FPS, all effects
        Ultra      // 60+ FPS, maximum quality
    }

    public class GraphicsSettings
    {
        [JsonPropertyName("quality")]
        public GraphicsQuality Quality { get; set; } = GraphicsQuality.High;

        [JsonPropertyName("particleDensity")]
        public float ParticleDensity { get; set; } = 1.0f;

        [JsonPropertyName("effectsIntensity")]
        public float EffectsIntensity { get; set; } = 1.0f;

        [JsonPropertyName("enableShaders")]
        public bool EnableShaders { get; set; } = true;

        [JsonPropertyName("enableParallax")]
        public bool EnableParallax { get; set; } = true;

        [JsonPropertyName("enableScreenEffects")]
        public bool EnableScreenEffects { get; set; } = true;

        [JsonPropertyName("enableParticleTrails")]
        public bool EnableParticleTrails { get; set; } = true;

        [JsonPropertyName("enableAnimatedHUD")]
        public bool EnableAnimatedHUD { get; set; } = true;

        [JsonPropertyName("enableDynamicColors")]
        public bool EnableDynamicColors { get; set; } = true;

        [JsonPropertyName("maxParticles")]
        public int MaxParticles { get; set; } = 500;

        [JsonPropertyName("targetFrameRate")]
        public int TargetFrameRate { get; set; } = 60;

        [JsonPropertyName("adaptiveQuality")]
        public bool AdaptiveQuality { get; set; } = true;

        [JsonPropertyName("showPerformanceOverlay")]
        public bool ShowPerformanceOverlay { get; set; } = false;

        public void ApplyQualityPreset(GraphicsQuality quality)
        {
            Quality = quality;
            
            switch (quality)
            {
                case GraphicsQuality.Potato:
                    ParticleDensity = 0.2f;
                    EffectsIntensity = 0.3f;
                    EnableShaders = false;
                    EnableParallax = false;
                    EnableScreenEffects = false;
                    EnableParticleTrails = false;
                    EnableAnimatedHUD = false;
                    EnableDynamicColors = false;
                    MaxParticles = 50;
                    TargetFrameRate = 30;
                    break;

                case GraphicsQuality.Low:
                    ParticleDensity = 0.5f;
                    EffectsIntensity = 0.5f;
                    EnableShaders = false;
                    EnableParallax = false;
                    EnableScreenEffects = true;
                    EnableParticleTrails = false;
                    EnableAnimatedHUD = true;
                    EnableDynamicColors = true;
                    MaxParticles = 150;
                    TargetFrameRate = 60;
                    break;

                case GraphicsQuality.Medium:
                    ParticleDensity = 0.7f;
                    EffectsIntensity = 0.7f;
                    EnableShaders = true;
                    EnableParallax = true;
                    EnableScreenEffects = true;
                    EnableParticleTrails = true;
                    EnableAnimatedHUD = true;
                    EnableDynamicColors = true;
                    MaxParticles = 300;
                    TargetFrameRate = 60;
                    break;

                case GraphicsQuality.High:
                    ParticleDensity = 1.0f;
                    EffectsIntensity = 1.0f;
                    EnableShaders = true;
                    EnableParallax = true;
                    EnableScreenEffects = true;
                    EnableParticleTrails = true;
                    EnableAnimatedHUD = true;
                    EnableDynamicColors = true;
                    MaxParticles = 500;
                    TargetFrameRate = 60;
                    break;

                case GraphicsQuality.Ultra:
                    ParticleDensity = 1.5f;
                    EffectsIntensity = 1.5f;
                    EnableShaders = true;
                    EnableParallax = true;
                    EnableScreenEffects = true;
                    EnableParticleTrails = true;
                    EnableAnimatedHUD = true;
                    EnableDynamicColors = true;
                    MaxParticles = 1000;
                    TargetFrameRate = 60;
                    break;
            }
        }

        public int GetScaledParticleCount(int baseCount)
        {
            return Math.Max(1, (int)(baseCount * ParticleDensity));
        }

        public float GetScaledEffectIntensity(float baseIntensity)
        {
            return baseIntensity * EffectsIntensity;
        }

        public bool ShouldSkipEffect(float effectCost)
        {
            // Skip expensive effects on lower quality settings
            return Quality switch
            {
                GraphicsQuality.Potato => effectCost > 0.1f,
                GraphicsQuality.Low => effectCost > 0.3f,
                GraphicsQuality.Medium => effectCost > 0.7f,
                _ => false
            };
        }

        public void AutoDetectQuality()
        {
            // Simple auto-detection based on initial performance
            // This would ideally be more sophisticated in a real implementation
            int fps = Raylib.GetFPS();
            
            if (fps < 20)
                ApplyQualityPreset(GraphicsQuality.Potato);
            else if (fps < 40)
                ApplyQualityPreset(GraphicsQuality.Low);
            else if (fps < 55)
                ApplyQualityPreset(GraphicsQuality.Medium);
            else if (fps < 90)
                ApplyQualityPreset(GraphicsQuality.High);
            else
                ApplyQualityPreset(GraphicsQuality.Ultra);
        }
    }

    public class GraphicsProfiler
    {
        private float _particleRenderTime;
        private float _shaderRenderTime;
        private float _backgroundRenderTime;
        private float _hudRenderTime;
        private float _effectsRenderTime;
        
        private int _activeParticles;
        private int _activeEffects;
        private int _drawCalls;
        
        private float _frameTime;
        private float _averageFrameTime;
        private int _frameCount;
        
        private float _memoryUsage;
        private float _gpuMemoryUsage;
        
        private readonly float[] _frameTimeHistory = new float[60]; // 1 second of history at 60fps
        private int _frameHistoryIndex;
        
        private double _particleTimer;
        private double _shaderTimer;
        private double _backgroundTimer;
        private double _hudTimer;
        private double _effectsTimer;

        public void BeginFrame()
        {
            _frameTime = Raylib.GetFrameTime();
            _frameTimeHistory[_frameHistoryIndex] = _frameTime;
            _frameHistoryIndex = (_frameHistoryIndex + 1) % _frameTimeHistory.Length;
            
            // Calculate rolling average
            float sum = 0;
            for (int i = 0; i < _frameTimeHistory.Length; i++)
                sum += _frameTimeHistory[i];
            _averageFrameTime = sum / _frameTimeHistory.Length;
            
            _frameCount++;
            _drawCalls = 0;
            
            // Estimate memory usage (simplified)
            _memoryUsage = GC.GetTotalMemory(false) / (1024.0f * 1024.0f); // MB
        }

        public void BeginParticleRender()
        {
            _particleTimer = Raylib.GetTime();
        }

        public void EndParticleRender(int particleCount)
        {
            _particleRenderTime = (float)(Raylib.GetTime() - _particleTimer) * 1000; // Convert to ms
            _activeParticles = particleCount;
            _drawCalls++;
        }

        public void BeginShaderRender()
        {
            _shaderTimer = Raylib.GetTime();
        }

        public void EndShaderRender()
        {
            _shaderRenderTime = (float)(Raylib.GetTime() - _shaderTimer) * 1000;
            _drawCalls++;
        }

        public void BeginBackgroundRender()
        {
            _backgroundTimer = Raylib.GetTime();
        }

        public void EndBackgroundRender()
        {
            _backgroundRenderTime = (float)(Raylib.GetTime() - _backgroundTimer) * 1000;
            _drawCalls++;
        }

        public void BeginHUDRender()
        {
            _hudTimer = Raylib.GetTime();
        }

        public void EndHUDRender()
        {
            _hudRenderTime = (float)(Raylib.GetTime() - _hudTimer) * 1000;
            _drawCalls++;
        }

        public void BeginEffectsRender()
        {
            _effectsTimer = Raylib.GetTime();
        }

        public void EndEffectsRender(int effectCount)
        {
            _effectsRenderTime = (float)(Raylib.GetTime() - _effectsTimer) * 1000;
            _activeEffects = effectCount;
            _drawCalls++;
        }

        public void DrawPerformanceOverlay(GraphicsSettings settings)
        {
            if (!settings.ShowPerformanceOverlay && !Raylib.IsKeyDown(KeyboardKey.F12))
                return;

            Vector2 overlayPos = new(10, Raylib.GetScreenHeight() - 180);
            Color overlayBg = new(0, 0, 0, 180);
            Color textColor = Color.White;
            
            // Background
            Raylib.DrawRectangle((int)overlayPos.X - 5, (int)overlayPos.Y - 5, 300, 175, overlayBg);
            
            // Title
            Raylib.DrawTextEx(Raylib.GetFontDefault(), "PERFORMANCE MONITOR", overlayPos, 14, 1, Color.Yellow);
            overlayPos.Y += 20;
            
            // FPS and frame time
            string fpsText = $"FPS: {Raylib.GetFPS():000} ({_averageFrameTime * 1000:F1}ms avg)";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), fpsText, overlayPos, 12, 1, GetPerformanceColor(Raylib.GetFPS(), 60));
            overlayPos.Y += 15;
            
            // Render times
            string particleText = $"Particles: {_particleRenderTime:F2}ms ({_activeParticles} active)";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), particleText, overlayPos, 12, 1, textColor);
            overlayPos.Y += 15;
            
            string effectsText = $"Effects: {_effectsRenderTime:F2}ms ({_activeEffects} active)";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), effectsText, overlayPos, 12, 1, textColor);
            overlayPos.Y += 15;
            
            string hudText = $"HUD: {_hudRenderTime:F2}ms";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), hudText, overlayPos, 12, 1, textColor);
            overlayPos.Y += 15;
            
            if (settings.EnableShaders)
            {
                string shaderText = $"Shaders: {_shaderRenderTime:F2}ms";
                Raylib.DrawTextEx(Raylib.GetFontDefault(), shaderText, overlayPos, 12, 1, textColor);
                overlayPos.Y += 15;
            }
            
            // Memory usage
            string memoryText = $"Memory: {_memoryUsage:F1}MB";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), memoryText, overlayPos, 12, 1, GetMemoryColor(_memoryUsage));
            overlayPos.Y += 15;
            
            // Quality settings
            string qualityText = $"Quality: {settings.Quality}";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), qualityText, overlayPos, 12, 1, textColor);
            overlayPos.Y += 15;
            
            // Draw calls
            string drawCallText = $"Draw Calls: {_drawCalls}";
            Raylib.DrawTextEx(Raylib.GetFontDefault(), drawCallText, overlayPos, 12, 1, textColor);
        }

        private Color GetPerformanceColor(int fps, int target)
        {
            float ratio = (float)fps / target;
            if (ratio >= 0.9f) return Color.Green;
            if (ratio >= 0.7f) return Color.Yellow;
            return Color.Red;
        }

        private Color GetMemoryColor(float memoryMB)
        {
            if (memoryMB < 100) return Color.Green;
            if (memoryMB < 200) return Color.Yellow;
            return Color.Red;
        }

        public PerformanceMetrics GetMetrics()
        {
            return new PerformanceMetrics
            {
                FPS = Raylib.GetFPS(),
                FrameTime = _frameTime,
                AverageFrameTime = _averageFrameTime,
                ParticleRenderTime = _particleRenderTime,
                EffectsRenderTime = _effectsRenderTime,
                HUDRenderTime = _hudRenderTime,
                ShaderRenderTime = _shaderRenderTime,
                ActiveParticles = _activeParticles,
                ActiveEffects = _activeEffects,
                MemoryUsage = _memoryUsage,
                DrawCalls = _drawCalls
            };
        }

        public bool ShouldReduceQuality()
        {
            // Suggest quality reduction if performance is consistently poor
            int lowFpsFrames = 0;
            for (int i = 0; i < _frameTimeHistory.Length; i++)
            {
                if (1.0f / _frameTimeHistory[i] < 45) // Below 45 FPS
                    lowFpsFrames++;
            }
            
            return lowFpsFrames > _frameTimeHistory.Length * 0.5f; // More than 50% of frames below target
        }

        public bool ShouldIncreaseQuality()
        {
            // Suggest quality increase if performance is consistently excellent
            int highFpsFrames = 0;
            for (int i = 0; i < _frameTimeHistory.Length; i++)
            {
                if (1.0f / _frameTimeHistory[i] > 75) // Above 75 FPS
                    highFpsFrames++;
            }
            
            return highFpsFrames > _frameTimeHistory.Length * 0.8f; // More than 80% of frames above target
        }
    }

    public struct PerformanceMetrics
    {
        public int FPS { get; set; }
        public float FrameTime { get; set; }
        public float AverageFrameTime { get; set; }
        public float ParticleRenderTime { get; set; }
        public float EffectsRenderTime { get; set; }
        public float HUDRenderTime { get; set; }
        public float ShaderRenderTime { get; set; }
        public int ActiveParticles { get; set; }
        public int ActiveEffects { get; set; }
        public float MemoryUsage { get; set; }
        public int DrawCalls { get; set; }
    }

    public class AdaptiveGraphicsManager
    {
        private readonly GraphicsSettings _settings;
        private readonly GraphicsProfiler _profiler;
        
        private float _qualityAdjustmentTimer = 0;
        private const float QUALITY_CHECK_INTERVAL = 5.0f; // Check every 5 seconds
        
        public AdaptiveGraphicsManager(GraphicsSettings settings, GraphicsProfiler profiler)
        {
            _settings = settings;
            _profiler = profiler;
        }

        public void Update(float deltaTime)
        {
            if (!_settings.AdaptiveQuality) return;

            _qualityAdjustmentTimer += deltaTime;
            if (_qualityAdjustmentTimer >= QUALITY_CHECK_INTERVAL)
            {
                _qualityAdjustmentTimer = 0;
                AdjustQualityIfNeeded();
            }
        }

        private void AdjustQualityIfNeeded()
        {
            if (_profiler.ShouldReduceQuality() && _settings.Quality > GraphicsQuality.Potato)
            {
                var newQuality = (GraphicsQuality)((int)_settings.Quality - 1);
                _settings.ApplyQualityPreset(newQuality);
                ErrorManager.LogInfo($"Adaptive graphics: Reduced quality to {newQuality}");
            }
            else if (_profiler.ShouldIncreaseQuality() && _settings.Quality < GraphicsQuality.Ultra)
            {
                var newQuality = (GraphicsQuality)((int)_settings.Quality + 1);
                _settings.ApplyQualityPreset(newQuality);
                ErrorManager.LogInfo($"Adaptive graphics: Increased quality to {newQuality}");
            }
        }
    }
}