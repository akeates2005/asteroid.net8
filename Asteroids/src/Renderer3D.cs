using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// 3D renderer implementation providing enhanced spatial rendering with camera systems.
    /// Integrates with existing Renderer3DIntegration for 3D capabilities and frustum culling.
    /// </summary>
    public class Renderer3D : IRenderer
    {
        private RenderStats _stats;
        private bool _isInitialized;

        public Renderer3D()
        {
            _stats = new RenderStats { RenderMode = "3D" };
            _isInitialized = false;
        }

        public bool Initialize()
        {
            try
            {
                _isInitialized = Renderer3DIntegration.Initialize();
                if (_isInitialized)
                {
                    ErrorManager.LogInfo("3D Renderer initialized successfully");
                }
                else
                {
                    ErrorManager.LogWarning("3D Renderer initialization failed, falling back to 2D");
                }
                return _isInitialized;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize 3D renderer", ex);
                return false;
            }
        }

        public void BeginFrame()
        {
            if (!_isInitialized) return;

            _stats.TotalItems = 0;
            _stats.RenderedItems = 0;
            _stats.CulledItems = 0;
            _stats.FrameTime = Raylib.GetFrameTime();

            // Delegate to existing 3D integration
            // Note: Player position/velocity would need to be passed from game state
            Renderer3DIntegration.BeginFrame(Vector2.Zero, Vector2.Zero, _stats.FrameTime);
        }

        public void EndFrame()
        {
            if (!_isInitialized) return;

            Renderer3DIntegration.EndFrame();
        }

        public void RenderPlayer(Vector2 position, float rotation, Color color, bool isShieldActive, float shieldAlpha = 0.5f)
        {
            if (!_isInitialized) return;

            _stats.TotalItems++;

            if (!IsInViewFrustum(position, GameConstants.PLAYER_SIZE))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            Renderer3DIntegration.RenderPlayer(position, rotation, color, isShieldActive);
        }

        public void RenderAsteroid(Vector2 position, float radius, Color color, int seed, int lodLevel = 0)
        {
            if (!_isInitialized) return;

            _stats.TotalItems++;

            if (!IsInViewFrustum(position, radius))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            Renderer3DIntegration.RenderAsteroid(position, radius, color, seed);
        }

        public void RenderBullet(Vector2 position, Color color)
        {
            if (!_isInitialized) return;

            _stats.TotalItems++;

            if (!IsInViewFrustum(position, GameConstants.BULLET_RADIUS))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            Renderer3DIntegration.RenderBullet(position, color);
        }

        public void RenderExplosion(Vector2 position, float intensity, Color color)
        {
            if (!_isInitialized) return;

            _stats.TotalItems++;

            float explosionRadius = GameConstants.EXPLOSION_MAX_RADIUS * intensity;
            if (!IsInViewFrustum(position, explosionRadius))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            Renderer3DIntegration.RenderExplosion(position, intensity, color);
        }

        public void RenderGrid(bool enabled, Color color)
        {
            if (!_isInitialized || !enabled) return;

            _stats.TotalItems++;
            _stats.RenderedItems++;
            Renderer3DIntegration.RenderGrid(enabled);
        }

        public bool IsInViewFrustum(Vector2 position, float radius)
        {
            if (!_isInitialized) return false;

            // Use existing 3D integration frustum culling
            // For now, implement basic bounds checking
            float margin = radius * 2;
            return position.X >= -margin && position.X <= GameConstants.SCREEN_WIDTH + margin &&
                   position.Y >= -margin && position.Y <= GameConstants.SCREEN_HEIGHT + margin;
        }

        public RenderStats GetRenderStats()
        {
            if (!_isInitialized)
            {
                return new RenderStats { RenderMode = "3D (Disabled)" };
            }

            // Merge with 3D integration stats
            var integrationStats = Renderer3DIntegration.GetRenderStats();
            _stats.TotalItems = integrationStats.TotalItems;
            return _stats;
        }

        public void Cleanup()
        {
            if (_isInitialized)
            {
                Renderer3DIntegration.Cleanup();
                ErrorManager.LogInfo("3D Renderer cleanup completed");
            }
        }
    }
}