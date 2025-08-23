using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.PowerUps
{
    /// <summary>
    /// Extended power-up types for advanced gameplay mechanics
    /// </summary>
    public enum AdvancedPowerUpType
    {
        // Advanced Weapon Modifications
        WeaponEvolution,        // Allows weapons to evolve during combat
        ElementalInfusion,      // Adds elemental damage to weapons
        ProjectileMultiplier,   // Increases projectile count dramatically
        WeaponSynergy,          // Combines multiple weapon types
        AdaptiveAiming,         // Auto-targeting system
        
        // Exotic Shield Systems
        QuantumShield,          // Exists in multiple dimensions
        AbsorbingShield,        // Converts damage to energy
        ReflectiveShield,       // Bounces projectiles back
        HardeningShield,        // Gets stronger when hit
        PhasedShield,           // Alternates between states
        
        // Advanced Mobility
        TeleportDash,           // Short-range teleportation
        GravityManipulation,    // Control local gravity
        TimeStep,               // Brief time acceleration
        DimensionalSlip,        // Phase through objects
        MomentumAmplifier,      // Builds speed over time
        
        // Tactical Systems
        HolographicDecoys,      // Creates false copies
        TacticalScanner,        // Reveals enemy weaknesses
        BattlefieldControl,     // Environmental manipulation
        ResourceConverter,      // Changes ammo types on demand
        SystemOverride,         // Temporarily bypass limitations
        
        // Symbiotic Enhancements
        NaniteIntegration,      // Permanent nanite symbiosis
        BioAdaptation,          // Evolves based on damage taken
        EnergySymbiont,         // Living energy creature companion
        CombatProtocol,         // AI combat assistance
        QuantumEntanglement,    // Links with another entity
        
        // Environmental Powers
        AsteroidCommand,        // Control over asteroids
        SpaceDistortion,        // Warp space around player
        GravityWell,            // Creates attraction fields
        EnergyStorm,            // Area energy discharge
        VoidManipulation,       // Control over empty space
        
        // Legendary Artifacts
        CosmicResonance,        // Harmonizes with universe
        DimensionalAnchor,      // Stabilizes reality
        TemporalCore,           // Controls time flow
        Voidheart,              // Channel void energy
        StarForge,              // Creates matter from energy
        
        // Meta Powers
        PowerAmplifier,         // Enhances other power-ups
        EffectExtender,         // Increases power-up duration
        StackEnhancer,          // Allows more stacking
        CooldownReducer,        // Reduces all cooldowns
        LuckManipulator         // Affects random events
    }
    
    /// <summary>
    /// Advanced power-up configuration with complex mechanics
    /// </summary>
    [Serializable]
    public struct AdvancedPowerUpConfig
    {
        public AdvancedPowerUpType Type;
        public string Name;
        public string Description;
        public string FlavorText;
        
        // Basic Properties
        public PowerUpRarity Rarity;
        public float Duration;
        public float EffectStrength;
        public float CooldownTime;
        public bool IsPermanent;
        public bool IsStackable;
        public int MaxStacks;
        public float SpawnWeight;
        
        // Visual Properties
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color ParticleColor;
        public PowerUpShape Shape;
        public float VisualScale;
        public ParticleEffectType ParticleEffect;
        
        // Advanced Mechanics
        public ActivationMethod ActivationMethod;
        public TargetingType TargetingType;
        public bool RequiresEnergy;
        public float EnergyConsumption;
        public bool HasCharges;
        public int MaxCharges;
        public float RechargeRate;
        
        // Synergy System
        public AdvancedPowerUpType[] SynergyTypes;
        public float SynergyMultiplier;
        public bool CreatesNewEffects;
        public AdvancedPowerUpType[] ConflictTypes;
        
        // Evolution Properties
        public bool CanEvolve;
        public EvolutionTrigger EvolutionTrigger;
        public AdvancedPowerUpType[] EvolutionChain;
        public int RequiredEvolutionPoints;
        
        // Conditional Effects
        public ActivationCondition[] ActivationConditions;
        public EffectModifier[] EffectModifiers;
        public StatusEffectType[] StatusEffectsGranted;
        public StatusEffectType[] StatusEffectsImmunity;
        
        public static AdvancedPowerUpConfig CreateDefault(AdvancedPowerUpType type)
        {
            return type switch
            {
                AdvancedPowerUpType.WeaponEvolution => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Weapon Evolution",
                    Description = "Allows weapons to evolve and adapt during combat",
                    FlavorText = "\"Evolution is the universe's way of teaching us to become more than we are.\"",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 0f,
                    EffectStrength = 1f,
                    IsPermanent = true,
                    IsStackable = false,
                    MaxStacks = 1,
                    SpawnWeight = 3f,
                    PrimaryColor = Color.Gold,
                    SecondaryColor = Color.Orange,
                    ParticleColor = Color.Yellow,
                    Shape = PowerUpShape.DoubleHelix,
                    VisualScale = 1.2f,
                    ParticleEffect = ParticleEffectType.EnergyRipples,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Self,
                    RequiresEnergy = false,
                    CanEvolve = false,
                    ActivationConditions = new[] { ActivationCondition.InCombat },
                    StatusEffectsGranted = new[] { StatusEffectType.DamageBoost }
                },
                
                AdvancedPowerUpType.QuantumShield => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Quantum Shield",
                    Description = "A shield that exists in multiple dimensions simultaneously",
                    FlavorText = "\"Reality is merely a suggestion when you control quantum states.\"",
                    Rarity = PowerUpRarity.Legendary,
                    Duration = 20f,
                    EffectStrength = 2f,
                    CooldownTime = 45f,
                    IsPermanent = false,
                    IsStackable = false,
                    MaxStacks = 1,
                    SpawnWeight = 1f,
                    PrimaryColor = Color.Purple,
                    SecondaryColor = Color.Magenta,
                    ParticleColor = Color.Violet,
                    Shape = PowerUpShape.Sphere,
                    VisualScale = 1.5f,
                    ParticleEffect = ParticleEffectType.QuantumDistortion,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Self,
                    RequiresEnergy = true,
                    EnergyConsumption = 100f,
                    StatusEffectsGranted = new[] { StatusEffectType.Quantum, StatusEffectType.Resistance }
                },
                
                AdvancedPowerUpType.TeleportDash => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Teleport Dash",
                    Description = "Instantly teleport short distances in any direction",
                    FlavorText = "\"Space is just another obstacle to overcome.\"",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 0f,
                    EffectStrength = 15f, // Teleport distance
                    IsPermanent = false,
                    IsStackable = true,
                    MaxStacks = 3,
                    HasCharges = true,
                    MaxCharges = 5,
                    RechargeRate = 0.2f, // 1 charge every 5 seconds
                    SpawnWeight = 8f,
                    PrimaryColor = Color.Cyan,
                    SecondaryColor = Color.Blue,
                    ParticleColor = Color.LightBlue,
                    Shape = PowerUpShape.Arrow,
                    VisualScale = 1f,
                    ParticleEffect = ParticleEffectType.DimensionalRift,
                    ActivationMethod = ActivationMethod.Manual,
                    TargetingType = TargetingType.Directional,
                    RequiresEnergy = true,
                    EnergyConsumption = 25f,
                    StatusEffectsGranted = new[] { StatusEffectType.SpeedBoost }
                },
                
                AdvancedPowerUpType.NaniteIntegration => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Nanite Integration",
                    Description = "Permanent nanite symbiosis enhances all capabilities",
                    FlavorText = "\"Become one with the swarm, and the swarm becomes part of you.\"",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 0f,
                    EffectStrength = 1.5f,
                    IsPermanent = true,
                    IsStackable = true,
                    MaxStacks = 3,
                    SpawnWeight = 4f,
                    PrimaryColor = Color.Green,
                    SecondaryColor = Color.DarkGreen,
                    ParticleColor = Color.Gray,
                    Shape = PowerUpShape.Swarm,
                    VisualScale = 1.1f,
                    ParticleEffect = ParticleEffectType.NaniteCloud,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Self,
                    RequiresEnergy = false,
                    CanEvolve = true,
                    EvolutionTrigger = EvolutionTrigger.KillCount,
                    RequiredEvolutionPoints = 50,
                    StatusEffectsGranted = new[] { StatusEffectType.Regeneration, StatusEffectType.DamageBoost }
                },
                
                AdvancedPowerUpType.CosmicResonance => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Cosmic Resonance",
                    Description = "Harmonize with the universe itself, gaining cosmic awareness",
                    FlavorText = "\"In perfect harmony with the cosmic symphony, all secrets are revealed.\"",
                    Rarity = PowerUpRarity.Legendary,
                    Duration = 30f,
                    EffectStrength = 3f,
                    CooldownTime = 120f,
                    IsPermanent = false,
                    IsStackable = false,
                    MaxStacks = 1,
                    SpawnWeight = 0.5f,
                    PrimaryColor = Color.White,
                    SecondaryColor = Color.Gold,
                    ParticleColor = Color.Yellow,
                    Shape = PowerUpShape.Star,
                    VisualScale = 2f,
                    ParticleEffect = ParticleEffectType.PhotonBeam,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Area,
                    RequiresEnergy = true,
                    EnergyConsumption = 200f,
                    StatusEffectsGranted = new[] { 
                        StatusEffectType.AccuracyBoost, 
                        StatusEffectType.DamageBoost,
                        StatusEffectType.EnergyRegen,
                        StatusEffectType.Resistance
                    }
                },
                
                AdvancedPowerUpType.HolographicDecoys => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Holographic Decoys",
                    Description = "Creates realistic holographic copies to confuse enemies",
                    FlavorText = "\"Reality is perception, and perception can be... influenced.\"",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 15f,
                    EffectStrength = 3f, // Number of decoys
                    CooldownTime = 20f,
                    IsPermanent = false,
                    IsStackable = true,
                    MaxStacks = 2,
                    SpawnWeight = 12f,
                    PrimaryColor = Color.LightBlue,
                    SecondaryColor = Color.White,
                    ParticleColor = Color.Cyan,
                    Shape = PowerUpShape.Prism,
                    VisualScale = 1f,
                    ParticleEffect = ParticleEffectType.EnergyRipples,
                    ActivationMethod = ActivationMethod.Manual,
                    TargetingType = TargetingType.Self,
                    RequiresEnergy = true,
                    EnergyConsumption = 50f,
                    StatusEffectsGranted = new[] { StatusEffectType.Cloaked }
                },
                
                AdvancedPowerUpType.PowerAmplifier => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Power Amplifier",
                    Description = "Enhances the effectiveness of all other active power-ups",
                    FlavorText = "\"True power lies not in strength alone, but in the wisdom to enhance what you already possess.\"",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 25f,
                    EffectStrength = 1.5f, // 50% amplification
                    CooldownTime = 60f,
                    IsPermanent = false,
                    IsStackable = true,
                    MaxStacks = 2,
                    SpawnWeight = 5f,
                    PrimaryColor = Color.Gold,
                    SecondaryColor = Color.White,
                    ParticleColor = Color.Yellow,
                    Shape = PowerUpShape.Crystal,
                    VisualScale = 1.3f,
                    ParticleEffect = ParticleEffectType.EnergyRipples,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Self,
                    RequiresEnergy = true,
                    EnergyConsumption = 75f
                },
                
                _ => new AdvancedPowerUpConfig
                {
                    Type = type,
                    Name = "Unknown Power",
                    Description = "A mysterious power with unknown effects",
                    FlavorText = "\"Some secrets are meant to be discovered, not explained.\"",
                    Rarity = PowerUpRarity.Common,
                    Duration = 10f,
                    EffectStrength = 1f,
                    IsStackable = false,
                    MaxStacks = 1,
                    SpawnWeight = 1f,
                    PrimaryColor = Color.Gray,
                    SecondaryColor = Color.White,
                    ParticleColor = Color.Gray,
                    Shape = PowerUpShape.Sphere,
                    VisualScale = 1f,
                    ParticleEffect = ParticleEffectType.Default,
                    ActivationMethod = ActivationMethod.Automatic,
                    TargetingType = TargetingType.Self
                }
            };
        }
    }
    
    /// <summary>
    /// Visual shapes for power-ups
    /// </summary>
    public enum PowerUpShape
    {
        Sphere,
        Cube,
        Crystal,
        Star,
        Ring,
        Prism,
        DoubleHelix,
        Arrow,
        Cross,
        Diamond,
        Swarm,
        Spiral,
        Torus,
        Octahedron
    }
    
    /// <summary>
    /// How power-ups are activated
    /// </summary>
    public enum ActivationMethod
    {
        Automatic,      // Activates immediately on pickup
        Manual,         // Requires player input to activate
        Conditional,    // Activates when conditions are met
        Triggered,      // Activates in response to events
        Charged         // Must be charged before activation
    }
    
    /// <summary>
    /// Targeting system for power-ups
    /// </summary>
    public enum TargetingType
    {
        Self,           // Affects only the player
        Target,         // Requires a target selection
        Area,           // Affects an area around player
        Directional,    // Affects in a direction from player
        Global,         // Affects entire game world
        Nearest,        // Automatically targets nearest entity
        All             // Affects all valid entities
    }
    
    /// <summary>
    /// Conditions that must be met for activation
    /// </summary>
    public enum ActivationCondition
    {
        None,
        LowHealth,      // Health below threshold
        LowEnergy,      // Energy below threshold
        InCombat,       // Currently in combat
        EnemiesNearby,  // Enemies within range
        MovingFast,     // Above speed threshold
        NotMoving,      // Below speed threshold
        WeaponFiring,   // While firing weapons
        ShieldActive,   // While shields are up
        UsingAbility    // While using special ability
    }
    
    /// <summary>
    /// How power-ups modify their effects
    /// </summary>
    public enum EffectModifier
    {
        None,
        ScaleWithHealth,    // Effect scales with current health
        ScaleWithEnergy,    // Effect scales with current energy
        ScaleWithSpeed,     // Effect scales with movement speed
        ScaleWithTime,      // Effect changes over time
        ScaleWithEnemies,   // Effect scales with nearby enemies
        InverseHealth,      // Stronger when health is lower
        RandomVariance,     // Random variation in effect
        Progressive,        // Gets stronger with continued use
        Diminishing        // Gets weaker with continued use
    }
    
    /// <summary>
    /// Triggers for power-up evolution
    /// </summary>
    public enum EvolutionTrigger
    {
        KillCount,          // Kill a certain number of enemies
        TimeAlive,          // Survive for a duration
        DamageTaken,        // Take a certain amount of damage
        DamageDealt,        // Deal a certain amount of damage
        EnergyConsumed,     // Consume a certain amount of energy
        DistanceTraveled,   // Travel a certain distance
        UsageCount,         // Use ability a certain number of times
        ComboAchieved,      // Achieve specific combo requirements
        SynergyActivated,   // Activate with synergistic power-ups
        BossKilled          // Kill a boss enemy
    }
    
    /// <summary>
    /// Status effect types that can be applied by advanced power-ups
    /// </summary>
    public enum StatusEffectType
    {
        // Offensive Buffs
        DamageBoost,
        CriticalBoost,
        PenetrationBoost,
        AccuracyBoost,
        FireRateBoost,
        
        // Defensive Buffs
        Resistance,
        Regeneration,
        ShieldBoost,
        EnergyRegen,
        Immunity,
        
        // Mobility Buffs
        SpeedBoost,
        AgilityBoost,
        TeleportReady,
        PhaseShift,
        GravityControl,
        
        // Tactical Buffs
        Cloaked,
        Scanner,
        Magnetic,
        TimeDialated,
        QuantumLinked,
        
        // Special States
        Evolving,
        Symbiotic,
        Overcharged,
        Resonating,
        Transcendent,
        
        // Debuff Immunities
        BurnImmune,
        FreezeImmune,
        StunImmune,
        SlowImmune,
        WeaknessImmune,
        
        // Unique States
        Quantum,
        Dimensional,
        Cosmic,
        Void,
        Temporal
    }
}