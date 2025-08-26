using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Core rendering interface providing abstraction over 2D and 3D rendering systems.
    /// Enables seamless switching between rendering modes and supports LOD optimization.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Initialize the rendering system
        /// </summary>
        /// <returns>True if initialization successful</returns>
        bool Initialize();

        /// <summary>
        /// Begin frame rendering
        /// </summary>
        void BeginFrame();

        /// <summary>
        /// End frame rendering
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Render a player entity with position, rotation, color, and shield state
        /// </summary>
        /// <param name="position">Player position</param>
        /// <param name="rotation">Player rotation in degrees</param>
        /// <param name="color">Player color</param>
        /// <param name="isShieldActive">Whether shield is active</param>
        /// <param name="shieldAlpha">Shield transparency (0-1)</param>
        void RenderPlayer(Vector2 position, float rotation, Color color, bool isShieldActive, float shieldAlpha = 0.5f);

        /// <summary>
        /// Render an asteroid with position, radius, and color
        /// </summary>
        /// <param name="position">Asteroid position</param>
        /// <param name="radius">Asteroid radius</param>
        /// <param name="color">Asteroid color</param>
        /// <param name="seed">Seed for consistent shape generation</param>
        /// <param name="lodLevel">Level of detail (0=highest, 2=lowest)</param>
        void RenderAsteroid(Vector2 position, float radius, Color color, int seed, int lodLevel = 0);

        /// <summary>
        /// Render a bullet with position and color
        /// </summary>
        /// <param name="position">Bullet position</param>
        /// <param name="color">Bullet color</param>
        void RenderBullet(Vector2 position, Color color);

        /// <summary>
        /// Render an explosion effect
        /// </summary>
        /// <param name="position">Explosion position</param>
        /// <param name="intensity">Explosion intensity (0-1)</param>
        /// <param name="color">Explosion color</param>
        void RenderExplosion(Vector2 position, float intensity, Color color);

        /// <summary>
        /// Render background grid
        /// </summary>
        /// <param name="enabled">Whether grid is enabled</param>
        /// <param name="color">Grid color</param>
        void RenderGrid(bool enabled, Color color);

        /// <summary>
        /// Check if a position is within the view frustum (for culling)
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="radius">Object radius</param>
        /// <returns>True if object should be rendered</returns>
        bool IsInViewFrustum(Vector2 position, float radius);

        /// <summary>
        /// Get current rendering statistics
        /// </summary>
        /// <returns>Render stats structure</returns>
        RenderStats GetRenderStats();

        /// <summary>
        /// Cleanup rendering resources
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// Rendering statistics for performance monitoring
    /// </summary>
    public struct RenderStats
    {
        public int TotalItems { get; set; }
        public int RenderedItems { get; set; }
        public int CulledItems { get; set; }
        public float FrameTime { get; set; }
        public string RenderMode { get; set; }
    }
}