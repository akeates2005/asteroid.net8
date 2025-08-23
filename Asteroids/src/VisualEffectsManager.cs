using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Enhanced visual effects manager for improved game feedback
    /// </summary>
    public class VisualEffectsManager
    {
        private readonly List<ScreenShake> _screenShakes;
        private readonly List<FlashEffect> _flashEffects;
        private readonly ObjectPool<TrailParticle> _trailPool;
        private readonly List<TrailParticle> _activeTrails;
        private readonly Random _random;

        private Vector2 _cameraOffset;
        private float _timeScale;

        public Vector2 CameraOffset => _cameraOffset;
        public float TimeScale => _timeScale;

        public VisualEffectsManager()
        {
            _screenShakes = new List<ScreenShake>();
            _flashEffects = new List<FlashEffect>();
            _trailPool = new ObjectPool<TrailParticle>(500, () => new TrailParticle(), particle => particle.Reset());
            _activeTrails = new List<TrailParticle>();
            _random = new Random();
            _timeScale = 1.0f;
            
            // Pool warm-up not available in current implementation
        }

        /// <summary>
        /// Update all visual effects
        /// </summary>
        public void Update()
        {
            UpdateScreenShakes();
            UpdateFlashEffects();
            UpdateTrailParticles();
            UpdateCameraEffects();
        }

        /// <summary>
        /// Add screen shake effect
        /// </summary>
        public void AddScreenShake(float intensity, float duration)
        {
            _screenShakes.Add(new ScreenShake(intensity, duration));
        }

        /// <summary>
        /// Add flash effect
        /// </summary>
        public void AddFlashEffect(Color color, float intensity, float duration)
        {
            _flashEffects.Add(new FlashEffect(color, intensity, duration));
        }

        /// <summary>
        /// Add trail particle
        /// </summary>
        public void AddTrail(Vector2 position, Vector2 velocity, Color color, float life)
        {
            var trail = _trailPool.Rent();
            trail.Initialize(position, velocity, color, life);
            _activeTrails.Add(trail);
        }

        /// <summary>
        /// Set time scale for slow motion effects
        /// </summary>
        public void SetTimeScale(float scale)
        {
            _timeScale = Math.Clamp(scale, 0.1f, 2.0f);
        }

        /// <summary>
        /// Create explosion visual effects
        /// </summary>
        public void CreateExplosionEffects(Vector2 position, float intensity)
        {
            // Screen shake based on intensity
            AddScreenShake(intensity * 3f, 0.3f);
            
            // Flash effect
            Color flashColor = intensity > 0.5f ? Color.White : Color.Orange;
            AddFlashEffect(flashColor, intensity * 0.8f, 0.2f);
            
            // Radial trail particles
            int particleCount = (int)(intensity * 20 + 10);
            for (int i = 0; i < particleCount; i++)
            {
                float angle = (float)(i * Math.PI * 2 / particleCount);
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 velocity = direction * (intensity * 4f + 2f);
                
                Color particleColor = Raylib.ColorLerp(Color.Orange, Color.Red, (float)_random.NextDouble());
                AddTrail(position, velocity, particleColor, intensity * 60f + 30f);
            }
        }

        /// <summary>
        /// Create bullet impact effects
        /// </summary>
        public void CreateBulletImpact(Vector2 position)
        {
            AddScreenShake(1f, 0.1f);
            
            // Small burst of particles
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = new Vector2(
                    (float)(_random.NextDouble() * 4 - 2),
                    (float)(_random.NextDouble() * 4 - 2)
                );
                
                AddTrail(position, velocity, Color.Yellow, 20f);
            }
        }

        /// <summary>
        /// Create player thrust trail effects
        /// </summary>
        public void CreateThrustTrail(Vector2 position, Vector2 direction, float intensity)
        {
            if (_random.NextDouble() < 0.7) // Don't create every frame
            {
                Vector2 velocity = -direction * (intensity * 2f + 1f);
                velocity += new Vector2(
                    (float)(_random.NextDouble() * 1 - 0.5),
                    (float)(_random.NextDouble() * 1 - 0.5)
                );
                
                Color trailColor = Raylib.ColorLerp(Color.Orange, Color.Red, (float)_random.NextDouble());
                AddTrail(position, velocity, trailColor, 15f);
            }
        }

        /// <summary>
        /// Update screen shake effects
        /// </summary>
        private void UpdateScreenShakes()
        {
            _cameraOffset = Vector2.Zero;
            
            for (int i = _screenShakes.Count - 1; i >= 0; i--)
            {
                var shake = _screenShakes[i];
                shake.Update();
                
                if (shake.IsExpired)
                {
                    _screenShakes.RemoveAt(i);
                }
                else
                {
                    _cameraOffset += shake.GetOffset();
                }
            }
        }

        /// <summary>
        /// Update flash effects
        /// </summary>
        private void UpdateFlashEffects()
        {
            for (int i = _flashEffects.Count - 1; i >= 0; i--)
            {
                _flashEffects[i].Update();
                
                if (_flashEffects[i].IsExpired)
                {
                    _flashEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Update trail particles
        /// </summary>
        private void UpdateTrailParticles()
        {
            for (int i = _activeTrails.Count - 1; i >= 0; i--)
            {
                _activeTrails[i].Update(_timeScale);
                
                if (!_activeTrails[i].IsActive)
                {
                    var trail = _activeTrails[i];
                    _activeTrails.RemoveAt(i);
                    _trailPool.Return(trail);
                }
            }
        }

        /// <summary>
        /// Update camera effects
        /// </summary>
        private void UpdateCameraEffects()
        {
            // Smooth camera shake dampening
            _cameraOffset *= 0.9f;
        }

        /// <summary>
        /// Draw all visual effects
        /// </summary>
        public void Draw()
        {
            DrawTrailParticles();
            DrawFlashEffects();
        }

        /// <summary>
        /// Draw trail particles
        /// </summary>
        private void DrawTrailParticles()
        {
            foreach (var trail in _activeTrails)
            {
                trail.Draw();
            }
        }

        /// <summary>
        /// Draw flash effects
        /// </summary>
        private void DrawFlashEffects()
        {
            foreach (var flash in _flashEffects)
            {
                if (!flash.IsExpired)
                {
                    var overlay = flash.GetOverlayColor();
                    Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), overlay);
                }
            }
        }

        /// <summary>
        /// Clear all effects
        /// </summary>
        public void Clear()
        {
            _screenShakes.Clear();
            _flashEffects.Clear();
            
            foreach (var trail in _activeTrails)
            {
                _trailPool.Return(trail);
            }
            _activeTrails.Clear();
            
            _cameraOffset = Vector2.Zero;
            _timeScale = 1.0f;
        }
    }

    /// <summary>
    /// Screen shake effect
    /// </summary>
    public class ScreenShake
    {
        private readonly float _intensity;
        private readonly float _duration;
        private float _remainingTime;
        private readonly Random _random;

        public bool IsExpired => _remainingTime <= 0;

        public ScreenShake(float intensity, float duration)
        {
            _intensity = intensity;
            _duration = duration;
            _remainingTime = duration;
            _random = new Random();
        }

        public void Update()
        {
            if (_remainingTime > 0)
            {
                _remainingTime -= Raylib.GetFrameTime() * 60f; // Convert to frame-based
            }
        }

        public Vector2 GetOffset()
        {
            if (IsExpired) return Vector2.Zero;

            float progress = _remainingTime / _duration;
            float currentIntensity = _intensity * progress;

            return new Vector2(
                (float)((_random.NextDouble() * 2 - 1) * currentIntensity),
                (float)((_random.NextDouble() * 2 - 1) * currentIntensity)
            );
        }
    }

    /// <summary>
    /// Flash effect for screen overlay
    /// </summary>
    public class FlashEffect
    {
        private readonly Color _color;
        private readonly float _intensity;
        private readonly float _duration;
        private float _remainingTime;

        public bool IsExpired => _remainingTime <= 0;

        public FlashEffect(Color color, float intensity, float duration)
        {
            _color = color;
            _intensity = intensity;
            _duration = duration;
            _remainingTime = duration;
        }

        public void Update()
        {
            if (_remainingTime > 0)
            {
                _remainingTime -= Raylib.GetFrameTime() * 60f; // Convert to frame-based
            }
        }

        public Color GetOverlayColor()
        {
            if (IsExpired) return new Color(0, 0, 0, 0);

            float progress = _remainingTime / _duration;
            float alpha = _intensity * progress;

            return new Color(_color.R, _color.G, _color.B, (byte)(alpha * 255));
        }
    }

    /// <summary>
    /// Trail particle for enhanced visual effects
    /// </summary>
    public class TrailParticle : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Color Color { get; set; }
        public float Life { get; set; }
        public float MaxLife { get; set; }
        public bool IsActive { get; set; }

        public void Initialize(Vector2 position, Vector2 velocity, Color color, float life)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Life = life;
            MaxLife = life;
            IsActive = true;
        }

        public void Update(float timeScale)
        {
            if (!IsActive) return;

            Position += Velocity * timeScale;
            Velocity *= 0.98f; // Friction
            Life -= timeScale;

            if (Life <= 0)
            {
                IsActive = false;
            }
        }

        public void Draw()
        {
            if (!IsActive) return;

            float alpha = Life / MaxLife;
            Color drawColor = new Color(Color.R, Color.G, Color.B, (byte)(alpha * Color.A));
            
            float size = alpha * 3f + 1f;
            Raylib.DrawCircleV(Position, size, drawColor);
        }

        public void Reset()
        {
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
            Color = Color.White;
            Life = 0;
            MaxLife = 0;
            IsActive = false;
        }
    }
}