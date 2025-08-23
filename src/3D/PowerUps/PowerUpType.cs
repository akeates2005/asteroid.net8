using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.PowerUps
{
    /// <summary>
    /// Different types of power-ups available in the game
    /// </summary>
    public enum PowerUpType
    {
        // Weapon Upgrades
        WeaponUpgrade,      // Increases current weapon level
        DualShot,           // Fires two projectiles
        TripleShot,         // Fires three projectiles
        RapidFire,          // Increases fire rate
        PiercingShots,      // Shots penetrate targets
        ExplosiveShots,     // Shots explode on impact
        
        // Shield and Defense
        ShieldBoost,        // Instantly recharges shield
        ShieldExtender,     // Extends shield duration
        ArmorPlating,       // Reduces incoming damage
        Regeneration,       // Gradually restores health
        
        // Movement and Speed
        SpeedBoost,         // Increases movement speed
        ThrustUpgrade,      // More powerful engines
        Agility,            // Better maneuverability
        
        // Energy and Ammo
        EnergyRecharge,     // Instantly restores energy
        EnergyEfficiency,   // Reduces energy consumption
        AmmoRefill,         // Refills all weapon ammo
        InfiniteAmmo,       // Temporary unlimited ammo
        
        // Special Abilities
        TimeDilation,       // Slows down time
        Cloaking,           // Temporary invisibility
        MagneticField,      // Attracts items and deflects projectiles
        ShieldOvercharge,   // Temporary invincibility
        
        // New Weapons
        PlasmaWeapon,       // Unlocks plasma cannon
        LaserWeapon,        // Unlocks laser beam
        MissileWeapon,      // Unlocks tracking missiles
        ShotgunWeapon,      // Unlocks shotgun
        BeamWeapon,         // Unlocks piercing beam
        LightningWeapon,    // Unlocks chain lightning
        
        // Score and Multipliers
        ScoreMultiplier,    // Increases score gain
        BonusPoints,        // Instant score bonus
        ExtraLife,          // Adds an extra life
        
        // Rare/Legendary
        Devastator,         // Massive damage boost
        Phoenix,            // Revive on death once
        TimeFreeze,         // Freezes all enemies briefly
        NuclearDevice       // Destroys all enemies on screen
    }

    /// <summary>
    /// Power-up rarity levels affecting spawn rates and effects
    /// </summary>
    public enum PowerUpRarity
    {
        Common,     // 60% spawn rate
        Uncommon,   // 25% spawn rate
        Rare,       // 12% spawn rate
        Epic,       // 2.5% spawn rate
        Legendary   // 0.5% spawn rate
    }

    /// <summary>
    /// Power-up configuration and stats
    /// </summary>
    [Serializable]
    public struct PowerUpConfig
    {
        public PowerUpType Type;
        public string Name;
        public string Description;
        public PowerUpRarity Rarity;
        public float Duration;          // Duration in seconds (0 = instant effect)
        public float EffectStrength;    // Multiplier or value for the effect
        public Color Color;
        public bool IsStackable;        // Can multiple instances be active
        public int MaxStacks;          // Maximum number of stacks
        public float CooldownTime;     // Time before same power-up can be picked up again
        public bool IsPermanent;       // Effect lasts until death
        public float SpawnWeight;      // Relative spawn probability

        public static PowerUpConfig CreateDefault(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.WeaponUpgrade => new PowerUpConfig
                {
                    Type = PowerUpType.WeaponUpgrade,
                    Name = "Weapon Upgrade",
                    Description = "Enhances your current weapon",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Instant
                    EffectStrength = 1f,
                    Color = Color.Gold,
                    IsStackable = true,
                    MaxStacks = 5,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 15f
                },

                PowerUpType.DualShot => new PowerUpConfig
                {
                    Type = PowerUpType.DualShot,
                    Name = "Dual Shot",
                    Description = "Fire two projectiles at once",
                    Rarity = PowerUpRarity.Common,
                    Duration = 15f,
                    EffectStrength = 2f,
                    Color = Color.Orange,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 5f,
                    IsPermanent = false,
                    SpawnWeight = 20f
                },

                PowerUpType.TripleShot => new PowerUpConfig
                {
                    Type = PowerUpType.TripleShot,
                    Name = "Triple Shot",
                    Description = "Fire three projectiles at once",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 12f,
                    EffectStrength = 3f,
                    Color = Color.Red,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 8f,
                    IsPermanent = false,
                    SpawnWeight = 12f
                },

                PowerUpType.RapidFire => new PowerUpConfig
                {
                    Type = PowerUpType.RapidFire,
                    Name = "Rapid Fire",
                    Description = "Significantly increases fire rate",
                    Rarity = PowerUpRarity.Common,
                    Duration = 10f,
                    EffectStrength = 2f, // 2x fire rate
                    Color = Color.Yellow,
                    IsStackable = true,
                    MaxStacks = 3,
                    CooldownTime = 3f,
                    IsPermanent = false,
                    SpawnWeight = 18f
                },

                PowerUpType.PiercingShots => new PowerUpConfig
                {
                    Type = PowerUpType.PiercingShots,
                    Name = "Piercing Shots",
                    Description = "Shots penetrate through enemies",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 8f,
                    EffectStrength = 0.8f, // Penetration power
                    Color = Color.Cyan,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 5f,
                    IsPermanent = false,
                    SpawnWeight = 14f
                },

                PowerUpType.ExplosiveShots => new PowerUpConfig
                {
                    Type = PowerUpType.ExplosiveShots,
                    Name = "Explosive Shots",
                    Description = "Shots explode on impact",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 12f,
                    EffectStrength = 1.5f, // Explosion damage multiplier
                    Color = Color.Orange,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 10f,
                    IsPermanent = false,
                    SpawnWeight = 8f
                },

                PowerUpType.ShieldBoost => new PowerUpConfig
                {
                    Type = PowerUpType.ShieldBoost,
                    Name = "Shield Boost",
                    Description = "Instantly recharges your shield",
                    Rarity = PowerUpRarity.Common,
                    Duration = 0f, // Instant
                    EffectStrength = 1f,
                    Color = Color.Blue,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 2f,
                    IsPermanent = false,
                    SpawnWeight = 25f
                },

                PowerUpType.ShieldExtender => new PowerUpConfig
                {
                    Type = PowerUpType.ShieldExtender,
                    Name = "Shield Extender",
                    Description = "Extends shield duration",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Instant upgrade
                    EffectStrength = 1.5f, // 50% longer shield
                    Color = Color.SkyBlue,
                    IsStackable = true,
                    MaxStacks = 3,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 12f
                },

                PowerUpType.ArmorPlating => new PowerUpConfig
                {
                    Type = PowerUpType.ArmorPlating,
                    Name = "Armor Plating",
                    Description = "Reduces incoming damage",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 20f,
                    EffectStrength = 0.5f, // 50% damage reduction
                    Color = Color.Gray,
                    IsStackable = true,
                    MaxStacks = 2,
                    CooldownTime = 15f,
                    IsPermanent = false,
                    SpawnWeight = 10f
                },

                PowerUpType.Regeneration => new PowerUpConfig
                {
                    Type = PowerUpType.Regeneration,
                    Name = "Regeneration",
                    Description = "Gradually restores health",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 15f,
                    EffectStrength = 5f, // Health per second
                    Color = Color.Green,
                    IsStackable = true,
                    MaxStacks = 2,
                    CooldownTime = 8f,
                    IsPermanent = false,
                    SpawnWeight = 13f
                },

                PowerUpType.SpeedBoost => new PowerUpConfig
                {
                    Type = PowerUpType.SpeedBoost,
                    Name = "Speed Boost",
                    Description = "Increases movement speed",
                    Rarity = PowerUpRarity.Common,
                    Duration = 12f,
                    EffectStrength = 1.5f, // 50% speed increase
                    Color = Color.Lime,
                    IsStackable = true,
                    MaxStacks = 2,
                    CooldownTime = 5f,
                    IsPermanent = false,
                    SpawnWeight = 20f
                },

                PowerUpType.ThrustUpgrade => new PowerUpConfig
                {
                    Type = PowerUpType.ThrustUpgrade,
                    Name = "Thrust Upgrade",
                    Description = "More powerful engines",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Permanent upgrade
                    EffectStrength = 1.3f, // 30% more thrust
                    Color = Color.Purple,
                    IsStackable = true,
                    MaxStacks = 3,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 11f
                },

                PowerUpType.Agility => new PowerUpConfig
                {
                    Type = PowerUpType.Agility,
                    Name = "Agility",
                    Description = "Better maneuverability",
                    Rarity = PowerUpRarity.Common,
                    Duration = 15f,
                    EffectStrength = 1.4f, // 40% better turning
                    Color = Color.Pink,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 7f,
                    IsPermanent = false,
                    SpawnWeight = 16f
                },

                PowerUpType.EnergyRecharge => new PowerUpConfig
                {
                    Type = PowerUpType.EnergyRecharge,
                    Name = "Energy Recharge",
                    Description = "Instantly restores energy",
                    Rarity = PowerUpRarity.Common,
                    Duration = 0f, // Instant
                    EffectStrength = 1f, // Full energy restore
                    Color = Color.White,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 3f,
                    IsPermanent = false,
                    SpawnWeight = 22f
                },

                PowerUpType.EnergyEfficiency => new PowerUpConfig
                {
                    Type = PowerUpType.EnergyEfficiency,
                    Name = "Energy Efficiency",
                    Description = "Reduces energy consumption",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 18f,
                    EffectStrength = 0.7f, // 30% less energy consumption
                    Color = Color.LightBlue,
                    IsStackable = true,
                    MaxStacks = 2,
                    CooldownTime = 10f,
                    IsPermanent = false,
                    SpawnWeight = 14f
                },

                PowerUpType.AmmoRefill => new PowerUpConfig
                {
                    Type = PowerUpType.AmmoRefill,
                    Name = "Ammo Refill",
                    Description = "Refills all weapon ammunition",
                    Rarity = PowerUpRarity.Common,
                    Duration = 0f, // Instant
                    EffectStrength = 1f,
                    Color = Color.Brown,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 5f,
                    IsPermanent = false,
                    SpawnWeight = 18f
                },

                PowerUpType.InfiniteAmmo => new PowerUpConfig
                {
                    Type = PowerUpType.InfiniteAmmo,
                    Name = "Infinite Ammo",
                    Description = "Unlimited ammunition for a short time",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 8f,
                    EffectStrength = 1f,
                    Color = Color.Gold,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 20f,
                    IsPermanent = false,
                    SpawnWeight = 6f
                },

                PowerUpType.TimeDilation => new PowerUpConfig
                {
                    Type = PowerUpType.TimeDilation,
                    Name = "Time Dilation",
                    Description = "Slows down time around you",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 5f,
                    EffectStrength = 0.3f, // 30% time speed
                    Color = Color.Violet,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 30f,
                    IsPermanent = false,
                    SpawnWeight = 3f
                },

                PowerUpType.Cloaking => new PowerUpConfig
                {
                    Type = PowerUpType.Cloaking,
                    Name = "Cloaking",
                    Description = "Temporary invisibility",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 6f,
                    EffectStrength = 0.2f, // 20% visibility
                    Color = Color.Gray,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 25f,
                    IsPermanent = false,
                    SpawnWeight = 5f
                },

                PowerUpType.MagneticField => new PowerUpConfig
                {
                    Type = PowerUpType.MagneticField,
                    Name = "Magnetic Field",
                    Description = "Attracts items and deflects projectiles",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 10f,
                    EffectStrength = 15f, // Field radius
                    Color = Color.Magenta,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 20f,
                    IsPermanent = false,
                    SpawnWeight = 7f
                },

                PowerUpType.ShieldOvercharge => new PowerUpConfig
                {
                    Type = PowerUpType.ShieldOvercharge,
                    Name = "Shield Overcharge",
                    Description = "Temporary invincibility",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 4f,
                    EffectStrength = 1f,
                    Color = Color.White,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 60f,
                    IsPermanent = false,
                    SpawnWeight = 2f
                },

                PowerUpType.PlasmaWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.PlasmaWeapon,
                    Name = "Plasma Weapon",
                    Description = "Unlocks the plasma cannon",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.Cyan,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 10f
                },

                PowerUpType.LaserWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.LaserWeapon,
                    Name = "Laser Weapon",
                    Description = "Unlocks the laser beam",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.Red,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 10f
                },

                PowerUpType.MissileWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.MissileWeapon,
                    Name = "Missile Weapon",
                    Description = "Unlocks tracking missiles",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.Orange,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 8f
                },

                PowerUpType.ShotgunWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.ShotgunWeapon,
                    Name = "Shotgun Weapon",
                    Description = "Unlocks the shotgun",
                    Rarity = PowerUpRarity.Uncommon,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.Yellow,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 9f
                },

                PowerUpType.BeamWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.BeamWeapon,
                    Name = "Beam Weapon",
                    Description = "Unlocks the piercing beam",
                    Rarity = PowerUpRarity.Rare,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.Purple,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 6f
                },

                PowerUpType.LightningWeapon => new PowerUpConfig
                {
                    Type = PowerUpType.LightningWeapon,
                    Name = "Lightning Weapon",
                    Description = "Unlocks chain lightning",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 0f, // Permanent unlock
                    EffectStrength = 1f,
                    Color = Color.White,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 4f
                },

                PowerUpType.ScoreMultiplier => new PowerUpConfig
                {
                    Type = PowerUpType.ScoreMultiplier,
                    Name = "Score Multiplier",
                    Description = "Increases score gain",
                    Rarity = PowerUpRarity.Common,
                    Duration = 20f,
                    EffectStrength = 2f, // 2x score
                    Color = Color.Gold,
                    IsStackable = true,
                    MaxStacks = 3,
                    CooldownTime = 0f,
                    IsPermanent = false,
                    SpawnWeight = 15f
                },

                PowerUpType.BonusPoints => new PowerUpConfig
                {
                    Type = PowerUpType.BonusPoints,
                    Name = "Bonus Points",
                    Description = "Instant score bonus",
                    Rarity = PowerUpRarity.Common,
                    Duration = 0f, // Instant
                    EffectStrength = 1000f, // Points awarded
                    Color = Color.Yellow,
                    IsStackable = true,
                    MaxStacks = 99,
                    CooldownTime = 0f,
                    IsPermanent = false,
                    SpawnWeight = 20f
                },

                PowerUpType.ExtraLife => new PowerUpConfig
                {
                    Type = PowerUpType.ExtraLife,
                    Name = "Extra Life",
                    Description = "Grants an additional life",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 0f, // Instant
                    EffectStrength = 1f,
                    Color = Color.Green,
                    IsStackable = true,
                    MaxStacks = 9,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 2f
                },

                PowerUpType.Devastator => new PowerUpConfig
                {
                    Type = PowerUpType.Devastator,
                    Name = "Devastator",
                    Description = "Massive damage boost",
                    Rarity = PowerUpRarity.Legendary,
                    Duration = 8f,
                    EffectStrength = 5f, // 5x damage
                    Color = Color.DarkRed,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 45f,
                    IsPermanent = false,
                    SpawnWeight = 1f
                },

                PowerUpType.Phoenix => new PowerUpConfig
                {
                    Type = PowerUpType.Phoenix,
                    Name = "Phoenix",
                    Description = "Revive once upon death",
                    Rarity = PowerUpRarity.Legendary,
                    Duration = 0f, // Until used
                    EffectStrength = 1f,
                    Color = Color.Orange,
                    IsStackable = true,
                    MaxStacks = 2,
                    CooldownTime = 0f,
                    IsPermanent = true,
                    SpawnWeight = 1f
                },

                PowerUpType.TimeFreeze => new PowerUpConfig
                {
                    Type = PowerUpType.TimeFreeze,
                    Name = "Time Freeze",
                    Description = "Freezes all enemies briefly",
                    Rarity = PowerUpRarity.Epic,
                    Duration = 3f,
                    EffectStrength = 0f, // 0% enemy speed
                    Color = Color.LightBlue,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 40f,
                    IsPermanent = false,
                    SpawnWeight = 2f
                },

                PowerUpType.NuclearDevice => new PowerUpConfig
                {
                    Type = PowerUpType.NuclearDevice,
                    Name = "Nuclear Device",
                    Description = "Destroys all enemies on screen",
                    Rarity = PowerUpRarity.Legendary,
                    Duration = 0f, // Instant
                    EffectStrength = 1f,
                    Color = Color.White,
                    IsStackable = false,
                    MaxStacks = 1,
                    CooldownTime = 120f,
                    IsPermanent = false,
                    SpawnWeight = 0.5f
                },

                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
        }
    }
}