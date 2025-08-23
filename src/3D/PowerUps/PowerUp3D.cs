using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.PowerUps
{
    /// <summary>
    /// 3D power-up object that can be collected by the player
    /// </summary>
    public class PowerUp3D
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public PowerUpType Type { get; private set; }
        public PowerUpConfig Config { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsCollected { get; private set; }
        public float Lifespan { get; private set; }
        public float CollectionRadius { get; private set; }

        private float _creationTime;
        private float _rotationSpeed;
        private float _bobSpeed;
        private float _bobHeight;
        private Vector3 _basePosition;
        private Random _random;
        private float _pulseTime;
        private float _magneticRange;
        private bool _isMagnetic;

        // Visual effects
        private float _glowIntensity;
        private Color _glowColor;
        private float _particleSpawnTimer;
        private float _scale;

        // Collection animation
        private bool _isBeingCollected;
        private float _collectionProgress;
        private Vector3 _targetPosition;

        public PowerUp3D(Vector3 position, PowerUpType type, Vector3? velocity = null)
        {
            Position = position;
            _basePosition = position;
            Type = type;
            Config = PowerUpConfig.CreateDefault(type);
            Velocity = velocity ?? Vector3.Zero;
            IsActive = true;
            IsCollected = false;
            Lifespan = 30f; // Power-ups last 30 seconds before disappearing
            CollectionRadius = 2f;
            
            _creationTime = GetTime();
            _random = new Random();
            _rotationSpeed = (float)(_random.NextDouble() * 2 - 1) * 45f; // -45 to 45 degrees per second
            _bobSpeed = 2f + (float)_random.NextDouble() * 2f; // 2-4 Hz
            _bobHeight = 0.5f + (float)_random.NextDouble() * 0.5f; // 0.5-1.0 units
            _glowIntensity = 1f;
            _glowColor = Config.Color;
            _scale = 1f;
            _magneticRange = 0f;
            _isMagnetic = false;

            // Rare power-ups have special effects
            if (Config.Rarity >= PowerUpRarity.Rare)
            {
                _magneticRange = 5f; // Attracts player from further away
                _isMagnetic = true;
            }

            if (Config.Rarity == PowerUpRarity.Legendary)
            {
                _magneticRange = 8f;
                _bobHeight *= 1.5f;
                _scale = 1.2f;
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            float currentTime = GetTime();
            float age = currentTime - _creationTime;

            // Update lifespan
            Lifespan -= deltaTime;
            if (Lifespan <= 0 && !_isBeingCollected)
            {
                IsActive = false;
                return;
            }

            if (_isBeingCollected)
            {
                UpdateCollectionAnimation(deltaTime);
                return;
            }

            // Update position and movement
            Position += Velocity * deltaTime;
            Velocity *= 0.98f; // Slight damping

            // Bobbing animation
            float bobOffset = MathF.Sin(age * _bobSpeed) * _bobHeight;
            Position = new Vector3(_basePosition.X, _basePosition.Y + bobOffset, _basePosition.Z);
            _basePosition += Velocity * deltaTime;

            // Update visual effects
            UpdateVisualEffects(deltaTime, age);

            // Handle screen wrapping
            HandleScreenWrapping();

            // Pulse effect for rare items
            if (Config.Rarity >= PowerUpRarity.Rare)
            {
                _pulseTime += deltaTime;
                float pulseScale = 1f + MathF.Sin(_pulseTime * 4f) * 0.1f;
                _scale = (Config.Rarity == PowerUpRarity.Legendary ? 1.2f : 1f) * pulseScale;
            }

            // Despawn warning (blink when close to expiring)
            if (Lifespan < 5f)
            {
                _glowIntensity = 0.5f + MathF.Sin(age * 10f) * 0.5f;
            }
        }

        private void UpdateVisualEffects(float deltaTime, float age)
        {
            // Rotation animation
            Vector3 rotation = new Vector3(age * 20f, age * _rotationSpeed, age * 15f);

            // Particle effects for rare items
            _particleSpawnTimer += deltaTime;
            if (Config.Rarity >= PowerUpRarity.Rare && _particleSpawnTimer >= 0.1f)
            {
                // Spawn particles around the power-up
                _particleSpawnTimer = 0f;
            }

            // Glow effect intensity based on rarity
            _glowIntensity = Config.Rarity switch
            {
                PowerUpRarity.Common => 0.8f,
                PowerUpRarity.Uncommon => 1f,
                PowerUpRarity.Rare => 1.2f,
                PowerUpRarity.Epic => 1.5f,
                PowerUpRarity.Legendary => 2f,
                _ => 1f
            };

            // Special color effects for legendary items
            if (Config.Rarity == PowerUpRarity.Legendary)
            {
                float colorShift = MathF.Sin(age * 3f) * 0.5f + 0.5f;
                _glowColor = ColorLerp(Config.Color, Color.White, colorShift * 0.3f);
            }
        }

        private void UpdateCollectionAnimation(float deltaTime)
        {
            _collectionProgress += deltaTime * 8f; // Fast collection animation

            if (_collectionProgress >= 1f)
            {
                IsCollected = true;
                IsActive = false;
                return;
            }

            // Move towards target (player)
            Position = Vector3.Lerp(Position, _targetPosition, _collectionProgress);
            
            // Scale down during collection
            _scale = 1f - _collectionProgress;
            
            // Increase glow during collection
            _glowIntensity = 1f + _collectionProgress * 2f;
        }

        private void HandleScreenWrapping()
        {
            // Simple screen wrapping - should be configured based on game world bounds
            float worldSize = 100f;
            
            if (_basePosition.X < -worldSize) _basePosition.X = worldSize;
            if (_basePosition.X > worldSize) _basePosition.X = -worldSize;
            if (_basePosition.Z < -worldSize) _basePosition.Z = worldSize;
            if (_basePosition.Z > worldSize) _basePosition.Z = -worldSize;
        }

        public bool CheckPlayerProximity(Vector3 playerPosition, out float distance)
        {
            distance = Vector3.Distance(Position, playerPosition);
            return distance <= CollectionRadius;
        }

        public bool CheckMagneticField(Vector3 playerPosition)
        {
            if (!_isMagnetic) return false;
            
            float distance = Vector3.Distance(Position, playerPosition);
            return distance <= _magneticRange;
        }

        public void StartCollection(Vector3 playerPosition)
        {
            if (_isBeingCollected) return;
            
            _isBeingCollected = true;
            _collectionProgress = 0f;
            _targetPosition = playerPosition;
        }

        public void ApplyMagneticForce(Vector3 playerPosition, float magneticStrength = 1f)
        {
            if (!_isMagnetic || _isBeingCollected) return;
            
            Vector3 direction = Vector3.Normalize(playerPosition - Position);
            float distance = Vector3.Distance(Position, playerPosition);
            
            if (distance <= _magneticRange && distance > CollectionRadius)
            {
                float force = magneticStrength * (_magneticRange - distance) / _magneticRange;
                Velocity += direction * force * GetTime();
            }
        }

        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;

            // Draw power-up based on type and rarity
            DrawPowerUpMesh(camera);
            DrawGlowEffect(camera);
            DrawRarityIndicator(camera);

            // Debug: Draw collection radius
            if (false) // Set to true for debugging
            {
                Raylib.DrawSphereWires(Position, CollectionRadius, Color.Green);
                if (_isMagnetic)
                {
                    Raylib.DrawSphereWires(Position, _magneticRange, Color.Blue);
                }
            }
        }

        private void DrawPowerUpMesh(Camera3D camera)
        {
            float currentTime = GetTime() - _creationTime;
            
            // Base shape depends on power-up type
            switch (Type)
            {
                case PowerUpType.WeaponUpgrade:
                case PowerUpType.PlasmaWeapon:
                case PowerUpType.LaserWeapon:
                case PowerUpType.MissileWeapon:
                case PowerUpType.ShotgunWeapon:
                case PowerUpType.BeamWeapon:
                case PowerUpType.LightningWeapon:
                    DrawWeaponPowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.ShieldBoost:
                case PowerUpType.ShieldExtender:
                case PowerUpType.ShieldOvercharge:
                    DrawShieldPowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.SpeedBoost:
                case PowerUpType.ThrustUpgrade:
                case PowerUpType.Agility:
                    DrawMovementPowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.EnergyRecharge:
                case PowerUpType.EnergyEfficiency:
                case PowerUpType.AmmoRefill:
                case PowerUpType.InfiniteAmmo:
                    DrawEnergyPowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.TimeDilation:
                case PowerUpType.TimeFreeze:
                    DrawTimePowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.ExtraLife:
                case PowerUpType.Phoenix:
                    DrawLifePowerUp(camera, currentTime);
                    break;
                    
                case PowerUpType.NuclearDevice:
                case PowerUpType.Devastator:
                    DrawLegendaryPowerUp(camera, currentTime);
                    break;
                    
                default:
                    DrawDefaultPowerUp(camera, currentTime);
                    break;
            }
        }

        private void DrawWeaponPowerUp(Camera3D camera, float time)
        {
            // Draw as rotating cube with weapon-specific details
            Matrix4x4 transform = Matrix4x4.CreateRotationY(time * _rotationSpeed * MathF.PI / 180f) *
                                 Matrix4x4.CreateScale(_scale) *
                                 Matrix4x4.CreateTranslation(Position);
            
            Raylib.DrawCube(Position, _scale, _scale, _scale, Config.Color);
            Raylib.DrawCubeWires(Position, _scale * 1.1f, _scale * 1.1f, _scale * 1.1f, Color.White);
        }

        private void DrawShieldPowerUp(Camera3D camera, float time)
        {
            // Draw as pulsing sphere
            float pulseSize = _scale * (1f + MathF.Sin(time * 4f) * 0.2f);
            Raylib.DrawSphere(Position, pulseSize * 0.8f, Config.Color);
            Raylib.DrawSphereWires(Position, pulseSize, Color.White);
        }

        private void DrawMovementPowerUp(Camera3D camera, float time)
        {
            // Draw as elongated diamond shape
            Vector3 v1 = Position + new Vector3(0, _scale, 0);
            Vector3 v2 = Position + new Vector3(_scale * 0.7f, 0, 0);
            Vector3 v3 = Position + new Vector3(0, -_scale, 0);
            Vector3 v4 = Position + new Vector3(-_scale * 0.7f, 0, 0);
            Vector3 v5 = Position + new Vector3(0, 0, _scale * 0.7f);
            Vector3 v6 = Position + new Vector3(0, 0, -_scale * 0.7f);
            
            // Draw octahedron-like shape
            Raylib.DrawLine3D(v1, v2, Config.Color);
            Raylib.DrawLine3D(v2, v3, Config.Color);
            Raylib.DrawLine3D(v3, v4, Config.Color);
            Raylib.DrawLine3D(v4, v1, Config.Color);
            Raylib.DrawLine3D(v1, v5, Config.Color);
            Raylib.DrawLine3D(v2, v5, Config.Color);
            Raylib.DrawLine3D(v3, v5, Config.Color);
            Raylib.DrawLine3D(v4, v5, Config.Color);
            Raylib.DrawLine3D(v1, v6, Config.Color);
            Raylib.DrawLine3D(v2, v6, Config.Color);
            Raylib.DrawLine3D(v3, v6, Config.Color);
            Raylib.DrawLine3D(v4, v6, Config.Color);
        }

        private void DrawEnergyPowerUp(Camera3D camera, float time)
        {
            // Draw as cylinder with energy effects
            float height = _scale * 1.5f;
            Raylib.DrawCylinder(Position, _scale * 0.5f, _scale * 0.5f, height, 8, Config.Color);
            Raylib.DrawCylinderWires(Position, _scale * 0.6f, _scale * 0.6f, height * 1.1f, 8, Color.White);
        }

        private void DrawTimePowerUp(Camera3D camera, float time)
        {
            // Draw as spinning torus
            Matrix4x4 transform = Matrix4x4.CreateRotationY(time * _rotationSpeed * MathF.PI / 180f) *
                                 Matrix4x4.CreateRotationX(time * 30f * MathF.PI / 180f);
            
            // Approximate torus with circles
            int segments = 12;
            float majorRadius = _scale * 0.8f;
            float minorRadius = _scale * 0.3f;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * MathF.PI * 2f;
                Vector3 center = Position + new Vector3(MathF.Cos(angle) * majorRadius, 0, MathF.Sin(angle) * majorRadius);
                
                // Draw cross-section circles
                for (int j = 0; j < 8; j++)
                {
                    float innerAngle = (float)j / 8 * MathF.PI * 2f;
                    Vector3 point1 = center + new Vector3(0, MathF.Sin(innerAngle) * minorRadius, MathF.Cos(innerAngle) * minorRadius);
                    Vector3 point2 = center + new Vector3(0, MathF.Sin(innerAngle + MathF.PI * 2f / 8) * minorRadius, MathF.Cos(innerAngle + MathF.PI * 2f / 8) * minorRadius);
                    Raylib.DrawLine3D(point1, point2, Config.Color);
                }
            }
        }

        private void DrawLifePowerUp(Camera3D camera, float time)
        {
            // Draw as heart-like shape or cross
            float size = _scale * 0.8f;
            
            // Draw cross shape
            Raylib.DrawLine3D(
                Position + new Vector3(-size, 0, 0),
                Position + new Vector3(size, 0, 0),
                Config.Color
            );
            Raylib.DrawLine3D(
                Position + new Vector3(0, -size, 0),
                Position + new Vector3(0, size, 0),
                Config.Color
            );
            Raylib.DrawLine3D(
                Position + new Vector3(0, 0, -size),
                Position + new Vector3(0, 0, size),
                Config.Color
            );
            
            // Draw spheres at ends
            Raylib.DrawSphere(Position + new Vector3(size, 0, 0), size * 0.2f, Config.Color);
            Raylib.DrawSphere(Position + new Vector3(-size, 0, 0), size * 0.2f, Config.Color);
            Raylib.DrawSphere(Position + new Vector3(0, size, 0), size * 0.2f, Config.Color);
            Raylib.DrawSphere(Position + new Vector3(0, -size, 0), size * 0.2f, Config.Color);
        }

        private void DrawLegendaryPowerUp(Camera3D camera, float time)
        {
            // Draw as complex multi-layered shape with special effects
            float innerScale = _scale * 0.6f;
            float outerScale = _scale * 1.2f;
            
            // Inner rotating cube
            Matrix4x4 innerTransform = Matrix4x4.CreateRotationY(time * _rotationSpeed * MathF.PI / 180f) *
                                     Matrix4x4.CreateRotationX(time * 45f * MathF.PI / 180f);
            Raylib.DrawCube(Position, innerScale, innerScale, innerScale, Config.Color);
            
            // Outer counter-rotating frame
            Matrix4x4 outerTransform = Matrix4x4.CreateRotationY(-time * _rotationSpeed * 0.7f * MathF.PI / 180f) *
                                     Matrix4x4.CreateRotationZ(time * 30f * MathF.PI / 180f);
            Raylib.DrawCubeWires(Position, outerScale, outerScale, outerScale, Color.White);
            
            // Additional wireframe sphere
            Raylib.DrawSphereWires(Position, _scale, _glowColor);
        }

        private void DrawDefaultPowerUp(Camera3D camera, float time)
        {
            // Default octahedron shape
            Raylib.DrawSphere(Position, _scale * 0.7f, Config.Color);
            Raylib.DrawSphereWires(Position, _scale, Color.White);
        }

        private void DrawGlowEffect(Camera3D camera)
        {
            if (_glowIntensity <= 0) return;

            // Draw multiple transparent spheres for glow effect
            Color glowColor = _glowColor;
            
            for (int i = 1; i <= 3; i++)
            {
                float glowRadius = _scale * (1f + i * 0.3f);
                glowColor.A = (byte)(50 * _glowIntensity / i);
                Raylib.DrawSphere(Position, glowRadius, glowColor);
            }
        }

        private void DrawRarityIndicator(Camera3D camera)
        {
            // Draw rarity indicator above the power-up
            float indicatorHeight = _scale * 2f;
            Vector3 indicatorPos = Position + new Vector3(0, indicatorHeight, 0);
            
            Color rarityColor = Config.Rarity switch
            {
                PowerUpRarity.Common => Color.White,
                PowerUpRarity.Uncommon => Color.Green,
                PowerUpRarity.Rare => Color.Blue,
                PowerUpRarity.Epic => Color.Purple,
                PowerUpRarity.Legendary => Color.Gold,
                _ => Color.White
            };

            // Draw stars based on rarity
            int starCount = (int)Config.Rarity + 1;
            for (int i = 0; i < starCount; i++)
            {
                Vector3 starPos = indicatorPos + new Vector3((i - starCount / 2f) * 0.3f, 0, 0);
                Raylib.DrawSphere(starPos, 0.1f, rarityColor);
            }
        }

        private Color ColorLerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t),
                (byte)(a.A + (b.A - a.A) * t)
            );
        }

        private float GetTime() => (float)Raylib.GetTime();
    }
}