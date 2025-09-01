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

        public void RenderPowerUp(Vector2 position, PowerUpType type, float pulseScale, float rotation)
        {
            _stats.TotalItems++;

            if (!IsInViewFrustum(position, GameConstants.POWERUP_RADIUS))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            
            // Get type-specific color and shape
            Color color = GetPowerUpColor(type);
            float baseRadius = GameConstants.POWERUP_RADIUS * pulseScale;
            
            // Draw power-up based on type
            switch (type)
            {
                case PowerUpType.Shield:
                    // Shield: Hexagon with inner glow
                    DrawPowerUpHexagon(position, baseRadius, color, rotation);
                    DrawPowerUpGlow(position, baseRadius * 1.3f, color, 0.3f);
                    break;
                    
                case PowerUpType.RapidFire:
                    // Rapid Fire: Triangle with pulse effect
                    DrawPowerUpTriangle(position, baseRadius, color, rotation);
                    DrawPowerUpGlow(position, baseRadius * 1.2f, color, 0.4f);
                    break;
                    
                case PowerUpType.MultiShot:
                    // Multi Shot: Star shape
                    DrawPowerUpStar(position, baseRadius, color, rotation);
                    DrawPowerUpGlow(position, baseRadius * 1.1f, color, 0.3f);
                    break;
                    
                case PowerUpType.Health:
                    // Health: Cross/Plus shape
                    DrawPowerUpCross(position, baseRadius, color, rotation);
                    DrawPowerUpGlow(position, baseRadius * 1.2f, color, 0.5f);
                    break;
                    
                case PowerUpType.Speed:
                    // Speed: Diamond with motion lines
                    DrawPowerUpDiamond(position, baseRadius, color, rotation);
                    DrawPowerUpGlow(position, baseRadius * 1.1f, color, 0.4f);
                    break;
                    
                default:
                    // Fallback: Circle
                    Raylib.DrawCircle((int)position.X, (int)position.Y, baseRadius, color);
                    Raylib.DrawCircleLines((int)position.X, (int)position.Y, baseRadius, Color.White);
                    break;
            }
        }
        
        public void RenderPowerUp3D(Vector2 position, PowerUpType type, float pulseScale, float rotation)
        {
            // Deprecated method - delegate to new implementation
            RenderPowerUp(position, type, pulseScale, rotation);
        }

        public bool Toggle3DMode()
        {
            // 2D renderer cannot switch to 3D mode
            // This would need to be handled by the renderer factory or game engine
            ErrorManager.LogInfo("2D Renderer cannot toggle to 3D mode - requires renderer switch");
            return false; // Still in 2D mode
        }

        public bool Is3DModeActive 
        { 
            get { return false; } // 2D renderer is never in 3D mode
        }

        public void HandleCameraInput()
        {
            // No-op for 2D renderer as specified in the interface
            // 2D rendering doesn't use camera input
        }

        public CameraState GetCameraState()
        {
            // Return inactive camera state for 2D mode
            return new CameraState
            {
                Position2D = Vector2.Zero,
                Position3D = Vector3.Zero,
                Target3D = Vector3.Zero,
                Up3D = Vector3.UnitY,
                FOV = 0f,
                Is3DMode = false,
                Mode = "2D",
                IsTransitioning = false
            };
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

        /// <summary>
        /// Render an enemy ship with shape based on type
        /// </summary>
        public void RenderEnemy(Vector2 position, float rotation, EnemyType type, Color color, float size, float healthPercentage)
        {
            // Draw enemy ship based on type
            switch (type)
            {
                case EnemyType.Scout:
                    RenderScoutShip(position, rotation, color, size);
                    break;
                case EnemyType.Hunter:
                    RenderHunterShip(position, rotation, color, size);
                    break;
                case EnemyType.Destroyer:
                    RenderDestroyerShip(position, rotation, color, size);
                    break;
                case EnemyType.Interceptor:
                    RenderInterceptorShip(position, rotation, color, size);
                    break;
                default:
                    // Fallback to circle
                    Raylib.DrawCircleV(position, size, color);
                    break;
            }

            // Draw health bar if damaged
            if (healthPercentage < 1.0f)
            {
                DrawEnemyHealthBar(position, size, healthPercentage);
            }
        }

        private void RenderScoutShip(Vector2 position, float rotation, Color color, float size)
        {
            // Scout: Small triangular ship
            Vector2[] points = new Vector2[3];
            float halfSize = size * 0.5f;
            
            // Triangle pointing forward
            points[0] = new Vector2(0, -halfSize);  // Front point
            points[1] = new Vector2(-halfSize * 0.7f, halfSize);   // Back left
            points[2] = new Vector2(halfSize * 0.7f, halfSize);    // Back right

            // Rotate and translate points
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = RotatePoint(points[i], rotation) + position;
            }

            // Draw triangle outline
            Raylib.DrawTriangle(points[0], points[1], points[2], color);
            Raylib.DrawTriangleLines(points[0], points[1], points[2], Color.White);
        }

        private void RenderHunterShip(Vector2 position, float rotation, Color color, float size)
        {
            // Hunter: Arrow-like shape with wings
            Vector2[] hull = new Vector2[5];
            float halfSize = size * 0.5f;
            
            hull[0] = new Vector2(0, -halfSize);           // Front point
            hull[1] = new Vector2(-halfSize * 0.6f, 0);    // Left wing
            hull[2] = new Vector2(-halfSize * 0.3f, halfSize); // Back left
            hull[3] = new Vector2(halfSize * 0.3f, halfSize);  // Back right
            hull[4] = new Vector2(halfSize * 0.6f, 0);     // Right wing

            // Rotate and translate
            for (int i = 0; i < hull.Length; i++)
            {
                hull[i] = RotatePoint(hull[i], rotation) + position;
            }

            // Draw hull
            for (int i = 0; i < hull.Length - 1; i++)
            {
                Raylib.DrawLineV(hull[i], hull[i + 1], color);
            }
            Raylib.DrawLineV(hull[hull.Length - 1], hull[0], color);
        }

        private void RenderDestroyerShip(Vector2 position, float rotation, Color color, float size)
        {
            // Destroyer: Large rectangular ship with details
            float halfSize = size * 0.5f;
            
            // Main body rectangle
            Rectangle body = new Rectangle(position.X - halfSize * 0.8f, position.Y - halfSize, 
                                         halfSize * 1.6f, halfSize * 2f);
            
            // Rotate rectangle (approximate with lines)
            Vector2[] corners = new Vector2[4];
            corners[0] = new Vector2(-halfSize * 0.8f, -halfSize);
            corners[1] = new Vector2(halfSize * 0.8f, -halfSize);
            corners[2] = new Vector2(halfSize * 0.8f, halfSize);
            corners[3] = new Vector2(-halfSize * 0.8f, halfSize);

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = RotatePoint(corners[i], rotation) + position;
            }

            // Draw main body
            for (int i = 0; i < corners.Length; i++)
            {
                Raylib.DrawLineV(corners[i], corners[(i + 1) % corners.Length], color);
            }

            // Add detail lines
            Vector2 centerLine1 = RotatePoint(new Vector2(0, -halfSize * 0.3f), rotation) + position;
            Vector2 centerLine2 = RotatePoint(new Vector2(0, halfSize * 0.3f), rotation) + position;
            Raylib.DrawLineV(centerLine1, centerLine2, color);
        }

        private void RenderInterceptorShip(Vector2 position, float rotation, Color color, float size)
        {
            // Interceptor: Diamond/kite shape for agility
            Vector2[] diamond = new Vector2[4];
            float halfSize = size * 0.5f;
            
            diamond[0] = new Vector2(0, -halfSize);           // Top point
            diamond[1] = new Vector2(halfSize * 0.7f, 0);     // Right point
            diamond[2] = new Vector2(0, halfSize * 0.8f);     // Bottom point
            diamond[3] = new Vector2(-halfSize * 0.7f, 0);    // Left point

            // Rotate and translate
            for (int i = 0; i < diamond.Length; i++)
            {
                diamond[i] = RotatePoint(diamond[i], rotation) + position;
            }

            // Draw diamond
            for (int i = 0; i < diamond.Length; i++)
            {
                Raylib.DrawLineV(diamond[i], diamond[(i + 1) % diamond.Length], color);
            }

            // Add center cross for detail
            Raylib.DrawLineV(diamond[0], diamond[2], color);
            Raylib.DrawLineV(diamond[1], diamond[3], color);
        }

        private Vector2 RotatePoint(Vector2 point, float rotation)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            return new Vector2(
                point.X * cos - point.Y * sin,
                point.X * sin + point.Y * cos
            );
        }

        private void DrawEnemyHealthBar(Vector2 position, float size, float healthPercentage)
        {
            float barWidth = size * 1.2f;
            float barHeight = 4f;
            Vector2 barPos = position + new Vector2(-barWidth * 0.5f, -size - 8f);
            
            // Background bar
            Raylib.DrawRectangleV(barPos, new Vector2(barWidth, barHeight), Color.Red);
            
            // Health bar
            float healthWidth = barWidth * healthPercentage;
            Color healthColor = healthPercentage > 0.6f ? Color.Green : 
                               healthPercentage > 0.3f ? Color.Yellow : Color.Red;
            Raylib.DrawRectangleV(barPos, new Vector2(healthWidth, barHeight), healthColor);
        }
        
        private Color GetPowerUpColor(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.Shield => Color.Blue,
                PowerUpType.RapidFire => Color.Red, 
                PowerUpType.MultiShot => Color.Orange,
                PowerUpType.Health => Color.Green,
                PowerUpType.Speed => Color.Yellow,
                _ => Color.White
            };
        }
        
        private void DrawPowerUpHexagon(Vector2 position, float radius, Color color, float rotation)
        {
            Vector2[] points = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + rotation) * MathF.PI / 180f;
                points[i] = position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }
            
            for (int i = 0; i < 6; i++)
            {
                Raylib.DrawLineV(points[i], points[(i + 1) % 6], color);
            }
        }
        
        private void DrawPowerUpTriangle(Vector2 position, float radius, Color color, float rotation)
        {
            Vector2[] points = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float angle = (i * 120f + rotation) * MathF.PI / 180f;
                points[i] = position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            }
            
            Raylib.DrawTriangle(points[0], points[1], points[2], color);
            Raylib.DrawTriangleLines(points[0], points[1], points[2], Color.White);
        }
        
        private void DrawPowerUpStar(Vector2 position, float radius, Color color, float rotation)
        {
            Vector2[] outerPoints = new Vector2[5];
            Vector2[] innerPoints = new Vector2[5];
            
            for (int i = 0; i < 5; i++)
            {
                float outerAngle = (i * 72f + rotation) * MathF.PI / 180f;
                float innerAngle = ((i * 72f) + 36f + rotation) * MathF.PI / 180f;
                
                outerPoints[i] = position + new Vector2(MathF.Cos(outerAngle), MathF.Sin(outerAngle)) * radius;
                innerPoints[i] = position + new Vector2(MathF.Cos(innerAngle), MathF.Sin(innerAngle)) * radius * 0.5f;
            }
            
            for (int i = 0; i < 5; i++)
            {
                Raylib.DrawLineV(outerPoints[i], innerPoints[i], color);
                Raylib.DrawLineV(innerPoints[i], outerPoints[(i + 1) % 5], color);
            }
        }
        
        private void DrawPowerUpCross(Vector2 position, float radius, Color color, float rotation)
        {
            float thickness = radius * 0.3f;
            
            // Vertical bar
            Raylib.DrawRectangle((int)(position.X - thickness / 2), (int)(position.Y - radius), 
                               (int)thickness, (int)(radius * 2), color);
            
            // Horizontal bar  
            Raylib.DrawRectangle((int)(position.X - radius), (int)(position.Y - thickness / 2),
                               (int)(radius * 2), (int)thickness, color);
        }
        
        private void DrawPowerUpDiamond(Vector2 position, float radius, Color color, float rotation)
        {
            Vector2[] points = new Vector2[4];
            points[0] = position + new Vector2(0, -radius); // Top
            points[1] = position + new Vector2(radius, 0);  // Right
            points[2] = position + new Vector2(0, radius);  // Bottom
            points[3] = position + new Vector2(-radius, 0); // Left
            
            for (int i = 0; i < 4; i++)
            {
                Raylib.DrawLineV(points[i], points[(i + 1) % 4], color);
            }
            
            // Motion lines for speed effect
            for (int i = 0; i < 3; i++)
            {
                float offset = (i + 1) * radius * 0.3f;
                Raylib.DrawLineV(position + new Vector2(-radius - offset, 0), 
                               position + new Vector2(-radius - offset * 0.5f, 0), color);
            }
        }
        
        private void DrawPowerUpGlow(Vector2 position, float radius, Color color, float alpha)
        {
            Color glowColor = new Color(color.R, color.G, color.B, (byte)(alpha * 255));
            Raylib.DrawCircle((int)position.X, (int)position.Y, radius, glowColor);
        }
    }
}