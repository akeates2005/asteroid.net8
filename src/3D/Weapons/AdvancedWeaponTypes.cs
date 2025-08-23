using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Extended weapon types for advanced gameplay
    /// </summary>
    public enum AdvancedWeaponType
    {        
        // Exotic Weapons
        GaussRifle,         // Electromagnetic projectile acceleration
        QuantumCannon,      // Reality-bending projectiles
        NaniteSwarm,        // Self-replicating projectiles
        GravityGun,         // Manipulates gravity fields
        EnergyVortex,       // Creates energy whirlpools
        PhotonTorpedo,      // Light-speed projectiles
        DarkMatterLance,    // Pierces through dimensions
        
        // Elemental Weapons
        CryoLaser,          // Freezing beam weapon
        PlasmaThrower,      // Advanced flamethrower with plasma
        IonStorm,           // Area-effect ion damage
        NeutronBlaster,     // Dense neutron projectiles
        AntiMatter,         // Matter-antimatter annihilation
        
        // Tactical Weapons
        ClusterBomb,        // Explosive with multiple warheads
        EMPDevice,          // Electromagnetic pulse
        HologramDecoy,      // Creates false targets
        TeleportMine,       // Instant-travel explosives
        ShieldBreaker,      // Designed to penetrate defenses
        
        // Support Weapons
        RepairBeam,         // Heals friendly units
        BuffProjectile,     // Enhances ally capabilities
        DebuffField,        // Weakens enemies in area
        ScannerPulse,       // Reveals hidden enemies
        
        // Transformation Weapons
        Morphic,            // Changes properties mid-flight
        Adaptive,           // Automatically adjusts to targets
        Evolutionary,       // Gets stronger with each kill
        Symbiotic          // Bonds with player for enhanced abilities
    }

    /// <summary>
    /// Enhanced weapon mechanics and properties
    /// </summary>
    public struct AdvancedWeaponStats
    {
        public AdvancedWeaponType Type;
        public string Name;
        public string Description;
        
        // Core Stats
        public float BaseDamage;
        public float FireRate;
        public float ProjectileSpeed;
        public float Range;
        public float EnergyConsumption;
        public int AmmoCapacity;
        public float ReloadTime;
        
        // Advanced Properties
        public ElementalType ElementType;
        public float ElementalDamage;
        public float ElementalDuration;
        public bool HasHomingCapability;
        public float HomingStrength;
        public bool CanPierceShields;
        public float ShieldPenetration;
        public bool HasAreaEffect;
        public float AreaRadius;
        public float AreaDamage;
        
        // Tactical Properties
        public bool CanRicochet;
        public int MaxRicochets;
        public float RicochetDamageRetention;
        public bool HasDelayedDetonation;
        public float DetonationDelay;
        public bool CreatesStatusEffects;
        public StatusEffectType[] StatusEffects;
        
        // Evolution Properties
        public bool CanEvolve;
        public float EvolutionRate;
        public int MaxEvolutionLevel;
        public bool HasSymbioticBonding;
        public float BondingStrength;
        
        // Visual Properties
        public Color ProjectileColor;
        public Color SecondaryColor;
        public ParticleEffectType ParticleEffect;
        public SoundEffectType SoundEffect;
        public float VisualScale;
        public bool HasTrail;
        public TrailType TrailEffect;
        
        public static AdvancedWeaponStats CreateDefault(AdvancedWeaponType type)
        {
            return type switch
            {
                AdvancedWeaponType.GaussRifle => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Gauss Rifle",
                    Description = "Electromagnetic projectile accelerator with extreme velocity",
                    BaseDamage = 80f,
                    FireRate = 2f,
                    ProjectileSpeed = 100f,
                    Range = 300f,
                    EnergyConsumption = 30f,
                    AmmoCapacity = 20,
                    ReloadTime = 2.5f,
                    ElementType = ElementalType.Electromagnetic,
                    ElementalDamage = 20f,
                    CanPierceShields = true,
                    ShieldPenetration = 0.6f,
                    ProjectileColor = Color.Cyan,
                    SecondaryColor = Color.Blue,
                    ParticleEffect = ParticleEffectType.ElectricSparks,
                    SoundEffect = SoundEffectType.ElectroMagnetic,
                    HasTrail = true,
                    TrailEffect = TrailType.Electric
                },
                
                AdvancedWeaponType.QuantumCannon => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Quantum Cannon",
                    Description = "Probability-based projectiles that exist in multiple dimensions",
                    BaseDamage = 120f,
                    FireRate = 1f,
                    ProjectileSpeed = 25f,
                    Range = 200f,
                    EnergyConsumption = 80f,
                    AmmoCapacity = 8,
                    ReloadTime = 4f,
                    ElementType = ElementalType.Quantum,
                    ElementalDamage = 40f,
                    HasAreaEffect = true,
                    AreaRadius = 8f,
                    AreaDamage = 60f,
                    CanEvolve = true,
                    EvolutionRate = 0.1f,
                    MaxEvolutionLevel = 3,
                    ProjectileColor = Color.Purple,
                    SecondaryColor = Color.Magenta,
                    ParticleEffect = ParticleEffectType.QuantumDistortion,
                    SoundEffect = SoundEffectType.Quantum,
                    HasTrail = true,
                    TrailEffect = TrailType.Quantum
                },
                
                AdvancedWeaponType.NaniteSwarm => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Nanite Swarm",
                    Description = "Self-replicating microscopic machines that consume targets",
                    BaseDamage = 15f, // Lower initial damage but replicates
                    FireRate = 5f,
                    ProjectileSpeed = 20f,
                    Range = 150f,
                    EnergyConsumption = 25f,
                    AmmoCapacity = 50,
                    ReloadTime = 3f,
                    ElementType = ElementalType.Technological,
                    ElementalDamage = 10f,
                    ElementalDuration = 5f,
                    HasHomingCapability = true,
                    HomingStrength = 3f,
                    CanEvolve = true,
                    EvolutionRate = 0.2f,
                    MaxEvolutionLevel = 5,
                    CreatesStatusEffects = true,
                    StatusEffects = new[] { StatusEffectType.Corrosion, StatusEffectType.Weakness },
                    ProjectileColor = Color.Gray,
                    SecondaryColor = Color.Green,
                    ParticleEffect = ParticleEffectType.NaniteCloud,
                    SoundEffect = SoundEffectType.Swarm,
                    HasTrail = true,
                    TrailEffect = TrailType.Nanite
                },
                
                AdvancedWeaponType.CryoLaser => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Cryo Laser",
                    Description = "Freezing beam that slows and damages enemies over time",
                    BaseDamage = 25f,
                    FireRate = 8f, // Continuous beam
                    ProjectileSpeed = float.MaxValue,
                    Range = 120f,
                    EnergyConsumption = 15f,
                    AmmoCapacity = 200,
                    ReloadTime = 2f,
                    ElementType = ElementalType.Ice,
                    ElementalDamage = 20f,
                    ElementalDuration = 3f,
                    CreatesStatusEffects = true,
                    StatusEffects = new[] { StatusEffectType.Frozen, StatusEffectType.Slowed },
                    ProjectileColor = Color.LightBlue,
                    SecondaryColor = Color.White,
                    ParticleEffect = ParticleEffectType.IceCrystals,
                    SoundEffect = SoundEffectType.CryoBeam,
                    HasTrail = true,
                    TrailEffect = TrailType.Frost
                },
                
                AdvancedWeaponType.ClusterBomb => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Cluster Bomb",
                    Description = "Explosive projectile that splits into multiple warheads",
                    BaseDamage = 60f,
                    FireRate = 1.5f,
                    ProjectileSpeed = 15f,
                    Range = 180f,
                    EnergyConsumption = 40f,
                    AmmoCapacity = 12,
                    ReloadTime = 3.5f,
                    ElementType = ElementalType.Explosive,
                    ElementalDamage = 80f,
                    HasAreaEffect = true,
                    AreaRadius = 12f,
                    AreaDamage = 40f,
                    HasDelayedDetonation = true,
                    DetonationDelay = 1f,
                    ProjectileColor = Color.Orange,
                    SecondaryColor = Color.Red,
                    ParticleEffect = ParticleEffectType.Explosion,
                    SoundEffect = SoundEffectType.ClusterExplosion,
                    HasTrail = true,
                    TrailEffect = TrailType.Smoke
                },
                
                AdvancedWeaponType.RepairBeam => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Repair Beam",
                    Description = "Restores shield and hull integrity of friendly units",
                    BaseDamage = -50f, // Negative damage = healing
                    FireRate = 6f,
                    ProjectileSpeed = float.MaxValue,
                    Range = 100f,
                    EnergyConsumption = 20f,
                    AmmoCapacity = -1, // Unlimited
                    ReloadTime = 0f,
                    ElementType = ElementalType.Restorative,
                    ElementalDamage = -30f,
                    ElementalDuration = 1f,
                    CreatesStatusEffects = true,
                    StatusEffects = new[] { StatusEffectType.Regeneration, StatusEffectType.ShieldBoost },
                    ProjectileColor = Color.Green,
                    SecondaryColor = Color.Lime,
                    ParticleEffect = ParticleEffectType.HealingSparkles,
                    SoundEffect = SoundEffectType.Restoration,
                    HasTrail = true,
                    TrailEffect = TrailType.Healing
                },
                
                _ => new AdvancedWeaponStats
                {
                    Type = type,
                    Name = "Unknown Weapon",
                    Description = "Experimental weapon technology",
                    BaseDamage = 50f,
                    FireRate = 2f,
                    ProjectileSpeed = 20f,
                    Range = 100f,
                    EnergyConsumption = 20f,
                    AmmoCapacity = 30,
                    ReloadTime = 2f,
                    ElementType = ElementalType.Kinetic,
                    ProjectileColor = Color.White,
                    SecondaryColor = Color.Gray,
                    ParticleEffect = ParticleEffectType.Default,
                    SoundEffect = SoundEffectType.Generic
                }
            };
        }
    }
    
    /// <summary>
    /// Elemental damage types for advanced weapons
    /// </summary>
    public enum ElementalType
    {
        Kinetic,          // Physical impact damage
        Energy,           // Pure energy damage
        Plasma,           // Superheated matter
        Ion,              // Charged particle damage
        Electromagnetic,  // EMP and magnetic effects
        Quantum,          // Reality distortion
        Technological,    // Nanite and AI effects
        Ice,              // Freezing damage
        Fire,             // Burning damage
        Explosive,        // Area blast damage
        Restorative,      // Healing effects
        Temporal,         // Time-based effects
        Gravitational,    // Gravity manipulation
        Dimensional       // Cross-dimensional effects
    }
    
    /// <summary>
    /// Status effects that weapons can inflict
    /// </summary>
    public enum StatusEffectType
    {
        // Debuffs
        Burning,          // Damage over time
        Frozen,           // Unable to move
        Slowed,           // Reduced movement speed
        Weakened,         // Reduced damage output
        Blinded,          // Reduced accuracy
        Confused,         // Random movement
        Corroded,         // Reduced armor/shields
        Stunned,          // Temporary paralysis
        Drained,          // Reduced energy
        Marked,           // Increased damage taken
        
        // Buffs
        Regeneration,     // Health over time
        ShieldBoost,      // Increased shield strength
        SpeedBoost,       // Increased movement
        DamageBoost,      // Increased damage output
        AccuracyBoost,    // Improved accuracy
        EnergyRegen,      // Faster energy recovery
        Resistance,       // Reduced damage taken
        
        // Special
        Phased,           // Can pass through matter
        Cloaked,          // Reduced visibility
        Magnetized,       // Attracts metal objects
        Electrified,      // Damages attackers
        Quantum,          // Exists in multiple states
        Weakness          // General vulnerability
    }
    
    /// <summary>
    /// Visual particle effects for weapons
    /// </summary>
    public enum ParticleEffectType
    {
        Default,
        ElectricSparks,
        QuantumDistortion,
        NaniteCloud,
        IceCrystals,
        Explosion,
        HealingSparkles,
        PlasmaTrail,
        GravityWaves,
        DimensionalRift,
        EnergyRipples,
        FireBurst,
        SmokeCloud,
        MetallicShards,
        PhotonBeam
    }
    
    /// <summary>
    /// Sound effect types for weapons
    /// </summary>
    public enum SoundEffectType
    {
        Generic,
        ElectroMagnetic,
        Quantum,
        Swarm,
        CryoBeam,
        ClusterExplosion,
        Restoration,
        PlasmaDischarge,
        GravityPulse,
        DimensionalTear,
        EnergyHum,
        Incineration,
        MetallicClang,
        PhotonBurst
    }
    
    /// <summary>
    /// Trail effects for projectiles
    /// </summary>
    public enum TrailType
    {
        None,
        Electric,
        Quantum,
        Nanite,
        Frost,
        Smoke,
        Healing,
        Plasma,
        Gravity,
        Dimensional,
        Energy,
        Fire,
        Metal,
        Photon
    }
    
    /// <summary>
    /// Status effect implementation
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType Type { get; set; }
        public float Duration { get; set; }
        public float Strength { get; set; }
        public float RemainingTime { get; set; }
        public bool IsActive { get; set; }
        public Vector3 SourcePosition { get; set; }
        public Color EffectColor { get; set; }
        
        public StatusEffect(StatusEffectType type, float duration, float strength, Vector3 sourcePosition)
        {
            Type = type;
            Duration = duration;
            Strength = strength;
            RemainingTime = duration;
            IsActive = true;
            SourcePosition = sourcePosition;
            EffectColor = GetEffectColor(type);
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            RemainingTime -= deltaTime;
            if (RemainingTime <= 0)
            {
                IsActive = false;
            }
        }
        
        public float GetIntensity()
        {
            if (!IsActive) return 0f;
            return RemainingTime / Duration;
        }
        
        private Color GetEffectColor(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Burning => Color.Orange,
                StatusEffectType.Frozen => Color.LightBlue,
                StatusEffectType.Slowed => Color.Blue,
                StatusEffectType.Weakened => Color.Gray,
                StatusEffectType.Blinded => Color.Black,
                StatusEffectType.Confused => Color.Purple,
                StatusEffectType.Corroded => Color.Green,
                StatusEffectType.Stunned => Color.Yellow,
                StatusEffectType.Drained => Color.DarkBlue,
                StatusEffectType.Marked => Color.Red,
                StatusEffectType.Regeneration => Color.Lime,
                StatusEffectType.ShieldBoost => Color.Cyan,
                StatusEffectType.SpeedBoost => Color.Gold,
                StatusEffectType.DamageBoost => Color.DarkRed,
                StatusEffectType.AccuracyBoost => Color.White,
                StatusEffectType.EnergyRegen => Color.Blue,
                StatusEffectType.Resistance => Color.Silver,
                StatusEffectType.Phased => Color.Violet,
                StatusEffectType.Cloaked => Color.Gray,
                StatusEffectType.Magnetized => Color.DarkGray,
                StatusEffectType.Electrified => Color.Yellow,
                StatusEffectType.Quantum => Color.Magenta,
                _ => Color.White
            };
        }
    }
}