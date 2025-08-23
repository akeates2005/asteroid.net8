# Object Pooling System - Technical Specification

## Overview

The Object Pooling System provides efficient memory management for the Asteroids game by reusing objects rather than constantly allocating and deallocating them. This system significantly reduces garbage collection pressure and improves performance, especially during intense gameplay with many objects.

## System Architecture

```
PoolManager (Singleton)
├── ObjectPool<T> (Generic base pool)
├── SpecializedPools
│   ├── BulletPool
│   ├── AsteroidPool
│   ├── ParticlePool
│   └── EffectPool
├── PoolMetrics (Performance tracking)
├── PoolConfiguration (Settings)
└── PoolStrategies (Allocation strategies)
```

## Core Interfaces

### 1. Poolable Object Interface

```csharp
public interface IPoolable
{
    /// <summary>
    /// Unique identifier for tracking objects
    /// </summary>
    uint PoolId { get; set; }
    
    /// <summary>
    /// Current state of the poolable object
    /// </summary>
    PoolableState State { get; set; }
    
    /// <summary>
    /// Whether the object is currently active/in-use
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Reset object to default state when returning to pool
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Initialize object when retrieved from pool
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Cleanup resources before destruction
    /// </summary>
    void Dispose();
    
    /// <summary>
    /// Validate object state for debugging
    /// </summary>
    bool IsValid();
}

public enum PoolableState
{
    InPool,          // Object is available in pool
    InUse,           // Object is currently being used
    Initializing,    // Object is being initialized
    Resetting,       // Object is being reset
    Disposed         // Object has been disposed
}
```

### 2. Object Pool Interface

```csharp
public interface IObjectPool<T> : IDisposable where T : class, IPoolable
{
    /// <summary>
    /// Current number of objects in pool
    /// </summary>
    int AvailableCount { get; }
    
    /// <summary>
    /// Total number of objects created
    /// </summary>
    int TotalCount { get; }
    
    /// <summary>
    /// Maximum pool capacity
    /// </summary>
    int MaxCapacity { get; }
    
    /// <summary>
    /// Pool utilization percentage
    /// </summary>
    float UtilizationRate { get; }
    
    /// <summary>
    /// Get object from pool
    /// </summary>
    T Get();
    
    /// <summary>
    /// Return object to pool
    /// </summary>
    void Return(T item);
    
    /// <summary>
    /// Pre-populate pool with objects
    /// </summary>
    void WarmUp(int count);
    
    /// <summary>
    /// Clear all objects from pool
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Trim pool to target size
    /// </summary>
    void Trim(int targetSize);
    
    /// <summary>
    /// Get pool statistics
    /// </summary>
    PoolStatistics GetStatistics();
}
```

## Advanced Object Pool Implementation

### 1. Generic Object Pool

```csharp
public class AdvancedObjectPool<T> : IObjectPool<T> where T : class, IPoolable, new()
{
    private readonly ConcurrentQueue<T> _pool;
    private readonly Func<T> _createFunc;
    private readonly Action<T> _resetFunc;
    private readonly Action<T> _initializeFunc;
    private readonly IPoolingStrategy _strategy;
    private readonly PoolConfiguration _config;
    private readonly PoolMetrics _metrics;
    private readonly object _lock = new object();
    
    private int _totalCreated;
    private int _currentInUse;
    private uint _nextId;
    
    public int AvailableCount => _pool.Count;
    public int TotalCount => _totalCreated;
    public int MaxCapacity => _config.MaxSize;
    public float UtilizationRate => _totalCreated > 0 ? (float)_currentInUse / _totalCreated : 0f;
    
    public AdvancedObjectPool(PoolConfiguration config, IPoolingStrategy strategy = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _strategy = strategy ?? new DefaultPoolingStrategy();
        _pool = new ConcurrentQueue<T>();
        _metrics = new PoolMetrics(typeof(T).Name);
        
        _createFunc = () => CreateNewObject();
        _resetFunc = obj => ResetObject(obj);
        _initializeFunc = obj => InitializeObject(obj);
        
        // Pre-warm the pool if configured
        if (_config.PreWarmSize > 0)
        {
            WarmUp(_config.PreWarmSize);
        }
    }
    
    public T Get()
    {
        _metrics.RecordRequest();
        
        T item = null;
        bool fromPool = _pool.TryDequeue(out item);
        
        if (!fromPool)
        {
            // Create new object if pool is empty and under capacity
            if (_totalCreated < _config.MaxSize)
            {
                item = CreateNewObject();
                _metrics.RecordCreation();
            }
            else if (_config.AllowGrowth)
            {
                // Pool is at capacity but growth is allowed
                item = CreateNewObject();
                _metrics.RecordGrowth();
            }
            else
            {
                // Pool exhausted and growth not allowed
                _metrics.RecordExhaustion();
                return null;
            }
        }
        else
        {
            _metrics.RecordPoolHit();
        }
        
        if (item != null)
        {
            InitializeObject(item);
            Interlocked.Increment(ref _currentInUse);
        }
        
        return item;
    }
    
    public void Return(T item)
    {
        if (item == null) return;
        
        _metrics.RecordReturn();
        
        try
        {
            // Validate object state
            if (!item.IsValid())
            {
                _metrics.RecordInvalidReturn();
                DisposeObject(item);
                return;
            }
            
            // Reset object state
            ResetObject(item);
            item.State = PoolableState.InPool;
            item.IsActive = false;
            
            Interlocked.Decrement(ref _currentInUse);
            
            // Return to pool if under capacity
            if (_pool.Count < _config.MaxSize)
            {
                _pool.Enqueue(item);
            }
            else
            {
                // Pool is full, dispose excess objects
                DisposeObject(item);
                _metrics.RecordDisposal();
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordError();
            ErrorManager.LogError($"Error returning object to pool: {ex.Message}", ex, "ObjectPool");
            DisposeObject(item);
        }
    }
    
    private T CreateNewObject()
    {
        lock (_lock)
        {
            var obj = new T();
            obj.PoolId = ++_nextId;
            obj.State = PoolableState.Initializing;
            Interlocked.Increment(ref _totalCreated);
            return obj;
        }
    }
    
    private void InitializeObject(T obj)
    {
        obj.State = PoolableState.Initializing;
        obj.IsActive = true;
        obj.Initialize();
        obj.State = PoolableState.InUse;
    }
    
    private void ResetObject(T obj)
    {
        obj.State = PoolableState.Resetting;
        obj.Reset();
    }
    
    private void DisposeObject(T obj)
    {
        obj.State = PoolableState.Disposed;
        obj.Dispose();
        Interlocked.Decrement(ref _totalCreated);
    }
    
    public void WarmUp(int count)
    {
        count = Math.Min(count, _config.MaxSize);
        
        var objectsToCreate = Math.Max(0, count - _pool.Count);
        
        for (int i = 0; i < objectsToCreate; i++)
        {
            var obj = CreateNewObject();
            ResetObject(obj);
            obj.State = PoolableState.InPool;
            _pool.Enqueue(obj);
        }
        
        _metrics.RecordWarmup(objectsToCreate);
    }
    
    public void Trim(int targetSize)
    {
        targetSize = Math.Max(0, targetSize);
        
        var objectsToRemove = Math.Max(0, _pool.Count - targetSize);
        
        for (int i = 0; i < objectsToRemove; i++)
        {
            if (_pool.TryDequeue(out var obj))
            {
                DisposeObject(obj);
            }
        }
        
        _metrics.RecordTrim(objectsToRemove);
    }
    
    public void Clear()
    {
        while (_pool.TryDequeue(out var obj))
        {
            DisposeObject(obj);
        }
        
        _totalCreated = 0;
        _currentInUse = 0;
        _nextId = 0;
        _metrics.Reset();
    }
    
    public PoolStatistics GetStatistics()
    {
        return new PoolStatistics
        {
            PoolName = typeof(T).Name,
            AvailableCount = AvailableCount,
            TotalCount = TotalCount,
            InUseCount = _currentInUse,
            MaxCapacity = MaxCapacity,
            UtilizationRate = UtilizationRate,
            Metrics = _metrics.GetSnapshot()
        };
    }
    
    public void Dispose()
    {
        Clear();
        _metrics?.Dispose();
    }
}
```

### 2. Specialized Pools

#### Bullet Pool

```csharp
public class BulletPool : AdvancedObjectPool<PooledBullet>
{
    private static readonly PoolConfiguration BulletPoolConfig = new()
    {
        MaxSize = 200,
        PreWarmSize = 50,
        AllowGrowth = false,
        TrimThreshold = 0.7f,
        ValidationEnabled = true
    };
    
    public BulletPool() : base(BulletPoolConfig, new BulletPoolingStrategy()) { }
    
    public PooledBullet Get(Vector2 position, Vector2 velocity, float damage = 1.0f)
    {
        var bullet = Get();
        if (bullet != null)
        {
            bullet.SetProperties(position, velocity, damage);
        }
        return bullet;
    }
}

public class PooledBullet : Bullet, IPoolable
{
    public uint PoolId { get; set; }
    public PoolableState State { get; set; }
    
    private Vector2 _originalPosition;
    private Vector2 _originalVelocity;
    private float _originalDamage;
    
    public void SetProperties(Vector2 position, Vector2 velocity, float damage)
    {
        Position = position;
        Velocity = velocity;
        _originalPosition = position;
        _originalVelocity = velocity;
        _originalDamage = damage;
        Active = true;
    }
    
    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Active = false;
        State = PoolableState.InPool;
    }
    
    public void Initialize()
    {
        // Nothing special needed for bullets
    }
    
    public void Dispose()
    {
        // Clean up any resources if needed
    }
    
    public bool IsValid()
    {
        // Validate bullet state
        return !float.IsNaN(Position.X) && !float.IsNaN(Position.Y) &&
               !float.IsNaN(Velocity.X) && !float.IsNaN(Velocity.Y);
    }
}
```

#### Particle Pool

```csharp
public class ParticlePool : AdvancedObjectPool<PooledParticle>
{
    private static readonly PoolConfiguration ParticlePoolConfig = new()
    {
        MaxSize = 1000,
        PreWarmSize = 200,
        AllowGrowth = true,
        GrowthFactor = 1.5f,
        TrimThreshold = 0.8f,
        ValidationEnabled = false // Particles are numerous and simple
    };
    
    public ParticlePool() : base(ParticlePoolConfig, new ParticlePoolingStrategy()) { }
    
    public PooledParticle Get(Vector2 position, Vector2 velocity, Color color, float lifespan)
    {
        var particle = Get();
        if (particle != null)
        {
            particle.SetProperties(position, velocity, color, lifespan);
        }
        return particle;
    }
}

public class PooledParticle : ExplosionParticle, IPoolable
{
    public uint PoolId { get; set; }
    public PoolableState State { get; set; }
    
    public void SetProperties(Vector2 position, Vector2 velocity, Color color, float lifespan)
    {
        Position = position;
        Velocity = velocity;
        Color = color;
        Lifespan = lifespan;
    }
    
    public void Reset()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Color = Color.White;
        Lifespan = 0;
        State = PoolableState.InPool;
    }
    
    public void Initialize()
    {
        // Particles need minimal initialization
    }
    
    public void Dispose()
    {
        // No special cleanup needed
    }
    
    public bool IsValid()
    {
        return Lifespan >= 0;
    }
}
```

## Pool Management System

### 1. Pool Manager

```csharp
public class PoolManager : IDisposable
{
    private static PoolManager _instance;
    private static readonly object _lock = new object();
    
    public static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new PoolManager();
                }
            }
            return _instance;
        }
    }
    
    private readonly Dictionary<Type, IObjectPool> _pools;
    private readonly PoolGlobalMetrics _globalMetrics;
    private readonly Timer _maintenanceTimer;
    
    // Specialized pools
    public BulletPool BulletPool { get; private set; }
    public ParticlePool ParticlePool { get; private set; }
    public AsteroidPool AsteroidPool { get; private set; }
    public EffectPool EffectPool { get; private set; }
    
    private PoolManager()
    {
        _pools = new Dictionary<Type, IObjectPool>();
        _globalMetrics = new PoolGlobalMetrics();
        
        InitializePools();
        
        // Set up maintenance timer (runs every 5 seconds)
        _maintenanceTimer = new Timer(PerformMaintenance, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    private void InitializePools()
    {
        BulletPool = RegisterPool<PooledBullet>(new BulletPool());
        ParticlePool = RegisterPool<PooledParticle>(new ParticlePool());
        AsteroidPool = RegisterPool<PooledAsteroid>(new AsteroidPool());
        EffectPool = RegisterPool<PooledEffect>(new EffectPool());
    }
    
    private T RegisterPool<T>(IObjectPool<T> pool) where T : class, IPoolable
    {
        _pools[typeof(T)] = pool;
        return pool;
    }
    
    public IObjectPool<T> GetPool<T>() where T : class, IPoolable
    {
        if (_pools.TryGetValue(typeof(T), out var pool))
        {
            return (IObjectPool<T>)pool;
        }
        
        throw new InvalidOperationException($"No pool registered for type {typeof(T).Name}");
    }
    
    public T Get<T>() where T : class, IPoolable
    {
        return GetPool<T>().Get();
    }
    
    public void Return<T>(T item) where T : class, IPoolable
    {
        if (item != null)
        {
            GetPool<T>().Return(item);
        }
    }
    
    private void PerformMaintenance(object state)
    {
        try
        {
            var memoryPressure = GC.GetTotalMemory(false);
            var pressureThreshold = 50 * 1024 * 1024; // 50 MB
            
            foreach (var pool in _pools.Values)
            {
                var stats = pool.GetStatistics();
                
                // Trim pools if memory pressure is high
                if (memoryPressure > pressureThreshold)
                {
                    var targetSize = (int)(stats.AvailableCount * 0.5f);
                    pool.Trim(targetSize);
                }
                // Trim pools with low utilization
                else if (stats.UtilizationRate < 0.3f && stats.AvailableCount > 10)
                {
                    var targetSize = Math.Max(5, (int)(stats.AvailableCount * 0.7f));
                    pool.Trim(targetSize);
                }
            }
            
            _globalMetrics.RecordMaintenanceCycle();
        }
        catch (Exception ex)
        {
            ErrorManager.LogError("Error during pool maintenance", ex, "PoolManager");
        }
    }
    
    public PoolManagerStatistics GetGlobalStatistics()
    {
        var poolStats = _pools.Values.Select(p => p.GetStatistics()).ToList();
        
        return new PoolManagerStatistics
        {
            TotalPools = _pools.Count,
            TotalObjectsInPools = poolStats.Sum(s => s.AvailableCount),
            TotalObjectsInUse = poolStats.Sum(s => s.InUseCount),
            TotalObjectsCreated = poolStats.Sum(s => s.TotalCount),
            AverageUtilization = poolStats.Average(s => s.UtilizationRate),
            MemoryUsage = GC.GetTotalMemory(false),
            PoolStatistics = poolStats
        };
    }
    
    public void Dispose()
    {
        _maintenanceTimer?.Dispose();
        
        foreach (var pool in _pools.Values)
        {
            pool.Dispose();
        }
        
        _pools.Clear();
        _globalMetrics?.Dispose();
    }
}
```

### 2. Pooling Strategies

```csharp
public interface IPoolingStrategy
{
    int CalculateOptimalSize(PoolUsageHistory history);
    bool ShouldGrow(PoolStatistics stats);
    bool ShouldTrim(PoolStatistics stats);
    int CalculateGrowthAmount(PoolStatistics stats);
    int CalculateTrimAmount(PoolStatistics stats);
}

public class AdaptivePoolingStrategy : IPoolingStrategy
{
    private readonly float _targetUtilization;
    private readonly TimeSpan _historyWindow;
    
    public AdaptivePoolingStrategy(float targetUtilization = 0.7f, TimeSpan? historyWindow = null)
    {
        _targetUtilization = targetUtilization;
        _historyWindow = historyWindow ?? TimeSpan.FromMinutes(5);
    }
    
    public int CalculateOptimalSize(PoolUsageHistory history)
    {
        var recentPeakUsage = history.GetPeakUsage(_historyWindow);
        var averageUsage = history.GetAverageUsage(_historyWindow);
        
        // Optimal size should handle peak usage with some buffer
        var optimalSize = (int)(Math.Max(recentPeakUsage, averageUsage * 1.5f) / _targetUtilization);
        
        return Math.Max(10, optimalSize); // Minimum size of 10
    }
    
    public bool ShouldGrow(PoolStatistics stats)
    {
        return stats.UtilizationRate > 0.9f && stats.AvailableCount < 5;
    }
    
    public bool ShouldTrim(PoolStatistics stats)
    {
        return stats.UtilizationRate < 0.3f && stats.AvailableCount > 20;
    }
    
    public int CalculateGrowthAmount(PoolStatistics stats)
    {
        return Math.Max(5, (int)(stats.TotalCount * 0.5f));
    }
    
    public int CalculateTrimAmount(PoolStatistics stats)
    {
        var targetSize = (int)(stats.InUseCount / _targetUtilization);
        return Math.Max(0, stats.AvailableCount - targetSize);
    }
}
```

## Performance Monitoring

### 1. Pool Metrics

```csharp
public class PoolMetrics : IDisposable
{
    private readonly string _poolName;
    private readonly PerformanceCounter _requestCounter;
    private readonly PerformanceCounter _hitRateCounter;
    private readonly Timer _metricsTimer;
    
    public long TotalRequests { get; private set; }
    public long PoolHits { get; private set; }
    public long Creations { get; private set; }
    public long Returns { get; private set; }
    public long Disposals { get; private set; }
    public long Errors { get; private set; }
    
    public float HitRate => TotalRequests > 0 ? (float)PoolHits / TotalRequests : 0f;
    public float CreationRate => TotalRequests > 0 ? (float)Creations / TotalRequests : 0f;
    
    public PoolMetrics(string poolName)
    {
        _poolName = poolName;
        _requestCounter = new PerformanceCounter();
        _hitRateCounter = new PerformanceCounter();
        
        _metricsTimer = new Timer(UpdateCounters, null, 
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public void RecordRequest() => Interlocked.Increment(ref TotalRequests);
    public void RecordPoolHit() => Interlocked.Increment(ref PoolHits);
    public void RecordCreation() => Interlocked.Increment(ref Creations);
    public void RecordReturn() => Interlocked.Increment(ref Returns);
    public void RecordDisposal() => Interlocked.Increment(ref Disposals);
    public void RecordError() => Interlocked.Increment(ref Errors);
    
    private void UpdateCounters(object state)
    {
        _requestCounter.Update(TotalRequests);
        _hitRateCounter.Update(HitRate * 100);
    }
    
    public PoolMetricsSnapshot GetSnapshot()
    {
        return new PoolMetricsSnapshot
        {
            PoolName = _poolName,
            TotalRequests = TotalRequests,
            PoolHits = PoolHits,
            Creations = Creations,
            Returns = Returns,
            Disposals = Disposals,
            Errors = Errors,
            HitRate = HitRate,
            CreationRate = CreationRate,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void Reset()
    {
        TotalRequests = 0;
        PoolHits = 0;
        Creations = 0;
        Returns = 0;
        Disposals = 0;
        Errors = 0;
    }
    
    public void Dispose()
    {
        _metricsTimer?.Dispose();
        _requestCounter?.Dispose();
        _hitRateCounter?.Dispose();
    }
}
```

## Configuration System

```csharp
public class PoolConfiguration
{
    public int MaxSize { get; set; } = 100;
    public int PreWarmSize { get; set; } = 0;
    public bool AllowGrowth { get; set; } = true;
    public float GrowthFactor { get; set; } = 1.5f;
    public float TrimThreshold { get; set; } = 0.7f;
    public bool ValidationEnabled { get; set; } = true;
    public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MinRetainSize { get; set; } = 5;
    
    public static PoolConfiguration Default => new PoolConfiguration();
    
    public static PoolConfiguration HighFrequency => new PoolConfiguration
    {
        MaxSize = 500,
        PreWarmSize = 100,
        AllowGrowth = true,
        GrowthFactor = 2.0f,
        ValidationEnabled = false
    };
    
    public static PoolConfiguration LowFrequency => new PoolConfiguration
    {
        MaxSize = 50,
        PreWarmSize = 5,
        AllowGrowth = false,
        TrimThreshold = 0.5f,
        ValidationEnabled = true
    };
}
```

This object pooling system provides efficient, scalable memory management with comprehensive monitoring and adaptive behavior based on usage patterns.