using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Special ability types that can be activated
    /// </summary>
    public enum SpecialAbilityType
    {
        TimeDilation,       // Slows down time
        ShieldOvercharge,   // Temporary invincibility
        Cloaking,          // Invisibility with reduced collision
        MagneticField,     // Attracts items and deflects projectiles
        EnergyOverload,    // Massive energy boost
        TeleportStrike,    // Teleport and damage enemies
        GravityWell,       // Attracts and damages enemies
        PhaseShift,        // Pass through objects temporarily
        Berserker,         // Increased damage and speed
        TimeFreeze         // Completely stops time
    }

    /// <summary>
    /// Manages special abilities that can be activated by the player
    /// </summary>
    public class SpecialAbilitiesSystem
    {
        private Dictionary<SpecialAbilityType, SpecialAbility> _abilities;
        private Dictionary<SpecialAbilityType, bool> _unlockedAbilities;
        private SpecialAbilityType? _activeAbility;
        private float _globalCooldownTimer;
        private const float GlobalCooldownDuration = 1f; // 1 second global cooldown

        // Events
        public event Action<SpecialAbilityType> OnAbilityActivated;
        public event Action<SpecialAbilityType> OnAbilityDeactivated;
        public event Action<SpecialAbilityType> OnAbilityUnlocked;
        public event Action<SpecialAbilityType, float> OnAbilityCooldownUpdate;

        public SpecialAbilitiesSystem()
        {
            _abilities = new Dictionary<SpecialAbilityType, SpecialAbility>();
            _unlockedAbilities = new Dictionary<SpecialAbilityType, bool>();
            
            InitializeAbilities();
        }

        private void InitializeAbilities()
        {
            foreach (SpecialAbilityType type in Enum.GetValues<SpecialAbilityType>())
            {
                _abilities[type] = CreateAbility(type);
                _unlockedAbilities[type] = type == SpecialAbilityType.MagneticField; // Start with one ability unlocked
            }
        }

        private SpecialAbility CreateAbility(SpecialAbilityType type)
        {
            return type switch
            {
                SpecialAbilityType.TimeDilation => new SpecialAbility
                {
                    Type = type,
                    Name = "Time Dilation",
                    Description = "Slows down time around you",
                    Duration = 8f,
                    Cooldown = 25f,
                    EnergyCost = 150f,
                    EffectStrength = 0.3f, // 30% time speed
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.ShieldOvercharge => new SpecialAbility
                {
                    Type = type,
                    Name = "Shield Overcharge",
                    Description = "Temporary invincibility",
                    Duration = 5f,
                    Cooldown = 35f,
                    EnergyCost = 200f,
                    EffectStrength = 1f,
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.Cloaking => new SpecialAbility
                {
                    Type = type,
                    Name = "Cloaking",
                    Description = "Become nearly invisible",
                    Duration = 10f,
                    Cooldown = 20f,
                    EnergyCost = 100f,
                    EffectStrength = 0.15f, // 15% visibility
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.MagneticField => new SpecialAbility
                {
                    Type = type,
                    Name = "Magnetic Field",
                    Description = "Attracts items and deflects projectiles",
                    Duration = 12f,
                    Cooldown = 18f,
                    EnergyCost = 80f,
                    EffectStrength = 20f, // Field radius
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.EnergyOverload => new SpecialAbility
                {
                    Type = type,
                    Name = "Energy Overload",
                    Description = "Massive energy boost and enhanced weapons",
                    Duration = 6f,
                    Cooldown = 30f,
                    EnergyCost = 50f, // Low cost because it gives energy
                    EffectStrength = 3f, // 3x energy regeneration
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.TeleportStrike => new SpecialAbility
                {
                    Type = type,
                    Name = "Teleport Strike",
                    Description = "Teleport to target location and damage nearby enemies",
                    Duration = 0f, // Instant
                    Cooldown = 15f,
                    EnergyCost = 120f,
                    EffectStrength = 100f, // Damage value
                    RequiresTargeting = true,
                    CanMoveWhileActive = false,
                    CanFireWhileActive = false
                },

                SpecialAbilityType.GravityWell => new SpecialAbility
                {
                    Type = type,
                    Name = "Gravity Well",
                    Description = "Creates a gravity well that attracts and damages enemies",
                    Duration = 8f,
                    Cooldown = 25f,
                    EnergyCost = 150f,
                    EffectStrength = 15f, // Pull strength
                    RequiresTargeting = true,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.PhaseShift => new SpecialAbility
                {
                    Type = type,
                    Name = "Phase Shift",
                    Description = "Pass through objects and projectiles",
                    Duration = 4f,
                    Cooldown = 20f,
                    EnergyCost = 100f,
                    EffectStrength = 1f,
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = false
                },

                SpecialAbilityType.Berserker => new SpecialAbility
                {
                    Type = type,
                    Name = "Berserker Mode",
                    Description = "Increased damage and speed, reduced defense",
                    Duration = 10f,
                    Cooldown = 30f,
                    EnergyCost = 120f,
                    EffectStrength = 2f, // 2x damage and speed
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                SpecialAbilityType.TimeFreeze => new SpecialAbility
                {
                    Type = type,
                    Name = "Time Freeze",
                    Description = "Completely stops time for all enemies",
                    Duration = 3f,
                    Cooldown = 60f,
                    EnergyCost = 300f,
                    EffectStrength = 0f, // 0% enemy speed
                    RequiresTargeting = false,
                    CanMoveWhileActive = true,
                    CanFireWhileActive = true
                },

                _ => throw new ArgumentException($"Unknown special ability type: {type}")
            };
        }

        public void Update(float deltaTime)
        {
            // Update global cooldown
            if (_globalCooldownTimer > 0)
            {
                _globalCooldownTimer -= deltaTime;
            }

            // Update all ability cooldowns
            foreach (var ability in _abilities.Values)
            {
                if (ability.IsOnCooldown)
                {
                    ability.UpdateCooldown(deltaTime);
                    OnAbilityCooldownUpdate?.Invoke(ability.Type, ability.GetCooldownProgress());
                }

                if (ability.IsActive)
                {
                    ability.UpdateDuration(deltaTime);
                    
                    if (!ability.IsActive) // Just ended
                    {
                        OnAbilityDeactivated?.Invoke(ability.Type);
                        if (_activeAbility == ability.Type)
                        {
                            _activeAbility = null;
                        }
                    }
                }
            }
        }

        public bool TryActivateAbility(SpecialAbilityType type, Vector3 playerPosition, Vector3? targetPosition = null, float currentEnergy = 1000f)
        {
            if (!CanActivateAbility(type, currentEnergy))
                return false;

            var ability = _abilities[type];

            // Handle targeting requirement
            if (ability.RequiresTargeting && !targetPosition.HasValue)
                return false;

            // Activate the ability
            ability.Activate(targetPosition);
            _activeAbility = type;
            _globalCooldownTimer = GlobalCooldownDuration;

            OnAbilityActivated?.Invoke(type);

            // Apply immediate effects
            ApplyAbilityEffect(type, playerPosition, targetPosition, ability.EffectStrength);

            return true;
        }

        private bool CanActivateAbility(SpecialAbilityType type, float currentEnergy)
        {
            if (!_unlockedAbilities[type]) return false;
            if (_globalCooldownTimer > 0) return false;
            
            var ability = _abilities[type];
            if (ability.IsOnCooldown) return false;
            if (ability.IsActive && ability.Duration > 0) return false; // Can't reactivate duration abilities
            if (currentEnergy < ability.EnergyCost) return false;

            return true;
        }

        private void ApplyAbilityEffect(SpecialAbilityType type, Vector3 playerPosition, Vector3? targetPosition, float effectStrength)
        {
            switch (type)
            {
                case SpecialAbilityType.TeleportStrike:
                    if (targetPosition.HasValue)
                    {
                        ApplyTeleportStrike(playerPosition, targetPosition.Value, effectStrength);
                    }
                    break;

                case SpecialAbilityType.GravityWell:
                    if (targetPosition.HasValue)
                    {
                        CreateGravityWell(targetPosition.Value, effectStrength);
                    }
                    break;

                // Other abilities are handled by the systems that subscribe to the events
            }
        }

        private void ApplyTeleportStrike(Vector3 fromPosition, Vector3 toPosition, float damage)
        {
            // Create teleport effect
            CreateTeleportEffect(fromPosition, toPosition);
            
            // Damage nearby enemies (this would be handled by the game's damage system)
            // For now, we'll just create a damage effect
            CreateDamageField(toPosition, 5f, damage);
        }

        private void CreateGravityWell(Vector3 position, float strength)
        {
            // Create a gravity well effect that lasts for the ability duration
            // This would be handled by a separate gravity well object
            var gravityWell = new GravityWell(position, strength, _abilities[SpecialAbilityType.GravityWell].Duration);
            // Add to game world...
        }

        private void CreateTeleportEffect(Vector3 from, Vector3 to)
        {
            // Visual effect for teleportation
            // This would create particle effects at both locations
        }

        private void CreateDamageField(Vector3 position, float radius, float damage)
        {
            // Create a damage field that affects all enemies in radius
            // This would interact with the collision system
        }

        public void UnlockAbility(SpecialAbilityType type)
        {
            if (!_unlockedAbilities[type])
            {
                _unlockedAbilities[type] = true;
                OnAbilityUnlocked?.Invoke(type);
            }
        }

        public void Draw(Camera3D camera, Vector3 playerPosition)
        {
            // Draw visual effects for active abilities
            foreach (var ability in _abilities.Values)
            {
                if (ability.IsActive)
                {
                    DrawAbilityEffect(camera, playerPosition, ability);
                }
            }
        }

        private void DrawAbilityEffect(Camera3D camera, Vector3 playerPosition, SpecialAbility ability)
        {
            switch (ability.Type)
            {
                case SpecialAbilityType.MagneticField:
                    DrawMagneticField(camera, playerPosition, ability.EffectStrength);
                    break;

                case SpecialAbilityType.ShieldOvercharge:
                    DrawShieldOvercharge(camera, playerPosition);
                    break;

                case SpecialAbilityType.Cloaking:
                    DrawCloakingEffect(camera, playerPosition, ability.EffectStrength);
                    break;

                case SpecialAbilityType.EnergyOverload:
                    DrawEnergyOverload(camera, playerPosition);
                    break;

                case SpecialAbilityType.PhaseShift:
                    DrawPhaseShift(camera, playerPosition);
                    break;

                case SpecialAbilityType.Berserker:
                    DrawBerserkerMode(camera, playerPosition);
                    break;

                case SpecialAbilityType.TimeDilation:
                case SpecialAbilityType.TimeFreeze:
                    DrawTimeEffect(camera, playerPosition, ability.Type);
                    break;
            }
        }

        private void DrawMagneticField(Camera3D camera, Vector3 playerPosition, float radius)
        {
            // Draw pulsing magnetic field effect
            float pulseIntensity = MathF.Sin(GetTime() * 6f) * 0.3f + 0.7f;
            Color fieldColor = new Color(100, 150, 255, (byte)(100 * pulseIntensity));
            
            Raylib.DrawSphereWires(playerPosition, radius, 16, 16, fieldColor);
            
            // Draw inner energy rings
            for (int i = 1; i <= 3; i++)
            {
                float ringRadius = radius * (i / 4f);
                Color ringColor = fieldColor;
                ringColor.A = (byte)(ringColor.A / i);
                Raylib.DrawSphereWires(playerPosition, ringRadius, 8, 8, ringColor);
            }
        }

        private void DrawShieldOvercharge(Camera3D camera, Vector3 playerPosition)
        {
            // Draw intense shield effect
            float intensity = MathF.Sin(GetTime() * 8f) * 0.4f + 0.6f;
            Color shieldColor = new Color(255, 255, 255, (byte)(200 * intensity));
            
            Raylib.DrawSphere(playerPosition, 3f, shieldColor);
            Raylib.DrawSphereWires(playerPosition, 3.5f, 12, 12, Color.White);
        }

        private void DrawCloakingEffect(Camera3D camera, Vector3 playerPosition, float visibility)
        {
            // Draw semi-transparent distortion effect
            Color cloakColor = new Color(150, 150, 255, (byte)(255 * visibility));
            Raylib.DrawSphereWires(playerPosition, 2f, 8, 8, cloakColor);
        }

        private void DrawEnergyOverload(Camera3D camera, Vector3 playerPosition)
        {
            // Draw crackling energy effect
            float time = GetTime();
            Color energyColor = new Color(255, 255, 0, 200);
            
            // Draw energy arcs
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * MathF.PI * 2f + time;
                Vector3 arcEnd = playerPosition + new Vector3(
                    MathF.Cos(angle) * 4f,
                    MathF.Sin(time * 3f + i) * 2f,
                    MathF.Sin(angle) * 4f
                );
                
                Raylib.DrawLine3D(playerPosition, arcEnd, energyColor);
            }
        }

        private void DrawPhaseShift(Camera3D camera, Vector3 playerPosition)
        {
            // Draw phase distortion effect
            Color phaseColor = new Color(255, 0, 255, 150);
            float distortion = MathF.Sin(GetTime() * 10f) * 0.5f + 1f;
            
            Raylib.DrawSphereWires(playerPosition, 2.5f * distortion, 6, 6, phaseColor);
        }

        private void DrawBerserkerMode(Camera3D camera, Vector3 playerPosition)
        {
            // Draw red rage aura
            Color rageColor = new Color(255, 0, 0, 100);
            float pulse = MathF.Sin(GetTime() * 15f) * 0.3f + 0.7f;
            
            Raylib.DrawSphere(playerPosition, 2f * pulse, rageColor);
        }

        private void DrawTimeEffect(Camera3D camera, Vector3 playerPosition, SpecialAbilityType type)
        {
            // Draw time distortion effect
            Color timeColor = type == SpecialAbilityType.TimeFreeze ? Color.LightBlue : Color.Purple;
            timeColor.A = 80;
            
            float rippleRadius = (GetTime() % 2f) * 10f;
            Raylib.DrawSphereWires(playerPosition, rippleRadius, 12, 12, timeColor);
        }

        // Getters for UI and game systems
        public bool IsAbilityUnlocked(SpecialAbilityType type) => _unlockedAbilities[type];
        public bool IsAbilityActive(SpecialAbilityType type) => _abilities[type].IsActive;
        public bool IsAbilityOnCooldown(SpecialAbilityType type) => _abilities[type].IsOnCooldown;
        public float GetAbilityCooldownProgress(SpecialAbilityType type) => _abilities[type].GetCooldownProgress();
        public float GetAbilityDurationProgress(SpecialAbilityType type) => _abilities[type].GetDurationProgress();
        public SpecialAbility GetAbility(SpecialAbilityType type) => _abilities[type];
        public SpecialAbilityType? GetActiveAbility() => _activeAbility;
        public bool IsOnGlobalCooldown() => _globalCooldownTimer > 0;
        public float GetGlobalCooldownProgress() => 1f - (_globalCooldownTimer / GlobalCooldownDuration);

        private float GetTime() => (float)Raylib.GetTime();
    }

    /// <summary>
    /// Represents a special ability with all its properties and state
    /// </summary>
    public class SpecialAbility
    {
        public SpecialAbilityType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Duration { get; set; }
        public float Cooldown { get; set; }
        public float EnergyCost { get; set; }
        public float EffectStrength { get; set; }
        public bool RequiresTargeting { get; set; }
        public bool CanMoveWhileActive { get; set; }
        public bool CanFireWhileActive { get; set; }

        public bool IsActive { get; private set; }
        public bool IsOnCooldown { get; private set; }
        public float RemainingDuration { get; private set; }
        public float RemainingCooldown { get; private set; }
        public Vector3? TargetPosition { get; private set; }

        public void Activate(Vector3? targetPosition = null)
        {
            IsActive = true;
            RemainingDuration = Duration;
            TargetPosition = targetPosition;
        }

        public void UpdateDuration(float deltaTime)
        {
            if (!IsActive) return;

            if (Duration > 0) // Duration-based abilities
            {
                RemainingDuration -= deltaTime;
                if (RemainingDuration <= 0)
                {
                    Deactivate();
                }
            }
            else // Instant abilities
            {
                Deactivate();
            }
        }

        private void Deactivate()
        {
            IsActive = false;
            IsOnCooldown = true;
            RemainingCooldown = Cooldown;
            TargetPosition = null;
        }

        public void UpdateCooldown(float deltaTime)
        {
            if (!IsOnCooldown) return;

            RemainingCooldown -= deltaTime;
            if (RemainingCooldown <= 0)
            {
                IsOnCooldown = false;
                RemainingCooldown = 0;
            }
        }

        public float GetDurationProgress()
        {
            if (!IsActive || Duration <= 0) return 0f;
            return RemainingDuration / Duration;
        }

        public float GetCooldownProgress()
        {
            if (!IsOnCooldown) return 1f;
            return 1f - (RemainingCooldown / Cooldown);
        }
    }

    /// <summary>
    /// Gravity well effect for the gravity well special ability
    /// </summary>
    public class GravityWell
    {
        public Vector3 Position { get; private set; }
        public float Strength { get; private set; }
        public float Radius { get; private set; }
        public bool IsActive { get; private set; }
        private float _duration;
        private float _remainingDuration;

        public GravityWell(Vector3 position, float strength, float duration, float radius = 15f)
        {
            Position = position;
            Strength = strength;
            Radius = radius;
            _duration = duration;
            _remainingDuration = duration;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            _remainingDuration -= deltaTime;
            if (_remainingDuration <= 0)
            {
                IsActive = false;
            }
        }

        public Vector3 CalculateForce(Vector3 objectPosition, float objectMass = 1f)
        {
            if (!IsActive) return Vector3.Zero;

            Vector3 direction = Position - objectPosition;
            float distance = direction.Length();

            if (distance > Radius || distance < 0.1f)
                return Vector3.Zero;

            direction = Vector3.Normalize(direction);
            float force = Strength * objectMass / (distance * distance);
            force *= (Radius - distance) / Radius; // Falloff at edge

            return direction * force;
        }

        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;

            float intensity = _remainingDuration / _duration;
            Color wellColor = new Color(150, 0, 255, (byte)(100 * intensity));

            // Draw gravity well effect
            Raylib.DrawSphereWires(Position, Radius, 16, 16, wellColor);
            
            // Draw energy swirls
            float time = (float)Raylib.GetTime();
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * MathF.PI * 2f + time * 2f;
                float spiralRadius = Radius * 0.8f;
                Vector3 spiralPos = Position + new Vector3(
                    MathF.Cos(angle) * spiralRadius,
                    MathF.Sin(time * 3f + i) * 2f,
                    MathF.Sin(angle) * spiralRadius
                );
                
                Raylib.DrawSphere(spiralPos, 0.5f, wellColor);
            }
        }
    }
}