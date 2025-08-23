using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using Asteroids.PowerUps;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Integrates weapon systems with the existing 3D game architecture
    /// </summary>
    public class WeaponIntegration
    {
        private WeaponSystem _weaponSystem;
        private SpecialAbilitiesSystem _specialAbilities;
        private PowerUpSystem _powerUpSystem;
        
        // Player input state
        private bool _firePressed;
        private bool _fireWasPressed;
        private bool _reloadPressed;
        private Dictionary<SpecialAbilityType, bool> _abilityPressed;
        private Dictionary<SpecialAbilityType, bool> _abilityWasPressed;
        
        // Weapon switching
        private List<WeaponType> _unlockedWeapons;
        private int _currentWeaponIndex;
        
        // Energy and ammo management
        private float _energy;
        private float _maxEnergy;
        private float _energyRegenRate;
        
        // Audio integration
        private Dictionary<WeaponType, Sound> _weaponSounds;
        private Dictionary<SpecialAbilityType, Sound> _abilitySounds;
        
        // Visual effects
        private List<WeaponEffect> _activeEffects;
        
        // Events for game system integration
        public event Action<float> OnEnergyChanged;
        public event Action<int, int> OnAmmoChanged;
        public event Action<WeaponType> OnWeaponChanged;
        public event Action<SpecialAbilityType> OnSpecialAbilityActivated;
        public event Action<Vector3, float, DamageType> OnDamageDealt;

        public WeaponIntegration(float maxEnergy = 1000f)
        {
            _weaponSystem = new WeaponSystem(maxEnergy);
            _specialAbilities = new SpecialAbilitiesSystem();
            _powerUpSystem = new PowerUpSystem();
            
            _maxEnergy = maxEnergy;
            _energy = maxEnergy;
            _energyRegenRate = 50f; // Energy per second
            
            _unlockedWeapons = new List<WeaponType> { WeaponType.Standard };
            _currentWeaponIndex = 0;
            
            _abilityPressed = new Dictionary<SpecialAbilityType, bool>();
            _abilityWasPressed = new Dictionary<SpecialAbilityType, bool>();
            _activeEffects = new List<WeaponEffect>();
            
            InitializeAbilityInputTracking();
            SetupEventHandlers();
            LoadAudioResources();
        }

        private void InitializeAbilityInputTracking()
        {
            foreach (SpecialAbilityType type in Enum.GetValues<SpecialAbilityType>())
            {
                _abilityPressed[type] = false;
                _abilityWasPressed[type] = false;
            }
        }

        private void SetupEventHandlers()
        {
            // Weapon system events
            _weaponSystem.OnWeaponChanged += (weaponType) => {
                OnWeaponChanged?.Invoke(weaponType);
                PlayWeaponSwitchSound(weaponType);
            };
            
            _weaponSystem.OnEnergyChanged += (energyPercent) => {
                _energy = energyPercent * _maxEnergy;
                OnEnergyChanged?.Invoke(energyPercent);
            };
            
            _weaponSystem.OnAmmoChanged += OnAmmoChanged;
            _weaponSystem.OnWeaponFired += OnWeaponFired;

            // Special abilities events
            _specialAbilities.OnAbilityActivated += (abilityType) => {
                OnSpecialAbilityActivated?.Invoke(abilityType);
                PlayAbilitySound(abilityType);
                CreateAbilityEffect(abilityType);
            };

            // Power-up system events
            _powerUpSystem.OnPowerUpCollected += OnPowerUpCollected;
            _powerUpSystem.OnWeaponUnlocked += OnWeaponUnlocked;
        }

        private void LoadAudioResources()
        {
            _weaponSounds = new Dictionary<WeaponType, Sound>();
            _abilitySounds = new Dictionary<SpecialAbilityType, Sound>();
            
            // Load weapon sounds (placeholder - actual audio files would be loaded here)
            foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
            {
                // _weaponSounds[weaponType] = Raylib.LoadSound($"sounds/weapons/{weaponType.ToString().ToLower()}.wav");
            }
            
            foreach (SpecialAbilityType abilityType in Enum.GetValues<SpecialAbilityType>())
            {
                // _abilitySounds[abilityType] = Raylib.LoadSound($"sounds/abilities/{abilityType.ToString().ToLower()}.wav");
            }
        }

        public void Update(float deltaTime, Vector3 playerPosition, Vector3 playerForward, Vector3 playerVelocity)
        {
            UpdateInput();
            UpdateWeaponSystem(deltaTime, playerPosition, playerForward, playerVelocity);
            UpdateSpecialAbilities(deltaTime, playerPosition);
            UpdatePowerUpSystem(deltaTime, playerPosition);
            UpdateEnergyRegeneration(deltaTime);
            UpdateVisualEffects(deltaTime);
            UpdateInputHistory();
        }

        private void UpdateInput()
        {
            // Store previous input state
            _fireWasPressed = _firePressed;
            foreach (var key in _abilityWasPressed.Keys.ToList())
            {
                _abilityWasPressed[key] = _abilityPressed[key];
            }

            // Update current input state
            _firePressed = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsMouseButtonDown(MouseButton.Left);
            _reloadPressed = Raylib.IsKeyPressed(KeyboardKey.R);

            // Weapon switching
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                SwitchToNextWeapon();
            }

            // Number keys for direct weapon selection
            for (int i = 1; i <= 9; i++)
            {
                if (Raylib.IsKeyPressed((KeyboardKey)(KeyboardKey.One + i - 1)))
                {
                    SwitchToWeapon(i - 1);
                }
            }

            // Special abilities input
            _abilityPressed[SpecialAbilityType.MagneticField] = Raylib.IsKeyDown(KeyboardKey.F);
            _abilityPressed[SpecialAbilityType.TimeDilation] = Raylib.IsKeyDown(KeyboardKey.T);
            _abilityPressed[SpecialAbilityType.ShieldOvercharge] = Raylib.IsKeyDown(KeyboardKey.G);
            _abilityPressed[SpecialAbilityType.Cloaking] = Raylib.IsKeyDown(KeyboardKey.C);
            _abilityPressed[SpecialAbilityType.EnergyOverload] = Raylib.IsKeyDown(KeyboardKey.V);
            _abilityPressed[SpecialAbilityType.TeleportStrike] = Raylib.IsKeyDown(KeyboardKey.B);
            _abilityPressed[SpecialAbilityType.GravityWell] = Raylib.IsKeyDown(KeyboardKey.N);
            _abilityPressed[SpecialAbilityType.PhaseShift] = Raylib.IsKeyDown(KeyboardKey.H);
            _abilityPressed[SpecialAbilityType.Berserker] = Raylib.IsKeyDown(KeyboardKey.J);
            _abilityPressed[SpecialAbilityType.TimeFreeze] = Raylib.IsKeyDown(KeyboardKey.K);
        }

        private void UpdateInputHistory()
        {
            // This is called at the end of Update to store the current frame's input for next frame
        }

        private void UpdateWeaponSystem(float deltaTime, Vector3 playerPosition, Vector3 playerForward, Vector3 playerVelocity)
        {
            // Handle reload
            if (_reloadPressed)
            {
                _weaponSystem.StartReload();
            }

            // Handle firing
            _weaponSystem.TryFire(playerPosition, playerForward, playerVelocity, _firePressed, _fireWasPressed);

            // Update weapon system
            _weaponSystem.Update(deltaTime);
        }

        private void UpdateSpecialAbilities(float deltaTime, Vector3 playerPosition)
        {
            // Try to activate abilities
            foreach (var abilityType in _abilityPressed.Keys)
            {
                if (_abilityPressed[abilityType] && !_abilityWasPressed[abilityType])
                {
                    Vector3? targetPosition = null;
                    
                    // Handle targeting for abilities that require it
                    if (_specialAbilities.GetAbility(abilityType).RequiresTargeting)
                    {
                        targetPosition = GetTargetPosition(playerPosition);
                    }

                    _specialAbilities.TryActivateAbility(abilityType, playerPosition, targetPosition, _energy);
                }
            }

            _specialAbilities.Update(deltaTime);
        }

        private Vector3 GetTargetPosition(Vector3 playerPosition)
        {
            // For now, target a position in front of the player
            // In a full implementation, this could use mouse cursor or gamepad targeting
            Vector2 mousePosition = Raylib.GetMousePosition();
            
            // Convert mouse position to world position (simplified)
            Vector3 targetOffset = new Vector3(
                (mousePosition.X - Raylib.GetScreenWidth() / 2f) * 0.1f,
                0f,
                (mousePosition.Y - Raylib.GetScreenHeight() / 2f) * 0.1f
            );
            
            return playerPosition + targetOffset;
        }

        private void UpdatePowerUpSystem(float deltaTime, Vector3 playerPosition)
        {
            _powerUpSystem.Update(deltaTime, playerPosition);
            
            // Apply magnetic field effect if active
            if (_specialAbilities.IsAbilityActive(SpecialAbilityType.MagneticField))
            {
                float magneticStrength = _specialAbilities.GetAbility(SpecialAbilityType.MagneticField).EffectStrength;
                _powerUpSystem.SetMagneticField(true, magneticStrength);
            }
            else
            {
                _powerUpSystem.SetMagneticField(false);
            }
        }

        private void UpdateEnergyRegeneration(float deltaTime)
        {
            // Apply energy regeneration
            float regenAmount = _energyRegenRate * deltaTime;
            
            // Apply power-up multipliers
            if (_powerUpSystem.HasActiveEffect(PowerUpType.EnergyEfficiency))
            {
                regenAmount *= _powerUpSystem.GetEffectMultiplier(PowerUpType.EnergyEfficiency);
            }
            
            if (_specialAbilities.IsAbilityActive(SpecialAbilityType.EnergyOverload))
            {
                regenAmount *= _specialAbilities.GetAbility(SpecialAbilityType.EnergyOverload).EffectStrength;
            }

            _energy = Math.Min(_maxEnergy, _energy + regenAmount);
            OnEnergyChanged?.Invoke(_energy / _maxEnergy);
        }

        private void UpdateVisualEffects(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].Update(deltaTime);
                if (!_activeEffects[i].IsActive)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        private void SwitchToNextWeapon()
        {
            if (_unlockedWeapons.Count <= 1) return;
            
            _currentWeaponIndex = (_currentWeaponIndex + 1) % _unlockedWeapons.Count;
            _weaponSystem.SetWeapon(_unlockedWeapons[_currentWeaponIndex]);
        }

        private void SwitchToWeapon(int index)
        {
            if (index < 0 || index >= _unlockedWeapons.Count) return;
            
            _currentWeaponIndex = index;
            _weaponSystem.SetWeapon(_unlockedWeapons[_currentWeaponIndex]);
        }

        private void OnWeaponFired(Vector3 position, WeaponType weaponType)
        {
            // Consume energy
            float energyCost = _weaponSystem.CurrentWeapon.EnergyConsumption;
            
            // Apply power-up modifiers
            if (_powerUpSystem.HasActiveEffect(PowerUpType.EnergyEfficiency))
            {
                energyCost *= (2f - _powerUpSystem.GetEffectMultiplier(PowerUpType.EnergyEfficiency));
            }
            
            _energy = Math.Max(0, _energy - energyCost);
            OnEnergyChanged?.Invoke(_energy / _maxEnergy);

            // Play weapon sound
            PlayWeaponSound(weaponType);
            
            // Create muzzle flash effect
            CreateMuzzleFlashEffect(position, weaponType);
        }

        private void OnPowerUpCollected(PowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case PowerUpType.EnergyRecharge:
                    _energy = _maxEnergy;
                    OnEnergyChanged?.Invoke(1f);
                    break;
                    
                case PowerUpType.AmmoRefill:
                    // This would refill ammo for the current weapon
                    break;
                    
                case PowerUpType.WeaponUpgrade:
                    _weaponSystem.UpgradeWeapon(_weaponSystem.CurrentWeaponType);
                    break;
            }
        }

        private void OnWeaponUnlocked(PowerUpType powerUpType)
        {
            WeaponType weaponType = powerUpType switch
            {
                PowerUpType.PlasmaWeapon => WeaponType.Plasma,
                PowerUpType.LaserWeapon => WeaponType.Laser,
                PowerUpType.MissileWeapon => WeaponType.Missile,
                PowerUpType.ShotgunWeapon => WeaponType.Shotgun,
                PowerUpType.BeamWeapon => WeaponType.Beam,
                PowerUpType.LightningWeapon => WeaponType.Lightning,
                _ => WeaponType.Standard
            };

            if (!_unlockedWeapons.Contains(weaponType))
            {
                _unlockedWeapons.Add(weaponType);
            }
        }

        private void PlayWeaponSound(WeaponType weaponType)
        {
            if (_weaponSounds.ContainsKey(weaponType))
            {
                // Raylib.PlaySound(_weaponSounds[weaponType]);
            }
        }

        private void PlayWeaponSwitchSound(WeaponType weaponType)
        {
            // Play weapon switch sound
        }

        private void PlayAbilitySound(SpecialAbilityType abilityType)
        {
            if (_abilitySounds.ContainsKey(abilityType))
            {
                // Raylib.PlaySound(_abilitySounds[abilityType]);
            }
        }

        private void CreateMuzzleFlashEffect(Vector3 position, WeaponType weaponType)
        {
            Color flashColor = weaponType switch
            {
                WeaponType.Plasma => Color.Cyan,
                WeaponType.Laser => Color.Red,
                WeaponType.Lightning => Color.White,
                _ => Color.Yellow
            };

            _activeEffects.Add(new WeaponEffect(position, flashColor, 0.1f, WeaponEffectType.MuzzleFlash));
        }

        private void CreateAbilityEffect(SpecialAbilityType abilityType)
        {
            // Create visual effects for ability activation
            _activeEffects.Add(new WeaponEffect(Vector3.Zero, Color.White, 1f, WeaponEffectType.AbilityActivation));
        }

        public void Draw(Camera3D camera, Vector3 playerPosition)
        {
            // Draw weapon projectiles
            _weaponSystem.Draw(camera);
            
            // Draw special ability effects
            _specialAbilities.Draw(camera, playerPosition);
            
            // Draw power-ups
            _powerUpSystem.Draw(camera);
            
            // Draw weapon effects
            foreach (var effect in _activeEffects)
            {
                effect.Draw(camera);
            }
        }

        // Public interface methods
        public WeaponType GetCurrentWeaponType() => _weaponSystem.CurrentWeaponType;
        public WeaponStats GetCurrentWeaponStats() => _weaponSystem.CurrentWeapon;
        public float GetEnergyPercentage() => _energy / _maxEnergy;
        public int GetCurrentAmmo() => _weaponSystem.CurrentAmmo;
        public int GetMaxAmmo() => _weaponSystem.MaxAmmo;
        public bool IsReloading() => _weaponSystem.IsReloading;
        public bool IsCharging() => _weaponSystem.IsCharging;
        public float GetChargeProgress() => _weaponSystem.ChargeProgress;
        public float GetReloadProgress() => _weaponSystem.ReloadProgress;
        public List<WeaponType> GetUnlockedWeapons() => new List<WeaponType>(_unlockedWeapons);
        public List<Projectile3D> GetActiveProjectiles() => _weaponSystem.ActiveProjectiles;
        public SpecialAbilitiesSystem GetSpecialAbilities() => _specialAbilities;
        public PowerUpSystem GetPowerUpSystem() => _powerUpSystem;

        // Damage calculation with power-up modifiers
        public float CalculateWeaponDamage(float baseDamage)
        {
            float finalDamage = baseDamage;
            
            // Apply power-up multipliers
            if (_powerUpSystem.HasActiveEffect(PowerUpType.ExplosiveShots))
            {
                finalDamage *= _powerUpSystem.GetEffectMultiplier(PowerUpType.ExplosiveShots);
            }
            
            if (_specialAbilities.IsAbilityActive(SpecialAbilityType.Berserker))
            {
                finalDamage *= _specialAbilities.GetAbility(SpecialAbilityType.Berserker).EffectStrength;
            }
            
            if (_powerUpSystem.HasActiveEffect(PowerUpType.Devastator))
            {
                finalDamage *= _powerUpSystem.GetEffectMultiplier(PowerUpType.Devastator);
            }
            
            return finalDamage;
        }

        // Check collision with projectiles
        public bool CheckProjectileCollisions(Vector3 targetPosition, float targetRadius, out List<DamageInfo> damageInfo)
        {
            damageInfo = new List<DamageInfo>();
            bool hitDetected = false;

            foreach (var projectile in _weaponSystem.ActiveProjectiles)
            {
                if (projectile.CheckCollision(targetPosition, targetRadius))
                {
                    var damage = projectile.CalculateDamageInfo(targetPosition);
                    damage.DirectDamage = CalculateWeaponDamage(damage.DirectDamage);
                    damageInfo.Add(damage);
                    
                    projectile.OnHitTarget();
                    hitDetected = true;
                    
                    OnDamageDealt?.Invoke(targetPosition, damage.DirectDamage + damage.SplashDamage, damage.DamageType);
                }
            }

            return hitDetected;
        }

        // Cleanup resources
        public void Dispose()
        {
            foreach (var sound in _weaponSounds.Values)
            {
                // Raylib.UnloadSound(sound);
            }
            
            foreach (var sound in _abilitySounds.Values)
            {
                // Raylib.UnloadSound(sound);
            }
        }
    }

    /// <summary>
    /// Visual effect for weapons and abilities
    /// </summary>
    public enum WeaponEffectType
    {
        MuzzleFlash,
        AbilityActivation,
        Explosion,
        Hit,
        Trail
    }

    public class WeaponEffect
    {
        public Vector3 Position { get; private set; }
        public Color Color { get; private set; }
        public bool IsActive { get; private set; }
        public WeaponEffectType Type { get; private set; }
        
        private float _duration;
        private float _remainingTime;
        private float _initialIntensity;

        public WeaponEffect(Vector3 position, Color color, float duration, WeaponEffectType type)
        {
            Position = position;
            Color = color;
            _duration = duration;
            _remainingTime = duration;
            Type = type;
            IsActive = true;
            _initialIntensity = 1f;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            _remainingTime -= deltaTime;
            if (_remainingTime <= 0)
            {
                IsActive = false;
                return;
            }

            // Fade out over time
            float progress = 1f - (_remainingTime / _duration);
            Color.A = (byte)(255 * (1f - progress));
        }

        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;

            switch (Type)
            {
                case WeaponEffectType.MuzzleFlash:
                    DrawMuzzleFlash(camera);
                    break;
                case WeaponEffectType.AbilityActivation:
                    DrawAbilityActivation(camera);
                    break;
                case WeaponEffectType.Explosion:
                    DrawExplosion(camera);
                    break;
            }
        }

        private void DrawMuzzleFlash(Camera3D camera)
        {
            float intensity = _remainingTime / _duration;
            Raylib.DrawSphere(Position, 0.5f * intensity, Color);
        }

        private void DrawAbilityActivation(Camera3D camera)
        {
            float radius = (1f - _remainingTime / _duration) * 5f;
            Raylib.DrawSphereWires(Position, radius, 8, 8, Color);
        }

        private void DrawExplosion(Camera3D camera)
        {
            float radius = (1f - _remainingTime / _duration) * 3f;
            Raylib.DrawSphere(Position, radius, Color);
        }
    }
}