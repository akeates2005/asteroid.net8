using System;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Level of Detail (LOD) management system for performance optimization.
    /// Dynamically adjusts rendering detail based on distance, performance, and entity importance.
    /// </summary>
    public class LODManager
    {
        private readonly GraphicsProfiler _profiler;
        private LODSettings _settings;
        private LODPerformanceMetrics _lastFrameMetrics;

        public LODManager(GraphicsProfiler profiler)
        {
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _settings = new LODSettings();
            _lastFrameMetrics = new LODPerformanceMetrics();
        }

        /// <summary>
        /// Calculate LOD level for an entity based on distance and performance
        /// </summary>
        /// <param name="entityPosition">Entity position</param>
        /// <param name="entityRadius">Entity radius</param>
        /// <param name="viewPosition">Camera/view position</param>
        /// <returns>LOD level (0=highest detail, 2=lowest detail)</returns>
        public int CalculateLOD(Vector2 entityPosition, float entityRadius, Vector2 viewPosition)
        {
            // Calculate distance from view position
            float distance = Vector2.Distance(entityPosition, viewPosition);
            
            // Normalize distance based on entity size
            float normalizedDistance = distance / (entityRadius * _settings.DistanceScale);
            
            // Apply performance-based LOD adjustment
            int performanceLOD = GetPerformanceBasedLOD();
            
            // Determine base LOD from distance
            int distanceLOD = 0;
            if (normalizedDistance < _settings.HighDetailDistance)
            {
                distanceLOD = 0;
            }
            else if (normalizedDistance < _settings.MediumDetailDistance)
            {
                distanceLOD = 1;
            }
            else
            {
                distanceLOD = 2;
            }

            // Return the higher LOD level (lower detail) between distance and performance
            return Math.Max(distanceLOD, performanceLOD);
        }

        /// <summary>
        /// Calculate LOD for asteroids specifically
        /// </summary>
        /// <param name="asteroidPosition">Asteroid position</param>
        /// <param name="asteroidRadius">Asteroid radius</param>
        /// <returns>LOD level for the asteroid</returns>
        public int CalculateAsteroidLOD(Vector2 asteroidPosition, float asteroidRadius)
        {
            Vector2 screenCenter = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
            return CalculateLOD(asteroidPosition, asteroidRadius, screenCenter);
        }

        /// <summary>
        /// Get vertex count for a given LOD level
        /// </summary>
        /// <param name="lodLevel">LOD level</param>
        /// <param name="baseVertexCount">Base vertex count for highest detail</param>
        /// <returns>Adjusted vertex count</returns>
        public int GetVertexCount(int lodLevel, int baseVertexCount)
        {
            return lodLevel switch
            {
                0 => baseVertexCount,                          // Full detail
                1 => Math.Max(6, baseVertexCount * 2 / 3),     // 2/3 vertices
                2 => Math.Max(4, baseVertexCount / 2),         // 1/2 vertices
                _ => Math.Max(3, baseVertexCount / 3)          // Minimum detail
            };
        }

        /// <summary>
        /// Update LOD system with current performance metrics
        /// </summary>
        /// <param name="deltaTime">Frame delta time</param>
        public void Update(float deltaTime)
        {
            UpdatePerformanceMetrics(deltaTime);
            AdaptLODSettings();
        }

        /// <summary>
        /// Configure LOD settings
        /// </summary>
        /// <param name="settings">New LOD settings</param>
        public void ConfigureSettings(LODSettings settings)
        {
            _settings = settings;
            ErrorManager.LogInfo("LOD settings updated");
        }

        /// <summary>
        /// Get current LOD statistics
        /// </summary>
        /// <returns>LOD performance statistics</returns>
        public LODStats GetStats()
        {
            return new LODStats
            {
                HighDetailObjects = GetObjectCountByLOD(0),
                MediumDetailObjects = GetObjectCountByLOD(1),
                LowDetailObjects = GetObjectCountByLOD(2),
                AverageFrameTime = _lastFrameMetrics.AverageFrameTime,
                PerformanceLODBias = GetPerformanceBasedLOD(),
                AdaptiveMode = _settings.EnableAdaptiveLOD
            };
        }

        /// <summary>
        /// Check if an object should be rendered at all (aggressive culling)
        /// </summary>
        /// <param name="position">Object position</param>
        /// <param name="radius">Object radius</param>
        /// <returns>True if object should be rendered</returns>
        public bool ShouldRender(Vector2 position, float radius)
        {
            // Screen bounds check with margin
            float margin = radius * 2;
            bool inBounds = position.X >= -margin && position.X <= GameConstants.SCREEN_WIDTH + margin &&
                           position.Y >= -margin && position.Y <= GameConstants.SCREEN_HEIGHT + margin;

            if (!inBounds) return false;

            // Skip tiny objects when performance is poor
            if (GetPerformanceBasedLOD() >= 2 && radius < _settings.MinRenderRadius)
            {
                return false;
            }

            return true;
        }

        private int GetPerformanceBasedLOD()
        {
            if (!_settings.EnableAdaptiveLOD) return 0;

            float targetFrameTime = 1000f / GameConstants.TARGET_FPS; // ~16.67ms for 60 FPS
            
            if (_lastFrameMetrics.AverageFrameTime > targetFrameTime * 1.5f)
            {
                return 2; // Poor performance, use lowest detail
            }
            else if (_lastFrameMetrics.AverageFrameTime > targetFrameTime * 1.2f)
            {
                return 1; // Moderate performance, use medium detail
            }
            
            return 0; // Good performance, use high detail
        }

        private void UpdatePerformanceMetrics(float deltaTime)
        {
            float frameTimeMs = deltaTime * 1000f;
            
            // Rolling average over last 10 frames
            _lastFrameMetrics.AverageFrameTime = 
                (_lastFrameMetrics.AverageFrameTime * 9f + frameTimeMs) / 10f;
            
            _lastFrameMetrics.CurrentFPS = 1f / deltaTime;
            _lastFrameMetrics.FrameCount++;
        }

        private void AdaptLODSettings()
        {
            if (!_settings.EnableAdaptiveLOD) return;

            // Adapt distance thresholds based on performance
            int performanceLOD = GetPerformanceBasedLOD();
            
            if (performanceLOD >= 2)
            {
                // Poor performance - be more aggressive with LOD
                _settings.HighDetailDistance *= 0.8f;
                _settings.MediumDetailDistance *= 0.9f;
            }
            else if (performanceLOD == 0)
            {
                // Good performance - can afford more detail
                _settings.HighDetailDistance = Math.Min(1.2f, _settings.HighDetailDistance * 1.01f);
                _settings.MediumDetailDistance = Math.Min(2.0f, _settings.MediumDetailDistance * 1.005f);
            }
        }

        private int GetObjectCountByLOD(int lodLevel)
        {
            // This would be implemented by tracking objects by LOD in a real system
            // For now, return estimated counts
            return lodLevel switch
            {
                0 => _lastFrameMetrics.HighDetailCount,
                1 => _lastFrameMetrics.MediumDetailCount,
                2 => _lastFrameMetrics.LowDetailCount,
                _ => 0
            };
        }
    }

    /// <summary>
    /// LOD system configuration
    /// </summary>
    public struct LODSettings
    {
        public float HighDetailDistance { get; set; }
        public float MediumDetailDistance { get; set; }
        public float DistanceScale { get; set; }
        public float MinRenderRadius { get; set; }
        public bool EnableAdaptiveLOD { get; set; }

        public LODSettings()
        {
            HighDetailDistance = 1.0f;
            MediumDetailDistance = 2.5f;
            DistanceScale = 1.0f;
            MinRenderRadius = 2.0f;
            EnableAdaptiveLOD = true;
        }
    }

    /// <summary>
    /// LOD performance statistics
    /// </summary>
    public struct LODStats
    {
        public int HighDetailObjects { get; set; }
        public int MediumDetailObjects { get; set; }
        public int LowDetailObjects { get; set; }
        public float AverageFrameTime { get; set; }
        public int PerformanceLODBias { get; set; }
        public bool AdaptiveMode { get; set; }
    }

    /// <summary>
    /// Performance metrics for LOD calculations
    /// </summary>
    public struct LODPerformanceMetrics
    {
        public float AverageFrameTime { get; set; }
        public float CurrentFPS { get; set; }
        public int FrameCount { get; set; }
        public int HighDetailCount { get; set; }
        public int MediumDetailCount { get; set; }
        public int LowDetailCount { get; set; }
    }
}