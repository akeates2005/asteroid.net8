# Error Handling and Resilience Patterns - Technical Specification

## Overview

The Error Handling and Resilience system provides comprehensive fault tolerance, graceful degradation, and recovery mechanisms for the Asteroids game. It implements industry-standard resilience patterns to ensure the game remains playable even when encountering errors or system failures.

## System Architecture

```
ResilienceSystem
├── Core Components
│   ├── ErrorHandler (Central error processing)
│   ├── CircuitBreaker (Failure prevention)
│   ├── RetryPolicy (Automatic retry logic)
│   ├── BulkheadPattern (Component isolation)
│   └── FallbackMechanism (Degraded mode operation)
├── Recovery Systems
│   ├── GameStateRecovery (State preservation/restoration)
│   ├── ComponentRecovery (System component recovery)
│   ├── MemoryRecovery (Memory leak prevention)
│   └── PerformanceRecovery (Performance degradation recovery)
├── Monitoring & Detection
│   ├── HealthMonitor (System health tracking)
│   ├── AnomalyDetector (Unusual behavior detection)
│   ├── ResourceMonitor (Resource usage tracking)
│   └── DiagnosticsCollector (Error diagnostics)
├── User Experience
│   ├── GracefulDegradation (Feature degradation)
│   ├── UserNotification (Error communication)
│   ├── ProgressPreservation (Save progress during errors)
│   └── RecoveryUI (Recovery interface)
└── Logging & Reporting
    ├── StructuredLogging (Comprehensive logging)
    ├── ErrorReporting (Automatic error reporting)
    ├── MetricsCollection (Error metrics)
    └── DebugSupport (Development debugging)
```

## Core Interfaces

### 1. Resilient Component Interface

```csharp
public interface IResilientComponent : IDisposable
{
    /// <summary>
    /// Component name for identification
    /// </summary>
    string ComponentName { get; }
    
    /// <summary>
    /// Current health status of the component
    /// </summary>
    HealthStatus HealthStatus { get; }
    
    /// <summary>
    /// Whether component is in degraded mode
    /// </summary>
    bool IsDegraded { get; }
    
    /// <summary>
    /// Execute an operation with resilience patterns
    /// </summary>
    Task<T> ExecuteWithResilience<T>(Func<Task<T>> operation, ResilienceOptions options = null);
    
    /// <summary>
    /// Execute an operation synchronously with resilience
    /// </summary>
    T ExecuteWithResilience<T>(Func<T> operation, ResilienceOptions options = null);
    
    /// <summary>
    /// Handle error and determine recovery action
    /// </summary>
    void OnError(Exception error, ErrorContext context);
    
    /// <summary>
    /// Check if component can recover from specific error
    /// </summary>
    bool CanRecover(Exception error);
    
    /// <summary>
    /// Attempt to recover from error state
    /// </summary>
    Task<RecoveryResult> RecoverAsync();
    
    /// <summary>
    /// Perform health check
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync();
    
    /// <summary>
    /// Enter degraded mode
    /// </summary>
    void EnterDegradedMode(DegradationReason reason);
    
    /// <summary>
    /// Exit degraded mode
    /// </summary>
    bool TryExitDegradedMode();
    
    /// <summary>
    /// Get current error statistics
    /// </summary>
    ComponentErrorStatistics GetErrorStatistics();
}

public enum HealthStatus
{
    Healthy,      // Operating normally
    Warning,      // Minor issues detected
    Degraded,     // Operating with reduced functionality
    Critical,     // Serious issues, may fail soon
    Failed        // Component has failed
}

public enum DegradationReason
{
    HighErrorRate,
    ResourceExhaustion,
    PerformanceIssues,
    ExternalDependencyFailure,
    UserRequested
}
```

### 2. Circuit Breaker Interface

```csharp
public interface ICircuitBreaker
{
    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    CircuitBreakerState State { get; }
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    int FailureCount { get; }
    
    /// <summary>
    /// Execute operation through circuit breaker
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
    
    /// <summary>
    /// Check if operation can be executed
    /// </summary>
    bool CanExecute { get; }
    
    /// <summary>
    /// Manual reset of circuit breaker
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Manual trip of circuit breaker
    /// </summary>
    void Trip();
    
    /// <summary>
    /// Get circuit breaker statistics
    /// </summary>
    CircuitBreakerStatistics GetStatistics();
}

public enum CircuitBreakerState
{
    Closed,     // Normal operation
    Open,       // Blocking all operations
    HalfOpen    // Testing if operations can resume
}
```

## Core Resilience Implementation

### 1. Advanced Error Handler

```csharp
public class AdvancedErrorHandler : IDisposable
{
    private readonly Dictionary<Type, IErrorStrategy> _errorStrategies;
    private readonly Dictionary<string, ComponentErrorTracker> _componentTrackers;
    private readonly ILogger _logger;
    private readonly ErrorMetrics _metrics;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new object();
    
    // Configuration
    private readonly ErrorHandlingConfiguration _config;
    
    public AdvancedErrorHandler(ErrorHandlingConfiguration config = null)
    {
        _config = config ?? ErrorHandlingConfiguration.Default;
        _errorStrategies = new Dictionary<Type, IErrorStrategy>();
        _componentTrackers = new Dictionary<string, ComponentErrorTracker>();
        _logger = LogManager.GetLogger("ErrorHandler");
        _metrics = new ErrorMetrics();
        
        InitializeDefaultStrategies();
        
        // Cleanup timer for error tracking
        _cleanupTimer = new Timer(CleanupOldErrors, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    private void InitializeDefaultStrategies()
    {
        // Default strategies for common exception types
        _errorStrategies[typeof(OutOfMemoryException)] = new OutOfMemoryStrategy();
        _errorStrategies[typeof(StackOverflowException)] = new StackOverflowStrategy();
        _errorStrategies[typeof(AccessViolationException)] = new AccessViolationStrategy();
        _errorStrategies[typeof(TimeoutException)] = new TimeoutStrategy();
        _errorStrategies[typeof(UnauthorizedAccessException)] = new UnauthorizedAccessStrategy();
        _errorStrategies[typeof(FileNotFoundException)] = new FileNotFoundStrategy();
        _errorStrategies[typeof(DirectoryNotFoundException)] = new DirectoryNotFoundStrategy();
        _errorStrategies[typeof(IOException)] = new IOExceptionStrategy();
        _errorStrategies[typeof(ArgumentException)] = new ArgumentExceptionStrategy();
        _errorStrategies[typeof(InvalidOperationException)] = new InvalidOperationStrategy();
        _errorStrategies[typeof(NotSupportedException)] = new NotSupportedStrategy();
    }
    
    public void RegisterErrorStrategy<T>(IErrorStrategy strategy) where T : Exception
    {
        lock (_lock)
        {
            _errorStrategies[typeof(T)] = strategy;
        }
    }
    
    public ErrorHandlingResult HandleError(Exception error, ErrorContext context)
    {
        lock (_lock)
        {
            try
            {
                // Record error metrics
                _metrics.RecordError(error, context);
                
                // Update component error tracking
                if (!string.IsNullOrEmpty(context.ComponentName))
                {
                    UpdateComponentErrorTracking(context.ComponentName, error);
                }
                
                // Find appropriate strategy
                var strategy = FindErrorStrategy(error);
                
                // Execute strategy
                var result = strategy.HandleError(error, context);
                
                // Log the error and result
                LogError(error, context, result);
                
                // Check if component should be degraded
                CheckComponentDegradation(context.ComponentName, error);
                
                return result;
            }
            catch (Exception handlingError)
            {
                // Error in error handling - use fallback
                return HandleErrorHandlingFailure(error, handlingError, context);
            }
        }
    }
    
    private IErrorStrategy FindErrorStrategy(Exception error)
    {
        var errorType = error.GetType();
        
        // Look for exact type match first
        if (_errorStrategies.TryGetValue(errorType, out var strategy))
        {
            return strategy;
        }
        
        // Look for base type matches
        var baseType = errorType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (_errorStrategies.TryGetValue(baseType, out strategy))
            {
                return strategy;
            }
            baseType = baseType.BaseType;
        }
        
        // Return default strategy
        return new DefaultErrorStrategy();
    }
    
    private void UpdateComponentErrorTracking(string componentName, Exception error)
    {
        if (!_componentTrackers.TryGetValue(componentName, out var tracker))
        {
            tracker = new ComponentErrorTracker(componentName);
            _componentTrackers[componentName] = tracker;
        }
        
        tracker.RecordError(error);
    }
    
    private void CheckComponentDegradation(string componentName, Exception error)
    {
        if (string.IsNullOrEmpty(componentName)) return;
        
        var tracker = _componentTrackers.GetValueOrDefault(componentName);
        if (tracker == null) return;
        
        // Check error rate threshold
        var errorRate = tracker.GetErrorRate(TimeSpan.FromMinutes(5));
        if (errorRate > _config.DegradationThreshold)
        {
            // Notify component to degrade
            NotifyComponentDegradation(componentName, DegradationReason.HighErrorRate);
        }
    }
    
    private void NotifyComponentDegradation(string componentName, DegradationReason reason)
    {
        // This would notify the actual component to enter degraded mode
        // Implementation depends on the component management system
        _logger.LogWarning($"Component {componentName} should degrade due to {reason}");
    }
    
    private ErrorHandlingResult HandleErrorHandlingFailure(Exception originalError, 
        Exception handlingError, ErrorContext context)
    {
        // Emergency fallback - just log and return failure
        _logger.LogCritical($"Error handling failed: {handlingError.Message}. " +
                           $"Original error: {originalError.Message}");
        
        return new ErrorHandlingResult
        {
            Action = ErrorAction.Fail,
            Message = "Critical error handling failure",
            ShouldRetry = false,
            RecoveryPossible = false
        };
    }
    
    private void LogError(Exception error, ErrorContext context, ErrorHandlingResult result)
    {
        var logLevel = GetLogLevel(error, result);
        var message = $"Error handled: {error.Message} | Action: {result.Action} | " +
                     $"Component: {context.ComponentName ?? "Unknown"}";
        
        _logger.Log(logLevel, message, error);
        
        // Additional structured logging
        _logger.LogStructured(logLevel, "ErrorHandled", new
        {
            ErrorType = error.GetType().Name,
            ErrorMessage = error.Message,
            Component = context.ComponentName,
            Operation = context.OperationName,
            Action = result.Action.ToString(),
            CanRetry = result.ShouldRetry,
            CanRecover = result.RecoveryPossible,
            StackTrace = error.StackTrace
        });
    }
    
    private LogLevel GetLogLevel(Exception error, ErrorHandlingResult result)
    {
        return error switch
        {
            OutOfMemoryException => LogLevel.Critical,
            StackOverflowException => LogLevel.Critical,
            AccessViolationException => LogLevel.Critical,
            _ => result.Action switch
            {
                ErrorAction.Fail => LogLevel.Error,
                ErrorAction.Retry => LogLevel.Warning,
                ErrorAction.Degrade => LogLevel.Warning,
                ErrorAction.Ignore => LogLevel.Information,
                _ => LogLevel.Warning
            }
        };
    }
    
    private void CleanupOldErrors(object state)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromHours(1);
            
            foreach (var tracker in _componentTrackers.Values)
            {
                tracker.CleanupOldErrors(cutoff);
            }
            
            // Remove empty trackers
            var emptyTrackers = _componentTrackers.Where(kvp => kvp.Value.ErrorCount == 0)
                                                  .Select(kvp => kvp.Key).ToList();
            
            foreach (var key in emptyTrackers)
            {
                _componentTrackers.Remove(key);
            }
        }
    }
    
    public ErrorHandlingStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new ErrorHandlingStatistics
            {
                TotalErrors = _metrics.TotalErrors,
                ErrorsByType = _metrics.GetErrorsByType(),
                ErrorsByComponent = _componentTrackers.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.GetStatistics()),
                AverageHandlingTime = _metrics.AverageHandlingTime,
                SuccessfulRecoveries = _metrics.SuccessfulRecoveries,
                FailedRecoveries = _metrics.FailedRecoveries
            };
        }
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _metrics?.Dispose();
        
        foreach (var strategy in _errorStrategies.Values)
        {
            strategy?.Dispose();
        }
    }
}
```

### 2. Circuit Breaker Implementation

```csharp
public class AdvancedCircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerConfiguration _config;
    private readonly object _lock = new object();
    private readonly Timer _stateTimer;
    
    private CircuitBreakerState _state;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private DateTime _nextAttemptTime;
    private readonly CircuitBreakerMetrics _metrics;
    
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                UpdateStateIfNeeded();
                return _state;
            }
        }
    }
    
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }
    
    public bool CanExecute => State != CircuitBreakerState.Open;
    
    public AdvancedCircuitBreaker(CircuitBreakerConfiguration config = null)
    {
        _config = config ?? CircuitBreakerConfiguration.Default;
        _state = CircuitBreakerState.Closed;
        _failureCount = 0;
        _metrics = new CircuitBreakerMetrics();
        
        // Timer to check state transitions
        _stateTimer = new Timer(CheckStateTransition, null,
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        
        lock (_lock)
        {
            UpdateStateIfNeeded();
            
            if (_state == CircuitBreakerState.Open)
            {
                _metrics.RecordBlocked();
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation().ConfigureAwait(false);
            OnSuccess(stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex, stopwatch.Elapsed);
            throw;
        }
    }
    
    private void OnSuccess(TimeSpan executionTime)
    {
        lock (_lock)
        {
            _metrics.RecordSuccess(executionTime);
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Successful call in half-open state - close circuit
                _state = CircuitBreakerState.Closed;
                _failureCount = 0;
                _metrics.RecordStateChange(CircuitBreakerState.Closed);
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                // Reset failure count on successful calls
                _failureCount = Math.Max(0, _failureCount - 1);
            }
        }
    }
    
    private void OnFailure(Exception exception, TimeSpan executionTime)
    {
        lock (_lock)
        {
            _metrics.RecordFailure(exception, executionTime);
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            
            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Failure in half-open state - back to open
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow + _config.OpenTimeout;
                _metrics.RecordStateChange(CircuitBreakerState.Open);
            }
            else if (_state == CircuitBreakerState.Closed && 
                     _failureCount >= _config.FailureThreshold)
            {
                // Too many failures - open circuit
                _state = CircuitBreakerState.Open;
                _nextAttemptTime = DateTime.UtcNow + _config.OpenTimeout;
                _metrics.RecordStateChange(CircuitBreakerState.Open);
            }
        }
    }
    
    private void UpdateStateIfNeeded()
    {
        if (_state == CircuitBreakerState.Open && DateTime.UtcNow >= _nextAttemptTime)
        {
            _state = CircuitBreakerState.HalfOpen;
            _metrics.RecordStateChange(CircuitBreakerState.HalfOpen);
        }
    }
    
    private void CheckStateTransition(object state)
    {
        lock (_lock)
        {
            UpdateStateIfNeeded();
        }
    }
    
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _nextAttemptTime = DateTime.MinValue;
            _metrics.RecordReset();
        }
    }
    
    public void Trip()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Open;
            _nextAttemptTime = DateTime.UtcNow + _config.OpenTimeout;
            _metrics.RecordTrip();
        }
    }
    
    public CircuitBreakerStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new CircuitBreakerStatistics
            {
                State = _state,
                FailureCount = _failureCount,
                LastFailureTime = _lastFailureTime,
                NextAttemptTime = _nextAttemptTime,
                TotalCalls = _metrics.TotalCalls,
                SuccessfulCalls = _metrics.SuccessfulCalls,
                FailedCalls = _metrics.FailedCalls,
                BlockedCalls = _metrics.BlockedCalls,
                AverageExecutionTime = _metrics.AverageExecutionTime,
                StateHistory = _metrics.GetStateHistory()
            };
        }
    }
    
    public void Dispose()
    {
        _stateTimer?.Dispose();
        _metrics?.Dispose();
    }
}
```

### 3. Retry Policy Implementation

```csharp
public class AdvancedRetryPolicy
{
    private readonly RetryConfiguration _config;
    private readonly ILogger _logger;
    private readonly RetryMetrics _metrics;
    
    public AdvancedRetryPolicy(RetryConfiguration config = null)
    {
        _config = config ?? RetryConfiguration.Default;
        _logger = LogManager.GetLogger("RetryPolicy");
        _metrics = new RetryMetrics();
    }
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = null)
    {
        var attempt = 0;
        var exceptions = new List<Exception>();
        
        while (attempt <= _config.MaxRetries)
        {
            try
            {
                var result = await operation().ConfigureAwait(false);
                
                if (attempt > 0)
                {
                    _metrics.RecordSuccessAfterRetry(operationName, attempt);
                    _logger.LogInformation($"Operation '{operationName}' succeeded after {attempt} retries");
                }
                else
                {
                    _metrics.RecordSuccess(operationName);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                attempt++;
                
                if (attempt > _config.MaxRetries)
                {
                    _metrics.RecordFinalFailure(operationName, attempt - 1);
                    throw new RetryExhaustedException(
                        $"Operation '{operationName}' failed after {_config.MaxRetries} retries",
                        new AggregateException(exceptions));
                }
                
                if (!ShouldRetry(ex))
                {
                    _metrics.RecordNonRetriableFailure(operationName, ex);
                    throw;
                }
                
                var delay = CalculateDelay(attempt);
                _metrics.RecordRetryAttempt(operationName, attempt, delay);
                
                _logger.LogWarning($"Operation '{operationName}' failed (attempt {attempt}/{_config.MaxRetries}), " +
                                 $"retrying in {delay.TotalMilliseconds}ms: {ex.Message}");
                
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }
        
        // Should never reach here
        throw new InvalidOperationException("Retry logic error");
    }
    
    public T Execute<T>(Func<T> operation, string operationName = null)
    {
        return ExecuteAsync(() => Task.FromResult(operation()), operationName).GetAwaiter().GetResult();
    }
    
    private bool ShouldRetry(Exception exception)
    {
        // Check if exception type is retryable
        var exceptionType = exception.GetType();
        
        if (_config.NonRetryableExceptions.Contains(exceptionType))
        {
            return false;
        }
        
        if (_config.RetryableExceptions.Any() && !_config.RetryableExceptions.Contains(exceptionType))
        {
            return false;
        }
        
        // Check specific conditions
        return exception switch
        {
            ArgumentException => false,
            ArgumentNullException => false,
            InvalidOperationException => false,
            NotSupportedException => false,
            OutOfMemoryException => false,
            StackOverflowException => false,
            TimeoutException => true,
            IOException => true,
            UnauthorizedAccessException => false,
            _ => true // Default to retryable
        };
    }
    
    private TimeSpan CalculateDelay(int attempt)
    {
        return _config.BackoffStrategy switch
        {
            BackoffStrategy.Fixed => _config.BaseDelay,
            BackoffStrategy.Linear => TimeSpan.FromTicks(_config.BaseDelay.Ticks * attempt),
            BackoffStrategy.Exponential => TimeSpan.FromTicks(_config.BaseDelay.Ticks * (1L << (attempt - 1))),
            BackoffStrategy.ExponentialWithJitter => CalculateJitteredDelay(attempt),
            _ => _config.BaseDelay
        };
    }
    
    private TimeSpan CalculateJitteredDelay(int attempt)
    {
        var exponentialDelay = TimeSpan.FromTicks(_config.BaseDelay.Ticks * (1L << (attempt - 1)));
        var maxDelay = Math.Min(exponentialDelay.TotalMilliseconds, _config.MaxDelay.TotalMilliseconds);
        
        // Add jitter (random variation of ±25%)
        var jitter = (Random.Shared.NextDouble() - 0.5) * 0.5;
        var jitteredDelay = maxDelay * (1 + jitter);
        
        return TimeSpan.FromMilliseconds(Math.Max(0, jitteredDelay));
    }
    
    public RetryPolicyStatistics GetStatistics()
    {
        return new RetryPolicyStatistics
        {
            TotalOperations = _metrics.TotalOperations,
            SuccessfulOperations = _metrics.SuccessfulOperations,
            FailedOperations = _metrics.FailedOperations,
            OperationsWithRetries = _metrics.OperationsWithRetries,
            AverageRetryCount = _metrics.AverageRetryCount,
            TotalRetryAttempts = _metrics.TotalRetryAttempts,
            RetrySuccessRate = _metrics.RetrySuccessRate
        };
    }
}

public enum BackoffStrategy
{
    Fixed,
    Linear,
    Exponential,
    ExponentialWithJitter
}
```

### 4. Graceful Degradation System

```csharp
public class GracefulDegradationManager : IDisposable
{
    private readonly Dictionary<string, FeatureController> _features;
    private readonly DegradationConfiguration _config;
    private readonly ILogger _logger;
    private readonly Timer _monitoringTimer;
    private readonly object _lock = new object();
    
    public GracefulDegradationManager(DegradationConfiguration config = null)
    {
        _config = config ?? DegradationConfiguration.Default;
        _features = new Dictionary<string, FeatureController>();
        _logger = LogManager.GetLogger("GracefulDegradation");
        
        InitializeFeatures();
        
        // Monitor system resources and adjust features accordingly
        _monitoringTimer = new Timer(MonitorAndAdjust, null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    private void InitializeFeatures()
    {
        // Game-specific features that can be degraded
        RegisterFeature("ParticleEffects", new ParticleEffectsController(), FeaturePriority.Low);
        RegisterFeature("AdvancedLighting", new LightingController(), FeaturePriority.Low);
        RegisterFeature("SoundEffects", new SoundEffectsController(), FeaturePriority.Medium);
        RegisterFeature("VisualEffects", new VisualEffectsController(), FeaturePriority.Medium);
        RegisterFeature("BackgroundMusic", new MusicController(), FeaturePriority.Low);
        RegisterFeature("CollisionDetection", new CollisionController(), FeaturePriority.High);
        RegisterFeature("GameLogic", new GameLogicController(), FeaturePriority.Critical);
        RegisterFeature("Rendering", new RenderingController(), FeaturePriority.Critical);
    }
    
    public void RegisterFeature(string featureName, IFeatureController controller, FeaturePriority priority)
    {
        lock (_lock)
        {
            _features[featureName] = new FeatureController
            {
                Name = featureName,
                Controller = controller,
                Priority = priority,
                CurrentLevel = FeatureLevel.Full,
                IsEnabled = true
            };
        }
    }
    
    public void DegradeFeature(string featureName, FeatureLevel targetLevel, DegradationReason reason)
    {
        lock (_lock)
        {
            if (!_features.TryGetValue(featureName, out var feature))
            {
                _logger.LogWarning($"Feature '{featureName}' not found for degradation");
                return;
            }
            
            if (targetLevel >= feature.CurrentLevel)
            {
                return; // Already at target level or better
            }
            
            var previousLevel = feature.CurrentLevel;
            feature.CurrentLevel = targetLevel;
            feature.DegradationReason = reason;
            feature.DegradationTime = DateTime.UtcNow;
            
            try
            {
                feature.Controller.SetFeatureLevel(targetLevel);
                _logger.LogInformation($"Feature '{featureName}' degraded from {previousLevel} to {targetLevel} " +
                                     $"due to {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to degrade feature '{featureName}': {ex.Message}", ex);
                feature.CurrentLevel = previousLevel; // Rollback
            }
        }
    }
    
    public void RestoreFeature(string featureName, FeatureLevel targetLevel)
    {
        lock (_lock)
        {
            if (!_features.TryGetValue(featureName, out var feature))
            {
                return;
            }
            
            if (targetLevel <= feature.CurrentLevel)
            {
                return; // Already at target level or better
            }
            
            var previousLevel = feature.CurrentLevel;
            
            try
            {
                feature.Controller.SetFeatureLevel(targetLevel);
                feature.CurrentLevel = targetLevel;
                feature.DegradationReason = null;
                feature.RestoreTime = DateTime.UtcNow;
                
                _logger.LogInformation($"Feature '{featureName}' restored from {previousLevel} to {targetLevel}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to restore feature '{featureName}': {ex.Message}", ex);
            }
        }
    }
    
    private void MonitorAndAdjust(object state)
    {
        try
        {
            var systemMetrics = CollectSystemMetrics();
            AdjustFeaturesBasedOnMetrics(systemMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during degradation monitoring: {ex.Message}", ex);
        }
    }
    
    private SystemMetrics CollectSystemMetrics()
    {
        return new SystemMetrics
        {
            MemoryUsagePercent = GetMemoryUsagePercent(),
            CPUUsagePercent = GetCPUUsagePercent(),
            FrameRate = GetCurrentFrameRate(),
            FrameTime = GetCurrentFrameTime()
        };
    }
    
    private void AdjustFeaturesBasedOnMetrics(SystemMetrics metrics)
    {
        lock (_lock)
        {
            // Memory-based degradation
            if (metrics.MemoryUsagePercent > _config.MemoryCriticalThreshold)
            {
                DegradeMemoryIntensiveFeatures(FeatureLevel.Minimal);
            }
            else if (metrics.MemoryUsagePercent > _config.MemoryWarningThreshold)
            {
                DegradeMemoryIntensiveFeatures(FeatureLevel.Reduced);
            }
            else if (metrics.MemoryUsagePercent < _config.MemoryRestoreThreshold)
            {
                RestoreMemoryIntensiveFeatures();
            }
            
            // Performance-based degradation
            if (metrics.FrameRate < _config.FrameRateCriticalThreshold)
            {
                DegradePerformanceIntensiveFeatures(FeatureLevel.Minimal);
            }
            else if (metrics.FrameRate < _config.FrameRateWarningThreshold)
            {
                DegradePerformanceIntensiveFeatures(FeatureLevel.Reduced);
            }
            else if (metrics.FrameRate > _config.FrameRateRestoreThreshold)
            {
                RestorePerformanceIntensiveFeatures();
            }
        }
    }
    
    private void DegradeMemoryIntensiveFeatures(FeatureLevel targetLevel)
    {
        var memoryIntensiveFeatures = new[] { "ParticleEffects", "AdvancedLighting", "VisualEffects" };
        
        foreach (var featureName in memoryIntensiveFeatures)
        {
            DegradeFeature(featureName, targetLevel, DegradationReason.ResourceExhaustion);
        }
    }
    
    private void DegradePerformanceIntensiveFeatures(FeatureLevel targetLevel)
    {
        var performanceIntensiveFeatures = new[] { "ParticleEffects", "AdvancedLighting", "VisualEffects" };
        
        foreach (var featureName in performanceIntensiveFeatures)
        {
            DegradeFeature(featureName, targetLevel, DegradationReason.PerformanceIssues);
        }
    }
    
    private void RestoreMemoryIntensiveFeatures()
    {
        var features = _features.Values.Where(f => 
            f.DegradationReason == DegradationReason.ResourceExhaustion);
        
        foreach (var feature in features)
        {
            RestoreFeature(feature.Name, FeatureLevel.Full);
        }
    }
    
    private void RestorePerformanceIntensiveFeatures()
    {
        var features = _features.Values.Where(f => 
            f.DegradationReason == DegradationReason.PerformanceIssues);
        
        foreach (var feature in features)
        {
            RestoreFeature(feature.Name, FeatureLevel.Full);
        }
    }
    
    private float GetMemoryUsagePercent()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var workingSet = Process.GetCurrentProcess().WorkingSet64;
        return Math.Min(100f, (float)(totalMemory * 100.0 / workingSet));
    }
    
    private float GetCPUUsagePercent()
    {
        // Implementation would depend on available CPU monitoring tools
        return 0f; // Placeholder
    }
    
    private float GetCurrentFrameRate()
    {
        // Would be provided by the game engine
        return 60f; // Placeholder
    }
    
    private float GetCurrentFrameTime()
    {
        // Would be provided by the game engine
        return 16.67f; // Placeholder
    }
    
    public DegradationStatus GetDegradationStatus()
    {
        lock (_lock)
        {
            return new DegradationStatus
            {
                Features = _features.Values.Select(f => new FeatureStatus
                {
                    Name = f.Name,
                    Priority = f.Priority,
                    CurrentLevel = f.CurrentLevel,
                    IsEnabled = f.IsEnabled,
                    DegradationReason = f.DegradationReason,
                    DegradationTime = f.DegradationTime,
                    RestoreTime = f.RestoreTime
                }).ToList(),
                SystemMetrics = CollectSystemMetrics()
            };
        }
    }
    
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        
        lock (_lock)
        {
            foreach (var feature in _features.Values)
            {
                feature.Controller?.Dispose();
            }
            _features.Clear();
        }
    }
    
    private class FeatureController
    {
        public string Name { get; set; }
        public IFeatureController Controller { get; set; }
        public FeaturePriority Priority { get; set; }
        public FeatureLevel CurrentLevel { get; set; }
        public bool IsEnabled { get; set; }
        public DegradationReason? DegradationReason { get; set; }
        public DateTime? DegradationTime { get; set; }
        public DateTime? RestoreTime { get; set; }
    }
}

public enum FeatureLevel
{
    Disabled = 0,
    Minimal = 1,
    Reduced = 2,
    Standard = 3,
    Full = 4
}

public enum FeaturePriority
{
    Critical = 0,   // Never degrade
    High = 1,       // Degrade only in emergency
    Medium = 2,     // Degrade when needed
    Low = 3         // First to degrade
}
```

## Game State Recovery System

### 1. Game State Preservation

```csharp
public class GameStateRecoverySystem : IDisposable
{
    private readonly Dictionary<string, IGameStateProvider> _stateProviders;
    private readonly GameStateStorage _storage;
    private readonly Timer _autoSaveTimer;
    private readonly ILogger _logger;
    private readonly object _lock = new object();
    
    public GameStateRecoverySystem(GameStateRecoveryConfiguration config = null)
    {
        config ??= GameStateRecoveryConfiguration.Default;
        
        _stateProviders = new Dictionary<string, IGameStateProvider>();
        _storage = new GameStateStorage(config.StoragePath);
        _logger = LogManager.GetLogger("GameStateRecovery");
        
        // Auto-save timer
        _autoSaveTimer = new Timer(PerformAutoSave, null,
            config.AutoSaveInterval, config.AutoSaveInterval);
    }
    
    public void RegisterStateProvider(string key, IGameStateProvider provider)
    {
        lock (_lock)
        {
            _stateProviders[key] = provider;
        }
    }
    
    public async Task<bool> SaveGameStateAsync(string checkpointName = null)
    {
        try
        {
            checkpointName ??= $"auto_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            var gameState = new Dictionary<string, object>();
            
            lock (_lock)
            {
                foreach (var kvp in _stateProviders)
                {
                    try
                    {
                        var state = kvp.Value.GetState();
                        gameState[kvp.Key] = state;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to get state from provider '{kvp.Key}': {ex.Message}");
                    }
                }
            }
            
            await _storage.SaveStateAsync(checkpointName, gameState);
            _logger.LogInformation($"Game state saved as checkpoint '{checkpointName}'");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save game state: {ex.Message}", ex);
            return false;
        }
    }
    
    public async Task<bool> RestoreGameStateAsync(string checkpointName = null)
    {
        try
        {
            checkpointName ??= await _storage.GetLatestCheckpointAsync();
            if (checkpointName == null)
            {
                _logger.LogWarning("No checkpoint found for restoration");
                return false;
            }
            
            var gameState = await _storage.LoadStateAsync(checkpointName);
            if (gameState == null)
            {
                _logger.LogWarning($"Failed to load checkpoint '{checkpointName}'");
                return false;
            }
            
            var successCount = 0;
            var totalCount = 0;
            
            lock (_lock)
            {
                foreach (var kvp in _stateProviders)
                {
                    totalCount++;
                    
                    if (gameState.TryGetValue(kvp.Key, out var state))
                    {
                        try
                        {
                            kvp.Value.RestoreState(state);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to restore state for provider '{kvp.Key}': {ex.Message}");
                        }
                    }
                }
            }
            
            _logger.LogInformation($"Game state restored from checkpoint '{checkpointName}' " +
                                 $"({successCount}/{totalCount} providers successful)");
            
            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to restore game state: {ex.Message}", ex);
            return false;
        }
    }
    
    public async Task<RecoveryResult> AttemptRecoveryAsync()
    {
        _logger.LogInformation("Attempting game state recovery...");
        
        try
        {
            // Try to restore from the most recent checkpoint
            var restored = await RestoreGameStateAsync();
            
            if (restored)
            {
                return new RecoveryResult
                {
                    Success = true,
                    Message = "Game state successfully restored from checkpoint",
                    RecoveryType = RecoveryType.StateRestoration
                };
            }
            
            // If no checkpoint available, try to recover to a safe state
            return await RecoverToSafeStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Recovery attempt failed: {ex.Message}", ex);
            return new RecoveryResult
            {
                Success = false,
                Message = $"Recovery failed: {ex.Message}",
                RecoveryType = RecoveryType.Failed
            };
        }
    }
    
    private async Task<RecoveryResult> RecoverToSafeStateAsync()
    {
        _logger.LogInformation("Attempting recovery to safe state...");
        
        try
        {
            // Reset all providers to safe state
            var resetCount = 0;
            
            lock (_lock)
            {
                foreach (var provider in _stateProviders.Values)
                {
                    try
                    {
                        provider.ResetToSafeState();
                        resetCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to reset provider to safe state: {ex.Message}");
                    }
                }
            }
            
            if (resetCount > 0)
            {
                // Save the safe state as a new checkpoint
                await SaveGameStateAsync("recovery_safe_state");
                
                return new RecoveryResult
                {
                    Success = true,
                    Message = $"Recovered to safe state ({resetCount} components reset)",
                    RecoveryType = RecoveryType.SafeStateReset
                };
            }
            
            return new RecoveryResult
            {
                Success = false,
                Message = "Unable to recover to safe state",
                RecoveryType = RecoveryType.Failed
            };
        }
        catch (Exception ex)
        {
            return new RecoveryResult
            {
                Success = false,
                Message = $"Safe state recovery failed: {ex.Message}",
                RecoveryType = RecoveryType.Failed
            };
        }
    }
    
    private void PerformAutoSave(object state)
    {
        Task.Run(async () =>
        {
            try
            {
                await SaveGameStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Auto-save failed: {ex.Message}", ex);
            }
        });
    }
    
    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        _storage?.Dispose();
    }
}

public enum RecoveryType
{
    StateRestoration,
    SafeStateReset,
    ComponentReset,
    Failed
}
```

This error handling and resilience system provides comprehensive fault tolerance with circuit breakers, retry policies, graceful degradation, and state recovery mechanisms to ensure the Asteroids game remains stable and playable even under adverse conditions.