using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// 3D renderer implementation providing enhanced spatial rendering with camera systems.
    /// Implements modern 3D capabilities with camera management and frustum culling.
    /// </summary>
    public class Renderer3D : IRenderer
    {
        private RenderStats _stats;
        private bool _isInitialized;
        private Camera3D _camera;
        private ProceduralAsteroidGenerator? _asteroidGenerator;

        public Renderer3D()
        {
            _stats = new RenderStats { RenderMode = "3D" };
            _isInitialized = false;
            _asteroidGenerator = new ProceduralAsteroidGenerator();
            
            // Initialize 3D camera
            _camera = new Camera3D
            {
                Position = new Vector3(0, 20, 20),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                FovY = GameConstants.CAMERA_FOV,
                Projection = CameraProjection.Perspective
            };
        }

        public bool Initialize()
        {
            try
            {
                // Modern 3D renderer initialization - no legacy integration needed
                _isInitialized = true; // 3D renderer is always available in modern implementation
                
                if (_isInitialized)
                {
                    ErrorManager.LogInfo("3D Renderer initialized successfully");
                }
                else
                {
                    ErrorManager.LogWarning("3D Renderer initialization failed");
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

            // Begin 3D mode with camera
            Raylib.BeginMode3D(_camera);
        }

        public void EndFrame()
        {
            if (!_isInitialized) return;

            // End 3D mode
            Raylib.EndMode3D();
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
            
            // Convert 2D position to 3D
            Vector3 position3D = new Vector3(position.X - GameConstants.SCREEN_WIDTH / 2, 1, position.Y - GameConstants.SCREEN_HEIGHT / 2);
            
            // Create rotation matrix from 2D rotation
            Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationY(rotation * MathF.PI / 180.0f);
            Matrix4x4 transform = rotationMatrix * Matrix4x4.CreateTranslation(position3D);
            
            // Draw player as a cone
            Raylib.DrawCube(position3D, GameConstants.PLAYER_3D_SIZE, GameConstants.PLAYER_3D_SIZE * 2, GameConstants.PLAYER_3D_SIZE, color);
            
            // Draw shield if active
            if (isShieldActive)
            {
                Color shieldColor = new Color(0, 255, 255, (int)(shieldAlpha * 255));
                Raylib.DrawSphereWires(position3D, GameConstants.PLAYER_SIZE * GameConstants.SHIELD_RADIUS_MULTIPLIER, 8, 8, shieldColor);
            }
        }

        public void RenderAsteroid(Vector2 position, float radius, Color color, int seed, int lodLevel = 0)
        {
            if (!_isInitialized || _asteroidGenerator == null) return;

            _stats.TotalItems++;

            if (!IsInViewFrustum(position, radius))
            {
                _stats.CulledItems++;
                return;
            }

            _stats.RenderedItems++;
            
            // Determine asteroid size based on radius
            AsteroidSize size = radius switch
            {
                >= GameConstants.LARGE_ASTEROID_RADIUS => AsteroidSize.Large,
                >= GameConstants.MEDIUM_ASTEROID_RADIUS => AsteroidSize.Medium,
                _ => AsteroidSize.Small
            };
            
            // Generate procedural mesh
            Mesh asteroidMesh = _asteroidGenerator.GenerateAsteroidMesh(size, seed, lodLevel);
            
            // Create material
            Material material = Raylib.LoadMaterialDefault();
            
            // Note: Material color setting simplified for compatibility
            // In a full implementation, you would set material properties appropriately
            
            // Calculate 3D position (convert 2D to 3D)
            Vector3 position3D = new Vector3(position.X - GameConstants.SCREEN_WIDTH / 2, 0, position.Y - GameConstants.SCREEN_HEIGHT / 2);
            
            // Draw the procedural asteroid mesh
            Raylib.DrawMesh(asteroidMesh, material, Matrix4x4.CreateTranslation(position3D));
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
            
            // Convert 2D position to 3D
            Vector3 position3D = new Vector3(position.X - GameConstants.SCREEN_WIDTH / 2, 0.5f, position.Y - GameConstants.SCREEN_HEIGHT / 2);
            
            // Draw bullet as a small sphere
            Raylib.DrawSphere(position3D, GameConstants.BULLET_3D_SIZE, color);
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
            
            // Convert 2D position to 3D
            Vector3 position3D = new Vector3(position.X - GameConstants.SCREEN_WIDTH / 2, 2, position.Y - GameConstants.SCREEN_HEIGHT / 2);
            
            // Draw explosion as expanding spheres with varying opacity
            float radius = explosionRadius * intensity;
            Color explosionColor = new Color(color.R, color.G, color.B, (int)(intensity * 255));
            
            // Draw multiple explosion spheres for effect
            Raylib.DrawSphereWires(position3D, radius, 8, 8, explosionColor);
            Raylib.DrawSphereWires(position3D, radius * 0.7f, 6, 6, explosionColor);
            Raylib.DrawSphere(position3D, radius * 0.3f, explosionColor);
        }

        public void RenderGrid(bool enabled, Color color)
        {
            if (!_isInitialized || !enabled) return;

            _stats.TotalItems++;
            _stats.RenderedItems++;
            
            // Draw 3D grid
            int gridSize = GameConstants.GRID_SIZE;
            int halfWidth = GameConstants.SCREEN_WIDTH / 2;
            int halfHeight = GameConstants.SCREEN_HEIGHT / 2;
            
            // Draw grid lines on the XZ plane (ground plane)
            for (int x = -halfWidth; x <= halfWidth; x += gridSize)
            {
                Raylib.DrawLine3D(
                    new Vector3(x, 0, -halfHeight),
                    new Vector3(x, 0, halfHeight),
                    color
                );
            }
            
            for (int z = -halfHeight; z <= halfHeight; z += gridSize)
            {
                Raylib.DrawLine3D(
                    new Vector3(-halfWidth, 0, z),
                    new Vector3(halfWidth, 0, z),
                    color
                );
            }
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

            // Return current stats (no legacy integration needed)
            return _stats;
        }

        public void RenderPowerUp(Vector2 position, PowerUpType type, float pulseScale, float rotation)
        {
            if (!_isInitialized) return;
            
            _stats.TotalItems++;
            
            if (!IsInViewFrustum(position, GameConstants.POWERUP_RADIUS))
            {
                _stats.CulledItems++;
                return;
            }
            
            _stats.RenderedItems++;
            
            // Convert 2D position to 3D
            Vector3 position3D = new Vector3(position.X - GameConstants.SCREEN_WIDTH / 2, 1.5f, position.Y - GameConstants.SCREEN_HEIGHT / 2);
            
            // Define power-up colors and sizes based on type
            Color color = type switch
            {
                PowerUpType.Shield => Color.Blue,
                PowerUpType.RapidFire => Color.Red,
                PowerUpType.MultiShot => Color.Orange,
                PowerUpType.Health => Color.Green,
                PowerUpType.Speed => Color.Yellow,
                _ => Color.White
            };
            
            float radius = GameConstants.POWERUP_RADIUS * pulseScale * 0.1f; // Scale down for 3D
            
            // Render as a spinning 3D cube or sphere
            if (type == PowerUpType.Shield || type == PowerUpType.Health)
            {
                // Render as sphere for defensive power-ups
                Raylib.DrawSphere(position3D, radius * 0.8f, color);
                
                // Add glow effect with wireframe
                Color glowColor = new Color(color.R, color.G, color.B, (byte)100);
                Raylib.DrawSphereWires(position3D, radius * 1.2f, 8, 8, glowColor);
            }
            else
            {
                // Render as rotating cube for offensive power-ups
                Raylib.DrawCube(position3D, radius * 1.2f, radius * 1.2f, radius * 1.2f, color);
                Raylib.DrawCubeWires(position3D, radius * 1.2f, radius * 1.2f, radius * 1.2f, Color.White);
            }
        }

        /// <summary>
        /// Render an enemy ship in 3D
        /// </summary>
        public void RenderEnemy(Vector2 position, float rotation, EnemyType type, Color color, float size, float healthPercentage)
        {
            Vector3 pos3D = new Vector3(position.X, 0, position.Y);
            
            switch (type)
            {
                case EnemyType.Scout:
                    // Scout: Sphere with wireframe
                    Raylib.DrawSphere(pos3D, size * 0.5f, color);
                    Raylib.DrawSphereWires(pos3D, size * 0.6f, 8, 8, Color.White);
                    break;
                    
                case EnemyType.Hunter:
                    // Hunter: Elongated cube
                    Vector3 hSize = new Vector3(size * 0.4f, size * 0.3f, size * 0.8f);
                    Raylib.DrawCube(pos3D, hSize.X, hSize.Y, hSize.Z, color);
                    Raylib.DrawCubeWires(pos3D, hSize.X, hSize.Y, hSize.Z, Color.White);
                    break;
                    
                case EnemyType.Destroyer:
                    // Destroyer: Large cube with detail elements
                    Vector3 dSize = new Vector3(size * 0.8f, size * 0.4f, size * 1.0f);
                    Raylib.DrawCube(pos3D, dSize.X, dSize.Y, dSize.Z, color);
                    Raylib.DrawCubeWires(pos3D, dSize.X, dSize.Y, dSize.Z, Color.White);
                    // Add detail cubes
                    Raylib.DrawCube(pos3D + new Vector3(0, 0.2f, 0.3f), 0.2f, 0.2f, 0.2f, Color.Gray);
                    Raylib.DrawCube(pos3D + new Vector3(0, 0.2f, -0.3f), 0.2f, 0.2f, 0.2f, Color.Gray);
                    break;
                    
                case EnemyType.Interceptor:
                    // Interceptor: Pyramid-like shape with forward point
                    // Draw as a series of triangles to form a pyramid
                    Vector3 front = pos3D + new Vector3(0, 0, -size * 0.7f);
                    Vector3 back1 = pos3D + new Vector3(-size * 0.5f, 0, size * 0.3f);
                    Vector3 back2 = pos3D + new Vector3(size * 0.5f, 0, size * 0.3f);
                    Vector3 top = pos3D + new Vector3(0, size * 0.4f, 0);
                    
                    // Draw pyramid faces
                    Raylib.DrawTriangle3D(front, back1, top, color);
                    Raylib.DrawTriangle3D(front, top, back2, color);
                    Raylib.DrawTriangle3D(back1, back2, top, color);
                    Raylib.DrawTriangle3D(front, back2, back1, color);
                    break;
                    
                default:
                    // Fallback to sphere
                    Raylib.DrawSphere(pos3D, size * 0.5f, color);
                    break;
            }
            
            // Draw health indicator if damaged
            if (healthPercentage < 1.0f)
            {
                Draw3DHealthBar(pos3D, size, healthPercentage);
            }
        }

        private void Draw3DHealthBar(Vector3 position, float size, float healthPercentage)
        {
            // Draw 3D health bar above the enemy
            Vector3 barPos = position + new Vector3(0, size + 0.5f, 0);
            float barWidth = size * 1.2f;
            float barHeight = 0.1f;
            float barDepth = 0.05f;
            
            // Background bar (red)
            Raylib.DrawCube(barPos, barWidth, barHeight, barDepth, Color.Red);
            
            // Health bar
            float healthWidth = barWidth * healthPercentage;
            Color healthColor = healthPercentage > 0.6f ? Color.Green : 
                               healthPercentage > 0.3f ? Color.Yellow : Color.Red;
            Vector3 healthBarPos = barPos + new Vector3(-(barWidth - healthWidth) * 0.5f, 0, 0);
            Raylib.DrawCube(healthBarPos, healthWidth, barHeight, barDepth, healthColor);
        }

        public void RenderPowerUp3D(Vector2 position, PowerUpType type, float pulseScale, float rotation)
        {
            // Deprecated method - delegate to new implementation
            RenderPowerUp(position, type, pulseScale, rotation);
        }

        public bool Toggle3DMode()
        {
            // 3D renderer cannot switch to 2D mode
            // This would need to be handled by the renderer factory or game engine
            ErrorManager.LogInfo("3D Renderer cannot toggle to 2D mode - requires renderer switch");
            return true; // Still in 3D mode
        }

        public bool Is3DModeActive 
        { 
            get { return _isInitialized; } // 3D renderer is active when initialized
        }

        public void HandleCameraInput()
        {
            if (!_isInitialized) return;

            // Modern 3D camera input handling
            // Camera movement is handled internally by the 3D renderer
            // Additional camera controls can be implemented here as needed
        }

        public CameraState GetCameraState()
        {
            if (!_isInitialized)
            {
                return new CameraState
                {
                    Position = Vector3.Zero,
                    Target = Vector3.Zero,
                    Up = Vector3.UnitY,
                    Fovy = 0f,
                    Projection = CameraProjection.Perspective,
                    IsActive = false
                };
            }

            return new CameraState
            {
                Position = _camera.Position,
                Target = _camera.Target,
                Up = _camera.Up,
                Fovy = _camera.FovY,
                Projection = _camera.Projection,
                IsActive = true
            };
        }
        
        public void Cleanup()
        {
            if (_isInitialized)
            {
                _asteroidGenerator?.ClearCache();
                ErrorManager.LogInfo("3D Renderer cleanup completed");
            }
        }
    }
}