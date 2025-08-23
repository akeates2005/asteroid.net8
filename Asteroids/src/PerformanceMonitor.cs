using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Comprehensive performance monitoring system for the Asteroids game
    /// Tracks FPS, memory usage, object counts, and performance metrics
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly List<float> _frameTimeHistory = new List<float>();
        private readonly List<int> _objectCountHistory = new List<int>();
        private readonly List<long> _memoryHistory = new List<long>();
        private readonly Dictionary<string, ProfilingData> _profiledOperations = new Dictionary<string, ProfilingData>();
        
        private float _frameTime;
        private int _fps;
        private long _memoryUsage;
        private int _totalObjects;
        private bool _showDashboard = false;
        private DateTime _lastUpdate = DateTime.Now;
        private readonly Process _currentProcess = Process.GetCurrentProcess();
        
        // Performance thresholds
        public float TargetFPS { get; set; } = 60f;
        public long MemoryWarningThreshold { get; set; } = 100 * 1024 * 1024; // 100MB
        public float FrameTimeWarningThreshold { get; set; } = 16.7f; // 60 FPS threshold
        
        // Statistics
        public PerformanceStats CurrentStats { get; private set; } = new PerformanceStats();
        
        public void Update(float deltaTime, int objectCount, int asteroidCount = 0, int bulletCount = 0, 
            int particleCount = 0, int score = 0, int level = 1, int lives = 3)
        {
            _frameTime = deltaTime * 1000f; // Convert to milliseconds
            _fps = (int)(1.0f / deltaTime);
            _memoryUsage = _currentProcess.WorkingSet64;
            _totalObjects = objectCount;
            
            // Update histories (keep last 300 frames = 5 seconds at 60 FPS)
            _frameTimeHistory.Add(_frameTime);
            if (_frameTimeHistory.Count > 300) _frameTimeHistory.RemoveAt(0);
            
            _objectCountHistory.Add(objectCount);
            if (_objectCountHistory.Count > 300) _objectCountHistory.RemoveAt(0);
            
            _memoryHistory.Add(_memoryUsage);
            if (_memoryHistory.Count > 300) _memoryHistory.RemoveAt(0);
            
            // Update statistics
            UpdateStats(asteroidCount, bulletCount, particleCount, score, level, lives);
            
            // Check for F12 key to toggle dashboard
            if (Raylib.IsKeyPressed(KeyboardKey.F12))
            {
                _showDashboard = !_showDashboard;
            }
            
            // Draw dashboard if enabled
            if (_showDashboard)
            {
                DrawDashboard();
            }
        }
        
        public IDisposable ProfileOperation(string operationName)
        {
            return new ProfilingScope(this, operationName);
        }
        
        internal void StartProfiling(string operationName)
        {
            if (!_profiledOperations.ContainsKey(operationName))
            {
                _profiledOperations[operationName] = new ProfilingData();
            }
            _profiledOperations[operationName].StartTime = DateTime.Now;
        }
        
        internal void EndProfiling(string operationName)
        {
            if (_profiledOperations.ContainsKey(operationName))
            {
                var data = _profiledOperations[operationName];
                var duration = (DateTime.Now - data.StartTime).TotalMilliseconds;
                data.TotalTime += duration;
                data.CallCount++;
                data.LastDuration = duration;
                data.MaxDuration = Math.Max(data.MaxDuration, duration);
                if (data.MinDuration == 0 || duration < data.MinDuration)
                    data.MinDuration = duration;
            }
        }
        
        private void UpdateStats(int asteroidCount, int bulletCount, int particleCount, int score, int level, int lives)
        {
            CurrentStats = new PerformanceStats
            {
                FPS = _fps,
                FrameTime = _frameTime,
                MemoryUsage = _memoryUsage,
                TotalObjects = _totalObjects,
                AsteroidCount = asteroidCount,
                BulletCount = bulletCount,
                ParticleCount = particleCount,
                Score = score,
                Level = level,
                Lives = lives,
                AverageFrameTime = CalculateAverage(_frameTimeHistory),
                MaxFrameTime = CalculateMax(_frameTimeHistory),
                MinFrameTime = CalculateMin(_frameTimeHistory),
                AverageObjectCount = (int)CalculateAverage(_objectCountHistory.ConvertAll(x => (float)x)),
                MaxMemoryUsage = CalculateMax(_memoryHistory.ConvertAll(x => (float)x)),
                IsPerformanceWarning = _fps < TargetFPS || _memoryUsage > MemoryWarningThreshold || _frameTime > FrameTimeWarningThreshold
            };
        }
        
        private float CalculateAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float sum = 0f;
            foreach (var value in values) sum += value;
            return sum / values.Count;
        }
        
        private float CalculateMax(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float max = values[0];
            foreach (var value in values) if (value > max) max = value;
            return max;
        }
        
        private float CalculateMin(List<float> values)
        {
            if (values.Count == 0) return 0f;
            float min = values[0];
            foreach (var value in values) if (value < min) min = value;
            return min;
        }
        
        private void DrawDashboard()
        {
            const int panelWidth = 350;
            const int panelHeight = 500;
            const int margin = 10;
            
            int x = Raylib.GetScreenWidth() - panelWidth - margin;
            int y = margin;
            
            // Background panel
            Raylib.DrawRectangle(x - 10, y - 10, panelWidth + 20, panelHeight + 20, new Color(0, 0, 0, 180));
            Raylib.DrawRectangleLines(x - 10, y - 10, panelWidth + 20, panelHeight + 20, Color.Gray);
            
            // Title
            Raylib.DrawText("PERFORMANCE MONITOR", x, y, 16, Color.White);
            y += 25;
            
            // FPS and Frame Time
            var fpsColor = _fps >= TargetFPS ? Color.Green : Color.Red;
            Raylib.DrawText($"FPS: {_fps}", x, y, 14, fpsColor);
            Raylib.DrawText($"Frame Time: {_frameTime:F1}ms", x + 120, y, 14, fpsColor);
            y += 20;
            
            // Memory Usage
            var memoryMB = _memoryUsage / (1024 * 1024);
            var memoryColor = _memoryUsage < MemoryWarningThreshold ? Color.Green : Color.Orange;
            Raylib.DrawText($"Memory: {memoryMB}MB", x, y, 14, memoryColor);
            y += 20;
            
            // Object Counts
            Raylib.DrawText($"Total Objects: {_totalObjects}", x, y, 14, Color.White);
            y += 15;
            Raylib.DrawText($"  Asteroids: {CurrentStats.AsteroidCount}", x + 20, y, 12, Color.SkyBlue);
            y += 15;
            Raylib.DrawText($"  Bullets: {CurrentStats.BulletCount}", x + 20, y, 12, Color.Yellow);
            y += 15;
            Raylib.DrawText($"  Particles: {CurrentStats.ParticleCount}", x + 20, y, 12, Color.Orange);
            y += 25;
            
            // Game Stats
            Raylib.DrawText($"Score: {CurrentStats.Score}", x, y, 14, Color.White);
            Raylib.DrawText($"Level: {CurrentStats.Level}", x + 120, y, 14, Color.White);
            Raylib.DrawText($"Lives: {CurrentStats.Lives}", x + 200, y, 14, Color.White);
            y += 25;
            
            // Performance Statistics
            Raylib.DrawText("PERFORMANCE STATS", x, y, 14, Color.Gray);
            y += 20;
            Raylib.DrawText($"Avg Frame: {CurrentStats.AverageFrameTime:F1}ms", x, y, 12, Color.White);
            y += 15;
            Raylib.DrawText($"Min Frame: {CurrentStats.MinFrameTime:F1}ms", x, y, 12, Color.Green);
            y += 15;
            Raylib.DrawText($"Max Frame: {CurrentStats.MaxFrameTime:F1}ms", x, y, 12, Color.Red);
            y += 15;
            Raylib.DrawText($"Avg Objects: {CurrentStats.AverageObjectCount}", x, y, 12, Color.White);
            y += 25;
            
            // Profiled Operations
            if (_profiledOperations.Count > 0)
            {
                Raylib.DrawText("PROFILED OPERATIONS", x, y, 14, Color.Gray);
                y += 20;
                
                foreach (var kvp in _profiledOperations)
                {
                    var data = kvp.Value;
                    var avgTime = data.CallCount > 0 ? data.TotalTime / data.CallCount : 0;
                    Raylib.DrawText($"{kvp.Key}: {avgTime:F2}ms avg", x, y, 12, Color.White);
                    y += 15;
                    
                    if (y > Raylib.GetScreenHeight() - 50) break; // Prevent overflow
                }
            }
            
            // Warning indicators
            if (CurrentStats.IsPerformanceWarning)
            {
                y = Raylib.GetScreenHeight() - 50;
                Raylib.DrawText("⚠️ PERFORMANCE WARNING", x, y, 14, Color.Red);
            }
            
            // Instructions
            Raylib.DrawText("Press F12 to toggle", x, Raylib.GetScreenHeight() - 30, 10, Color.Gray);
        }
        
        public void ExportReport(string filename = "performance_report.txt")
        {
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    writer.WriteLine("=== ASTEROIDS PERFORMANCE REPORT ===");
                    writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();
                    
                    writer.WriteLine("Current Statistics:");
                    writer.WriteLine($"  FPS: {CurrentStats.FPS}");
                    writer.WriteLine($"  Frame Time: {CurrentStats.FrameTime:F2}ms");
                    writer.WriteLine($"  Memory Usage: {CurrentStats.MemoryUsage / (1024 * 1024):F1}MB");
                    writer.WriteLine($"  Total Objects: {CurrentStats.TotalObjects}");
                    writer.WriteLine();
                    
                    writer.WriteLine("Performance Statistics:");
                    writer.WriteLine($"  Average Frame Time: {CurrentStats.AverageFrameTime:F2}ms");
                    writer.WriteLine($"  Min Frame Time: {CurrentStats.MinFrameTime:F2}ms");
                    writer.WriteLine($"  Max Frame Time: {CurrentStats.MaxFrameTime:F2}ms");
                    writer.WriteLine($"  Average Objects: {CurrentStats.AverageObjectCount}");
                    writer.WriteLine($"  Max Memory: {CurrentStats.MaxMemoryUsage / (1024 * 1024):F1}MB");
                    writer.WriteLine();
                    
                    if (_profiledOperations.Count > 0)
                    {
                        writer.WriteLine("Profiled Operations:");
                        foreach (var kvp in _profiledOperations)
                        {
                            var data = kvp.Value;
                            var avgTime = data.CallCount > 0 ? data.TotalTime / data.CallCount : 0;
                            writer.WriteLine($"  {kvp.Key}:");
                            writer.WriteLine($"    Call Count: {data.CallCount}");
                            writer.WriteLine($"    Total Time: {data.TotalTime:F2}ms");
                            writer.WriteLine($"    Average Time: {avgTime:F2}ms");
                            writer.WriteLine($"    Min Time: {data.MinDuration:F2}ms");
                            writer.WriteLine($"    Max Time: {data.MaxDuration:F2}ms");
                            writer.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to export performance report: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Current performance statistics snapshot
    /// </summary>
    public struct PerformanceStats
    {
        public int FPS;
        public float FrameTime;
        public long MemoryUsage;
        public int TotalObjects;
        public int AsteroidCount;
        public int BulletCount;
        public int ParticleCount;
        public int Score;
        public int Level;
        public int Lives;
        public float AverageFrameTime;
        public float MaxFrameTime;
        public float MinFrameTime;
        public int AverageObjectCount;
        public float MaxMemoryUsage;
        public bool IsPerformanceWarning;
    }
    
    /// <summary>
    /// Profiling data for individual operations
    /// </summary>
    internal class ProfilingData
    {
        public DateTime StartTime;
        public double TotalTime;
        public int CallCount;
        public double LastDuration;
        public double MaxDuration;
        public double MinDuration;
    }
    
    /// <summary>
    /// Disposable profiling scope for automatic timing
    /// </summary>
    internal class ProfilingScope : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        
        public ProfilingScope(PerformanceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _monitor.StartProfiling(_operationName);
        }
        
        public void Dispose()
        {
            _monitor.EndProfiling(_operationName);
        }
    }
}