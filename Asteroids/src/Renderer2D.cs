using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// 2D renderer implementation with optimized drawing and frustum culling support.
    /// Provides efficient 2D rendering with LOD system and performance optimizations.
    /// </summary>
    public class Renderer2D : IRenderer
    {
        private RenderStats _stats;
        private readonly Dictionary<int, AsteroidShape> _asteroidShapeCache;
        private static readonly Vector2 ScreenBounds = new Vector2(GameConstants.SCREEN_WIDTH, GameConstants.SCREEN_HEIGHT);

        public Renderer2D()
        {
            _asteroidShapeCache = new Dictionary<int, AsteroidShape>();
            _stats = new RenderStats { RenderMode = "2D" };
        }

        public bool Initialize()
        {
            try
            {
                ErrorManager.LogInfo("2D Renderer initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize 2D renderer", ex);
                return false;
            }
        }

        public void BeginFrame()
        {
            _stats.TotalItems = 0;
            _stats.RenderedItems = 0;
            _stats.CulledItems = 0;
            _stats.FrameTime = Raylib.GetFrameTime();
        }

        public void EndFrame()
        {
            // Frame statistics are complete
        }

        public void RenderPlayer(Vector2 position, float rotation, Color color, bool isShieldActive, float shieldAlpha = 0.5f)
        {
            _stats.TotalItems++;

            if (!IsInViewFrustum(position, GameConstants.PLAYER_SIZE))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;

            // Calculate triangle vertices
            float size = GameConstants.PLAYER_SIZE;
            Vector2 v1 = position + Vector2.Transform(new Vector2(0, -size), Matrix3x2.CreateRotation(MathF.PI / 180 * rotation));
            Vector2 v2 = position + Vector2.Transform(new Vector2(-size / 2, size / 2), Matrix3x2.CreateRotation(MathF.PI / 180 * rotation));
            Vector2 v3 = position + Vector2.Transform(new Vector2(size / 2, size / 2), Matrix3x2.CreateRotation(MathF.PI / 180 * rotation));
            
            Raylib.DrawTriangleLines(v1, v2, v3, color);

            // Render shield if active
            if (isShieldActive)
            {
                float shieldRadius = size * 1.5f;
                float pulseFactor = 0.3f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 8);
                Color shieldColor = new Color(color.R, color.G, color.B, (byte)(255 * shieldAlpha * pulseFactor));
                
                Raylib.DrawCircleLines((int)position.X, (int)position.Y, shieldRadius, shieldColor);
                
                // Inner glow
                Color innerGlow = new Color(shieldColor.R, shieldColor.G, shieldColor.B, (byte)30);
                Raylib.DrawCircle((int)position.X, (int)position.Y, shieldRadius * 0.8f, innerGlow);
            }
        }

        public void RenderAsteroid(Vector2 position, float radius, Color color, int seed, int lodLevel = 0)
        {
            _stats.TotalItems++;

            if (!IsInViewFrustum(position, radius))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;

            // Get or create cached shape
            if (!_asteroidShapeCache.TryGetValue(seed, out AsteroidShape shape))
            {
                var random = new Random(seed);
                int vertexCount = GetLODVertexCount(lodLevel);
                shape = new AsteroidShape(vertexCount, radius, random);
                _asteroidShapeCache[seed] = shape;
            }

            // Draw asteroid polygon
            for (int i = 0; i < shape.Points.Length; i++)
            {
                Vector2 p1 = shape.Points[i] + position;
                Vector2 p2 = shape.Points[(i + 1) % shape.Points.Length] + position;
                Raylib.DrawLineV(p1, p2, color);
            }
        }

        public void RenderBullet(Vector2 position, Color color)
        {
            _stats.TotalItems++;

            if (!IsInViewFrustum(position, GameConstants.BULLET_RADIUS))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            Raylib.DrawCircle((int)position.X, (int)position.Y, GameConstants.BULLET_RADIUS, color);
        }

        public void RenderExplosion(Vector2 position, float intensity, Color color)
        {
            _stats.TotalItems++;

            float explosionRadius = GameConstants.EXPLOSION_MAX_RADIUS * intensity;
            if (!IsInViewFrustum(position, explosionRadius))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            
            // Render explosion as expanding circle with fading alpha
            byte alpha = (byte)(255 * intensity);
            Color explosionColor = new Color(color.R, color.G, color.B, alpha);
            Raylib.DrawCircleLines((int)position.X, (int)position.Y, explosionRadius, explosionColor);
        }

        public void RenderGrid(bool enabled, Color color)
        {
            if (!enabled) return;

            _stats.TotalItems++;
            _stats.RenderedItems++;

            // Vertical lines
            for (int i = 0; i < GameConstants.SCREEN_WIDTH; i += GameConstants.GRID_SIZE)
            {
                Raylib.DrawLine(i, 0, i, GameConstants.SCREEN_HEIGHT, color);
            }

            // Horizontal lines
            for (int i = 0; i < GameConstants.SCREEN_HEIGHT; i += GameConstants.GRID_SIZE)
            {
                Raylib.DrawLine(0, i, GameConstants.SCREEN_WIDTH, i, color);
            }
        }

        public bool IsInViewFrustum(Vector2 position, float radius)
        {
            // Simple 2D bounds check with screen wrapping consideration
            float margin = radius * 2; // Account for screen wrapping
            
            return position.X >= -margin && position.X <= ScreenBounds.X + margin &&
                   position.Y >= -margin && position.Y <= ScreenBounds.Y + margin;
        }

        public RenderStats GetRenderStats()
        {
            return _stats;
        }

        public void Cleanup()
        {
            _asteroidShapeCache.Clear();
            ErrorManager.LogInfo("2D Renderer cleanup completed");
        }

        private int GetLODVertexCount(int lodLevel)
        {
            return lodLevel switch
            {
                0 => 12,  // High detail
                1 => 8,   // Medium detail
                2 => 6,   // Low detail
                _ => 8    // Default
            };
        }
    }
}