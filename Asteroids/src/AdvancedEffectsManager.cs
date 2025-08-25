using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Defines types of visual screen effects available for game feedback and atmosphere
    /// </summary>
    public enum ScreenEffectType
    {
        /// <summary>Screen shaking effect for impacts and explosions</summary>
        Shake,
        /// <summary>Brief color flash effect for damage or important events</summary>
        Flash,
        /// <summary>Gradual screen color transition for scene changes</summary>
        Fade,
        /// <summary>Pulsing scale effect for rhythmic feedback</summary>
        Pulse,
        /// <summary>Camera zoom effect for dramatic emphasis</summary>
        Zoom,
        /// <summary>Screen distortion effect for special events</summary>
        Distortion
    }

    /// <summary>
    /// Represents an individual screen effect with timing, intensity, and easing functions.
    /// Manages the lifecycle and visual properties of screen-wide visual effects.
    /// </summary>
    public class ScreenEffect
    {
        public ScreenEffectType Type { get; set; }
        public float Intensity { get; set; }
        public float Duration { get; set; }
        public float MaxDuration { get; set; }
        public Color Color { get; set; }
        public bool Active { get; set; }
        public Func<float, float> EasingFunction { get; set; }

        public ScreenEffect(ScreenEffectType type, float intensity, float duration, Color color = default)
        {
            Type = type;
            Intensity = intensity;
            Duration = duration;
            MaxDuration = duration;
            Color = color.A == 0 ? new Color(255, 255, 255, 255) : color;
            Active = true;
            EasingFunction = Linear;
        }

        public void Update(float deltaTime)
        {
            if (!Active) return;

            Duration -= deltaTime;
            if (Duration <= 0)
            {
                Active = false;
            }
        }

        public float GetCurrentIntensity()
        {
            if (!Active) return 0;
            float progress = 1.0f - (Duration / MaxDuration);
            return Intensity * EasingFunction(progress);
        }

        // Easing functions
        public static float Linear(float t) => t;
        public static float EaseOut(float t) => 1 - (1 - t) * (1 - t);
        public static float EaseIn(float t) => t * t;
        public static float EaseInOut(float t) => t < 0.5f ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
        public static float Bounce(float t) => MathF.Abs(MathF.Sin(t * MathF.PI));
    }

    /// <summary>
    /// Manages multiple screen effects including shake, flash, fade, and zoom effects.
    /// Provides a centralized system for coordinating visual feedback throughout the game.
    /// </summary>
    public class AdvancedEffectsManager
    {
        private readonly List<ScreenEffect> _activeEffects = new();
        private Vector2 _shakeOffset = Vector2.Zero;
        private float _zoomLevel = 1.0f;
        private Color _flashColor = new Color(0, 0, 0, 0);
        private float _fadeAlpha = 0.0f;
        private readonly Random _random = new();

        /// <summary>
        /// Updates all active screen effects and applies their cumulative results.
        /// Should be called once per frame before rendering.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame</param>
        public void Update(float deltaTime)
        {

            // Reset effect values
            _shakeOffset = Vector2.Zero;
            _zoomLevel = 1.0f;
            _flashColor = new Color(0, 0, 0, 0);
            _fadeAlpha = 0.0f;

            // Update all active effects
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Update(deltaTime);
                
                if (!_activeEffects[i].Active)
                {
                    _activeEffects.RemoveAt(i);
                    continue;
                }

                ApplyEffect(_activeEffects[i]);
            }
        }

        private void ApplyEffect(ScreenEffect effect)
        {
            float intensity = effect.GetCurrentIntensity();

            switch (effect.Type)
            {
                case ScreenEffectType.Shake:
                    ApplyShakeEffect(intensity);
                    break;

                case ScreenEffectType.Flash:
                    ApplyFlashEffect(effect.Color, intensity);
                    break;

                case ScreenEffectType.Fade:
                    ApplyFadeEffect(effect.Color, intensity);
                    break;

                case ScreenEffectType.Pulse:
                    ApplyPulseEffect(intensity);
                    break;

                case ScreenEffectType.Zoom:
                    ApplyZoomEffect(intensity);
                    break;

                case ScreenEffectType.Distortion:
                    ApplyDistortionEffect(intensity);
                    break;
            }
        }

        private void ApplyShakeEffect(float intensity)
        {
            _shakeOffset += new Vector2(
                (float)(_random.NextDouble() - 0.5) * intensity * 2,
                (float)(_random.NextDouble() - 0.5) * intensity * 2
            );
        }

        private void ApplyFlashEffect(Color color, float intensity)
        {
            if (intensity > _flashColor.A / 255.0f)
            {
                _flashColor = new Color(
                    Math.Clamp((int)color.R, 0, 255),
                    Math.Clamp((int)color.G, 0, 255),
                    Math.Clamp((int)color.B, 0, 255),
                    Math.Clamp((int)(intensity * 255), 0, 255)
                );
            }
        }

        private void ApplyFadeEffect(Color color, float intensity)
        {
            if (intensity > _fadeAlpha)
            {
                _fadeAlpha = intensity;
                _flashColor = new Color(
                    Math.Clamp((int)color.R, 0, 255),
                    Math.Clamp((int)color.G, 0, 255),
                    Math.Clamp((int)color.B, 0, 255),
                    Math.Clamp((int)(intensity * 255), 0, 255)
                );
            }
        }

        private void ApplyPulseEffect(float intensity)
        {
            float pulse = 0.95f + 0.05f * intensity * MathF.Sin((float)Raylib.GetTime() * 8);
            _zoomLevel *= pulse;
        }

        private void ApplyZoomEffect(float intensity)
        {
            _zoomLevel += intensity * 0.1f;
        }

        private void ApplyDistortionEffect(float intensity)
        {
            // For now, apply as a mild shake with wave pattern
            float wave = MathF.Sin((float)Raylib.GetTime() * 10) * intensity;
            _shakeOffset += new Vector2(wave * 2, 0);
        }

        // Public methods to trigger effects
        /// <summary>
        /// Adds a screen shake effect with the specified intensity and duration.
        /// Creates camera displacement for impact feedback.
        /// </summary>
        /// <param name="intensity">Shake intensity (higher values = more displacement)</param>
        /// <param name="duration">Duration of the effect in seconds</param>
        public void AddScreenShake(float intensity, float duration = 0.2f)
        {
            var effect = new ScreenEffect(ScreenEffectType.Shake, intensity, duration)
            {
                EasingFunction = ScreenEffect.EaseOut
            };
            _activeEffects.Add(effect);
        }

        /// <summary>
        /// Adds a brief color flash effect over the entire screen.
        /// Useful for damage indicators or important events.
        /// </summary>
        /// <param name="color">Color of the flash effect</param>
        /// <param name="intensity">Flash opacity intensity (0.0 to 1.0)</param>
        /// <param name="duration">Duration of the effect in seconds</param>
        public void AddScreenFlash(Color color, float intensity, float duration = 0.1f)
        {
            var effect = new ScreenEffect(ScreenEffectType.Flash, intensity, duration, color)
            {
                EasingFunction = ScreenEffect.EaseOut
            };
            _activeEffects.Add(effect);
        }

        public void AddScreenFade(Color color, float intensity, float duration = 1.0f)
        {
            var effect = new ScreenEffect(ScreenEffectType.Fade, intensity, duration, color)
            {
                EasingFunction = ScreenEffect.EaseInOut
            };
            _activeEffects.Add(effect);
        }

        public void AddScreenPulse(float intensity, float duration = 0.5f)
        {
            var effect = new ScreenEffect(ScreenEffectType.Pulse, intensity, duration)
            {
                EasingFunction = ScreenEffect.Bounce
            };
            _activeEffects.Add(effect);
        }

        public void AddScreenZoom(float intensity, float duration = 0.3f)
        {
            var effect = new ScreenEffect(ScreenEffectType.Zoom, intensity, duration)
            {
                EasingFunction = ScreenEffect.EaseInOut
            };
            _activeEffects.Add(effect);
        }

        /// <summary>
        /// Adds a combined hit effect with shake and red flash based on damage amount.
        /// Provides immediate visual feedback for player damage.
        /// </summary>
        /// <param name="damage">Damage multiplier affecting intensity (1.0 = normal damage)</param>
        public void AddHitEffect(float damage = 1.0f)
        {
            // Combine shake and flash for impact
            AddScreenShake(damage * 5.0f, 0.15f);
            AddScreenFlash(new Color(255, 0, 0, 255), damage * 0.3f, 0.1f);
        }

        /// <summary>
        /// Adds an explosion effect with distance-based intensity calculation.
        /// Shake intensity decreases based on distance from screen center.
        /// </summary>
        /// <param name="position">World position of the explosion</param>
        /// <param name="size">Size multiplier affecting the intensity (1.0 = normal explosion)</param>
        public void AddExplosionEffect(Vector2 position, float size = 1.0f)
        {
            // Distance-based shake intensity
            Vector2 screenCenter = new Vector2(
                Raylib.GetScreenWidth() / 2,
                Raylib.GetScreenHeight() / 2
            );
            
            float distance = Vector2.Distance(position, screenCenter);
            float maxDistance = MathF.Sqrt(
                screenCenter.X * screenCenter.X + screenCenter.Y * screenCenter.Y
            );
            
            float intensityFromDistance = 1.0f - (distance / maxDistance);
            float shakeIntensity = size * intensityFromDistance * 8.0f;
            
            AddScreenShake(shakeIntensity, 0.25f);
            AddScreenFlash(DynamicTheme.GetExplosionColor(), intensityFromDistance * 0.2f, 0.15f);
        }

        public void AddShieldActivationEffect()
        {
            AddScreenPulse(0.5f, 0.3f);
            AddScreenFlash(DynamicTheme.GetShieldColor(), 0.2f, 0.2f);
        }

        public void AddLevelTransitionEffect(bool completed = true)
        {
            if (completed)
            {
                AddScreenFade(DynamicTheme.GetLevelCompleteColor(), 0.8f, 1.0f);
                AddScreenPulse(0.3f, 1.0f);
            }
            else
            {
                AddScreenFade(new Color(0, 0, 0, 255), 1.0f, 0.5f);
            }
        }

        public void AddGameOverEffect()
        {
            AddScreenShake(10.0f, 1.0f);
            AddScreenFade(DynamicTheme.GetGameOverColor(), 0.7f, 2.0f);
        }

        // Camera matrix with all effects applied
        /// <summary>
        /// Gets the cumulative camera transformation matrix with all active effects applied.
        /// Use this matrix to transform the camera for rendering with effects.
        /// </summary>
        /// <returns>4x4 transformation matrix for camera effects</returns>
        public Matrix4x4 GetCameraMatrix()
        {
            Matrix4x4 transform = Matrix4x4.Identity;

            // Apply zoom
            if (_zoomLevel != 1.0f)
            {
                transform *= Matrix4x4.CreateScale(_zoomLevel);
            }

            // Apply shake (translation)
            if (_shakeOffset != Vector2.Zero)
            {
                transform *= Matrix4x4.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);
            }

            return transform;
        }

        // Render screen-wide effects (call after all game objects are drawn)
        /// <summary>
        /// Renders screen-wide overlay effects like flashes and fades.
        /// Should be called after all game objects are drawn but before UI.
        /// </summary>
        public void RenderScreenEffects()
        {
            if (_flashColor.A > 0)
            {
                Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), _flashColor);
            }
        }

        public void Clear()
        {
            _activeEffects.Clear();
            _shakeOffset = Vector2.Zero;
            _zoomLevel = 1.0f;
            _flashColor = new Color(0, 0, 0, 0);
            _fadeAlpha = 0.0f;
        }

        // Utility methods for common game events
        public void OnPlayerHit(float damage)
        {
            AddHitEffect(damage);
        }

        public void OnAsteroidDestroyed(Vector2 position, AsteroidSize size)
        {
            float effectSize = size switch
            {
                AsteroidSize.Large => 2.0f,
                AsteroidSize.Medium => 1.0f,
                AsteroidSize.Small => 0.5f,
                _ => 1.0f
            };
            AddExplosionEffect(position, effectSize);
        }

        public void OnShieldActivated()
        {
            AddShieldActivationEffect();
        }

        public void OnLevelComplete()
        {
            AddLevelTransitionEffect(true);
        }

        public void OnGameOver()
        {
            AddGameOverEffect();
        }

        public void OnBulletFired()
        {
            // Subtle screen pulse for feedback
            AddScreenPulse(0.1f, 0.05f);
        }

        // Properties for external systems
        public Vector2 ShakeOffset => _shakeOffset;
        public float ZoomLevel => _zoomLevel;
        public bool HasActiveEffects => _activeEffects.Count > 0;
        public int ActiveEffectCount => _activeEffects.Count;
    }
}