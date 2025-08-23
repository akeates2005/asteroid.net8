using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Asteroids.Weapons;

namespace Asteroids.PowerUps
{
    /// <summary>
    /// Manages power-up spawning, collection, and active effects
    /// </summary>
    public class PowerUpSystem
    {
        private List<PowerUp3D> _activePowerUps;
        private List<ActivePowerUpEffect> _activeEffects;
        private Dictionary<PowerUpType, PowerUpConfig> _powerUpConfigs;
        private Dictionary<PowerUpType, float> _lastPickupTimes;
        private Random _random;
        
        // Spawning parameters
        private float _lastSpawnTime;
        private float _baseSpawnInterval = 8f; // Base spawn interval in seconds
        private float _spawnIntervalVariation = 4f; // ±4 seconds variation
        private int _maxActivePowerUps = 5;
        private Vector3 _worldBounds = new Vector3(100f, 50f, 100f);
        
        // Player interaction
        private Vector3 _playerPosition;
        private float _magneticFieldStrength = 1f;
        private bool _hasMagneticField = false;
        
        // Events
        public event Action<PowerUpType> OnPowerUpCollected;
        public event Action<PowerUpType, float> OnPowerUpEffectStarted;
        public event Action<PowerUpType> OnPowerUpEffectEnded;
        public event Action<PowerUpType> OnWeaponUnlocked;
        
        public PowerUpSystem()
        {
            _activePowerUps = new List<PowerUp3D>();
            _activeEffects = new List<ActivePowerUpEffect>();
            _powerUpConfigs = new Dictionary<PowerUpType, PowerUpConfig>();
            _lastPickupTimes = new Dictionary<PowerUpType, float>();
            _random = new Random();
            
            InitializePowerUpConfigs();
        }
        
        private void InitializePowerUpConfigs()
        {
            foreach (PowerUpType type in Enum.GetValues<PowerUpType>())
            {
                _powerUpConfigs[type] = PowerUpConfig.CreateDefault(type);
                _lastPickupTimes[type] = 0f;
            }
        }
        
        public void Update(float deltaTime, Vector3 playerPosition)
        {
            _playerPosition = playerPosition;
            
            UpdatePowerUpSpawning(deltaTime);
            UpdateActivePowerUps(deltaTime);
            UpdateActiveEffects(deltaTime);
            CheckPowerUpCollection();
        }
        
        private void UpdatePowerUpSpawning(float deltaTime)
        {
            float currentTime = GetTime();
            
            // Check if we should spawn a new power-up
            if (_activePowerUps.Count < _maxActivePowerUps && 
                currentTime - _lastSpawnTime >= GetNextSpawnInterval())
            {
                SpawnRandomPowerUp();
                _lastSpawnTime = currentTime;
            }
        }
        
        private float GetNextSpawnInterval()
        {
            float variation = (float)(_random.NextDouble() - 0.5) * _spawnIntervalVariation * 2f;
            return _baseSpawnInterval + variation;
        }
        
        private void SpawnRandomPowerUp()
        {
            PowerUpType selectedType = SelectRandomPowerUpType();
            Vector3 spawnPosition = GetRandomSpawnPosition();
            Vector3 spawnVelocity = GetRandomVelocity();
            
            var powerUp = new PowerUp3D(spawnPosition, selectedType, spawnVelocity);
            _activePowerUps.Add(powerUp);
        }
        
        private PowerUpType SelectRandomPowerUpType()
        {
            // Calculate total weight
            float totalWeight = 0f;
            var availableTypes = new List<(PowerUpType type, float weight)>();
            
            foreach (var kvp in _powerUpConfigs)
            {
                PowerUpType type = kvp.Key;
                PowerUpConfig config = kvp.Value;
                
                // Check cooldown
                float timeSinceLastPickup = GetTime() - _lastPickupTimes[type];
                if (timeSinceLastPickup < config.CooldownTime)
                    continue;
                
                // Apply rarity weighting
                float weight = config.SpawnWeight * GetRarityMultiplier(config.Rarity);
                availableTypes.Add((type, weight));
                totalWeight += weight;
            }
            
            if (availableTypes.Count == 0)
                return PowerUpType.EnergyRecharge; // Fallback
            
            // Select based on weighted probability
            float randomValue = (float)_random.NextDouble() * totalWeight;
            float currentWeight = 0f;
            
            foreach (var (type, weight) in availableTypes)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                    return type;
            }
            
            return availableTypes.Last().type; // Fallback
        }
        
        private float GetRarityMultiplier(PowerUpRarity rarity)
        {
            return rarity switch
            {
                PowerUpRarity.Common => 1f,
                PowerUpRarity.Uncommon => 0.4f,
                PowerUpRarity.Rare => 0.2f,
                PowerUpRarity.Epic => 0.04f,
                PowerUpRarity.Legendary => 0.008f,
                _ => 1f
            };
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            // Spawn outside player's immediate vicinity but within world bounds
            Vector3 offset;
            do
            {
                offset = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * _worldBounds.X,
                    (float)(_random.NextDouble() - 0.5) * _worldBounds.Y,
                    (float)(_random.NextDouble() - 0.5) * _worldBounds.Z
                );
            }
            while (Vector3.Distance(offset, _playerPosition) < 15f); // Minimum spawn distance
            
            return offset;
        }
        
        private Vector3 GetRandomVelocity()
        {
            return new Vector3(
                (float)(_random.NextDouble() - 0.5) * 2f,
                0f, // No vertical velocity
                (float)(_random.NextDouble() - 0.5) * 2f
            );
        }
        
        private void UpdateActivePowerUps(float deltaTime)
        {
            for (int i = _activePowerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = _activePowerUps[i];
                powerUp.Update(deltaTime);
                
                // Apply magnetic field if active
                if (_hasMagneticField)
                {
                    powerUp.ApplyMagneticForce(_playerPosition, _magneticFieldStrength);
                }
                
                // Remove inactive power-ups
                if (!powerUp.IsActive)
                {
                    _activePowerUps.RemoveAt(i);
                }
            }
        }
        
        private void UpdateActiveEffects(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);
                
                if (!effect.IsActive)
                {
                    OnPowerUpEffectEnded?.Invoke(effect.Type);
                    _activeEffects.RemoveAt(i);
                }
            }
        }
        
        private void CheckPowerUpCollection()
        {
            for (int i = _activePowerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = _activePowerUps[i];
                
                if (powerUp.CheckPlayerProximity(_playerPosition, out float distance))
                {
                    CollectPowerUp(powerUp);
                    _activePowerUps.RemoveAt(i);
                }
                else if (powerUp.CheckMagneticField(_playerPosition))
                {
                    powerUp.StartCollection(_playerPosition);
                }
            }
        }
        
        private void CollectPowerUp(PowerUp3D powerUp)
        {
            PowerUpType type = powerUp.Type;
            PowerUpConfig config = powerUp.Config;
            
            _lastPickupTimes[type] = GetTime();
            OnPowerUpCollected?.Invoke(type);
            
            // Apply immediate effects
            ApplyPowerUpEffect(type, config);
            
            // Add timed effect if applicable
            if (config.Duration > 0 && !config.IsPermanent)
            {
                AddTimedEffect(type, config);
            }
        }
        
        private void ApplyPowerUpEffect(PowerUpType type, PowerUpConfig config)
        {
            switch (type)
            {
                // Instant effects
                case PowerUpType.EnergyRecharge:
                case PowerUpType.AmmoRefill:
                case PowerUpType.ShieldBoost:
                case PowerUpType.BonusPoints:
                case PowerUpType.ExtraLife:
                case PowerUpType.NuclearDevice:
                    ApplyInstantEffect(type, config);
                    break;
                
                // Permanent upgrades
                case PowerUpType.WeaponUpgrade:
                case PowerUpType.ShieldExtender:
                case PowerUpType.ThrustUpgrade:
                    ApplyPermanentUpgrade(type, config);
                    break;
                
                // Weapon unlocks
                case PowerUpType.PlasmaWeapon:
                case PowerUpType.LaserWeapon:
                case PowerUpType.MissileWeapon:
                case PowerUpType.ShotgunWeapon:
                case PowerUpType.BeamWeapon:
                case PowerUpType.LightningWeapon:
                    UnlockWeapon(type);
                    break;
            }
        }
        
        private void ApplyInstantEffect(PowerUpType type, PowerUpConfig config)
        {
            // These effects are handled by the game systems that use this PowerUpSystem
            // The events notify those systems to apply the effects
            OnPowerUpEffectStarted?.Invoke(type, config.EffectStrength);
        }
        
        private void ApplyPermanentUpgrade(PowerUpType type, PowerUpConfig config)
        {
            // Find existing permanent effect and stack it
            var existingEffect = _activeEffects.FirstOrDefault(e => e.Type == type && e.IsPermanent);
            
            if (existingEffect != null && config.IsStackable && existingEffect.StackCount < config.MaxStacks)
            {
                existingEffect.AddStack();
            }
            else if (existingEffect == null)
            {
                var effect = new ActivePowerUpEffect(type, config, true);
                _activeEffects.Add(effect);
            }
            
            OnPowerUpEffectStarted?.Invoke(type, config.EffectStrength);
        }
        
        private void UnlockWeapon(PowerUpType type)
        {
            OnWeaponUnlocked?.Invoke(type);
        }
        
        private void AddTimedEffect(PowerUpType type, PowerUpConfig config)
        {
            // Check if effect can be stacked
            var existingEffect = _activeEffects.FirstOrDefault(e => e.Type == type);
            
            if (existingEffect != null && config.IsStackable && existingEffect.StackCount < config.MaxStacks)
            {
                existingEffect.AddStack();
                existingEffect.RefreshDuration();
            }
            else if (existingEffect != null && !config.IsStackable)
            {
                existingEffect.RefreshDuration();
            }
            else
            {
                var effect = new ActivePowerUpEffect(type, config, false);
                _activeEffects.Add(effect);
            }
            
            OnPowerUpEffectStarted?.Invoke(type, config.EffectStrength);
        }
        
        public void Draw(Camera3D camera)
        {
            foreach (var powerUp in _activePowerUps)
            {
                powerUp.Draw(camera);
            }
        }
        
        // Forced spawn for testing or special events
        public void SpawnPowerUp(PowerUpType type, Vector3 position, Vector3? velocity = null)
        {
            var powerUp = new PowerUp3D(position, type, velocity);
            _activePowerUps.Add(powerUp);
        }
        
        // Set magnetic field effect
        public void SetMagneticField(bool active, float strength = 1f)
        {
            _hasMagneticField = active;
            _magneticFieldStrength = strength;
        }
        
        // Get active effect multipliers for other systems
        public float GetEffectMultiplier(PowerUpType type)
        {
            var effect = _activeEffects.FirstOrDefault(e => e.Type == type);
            if (effect == null) return 1f;
            
            float multiplier = effect.Config.EffectStrength;
            if (effect.Config.IsStackable && effect.StackCount > 1)
            {
                multiplier = 1f + (multiplier - 1f) * effect.StackCount;
            }
            
            return multiplier;
        }
        
        public bool HasActiveEffect(PowerUpType type)
        {
            return _activeEffects.Any(e => e.Type == type);
        }
        
        public List<ActivePowerUpEffect> GetActiveEffects()
        {
            return new List<ActivePowerUpEffect>(_activeEffects);
        }
        
        public int GetActivePowerUpCount() => _activePowerUps.Count;
        public int GetActiveEffectCount() => _activeEffects.Count;
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    /// <summary>
    /// Represents an active power-up effect with duration and stacking
    /// </summary>
    public class ActivePowerUpEffect
    {
        public PowerUpType Type { get; private set; }
        public PowerUpConfig Config { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPermanent { get; private set; }
        public float RemainingDuration { get; private set; }
        public int StackCount { get; private set; }
        
        private float _originalDuration;
        
        public ActivePowerUpEffect(PowerUpType type, PowerUpConfig config, bool isPermanent)
        {
            Type = type;
            Config = config;
            IsPermanent = isPermanent;
            IsActive = true;
            RemainingDuration = config.Duration;
            _originalDuration = config.Duration;
            StackCount = 1;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsActive || IsPermanent) return;
            
            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0)
            {
                IsActive = false;
            }
        }
        
        public void AddStack()
        {
            if (Config.IsStackable && StackCount < Config.MaxStacks)
            {
                StackCount++;
            }
        }
        
        public void RefreshDuration()
        {
            RemainingDuration = _originalDuration;
        }
        
        public float GetProgressPercentage()
        {
            if (IsPermanent) return 1f;
            return RemainingDuration / _originalDuration;
        }
    }
}