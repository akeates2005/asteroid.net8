using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public enum AnimationType
    {
        CountUp,
        Pulse,
        Slide,
        Fade,
        Bounce,
        Shake
    }

    public class AnimatedValue<T>
    {
        public T Current { get; private set; }
        public T Target { get; private set; }
        public AnimationType AnimationType { get; set; }
        public float Duration { get; set; }
        public float ElapsedTime { get; private set; }
        public bool IsAnimating => ElapsedTime < Duration;

        public AnimatedValue(T initialValue)
        {
            Current = initialValue;
            Target = initialValue;
            AnimationType = AnimationType.CountUp;
            Duration = 0.5f;
        }

        public void SetTarget(T newTarget, AnimationType animationType = AnimationType.CountUp, float duration = 0.5f)
        {
            Target = newTarget;
            AnimationType = animationType;
            Duration = duration;
            ElapsedTime = 0;
        }

        public void Update(float deltaTime)
        {
            if (!IsAnimating) return;

            ElapsedTime += deltaTime;
            float progress = Math.Min(ElapsedTime / Duration, 1.0f);

            // Apply easing based on animation type
            float easedProgress = AnimationType switch
            {
                AnimationType.CountUp => EaseOutQuad(progress),
                AnimationType.Pulse => EaseInOutSine(progress),
                AnimationType.Slide => EaseOutCubic(progress),
                AnimationType.Fade => Linear(progress),
                AnimationType.Bounce => EaseOutBounce(progress),
                AnimationType.Shake => progress, // Handled separately
                _ => Linear(progress)
            };

            Current = InterpolateValue(Current, Target, easedProgress);
        }

        private T InterpolateValue(T from, T to, float t)
        {
            if (typeof(T) == typeof(int))
            {
                int fromInt = (int)(object)from;
                int toInt = (int)(object)to;
                return (T)(object)(int)(fromInt + (toInt - fromInt) * t);
            }
            else if (typeof(T) == typeof(float))
            {
                float fromFloat = (float)(object)from;
                float toFloat = (float)(object)to;
                return (T)(object)(fromFloat + (toFloat - fromFloat) * t);
            }
            else if (typeof(T) == typeof(Vector2))
            {
                Vector2 fromVec = (Vector2)(object)from;
                Vector2 toVec = (Vector2)(object)to;
                return (T)(object)Vector2.Lerp(fromVec, toVec, t);
            }
            
            return to; // Fallback for unsupported types
        }

        // Easing functions
        private static float Linear(float t) => t;
        private static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
        private static float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
        private static float EaseInOutSine(float t) => -(MathF.Cos(MathF.PI * t) - 1) / 2;
        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
                return n1 * t * t;
            else if (t < 2 / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5 / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }

    public class AnimatedHUD
    {
        private readonly AnimatedValue<int> _animatedScore;
        private readonly AnimatedValue<int> _animatedLevel;
        private readonly AnimatedValue<float> _animatedShield;
        private readonly AnimatedValue<Vector2> _scorePosition;
        private readonly AnimatedValue<Vector2> _levelPosition;

        private float _scorePulse = 1.0f;
        private float _shieldPulse = 1.0f;
        private float _levelFlash = 0.0f;
        private bool _scoreIncreasing = false;
        private bool _shieldCritical = false;

        private readonly List<FloatingText> _floatingTexts = new();
        private readonly Random _random = new();

        // HUD element positions
        private readonly Vector2 _scoreBasePosition = new(10, 10);
        private readonly Vector2 _levelBasePosition = new(10, 40);
        private readonly Vector2 _shieldBasePosition = new(10, 70);
        private readonly Vector2 _livesBasePosition = new(10, 100);

        public AnimatedHUD()
        {
            _animatedScore = new AnimatedValue<int>(0);
            _animatedLevel = new AnimatedValue<int>(1);
            _animatedShield = new AnimatedValue<float>(0.0f);
            _scorePosition = new AnimatedValue<Vector2>(_scoreBasePosition);
            _levelPosition = new AnimatedValue<Vector2>(_levelBasePosition);
        }

        public void SetScore(int newScore)
        {
            bool wasIncreasing = _animatedScore.Target < newScore;
            _animatedScore.SetTarget(newScore, AnimationType.CountUp, 0.8f);
            
            if (wasIncreasing && newScore > _animatedScore.Current)
            {
                _scoreIncreasing = true;
                AddFloatingText($"+{newScore - _animatedScore.Current}", _scoreBasePosition + new Vector2(0, -20), DynamicTheme.GetUIColor());
            }
        }

        public void SetLevel(int newLevel)
        {
            _animatedLevel.SetTarget(newLevel, AnimationType.Bounce, 1.0f);
            _levelFlash = 1.0f; // Trigger level flash effect
            
            // Add floating "LEVEL UP!" text
            if (newLevel > _animatedLevel.Current)
            {
                AddFloatingText("LEVEL UP!", new Vector2(Raylib.GetScreenWidth() / 2 - 60, Raylib.GetScreenHeight() / 2), 
                    DynamicTheme.GetLevelCompleteColor(), 2.0f, 30);
            }
        }

        public void SetShieldLevel(float shieldPercent)
        {
            _animatedShield.SetTarget(shieldPercent, AnimationType.Slide, 0.3f);
            _shieldCritical = shieldPercent < 0.3f && shieldPercent > 0;
        }

        public void Update(float deltaTime)
        {
            // Update animated values
            _animatedScore.Update(deltaTime);
            _animatedLevel.Update(deltaTime);
            _animatedShield.Update(deltaTime);
            _scorePosition.Update(deltaTime);
            _levelPosition.Update(deltaTime);

            // Update pulse effects
            if (_scoreIncreasing && _animatedScore.IsAnimating)
            {
                _scorePulse = 1.0f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 10);
            }
            else
            {
                _scorePulse = 1.0f;
                _scoreIncreasing = false;
            }

            if (_shieldCritical)
            {
                _shieldPulse = 0.7f + 0.3f * MathF.Sin((float)Raylib.GetTime() * 6);
            }
            else
            {
                _shieldPulse = 1.0f;
            }

            // Update level flash
            if (_levelFlash > 0)
            {
                _levelFlash -= deltaTime * 2.0f;
                _levelFlash = Math.Max(0, _levelFlash);
            }

            // Update floating texts
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                _floatingTexts[i].Update(deltaTime);
                if (!_floatingTexts[i].Active)
                {
                    _floatingTexts.RemoveAt(i);
                }
            }
        }

        public void DrawHUD(Player player, int level, int score, int lives)
        {
            // Update values if needed
            if (_animatedLevel.Target != level) SetLevel(level);
            if (_animatedScore.Target != score) SetScore(score);

            float shieldPercent = player.IsShieldActive ? player.ShieldDuration / GameConstants.MAX_SHIELD_DURATION : 0;
            if (Math.Abs(_animatedShield.Target - shieldPercent) > 0.01f) SetShieldLevel(shieldPercent);

            // Draw score with pulse effect
            DrawAnimatedScore();

            // Draw level with flash effect
            DrawAnimatedLevel();

            // Draw shield meter
            DrawShieldMeter(player);

            // Draw lives
            DrawLives(lives);

            // Draw player health/status
            DrawPlayerStatus(player);

            // Draw floating texts
            DrawFloatingTexts();

            // Draw performance info if enabled
            if (Raylib.IsKeyDown(KeyboardKey.F12))
            {
                DrawPerformanceInfo();
            }
        }

        private void DrawAnimatedScore()
        {
            string scoreText = $"Score: {_animatedScore.Current:N0}";
            float fontSize = 20 * _scorePulse;
            Color scoreColor = DynamicTheme.GetTextColor();

            // Add glow effect for high scores
            if (_animatedScore.Current > 10000)
            {
                Color glowColor = new Color(255, 255, 255, 30);
                for (int i = 1; i <= 3; i++)
                {
                    Raylib.DrawTextEx(Raylib.GetFontDefault(), scoreText,
                        _scorePosition.Current + new Vector2(-i, -i), fontSize, 1, glowColor);
                }
            }

            // Main score text
            if (_scoreIncreasing)
            {
                scoreColor = DynamicTheme.GetPulsingColor(scoreColor, 8.0f, 0.5f);
            }

            Raylib.DrawTextEx(Raylib.GetFontDefault(), scoreText,
                _scorePosition.Current, fontSize, 1, scoreColor);
        }

        private void DrawAnimatedLevel()
        {
            string levelText = $"Level: {_animatedLevel.Current}";
            float fontSize = 20;
            Color levelColor = DynamicTheme.GetTextColor();

            // Apply flash effect
            if (_levelFlash > 0)
            {
                levelColor = HUDUtilities.LerpColor(levelColor, DynamicTheme.GetLevelCompleteColor(), _levelFlash);
                fontSize += 5 * _levelFlash;
            }

            Raylib.DrawTextEx(Raylib.GetFontDefault(), levelText,
                _levelPosition.Current, fontSize, 1, levelColor);
        }

        private void DrawShieldMeter(Player player)
        {
            Vector2 shieldPos = _shieldBasePosition;
            string shieldText = "Shield:";
            
            Raylib.DrawTextEx(Raylib.GetFontDefault(), shieldText, shieldPos, 16, 1, DynamicTheme.GetTextColor());
            
            // Shield meter background
            Raylib_cs.Rectangle meterBg = new(shieldPos.X + 60, shieldPos.Y + 2, 100, 12);
            Raylib.DrawRectangleRec(meterBg, new Color(169, 169, 169, 255));
            
            // Shield meter fill
            if (_animatedShield.Current > 0)
            {
                Raylib_cs.Rectangle meterFill = new(meterBg.X + 1, meterBg.Y + 1, 
                    (meterBg.Width - 2) * _animatedShield.Current, meterBg.Height - 2);
                
                Color shieldColor = DynamicTheme.GetShieldColor(_shieldPulse);
                if (_shieldCritical)
                {
                    shieldColor = DynamicTheme.GetFlashingColor(shieldColor, new Color(255, 0, 0, 255), 6.0f);
                }
                
                Raylib.DrawRectangleRec(meterFill, shieldColor);
            }
            
            // Shield cooldown indicator
            if (!player.IsShieldActive && player.ShieldCooldown > 0)
            {
                float cooldownPercent = 1.0f - (player.ShieldCooldown / GameConstants.MAX_SHIELD_COOLDOWN);
                Raylib_cs.Rectangle cooldownFill = new(meterBg.X + 1, meterBg.Y + 1, 
                    (meterBg.Width - 2) * cooldownPercent, meterBg.Height - 2);
                
                Raylib.DrawRectangleRec(cooldownFill, new Color(100, 100, 100, 128));
            }
            
            Raylib.DrawRectangleLinesEx(meterBg, 1, DynamicTheme.GetTextColor());
        }

        private void DrawLives(int lives)
        {
            Vector2 livesPos = _livesBasePosition;
            string livesText = $"Lives: ";
            
            Raylib.DrawTextEx(Raylib.GetFontDefault(), livesText, livesPos, 16, 1, DynamicTheme.GetTextColor());
            
            // Draw life indicators as small ship icons
            Vector2 iconPos = livesPos + new Vector2(50, 0);
            for (int i = 0; i < lives; i++)
            {
                DrawMiniShip(iconPos + new Vector2(i * 20, 0), DynamicTheme.GetPlayerColor());
            }
        }

        private void DrawPlayerStatus(Player player)
        {
            // Health indicator (assuming player has health system)
            Vector2 statusPos = new(Raylib.GetScreenWidth() - 150, 10);
            
            // Engine status
            if (Raylib.IsKeyDown(KeyboardKey.Up))
            {
                Color engineColor = DynamicTheme.GetEngineColor(1.0f);
                string engineText = "ENGINE";
                Raylib.DrawTextEx(Raylib.GetFontDefault(), engineText, statusPos, 12, 1, engineColor);
            }
            
            // Shield status
            if (player.IsShieldActive)
            {
                Vector2 shieldStatusPos = statusPos + new Vector2(0, 20);
                Color shieldColor = DynamicTheme.GetShieldColor(_shieldPulse);
                string shieldText = "SHIELD ACTIVE";
                Raylib.DrawTextEx(Raylib.GetFontDefault(), shieldText, shieldStatusPos, 12, 1, shieldColor);
            }
        }

        private void DrawFloatingTexts()
        {
            foreach (var text in _floatingTexts)
            {
                text.Draw();
            }
        }

        private void DrawPerformanceInfo()
        {
            Vector2 perfPos = new(Raylib.GetScreenWidth() - 200, Raylib.GetScreenHeight() - 100);
            Color perfColor = new Color(200, 200, 200, 200);
            
            string fpsText = $"FPS: {Raylib.GetFPS()}";
            string particlesText = $"Particles: {_floatingTexts.Count}";
            string effectsText = $"Effects: Active";
            
            Raylib.DrawTextEx(Raylib.GetFontDefault(), fpsText, perfPos, 12, 1, perfColor);
            Raylib.DrawTextEx(Raylib.GetFontDefault(), particlesText, perfPos + new Vector2(0, 15), 12, 1, perfColor);
            Raylib.DrawTextEx(Raylib.GetFontDefault(), effectsText, perfPos + new Vector2(0, 30), 12, 1, perfColor);
        }

        private void DrawMiniShip(Vector2 position, Color color)
        {
            Vector2[] points = {
                position + new Vector2(0, -6),
                position + new Vector2(-4, 4),
                position + new Vector2(4, 4)
            };
            
            Raylib.DrawTriangleLines(points[0], points[1], points[2], color);
        }

        private void AddFloatingText(string text, Vector2 position, Color color, float duration = 1.0f, int fontSize = 16)
        {
            _floatingTexts.Add(new FloatingText(text, position, color, duration, fontSize));
        }

        public void OnScoreIncrease(int amount, Vector2 position)
        {
            AddFloatingText($"+{amount}", position, DynamicTheme.GetUIColor(), 1.5f, 20);
        }

        public void OnPlayerHit()
        {
            // Add screen shake to HUD elements
            _scorePosition.SetTarget(_scoreBasePosition + new Vector2(_random.Next(-5, 5), 0), AnimationType.Shake, 0.2f);
            _levelPosition.SetTarget(_levelBasePosition + new Vector2(_random.Next(-5, 5), 0), AnimationType.Shake, 0.2f);
        }

        public void Reset()
        {
            _animatedScore.SetTarget(0, AnimationType.Fade, 0.0f);
            _animatedLevel.SetTarget(1, AnimationType.Fade, 0.0f);
            _animatedShield.SetTarget(0.0f, AnimationType.Fade, 0.0f);
            _scorePosition.SetTarget(_scoreBasePosition, AnimationType.Fade, 0.0f);
            _levelPosition.SetTarget(_levelBasePosition, AnimationType.Fade, 0.0f);
            
            _floatingTexts.Clear();
            _scoreIncreasing = false;
            _shieldCritical = false;
            _scorePulse = 1.0f;
            _shieldPulse = 1.0f;
            _levelFlash = 0.0f;
        }
    }

    public class FloatingText
    {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Color Color { get; set; }
        public float Lifespan { get; set; }
        public float MaxLifespan { get; set; }
        public int FontSize { get; set; }
        public bool Active => Lifespan > 0;

        public FloatingText(string text, Vector2 position, Color color, float lifespan = 1.0f, int fontSize = 16)
        {
            Text = text;
            Position = position;
            Color = color;
            Lifespan = lifespan;
            MaxLifespan = lifespan;
            FontSize = fontSize;
            Velocity = new Vector2(0, -30); // Float upward
        }

        public void Update(float deltaTime)
        {
            if (!Active) return;

            Position += Velocity * deltaTime;
            Lifespan -= deltaTime;
            
            // Fade out over time
            float alpha = Lifespan / MaxLifespan;
            Color = new Color(
                Math.Clamp((int)Color.R, 0, 255),
                Math.Clamp((int)Color.G, 0, 255),
                Math.Clamp((int)Color.B, 0, 255),
                Math.Clamp((int)(alpha * 255), 0, 255)
            );
        }

        public void Draw()
        {
            if (!Active) return;

            Raylib.DrawTextEx(Raylib.GetFontDefault(), Text, Position, FontSize, 1, Color);
        }
    }

    public static class HUDUtilities
    {
        public static Color LerpColor(Color from, Color to, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return new Color(
                (int)(from.R + (to.R - from.R) * t),
                (int)(from.G + (to.G - from.G) * t),
                (int)(from.B + (to.B - from.B) * t),
                (int)(from.A + (to.A - from.A) * t)
            );
        }
    }
}