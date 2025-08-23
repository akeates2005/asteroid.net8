using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Main weapon system that manages all weapon types and firing mechanics
    /// </summary>
    public class WeaponSystem
    {
        private Dictionary<WeaponType, WeaponStats> _weaponConfigs;
        private WeaponType _currentWeaponType;
        private WeaponStats _currentWeapon;
        private float _lastFireTime;
        private float _chargeStartTime;
        private bool _isCharging;
        private int _currentAmmo;
        private float _reloadStartTime;
        private bool _isReloading;
        private float _energy;
        private float _maxEnergy;
        private List<Projectile3D> _activeProjectiles;
        private Random _random;
        private int _burstShotsFired;
        private float _lastBurstShotTime;
        private bool _isBurstActive;

        // Upgrade system
        private Dictionary<WeaponType, int> _weaponLevels;
        private const int MaxWeaponLevel = 5;

        // Events for UI and effects
        public event Action<WeaponType> OnWeaponChanged;
        public event Action<float> OnEnergyChanged;
        public event Action<int, int> OnAmmoChanged;
        public event Action<WeaponType> OnWeaponUpgraded;
        public event Action<Vector3, WeaponType> OnWeaponFired;

        public WeaponSystem(float maxEnergy = 1000f)
        {
            _weaponConfigs = new Dictionary<WeaponType, WeaponStats>();
            _weaponLevels = new Dictionary<WeaponType, int>();
            _activeProjectiles = new List<Projectile3D>();
            _random = new Random();
            _maxEnergy = maxEnergy;
            _energy = maxEnergy;

            InitializeWeapons();
            SetWeapon(WeaponType.Standard);
        }

        private void InitializeWeapons()
        {
            foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
            {
                _weaponConfigs[weaponType] = WeaponStats.CreateDefault(weaponType);
                _weaponLevels[weaponType] = 1;
            }
        }

        public void SetWeapon(WeaponType weaponType)
        {
            if (_currentWeaponType == weaponType) return;

            _currentWeaponType = weaponType;
            _currentWeapon = _weaponConfigs[weaponType];
            
            // Apply weapon level upgrades
            ApplyWeaponUpgrades();
            
            _currentAmmo = _currentWeapon.AmmoCapacity;
            _isCharging = false;
            _isReloading = false;
            _isBurstActive = false;
            _burstShotsFired = 0;

            OnWeaponChanged?.Invoke(weaponType);
            OnAmmoChanged?.Invoke(_currentAmmo, _currentWeapon.AmmoCapacity);
        }

        private void ApplyWeaponUpgrades()
        {
            int level = _weaponLevels[_currentWeaponType];
            float levelMultiplier = 1f + (level - 1) * 0.2f; // 20% increase per level

            var upgraded = _currentWeapon;
            upgraded.Damage *= levelMultiplier;
            upgraded.FireRate *= (1f + (level - 1) * 0.15f); // 15% fire rate increase
            upgraded.Range *= (1f + (level - 1) * 0.1f); // 10% range increase
            upgraded.Accuracy = Math.Min(1f, upgraded.Accuracy + (level - 1) * 0.02f); // 2% accuracy increase
            upgraded.CriticalChance = Math.Min(0.5f, upgraded.CriticalChance + (level - 1) * 0.02f);

            _currentWeapon = upgraded;
        }

        public bool CanFire()
        {
            float currentTime = GetTime();
            
            if (_isReloading) return false;
            if (_energy < _currentWeapon.EnergyConsumption) return false;
            if (_currentWeapon.AmmoCapacity > 0 && _currentAmmo <= 0) return false;
            
            float timeSinceLastShot = currentTime - _lastFireTime;
            float fireInterval = 1f / _currentWeapon.FireRate;
            
            return timeSinceLastShot >= fireInterval;
        }

        public bool TryFire(Vector3 position, Vector3 direction, Vector3 velocity, bool isPressed, bool wasPressed)
        {
            if (!CanFire()) return false;

            // Handle charging weapons
            if (_currentWeapon.RequiresCharging)
            {
                if (isPressed && !wasPressed)
                {
                    _chargeStartTime = GetTime();
                    _isCharging = true;
                    return false;
                }
                
                if (_isCharging && !isPressed)
                {
                    float chargeTime = GetTime() - _chargeStartTime;
                    if (chargeTime >= _currentWeapon.ChargeTime)
                    {
                        _isCharging = false;
                        return FireWeapon(position, direction, velocity, chargeTime / _currentWeapon.ChargeTime);
                    }
                    _isCharging = false;
                    return false;
                }
                
                return false;
            }

            // Handle burst fire
            if (_currentWeapon.Pattern == FiringPattern.Burst)
            {
                if (!_isBurstActive && isPressed && !wasPressed)
                {
                    _isBurstActive = true;
                    _burstShotsFired = 0;
                    _lastBurstShotTime = GetTime();
                }

                if (_isBurstActive)
                {
                    float currentTime = GetTime();
                    if (currentTime - _lastBurstShotTime >= 0.1f) // 100ms between burst shots
                    {
                        bool fired = FireWeapon(position, direction, velocity, 1f);
                        if (fired)
                        {
                            _burstShotsFired++;
                            _lastBurstShotTime = currentTime;
                            
                            if (_burstShotsFired >= _currentWeapon.ProjectileCount)
                            {
                                _isBurstActive = false;
                                return true;
                            }
                        }
                        return fired;
                    }
                }
                return false;
            }

            // Handle full auto
            if (_currentWeapon.FullAuto)
            {
                if (isPressed)
                {
                    return FireWeapon(position, direction, velocity, 1f);
                }
                return false;
            }

            // Handle single shot
            if (isPressed && !wasPressed)
            {
                return FireWeapon(position, direction, velocity, 1f);
            }

            return false;
        }

        private bool FireWeapon(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            float currentTime = GetTime();
            
            // Consume energy and ammo
            _energy -= _currentWeapon.EnergyConsumption;
            if (_currentWeapon.AmmoCapacity > 0)
            {
                _currentAmmo--;
                OnAmmoChanged?.Invoke(_currentAmmo, _currentWeapon.AmmoCapacity);
            }

            OnEnergyChanged?.Invoke(_energy / _maxEnergy);

            // Create projectiles based on weapon type and pattern
            CreateProjectiles(position, direction, velocity, chargeMultiplier);

            _lastFireTime = currentTime;
            OnWeaponFired?.Invoke(position, _currentWeaponType);

            // Start reload if out of ammo
            if (_currentWeapon.AmmoCapacity > 0 && _currentAmmo <= 0)
            {
                StartReload();
            }

            return true;
        }

        private void CreateProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            switch (_currentWeapon.Pattern)
            {
                case FiringPattern.Single:
                    CreateSingleProjectile(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Spread:
                case FiringPattern.Cone:
                    CreateSpreadProjectiles(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Ring:
                    CreateRingProjectiles(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Spiral:
                    CreateSpiralProjectiles(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Wave:
                    CreateWaveProjectiles(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Helix:
                    CreateHelixProjectiles(position, direction, velocity, chargeMultiplier);
                    break;
                    
                case FiringPattern.Burst:
                    CreateSingleProjectile(position, direction, velocity, chargeMultiplier);
                    break;
            }
        }

        private void CreateSingleProjectile(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            var projectile = new Projectile3D(
                position,
                ApplyAccuracy(direction) * _currentWeapon.Speed * chargeMultiplier + velocity,
                _currentWeapon,
                _currentWeaponType,
                CalculateDamage(chargeMultiplier)
            );
            
            _activeProjectiles.Add(projectile);
        }

        private void CreateSpreadProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            int projectileCount = _currentWeapon.ProjectileCount;
            float angleStep = _currentWeapon.SpreadAngle / (projectileCount - 1);
            float startAngle = -_currentWeapon.SpreadAngle / 2f;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + (i * angleStep);
                Vector3 spreadDirection = RotateVectorY(direction, angle * MathF.PI / 180f);
                
                var projectile = new Projectile3D(
                    position,
                    ApplyAccuracy(spreadDirection) * _currentWeapon.Speed * chargeMultiplier + velocity,
                    _currentWeapon,
                    _currentWeaponType,
                    CalculateDamage(chargeMultiplier)
                );
                
                _activeProjectiles.Add(projectile);
            }
        }

        private void CreateRingProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            int projectileCount = Math.Max(8, _currentWeapon.ProjectileCount);
            float angleStep = 360f / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * angleStep * MathF.PI / 180f;
                Vector3 ringDirection = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
                
                var projectile = new Projectile3D(
                    position,
                    ringDirection * _currentWeapon.Speed * chargeMultiplier + velocity,
                    _currentWeapon,
                    _currentWeaponType,
                    CalculateDamage(chargeMultiplier)
                );
                
                _activeProjectiles.Add(projectile);
            }
        }

        private void CreateSpiralProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            int projectileCount = _currentWeapon.ProjectileCount;
            float spiralStep = 720f / projectileCount; // Two full rotations

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * spiralStep * MathF.PI / 180f;
                float radius = i * 0.1f; // Increasing radius
                Vector3 spiralOffset = new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius);
                Vector3 spiralDirection = Vector3.Normalize(direction + spiralOffset);
                
                var projectile = new Projectile3D(
                    position + spiralOffset,
                    spiralDirection * _currentWeapon.Speed * chargeMultiplier + velocity,
                    _currentWeapon,
                    _currentWeaponType,
                    CalculateDamage(chargeMultiplier)
                );
                
                // Stagger the creation time for spiral effect
                projectile.SetDelay(i * 0.05f);
                _activeProjectiles.Add(projectile);
            }
        }

        private void CreateWaveProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            int projectileCount = _currentWeapon.ProjectileCount;
            float waveLength = _currentWeapon.SpreadAngle;

            for (int i = 0; i < projectileCount; i++)
            {
                float t = (float)i / (projectileCount - 1);
                float waveOffset = MathF.Sin(t * MathF.PI * 2) * waveLength;
                Vector3 waveDirection = RotateVectorY(direction, waveOffset * MathF.PI / 180f);
                
                var projectile = new Projectile3D(
                    position,
                    ApplyAccuracy(waveDirection) * _currentWeapon.Speed * chargeMultiplier + velocity,
                    _currentWeapon,
                    _currentWeaponType,
                    CalculateDamage(chargeMultiplier)
                );
                
                _activeProjectiles.Add(projectile);
            }
        }

        private void CreateHelixProjectiles(Vector3 position, Vector3 direction, Vector3 velocity, float chargeMultiplier)
        {
            int projectileCount = _currentWeapon.ProjectileCount;
            int helixCount = 2; // Number of helix strands

            for (int strand = 0; strand < helixCount; strand++)
            {
                for (int i = 0; i < projectileCount / helixCount; i++)
                {
                    float t = (float)i / (projectileCount / helixCount - 1);
                    float angle = (strand * 180f + t * 720f) * MathF.PI / 180f; // Two full rotations per strand
                    float radius = 0.5f;
                    
                    Vector3 helixOffset = new Vector3(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius, 0);
                    Vector3 helixDirection = Vector3.Normalize(direction + helixOffset * 0.1f);
                    
                    var projectile = new Projectile3D(
                        position + helixOffset,
                        helixDirection * _currentWeapon.Speed * chargeMultiplier + velocity,
                        _currentWeapon,
                        _currentWeaponType,
                        CalculateDamage(chargeMultiplier)
                    );
                    
                    projectile.SetDelay((strand * 0.1f) + (i * 0.05f));
                    _activeProjectiles.Add(projectile);
                }
            }
        }

        private Vector3 ApplyAccuracy(Vector3 direction)
        {
            if (_currentWeapon.Accuracy >= 1f) return direction;

            float inaccuracy = 1f - _currentWeapon.Accuracy;
            float maxDeviation = inaccuracy * 0.2f; // Max 20% deviation at 0 accuracy

            Vector3 randomOffset = new Vector3(
                (float)(_random.NextDouble() - 0.5) * maxDeviation,
                (float)(_random.NextDouble() - 0.5) * maxDeviation,
                (float)(_random.NextDouble() - 0.5) * maxDeviation
            );

            return Vector3.Normalize(direction + randomOffset);
        }

        private float CalculateDamage(float chargeMultiplier)
        {
            float baseDamage = _currentWeapon.Damage * chargeMultiplier;
            
            // Apply critical hit
            if (_random.NextSingle() < _currentWeapon.CriticalChance)
            {
                baseDamage *= _currentWeapon.CriticalMultiplier;
            }

            return baseDamage;
        }

        private Vector3 RotateVectorY(Vector3 vector, float angle)
        {
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            
            return new Vector3(
                vector.X * cos - vector.Z * sin,
                vector.Y,
                vector.X * sin + vector.Z * cos
            );
        }

        public void Update(float deltaTime)
        {
            // Update reload
            if (_isReloading)
            {
                float currentTime = GetTime();
                if (currentTime - _reloadStartTime >= _currentWeapon.ReloadTime)
                {
                    _currentAmmo = _currentWeapon.AmmoCapacity;
                    _isReloading = false;
                    OnAmmoChanged?.Invoke(_currentAmmo, _currentWeapon.AmmoCapacity);
                }
            }

            // Regenerate energy
            _energy = Math.Min(_maxEnergy, _energy + deltaTime * 50f); // 50 energy per second
            OnEnergyChanged?.Invoke(_energy / _maxEnergy);

            // Update projectiles
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                _activeProjectiles[i].Update(deltaTime);
                if (!_activeProjectiles[i].IsActive)
                {
                    _activeProjectiles.RemoveAt(i);
                }
            }
        }

        public void StartReload()
        {
            if (_currentWeapon.AmmoCapacity <= 0 || _isReloading) return;
            
            _isReloading = true;
            _reloadStartTime = GetTime();
        }

        public void UpgradeWeapon(WeaponType weaponType)
        {
            if (_weaponLevels[weaponType] < MaxWeaponLevel)
            {
                _weaponLevels[weaponType]++;
                
                if (_currentWeaponType == weaponType)
                {
                    ApplyWeaponUpgrades();
                }
                
                OnWeaponUpgraded?.Invoke(weaponType);
            }
        }

        public void Draw(Camera3D camera)
        {
            foreach (var projectile in _activeProjectiles)
            {
                projectile.Draw(camera);
            }
        }

        // Getters
        public WeaponType CurrentWeaponType => _currentWeaponType;
        public WeaponStats CurrentWeapon => _currentWeapon;
        public bool IsCharging => _isCharging;
        public bool IsReloading => _isReloading;
        public float ChargeProgress => _isCharging ? Math.Min(1f, (GetTime() - _chargeStartTime) / _currentWeapon.ChargeTime) : 0f;
        public float ReloadProgress => _isReloading ? Math.Min(1f, (GetTime() - _reloadStartTime) / _currentWeapon.ReloadTime) : 0f;
        public float EnergyPercentage => _energy / _maxEnergy;
        public int CurrentAmmo => _currentAmmo;
        public int MaxAmmo => _currentWeapon.AmmoCapacity;
        public List<Projectile3D> ActiveProjectiles => _activeProjectiles;
        public int GetWeaponLevel(WeaponType weaponType) => _weaponLevels[weaponType];

        private float GetTime() => (float)Raylib.GetTime();
    }
}