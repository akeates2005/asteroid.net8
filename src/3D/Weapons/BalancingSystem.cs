using System;
using System.Collections.Generic;
using System.Linq;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Manages weapon balancing, difficulty scaling, and damage calculations
    /// </summary>
    public class BalancingSystem
    {
        private Dictionary<WeaponType, WeaponBalanceProfile> _balanceProfiles;
        private DifficultySettings _currentDifficulty;
        private int _gameLevel;
        private float _timePlayed;
        private PlayerPerformanceTracker _performanceTracker;
        
        // Dynamic balancing parameters
        private float _globalDamageMultiplier = 1f;
        private float _globalFireRateMultiplier = 1f;
        private float _globalEnergyEfficiency = 1f;
        private float _enemyHealthMultiplier = 1f;
        private float _enemySpeedMultiplier = 1f;
        
        // Balance events
        public event Action<WeaponType, float> OnWeaponBalanceChanged;
        public event Action<DifficultyLevel> OnDifficultyChanged;
        public event Action<BalanceMetrics> OnBalanceMetricsUpdated;

        public BalancingSystem()
        {
            _balanceProfiles = new Dictionary<WeaponType, WeaponBalanceProfile>();
            _performanceTracker = new PlayerPerformanceTracker();
            _currentDifficulty = DifficultySettings.CreateDefault(DifficultyLevel.Normal);
            _gameLevel = 1;
            _timePlayed = 0f;
            
            InitializeBalanceProfiles();
        }

        private void InitializeBalanceProfiles()
        {
            foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
            {
                _balanceProfiles[weaponType] = CreateBalanceProfile(weaponType);
            }
        }

        private WeaponBalanceProfile CreateBalanceProfile(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Standard => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1f,
                    DamageScaling = 1f,
                    FireRateScaling = 1f,
                    EnergyEfficiencyScaling = 1f,
                    TargetDPS = 25f,
                    BalanceWeight = 1f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Plasma => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.2f,
                    DamageScaling = 1.1f,
                    FireRateScaling = 0.9f,
                    EnergyEfficiencyScaling = 0.8f,
                    TargetDPS = 35f,
                    BalanceWeight = 0.8f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Laser => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.1f,
                    DamageScaling = 1.05f,
                    FireRateScaling = 1.2f,
                    EnergyEfficiencyScaling = 0.9f,
                    TargetDPS = 40f,
                    BalanceWeight = 0.9f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Missile => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.5f,
                    DamageScaling = 1.3f,
                    FireRateScaling = 0.7f,
                    EnergyEfficiencyScaling = 0.6f,
                    TargetDPS = 45f,
                    BalanceWeight = 0.7f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Shotgun => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.3f,
                    DamageScaling = 1.2f,
                    FireRateScaling = 0.8f,
                    EnergyEfficiencyScaling = 0.85f,
                    TargetDPS = 38f,
                    BalanceWeight = 0.8f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Beam => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.4f,
                    DamageScaling = 1.25f,
                    FireRateScaling = 0.6f,
                    EnergyEfficiencyScaling = 0.7f,
                    TargetDPS = 50f,
                    BalanceWeight = 0.6f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.RailGun => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.8f,
                    DamageScaling = 1.5f,
                    FireRateScaling = 0.5f,
                    EnergyEfficiencyScaling = 0.5f,
                    TargetDPS = 55f,
                    BalanceWeight = 0.5f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Pulse => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.15f,
                    DamageScaling = 1.1f,
                    FireRateScaling = 1.1f,
                    EnergyEfficiencyScaling = 0.95f,
                    TargetDPS = 32f,
                    BalanceWeight = 0.9f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Flamethrower => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.1f,
                    DamageScaling = 1.0f,
                    FireRateScaling = 1.3f,
                    EnergyEfficiencyScaling = 0.8f,
                    TargetDPS = 30f,
                    BalanceWeight = 0.85f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                WeaponType.Lightning => new WeaponBalanceProfile
                {
                    WeaponType = weaponType,
                    BaseEffectiveness = 1.6f,
                    DamageScaling = 1.4f,
                    FireRateScaling = 0.8f,
                    EnergyEfficiencyScaling = 0.65f,
                    TargetDPS = 48f,
                    BalanceWeight = 0.65f,
                    UsageFrequency = 0f,
                    SuccessRate = 0f
                },

                _ => new WeaponBalanceProfile { WeaponType = weaponType }
            };
        }

        public void Update(float deltaTime)
        {
            _timePlayed += deltaTime;
            
            UpdatePerformanceTracking(deltaTime);
            UpdateDynamicBalancing(deltaTime);
            UpdateDifficultyScaling();
            
            // Emit balance metrics every 5 seconds
            if (_timePlayed % 5f < deltaTime)
            {
                EmitBalanceMetrics();
            }
        }

        private void UpdatePerformanceTracking(float deltaTime)
        {
            _performanceTracker.Update(deltaTime);
        }

        private void UpdateDynamicBalancing(float deltaTime)
        {
            // Adjust balance based on player performance
            var performance = _performanceTracker.GetCurrentPerformance();
            
            // If player is struggling, make weapons more effective
            if (performance.OverallPerformance < 0.4f)
            {
                _globalDamageMultiplier = Math.Min(1.3f, _globalDamageMultiplier + deltaTime * 0.1f);
                _globalEnergyEfficiency = Math.Min(1.2f, _globalEnergyEfficiency + deltaTime * 0.05f);
            }
            // If player is dominating, make weapons less effective
            else if (performance.OverallPerformance > 0.8f)
            {
                _globalDamageMultiplier = Math.Max(0.8f, _globalDamageMultiplier - deltaTime * 0.05f);
                _globalEnergyEfficiency = Math.Max(0.9f, _globalEnergyEfficiency - deltaTime * 0.025f);
            }
            // Gradually return to baseline
            else
            {
                _globalDamageMultiplier = Lerp(_globalDamageMultiplier, 1f, deltaTime * 0.1f);
                _globalEnergyEfficiency = Lerp(_globalEnergyEfficiency, 1f, deltaTime * 0.05f);
            }
        }

        private void UpdateDifficultyScaling()
        {
            // Scale difficulty based on game level and time played
            float levelMultiplier = 1f + (_gameLevel - 1) * 0.1f;
            float timeMultiplier = 1f + (_timePlayed / 300f) * 0.05f; // 5% increase every 5 minutes
            
            _enemyHealthMultiplier = levelMultiplier * timeMultiplier;
            _enemySpeedMultiplier = 1f + (levelMultiplier - 1f) * 0.5f; // Half the health scaling for speed
        }

        public WeaponStats ApplyBalancing(WeaponStats originalStats)
        {
            var profile = _balanceProfiles[originalStats.Type];
            var balancedStats = originalStats;

            // Apply balance profile scaling
            balancedStats.Damage *= profile.DamageScaling * _globalDamageMultiplier;
            balancedStats.FireRate *= profile.FireRateScaling * _globalFireRateMultiplier;
            balancedStats.EnergyConsumption *= profile.EnergyEfficiencyScaling * (2f - _globalEnergyEfficiency);

            // Apply difficulty scaling
            balancedStats.Damage *= _currentDifficulty.PlayerDamageMultiplier;
            balancedStats.Range *= _currentDifficulty.WeaponRangeMultiplier;
            balancedStats.EnergyConsumption *= _currentDifficulty.EnergyConsumptionMultiplier;

            // Ensure minimum viability
            balancedStats.Damage = Math.Max(1f, balancedStats.Damage);
            balancedStats.FireRate = Math.Max(0.1f, balancedStats.FireRate);

            return balancedStats;
        }

        public float CalculateEnemyHealth(float baseHealth)
        {
            return baseHealth * _enemyHealthMultiplier * _currentDifficulty.EnemyHealthMultiplier;
        }

        public float CalculateEnemySpeed(float baseSpeed)
        {
            return baseSpeed * _enemySpeedMultiplier * _currentDifficulty.EnemySpeedMultiplier;
        }

        public float CalculateEnemyDamage(float baseDamage)
        {
            return baseDamage * _currentDifficulty.EnemyDamageMultiplier;
        }

        public void RecordWeaponUsage(WeaponType weaponType, bool hitTarget, float damageDealt)
        {
            var profile = _balanceProfiles[weaponType];
            
            _performanceTracker.RecordWeaponUsage(weaponType, hitTarget, damageDealt);
            
            // Update weapon-specific metrics
            profile.UsageFrequency += 1f;
            if (hitTarget)
            {
                profile.SuccessRate = (profile.SuccessRate * (profile.UsageFrequency - 1) + 1f) / profile.UsageFrequency;
            }
            else
            {
                profile.SuccessRate = (profile.SuccessRate * (profile.UsageFrequency - 1)) / profile.UsageFrequency;
            }

            // Trigger balance adjustment if weapon is over/under performing
            CheckWeaponBalance(weaponType);
        }

        private void CheckWeaponBalance(WeaponType weaponType)
        {
            var profile = _balanceProfiles[weaponType];
            
            // If weapon is too effective, reduce its power
            if (profile.SuccessRate > 0.9f && profile.UsageFrequency > 50)
            {
                profile.DamageScaling *= 0.95f;
                profile.EnergyEfficiencyScaling *= 0.98f;
                OnWeaponBalanceChanged?.Invoke(weaponType, -0.05f);
            }
            // If weapon is underperforming, boost it
            else if (profile.SuccessRate < 0.4f && profile.UsageFrequency > 30)
            {
                profile.DamageScaling *= 1.05f;
                profile.EnergyEfficiencyScaling *= 1.02f;
                OnWeaponBalanceChanged?.Invoke(weaponType, 0.05f);
            }
        }

        public void SetDifficulty(DifficultyLevel difficulty)
        {
            _currentDifficulty = DifficultySettings.CreateDefault(difficulty);
            OnDifficultyChanged?.Invoke(difficulty);
        }

        public void SetGameLevel(int level)
        {
            _gameLevel = Math.Max(1, level);
        }

        public void ResetBalance()
        {
            _globalDamageMultiplier = 1f;
            _globalFireRateMultiplier = 1f;
            _globalEnergyEfficiency = 1f;
            _enemyHealthMultiplier = 1f;
            _enemySpeedMultiplier = 1f;
            
            InitializeBalanceProfiles();
            _performanceTracker.Reset();
        }

        private void EmitBalanceMetrics()
        {
            var metrics = new BalanceMetrics
            {
                GlobalDamageMultiplier = _globalDamageMultiplier,
                GlobalEnergyEfficiency = _globalEnergyEfficiency,
                EnemyHealthMultiplier = _enemyHealthMultiplier,
                EnemySpeedMultiplier = _enemySpeedMultiplier,
                AverageWeaponSuccessRate = _balanceProfiles.Values.Average(p => p.SuccessRate),
                TotalWeaponUsage = _balanceProfiles.Values.Sum(p => p.UsageFrequency),
                OverallPerformance = _performanceTracker.GetCurrentPerformance().OverallPerformance
            };
            
            OnBalanceMetricsUpdated?.Invoke(metrics);
        }

        // Utility methods
        private float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);

        // Getters
        public DifficultySettings GetCurrentDifficulty() => _currentDifficulty;
        public WeaponBalanceProfile GetWeaponBalance(WeaponType weaponType) => _balanceProfiles[weaponType];
        public PlayerPerformanceMetrics GetPlayerPerformance() => _performanceTracker.GetCurrentPerformance();
        public float GetGlobalDamageMultiplier() => _globalDamageMultiplier;
        public float GetGlobalEnergyEfficiency() => _globalEnergyEfficiency;
        public float GetEnemyHealthMultiplier() => _enemyHealthMultiplier;
        public float GetEnemySpeedMultiplier() => _enemySpeedMultiplier;
    }

    /// <summary>
    /// Difficulty levels affecting game balance
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert,
        Nightmare
    }

    /// <summary>
    /// Difficulty settings that affect gameplay balance
    /// </summary>
    public struct DifficultySettings
    {
        public DifficultyLevel Level;
        public float PlayerDamageMultiplier;
        public float EnemyHealthMultiplier;
        public float EnemyDamageMultiplier;
        public float EnemySpeedMultiplier;
        public float EnergyConsumptionMultiplier;
        public float WeaponRangeMultiplier;
        public float PowerUpSpawnRate;
        public float ScoreMultiplier;

        public static DifficultySettings CreateDefault(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new DifficultySettings
                {
                    Level = level,
                    PlayerDamageMultiplier = 1.5f,
                    EnemyHealthMultiplier = 0.7f,
                    EnemyDamageMultiplier = 0.6f,
                    EnemySpeedMultiplier = 0.8f,
                    EnergyConsumptionMultiplier = 0.7f,
                    WeaponRangeMultiplier = 1.2f,
                    PowerUpSpawnRate = 1.5f,
                    ScoreMultiplier = 0.8f
                },

                DifficultyLevel.Normal => new DifficultySettings
                {
                    Level = level,
                    PlayerDamageMultiplier = 1f,
                    EnemyHealthMultiplier = 1f,
                    EnemyDamageMultiplier = 1f,
                    EnemySpeedMultiplier = 1f,
                    EnergyConsumptionMultiplier = 1f,
                    WeaponRangeMultiplier = 1f,
                    PowerUpSpawnRate = 1f,
                    ScoreMultiplier = 1f
                },

                DifficultyLevel.Hard => new DifficultySettings
                {
                    Level = level,
                    PlayerDamageMultiplier = 0.9f,
                    EnemyHealthMultiplier = 1.3f,
                    EnemyDamageMultiplier = 1.2f,
                    EnemySpeedMultiplier = 1.1f,
                    EnergyConsumptionMultiplier = 1.2f,
                    WeaponRangeMultiplier = 0.9f,
                    PowerUpSpawnRate = 0.8f,
                    ScoreMultiplier = 1.2f
                },

                DifficultyLevel.Expert => new DifficultySettings
                {
                    Level = level,
                    PlayerDamageMultiplier = 0.8f,
                    EnemyHealthMultiplier = 1.6f,
                    EnemyDamageMultiplier = 1.4f,
                    EnemySpeedMultiplier = 1.25f,
                    EnergyConsumptionMultiplier = 1.4f,
                    WeaponRangeMultiplier = 0.8f,
                    PowerUpSpawnRate = 0.6f,
                    ScoreMultiplier = 1.5f
                },

                DifficultyLevel.Nightmare => new DifficultySettings
                {
                    Level = level,
                    PlayerDamageMultiplier = 0.7f,
                    EnemyHealthMultiplier = 2f,
                    EnemyDamageMultiplier = 1.8f,
                    EnemySpeedMultiplier = 1.5f,
                    EnergyConsumptionMultiplier = 1.6f,
                    WeaponRangeMultiplier = 0.7f,
                    PowerUpSpawnRate = 0.4f,
                    ScoreMultiplier = 2f
                },

                _ => CreateDefault(DifficultyLevel.Normal)
            };
        }
    }

    /// <summary>
    /// Balance profile for individual weapons
    /// </summary>
    public class WeaponBalanceProfile
    {
        public WeaponType WeaponType { get; set; }
        public float BaseEffectiveness { get; set; } = 1f;
        public float DamageScaling { get; set; } = 1f;
        public float FireRateScaling { get; set; } = 1f;
        public float EnergyEfficiencyScaling { get; set; } = 1f;
        public float TargetDPS { get; set; } = 30f;
        public float BalanceWeight { get; set; } = 1f;
        public float UsageFrequency { get; set; } = 0f;
        public float SuccessRate { get; set; } = 0f;
    }

    /// <summary>
    /// Tracks player performance for dynamic balancing
    /// </summary>
    public class PlayerPerformanceTracker
    {
        private List<PerformanceSnapshot> _recentPerformance;
        private Dictionary<WeaponType, WeaponPerformance> _weaponPerformance;
        private float _totalTimePlayed;
        private int _totalEnemiesKilled;
        private int _totalShotsFired;
        private int _totalShotsHit;
        private float _totalDamageDealt;
        private int _totalDeaths;

        public PlayerPerformanceTracker()
        {
            _recentPerformance = new List<PerformanceSnapshot>();
            _weaponPerformance = new Dictionary<WeaponType, WeaponPerformance>();
            
            foreach (WeaponType weaponType in Enum.GetValues<WeaponType>())
            {
                _weaponPerformance[weaponType] = new WeaponPerformance();
            }
        }

        public void Update(float deltaTime)
        {
            _totalTimePlayed += deltaTime;
            
            // Record performance snapshot every 10 seconds
            if (_totalTimePlayed % 10f < deltaTime)
            {
                RecordPerformanceSnapshot();
            }
        }

        public void RecordWeaponUsage(WeaponType weaponType, bool hit, float damage)
        {
            var performance = _weaponPerformance[weaponType];
            performance.ShotsFired++;
            
            if (hit)
            {
                performance.ShotsHit++;
                performance.DamageDealt += damage;
                _totalShotsHit++;
                _totalDamageDealt += damage;
            }
            
            _totalShotsFired++;
        }

        public void RecordEnemyKilled()
        {
            _totalEnemiesKilled++;
        }

        public void RecordPlayerDeath()
        {
            _totalDeaths++;
        }

        private void RecordPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = _totalTimePlayed,
                Accuracy = _totalShotsFired > 0 ? (float)_totalShotsHit / _totalShotsFired : 0f,
                DamagePerSecond = _totalTimePlayed > 0 ? _totalDamageDealt / _totalTimePlayed : 0f,
                KillsPerMinute = _totalTimePlayed > 0 ? _totalEnemiesKilled / (_totalTimePlayed / 60f) : 0f,
                DeathRate = _totalTimePlayed > 0 ? _totalDeaths / (_totalTimePlayed / 60f) : 0f
            };
            
            _recentPerformance.Add(snapshot);
            
            // Keep only last 30 snapshots (5 minutes of data)
            if (_recentPerformance.Count > 30)
            {
                _recentPerformance.RemoveAt(0);
            }
        }

        public PlayerPerformanceMetrics GetCurrentPerformance()
        {
            if (_recentPerformance.Count == 0)
            {
                return new PlayerPerformanceMetrics();
            }

            var recent = _recentPerformance.TakeLast(6).ToList(); // Last minute
            
            return new PlayerPerformanceMetrics
            {
                OverallAccuracy = _totalShotsFired > 0 ? (float)_totalShotsHit / _totalShotsFired : 0f,
                RecentAccuracy = recent.Count > 0 ? recent.Average(s => s.Accuracy) : 0f,
                DamagePerSecond = recent.Count > 0 ? recent.Average(s => s.DamagePerSecond) : 0f,
                KillsPerMinute = recent.Count > 0 ? recent.Average(s => s.KillsPerMinute) : 0f,
                DeathRate = recent.Count > 0 ? recent.Average(s => s.DeathRate) : 0f,
                OverallPerformance = CalculateOverallPerformance(recent)
            };
        }

        private float CalculateOverallPerformance(List<PerformanceSnapshot> recent)
        {
            if (recent.Count == 0) return 0.5f; // Neutral performance
            
            float accuracyScore = Math.Min(1f, recent.Average(s => s.Accuracy) / 0.7f); // 70% accuracy = 1.0 score
            float dpsScore = Math.Min(1f, recent.Average(s => s.DamagePerSecond) / 50f); // 50 DPS = 1.0 score
            float killScore = Math.Min(1f, recent.Average(s => s.KillsPerMinute) / 10f); // 10 kills/min = 1.0 score
            float survivalScore = Math.Max(0f, 1f - recent.Average(s => s.DeathRate) / 2f); // 2 deaths/min = 0.0 score
            
            return (accuracyScore * 0.3f + dpsScore * 0.3f + killScore * 0.2f + survivalScore * 0.2f);
        }

        public void Reset()
        {
            _recentPerformance.Clear();
            _totalTimePlayed = 0f;
            _totalEnemiesKilled = 0;
            _totalShotsFired = 0;
            _totalShotsHit = 0;
            _totalDamageDealt = 0f;
            _totalDeaths = 0;
            
            foreach (var performance in _weaponPerformance.Values)
            {
                performance.Reset();
            }
        }
    }

    /// <summary>
    /// Performance metrics for balancing calculations
    /// </summary>
    public struct PlayerPerformanceMetrics
    {
        public float OverallAccuracy;
        public float RecentAccuracy;
        public float DamagePerSecond;
        public float KillsPerMinute;
        public float DeathRate;
        public float OverallPerformance; // 0-1 composite score
    }

    /// <summary>
    /// Snapshot of player performance at a specific time
    /// </summary>
    public struct PerformanceSnapshot
    {
        public float Timestamp;
        public float Accuracy;
        public float DamagePerSecond;
        public float KillsPerMinute;
        public float DeathRate;
    }

    /// <summary>
    /// Performance tracking for individual weapons
    /// </summary>
    public class WeaponPerformance
    {
        public int ShotsFired { get; set; }
        public int ShotsHit { get; set; }
        public float DamageDealt { get; set; }
        public float TimeUsed { get; set; }

        public float GetAccuracy() => ShotsFired > 0 ? (float)ShotsHit / ShotsFired : 0f;
        public float GetDPS() => TimeUsed > 0 ? DamageDealt / TimeUsed : 0f;

        public void Reset()
        {
            ShotsFired = 0;
            ShotsHit = 0;
            DamageDealt = 0f;
            TimeUsed = 0f;
        }
    }

    /// <summary>
    /// Balance metrics for monitoring and debugging
    /// </summary>
    public struct BalanceMetrics
    {
        public float GlobalDamageMultiplier;
        public float GlobalEnergyEfficiency;
        public float EnemyHealthMultiplier;
        public float EnemySpeedMultiplier;
        public float AverageWeaponSuccessRate;
        public float TotalWeaponUsage;
        public float OverallPerformance;
    }
}