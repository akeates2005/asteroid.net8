using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Asteroids.PowerUps;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Enhanced integration system that connects advanced weapons with power-ups and game systems
    /// </summary>
    public class EnhancedWeaponIntegration
    {
        private Dictionary<WeaponType, WeaponSystem> _standardWeapons;
        private Dictionary<AdvancedWeaponType, AdvancedWeaponSystem> _advancedWeapons;
        private List<AdvancedProjectile3D> _activeAdvancedProjectiles;
        private BalancingSystem _balancingSystem;
        private SpecialAbilitiesSystem _specialAbilities;
        private StatusEffectManager _statusEffectManager;
        private WeaponSynergySystem _synergySystem;
        
        // Current equipped weapons
        private WeaponType? _currentStandardWeapon;
        private AdvancedWeaponType? _currentAdvancedWeapon;
        private WeaponMode _weaponMode;
        
        // Power-up integration
        private List<AdvancedPowerUpEffect> _activePowerUpEffects;
        private WeaponEvolutionTracker _evolutionTracker;
        
        // Events
        public event Action<AdvancedWeaponType> OnAdvancedWeaponUnlocked;
        public event Action<WeaponType, int> OnWeaponEvolved;
        public event Action<StatusEffectType[], Vector3> OnStatusEffectsApplied;
        public event Action<SynergyEffect> OnSynergyActivated;
        
        public EnhancedWeaponIntegration(BalancingSystem balancingSystem)
        {
            _standardWeapons = new Dictionary<WeaponType, WeaponSystem>();
            _advancedWeapons = new Dictionary<AdvancedWeaponType, AdvancedWeaponSystem>();
            _activeAdvancedProjectiles = new List<AdvancedProjectile3D>();
            _balancingSystem = balancingSystem;
            _specialAbilities = new SpecialAbilitiesSystem();
            _statusEffectManager = new StatusEffectManager();
            _synergySystem = new WeaponSynergySystem();
            _activePowerUpEffects = new List<AdvancedPowerUpEffect>();
            _evolutionTracker = new WeaponEvolutionTracker();
            _weaponMode = WeaponMode.Standard;
            
            InitializeWeaponSystems();
        }
        
        private void InitializeWeaponSystems()
        {
            // Initialize standard weapon system
            foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
            {
                var weaponSystem = new WeaponSystem();
                _standardWeapons[weaponType] = weaponSystem;
            }
            
            // Initialize advanced weapon systems
            foreach (AdvancedWeaponType weaponType in Enum.GetValues<AdvancedWeaponType>())
            {
                var advancedSystem = new AdvancedWeaponSystem(weaponType);
                _advancedWeapons[weaponType] = advancedSystem;
            }
        }
        
        public void Update(float deltaTime, Vector3 playerPosition, Vector3 playerVelocity)
        {
            // Update all systems
            _statusEffectManager.Update(deltaTime);
            _synergySystem.Update(deltaTime);
            _evolutionTracker.Update(deltaTime);
            
            // Update active power-up effects
            UpdatePowerUpEffects(deltaTime);
            
            // Update active weapons
            UpdateActiveWeapons(deltaTime, playerPosition, playerVelocity);
            
            // Update advanced projectiles
            UpdateAdvancedProjectiles(deltaTime);
            
            // Check for weapon evolution triggers
            CheckWeaponEvolution();
            
            // Process synergy effects
            ProcessSynergyEffects();
        }
        
        private void UpdatePowerUpEffects(float deltaTime)
        {
            for (int i = _activePowerUpEffects.Count - 1; i >= 0; i--)
            {
                _activePowerUpEffects[i].Update(deltaTime);
                if (!_activePowerUpEffects[i].IsActive)
                {
                    _activePowerUpEffects.RemoveAt(i);
                }
            }
        }
        
        private void UpdateActiveWeapons(float deltaTime, Vector3 position, Vector3 velocity)
        {
            switch (_weaponMode)
            {
                case WeaponMode.Standard:
                    if (_currentStandardWeapon.HasValue)
                    {
                        _standardWeapons[_currentStandardWeapon.Value].Update(deltaTime);
                    }
                    break;
                    
                case WeaponMode.Advanced:
                    if (_currentAdvancedWeapon.HasValue)
                    {
                        _advancedWeapons[_currentAdvancedWeapon.Value].Update(deltaTime, position, velocity);
                    }
                    break;
                    
                case WeaponMode.Hybrid:
                    // Update both systems when in hybrid mode
                    if (_currentStandardWeapon.HasValue)
                        _standardWeapons[_currentStandardWeapon.Value].Update(deltaTime);
                    if (_currentAdvancedWeapon.HasValue)
                        _advancedWeapons[_currentAdvancedWeapon.Value].Update(deltaTime, position, velocity);
                    break;
            }
        }
        
        private void UpdateAdvancedProjectiles(float deltaTime)
        {
            for (int i = _activeAdvancedProjectiles.Count - 1; i >= 0; i--)
            {
                _activeAdvancedProjectiles[i].Update(deltaTime);
                if (!_activeAdvancedProjectiles[i].IsActive)
                {
                    _activeAdvancedProjectiles.RemoveAt(i);
                }
            }
        }
        
        public bool TryFireWeapon(Vector3 position, Vector3 direction, Vector3 velocity, bool isPressed, bool wasPressed)
        {
            bool weaponFired = false;
            
            switch (_weaponMode)
            {
                case WeaponMode.Standard:
                    weaponFired = FireStandardWeapon(position, direction, velocity, isPressed, wasPressed);
                    break;
                    
                case WeaponMode.Advanced:
                    weaponFired = FireAdvancedWeapon(position, direction, velocity, isPressed, wasPressed);
                    break;
                    
                case WeaponMode.Hybrid:
                    // Fire both weapons with slight delay
                    bool standard = FireStandardWeapon(position, direction, velocity, isPressed, wasPressed);
                    bool advanced = FireAdvancedWeapon(position, direction + GetRandomSpread(0.1f), velocity, isPressed, wasPressed);
                    weaponFired = standard || advanced;
                    break;
            }
            
            if (weaponFired)
            {
                // Apply power-up modifications
                ApplyPowerUpModifications(position, direction);
                
                // Trigger evolution tracking
                _evolutionTracker.RecordWeaponUsage(_currentStandardWeapon, _currentAdvancedWeapon);
            }
            
            return weaponFired;
        }
        
        private bool FireStandardWeapon(Vector3 position, Vector3 direction, Vector3 velocity, bool isPressed, bool wasPressed)
        {
            if (!_currentStandardWeapon.HasValue) return false;
            
            var weaponSystem = _standardWeapons[_currentStandardWeapon.Value];
            return weaponSystem.TryFire(position, direction, velocity, isPressed, wasPressed);
        }
        
        private bool FireAdvancedWeapon(Vector3 position, Vector3 direction, Vector3 velocity, bool isPressed, bool wasPressed)
        {
            if (!_currentAdvancedWeapon.HasValue) return false;
            
            var advancedSystem = _advancedWeapons[_currentAdvancedWeapon.Value];
            var projectile = advancedSystem.TryFire(position, direction, velocity, isPressed, wasPressed);
            
            if (projectile != null)
            {
                _activeAdvancedProjectiles.Add(projectile);
                return true;
            }
            
            return false;
        }
        
        public void ApplyPowerUp(AdvancedPowerUpType powerUpType, AdvancedPowerUpConfig config)
        {
            switch (powerUpType)
            {
                case AdvancedPowerUpType.WeaponEvolution:
                    ApplyWeaponEvolution(config);
                    break;
                    
                case AdvancedPowerUpType.ElementalInfusion:
                    ApplyElementalInfusion(config);
                    break;
                    
                case AdvancedPowerUpType.ProjectileMultiplier:
                    ApplyProjectileMultiplier(config);
                    break;
                    
                case AdvancedPowerUpType.WeaponSynergy:
                    ApplyWeaponSynergy(config);
                    break;
                    
                case AdvancedPowerUpType.AdaptiveAiming:
                    ApplyAdaptiveAiming(config);
                    break;
                    
                case AdvancedPowerUpType.PowerAmplifier:
                    ApplyPowerAmplifier(config);
                    break;
                    
                default:
                    ApplyGenericPowerUp(powerUpType, config);
                    break;
            }
            
            // Add to active effects if it has duration
            if (config.Duration > 0 && !config.IsPermanent)
            {
                var effect = new AdvancedPowerUpEffect(powerUpType, config);
                _activePowerUpEffects.Add(effect);
            }
            
            // Check for synergies
            _synergySystem.CheckForSynergies(_activePowerUpEffects.Select(e => e.Type).ToArray());
        }
        
        private void ApplyWeaponEvolution(AdvancedPowerUpConfig config)
        {
            _evolutionTracker.EnableEvolution();
            
            // Immediately evolve current weapon if possible
            if (_currentStandardWeapon.HasValue)
            {
                _evolutionTracker.TriggerEvolution(_currentStandardWeapon.Value);
            }
            if (_currentAdvancedWeapon.HasValue)
            {
                _evolutionTracker.TriggerAdvancedEvolution(_currentAdvancedWeapon.Value);
            }
        }
        
        private void ApplyElementalInfusion(AdvancedPowerUpConfig config)
        {
            // Add random elemental damage to all weapons
            var elements = Enum.GetValues<ElementalType>().Where(e => e != ElementalType.Kinetic).ToArray();
            var randomElement = elements[new Random().Next(elements.Length)];
            
            var infusion = new ElementalInfusion
            {
                ElementType = randomElement,
                Damage = config.EffectStrength * 20f,
                Duration = config.Duration,
                VisualEffect = GetElementalEffect(randomElement)
            };
            
            ApplyElementalInfusionToWeapons(infusion);
        }
        
        private void ApplyProjectileMultiplier(AdvancedPowerUpConfig config)
        {
            int multiplier = (int)config.EffectStrength;
            
            // Apply to current weapons
            if (_currentStandardWeapon.HasValue)
            {
                var weapon = _standardWeapons[_currentStandardWeapon.Value];
                // Modify weapon to fire multiple projectiles
            }
            
            if (_currentAdvancedWeapon.HasValue)
            {
                var weapon = _advancedWeapons[_currentAdvancedWeapon.Value];
                weapon.SetProjectileMultiplier(multiplier, config.Duration);
            }
        }
        
        private void ApplyWeaponSynergy(AdvancedPowerUpConfig config)
        {
            if (_currentStandardWeapon.HasValue && _currentAdvancedWeapon.HasValue)
            {
                // Create synergy between current weapons
                var synergy = _synergySystem.CreateWeaponSynergy(
                    _currentStandardWeapon.Value,
                    _currentAdvancedWeapon.Value,
                    config.EffectStrength
                );
                
                OnSynergyActivated?.Invoke(synergy);
            }
            else
            {
                // Enable hybrid mode if only one weapon is equipped
                _weaponMode = WeaponMode.Hybrid;
                UnlockRandomAdvancedWeapon();
            }
        }
        
        private void ApplyAdaptiveAiming(AdvancedPowerUpConfig config)
        {
            // Enable auto-targeting for all weapons
            foreach (var weapon in _advancedWeapons.Values)
            {
                weapon.EnableAutoTargeting(config.EffectStrength, config.Duration);
            }
        }
        
        private void ApplyPowerAmplifier(AdvancedPowerUpConfig config)
        {
            // Amplify all existing power-up effects
            foreach (var effect in _activePowerUpEffects)
            {
                effect.Amplify(config.EffectStrength);
            }
        }
        
        private void ApplyGenericPowerUp(AdvancedPowerUpType type, AdvancedPowerUpConfig config)
        {
            // Handle generic power-up effects
            var statusEffects = config.StatusEffectsGranted;
            if (statusEffects?.Length > 0)
            {
                foreach (var statusEffect in statusEffects)
                {
                    _statusEffectManager.ApplyStatusEffect(
                        new StatusEffect(statusEffect, config.Duration, config.EffectStrength, Vector3.Zero)
                    );
                }
            }
        }
        
        private void ApplyPowerUpModifications(Vector3 position, Vector3 direction)
        {
            // Apply active power-up modifications to weapon fire
            foreach (var effect in _activePowerUpEffects)
            {
                switch (effect.Type)
                {
                    case AdvancedPowerUpType.ElementalInfusion:
                        CreateElementalEffect(position, direction, effect.Config.EffectStrength);
                        break;
                        
                    case AdvancedPowerUpType.HolographicDecoys:
                        CreateHolographicDecoys(position, (int)effect.Config.EffectStrength);
                        break;
                        
                    case AdvancedPowerUpType.NaniteIntegration:
                        EnhanceProjectilesWithNanites();
                        break;
                }
            }
        }
        
        private void CheckWeaponEvolution()
        {
            var evolutionResults = _evolutionTracker.CheckEvolutionTriggers();
            
            foreach (var result in evolutionResults)
            {
                if (result.WeaponType.HasValue)
                {
                    OnWeaponEvolved?.Invoke(result.WeaponType.Value, result.NewLevel);
                }
                else if (result.AdvancedWeaponType.HasValue)
                {
                    // Handle advanced weapon evolution
                    EvolveAdvancedWeapon(result.AdvancedWeaponType.Value, result.NewLevel);
                }
            }
        }
        
        private void ProcessSynergyEffects()
        {
            var synergyEffects = _synergySystem.GetActiveSynergies();
            
            foreach (var synergy in synergyEffects)
            {
                ApplySynergyEffect(synergy);
            }
        }
        
        private void ApplySynergyEffect(SynergyEffect synergy)
        {
            switch (synergy.Type)
            {
                case SynergyType.DamageAmplification:
                    AmplifyWeaponDamage(synergy.Strength);
                    break;
                    
                case SynergyType.ElementalFusion:
                    CreateElementalFusion(synergy.Elements);
                    break;
                    
                case SynergyType.ProjectileChaining:
                    EnableProjectileChaining(synergy.Strength);
                    break;
                    
                case SynergyType.EnergyResonance:
                    CreateEnergyResonance(synergy.Strength);
                    break;
            }
        }
        
        public void UnlockAdvancedWeapon(AdvancedWeaponType weaponType)
        {
            if (_advancedWeapons.ContainsKey(weaponType))
            {
                _advancedWeapons[weaponType].Unlock();
                OnAdvancedWeaponUnlocked?.Invoke(weaponType);
            }
        }
        
        public void SwitchWeapon(WeaponType standardWeapon)
        {
            _currentStandardWeapon = standardWeapon;
            
            if (_weaponMode == WeaponMode.Advanced)
            {
                _weaponMode = WeaponMode.Standard;
            }
        }
        
        public void SwitchAdvancedWeapon(AdvancedWeaponType advancedWeapon)
        {
            _currentAdvancedWeapon = advancedWeapon;
            _weaponMode = WeaponMode.Advanced;
        }
        
        public void ToggleWeaponMode()
        {
            _weaponMode = _weaponMode switch
            {
                WeaponMode.Standard => WeaponMode.Advanced,
                WeaponMode.Advanced => _currentStandardWeapon.HasValue ? WeaponMode.Hybrid : WeaponMode.Standard,
                WeaponMode.Hybrid => WeaponMode.Standard,
                _ => WeaponMode.Standard
            };
        }
        
        private void UnlockRandomAdvancedWeapon()
        {
            var lockedWeapons = _advancedWeapons.Where(w => !w.Value.IsUnlocked).ToArray();
            if (lockedWeapons.Length > 0)
            {
                var random = new Random();
                var randomWeapon = lockedWeapons[random.Next(lockedWeapons.Length)];
                UnlockAdvancedWeapon(randomWeapon.Key);
                _currentAdvancedWeapon = randomWeapon.Key;
            }
        }
        
        public void Draw(Camera3D camera)
        {
            // Draw all active advanced projectiles
            foreach (var projectile in _activeAdvancedProjectiles)
            {
                projectile.Draw(camera);
            }
            
            // Draw weapon effects
            if (_currentStandardWeapon.HasValue)
            {
                _standardWeapons[_currentStandardWeapon.Value].Draw(camera);
            }
            
            if (_currentAdvancedWeapon.HasValue)
            {
                _advancedWeapons[_currentAdvancedWeapon.Value].Draw(camera);
            }
            
            // Draw status effects
            _statusEffectManager.Draw(camera);
            
            // Draw synergy effects
            _synergySystem.Draw(camera);
        }
        
        // Collision detection for advanced projectiles
        public List<AdvancedDamageInfo> CheckCollisions(List<CollisionTarget> targets)
        {
            var damageInfos = new List<AdvancedDamageInfo>();
            
            foreach (var projectile in _activeAdvancedProjectiles.Where(p => p.IsActive))
            {
                foreach (var target in targets)
                {
                    if (projectile.CheckCollision(target.Position, target.Radius))
                    {
                        var damageInfo = projectile.CalculateDamageInfo(target.Position);
                        projectile.OnHitTarget(target.Position);
                        damageInfos.Add(damageInfo);
                        
                        // Apply status effects
                        if (damageInfo.StatusEffects?.Length > 0)
                        {
                            OnStatusEffectsApplied?.Invoke(damageInfo.StatusEffects, target.Position);
                        }
                    }
                }
            }
            
            return damageInfos;
        }
        
        // Helper methods for specific effects
        private Vector3 GetRandomSpread(float maxSpread)
        {
            var random = new Random();
            return new Vector3(
                (float)(random.NextDouble() - 0.5) * maxSpread,
                (float)(random.NextDouble() - 0.5) * maxSpread,
                (float)(random.NextDouble() - 0.5) * maxSpread
            );
        }
        
        private ParticleEffectType GetElementalEffect(ElementalType elementType)
        {
            return elementType switch
            {
                ElementalType.Fire => ParticleEffectType.FireBurst,
                ElementalType.Ice => ParticleEffectType.IceCrystals,
                ElementalType.Electromagnetic => ParticleEffectType.ElectricSparks,
                ElementalType.Quantum => ParticleEffectType.QuantumDistortion,
                ElementalType.Plasma => ParticleEffectType.PlasmaTrail,
                _ => ParticleEffectType.EnergyRipples
            };
        }
        
        private void CreateElementalEffect(Vector3 position, Vector3 direction, float strength)
        {
            // Implementation for creating elemental effects
        }
        
        private void CreateHolographicDecoys(Vector3 position, int count)
        {
            // Implementation for creating holographic decoys
        }
        
        private void EnhanceProjectilesWithNanites()
        {
            // Implementation for nanite enhancement
        }
        
        private void EvolveAdvancedWeapon(AdvancedWeaponType weaponType, int newLevel)
        {
            _advancedWeapons[weaponType].EvolveWeapon(newLevel);
        }
        
        private void ApplyElementalInfusionToWeapons(ElementalInfusion infusion)
        {
            // Implementation for applying elemental infusion
        }
        
        private void AmplifyWeaponDamage(float amplification)
        {
            // Implementation for damage amplification
        }
        
        private void CreateElementalFusion(ElementalType[] elements)
        {
            // Implementation for elemental fusion
        }
        
        private void EnableProjectileChaining(float strength)
        {
            // Implementation for projectile chaining
        }
        
        private void CreateEnergyResonance(float strength)
        {
            // Implementation for energy resonance
        }
        
        // Getters
        public WeaponType? CurrentStandardWeapon => _currentStandardWeapon;
        public AdvancedWeaponType? CurrentAdvancedWeapon => _currentAdvancedWeapon;
        public WeaponMode WeaponMode => _weaponMode;
        public List<AdvancedProjectile3D> ActiveAdvancedProjectiles => _activeAdvancedProjectiles;
        public StatusEffectManager StatusEffectManager => _statusEffectManager;
        public WeaponSynergySystem SynergySystem => _synergySystem;
    }
    
    /// <summary>
    /// Weapon operation modes
    /// </summary>
    public enum WeaponMode
    {
        Standard,   // Only standard weapons
        Advanced,   // Only advanced weapons
        Hybrid      // Both weapon types simultaneously
    }
    
    /// <summary>
    /// Collision target for projectile collision detection
    /// </summary>
    public struct CollisionTarget
    {
        public Vector3 Position;
        public float Radius;
        public int EntityId;
        public bool IsEnemy;
        public bool HasShield;
    }
    
    /// <summary>
    /// Elemental infusion data
    /// </summary>
    public struct ElementalInfusion
    {
        public ElementalType ElementType;
        public float Damage;
        public float Duration;
        public ParticleEffectType VisualEffect;
    }
}