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
        /// Render a power-up with type-specific visuals and animation
        /// </summary>
        /// <param name="position">Power-up position</param>
        /// <param name="type">Power-up type</param>
        /// <param name="pulseScale">Pulse scaling factor</param>
        /// <param name="rotation">Power-up rotation</param>
        void RenderPowerUp(Vector2 position, PowerUpType type, float pulseScale, float rotation);

        /// <summary>
        /// Render a power-up in 3D mode (DEPRECATED - Use RenderPowerUp instead)
        /// </summary>
        /// <param name="position">Power-up position</param>
        /// <param name="type">Power-up type</param>
        /// <param name="pulseScale">Pulse scaling factor</param>
        /// <param name="rotation">Power-up rotation</param>
        [Obsolete("Use RenderPowerUp instead")]
        void RenderPowerUp3D(Vector2 position, PowerUpType type, float pulseScale, float rotation);

        /// <summary>
        /// Render an enemy ship
        /// </summary>
        /// <param name="position">Enemy position</param>
        /// <param name="rotation">Enemy rotation</param>
        /// <param name="type">Enemy type</param>
        /// <param name="color">Enemy color</param>
        /// <param name="size">Enemy size</param>
        /// <param name="healthPercentage">Health percentage (0-1)</param>
        void RenderEnemy(Vector2 position, float rotation, EnemyType type, Color color, float size, float healthPercentage);

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
        /// Toggle between 2D and 3D rendering modes
        /// </summary>
        /// <returns>True if 3D mode is now active, false if 2D mode</returns>
        bool Toggle3DMode();
        
        /// <summary>
        /// Check if 3D rendering mode is currently active
        /// </summary>
        bool Is3DModeActive { get; }
        
        /// <summary>
        /// Handle camera input for the current rendering mode
        /// Only applicable in 3D mode - no-op in 2D mode
        /// </summary>
        void HandleCameraInput();
        
        /// <summary>
        /// Get current camera state information
        /// </summary>
        CameraState GetCameraState();

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

    /// <summary>
    /// Camera state information for both 2D and 3D modes
    /// </summary>
    public struct CameraState
    {
        public Vector2 Position2D { get; set; }
        public Vector3 Position3D { get; set; }
        public Vector3 Target3D { get; set; }
        public Vector3 Up3D { get; set; }
        public float FOV { get; set; }
        public bool Is3DMode { get; set; }
        public string Mode { get; set; }
        public bool IsTransitioning { get; set; }
    }
}