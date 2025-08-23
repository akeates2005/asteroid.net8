# Test Strategy and Coverage Plan - Technical Specification

## Overview

The Test Strategy and Coverage Plan provides a comprehensive testing framework for Phase 2 of the Asteroids game, ensuring high code quality, performance reliability, and maintainability through automated testing, performance benchmarks, and continuous integration.

## Testing Architecture

```
TestingFramework
├── Unit Testing Layer (95% Coverage Target)
│   ├── Core Component Tests
│   ├── Collision Detection Tests
│   ├── Object Pooling Tests
│   ├── Spatial Partitioning Tests
│   ├── Particle System Tests
│   ├── Performance Monitoring Tests
│   └── Error Handling Tests
├── Integration Testing Layer
│   ├── System Integration Tests
│   ├── Component Interaction Tests
│   ├── Performance Integration Tests
│   └── End-to-End Scenarios
├── Performance Testing Layer
│   ├── Benchmark Suites
│   ├── Load Testing
│   ├── Stress Testing
│   ├── Memory Testing
│   └── Regression Testing
├── Quality Assurance Layer
│   ├── Code Quality Gates
│   ├── Coverage Analysis
│   ├── Mutation Testing
│   └── Static Analysis
└── Continuous Testing Pipeline
    ├── Pre-commit Hooks
    ├── Build Pipeline Tests
    ├── Deployment Testing
    └── Monitoring & Reporting
```

## Testing Philosophy

### Core Principles

1. **Test-First Development**: Write tests before implementation
2. **Comprehensive Coverage**: Target 95%+ code coverage with meaningful tests
3. **Fast Feedback**: Unit tests run in <5 seconds, full suite in <30 seconds
4. **Reliable Tests**: No flaky tests, deterministic behavior
5. **Maintainable Tests**: Clear, readable, and well-structured test code
6. **Performance-Aware**: Every test includes performance assertions
7. **Isolation**: Tests are independent and can run in any order

### Testing Pyramid

```
           /\
          /  \
         / UI \         <- 10% (End-to-End Tests)
        /______\
       /        \
      /Integration\     <- 20% (Integration Tests)
     /____________\
    /              \
   /   Unit Tests   \   <- 70% (Unit Tests)
  /________________\
```

## Unit Testing Strategy

### 1. Test Framework Configuration

```csharp
// Global test configuration
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 4)]
[assembly: TestFramework("Asteroids.Tests.CustomTestFramework", "Asteroids.Tests")]

public class CustomTestFramework : XunitTestFramework
{
    public CustomTestFramework(IMessageSink messageSink) : base(messageSink)
    {
        // Configure performance tracking for all tests
        PerformanceTestTracker.Initialize();
    }
}

// Base test class with common setup
public abstract class BaseTestClass : IDisposable
{
    protected ITestOutputHelper Output { get; }
    protected IServiceProvider ServiceProvider { get; }
    protected PerformanceTracker PerformanceTracker { get; }
    
    protected BaseTestClass(ITestOutputHelper output)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        ServiceProvider = CreateServiceProvider();
        PerformanceTracker = new PerformanceTracker();
    }
    
    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Register test doubles and mocks
        services.AddSingleton<ILogger>(new TestLogger(Output));
        services.AddSingleton<IErrorHandler, TestErrorHandler>();
        services.AddSingleton<IPerformanceMonitor, TestPerformanceMonitor>();
        
        return services.BuildServiceProvider();
    }
    
    public virtual void Dispose()
    {
        ServiceProvider?.Dispose();
        PerformanceTracker?.Dispose();
    }
}
```

### 2. Collision Detection Tests

```csharp
[TestClass]
[TestCategory("CollisionDetection")]
public class CollisionDetectionSystemTests : BaseTestClass
{
    private CollisionDetectionSystem _system;
    private List<ICollidableAdvanced> _testObjects;
    
    public CollisionDetectionSystemTests(ITestOutputHelper output) : base(output) { }
    
    [TestInitialize]
    public void Setup()
    {
        _system = new CollisionDetectionSystem(new CollisionConfiguration
        {
            BroadPhaseType = BroadPhaseType.SpatialGrid,
            CellSize = 50f
        });
        
        _testObjects = new List<ICollidableAdvanced>();
    }
    
    [TestMethod]
    [TestCategory("BroadPhase")]
    [Performance(MaxExecutionTime = 1)] // 1ms max
    public void BroadPhase_WithManyObjects_PerformsWithinThreshold()
    {
        // Arrange
        const int objectCount = 1000;
        var objects = CreateTestObjects(objectCount);
        
        // Act
        using (PerformanceTracker.StartTiming("BroadPhase"))
        {
            var pairs = _system.GetPotentialCollisionPairs(objects);
            
            // Assert
            Assert.IsNotNull(pairs);
            Assert.IsTrue(pairs.Count >= 0);
        }
        
        // Performance assertion
        var timing = PerformanceTracker.GetLastTiming("BroadPhase");
        Assert.IsTrue(timing.TotalMilliseconds < 1.0, 
            $"BroadPhase took {timing.TotalMilliseconds}ms, expected < 1ms");
    }
    
    [TestMethod]
    [TestCategory("NarrowPhase")]
    [DataRow(0, 0, 10, 10, 0, 0, 10, 10, true)] // Overlapping circles
    [DataRow(0, 0, 10, 20, 0, 10, 20, 0, false)] // Separated circles
    [DataRow(5, 5, 10, 5, 5, 10, 5, 5, true)] // Touching circles
    public void NarrowPhase_CircleCollision_DetectsCorrectly(
        float x1, float y1, float r1, float x2, float y2, float r2, bool expectedCollision)
    {
        // Arrange
        var circle1 = CreateCircleObject(new Vector2(x1, y1), r1);
        var circle2 = CreateCircleObject(new Vector2(x2, y2), r2);
        
        // Act
        var collision = _system.TestCollision(circle1, circle2);
        
        // Assert
        Assert.AreEqual(expectedCollision, collision.HasValue,
            $"Expected collision: {expectedCollision}, Got: {collision.HasValue}");
        
        if (expectedCollision && collision.HasValue)
        {
            Assert.IsTrue(collision.Value.PenetrationDepth > 0);
            Assert.IsNotNull(collision.Value.ContactNormal);
        }
    }
    
    [TestMethod]
    [TestCategory("Performance")]
    [Repeat(10)] // Run multiple times for statistical validity
    public void CollisionDetection_FullPipeline_MaintainsPerformance()
    {
        // Arrange
        const int objectCount = 500;
        var objects = CreateRandomTestObjects(objectCount);
        
        var performanceMetrics = new List<double>();
        
        // Act & Assert
        for (int iteration = 0; iteration < 100; iteration++)
        {
            using (var timer = PerformanceTracker.StartTiming($"Iteration_{iteration}"))
            {
                _system.Update(objects, 16.67f); // Simulate 60 FPS
            }
            
            var timing = PerformanceTracker.GetLastTiming($"Iteration_{iteration}");
            performanceMetrics.Add(timing.TotalMilliseconds);
        }
        
        // Statistical analysis
        var averageTime = performanceMetrics.Average();
        var maxTime = performanceMetrics.Max();
        var stdDev = CalculateStandardDeviation(performanceMetrics);
        
        // Performance assertions
        Assert.IsTrue(averageTime < 2.0, $"Average time {averageTime}ms exceeds 2ms threshold");
        Assert.IsTrue(maxTime < 5.0, $"Max time {maxTime}ms exceeds 5ms threshold");
        Assert.IsTrue(stdDev < 1.0, $"Standard deviation {stdDev}ms indicates inconsistent performance");
        
        Output.WriteLine($"Performance Stats - Avg: {averageTime:F2}ms, Max: {maxTime:F2}ms, StdDev: {stdDev:F2}ms");
    }
    
    [TestMethod]
    [TestCategory("EdgeCases")]
    public void CollisionDetection_WithNullObjects_HandlesGracefully()
    {
        // Arrange
        var objects = new List<ICollidableAdvanced> { null, CreateCircleObject(Vector2.Zero, 10) };
        
        // Act & Assert
        Assert.DoesNotThrow(() => _system.Update(objects, 16.67f));
    }
    
    [TestMethod]
    [TestCategory("Memory")]
    public void CollisionDetection_RepeatedOperations_DoesNotLeak()
    {
        // Arrange
        const int iterations = 1000;
        var objects = CreateTestObjects(100);
        
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        for (int i = 0; i < iterations; i++)
        {
            _system.Update(objects, 16.67f);
            
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerIteration = memoryIncrease / (double)iterations;
        
        Assert.IsTrue(memoryIncreasePerIteration < 1024, // Less than 1KB per iteration
            $"Memory leak detected: {memoryIncreasePerIteration} bytes per iteration");
        
        Output.WriteLine($"Memory usage: Initial: {initialMemory}, Final: {finalMemory}, " +
                        $"Increase: {memoryIncrease} bytes ({memoryIncreasePerIteration:F2} per iteration)");
    }
    
    // Helper methods
    private List<ICollidableAdvanced> CreateTestObjects(int count)
    {
        var objects = new List<ICollidableAdvanced>();
        var random = new Random(42); // Fixed seed for reproducible tests
        
        for (int i = 0; i < count; i++)
        {
            var position = new Vector2(random.Next(0, 1000), random.Next(0, 1000));
            var radius = random.Next(5, 25);
            objects.Add(CreateCircleObject(position, radius));
        }
        
        return objects;
    }
    
    private ICollidableAdvanced CreateCircleObject(Vector2 position, float radius)
    {
        return new TestCollidableObject
        {
            Position = position,
            Shape = new CircleShape(position, radius),
            Layer = CollisionLayer.Default,
            Mask = CollisionLayer.All,
            IsActive = true
        };
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _system?.Dispose();
        _testObjects?.Clear();
    }
}
```

### 3. Object Pooling Tests

```csharp
[TestClass]
[TestCategory("ObjectPooling")]
public class ObjectPoolTests : BaseTestClass
{
    public ObjectPoolTests(ITestOutputHelper output) : base(output) { }
    
    [TestMethod]
    [TestCategory("BasicFunctionality")]
    public void ObjectPool_GetAndReturn_WorksCorrectly()
    {
        // Arrange
        var config = new PoolConfiguration { MaxSize = 10, PreWarmSize = 5 };
        using var pool = new AdvancedObjectPool<TestPoolableObject>(config);
        
        // Act
        var obj1 = pool.Get();
        var obj2 = pool.Get();
        var initialCount = pool.AvailableCount;
        
        pool.Return(obj1);
        var countAfterReturn = pool.AvailableCount;
        
        // Assert
        Assert.IsNotNull(obj1);
        Assert.IsNotNull(obj2);
        Assert.AreNotSame(obj1, obj2);
        Assert.AreEqual(initialCount + 1, countAfterReturn);
    }
    
    [TestMethod]
    [TestCategory("Performance")]
    [Performance(MaxExecutionTime = 10)]
    public void ObjectPool_HighFrequencyOperations_PerformsWell()
    {
        // Arrange
        const int operationCount = 10000;
        var config = new PoolConfiguration { MaxSize = 100, PreWarmSize = 50 };
        using var pool = new AdvancedObjectPool<TestPoolableObject>(config);
        
        // Act
        using (PerformanceTracker.StartTiming("HighFrequencyOperations"))
        {
            var objects = new List<TestPoolableObject>();
            
            // Get objects
            for (int i = 0; i < operationCount; i++)
            {
                objects.Add(pool.Get());
            }
            
            // Return objects
            foreach (var obj in objects)
            {
                pool.Return(obj);
            }
        }
        
        // Assert
        var timing = PerformanceTracker.GetLastTiming("HighFrequencyOperations");
        Assert.IsTrue(timing.TotalMilliseconds < 10.0,
            $"High frequency operations took {timing.TotalMilliseconds}ms, expected < 10ms");
        
        // Verify pool state
        Assert.IsTrue(pool.AvailableCount >= 50); // Should have pre-warmed objects available
    }
    
    [TestMethod]
    [TestCategory("ThreadSafety")]
    [Repeat(5)]
    public void ObjectPool_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 1000;
        var config = new PoolConfiguration { MaxSize = 1000 };
        using var pool = new AdvancedObjectPool<TestPoolableObject>(config);
        
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(threadCount);
        var tasks = new Task[threadCount];
        
        // Act
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    
                    var localObjects = new List<TestPoolableObject>();
                    
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var obj = pool.Get();
                        if (obj != null)
                        {
                            localObjects.Add(obj);
                        }
                    }
                    
                    foreach (var obj in localObjects)
                    {
                        pool.Return(obj);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }
        
        Task.WaitAll(tasks);
        
        // Assert
        Assert.IsTrue(exceptions.IsEmpty, 
            $"Thread safety test failed with {exceptions.Count} exceptions: " +
            string.Join(", ", exceptions.Select(e => e.Message)));
    }
    
    [TestMethod]
    [TestCategory("ResourceManagement")]
    public void ObjectPool_Dispose_CleansUpResources()
    {
        // Arrange
        var config = new PoolConfiguration { MaxSize = 10, PreWarmSize = 5 };
        var pool = new AdvancedObjectPool<TestPoolableObject>(config);
        
        var obj1 = pool.Get();
        var obj2 = pool.Get();
        
        // Act
        pool.Dispose();
        
        // Assert
        Assert.ThrowsException<ObjectDisposedException>(() => pool.Get());
        Assert.AreEqual(PoolableState.Disposed, obj1.State);
        Assert.AreEqual(PoolableState.Disposed, obj2.State);
    }
}

// Test helper classes
public class TestPoolableObject : IPoolable
{
    public uint PoolId { get; set; }
    public PoolableState State { get; set; }
    public bool IsActive { get; set; }
    
    public int InitializeCallCount { get; private set; }
    public int ResetCallCount { get; private set; }
    public int DisposeCallCount { get; private set; }
    
    public void Initialize()
    {
        InitializeCallCount++;
        IsActive = true;
        State = PoolableState.InUse;
    }
    
    public void Reset()
    {
        ResetCallCount++;
        IsActive = false;
        State = PoolableState.InPool;
    }
    
    public void Dispose()
    {
        DisposeCallCount++;
        State = PoolableState.Disposed;
    }
    
    public bool IsValid() => State != PoolableState.Disposed;
}
```

### 4. Performance Monitoring Tests

```csharp
[TestClass]
[TestCategory("PerformanceMonitoring")]
public class PerformanceMonitorTests : BaseTestClass
{
    private MasterPerformanceMonitor _monitor;
    
    public PerformanceMonitorTests(ITestOutputHelper output) : base(output) { }
    
    [TestInitialize]
    public void Setup()
    {
        _monitor = new MasterPerformanceMonitor();
    }
    
    [TestMethod]
    [TestCategory("MetricCollection")]
    public void PerformanceMonitor_MetricUpdates_AreAccurate()
    {
        // Arrange
        var metric = new AdvancedPerformanceMetric("TestMetric", MetricCategory.Performance, "ms");
        _monitor.RegisterMetric(metric);
        
        var testValues = new[] { 10.0, 20.0, 30.0, 40.0, 50.0 };
        
        // Act
        foreach (var value in testValues)
        {
            metric.Update(value);
        }
        
        // Assert
        var stats = metric.GetStatistics();
        Assert.AreEqual(50.0, stats.Current);
        Assert.AreEqual(30.0, stats.Average);
        Assert.AreEqual(10.0, stats.Min);
        Assert.AreEqual(50.0, stats.Max);
        Assert.AreEqual(5, stats.SampleCount);
    }
    
    [TestMethod]
    [TestCategory("ProfilingSession")]
    public void PerformanceMonitor_ProfilingSession_TracksCorrectly()
    {
        // Arrange
        const string sessionName = "TestSession";
        
        // Act
        _monitor.StartProfiling(sessionName);
        
        // Simulate some work
        Thread.Sleep(100);
        
        var results = _monitor.EndProfiling(sessionName);
        
        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(sessionName, results.SessionName);
        Assert.IsTrue(results.Duration.TotalMilliseconds >= 100);
        Assert.IsNotNull(results.StartSnapshot);
        Assert.IsNotNull(results.EndSnapshot);
    }
    
    [TestMethod]
    [TestCategory("OperationTiming")]
    public void PerformanceMonitor_OperationTiming_MeasuresAccurately()
    {
        // Arrange
        const string operationName = "TestOperation";
        const int sleepTime = 50;
        
        // Act
        using (_monitor.BeginTiming(operationName))
        {
            Thread.Sleep(sleepTime);
        }
        
        // Assert
        var metric = _monitor.GetMetric($"Operation_{operationName}");
        Assert.IsNotNull(metric);
        
        var stats = metric.GetStatistics();
        Assert.IsTrue(stats.Current >= sleepTime - 10, // Allow 10ms tolerance
            $"Measured time {stats.Current}ms is less than expected {sleepTime}ms");
        Assert.IsTrue(stats.Current <= sleepTime + 50, // Allow overhead
            $"Measured time {stats.Current}ms exceeds reasonable overhead");
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _monitor?.Dispose();
    }
}
```

## Integration Testing Strategy

### 1. System Integration Tests

```csharp
[TestClass]
[TestCategory("Integration")]
public class SystemIntegrationTests : BaseTestClass
{
    private GameSystemManager _systemManager;
    private CollisionDetectionSystem _collisionSystem;
    private ObjectPoolManager _poolManager;
    private SpatialManager _spatialManager;
    
    public SystemIntegrationTests(ITestOutputHelper output) : base(output) { }
    
    [TestInitialize]
    public void Setup()
    {
        // Initialize systems in correct order
        var worldBounds = new BoundingBox(Vector2.Zero, new Vector2(1000, 1000));
        
        _poolManager = new ObjectPoolManager();
        _spatialManager = new SpatialManager(worldBounds);
        _collisionSystem = new CollisionDetectionSystem(_spatialManager);
        _systemManager = new GameSystemManager();
        
        _systemManager.RegisterSystem(_poolManager);
        _systemManager.RegisterSystem(_spatialManager);
        _systemManager.RegisterSystem(_collisionSystem);
    }
    
    [TestMethod]
    [TestCategory("SystemInteraction")]
    [Integration]
    public void Systems_WorkingTogether_ProcessGameLoop()
    {
        // Arrange
        const int gameObjects = 100;
        const int gameLoopIterations = 60; // 1 second at 60 FPS
        
        var gameObjects = CreateGameObjectsWithSystems(gameObjects);
        
        var performanceMetrics = new List<GameLoopMetrics>();
        
        // Act
        for (int frame = 0; frame < gameLoopIterations; frame++)
        {
            var frameMetrics = new GameLoopMetrics { Frame = frame };
            
            using (var timer = PerformanceTracker.StartTiming($"Frame_{frame}"))
            {
                // Update all systems
                frameMetrics.UpdateTime = MeasureOperation(() => 
                    _systemManager.Update(16.67f)); // 60 FPS
                
                frameMetrics.CollisionTime = MeasureOperation(() =>
                    _collisionSystem.DetectCollisions());
                
                frameMetrics.SpatialTime = MeasureOperation(() =>
                    _spatialManager.UpdateObjects(gameObjects));
            }
            
            performanceMetrics.Add(frameMetrics);
        }
        
        // Assert
        var averageFrameTime = performanceMetrics.Average(m => m.TotalTime);
        var maxFrameTime = performanceMetrics.Max(m => m.TotalTime);
        
        Assert.IsTrue(averageFrameTime < 16.67, 
            $"Average frame time {averageFrameTime}ms exceeds 60 FPS target");
        
        Assert.IsTrue(maxFrameTime < 33.33,
            $"Max frame time {maxFrameTime}ms exceeds 30 FPS minimum");
        
        // Verify system integrity
        Assert.AreEqual(gameObjects, _spatialManager.ObjectCount);
        Assert.IsTrue(_poolManager.GetGlobalStatistics().TotalObjectsInPools > 0);
        
        // Output performance summary
        Output.WriteLine($"Integration Test Results:");
        Output.WriteLine($"  Average Frame Time: {averageFrameTime:F2}ms");
        Output.WriteLine($"  Max Frame Time: {maxFrameTime:F2}ms");
        Output.WriteLine($"  Average Update Time: {performanceMetrics.Average(m => m.UpdateTime):F2}ms");
        Output.WriteLine($"  Average Collision Time: {performanceMetrics.Average(m => m.CollisionTime):F2}ms");
        Output.WriteLine($"  Average Spatial Time: {performanceMetrics.Average(m => m.SpatialTime):F2}ms");
    }
    
    [TestMethod]
    [TestCategory("ErrorRecovery")]
    [Integration]
    public void Systems_WithErrors_RecoverGracefully()
    {
        // Arrange
        var errorSystem = new FaultyTestSystem();
        _systemManager.RegisterSystem(errorSystem);
        
        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            if (i == 5)
            {
                errorSystem.ShouldThrowError = true;
            }
            
            // System should not crash despite error
            Assert.DoesNotThrow(() => _systemManager.Update(16.67f));
        }
        
        // Verify error handling worked
        Assert.IsTrue(errorSystem.ErrorCount > 0);
        Assert.IsTrue(_systemManager.IsSystemHealthy(errorSystem.GetType()));
    }
    
    private double MeasureOperation(Action operation)
    {
        var stopwatch = Stopwatch.StartNew();
        operation();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _systemManager?.Dispose();
        _collisionSystem?.Dispose();
        _spatialManager?.Dispose();
        _poolManager?.Dispose();
    }
}

// Helper classes for integration testing
public class GameLoopMetrics
{
    public int Frame { get; set; }
    public double UpdateTime { get; set; }
    public double CollisionTime { get; set; }
    public double SpatialTime { get; set; }
    public double TotalTime => UpdateTime + CollisionTime + SpatialTime;
}

public class FaultyTestSystem : IGameSystem
{
    public bool ShouldThrowError { get; set; }
    public int ErrorCount { get; private set; }
    
    public void Update(float deltaTime)
    {
        if (ShouldThrowError)
        {
            ErrorCount++;
            throw new InvalidOperationException("Test error");
        }
    }
    
    public void Dispose() { }
}
```

## Performance Testing Strategy

### 1. Automated Benchmarks

```csharp
[TestClass]
[TestCategory("Performance")]
[TestCategory("Benchmarks")]
public class PerformanceBenchmarkTests : BaseTestClass
{
    public PerformanceBenchmarkTests(ITestOutputHelper output) : base(output) { }
    
    [TestMethod]
    [Benchmark]
    [TestCategory("CollisionDetection")]
    public void Benchmark_CollisionDetection_WithVaryingObjectCounts()
    {
        var objectCounts = new[] { 100, 500, 1000, 2000, 5000 };
        var results = new Dictionary<int, BenchmarkResult>();
        
        foreach (var objectCount in objectCounts)
        {
            var system = new CollisionDetectionSystem();
            var objects = CreateBenchmarkObjects(objectCount);
            
            // Warm up
            for (int i = 0; i < 10; i++)
            {
                system.Update(objects, 16.67f);
            }
            
            // Benchmark
            var times = new List<double>();
            for (int i = 0; i < 100; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                system.Update(objects, 16.67f);
                stopwatch.Stop();
                times.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            
            results[objectCount] = new BenchmarkResult
            {
                ObjectCount = objectCount,
                AverageTime = times.Average(),
                MinTime = times.Min(),
                MaxTime = times.Max(),
                StandardDeviation = CalculateStandardDeviation(times),
                Percentile95 = CalculatePercentile(times, 0.95)
            };
            
            system.Dispose();
        }
        
        // Analyze results and assert performance requirements
        foreach (var result in results.Values)
        {
            var expectedTime = CalculateExpectedTime(result.ObjectCount);
            
            Assert.IsTrue(result.AverageTime <= expectedTime,
                $"Performance regression: {result.ObjectCount} objects took {result.AverageTime:F2}ms, " +
                $"expected <= {expectedTime:F2}ms");
            
            Output.WriteLine($"{result.ObjectCount} objects: {result.AverageTime:F2}ms avg, " +
                           $"{result.Percentile95:F2}ms p95, σ={result.StandardDeviation:F2}");
        }
        
        // Check scalability (should be O(n log n) or better)
        AnalyzeScalability(results);
    }
    
    [TestMethod]
    [Benchmark]
    [TestCategory("Memory")]
    public void Benchmark_MemoryAllocation_UnderLoad()
    {
        const int iterations = 1000;
        const int objectsPerIteration = 100;
        
        var poolManager = new ObjectPoolManager();
        var initialMemory = GC.GetTotalMemory(true);
        
        // Benchmark allocation patterns
        for (int i = 0; i < iterations; i++)
        {
            var objects = new List<IPoolable>();
            
            // Allocate
            for (int j = 0; j < objectsPerIteration; j++)
            {
                objects.Add(poolManager.BulletPool.Get());
            }
            
            // Use objects briefly
            Thread.Sleep(1);
            
            // Return to pool
            foreach (var obj in objects)
            {
                poolManager.BulletPool.Return(obj);
            }
            
            // Periodic GC to measure actual allocation
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(false);
        
        var totalAllocations = iterations * objectsPerIteration;
        var memoryIncrease = finalMemory - initialMemory;
        var memoryPerAllocation = memoryIncrease / (double)totalAllocations;
        
        // Assert memory efficiency
        Assert.IsTrue(memoryPerAllocation < 100, // Less than 100 bytes per allocation
            $"Memory efficiency issue: {memoryPerAllocation:F2} bytes per allocation");
        
        Assert.IsTrue(memoryIncrease < 10 * 1024 * 1024, // Less than 10 MB total increase
            $"Memory leak suspected: {memoryIncrease / (1024 * 1024):F2} MB increase");
        
        Output.WriteLine($"Memory Benchmark Results:");
        Output.WriteLine($"  Total allocations: {totalAllocations:N0}");
        Output.WriteLine($"  Memory increase: {memoryIncrease / (1024 * 1024):F2} MB");
        Output.WriteLine($"  Memory per allocation: {memoryPerAllocation:F2} bytes");
        
        poolManager.Dispose();
    }
    
    private double CalculateExpectedTime(int objectCount)
    {
        // Expected performance based on O(n log n) complexity
        return Math.Max(0.1, objectCount * Math.Log(objectCount) / 10000.0);
    }
    
    private void AnalyzeScalability(Dictionary<int, BenchmarkResult> results)
    {
        var sortedResults = results.OrderBy(r => r.Key).ToList();
        
        for (int i = 1; i < sortedResults.Count; i++)
        {
            var prev = sortedResults[i - 1].Value;
            var curr = sortedResults[i].Value;
            
            var objectRatio = (double)curr.ObjectCount / prev.ObjectCount;
            var timeRatio = curr.AverageTime / prev.AverageTime;
            
            // For O(n log n), time ratio should be approximately objectRatio * log(objectRatio)
            var expectedTimeRatio = objectRatio * Math.Log(objectRatio);
            var scalabilityRatio = timeRatio / expectedTimeRatio;
            
            Assert.IsTrue(scalabilityRatio <= 2.0, // Allow some overhead
                $"Poor scalability: {prev.ObjectCount} to {curr.ObjectCount} objects, " +
                $"time ratio {timeRatio:F2}, expected {expectedTimeRatio:F2}");
            
            Output.WriteLine($"Scalability {prev.ObjectCount} -> {curr.ObjectCount}: " +
                           $"time ratio {timeRatio:F2}, expected {expectedTimeRatio:F2}");
        }
    }
}
```

## Continuous Testing Pipeline

### 1. Pre-commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit

echo "Running pre-commit tests..."

# Run fast unit tests
dotnet test --configuration Debug --filter "Category!=Integration&Category!=Performance" --no-build --logger "console;verbosity=minimal"

if [ $? -ne 0 ]; then
    echo "❌ Unit tests failed. Commit aborted."
    exit 1
fi

# Run code coverage analysis
dotnet test --configuration Debug --collect:"XPlat Code Coverage" --no-build

# Check coverage threshold (90% minimum)
COVERAGE=$(grep -oP 'Line coverage: \K[0-9.]+' coverage.txt)
if (( $(echo "$COVERAGE < 90" | bc -l) )); then
    echo "❌ Code coverage $COVERAGE% below 90% threshold. Commit aborted."
    exit 1
fi

# Run static analysis
dotnet sonarscanner begin
dotnet build
dotnet sonarscanner end

echo "✅ Pre-commit checks passed."
```

### 2. CI/CD Pipeline Configuration

```yaml
# .github/workflows/test-pipeline.yml
name: Test Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest
    timeout-minutes: 10
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Run Unit Tests
      run: |
        dotnet test \
          --configuration Release \
          --no-build \
          --filter "Category!=Integration&Category!=Performance" \
          --collect:"XPlat Code Coverage" \
          --logger trx \
          --results-directory TestResults/
          
    - name: Generate Coverage Report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:"TestResults/**/coverage.cobertura.xml" \
          -targetdir:"CoverageReport" \
          -reporttypes:Html
          
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: TestResults/**/coverage.cobertura.xml
        
    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Unit Test Results
        path: TestResults/*.trx
        reporter: dotnet-trx

  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest
    timeout-minutes: 20
    needs: unit-tests
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Run Integration Tests
      run: |
        dotnet test \
          --configuration Release \
          --filter "Category=Integration" \
          --logger trx \
          --results-directory TestResults/

  performance-tests:
    name: Performance Tests
    runs-on: ubuntu-latest
    timeout-minutes: 30
    needs: unit-tests
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Run Performance Tests
      run: |
        dotnet test \
          --configuration Release \
          --filter "Category=Performance" \
          --logger trx \
          --results-directory TestResults/
          
    - name: Analyze Performance Results
      run: |
        # Parse performance results and check for regressions
        python scripts/analyze-performance.py TestResults/
        
  quality-gates:
    name: Quality Gates
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    
    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0
        
    - name: SonarCloud Scan
      uses: SonarSource/sonarcloud-github-action@master
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        
    - name: Check Quality Gate
      run: |
        # Fail build if quality gate fails
        if ! curl -s "https://sonarcloud.io/api/qualitygates/project_status?projectKey=asteroids" | grep -q '"status":"OK"'; then
          echo "❌ Quality gate failed"
          exit 1
        fi
```

## Test Data Management

### 1. Test Data Builders

```csharp
public class TestDataBuilder
{
    public static CollisionTestData CreateCollisionScenario(string scenarioName)
    {
        return scenarioName switch
        {
            "SimpleOverlap" => new CollisionTestData
            {
                Object1 = new CircleTestObject { Position = new Vector2(0, 0), Radius = 10 },
                Object2 = new CircleTestObject { Position = new Vector2(5, 0), Radius = 10 },
                ExpectedCollision = true,
                ExpectedPenetration = 5.0f
            },
            
            "JustTouching" => new CollisionTestData
            {
                Object1 = new CircleTestObject { Position = new Vector2(0, 0), Radius = 10 },
                Object2 = new CircleTestObject { Position = new Vector2(20, 0), Radius = 10 },
                ExpectedCollision = true,
                ExpectedPenetration = 0.0f
            },
            
            "HighSpeedCollision" => CreateHighSpeedCollisionData(),
            
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
    }
}
```

This comprehensive test strategy ensures high code quality, performance reliability, and maintainability for the Phase 2 Asteroids game implementation.