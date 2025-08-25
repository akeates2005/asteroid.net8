using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    public enum FadePattern
    {
        Linear,
        Exponential,
        Pulse
    }

    public class TrailParticle : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public Vector2 Velocity { get; set; }
        public Color Color { get; set; }
        public float Alpha { get; set; }
        public float MaxLifespan { get; set; }
        public float Lifespan { get; set; }
        public FadePattern FadePattern { get; set; }
        public bool Active { get; set; }

        public void Initialize(Vector2 position, Vector2 velocity, Color color, float lifespan)
        {
            Position = position;
            PreviousPosition = position;
            Velocity = velocity;
            Color = color;
            Alpha = 1.0f;
            MaxLifespan = lifespan;
            Lifespan = lifespan;
            FadePattern = FadePattern.Linear;
            Active = true;
        }

        public void SetFadePattern(FadePattern pattern)
        {
            FadePattern = pattern;
        }

        public void Update()
        {
            if (!Active) return;

            float deltaTime = Raylib.GetFrameTime();
            PreviousPosition = Position;
            Position += Velocity * deltaTime;
            Lifespan -= deltaTime * 60.0f; // Convert to frame-based countdown

            // Calculate alpha based on fade pattern
            float lifePercent = Lifespan / MaxLifespan;
            Alpha = FadePattern switch
            {
                FadePattern.Linear => lifePercent,
                FadePattern.Exponential => lifePercent * lifePercent,
                FadePattern.Pulse => 0.5f + 0.5f * MathF.Sin(lifePercent * MathF.PI * 4),
                _ => lifePercent
            };

            if (Lifespan <= 2.0f) // Set inactive much earlier - aggressive cleanup
            {
                Active = false;
            }
        }

        public void Draw()
        {
            if (!Active || Alpha <= 0) return;

            // Draw trail line with fading alpha
            Color fadeColor = new Color(
                Math.Clamp((int)Color.R, 0, 255),
                Math.Clamp((int)Color.G, 0, 255),
                Math.Clamp((int)Color.B, 0, 255),
                Math.Clamp((int)(Alpha * 255), 0, 255)
            );
            Raylib.DrawLineEx(PreviousPosition, Position, 2.0f, fadeColor);

            // Optional: Add glow effect for bright particles
            if (Alpha > 0.5f)
            {
                Color glowColor = new Color(
                    Math.Clamp((int)Color.R, 0, 255),
                    Math.Clamp((int)Color.G, 0, 255),
                    Math.Clamp((int)Color.B, 0, 255),
                    Math.Clamp((int)(Alpha * 50), 0, 255)
                );
                Raylib.DrawCircleV(Position, 3.0f, glowColor);
            }
        }

        public void Reset()
        {
            Active = false;
            Alpha = 0;
            Lifespan = 0;
        }
    }

    public class DebrisParticle : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }
        public Color Color { get; set; }
        public float Size { get; set; }
        public float Lifespan { get; set; }
        public bool Active { get; set; }

        public void Initialize(Vector2 position, Vector2 velocity, Color color, float size, float lifespan)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Size = size;
            Lifespan = lifespan;
            Rotation = 0;
            RotationSpeed = (float)(Random.Shared.NextDouble() - 0.5) * 5.0f;
            Active = true;
        }

        public void Update()
        {
            if (!Active) return;

            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Rotation += RotationSpeed * deltaTime;
            Velocity *= 0.98f; // Slight drag
            Lifespan -= deltaTime * 60.0f; // Convert to frame-based countdown

            if (Lifespan <= 2.0f) // Set inactive much earlier - aggressive cleanup
            {
                Active = false;
            }
        }

        public void Draw()
        {
            if (!Active) return;

            float alpha = Math.Max(0, Lifespan / 60.0f); // Fade over 1 second
            Color fadeColor = new Color(
                Math.Clamp((int)Color.R, 0, 255),
                Math.Clamp((int)Color.G, 0, 255), 
                Math.Clamp((int)Color.B, 0, 255),
                Math.Clamp((int)(alpha * 255), 0, 255)
            );
            
            // Draw rotating debris fragment
            Vector2[] points = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float angle = Rotation + (i * 2.0f * MathF.PI / 3);
                points[i] = Position + new Vector2(
                    MathF.Cos(angle) * Size,
                    MathF.Sin(angle) * Size
                );
            }

            Raylib.DrawTriangle(points[0], points[1], points[2], fadeColor);
        }

        public void Reset()
        {
            Active = false;
            Lifespan = 0;
        }
    }

    public class AdvancedEngineParticle : IPoolable
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Color Color { get; set; }
        public float Lifespan { get; set; }
        public float MaxLifespan { get; set; }
        public float Size { get; set; }
        public bool Active { get; set; }

        public void Initialize(Vector2 position, Vector2 velocity, Color color, float lifespan, float size = 2.0f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Lifespan = lifespan;
            MaxLifespan = lifespan;
            Size = size;
            Active = true;
        }

        public void Update()
        {
            if (!Active) return;

            float deltaTime = Raylib.GetFrameTime();
            Position += Velocity * deltaTime;
            Lifespan -= deltaTime * 60.0f; // Convert to frame-based countdown

            if (Lifespan <= 2.0f) // Set inactive much earlier - aggressive cleanup
            {
                Active = false;
            }
        }

        public void Draw()
        {
            if (!Active) return;

            float alpha = Lifespan / MaxLifespan;
            float currentSize = Size * alpha;
            Color fadeColor = new Color(
                Math.Clamp((int)Color.R, 0, 255),
                Math.Clamp((int)Color.G, 0, 255),
                Math.Clamp((int)Color.B, 0, 255),
                Math.Clamp((int)(alpha * 255), 0, 255)
            );
            
            Raylib.DrawCircleV(Position, currentSize, fadeColor);
            
            // Add inner bright core
            if (alpha > 0.5f)
            {
                Color coreColor = new Color(255, 255, 255, Math.Clamp((int)(alpha * 128), 0, 255));
                Raylib.DrawCircleV(Position, currentSize * 0.5f, coreColor);
            }
        }

        public void Reset()
        {
            Active = false;
            Lifespan = 0;
        }
    }

    public class AdvancedParticlePool : ParticlePool
    {
        private readonly ObjectPool<TrailParticle> _trailPool;
        private readonly ObjectPool<DebrisParticle> _debrisPool;
        private readonly ObjectPool<AdvancedEngineParticle> _enginePool;
        
        private readonly List<TrailParticle> _activeTrails = new();
        private readonly List<DebrisParticle> _activeDebris = new();
        private readonly List<AdvancedEngineParticle> _activeEngineParticles = new();

        public AdvancedParticlePool(int maxParticles = 1000) : base(maxParticles)
        {
            _trailPool = new ObjectPool<TrailParticle>(maxParticles / 4, () => new TrailParticle());
            _debrisPool = new ObjectPool<DebrisParticle>(maxParticles / 4, () => new DebrisParticle());
            _enginePool = new ObjectPool<AdvancedEngineParticle>(maxParticles / 2, () => new AdvancedEngineParticle());
        }

        public void CreateBulletTrail(Vector2 position, Vector2 velocity, Color color)
        {
            var trail = _trailPool.Rent();
            trail.Initialize(position, velocity * 0.3f, color, 30); // 0.5 second lifespan
            trail.SetFadePattern(FadePattern.Linear);
            _activeTrails.Add(trail);
        }

        public void CreateExplosionBurst(Vector2 center, int count, float force)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(i * 2 * Math.PI / count);
                Vector2 velocity = new Vector2(
                    MathF.Cos(angle) * force,
                    MathF.Sin(angle) * force
                );

                // Create debris particles
                var debris = _debrisPool.Rent();
                debris.Initialize(center, velocity, GetRandomExplosionColor(), 3.0f, 120); // 2 second lifespan
                _activeDebris.Add(debris);

                // Create trail particles for extra visual impact
                var trail = _trailPool.Rent();
                trail.Initialize(center, velocity * 0.5f, GetRandomExplosionColor(), 60);
                trail.SetFadePattern(FadePattern.Exponential);
                _activeTrails.Add(trail);
            }
        }

        public void CreateEngineTrail(Vector2 position, Vector2 velocity, Color baseColor)
        {
            // Create multiple engine particles for richer effect
            for (int i = 0; i < 3; i++)
            {
                Vector2 particlePos = position + new Vector2(
                    (float)(Random.Shared.NextDouble() - 0.5) * 4,
                    (float)(Random.Shared.NextDouble() - 0.5) * 4
                );

                Vector2 particleVel = velocity + new Vector2(
                    (float)(Random.Shared.NextDouble() - 0.5) * 50,
                    (float)(Random.Shared.NextDouble() - 0.5) * 50
                );

                var particle = _enginePool.Rent();
                particle.Initialize(particlePos, particleVel, baseColor, 20, 3.0f);
                _activeEngineParticles.Add(particle);
            }
        }

        public void CreateDebrisField(Vector2 center, AsteroidSize size)
        {
            int debrisCount = size switch
            {
                AsteroidSize.Large => 8,
                AsteroidSize.Medium => 5,
                AsteroidSize.Small => 3,
                _ => 3
            };

            for (int i = 0; i < debrisCount; i++)
            {
                float angle = (float)(Random.Shared.NextDouble() * 2 * Math.PI);
                float speed = (float)(Random.Shared.NextDouble() * 100 + 50);
                Vector2 velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
                
                var debris = _debrisPool.Rent();
                debris.Initialize(center, velocity, Theme.AsteroidColor, 2.0f, 15); // 0.25 second lifespan
                _activeDebris.Add(debris);
            }
        }

        public new void Update()
        {
            base.Update(); // Update base particle system

            // Update trail particles
            for (int i = _activeTrails.Count - 1; i >= 0; i--)
            {
                _activeTrails[i].Update();
                if (!_activeTrails[i].Active)
                {
                    _trailPool.Return(_activeTrails[i]);
                    _activeTrails.RemoveAt(i);
                }
            }

            // Update debris particles
            for (int i = _activeDebris.Count - 1; i >= 0; i--)
            {
                _activeDebris[i].Update();
                if (!_activeDebris[i].Active)
                {
                    _debrisPool.Return(_activeDebris[i]);
                    _activeDebris.RemoveAt(i);
                }
            }

            // Update engine particles
            for (int i = _activeEngineParticles.Count - 1; i >= 0; i--)
            {
                _activeEngineParticles[i].Update();
                if (!_activeEngineParticles[i].Active)
                {
                    _enginePool.Return(_activeEngineParticles[i]);
                    _activeEngineParticles.RemoveAt(i);
                }
            }
        }

        public new void Draw()
        {
            base.Draw(); // Draw base particles

            // Draw trail particles
            foreach (var trail in _activeTrails)
            {
                trail.Draw();
            }

            // Draw debris particles
            foreach (var debris in _activeDebris)
            {
                debris.Draw();
            }

            // Draw engine particles
            foreach (var particle in _activeEngineParticles)
            {
                particle.Draw();
            }
        }

        public new void Clear()
        {
            base.Clear();

            // Return all particles to pools
            foreach (var trail in _activeTrails)
            {
                _trailPool.Return(trail);
            }
            _activeTrails.Clear();

            foreach (var debris in _activeDebris)
            {
                _debrisPool.Return(debris);
            }
            _activeDebris.Clear();

            foreach (var particle in _activeEngineParticles)
            {
                _enginePool.Return(particle);
            }
            _activeEngineParticles.Clear();
        }

        /// <summary>
        /// Nuclear option - immediately clear all advanced particles
        /// </summary>
        public new void ClearAll()
        {
            Clear(); // Call existing clear method
            ErrorManager.LogInfo("Advanced particle pool cleared completely");
        }

        private Color GetRandomExplosionColor()
        {
            Color[] explosionColors = {
                new Color(255, 100, 0, 255),   // Orange
                new Color(255, 200, 0, 255),   // Yellow
                new Color(255, 50, 0, 255),    // Red-orange
                new Color(255, 255, 100, 255), // Light yellow
                new Color(255, 150, 50, 255)   // Orange-yellow
            };

            return explosionColors[Random.Shared.Next(explosionColors.Length)];
        }

        public int GetActiveParticleCount()
        {
            return _activeTrails.Count + _activeDebris.Count + _activeEngineParticles.Count + GetActiveExplosionParticles();
        }

        private int GetActiveExplosionParticles()
        {
            // This would need to be implemented in the base ParticlePool class
            // For now, return 0 as a placeholder
            return 0;
        }
    }
}