using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Generic entity pooling system for improved memory management and performance.
    /// Reduces garbage collection pressure by reusing entity instances.
    /// </summary>
    /// <typeparam name="T">Entity type to pool</typeparam>
    public class EntityPool<T> where T : class, IGameEntity
    {
        private readonly Queue<T> _availableEntities;
        private readonly HashSet<T> _activeEntities;
        private readonly Func<int, T> _entityFactory;
        private readonly int _maxPoolSize;
        private int _nextEntityId;

        public EntityPool(Func<int, T> entityFactory, int initialSize = 50, int maxSize = 500)
        {
            _entityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
            _maxPoolSize = maxSize;
            _nextEntityId = 1;
            
            _availableEntities = new Queue<T>(initialSize);
            _activeEntities = new HashSet<T>();
            
            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                var entity = _entityFactory(_nextEntityId++);
                entity.Active = false;
                _availableEntities.Enqueue(entity);
            }
            
            ErrorManager.LogInfo($"EntityPool<{typeof(T).Name}> created with {initialSize} pre-allocated entities");
        }

        /// <summary>
        /// Get an entity from the pool or create a new one
        /// </summary>
        /// <returns>Available entity instance</returns>
        public T GetEntity()
        {
            T entity;
            
            if (_availableEntities.Count > 0)
            {
                entity = _availableEntities.Dequeue();
            }
            else if (_activeEntities.Count + _availableEntities.Count < _maxPoolSize)
            {
                entity = _entityFactory(_nextEntityId++);
                ErrorManager.LogInfo($"EntityPool<{typeof(T).Name}> created new entity {entity.Id}");
            }
            else
            {
                // Pool is at capacity, force reuse oldest active entity
                entity = _activeEntities.First();
                _activeEntities.Remove(entity);
                entity.Dispose();
                entity = _entityFactory(_nextEntityId++);
                ErrorManager.LogWarning($"EntityPool<{typeof(T).Name}> at capacity, forced reuse");
            }
            
            entity.Active = true;
            _activeEntities.Add(entity);
            entity.Initialize();
            
            return entity;
        }

        /// <summary>
        /// Return an entity to the pool
        /// </summary>
        /// <param name="entity">Entity to return</param>
        public void ReturnEntity(T entity)
        {
            if (entity == null) return;
            
            if (_activeEntities.Remove(entity))
            {
                entity.Active = false;
                entity.Dispose();
                _availableEntities.Enqueue(entity);
            }
        }

        /// <summary>
        /// Get all currently active entities
        /// </summary>
        /// <returns>Collection of active entities</returns>
        public IReadOnlyCollection<T> GetActiveEntities()
        {
            return _activeEntities;
        }

        /// <summary>
        /// Clear all entities and reset pool
        /// </summary>
        public void Clear()
        {
            foreach (var entity in _activeEntities)
            {
                entity.Dispose();
            }
            
            foreach (var entity in _availableEntities)
            {
                entity.Dispose();
            }
            
            _activeEntities.Clear();
            _availableEntities.Clear();
            
            ErrorManager.LogInfo($"EntityPool<{typeof(T).Name}> cleared");
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        /// <returns>Pool usage statistics</returns>
        public EntityPoolStats GetStats()
        {
            return new EntityPoolStats
            {
                ActiveCount = _activeEntities.Count,
                AvailableCount = _availableEntities.Count,
                TotalAllocated = _activeEntities.Count + _availableEntities.Count,
                MaxPoolSize = _maxPoolSize,
                UtilizationPercent = (_activeEntities.Count / (float)_maxPoolSize) * 100f
            };
        }

        /// <summary>
        /// Compact pool by removing excess unused entities
        /// </summary>
        /// <param name="targetAvailable">Target number of available entities to keep</param>
        public void Compact(int targetAvailable = 25)
        {
            int excess = _availableEntities.Count - targetAvailable;
            if (excess <= 0) return;
            
            for (int i = 0; i < excess; i++)
            {
                var entity = _availableEntities.Dequeue();
                entity.Dispose();
            }
            
            ErrorManager.LogInfo($"EntityPool<{typeof(T).Name}> compacted, removed {excess} unused entities");
        }
    }

    /// <summary>
    /// Enhanced adaptive graphics manager with dynamic quality adjustment
    /// </summary>
    public class EnhancedAdaptiveGraphicsManager
    {
        private readonly GraphicsSettings _graphicsSettings;
        private readonly GraphicsProfiler _profiler;
        private readonly LODManager _lodManager;
        private QualityLevel _currentQuality;
        private QualityLevel _targetQuality;
        private float _qualityTransitionTimer;
        private PerformanceHistory _performanceHistory;

        private const float QUALITY_TRANSITION_TIME = 2f; // Seconds to transition between quality levels
        private const int PERFORMANCE_HISTORY_SIZE = 120; // 2 seconds at 60 FPS

        public EnhancedAdaptiveGraphicsManager(GraphicsSettings graphicsSettings, GraphicsProfiler profiler)
        {
            _graphicsSettings = graphicsSettings ?? throw new ArgumentNullException(nameof(graphicsSettings));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _lodManager = new LODManager(profiler);
            
            _currentQuality = QualityLevel.High;
            _targetQuality = QualityLevel.High;
            _qualityTransitionTimer = 0f;
            _performanceHistory = new PerformanceHistory(PERFORMANCE_HISTORY_SIZE);
            
            ErrorManager.LogInfo("Enhanced adaptive graphics manager initialized");
        }

        /// <summary>
        /// Update adaptive graphics system
        /// </summary>
        /// <param name="deltaTime">Frame delta time</param>
        public void Update(float deltaTime)
        {
            UpdatePerformanceHistory(deltaTime);
            UpdateTargetQuality();
            UpdateQualityTransition(deltaTime);
            _lodManager.Update(deltaTime);
        }

        /// <summary>
        /// Get current quality level
        /// </summary>
        /// <returns>Current quality level</returns>
        public QualityLevel GetCurrentQuality()
        {
            return _currentQuality;
        }

        /// <summary>
        /// Get LOD manager for distance-based detail reduction
        /// </summary>
        /// <returns>LOD manager instance</returns>
        public LODManager GetLODManager()
        {
            return _lodManager;
        }

        /// <summary>
        /// Get adaptive graphics statistics
        /// </summary>
        /// <returns>Performance and adaptation statistics</returns>
        public AdaptiveGraphicsStats GetStats()
        {
            var perfStats = _performanceHistory.GetStats();
            
            return new AdaptiveGraphicsStats
            {
                CurrentQuality = _currentQuality,
                TargetQuality = _targetQuality,
                AverageFPS = perfStats.AverageFPS,
                MinFPS = perfStats.MinFPS,
                MaxFPS = perfStats.MaxFPS,
                FrameTimeVariance = perfStats.FrameTimeVariance,
                QualityTransitionProgress = _qualityTransitionTimer / QUALITY_TRANSITION_TIME
            };
        }

        /// <summary>
        /// Apply current quality settings to graphics configuration
        /// </summary>
        public void ApplyQualitySettings()
        {
            switch (_currentQuality)
            {
                case QualityLevel.Low:
                    _graphicsSettings.MaxParticles = 250;
                    _graphicsSettings.EnableParticleTrails = false;
                    _graphicsSettings.EnableScreenEffects = false;
                    break;
                    
                case QualityLevel.Medium:
                    _graphicsSettings.MaxParticles = 500;
                    _graphicsSettings.EnableParticleTrails = true;
                    _graphicsSettings.EnableScreenEffects = false;
                    break;
                    
                case QualityLevel.High:
                    _graphicsSettings.MaxParticles = 1000;
                    _graphicsSettings.EnableParticleTrails = true;
                    _graphicsSettings.EnableScreenEffects = true;
                    break;
            }
            
            ErrorManager.LogInfo($"Applied {_currentQuality} quality settings");
        }

        private void UpdatePerformanceHistory(float deltaTime)
        {
            _performanceHistory.AddSample(deltaTime);
        }

        private void UpdateTargetQuality()
        {
            var perfStats = _performanceHistory.GetStats();
            float targetFPS = GameConstants.TARGET_FPS * 0.9f; // Allow 10% variance
            
            if (perfStats.AverageFPS < targetFPS * 0.7f) // < 42 FPS
            {
                _targetQuality = QualityLevel.Low;
            }
            else if (perfStats.AverageFPS < targetFPS * 0.85f) // < 51 FPS
            {
                _targetQuality = QualityLevel.Medium;
            }
            else if (perfStats.AverageFPS > targetFPS * 1.1f) // > 59 FPS
            {
                _targetQuality = QualityLevel.High;
            }
        }

        private void UpdateQualityTransition(float deltaTime)
        {
            if (_currentQuality != _targetQuality)
            {
                _qualityTransitionTimer += deltaTime;
                
                if (_qualityTransitionTimer >= QUALITY_TRANSITION_TIME)
                {
                    _currentQuality = _targetQuality;
                    _qualityTransitionTimer = 0f;
                    ApplyQualitySettings();
                    
                    ErrorManager.LogInfo($"Quality transition completed: {_currentQuality}");
                }
            }
        }
    }

    /// <summary>
    /// Entity pool statistics
    /// </summary>
    public struct EntityPoolStats
    {
        public int ActiveCount { get; set; }
        public int AvailableCount { get; set; }
        public int TotalAllocated { get; set; }
        public int MaxPoolSize { get; set; }
        public float UtilizationPercent { get; set; }
    }

    /// <summary>
    /// Graphics quality levels
    /// </summary>
    public enum QualityLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Performance history tracking for adaptive quality
    /// </summary>
    public class PerformanceHistory
    {
        private readonly Queue<float> _frameTimes;
        private readonly int _maxSamples;

        public PerformanceHistory(int maxSamples)
        {
            _maxSamples = maxSamples;
            _frameTimes = new Queue<float>(maxSamples);
        }

        public void AddSample(float deltaTime)
        {
            _frameTimes.Enqueue(deltaTime);
            if (_frameTimes.Count > _maxSamples)
            {
                _frameTimes.Dequeue();
            }
        }

        public PerformanceStats GetStats()
        {
            if (_frameTimes.Count == 0)
            {
                return new PerformanceStats();
            }

            var times = _frameTimes.ToArray();
            float avg = times.Average();
            float avgFPS = 1f / avg;
            float minFPS = 1f / times.Max();
            float maxFPS = 1f / times.Min();
            float variance = times.Select(t => (t - avg) * (t - avg)).Average();

            return new PerformanceStats
            {
                AverageFPS = avgFPS,
                MinFPS = minFPS,
                MaxFPS = maxFPS,
                FrameTimeVariance = variance
            };
        }
    }

    /// <summary>
    /// Performance statistics
    /// </summary>
    public struct PerformanceStats
    {
        public float AverageFPS { get; set; }
        public float MinFPS { get; set; }
        public float MaxFPS { get; set; }
        public float FrameTimeVariance { get; set; }
    }

    /// <summary>
    /// Adaptive graphics statistics
    /// </summary>
    public struct AdaptiveGraphicsStats
    {
        public QualityLevel CurrentQuality { get; set; }
        public QualityLevel TargetQuality { get; set; }
        public float AverageFPS { get; set; }
        public float MinFPS { get; set; }
        public float MaxFPS { get; set; }
        public float FrameTimeVariance { get; set; }
        public float QualityTransitionProgress { get; set; }
    }
}