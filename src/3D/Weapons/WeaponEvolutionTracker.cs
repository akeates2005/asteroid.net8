using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids3D.Weapons
{
    public class WeaponEvolutionTracker
    {
        private Dictionary<AdvancedWeaponType, WeaponEvolutionData> _evolutionData;
        private Dictionary<AdvancedWeaponType, float> _experiencePoints;
        private List<EvolutionMilestone> _milestones;
        private Random _random;
        
        public event Action<AdvancedWeaponType, int> OnWeaponEvolved;
        public event Action<EvolutionMilestone> OnMilestoneReached;
        
        public WeaponEvolutionTracker()
        {
            _evolutionData = new Dictionary<AdvancedWeaponType, WeaponEvolutionData>();
            _experiencePoints = new Dictionary<AdvancedWeaponType, float>();
            _milestones = new List<EvolutionMilestone>();
            _random = new Random();
            
            InitializeEvolutionData();
            InitializeMilestones();
        }
        
        private void InitializeEvolutionData()
        {
            foreach (AdvancedWeaponType weaponType in Enum.GetValues<AdvancedWeaponType>())
            {
                _evolutionData[weaponType] = new WeaponEvolutionData
                {
                    CurrentLevel = 1,
                    MaxLevel = 10,
                    ExperienceRequired = CalculateExperienceRequired(weaponType, 1),
                    EvolutionTree = GenerateEvolutionTree(weaponType),
                    UnlockedAbilities = new List<string>(),
                    EvolutionBonuses = new Dictionary<string, float>()
                };
                
                _experiencePoints[weaponType] = 0f;
            }
        }
        
        private void InitializeMilestones()
        {
            _milestones.AddRange(new[]
            {
                new EvolutionMilestone
                {
                    Id = "first_evolution",
                    Name = "First Evolution",
                    Description = "Evolve any weapon to level 2",
                    RequiredConditions = new Dictionary<string, object> { { "any_weapon_level", 2 } },
                    Rewards = new List<string> { "unlock_advanced_targeting", "experience_boost_10" },
                    IsCompleted = false
                },
                new EvolutionMilestone
                {
                    Id = "weapon_master",
                    Name = "Weapon Master",
                    Description = "Reach level 5 with 3 different weapons",
                    RequiredConditions = new Dictionary<string, object> { { "weapons_at_level_5", 3 } },
                    Rewards = new List<string> { "unlock_weapon_fusion", "damage_boost_15" },
                    IsCompleted = false
                },
                new EvolutionMilestone
                {
                    Id = "evolution_grandmaster",
                    Name = "Evolution Grandmaster",
                    Description = "Reach maximum level with any weapon",
                    RequiredConditions = new Dictionary<string, object> { { "max_level_weapon", 1 } },
                    Rewards = new List<string> { "unlock_legendary_modes", "cosmic_resonance" },
                    IsCompleted = false
                }
            });
        }
        
        public void AddExperience(AdvancedWeaponType weaponType, float experience, ExperienceSource source = ExperienceSource.Combat)
        {
            if (!_experiencePoints.ContainsKey(weaponType)) return;
            
            // Apply source multiplier
            float multiplier = source switch
            {
                ExperienceSource.Combat => 1.0f,
                ExperienceSource.CriticalHit => 1.5f,
                ExperienceSource.MultiKill => 2.0f,
                ExperienceSource.BossKill => 3.0f,
                ExperienceSource.PerfectAccuracy => 1.25f,
                _ => 1.0f
            };
            
            _experiencePoints[weaponType] += experience * multiplier;
            
            CheckForEvolution(weaponType);
            CheckMilestones();
        }
        
        private void CheckForEvolution(AdvancedWeaponType weaponType)
        {
            var data = _evolutionData[weaponType];
            var currentExp = _experiencePoints[weaponType];
            
            while (currentExp >= data.ExperienceRequired && data.CurrentLevel < data.MaxLevel)
            {
                // Level up!
                _experiencePoints[weaponType] -= data.ExperienceRequired;
                data.CurrentLevel++;
                
                // Unlock new abilities
                UnlockAbilitiesForLevel(weaponType, data.CurrentLevel);
                
                // Calculate next level requirement
                if (data.CurrentLevel < data.MaxLevel)
                {
                    data.ExperienceRequired = CalculateExperienceRequired(weaponType, data.CurrentLevel);
                }
                
                OnWeaponEvolved?.Invoke(weaponType, data.CurrentLevel);
                
                currentExp = _experiencePoints[weaponType];
            }
        }
        
        private void UnlockAbilitiesForLevel(AdvancedWeaponType weaponType, int level)
        {
            var data = _evolutionData[weaponType];
            var tree = data.EvolutionTree;
            
            if (tree.ContainsKey(level))
            {
                var levelData = tree[level];
                data.UnlockedAbilities.AddRange(levelData.UnlockedAbilities);
                
                foreach (var bonus in levelData.StatBonuses)
                {
                    if (data.EvolutionBonuses.ContainsKey(bonus.Key))
                        data.EvolutionBonuses[bonus.Key] += bonus.Value;
                    else
                        data.EvolutionBonuses[bonus.Key] = bonus.Value;
                }
            }
        }
        
        private float CalculateExperienceRequired(AdvancedWeaponType weaponType, int level)
        {
            // Base experience with weapon-specific multipliers
            float baseExp = 100f * level * level;
            
            float weaponMultiplier = weaponType switch
            {
                AdvancedWeaponType.GaussRifle => 1.0f,
                AdvancedWeaponType.QuantumCannon => 1.3f,
                AdvancedWeaponType.NaniteSwarm => 1.5f,
                AdvancedWeaponType.GravityGun => 1.2f,
                AdvancedWeaponType.EnergyVortex => 1.4f,
                AdvancedWeaponType.PhotonTorpedo => 1.1f,
                AdvancedWeaponType.DarkMatterLance => 1.6f,
                AdvancedWeaponType.CryoLaser => 1.0f,
                AdvancedWeaponType.PlasmaThrower => 1.0f,
                AdvancedWeaponType.IonStorm => 1.3f,
                _ => 1.0f
            };
            
            return baseExp * weaponMultiplier;
        }
        
        private Dictionary<int, EvolutionLevelData> GenerateEvolutionTree(AdvancedWeaponType weaponType)
        {
            var tree = new Dictionary<int, EvolutionLevelData>();
            
            // Generate level-specific evolution data
            for (int level = 2; level <= 10; level++)
            {
                tree[level] = GenerateEvolutionLevelData(weaponType, level);
            }
            
            return tree;
        }
        
        private EvolutionLevelData GenerateEvolutionLevelData(AdvancedWeaponType weaponType, int level)
        {
            var abilities = new List<string>();
            var bonuses = new Dictionary<string, float>();
            
            // Base stat improvements
            bonuses["damage_multiplier"] = 0.1f * level;
            bonuses["fire_rate_bonus"] = 0.05f * level;
            
            // Weapon-specific abilities and bonuses
            switch (weaponType)
            {
                case AdvancedWeaponType.GaussRifle:
                    if (level == 3) abilities.Add("piercing_shots");
                    if (level == 5) abilities.Add("electromagnetic_pulse");
                    if (level == 7) abilities.Add("railgun_mode");
                    if (level == 10) abilities.Add("mass_driver_cannon");
                    bonuses["projectile_speed"] = 0.15f * level;
                    break;
                    
                case AdvancedWeaponType.QuantumCannon:
                    if (level == 3) abilities.Add("quantum_tunneling");
                    if (level == 5) abilities.Add("probability_manipulation");
                    if (level == 7) abilities.Add("dimensional_rift");
                    if (level == 10) abilities.Add("reality_collapse");
                    bonuses["crit_chance"] = 0.05f * level;
                    break;
                    
                case AdvancedWeaponType.NaniteSwarm:
                    if (level == 3) abilities.Add("self_replication");
                    if (level == 5) abilities.Add("adaptive_behavior");
                    if (level == 7) abilities.Add("hive_mind_coordination");
                    if (level == 10) abilities.Add("technological_singularity");
                    bonuses["swarm_size"] = 2f * level;
                    break;
                    
                case AdvancedWeaponType.GravityGun:
                    if (level == 3) abilities.Add("gravity_well");
                    if (level == 5) abilities.Add("tidal_force");
                    if (level == 7) abilities.Add("space_time_distortion");
                    if (level == 10) abilities.Add("black_hole_generator");
                    bonuses["gravity_strength"] = 0.2f * level;
                    break;
                    
                case AdvancedWeaponType.EnergyVortex:
                    if (level == 3) abilities.Add("energy_absorption");
                    if (level == 5) abilities.Add("vortex_expansion");
                    if (level == 7) abilities.Add("energy_cascade");
                    if (level == 10) abilities.Add("cosmic_storm");
                    bonuses["energy_efficiency"] = 0.1f * level;
                    break;
            }
            
            return new EvolutionLevelData
            {
                Level = level,
                UnlockedAbilities = abilities,
                StatBonuses = bonuses,
                VisualUpgrades = GenerateVisualUpgrades(weaponType, level)
            };
        }
        
        private List<string> GenerateVisualUpgrades(AdvancedWeaponType weaponType, int level)
        {
            var upgrades = new List<string>();
            
            if (level >= 3) upgrades.Add($"enhanced_{weaponType.ToString().ToLower()}_glow");
            if (level >= 5) upgrades.Add($"advanced_{weaponType.ToString().ToLower()}_particles");
            if (level >= 7) upgrades.Add($"legendary_{weaponType.ToString().ToLower()}_aura");
            if (level >= 10) upgrades.Add($"cosmic_{weaponType.ToString().ToLower()}_transcendence");
            
            return upgrades;
        }
        
        private void CheckMilestones()
        {
            foreach (var milestone in _milestones)
            {
                if (milestone.IsCompleted) continue;
                
                bool conditionsMet = true;
                
                foreach (var condition in milestone.RequiredConditions)
                {
                    switch (condition.Key)
                    {
                        case "any_weapon_level":
                            var requiredLevel = (int)condition.Value;
                            bool hasWeaponAtLevel = false;
                            foreach (var data in _evolutionData.Values)
                            {
                                if (data.CurrentLevel >= requiredLevel)
                                {
                                    hasWeaponAtLevel = true;
                                    break;
                                }
                            }
                            if (!hasWeaponAtLevel) conditionsMet = false;
                            break;
                            
                        case "weapons_at_level_5":
                            var requiredCount = (int)condition.Value;
                            int weaponsAtLevel5 = 0;
                            foreach (var data in _evolutionData.Values)
                            {
                                if (data.CurrentLevel >= 5) weaponsAtLevel5++;
                            }
                            if (weaponsAtLevel5 < requiredCount) conditionsMet = false;
                            break;
                            
                        case "max_level_weapon":
                            bool hasMaxLevelWeapon = false;
                            foreach (var data in _evolutionData.Values)
                            {
                                if (data.CurrentLevel >= data.MaxLevel)
                                {
                                    hasMaxLevelWeapon = true;
                                    break;
                                }
                            }
                            if (!hasMaxLevelWeapon) conditionsMet = false;
                            break;
                    }
                    
                    if (!conditionsMet) break;
                }
                
                if (conditionsMet)
                {
                    milestone.IsCompleted = true;
                    OnMilestoneReached?.Invoke(milestone);
                }
            }
        }
        
        public WeaponEvolutionData GetEvolutionData(AdvancedWeaponType weaponType)
        {
            return _evolutionData.GetValueOrDefault(weaponType);
        }
        
        public float GetExperience(AdvancedWeaponType weaponType)
        {
            return _experiencePoints.GetValueOrDefault(weaponType);
        }
        
        public float GetEvolutionProgress(AdvancedWeaponType weaponType)
        {
            var data = GetEvolutionData(weaponType);
            if (data == null || data.CurrentLevel >= data.MaxLevel) return 1.0f;
            
            var currentExp = GetExperience(weaponType);
            return Math.Min(1.0f, currentExp / data.ExperienceRequired);
        }
        
        public List<EvolutionMilestone> GetMilestones()
        {
            return new List<EvolutionMilestone>(_milestones);
        }
        
        public void Update(float deltaTime)
        {
            // Update any time-based evolution mechanics
            foreach (var weaponType in _evolutionData.Keys)
            {
                var data = _evolutionData[weaponType];
                
                // Passive experience gain for active weapons
                if (data.UnlockedAbilities.Contains("passive_growth"))
                {
                    AddExperience(weaponType, 0.1f * deltaTime, ExperienceSource.Passive);
                }
            }
        }
        
        public void Render(Vector2 position, float scale = 1.0f)
        {
            // Render evolution UI elements
            var startY = position.Y;
            var lineHeight = 25 * scale;
            
            Raylib.DrawText("WEAPON EVOLUTION", (int)position.X, (int)startY, 
                           (int)(20 * scale), Color.WHITE);
            startY += lineHeight * 1.5f;
            
            foreach (var kvp in _evolutionData)
            {
                var weaponType = kvp.Key;
                var data = kvp.Value;
                var progress = GetEvolutionProgress(weaponType);
                
                // Weapon name and level
                var weaponName = weaponType.ToString().Replace("_", " ");
                var levelText = $"{weaponName} Lv.{data.CurrentLevel}";
                Raylib.DrawText(levelText, (int)position.X, (int)startY, 
                               (int)(16 * scale), Color.YELLOW);
                
                // Progress bar
                var barWidth = 200 * scale;
                var barHeight = 8 * scale;
                var barX = position.X + 250 * scale;
                var barY = startY + 4 * scale;
                
                Raylib.DrawRectangle((int)barX, (int)barY, (int)barWidth, (int)barHeight, Color.DARKGRAY);
                Raylib.DrawRectangle((int)barX, (int)barY, (int)(barWidth * progress), (int)barHeight, Color.GREEN);
                
                // Experience text
                if (data.CurrentLevel < data.MaxLevel)
                {
                    var expText = $"{(int)GetExperience(weaponType)}/{(int)data.ExperienceRequired}";
                    Raylib.DrawText(expText, (int)(barX + barWidth + 10), (int)startY, 
                                   (int)(14 * scale), Color.WHITE);
                }
                else
                {
                    Raylib.DrawText("MAX", (int)(barX + barWidth + 10), (int)startY, 
                                   (int)(14 * scale), Color.GOLD);
                }
                
                startY += lineHeight;
            }
        }
    }
    
    public enum ExperienceSource
    {
        Combat,
        CriticalHit,
        MultiKill,
        BossKill,
        PerfectAccuracy,
        Passive
    }
    
    public class WeaponEvolutionData
    {
        public int CurrentLevel { get; set; }
        public int MaxLevel { get; set; }
        public float ExperienceRequired { get; set; }
        public Dictionary<int, EvolutionLevelData> EvolutionTree { get; set; }
        public List<string> UnlockedAbilities { get; set; }
        public Dictionary<string, float> EvolutionBonuses { get; set; }
    }
    
    public class EvolutionLevelData
    {
        public int Level { get; set; }
        public List<string> UnlockedAbilities { get; set; }
        public Dictionary<string, float> StatBonuses { get; set; }
        public List<string> VisualUpgrades { get; set; }
    }
    
    public class EvolutionMilestone
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> RequiredConditions { get; set; }
        public List<string> Rewards { get; set; }
        public bool IsCompleted { get; set; }
    }
}