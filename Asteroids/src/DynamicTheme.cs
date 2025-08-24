using System;
using System.Collections.Generic;
using Raylib_cs;

namespace Asteroids
{
    public class ColorPalette
    {
        public Color PlayerColor { get; set; }
        public Color AsteroidColor { get; set; }
        public Color BulletColor { get; set; }
        public Color ExplosionColor { get; set; }
        public Color EngineColor { get; set; }
        public Color ShieldColor { get; set; }
        public Color TextColor { get; set; }
        public Color GridColor { get; set; }
        public Color GameOverColor { get; set; }
        public Color LevelCompleteColor { get; set; }
        public Color DamageColor { get; set; }
        public Color UIAccentColor { get; set; }

        public ColorPalette(Color primary, Color secondary, Color accent)
        {
            PlayerColor = primary;
            AsteroidColor = secondary;
            BulletColor = accent;
            ExplosionColor = BlendColors(primary, accent, 0.7f);
            EngineColor = BlendColors(accent, Color.White, 0.3f);
            ShieldColor = BlendColors(primary, Color.SkyBlue, 0.5f);
            TextColor = Color.White;
            GridColor = new Color(20, 20, 20, 255);
            GameOverColor = Color.Red;
            LevelCompleteColor = Color.Green;
            DamageColor = Color.Red;
            UIAccentColor = accent;
        }

        private static Color BlendColors(Color color1, Color color2, float ratio)
        {
            return new Color(
                (int)(color1.R * (1 - ratio) + color2.R * ratio),
                (int)(color1.G * (1 - ratio) + color2.G * ratio),
                (int)(color1.B * (1 - ratio) + color2.B * ratio),
                255
            );
        }
    }

    public static class DynamicTheme
    {
        private static readonly Dictionary<int, ColorPalette> _levelPalettes = new()
        {
            { 1, new ColorPalette(new Color(0, 255, 255, 255), new Color(255, 0, 255, 255), new Color(255, 255, 0, 255)) },
            { 4, new ColorPalette(new Color(135, 206, 235, 255), new Color(128, 0, 128, 255), new Color(255, 255, 255, 255)) },
            { 7, new ColorPalette(new Color(255, 165, 0, 255), new Color(255, 0, 0, 255), new Color(255, 215, 0, 255)) },
            { 10, CreateRainbowPalette() },
            { 15, CreateNeonPalette() },
            { 20, CreateCosmicPalette() }
        };

        private static int _currentLevel = 1;
        private static ColorPalette _currentPalette = _levelPalettes[1];
        private static ColorPalette _previousPalette = _levelPalettes[1];
        private static float _transitionProgress = 0.0f;
        private static bool _isTransitioning = false;

        public static void UpdateLevel(int newLevel)
        {
            if (newLevel != _currentLevel)
            {
                _previousPalette = _currentPalette;
                _currentLevel = newLevel;
                _currentPalette = GetPaletteForLevel(newLevel);
                _transitionProgress = 0.0f;
                _isTransitioning = true;
            }
        }

        public static void Update(float deltaTime)
        {
            if (_isTransitioning)
            {
                _transitionProgress += deltaTime * 2.0f; // 0.5 second transition
                if (_transitionProgress >= 1.0f)
                {
                    _transitionProgress = 1.0f;
                    _isTransitioning = false;
                }
            }
        }

        private static ColorPalette GetPaletteForLevel(int level)
        {
            // Find the highest level palette that doesn't exceed current level
            int paletteLevel = 1;
            foreach (var kvp in _levelPalettes)
            {
                if (kvp.Key <= level && kvp.Key > paletteLevel)
                {
                    paletteLevel = kvp.Key;
                }
            }
            return _levelPalettes[paletteLevel];
        }

        public static Color GetPlayerColor(float healthPercent = 1.0f)
        {
            Color baseColor = GetCurrentColor(_currentPalette.PlayerColor, _previousPalette.PlayerColor);
            
            if (healthPercent < 1.0f)
            {
                // Blend with damage color based on health
                Color damageColor = GetCurrentColor(_currentPalette.DamageColor, _previousPalette.DamageColor);
                return LerpColor(damageColor, baseColor, healthPercent);
            }
            
            return baseColor;
        }

        public static Color GetAsteroidColor(AsteroidSize size)
        {
            Color baseColor = GetCurrentColor(_currentPalette.AsteroidColor, _previousPalette.AsteroidColor);
            
            float intensity = size switch
            {
                AsteroidSize.Large => 1.0f,
                AsteroidSize.Medium => 0.8f,
                AsteroidSize.Small => 0.6f,
                _ => 1.0f
            };
            
            return LerpColor(new Color(169, 169, 169, 255), baseColor, intensity);
        }

        public static Color GetBulletColor()
        {
            return GetCurrentColor(_currentPalette.BulletColor, _previousPalette.BulletColor);
        }

        public static Color GetExplosionColor()
        {
            return GetCurrentColor(_currentPalette.ExplosionColor, _previousPalette.ExplosionColor);
        }

        public static Color GetEngineColor(float intensity = 1.0f)
        {
            Color baseColor = GetCurrentColor(_currentPalette.EngineColor, _previousPalette.EngineColor);
            return new Color(
                Math.Clamp((int)(baseColor.R * intensity), 0, 255),
                Math.Clamp((int)(baseColor.G * intensity), 0, 255),
                Math.Clamp((int)(baseColor.B * intensity), 0, 255),
                Math.Clamp((int)baseColor.A, 0, 255)
            );
        }

        public static Color GetShieldColor(float alpha = 1.0f)
        {
            Color baseColor = GetCurrentColor(_currentPalette.ShieldColor, _previousPalette.ShieldColor);
            return new Color(
                Math.Clamp((int)baseColor.R, 0, 255),
                Math.Clamp((int)baseColor.G, 0, 255),
                Math.Clamp((int)baseColor.B, 0, 255),
                Math.Clamp((int)(255 * alpha), 0, 255)
            );
        }

        public static Color GetTextColor()
        {
            return GetCurrentColor(_currentPalette.TextColor, _previousPalette.TextColor);
        }

        public static Color GetGridColor()
        {
            return GetCurrentColor(_currentPalette.GridColor, _previousPalette.GridColor);
        }

        public static Color GetGameOverColor()
        {
            return GetCurrentColor(_currentPalette.GameOverColor, _previousPalette.GameOverColor);
        }

        public static Color GetLevelCompleteColor()
        {
            return GetCurrentColor(_currentPalette.LevelCompleteColor, _previousPalette.LevelCompleteColor);
        }

        public static Color GetUIColor()
        {
            return GetCurrentColor(_currentPalette.UIAccentColor, _previousPalette.UIAccentColor);
        }

        public static Color GetUIColor(int level)
        {
            var palette = GetPaletteForLevel(level);
            return palette.UIAccentColor;
        }

        private static Color GetCurrentColor(Color currentColor, Color previousColor)
        {
            if (!_isTransitioning)
                return currentColor;

            return LerpColor(previousColor, currentColor, _transitionProgress);
        }

        private static Color LerpColor(Color from, Color to, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return new Color(
                (int)(from.R + (to.R - from.R) * t),
                (int)(from.G + (to.G - from.G) * t),
                (int)(from.B + (to.B - from.B) * t),
                (int)(from.A + (to.A - from.A) * t)
            );
        }

        private static ColorPalette CreateRainbowPalette()
        {
            return new ColorPalette(
                new Color(255, 0, 128, 255),   // Hot pink
                new Color(128, 0, 255, 255),   // Purple
                new Color(255, 255, 0, 255)    // Bright yellow
            );
        }

        private static ColorPalette CreateNeonPalette()
        {
            return new ColorPalette(
                new Color(0, 255, 255, 255),   // Neon cyan
                new Color(255, 0, 255, 255),   // Neon magenta
                new Color(0, 255, 0, 255)      // Neon green
            );
        }

        private static ColorPalette CreateCosmicPalette()
        {
            return new ColorPalette(
                new Color(138, 43, 226, 255),  // Blue violet
                new Color(75, 0, 130, 255),    // Indigo
                new Color(255, 215, 0, 255)    // Gold
            );
        }

        // Utility methods for special effects
        public static Color GetPulsingColor(Color baseColor, float frequency = 2.0f, float amplitude = 0.3f)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin((float)Raylib.GetTime() * frequency);
            float intensity = 1.0f + amplitude * pulse;
            
            return new Color(
                Math.Clamp((int)(baseColor.R * intensity), 0, 255),
                Math.Clamp((int)(baseColor.G * intensity), 0, 255),
                Math.Clamp((int)(baseColor.B * intensity), 0, 255),
                Math.Clamp((int)baseColor.A, 0, 255)
            );
        }

        public static Color GetFlashingColor(Color baseColor, Color flashColor, float frequency = 4.0f)
        {
            float flash = MathF.Sin((float)Raylib.GetTime() * frequency);
            return flash > 0 ? flashColor : baseColor;
        }

        public static Color GetHealthColor(float healthPercent)
        {
            if (healthPercent > 0.6f)
                return new Color(0, 255, 0, 255);
            else if (healthPercent > 0.3f)
                return new Color(255, 255, 0, 255);
            else
                return GetFlashingColor(new Color(255, 0, 0, 255), new Color(139, 0, 0, 255), 8.0f);
        }

        public static void ResetToLevel(int level)
        {
            _currentLevel = level;
            _currentPalette = GetPaletteForLevel(level);
            _previousPalette = _currentPalette;
            _transitionProgress = 0.0f;
            _isTransitioning = false;
        }

        public static bool IsTransitioning => _isTransitioning;
        public static int CurrentLevel => _currentLevel;
    }
}