using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Asteroids.PowerUps;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Manages synergies between different weapons and power-ups
    /// </summary>
    public class WeaponSynergySystem
    {
        private List<SynergyEffect> _activeSynergies;
        private Dictionary<SynergyKey, SynergyDefinition> _synergyDefinitions;
        private List<SynergyCombo> _discoveredCombos;
        private SynergyAnalyzer _analyzer;
        private Random _random;
        
        public event Action<SynergyEffect> OnSynergyActivated;
        public event Action<SynergyCombo> OnNewComboDiscovered;
        public event Action<SynergyChain> OnChainReactionTriggered;
        
        public WeaponSynergySystem()
        {
            _activeSynergies = new List<SynergyEffect>();
            _synergyDefinitions = new Dictionary<SynergyKey, SynergyDefinition>();
            _discoveredCombos = new List<SynergyCombo>();
            _analyzer = new SynergyAnalyzer();
            _random = new Random();
            
            InitializeSynergyDefinitions();
        }
        
        private void InitializeSynergyDefinitions()
        {
            // Weapon + Weapon synergies
            DefineWeaponSynergy(
                WeaponType.Plasma, WeaponType.Lightning,
                SynergyType.ElementalFusion,
                1.5f,
                "Plasma Lightning Fusion",
                "Plasma and lightning combine to create devastating plasma arcs",
                new[] { StatusEffectType.Electrified, StatusEffectType.Burning }
            );
            
            DefineWeaponSynergy(
                WeaponType.Missile, WeaponType.Shotgun,
                SynergyType.ExplosiveSpread,
                1.8f,
                "Cluster Bombardment",
                "Missiles fragment into multiple explosive pellets",
                new[] { StatusEffectType.DamageBoost }
            );
            
            DefineWeaponSynergy(
                WeaponType.Laser, WeaponType.Beam,
                SynergyType.BeamAmplification,
                2f,
                "Focused Devastation",
                "Laser and beam weapons focus into a single devastating ray",
                new[] { StatusEffectType.AccuracyBoost }
            );
            
            // Advanced Weapon + Power-Up synergies
            DefineAdvancedSynergy(
                AdvancedWeaponType.QuantumCannon, AdvancedPowerUpType.TimeDilation,
                SynergyType.TemporalDistortion,
                2.5f,
                "Quantum Temporal Rift",
                "Quantum projectiles exist outside normal time flow",
                new[] { StatusEffectType.TimeDialated, StatusEffectType.Quantum }
            );
            
            DefineAdvancedSynergy(
                AdvancedWeaponType.NaniteSwarm, AdvancedPowerUpType.NaniteIntegration,
                SynergyType.SwarmAmplification,
                3f,
                "Nanite Hive Mind",
                "Nanite swarms achieve collective intelligence",
                new[] { StatusEffectType.Evolving, StatusEffectType.DamageBoost }
            );
            
            DefineAdvancedSynergy(
                AdvancedWeaponType.GravityGun, AdvancedPowerUpType.SpaceDistortion,
                SynergyType.GravitationalSingularity,
                4f,
                "Event Horizon",
                "Creates localized gravitational singularities",
                new[] { StatusEffectType.GravityControl, StatusEffectType.Slowed }
            );
            
            // Power-Up combinations
            DefinePowerUpSynergy(
                new[] { AdvancedPowerUpType.ElementalInfusion, AdvancedPowerUpType.WeaponEvolution },
                SynergyType.ElementalEvolution,
                2.2f,
                "Evolving Elements",
                "Weapons evolve to match elemental affinities",
                new[] { StatusEffectType.Evolving, StatusEffectType.DamageBoost }
            );
            
            DefinePowerUpSynergy(
                new[] { AdvancedPowerUpType.QuantumShield, AdvancedPowerUpType.TeleportDash },
                SynergyType.QuantumMobility,
                1.8f,
                "Quantum Phase",
                "Movement and defense exist in quantum superposition",
                new[] { StatusEffectType.Quantum, StatusEffectType.Phased }
            );
            
            // Legendary three-way synergies
            DefineTripleSynergy(
                AdvancedWeaponType.QuantumCannon,
                AdvancedPowerUpType.CosmicResonance,
                AdvancedPowerUpType.DimensionalAnchor,
                SynergyType.CosmicSingularity,
                5f,
                "Universe Breaker",
                "Harness the power of cosmic forces to tear reality itself",
                new[] { StatusEffectType.Cosmic, StatusEffectType.Transcendent, StatusEffectType.Quantum }
            );
        }
        
        private void DefineWeaponSynergy(WeaponType weapon1, WeaponType weapon2, SynergyType type,
                                       float strength, string name, string description, StatusEffectType[] effects)
        {
            var key = new SynergyKey(weapon1, weapon2);
            _synergyDefinitions[key] = new SynergyDefinition
            {
                Type = type,
                Strength = strength,
                Name = name,
                Description = description,
                StatusEffects = effects,
                Requirements = new[] { weapon1.ToString(), weapon2.ToString() },
                Rarity = SynergyRarity.Common
            };
        }
        
        private void DefineAdvancedSynergy(AdvancedWeaponType weapon, AdvancedPowerUpType powerUp,
                                         SynergyType type, float strength, string name, string description, StatusEffectType[] effects)
        {
            var key = new SynergyKey(weapon, powerUp);
            _synergyDefinitions[key] = new SynergyDefinition
            {
                Type = type,
                Strength = strength,
                Name = name,
                Description = description,
                StatusEffects = effects,
                Requirements = new[] { weapon.ToString(), powerUp.ToString() },
                Rarity = SynergyRarity.Rare
            };
        }
        
        private void DefinePowerUpSynergy(AdvancedPowerUpType[] powerUps, SynergyType type,
                                        float strength, string name, string description, StatusEffectType[] effects)
        {
            var key = new SynergyKey(powerUps);
            _synergyDefinitions[key] = new SynergyDefinition
            {
                Type = type,
                Strength = strength,
                Name = name,
                Description = description,
                StatusEffects = effects,
                Requirements = powerUps.Select(p => p.ToString()).ToArray(),
                Rarity = SynergyRarity.Epic
            };
        }
        
        private void DefineTripleSynergy(AdvancedWeaponType weapon, AdvancedPowerUpType powerUp1, AdvancedPowerUpType powerUp2,
                                       SynergyType type, float strength, string name, string description, StatusEffectType[] effects)
        {
            var key = new SynergyKey(weapon, powerUp1, powerUp2);
            _synergyDefinitions[key] = new SynergyDefinition
            {
                Type = type,
                Strength = strength,
                Name = name,
                Description = description,
                StatusEffects = effects,
                Requirements = new[] { weapon.ToString(), powerUp1.ToString(), powerUp2.ToString() },
                Rarity = SynergyRarity.Legendary
            };
        }
        
        public void Update(float deltaTime)
        {
            // Update active synergies
            for (int i = _activeSynergies.Count - 1; i >= 0; i--)
            {
                _activeSynergies[i].Update(deltaTime);
                if (!_activeSynergies[i].IsActive)
                {
                    _activeSynergies.RemoveAt(i);
                }
            }
            
            // Update synergy analyzer
            _analyzer.Update(deltaTime);
            
            // Check for chain reactions
            CheckChainReactions();
        }
        
        public void CheckForSynergies(AdvancedPowerUpType[] activePowerUps,
                                    WeaponType? currentStandardWeapon = null,
                                    AdvancedWeaponType? currentAdvancedWeapon = null)
        {
            var newSynergies = new List<SynergyEffect>();
            
            // Check weapon combinations
            if (currentStandardWeapon.HasValue && currentAdvancedWeapon.HasValue)
            {
                var weaponSynergy = FindWeaponSynergy(currentStandardWeapon.Value, currentAdvancedWeapon.Value);
                if (weaponSynergy != null)
                {
                    newSynergies.Add(weaponSynergy);
                }
            }
            
            // Check weapon + power-up combinations
            if (currentAdvancedWeapon.HasValue)
            {
                foreach (var powerUp in activePowerUps)
                {
                    var synergy = FindAdvancedSynergy(currentAdvancedWeapon.Value, powerUp);
                    if (synergy != null)
                    {
                        newSynergies.Add(synergy);
                    }
                }
            }
            
            // Check power-up combinations
            var powerUpSynergies = FindPowerUpSynergies(activePowerUps);
            newSynergies.AddRange(powerUpSynergies);
            
            // Check triple combinations (legendary synergies)
            if (currentAdvancedWeapon.HasValue)
            {
                var tripleSynergies = FindTripleSynergies(currentAdvancedWeapon.Value, activePowerUps);
                newSynergies.AddRange(tripleSynergies);
            }
            
            // Activate new synergies
            foreach (var synergy in newSynergies)
            {
                if (!IsActiveSynergy(synergy.Definition.Type))
                {
                    ActivateSynergy(synergy);
                }
            }
        }
        
        private SynergyEffect FindWeaponSynergy(WeaponType weapon1, AdvancedWeaponType weapon2)
        {
            // Convert advanced weapon to basic weapon type for comparison
            var basicWeapon2 = ConvertAdvancedToBasic(weapon2);
            if (!basicWeapon2.HasValue) return null;
            
            var key = new SynergyKey(weapon1, basicWeapon2.Value);
            if (_synergyDefinitions.TryGetValue(key, out var definition))
            {
                return CreateSynergyEffect(definition);
            }
            
            return null;
        }
        
        private SynergyEffect FindAdvancedSynergy(AdvancedWeaponType weapon, AdvancedPowerUpType powerUp)
        {
            var key = new SynergyKey(weapon, powerUp);
            if (_synergyDefinitions.TryGetValue(key, out var definition))
            {
                return CreateSynergyEffect(definition);
            }
            
            return null;
        }
        
        private List<SynergyEffect> FindPowerUpSynergies(AdvancedPowerUpType[] powerUps)
        {
            var synergies = new List<SynergyEffect>();
            
            // Check all possible combinations of 2 or more power-ups
            for (int i = 0; i < powerUps.Length - 1; i++)
            {
                for (int j = i + 1; j < powerUps.Length; j++)
                {
                    var key = new SynergyKey(new[] { powerUps[i], powerUps[j] });
                    if (_synergyDefinitions.TryGetValue(key, out var definition))
                    {
                        synergies.Add(CreateSynergyEffect(definition));
                    }
                }
            }
            
            return synergies;
        }
        
        private List<SynergyEffect> FindTripleSynergies(AdvancedWeaponType weapon, AdvancedPowerUpType[] powerUps)
        {
            var synergies = new List<SynergyEffect>();
            
            // Check all possible combinations of weapon + 2 power-ups
            for (int i = 0; i < powerUps.Length - 1; i++)
            {
                for (int j = i + 1; j < powerUps.Length; j++)
                {
                    var key = new SynergyKey(weapon, powerUps[i], powerUps[j]);
                    if (_synergyDefinitions.TryGetValue(key, out var definition))
                    {
                        synergies.Add(CreateSynergyEffect(definition));
                    }
                }
            }
            
            return synergies;
        }
        
        private SynergyEffect CreateSynergyEffect(SynergyDefinition definition)
        {
            return new SynergyEffect
            {
                Definition = definition,
                Type = definition.Type,
                Strength = definition.Strength,
                Duration = CalculateSynergyDuration(definition),
                RemainingTime = CalculateSynergyDuration(definition),
                IsActive = true,
                CreationTime = GetTime(),
                Elements = GetSynergyElements(definition.Type)
            };
        }
        
        private float CalculateSynergyDuration(SynergyDefinition definition)
        {
            return definition.Rarity switch
            {
                SynergyRarity.Common => 15f,
                SynergyRarity.Rare => 20f,
                SynergyRarity.Epic => 30f,
                SynergyRarity.Legendary => 45f,
                _ => 10f
            };
        }
        
        private ElementalType[] GetSynergyElements(SynergyType type)
        {
            return type switch
            {
                SynergyType.ElementalFusion => new[] { ElementalType.Plasma, ElementalType.Electromagnetic },
                SynergyType.TemporalDistortion => new[] { ElementalType.Temporal, ElementalType.Quantum },
                SynergyType.GravitationalSingularity => new[] { ElementalType.Gravitational },
                SynergyType.SwarmAmplification => new[] { ElementalType.Technological },
                _ => new[] { ElementalType.Energy }
            };
        }
        
        private void ActivateSynergy(SynergyEffect synergy)
        {
            _activeSynergies.Add(synergy);
            OnSynergyActivated?.Invoke(synergy);
            
            // Check if this is a new combo discovery
            var combo = _discoveredCombos.FirstOrDefault(c => c.Type == synergy.Type);
            if (combo == null)
            {
                combo = new SynergyCombo
                {
                    Type = synergy.Type,
                    Name = synergy.Definition.Name,
                    Description = synergy.Definition.Description,
                    TimesActivated = 1,
                    FirstDiscoveryTime = GetTime(),
                    Rarity = synergy.Definition.Rarity
                };
                
                _discoveredCombos.Add(combo);
                OnNewComboDiscovered?.Invoke(combo);
            }
            else
            {
                combo.TimesActivated++;
            }
        }
        
        private void CheckChainReactions()
        {
            // Check if multiple synergies can create chain reactions
            var activeTypes = _activeSynergies.Select(s => s.Type).ToHashSet();
            
            if (activeTypes.Contains(SynergyType.ElementalFusion) && 
                activeTypes.Contains(SynergyType.BeamAmplification))
            {
                TriggerChainReaction("Elemental Beam Storm", 
                    new[] { SynergyType.ElementalFusion, SynergyType.BeamAmplification },
                    3f);
            }
            
            if (activeTypes.Contains(SynergyType.QuantumMobility) &&
                activeTypes.Contains(SynergyType.TemporalDistortion))
            {
                TriggerChainReaction("Quantum Temporal Cascade",
                    new[] { SynergyType.QuantumMobility, SynergyType.TemporalDistortion },
                    4f);
            }
        }
        
        private void TriggerChainReaction(string name, SynergyType[] triggerTypes, float multiplier)
        {
            var chainReaction = new SynergyChain
            {
                Name = name,
                TriggerTypes = triggerTypes,
                Multiplier = multiplier,
                Duration = 8f,
                IsActive = true
            };
            
            OnChainReactionTriggered?.Invoke(chainReaction);
            
            // Amplify all synergies involved in the chain
            foreach (var synergy in _activeSynergies.Where(s => triggerTypes.Contains(s.Type)))
            {
                synergy.Strength *= multiplier;
                synergy.Duration += chainReaction.Duration;
                synergy.RemainingTime += chainReaction.Duration;
            }
        }
        
        public SynergyEffect CreateWeaponSynergy(WeaponType weapon1, AdvancedWeaponType weapon2, float strength)
        {
            // Create a custom synergy between weapons
            var synergy = new SynergyEffect
            {
                Type = SynergyType.CustomWeaponSynergy,
                Strength = strength,
                Duration = 20f,
                RemainingTime = 20f,
                IsActive = true,
                CreationTime = GetTime(),
                Definition = new SynergyDefinition
                {
                    Type = SynergyType.CustomWeaponSynergy,
                    Name = $"{weapon1} + {weapon2} Synergy",
                    Description = "Custom weapon combination creates enhanced effects",
                    Strength = strength,
                    Rarity = SynergyRarity.Common
                }
            };
            
            _activeSynergies.Add(synergy);
            return synergy;
        }
        
        public bool IsActiveSynergy(SynergyType type)
        {
            return _activeSynergies.Any(s => s.Type == type && s.IsActive);
        }
        
        public List<SynergyEffect> GetActiveSynergies()
        {
            return _activeSynergies.Where(s => s.IsActive).ToList();
        }
        
        public List<SynergyCombo> GetDiscoveredCombos()
        {
            return _discoveredCombos.ToList();
        }
        
        public void Draw(Camera3D camera)
        {
            foreach (var synergy in _activeSynergies.Where(s => s.IsActive))
            {
                DrawSynergyEffect(camera, synergy);
            }
        }
        
        private void DrawSynergyEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw synergy visual effects based on type
            switch (synergy.Type)
            {
                case SynergyType.ElementalFusion:
                    DrawElementalFusionEffect(camera, synergy);
                    break;
                    
                case SynergyType.TemporalDistortion:
                    DrawTemporalDistortionEffect(camera, synergy);
                    break;
                    
                case SynergyType.GravitationalSingularity:
                    DrawGravitationalSingularityEffect(camera, synergy);
                    break;
                    
                case SynergyType.SwarmAmplification:
                    DrawSwarmAmplificationEffect(camera, synergy);
                    break;
                    
                default:
                    DrawGenericSynergyEffect(camera, synergy);
                    break;
            }
        }
        
        private void DrawElementalFusionEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw swirling elemental energies
            float time = GetTime() - synergy.CreationTime;
            float intensity = synergy.Strength * (synergy.RemainingTime / synergy.Duration);
            
            // This would draw appropriate visual effects
        }
        
        private void DrawTemporalDistortionEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw time distortion ripples
            float intensity = synergy.Strength * (synergy.RemainingTime / synergy.Duration);
            // Visual implementation here
        }
        
        private void DrawGravitationalSingularityEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw gravitational field distortions
            float intensity = synergy.Strength * (synergy.RemainingTime / synergy.Duration);
            // Visual implementation here
        }
        
        private void DrawSwarmAmplificationEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw nanite network connections
            float intensity = synergy.Strength * (synergy.RemainingTime / synergy.Duration);
            // Visual implementation here
        }
        
        private void DrawGenericSynergyEffect(Camera3D camera, SynergyEffect synergy)
        {
            // Draw generic synergy glow
            // Visual implementation here
        }
        
        private WeaponType? ConvertAdvancedToBasic(AdvancedWeaponType advancedType)
        {
            return advancedType switch
            {
                AdvancedWeaponType.GaussRifle => WeaponType.RailGun,
                AdvancedWeaponType.QuantumCannon => WeaponType.Plasma,
                AdvancedWeaponType.CryoLaser => WeaponType.Laser,
                AdvancedWeaponType.ClusterBomb => WeaponType.Missile,
                _ => null
            };
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    // Supporting classes and enums
    public enum SynergyType
    {
        ElementalFusion,
        ExplosiveSpread,
        BeamAmplification,
        TemporalDistortion,
        SwarmAmplification,
        GravitationalSingularity,
        ElementalEvolution,
        QuantumMobility,
        CosmicSingularity,
        DamageAmplification,
        ProjectileChaining,
        EnergyResonance,
        CustomWeaponSynergy
    }
    
    public enum SynergyRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
    
    public struct SynergyKey : IEquatable<SynergyKey>
    {
        private readonly string[] _components;
        
        public SynergyKey(WeaponType weapon1, WeaponType weapon2)
        {
            _components = new[] { weapon1.ToString(), weapon2.ToString() }.OrderBy(s => s).ToArray();
        }
        
        public SynergyKey(AdvancedWeaponType weapon, AdvancedPowerUpType powerUp)
        {
            _components = new[] { weapon.ToString(), powerUp.ToString() }.OrderBy(s => s).ToArray();
        }
        
        public SynergyKey(AdvancedPowerUpType[] powerUps)
        {
            _components = powerUps.Select(p => p.ToString()).OrderBy(s => s).ToArray();
        }
        
        public SynergyKey(AdvancedWeaponType weapon, AdvancedPowerUpType powerUp1, AdvancedPowerUpType powerUp2)
        {
            _components = new[] { weapon.ToString(), powerUp1.ToString(), powerUp2.ToString() }
                         .OrderBy(s => s).ToArray();
        }
        
        public bool Equals(SynergyKey other)
        {
            return _components?.SequenceEqual(other._components) == true;
        }
        
        public override bool Equals(object obj)
        {
            return obj is SynergyKey other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return _components?.Aggregate(0, (hash, component) => hash ^ component.GetHashCode()) ?? 0;
        }
    }
    
    public struct SynergyDefinition
    {
        public SynergyType Type;
        public float Strength;
        public string Name;
        public string Description;
        public StatusEffectType[] StatusEffects;
        public string[] Requirements;
        public SynergyRarity Rarity;
    }
    
    public class SynergyEffect
    {
        public SynergyDefinition Definition { get; set; }
        public SynergyType Type { get; set; }
        public float Strength { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; set; }
        public bool IsActive { get; set; }
        public float CreationTime { get; set; }
        public ElementalType[] Elements { get; set; }
        
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
            return IsActive ? (RemainingTime / Duration) : 0f;
        }
    }
    
    public class SynergyCombo
    {
        public SynergyType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int TimesActivated { get; set; }
        public float FirstDiscoveryTime { get; set; }
        public SynergyRarity Rarity { get; set; }
    }
    
    public class SynergyChain
    {
        public string Name { get; set; }
        public SynergyType[] TriggerTypes { get; set; }
        public float Multiplier { get; set; }
        public float Duration { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class SynergyAnalyzer
    {
        private Dictionary<string, int> _combinationHistory;
        private float _lastAnalysisTime;
        
        public SynergyAnalyzer()
        {
            _combinationHistory = new Dictionary<string, int>();
        }
        
        public void Update(float deltaTime)
        {
            // Analyze synergy patterns and suggest optimal combinations
            float currentTime = (float)Raylib.GetTime();
            if (currentTime - _lastAnalysisTime >= 5f) // Analyze every 5 seconds
            {
                PerformAnalysis();
                _lastAnalysisTime = currentTime;
            }
        }
        
        private void PerformAnalysis()
        {
            // Analyze which combinations are most effective
            // This could feed into an AI system for suggesting synergies
        }
        
        public void RecordCombination(string combination)
        {
            _combinationHistory.TryGetValue(combination, out int count);
            _combinationHistory[combination] = count + 1;
        }
        
        public List<string> GetMostEffectiveCombinations()
        {
            return _combinationHistory
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }
}