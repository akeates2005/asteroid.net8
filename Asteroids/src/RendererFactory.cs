using System;

namespace Asteroids
{
    /// <summary>
    /// Enhanced renderer factory with auto-detection and seamless 2D/3D switching.
    /// Handles fallback scenarios and renderer selection logic based on system capabilities.
    /// </summary>
    public static class RendererFactory
    {
        /// <summary>
        /// Supported render modes
        /// </summary>
        public enum RenderMode
        {
            Auto,       // Automatically detect best mode
            Force2D,    // Force 2D rendering
            Force3D,    // Force 3D rendering (fallback to 2D if unavailable)
            Prefer3D    // Prefer 3D but allow 2D fallback
        }

        /// <summary>
        /// Create a renderer instance based on preferred mode
        /// </summary>
        /// <param name="prefer3D">Whether to prefer 3D rendering if available</param>
        /// <returns>Initialized renderer instance</returns>
        public static IRenderer CreateRenderer(bool prefer3D = false)
        {
            RenderMode mode = prefer3D ? RenderMode.Prefer3D : RenderMode.Auto;
            return CreateRenderer(mode);
        }

        /// <summary>
        /// Create a renderer instance based on specified render mode
        /// </summary>
        /// <param name="mode">Desired render mode</param>
        /// <returns>Initialized renderer instance</returns>
        public static IRenderer CreateRenderer(RenderMode mode)
        {
            try
            {
                switch (mode)
                {
                    case RenderMode.Force2D:
                        return CreateRenderer2D();

                    case RenderMode.Force3D:
                        return CreateRenderer3D() ?? CreateRenderer2D();

                    case RenderMode.Prefer3D:
                        return CreateRenderer3D() ?? CreateRenderer2D();

                    case RenderMode.Auto:
                    default:
                        return AutoDetectRenderer();
                }
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to create renderer", ex);
                
                // Last resort fallback to 2D
                try
                {
                    return CreateRenderer2D();
                }
                catch
                {
                    throw new InvalidOperationException("Failed to initialize any renderer", ex);
                }
            }
        }

        /// <summary>
        /// Create a renderer based on string configuration
        /// </summary>
        /// <param name="renderModeString">Render mode as string ("2D", "3D", "auto")</param>
        /// <returns>Initialized renderer instance</returns>
        public static IRenderer CreateRenderer(string renderModeString)
        {
            if (string.IsNullOrEmpty(renderModeString))
            {
                return CreateRenderer(RenderMode.Auto);
            }

            RenderMode mode = ParseRenderModeString(renderModeString);
            return CreateRenderer(mode);
        }

        /// <summary>
        /// Create a renderer based on graphics settings
        /// </summary>
        /// <param name="graphicsSettings">Current graphics configuration</param>
        /// <returns>Initialized renderer instance</returns>
        public static IRenderer CreateRenderer(GraphicsSettings graphicsSettings)
        {
            if (graphicsSettings == null)
            {
                return CreateRenderer(RenderMode.Auto);
            }

            // Determine preferred mode based on settings
            RenderMode mode = RenderMode.Auto;
            
            // Default to 3D mode with 2D fallback for production
            return CreateRenderer(RenderMode.Prefer3D);
        }

        /// <summary>
        /// Parse render mode string to enum
        /// </summary>
        /// <param name="renderModeString">String representation of render mode</param>
        /// <returns>Corresponding RenderMode enum</returns>
        private static RenderMode ParseRenderModeString(string renderModeString)
        {
            return renderModeString.ToLowerInvariant() switch
            {
                "2d" => RenderMode.Force2D,
                "3d" => RenderMode.Prefer3D,
                "auto" => RenderMode.Auto,
                "force2d" => RenderMode.Force2D,
                "force3d" => RenderMode.Force3D,
                "prefer3d" => RenderMode.Prefer3D,
                _ => RenderMode.Auto
            };
        }

        /// <summary>
        /// Auto-detect the best renderer based on system capabilities
        /// </summary>
        /// <returns>Best available renderer</returns>
        private static IRenderer AutoDetectRenderer()
        {
            // Check if 3D rendering is supported
            if (Is3DRenderingSupported())
            {
                var renderer3D = CreateRenderer3D();
                if (renderer3D != null)
                {
                    ErrorManager.LogInfo("Auto-detected 3D renderer as optimal");
                    return renderer3D;
                }
            }

            // Fallback to 2D
            ErrorManager.LogInfo("Auto-detected 2D renderer as optimal");
            return CreateRenderer2D();
        }

        /// <summary>
        /// Create a 2D renderer instance
        /// </summary>
        /// <returns>2D renderer or null if creation failed</returns>
        private static IRenderer CreateRenderer2D()
        {
            try
            {
                var renderer2D = new Renderer2D();
                if (renderer2D.Initialize())
                {
                    ErrorManager.LogInfo("Created 2D renderer successfully");
                    return renderer2D;
                }
                else
                {
                    ErrorManager.LogError("2D renderer initialization failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to create 2D renderer", ex);
                return null;
            }
        }

        /// <summary>
        /// Create a 3D renderer instance
        /// </summary>
        /// <returns>3D renderer or null if creation failed</returns>
        private static IRenderer CreateRenderer3D()
        {
            try
            {
                var renderer3D = new Renderer3D();
                if (renderer3D.Initialize())
                {
                    ErrorManager.LogInfo("Created 3D renderer successfully");
                    return renderer3D;
                }
                else
                {
                    ErrorManager.LogInfo("3D renderer initialization failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to create 3D renderer", ex);
                return null;
            }
        }

        /// <summary>
        /// Check if 3D rendering is supported on the current system
        /// </summary>
        /// <returns>True if 3D rendering is supported</returns>
        private static bool Is3DRenderingSupported()
        {
            try
            {
                // Test 3D support by attempting to create and initialize a 3D renderer
                var testRenderer = new Renderer3D();
                bool isSupported = testRenderer.Initialize();
                
                // Clean up the test renderer
                if (isSupported)
                {
                    testRenderer.Cleanup();
                }
                
                return isSupported;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error checking 3D rendering support", ex);
                return false;
            }
        }

        /// <summary>
        /// Get renderer capability information
        /// </summary>
        /// <returns>Capability information</returns>
        public static RendererCapabilities GetCapabilities()
        {
            return new RendererCapabilities
            {
                Supports2D = true, // Always supported
                Supports3D = Is3DRenderingSupported(),
                SupportsFrustumCulling = true,
                SupportsLOD = true,
                DefaultMode = Is3DRenderingSupported() ? RenderMode.Prefer3D : RenderMode.Force2D
            };
        }
    }

    /// <summary>
    /// Renderer capability information
    /// </summary>
    public struct RendererCapabilities
    {
        public bool Supports2D { get; set; }
        public bool Supports3D { get; set; }
        public bool SupportsFrustumCulling { get; set; }
        public bool SupportsLOD { get; set; }
        public RendererFactory.RenderMode DefaultMode { get; set; }
    }
}