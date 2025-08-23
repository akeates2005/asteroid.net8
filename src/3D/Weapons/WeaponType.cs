using System;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Defines different weapon types available in the game
    /// </summary>
    public enum WeaponType
    {
        Standard,       // Basic single shot
        Plasma,         // High damage, slow rate
        Laser,          // Instant hit, continuous beam
        Missile,        // Tracking projectiles
        Shotgun,        // Multiple projectiles
        Beam,           // Piercing laser beam
        RailGun,        // High velocity, high damage
        Pulse,          // Energy burst
        Flamethrower,   // Short range area damage
        Lightning       // Chain lightning effect
    }

    /// <summary>
    /// Weapon firing patterns
    /// </summary>
    public enum FiringPattern
    {
        Single,         // One projectile
        Burst,          // Multiple shots in sequence
        Spread,         // Multiple shots at once in different directions
        Spiral,         // Rotating pattern
        Wave,           // Sine wave pattern
        Cone,           // Cone-shaped spread
        Ring,           // 360-degree ring
        Helix          // Helical pattern
    }

    /// <summary>
    /// Damage types for different effects
    /// </summary>
    public enum DamageType
    {
        Kinetic,        // Standard physical damage
        Energy,         // Energy-based damage
        Explosive,      // Area of effect damage
        Piercing,       // Goes through multiple targets
        Plasma,         // High heat damage
        Ion,            // Disruptive damage
        Electromagnetic, // EMP-like effects
        Thermal         // Heat-based damage
    }

    /// <summary>
    /// Weapon attributes and statistics
    /// </summary>
    [Serializable]
    public struct WeaponStats
    {
        public WeaponType Type;
        public string Name;
        public float Damage;
        public float FireRate;          // Shots per second
        public float Range;
        public float Speed;             // Projectile speed
        public float EnergyConsumption;
        public int AmmoCapacity;
        public float ReloadTime;
        public FiringPattern Pattern;
        public DamageType DamageType;
        public float Accuracy;          // 0.0 to 1.0
        public float CriticalChance;    // 0.0 to 1.0
        public float CriticalMultiplier;
        public int ProjectileCount;     // For shotgun/spread weapons
        public float SpreadAngle;       // Degrees
        public bool RequiresCharging;
        public float ChargeTime;
        public bool FullAuto;
        public float Knockback;
        public float PenetrationPower;  // Ability to pierce targets
        public bool HasSplashDamage;
        public float SplashRadius;
        public float SplashDamage;

        public static WeaponStats CreateDefault(WeaponType type)
        {
            return type switch
            {
                WeaponType.Standard => new WeaponStats
                {
                    Type = WeaponType.Standard,
                    Name = "Standard Cannon",
                    Damage = 25f,
                    FireRate = 3f,
                    Range = 100f,
                    Speed = 15f,
                    EnergyConsumption = 5f,
                    AmmoCapacity = -1, // Unlimited
                    ReloadTime = 0f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Kinetic,
                    Accuracy = 0.95f,
                    CriticalChance = 0.05f,
                    CriticalMultiplier = 2f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = false,
                    Knockback = 10f,
                    PenetrationPower = 0f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.Plasma => new WeaponStats
                {
                    Type = WeaponType.Plasma,
                    Name = "Plasma Cannon",
                    Damage = 50f,
                    FireRate = 1.5f,
                    Range = 80f,
                    Speed = 12f,
                    EnergyConsumption = 15f,
                    AmmoCapacity = 20,
                    ReloadTime = 2f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Plasma,
                    Accuracy = 0.9f,
                    CriticalChance = 0.15f,
                    CriticalMultiplier = 2.5f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = true,
                    ChargeTime = 0.5f,
                    FullAuto = false,
                    Knockback = 20f,
                    PenetrationPower = 0.3f,
                    HasSplashDamage = true,
                    SplashRadius = 5f,
                    SplashDamage = 15f
                },

                WeaponType.Laser => new WeaponStats
                {
                    Type = WeaponType.Laser,
                    Name = "Laser Beam",
                    Damage = 35f,
                    FireRate = 10f, // Continuous
                    Range = 150f,
                    Speed = float.MaxValue, // Instant
                    EnergyConsumption = 8f,
                    AmmoCapacity = 100,
                    ReloadTime = 1.5f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Energy,
                    Accuracy = 1f,
                    CriticalChance = 0.1f,
                    CriticalMultiplier = 1.8f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = true,
                    Knockback = 5f,
                    PenetrationPower = 0.8f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.Missile => new WeaponStats
                {
                    Type = WeaponType.Missile,
                    Name = "Tracking Missile",
                    Damage = 75f,
                    FireRate = 0.8f,
                    Range = 120f,
                    Speed = 8f,
                    EnergyConsumption = 25f,
                    AmmoCapacity = 10,
                    ReloadTime = 3f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Explosive,
                    Accuracy = 0.7f, // Lower initial accuracy, but tracks
                    CriticalChance = 0.2f,
                    CriticalMultiplier = 3f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = false,
                    Knockback = 30f,
                    PenetrationPower = 0f,
                    HasSplashDamage = true,
                    SplashRadius = 15f,
                    SplashDamage = 40f
                },

                WeaponType.Shotgun => new WeaponStats
                {
                    Type = WeaponType.Shotgun,
                    Name = "Plasma Shotgun",
                    Damage = 20f, // Per pellet
                    FireRate = 2f,
                    Range = 60f,
                    Speed = 18f,
                    EnergyConsumption = 12f,
                    AmmoCapacity = 15,
                    ReloadTime = 2.5f,
                    Pattern = FiringPattern.Spread,
                    DamageType = DamageType.Kinetic,
                    Accuracy = 0.8f,
                    CriticalChance = 0.08f,
                    CriticalMultiplier = 2.2f,
                    ProjectileCount = 7,
                    SpreadAngle = 30f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = false,
                    Knockback = 25f,
                    PenetrationPower = 0.1f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.Beam => new WeaponStats
                {
                    Type = WeaponType.Beam,
                    Name = "Piercing Beam",
                    Damage = 40f,
                    FireRate = 1f,
                    Range = 200f,
                    Speed = float.MaxValue,
                    EnergyConsumption = 20f,
                    AmmoCapacity = 30,
                    ReloadTime = 2f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Energy,
                    Accuracy = 0.98f,
                    CriticalChance = 0.12f,
                    CriticalMultiplier = 2.5f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = true,
                    ChargeTime = 1f,
                    FullAuto = false,
                    Knockback = 15f,
                    PenetrationPower = 1f, // Pierces everything
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.RailGun => new WeaponStats
                {
                    Type = WeaponType.RailGun,
                    Name = "Rail Gun",
                    Damage = 100f,
                    FireRate = 0.5f,
                    Range = 250f,
                    Speed = 50f,
                    EnergyConsumption = 40f,
                    AmmoCapacity = 5,
                    ReloadTime = 4f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Electromagnetic,
                    Accuracy = 0.99f,
                    CriticalChance = 0.3f,
                    CriticalMultiplier = 4f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = true,
                    ChargeTime = 2f,
                    FullAuto = false,
                    Knockback = 50f,
                    PenetrationPower = 1f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.Pulse => new WeaponStats
                {
                    Type = WeaponType.Pulse,
                    Name = "Pulse Cannon",
                    Damage = 30f,
                    FireRate = 4f,
                    Range = 90f,
                    Speed = 20f,
                    EnergyConsumption = 10f,
                    AmmoCapacity = 50,
                    ReloadTime = 1.8f,
                    Pattern = FiringPattern.Burst,
                    DamageType = DamageType.Energy,
                    Accuracy = 0.88f,
                    CriticalChance = 0.1f,
                    CriticalMultiplier = 2.3f,
                    ProjectileCount = 3, // Burst of 3
                    SpreadAngle = 5f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = true,
                    Knockback = 12f,
                    PenetrationPower = 0.2f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                WeaponType.Flamethrower => new WeaponStats
                {
                    Type = WeaponType.Flamethrower,
                    Name = "Plasma Flamethrower",
                    Damage = 15f, // Per tick
                    FireRate = 15f, // High tick rate
                    Range = 40f,
                    Speed = 8f,
                    EnergyConsumption = 6f,
                    AmmoCapacity = 200,
                    ReloadTime = 3f,
                    Pattern = FiringPattern.Cone,
                    DamageType = DamageType.Thermal,
                    Accuracy = 0.9f,
                    CriticalChance = 0.05f,
                    CriticalMultiplier = 1.5f,
                    ProjectileCount = 10,
                    SpreadAngle = 45f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = true,
                    Knockback = 8f,
                    PenetrationPower = 0f,
                    HasSplashDamage = true,
                    SplashRadius = 8f,
                    SplashDamage = 8f
                },

                WeaponType.Lightning => new WeaponStats
                {
                    Type = WeaponType.Lightning,
                    Name = "Chain Lightning",
                    Damage = 45f,
                    FireRate = 2f,
                    Range = 100f,
                    Speed = float.MaxValue,
                    EnergyConsumption = 18f,
                    AmmoCapacity = 25,
                    ReloadTime = 2.2f,
                    Pattern = FiringPattern.Single,
                    DamageType = DamageType.Electromagnetic,
                    Accuracy = 0.85f,
                    CriticalChance = 0.15f,
                    CriticalMultiplier = 2.8f,
                    ProjectileCount = 1,
                    SpreadAngle = 0f,
                    RequiresCharging = false,
                    ChargeTime = 0f,
                    FullAuto = false,
                    Knockback = 20f,
                    PenetrationPower = 0.5f,
                    HasSplashDamage = false,
                    SplashRadius = 0f,
                    SplashDamage = 0f
                },

                _ => throw new ArgumentException($"Unknown weapon type: {type}")
            };
        }
    }
}