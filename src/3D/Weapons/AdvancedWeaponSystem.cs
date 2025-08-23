using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Advanced weapon system for handling exotic weapon types
    /// </summary>
    public class AdvancedWeaponSystem
    {
        public AdvancedWeaponType WeaponType { get; private set; }
        public bool IsUnlocked { get; private set; }
        public int EvolutionLevel { get; private set; }
        public float Energy { get; private set; }
        public int CurrentAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsCharging { get; private set; }
        
        private AdvancedWeaponStats _baseStats;
        private AdvancedWeaponStats _currentStats;
        private float _lastFireTime;
        private float _chargeStartTime;
        private float _reloadStartTime;
        private float _maxEnergy;
        private Random _random;
        
        // Advanced features
        private List<WeaponModification> _activeModifications;
        private AutoTargetingSystem _autoTargeting;
        private int _projectileMultiplier;
        private float _multiplierDuration;
        private bool _isAutoTargetingEnabled;
        
        // Events
        public event Action<AdvancedWeaponType, int> OnWeaponEvolved;
        public event Action<AdvancedProjectile3D> OnProjectileFired;
        public event Action<float> OnEnergyChanged;
        public event Action<int, int> OnAmmoChanged;
        
        public AdvancedWeaponSystem(AdvancedWeaponType weaponType)
        {
            WeaponType = weaponType;
            _baseStats = AdvancedWeaponStats.CreateDefault(weaponType);
            _currentStats = _baseStats;
            IsUnlocked = false;
            EvolutionLevel = 1;
            _maxEnergy = 1000f;
            Energy = _maxEnergy;
            CurrentAmmo = _currentStats.AmmoCapacity;
            _random = new Random();
            
            _activeModifications = new List<WeaponModification>();
            _autoTargeting = new AutoTargetingSystem();
            _projectileMultiplier = 1;
            
            InitializeWeaponSpecifics();
        }
        
        private void InitializeWeaponSpecifics()
        {
            switch (WeaponType)
            {
                case AdvancedWeaponType.NaniteSwarm:
                    // Nanite swarms start with lower damage but can replicate
                    _currentStats.BaseDamage *= 0.8f;
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    // Quantum weapons have unstable energy consumption
                    _currentStats.EnergyConsumption *= 1.2f;
                    break;
                    
                case AdvancedWeaponType.GaussRifle:
                    // Gauss rifles are very energy efficient but need to charge
                    _currentStats.EnergyConsumption *= 0.7f;
                    break;
            }
        }
        
        public void Update(float deltaTime, Vector3 playerPosition, Vector3 playerVelocity)
        {
            UpdateReloading(deltaTime);
            UpdateCharging(deltaTime);
            UpdateEnergy(deltaTime);
            UpdateModifications(deltaTime);
            UpdateAutoTargeting(deltaTime, playerPosition);
            UpdateProjectileMultiplier(deltaTime);
        }
        
        private void UpdateReloading(float deltaTime)
        {
            if (!IsReloading) return;
            
            float currentTime = GetTime();
            if (currentTime - _reloadStartTime >= _currentStats.ReloadTime)
            {
                CurrentAmmo = _currentStats.AmmoCapacity;
                IsReloading = false;
                OnAmmoChanged?.Invoke(CurrentAmmo, _currentStats.AmmoCapacity);
            }
        }
        
        private void UpdateCharging(float deltaTime)
        {
            // Charging is handled during firing
        }
        
        private void UpdateEnergy(float deltaTime)
        {
            // Regenerate energy
            float regenRate = 50f + (EvolutionLevel - 1) * 10f;
            Energy = Math.Min(_maxEnergy, Energy + regenRate * deltaTime);
            OnEnergyChanged?.Invoke(Energy / _maxEnergy);
        }
        
        private void UpdateModifications(float deltaTime)
        {
            for (int i = _activeModifications.Count - 1; i >= 0; i--)
            {
                _activeModifications[i].Update(deltaTime);
                if (!_activeModifications[i].IsActive)
                {
                    RemoveModification(_activeModifications[i]);
                    _activeModifications.RemoveAt(i);
                }
            }
        }
        
        private void UpdateAutoTargeting(float deltaTime, Vector3 playerPosition)
        {
            if (_isAutoTargetingEnabled)
            {
                _autoTargeting.Update(deltaTime, playerPosition);
            }
        }
        
        private void UpdateProjectileMultiplier(float deltaTime)
        {
            if (_projectileMultiplier > 1)
            {
                _multiplierDuration -= deltaTime;
                if (_multiplierDuration <= 0)
                {
                    _projectileMultiplier = 1;
                }
            }
        }
        
        public AdvancedProjectile3D TryFire(Vector3 position, Vector3 direction, Vector3 velocity, bool isPressed, bool wasPressed)
        {
            if (!CanFire()) return null;
            
            // Handle charging weapons
            if (_currentStats.RequiresCharging)
            {
                if (isPressed && !wasPressed)
                {
                    StartCharging();
                    return null;
                }
                
                if (IsCharging && !isPressed)
                {
                    float chargeRatio = GetChargeRatio();
                    if (chargeRatio >= 0.3f) // Minimum 30% charge
                    {
                        return FireProjectile(position, direction, velocity, chargeRatio);
                    }
                    else
                    {
                        StopCharging();
                        return null;
                    }
                }
                
                return null;
            }
            
            // Handle regular firing
            if (isPressed && (!wasPressed || _currentStats.FullAuto))
            {
                return FireProjectile(position, direction, velocity, 1f);
            }
            
            return null;
        }
        
        private AdvancedProjectile3D FireProjectile(Vector3 position, Vector3 direction, Vector3 velocity, float chargeRatio)
        {
            // Consume resources
            Energy -= _currentStats.EnergyConsumption * chargeRatio;
            if (_currentStats.AmmoCapacity > 0)
            {
                CurrentAmmo--;
                OnAmmoChanged?.Invoke(CurrentAmmo, _currentStats.AmmoCapacity);
                
                if (CurrentAmmo <= 0)
                {
                    StartReload();
                }
            }
            
            OnEnergyChanged?.Invoke(Energy / _maxEnergy);
            
            // Apply auto-targeting
            if (_isAutoTargetingEnabled && _autoTargeting.HasTarget)
            {
                direction = _autoTargeting.GetTargetDirection(position);
            }
            
            // Calculate damage with all modifiers
            float damage = CalculateDamage(chargeRatio);
            
            // Create projectile
            var projectile = new AdvancedProjectile3D(
                position,
                direction * _currentStats.ProjectileSpeed * chargeRatio + velocity,
                _currentStats,
                damage
            );
            
            // Apply homing if weapon supports it
            if (_currentStats.HasHomingCapability && _autoTargeting.HasTarget)
            {
                projectile.SetHomingTarget(_autoTargeting.CurrentTarget);
            }
            
            _lastFireTime = GetTime();
            OnProjectileFired?.Invoke(projectile);
            
            // Handle projectile multiplier
            if (_projectileMultiplier > 1)
            {
                return CreateMultipleProjectiles(position, direction, velocity, chargeRatio, damage);
            }
            
            if (IsCharging)
            {
                StopCharging();
            }
            
            return projectile;
        }
        
        private AdvancedProjectile3D CreateMultipleProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeRatio, float damage)
        {
            var mainProjectile = new AdvancedProjectile3D(
                position,
                direction * _currentStats.ProjectileSpeed * chargeRatio + velocity,
                _currentStats,
                damage
            );
            
            // Create additional projectiles with slight variations
            for (int i = 1; i < _projectileMultiplier; i++)
            {
                Vector3 spreadDirection = ApplySpread(direction, i * 0.1f);
                var additionalProjectile = new AdvancedProjectile3D(
                    position,
                    spreadDirection * _currentStats.ProjectileSpeed * chargeRatio + velocity,
                    _currentStats,
                    damage * 0.8f // Slightly reduced damage for additional projectiles
                );
                
                OnProjectileFired?.Invoke(additionalProjectile);
            }
            
            return mainProjectile;
        }
        
        private bool CanFire()
        {
            if (!IsUnlocked) return false;
            if (IsReloading) return false;
            if (Energy < _currentStats.EnergyConsumption) return false;
            if (_currentStats.AmmoCapacity > 0 && CurrentAmmo <= 0) return false;
            
            float currentTime = GetTime();
            float timeSinceLastShot = currentTime - _lastFireTime;
            float fireInterval = 1f / _currentStats.FireRate;
            
            return timeSinceLastShot >= fireInterval;
        }
        
        private void StartCharging()
        {
            IsCharging = true;
            _chargeStartTime = GetTime();
        }
        
        private void StopCharging()
        {
            IsCharging = false;
        }
        
        private float GetChargeRatio()
        {
            if (!IsCharging) return 0f;
            
            float chargeTime = GetTime() - _chargeStartTime;
            return Math.Min(1f, chargeTime / Math.Max(0.1f, _currentStats.ChargeTime));
        }
        
        private void StartReload()
        {
            if (IsReloading || _currentStats.AmmoCapacity <= 0) return;
            
            IsReloading = true;
            _reloadStartTime = GetTime();
        }
        
        private float CalculateDamage(float chargeRatio)
        {
            float baseDamage = _currentStats.BaseDamage * chargeRatio;
            
            // Apply evolution bonus
            baseDamage *= 1f + (EvolutionLevel - 1) * 0.2f;
            
            // Apply modifications
            foreach (var mod in _activeModifications)
            {
                baseDamage = mod.ApplyDamageModification(baseDamage);
            }
            
            // Random variance
            float variance = 1f + ((float)_random.NextDouble() - 0.5f) * 0.2f;
            baseDamage *= variance;
            
            return baseDamage;
        }
        
        private Vector3 ApplySpread(Vector3 direction, float spreadAmount)
        {
            Vector3 spread = new Vector3(
                (float)(_random.NextDouble() - 0.5) * spreadAmount,
                (float)(_random.NextDouble() - 0.5) * spreadAmount,
                (float)(_random.NextDouble() - 0.5) * spreadAmount
            );
            
            return Vector3.Normalize(direction + spread);
        }
        
        public void Unlock()
        {
            IsUnlocked = true;
        }
        
        public void EvolveWeapon(int newLevel)
        {
            if (newLevel <= EvolutionLevel) return;
            
            EvolutionLevel = newLevel;
            ApplyEvolutionBonuses();
            OnWeaponEvolved?.Invoke(WeaponType, EvolutionLevel);
        }
        
        private void ApplyEvolutionBonuses()
        {
            float evolutionMultiplier = 1f + (EvolutionLevel - 1) * 0.25f;
            
            _currentStats.BaseDamage = _baseStats.BaseDamage * evolutionMultiplier;
            _currentStats.FireRate = _baseStats.FireRate * (1f + (EvolutionLevel - 1) * 0.15f);
            _currentStats.Range = _baseStats.Range * (1f + (EvolutionLevel - 1) * 0.1f);
            _currentStats.EnergyConsumption = _baseStats.EnergyConsumption * (1f - (EvolutionLevel - 1) * 0.1f);
            
            // Special evolution effects
            if (EvolutionLevel >= 3)
            {
                _currentStats.HasHomingCapability = true;
                _currentStats.HomingStrength = Math.Min(3f, _currentStats.HomingStrength + 0.5f);
            }
            
            if (EvolutionLevel >= 5)
            {
                _currentStats.CanPierceShields = true;
                _currentStats.ShieldPenetration = 0.5f;
            }
        }
        
        public void AddModification(WeaponModification modification)
        {
            _activeModifications.Add(modification);
            ApplyModification(modification);
        }
        
        private void ApplyModification(WeaponModification modification)
        {
            switch (modification.Type)
            {
                case ModificationType.DamageBoost:
                    _currentStats.BaseDamage *= modification.Strength;
                    break;
                    
                case ModificationType.FireRateBoost:
                    _currentStats.FireRate *= modification.Strength;
                    break;
                    
                case ModificationType.EnergyEfficiency:
                    _currentStats.EnergyConsumption /= modification.Strength;
                    break;
                    
                case ModificationType.AccuracyBoost:
                    _currentStats.Accuracy = Math.Min(1f, _currentStats.Accuracy * modification.Strength);
                    break;
                    
                case ModificationType.ElementalInfusion:
                    _currentStats.ElementalDamage += modification.Strength;
                    break;
            }
        }
        
        private void RemoveModification(WeaponModification modification)
        {
            // Recalculate stats from base
            _currentStats = _baseStats;
            ApplyEvolutionBonuses();
            
            // Reapply remaining modifications
            foreach (var mod in _activeModifications)
            {
                if (mod != modification)
                {
                    ApplyModification(mod);
                }
            }
        }
        
        public void EnableAutoTargeting(float strength, float duration)
        {
            _isAutoTargetingEnabled = true;
            _autoTargeting.SetStrength(strength);
            _autoTargeting.SetDuration(duration);
        }
        
        public void SetProjectileMultiplier(int multiplier, float duration)
        {
            _projectileMultiplier = multiplier;
            _multiplierDuration = duration;
        }
        
        public void Draw(Camera3D camera)
        {
            // Draw weapon-specific effects
            if (IsCharging)
            {
                DrawChargingEffect(camera);
            }
            
            if (_isAutoTargetingEnabled && _autoTargeting.HasTarget)
            {
                DrawTargetingEffect(camera);
            }
            
            // Draw modifications effects
            foreach (var mod in _activeModifications)
            {
                mod.Draw(camera);
            }
        }
        
        private void DrawChargingEffect(Camera3D camera)
        {
            if (!IsCharging) return;
            
            float chargeRatio = GetChargeRatio();
            Color chargeColor = ColorLerp(Color.Yellow, Color.Red, chargeRatio);
            chargeColor.A = (byte)(200 * chargeRatio);
            
            // Draw charging effect based on weapon type
            switch (WeaponType)
            {
                case AdvancedWeaponType.GaussRifle:
                    DrawElectricChargingEffect(camera, chargeColor, chargeRatio);
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    DrawQuantumChargingEffect(camera, chargeColor, chargeRatio);
                    break;
                    
                default:
                    DrawGenericChargingEffect(camera, chargeColor, chargeRatio);
                    break;
            }
        }
        
        private void DrawElectricChargingEffect(Camera3D camera, Color color, float intensity)
        {
            // Draw electric arcs
            // Implementation would depend on weapon position
        }
        
        private void DrawQuantumChargingEffect(Camera3D camera, Color color, float intensity)
        {
            // Draw quantum distortion field
            // Implementation would depend on weapon position
        }
        
        private void DrawGenericChargingEffect(Camera3D camera, Color color, float intensity)
        {
            // Draw generic energy buildup
            // Implementation would depend on weapon position
        }
        
        private void DrawTargetingEffect(Camera3D camera)
        {
            // Draw targeting reticle or line
            // Implementation would depend on target position
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
    
    /// <summary>
    /// Weapon modification system
    /// </summary>
    public class WeaponModification
    {
        public ModificationType Type { get; set; }
        public float Strength { get; set; }
        public float Duration { get; set; }
        public bool IsActive { get; private set; }
        
        private float _remainingTime;
        
        public WeaponModification(ModificationType type, float strength, float duration)
        {
            Type = type;
            Strength = strength;
            Duration = duration;
            _remainingTime = duration;
            IsActive = true;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            _remainingTime -= deltaTime;
            if (_remainingTime <= 0)
            {
                IsActive = false;
            }
        }
        
        public float ApplyDamageModification(float baseDamage)
        {
            return Type == ModificationType.DamageBoost ? baseDamage * Strength : baseDamage;
        }
        
        public void Draw(Camera3D camera)
        {
            // Draw modification visual effect
        }
    }
    
    /// <summary>
    /// Types of weapon modifications
    /// </summary>
    public enum ModificationType
    {
        DamageBoost,
        FireRateBoost,
        EnergyEfficiency,
        AccuracyBoost,
        RangeExtension,
        ElementalInfusion,
        PenetrationBoost,
        CriticalBoost,
        HomingEnhancement,
        SpeedBoost
    }
    
    /// <summary>
    /// Auto-targeting system for advanced weapons
    /// </summary>
    public class AutoTargetingSystem
    {
        public bool HasTarget { get; private set; }
        public Vector3 CurrentTarget { get; private set; }
        
        private float _strength;
        private float _duration;
        private float _remainingTime;
        private float _targetingRange;
        private float _lastScanTime;
        private List<Vector3> _potentialTargets;
        
        public AutoTargetingSystem()
        {
            _potentialTargets = new List<Vector3>();
            _targetingRange = 50f;
            _strength = 1f;
        }
        
        public void Update(float deltaTime, Vector3 playerPosition)
        {
            if (_remainingTime > 0)
            {
                _remainingTime -= deltaTime;
                if (_remainingTime <= 0)
                {
                    HasTarget = false;
                }
            }
            
            // Scan for targets periodically
            float currentTime = GetTime();
            if (currentTime - _lastScanTime >= 0.5f) // Scan every 0.5 seconds
            {
                ScanForTargets(playerPosition);
                _lastScanTime = currentTime;
            }
        }
        
        private void ScanForTargets(Vector3 playerPosition)
        {
            // This would interface with the game's enemy system
            // For now, we'll use placeholder logic
            
            if (_potentialTargets.Count > 0)
            {
                // Find nearest target
                Vector3 nearestTarget = _potentialTargets[0];
                float nearestDistance = Vector3.Distance(playerPosition, nearestTarget);
                
                foreach (var target in _potentialTargets)
                {
                    float distance = Vector3.Distance(playerPosition, target);
                    if (distance < nearestDistance && distance <= _targetingRange)
                    {
                        nearestTarget = target;
                        nearestDistance = distance;
                    }
                }
                
                if (nearestDistance <= _targetingRange)
                {
                    CurrentTarget = nearestTarget;
                    HasTarget = true;
                }
            }
        }
        
        public Vector3 GetTargetDirection(Vector3 fromPosition)
        {
            if (!HasTarget) return Vector3.UnitZ;
            
            return Vector3.Normalize(CurrentTarget - fromPosition);
        }
        
        public void SetStrength(float strength)
        {
            _strength = strength;
            _targetingRange = 50f * strength;
        }
        
        public void SetDuration(float duration)
        {
            _duration = duration;
            _remainingTime = duration;
        }
        
        public void AddPotentialTarget(Vector3 target)
        {
            _potentialTargets.Add(target);
        }
        
        public void RemovePotentialTarget(Vector3 target)
        {
            _potentialTargets.Remove(target);
        }
        
        public void ClearTargets()
        {
            _potentialTargets.Clear();
            HasTarget = false;
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
}