using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Centralized rendering coordination manager that handles 2D/3D rendering modes.
    /// Manages renderer lifecycle, performance monitoring, and effects rendering.
    /// </summary>
    public class RenderingManager
    {
        private IRenderer _currentRenderer;
        private readonly GraphicsSettings _graphicsSettings;
        private readonly GraphicsProfiler _profiler;
        private readonly LODManager _lodManager;
        private readonly AdvancedEffectsManager _effectsManager;
        private bool _isInitialized;

        public RenderingManager(GraphicsSettings graphicsSettings, GraphicsProfiler profiler, AdvancedEffectsManager effectsManager)
        {
            _graphicsSettings = graphicsSettings ?? throw new ArgumentNullException(nameof(graphicsSettings));
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
            _effectsManager = effectsManager ?? throw new ArgumentNullException(nameof(effectsManager));
            _lodManager = new LODManager(profiler);
            _isInitialized = false;
        }

        /// <summary>
        /// Initialize the rendering manager and create appropriate renderer
        /// </summary>
        /// <param name="prefer3D">Whether to prefer 3D rendering if available</param>
        /// <returns>True if initialization successful</returns>
        public bool Initialize(bool prefer3D = false)
        {
            try
            {
                _currentRenderer = RendererFactory.CreateRenderer(prefer3D);
                if (_currentRenderer.Initialize())
                {
                    _isInitialized = true;
                    ErrorManager.LogInfo($"RenderingManager initialized with {(_currentRenderer is Renderer3D ? "3D" : "2D")} renderer");
                    return true;
                }
                
                ErrorManager.LogError("Failed to initialize renderer");
                return false;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("RenderingManager initialization failed", ex);
                return false;
            }
        }

        /// <summary>
        /// Begin frame rendering
        /// </summary>
        public void BeginFrame()
        {
            if (!_isInitialized) return;

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            
            _profiler?.BeginFrame();
            _currentRenderer.BeginFrame();
        }

        /// <summary>
        /// End frame rendering and apply screen effects
        /// </summary>
        public void EndFrame()
        {
            if (!_isInitialized) return;

            // End main rendering
            _currentRenderer.EndFrame();

            // Render screen effects (overlays like flash, fade)
            _profiler?.BeginEffectsRender();
            _effectsManager?.RenderScreenEffects();
            _profiler?.EndEffectsRender(_effectsManager?.ActiveEffectCount ?? 0);

            // Draw performance overlay if enabled
            _profiler?.DrawPerformanceOverlay(_graphicsSettings);

            Raylib.EndDrawing();
        }

        /// <summary>
        /// Render a player entity
        /// </summary>
        /// <param name="player">Player entity to render</param>
        public void RenderPlayer(PlayerEntity player)
        {
            if (!_isInitialized || player == null) return;
            player.Render(_currentRenderer);
        }

        /// <summary>
        /// Render an asteroid entity
        /// </summary>
        /// <param name="asteroid">Asteroid entity to render</param>
        public void RenderAsteroid(AsteroidEntity asteroid)
        {
            if (!_isInitialized || asteroid == null) return;
            asteroid.Render(_currentRenderer);
        }

        /// <summary>
        /// Render multiple entities efficiently
        /// </summary>
        /// <param name="entities">Entities to render</param>
        public void RenderEntities<T>(System.Collections.Generic.IEnumerable<T> entities) where T : IGameEntity
        {
            if (!_isInitialized || entities == null) return;

            foreach (var entity in entities)
            {
                if (entity != null && entity.Active)
                {
                    entity.Render(_currentRenderer);
                }
            }
        }

        /// <summary>
        /// Render background grid
        /// </summary>
        /// <param name="showGrid">Whether to show the grid</param>
        public void RenderGrid(bool showGrid)
        {
            if (!_isInitialized || !showGrid) return;
            
            Color gridColor = DynamicTheme.GetGridColor();
            _currentRenderer.RenderGrid(true, gridColor);
        }

        /// <summary>
        /// Update rendering systems (LOD, adaptive graphics)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;
            
            _lodManager?.Update(deltaTime);
            _effectsManager?.Update(deltaTime);
        }

        /// <summary>
        /// Switch between 2D and 3D rendering modes
        /// </summary>
        /// <param name="enable3D">Whether to enable 3D rendering</param>
        /// <returns>True if switch was successful</returns>
        public bool SwitchRenderMode(bool enable3D)
        {
            if (!_isInitialized) return false;

            try
            {
                _currentRenderer?.Cleanup();
                _currentRenderer = RendererFactory.CreateRenderer(enable3D);
                
                if (_currentRenderer.Initialize())
                {
                    ErrorManager.LogInfo($"Switched to {(enable3D ? "3D" : "2D")} rendering mode");
                    return true;
                }
                
                ErrorManager.LogError("Failed to switch rendering mode, falling back to 2D");
                _currentRenderer = RendererFactory.CreateRenderer(false);
                return _currentRenderer.Initialize();
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error switching render mode", ex);
                return false;
            }
        }

        /// <summary>
        /// Get current renderer type
        /// </summary>
        /// <returns>True if 3D renderer is active</returns>
        public bool Is3DActive()
        {
            return _currentRenderer is Renderer3D;
        }

        /// <summary>
        /// Get current rendering statistics
        /// </summary>
        /// <returns>Render statistics</returns>
        public RenderStats GetRenderStats()
        {
            return _currentRenderer?.GetRenderStats() ?? new RenderStats { RenderMode = "None" };
        }

        /// <summary>
        /// Get current renderer instance for entity rendering
        /// </summary>
        /// <returns>Current renderer interface</returns>
        public IRenderer GetCurrentRenderer()
        {
            return _currentRenderer;
        }

        /// <summary>
        /// Get LOD manager for external LOD calculations
        /// </summary>
        /// <returns>LOD manager instance</returns>
        public LODManager GetLODManager()
        {
            return _lodManager;
        }

        /// <summary>
        /// Cleanup rendering resources
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _currentRenderer?.Cleanup();
                // LOD manager will be garbage collected
                _isInitialized = false;
                ErrorManager.LogInfo("RenderingManager cleanup completed");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error during RenderingManager cleanup", ex);
            }
        }
    }
}