using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Asteroids
{
    /// <summary>
    /// Generic object pool implementation for efficient memory management
    /// Reduces garbage collection pressure by reusing objects
    /// </summary>
    public class ObjectPool<T> : IDisposable where T : class, IPoolable, new()
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _resetAction;
        private readonly int _maxPoolSize;
        private int _currentCount;
        private bool _disposed;

        // Statistics
        public int TotalCreated { get; private set; }
        public int TotalReturned { get; private set; }
        public int TotalRented { get; private set; }
        public int CurrentPoolSize => _currentCount;
        public int MaxPoolSize => _maxPoolSize;

        /// <summary>
        /// Creates a new object pool
        /// </summary>
        /// <param name="maxPoolSize">Maximum number of objects to keep in pool</param>
        /// <param name="objectGenerator">Optional custom object creation function</param>
        /// <param name="resetAction">Optional custom reset action when returning objects</param>
        /// <param name="preloadCount">Number of objects to create initially</param>
        public ObjectPool(int maxPoolSize = 100, Func<T> objectGenerator = null, Action<T> resetAction = null, int preloadCount = 10)
        {
            if (maxPoolSize <= 0)
                throw new ArgumentException("Pool size must be positive", nameof(maxPoolSize));

            _objects = new ConcurrentQueue<T>();
            _maxPoolSize = maxPoolSize;
            _objectGenerator = objectGenerator ?? (() => new T());
            _resetAction = resetAction ?? (obj => obj.Reset());
            
            ErrorManager.LogDebug($"ObjectPool<{typeof(T).Name}> created with max size: {maxPoolSize}");

            // Pre-populate pool
            PreloadObjects(preloadCount);
        }

        /// <summary>
        /// Pre-populates the pool with objects
        /// </summary>
        private void PreloadObjects(int count)
        {
            count = Math.Min(count, _maxPoolSize);
            
            ErrorManager.SafeExecute(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var obj = CreateNewObject();
                    if (obj != null)
                    {
                        _objects.Enqueue(obj);
                        Interlocked.Increment(ref _currentCount);
                    }
                }
                ErrorManager.LogDebug($"ObjectPool<{typeof(T).Name}> preloaded with {_currentCount} objects");
            }, $"PreloadObjects for {typeof(T).Name}");
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one
        /// </summary>
        public T Rent()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPool<T>));

            T item = null;

            // Try to get from pool first
            if (_objects.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _currentCount);
                TotalRented++;
                
                // Ensure object is properly reset
                ErrorManager.SafeExecute(() => _resetAction(item), $"Reset pooled {typeof(T).Name}");
                
                return item;
            }

            // Create new object if pool is empty
            item = CreateNewObject();
            if (item != null)
            {
                TotalRented++;
                ErrorManager.SafeExecute(() => _resetAction(item), $"Reset new {typeof(T).Name}");
            }

            return item;
        }

        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        public void Return(T item)
        {
            if (_disposed || item == null)
                return;

            ErrorManager.SafeExecute(() =>
            {
                // Reset the object before returning to pool
                _resetAction(item);

                // Only return to pool if we have space
                if (_currentCount < _maxPoolSize)
                {
                    _objects.Enqueue(item);
                    Interlocked.Increment(ref _currentCount);
                }

                TotalReturned++;
                
            }, $"Return {typeof(T).Name} to pool");
        }

        /// <summary>
        /// Returns multiple objects to the pool
        /// </summary>
        public void Return(IEnumerable<T> items)
        {
            if (_disposed || items == null)
                return;

            foreach (var item in items)
            {
                Return(item);
            }
        }

        /// <summary>
        /// Creates a new object instance
        /// </summary>
        private T CreateNewObject()
        {
            return ErrorManager.SafeExecute(() =>
            {
                var obj = _objectGenerator();
                TotalCreated++;
                return obj;
            }, context: $"CreateNewObject {typeof(T).Name}");
        }

        /// <summary>
        /// Clears all objects from the pool
        /// </summary>
        public void Clear()
        {
            ErrorManager.SafeExecute(() =>
            {
                while (_objects.TryDequeue(out var _))
                {
                    Interlocked.Decrement(ref _currentCount);
                }
                ErrorManager.LogDebug($"ObjectPool<{typeof(T).Name}> cleared");
            }, $"Clear pool {typeof(T).Name}");
        }

        /// <summary>
        /// Optimizes pool size based on usage patterns
        /// </summary>
        public void OptimizePoolSize()
        {
            ErrorManager.SafeExecute(() =>
            {
                var targetSize = Math.Min(_maxPoolSize, Math.Max(10, TotalRented / 10));
                
                // Shrink pool if it's too large
                while (_currentCount > targetSize && _objects.TryDequeue(out var _))
                {
                    Interlocked.Decrement(ref _currentCount);
                }
                
                // Grow pool if it's too small
                while (_currentCount < targetSize / 2)
                {
                    var obj = CreateNewObject();
                    if (obj != null)
                    {
                        _objects.Enqueue(obj);
                        Interlocked.Increment(ref _currentCount);
                    }
                    else
                    {
                        break;
                    }
                }
                
                ErrorManager.LogDebug($"ObjectPool<{typeof(T).Name}> optimized to size: {_currentCount}");
            }, $"OptimizePoolSize {typeof(T).Name}");
        }

        /// <summary>
        /// Gets pool statistics
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                TypeName = typeof(T).Name,
                CurrentPoolSize = _currentCount,
                MaxPoolSize = _maxPoolSize,
                TotalCreated = TotalCreated,
                TotalRented = TotalRented,
                TotalReturned = TotalReturned,
                HitRatio = TotalRented > 0 ? (float)(TotalReturned) / TotalRented : 0f
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Clear();
            
            ErrorManager.LogInfo($"ObjectPool<{typeof(T).Name}> disposed. Stats: Created={TotalCreated}, Rented={TotalRented}, Returned={TotalReturned}");
        }
    }

    /// <summary>
    /// Interface for objects that can be pooled
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Resets the object to its initial state for reuse
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Statistics for object pools
    /// </summary>
    public struct PoolStatistics
    {
        public string TypeName { get; set; }
        public int CurrentPoolSize { get; set; }
        public int MaxPoolSize { get; set; }
        public int TotalCreated { get; set; }
        public int TotalRented { get; set; }
        public int TotalReturned { get; set; }
        public float HitRatio { get; set; }

        public override string ToString()
        {
            return $"{TypeName}: Size={CurrentPoolSize}/{MaxPoolSize}, Created={TotalCreated}, Rented={TotalRented}, Returned={TotalReturned}, Hit={HitRatio:P1}";
        }
    }
}