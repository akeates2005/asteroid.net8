using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace Asteroids3D.PowerUps
{
    public class AdvancedPowerUpEffect
    {
        private Dictionary<AdvancedPowerUpType, PowerUpEffectData> _effectData;
        private List<ActivePowerUpEffect> _activeEffects;
        private Dictionary<string, PowerUpChain> _chainEffects;
        private Random _random;
        
        public event Action<AdvancedPowerUpType, float> OnPowerUpActivated;
        public event Action<AdvancedPowerUpType> OnPowerUpExpired;
        public event Action<PowerUpChain> OnChainEffectTriggered;
        
        public AdvancedPowerUpEffect()
        {
            _effectData = new Dictionary<AdvancedPowerUpType, PowerUpEffectData>();
            _activeEffects = new List<ActivePowerUpEffect>();
            _chainEffects = new Dictionary<string, PowerUpChain>();
            _random = new Random();
            
            InitializeEffectData();
            InitializeChainEffects();
        }
        
        private void InitializeEffectData()
        {
            _effectData[AdvancedPowerUpType.WeaponEvolution] = new PowerUpEffectData
            {
                Duration = 30f,
                Intensity = 1.0f,
                ParticleEffect = ParticleEffectType.EvolutionSpiral,
                AudioEffect = "evolution_activate",
                VisualEffects = new List<string> { "weapon_glow_enhancement", "evolution_aura" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "damage_multiplier", 1.5f },
                    { "evolution_speed", 2.0f },
                    { "ability_unlock_chance", 0.25f }
                }
            };
            
            _effectData[AdvancedPowerUpType.QuantumShield] = new PowerUpEffectData
            {
                Duration = 25f,
                Intensity = 1.2f,
                ParticleEffect = ParticleEffectType.QuantumDistortion,
                AudioEffect = "quantum_shield_activate",
                VisualEffects = new List<string> { "quantum_barrier", "phase_shift_glow" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "damage_reduction", 0.75f },
                    { "phase_chance", 0.3f },
                    { "quantum_reflection", 0.2f }
                }
            };
            
            _effectData[AdvancedPowerUpType.TeleportDash] = new PowerUpEffectData
            {
                Duration = 0.5f, // Instant effect
                Intensity = 2.0f,
                ParticleEffect = ParticleEffectType.TeleportFlash,
                AudioEffect = "teleport_dash",
                VisualEffects = new List<string> { "teleport_trail", "dimensional_rift" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "teleport_distance", 500f },
                    { "invulnerability_frames", 0.5f },
                    { "damage_on_arrival", 150f }
                }
            };
            
            _effectData[AdvancedPowerUpType.NaniteIntegration] = new PowerUpEffectData
            {
                Duration = 45f,
                Intensity = 1.3f,
                ParticleEffect = ParticleEffectType.NaniteCloud,
                AudioEffect = "nanite_integration",
                VisualEffects = new List<string> { "nanite_swarm_aura", "technological_enhancement" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "repair_rate", 5f },
                    { "weapon_enhancement", 1.25f },
                    { "nanite_production", 2.0f }
                }
            };
            
            _effectData[AdvancedPowerUpType.CosmicResonance] = new PowerUpEffectData
            {
                Duration = 60f,
                Intensity = 1.5f,
                ParticleEffect = ParticleEffectType.CosmicEnergy,
                AudioEffect = "cosmic_resonance",
                VisualEffects = new List<string> { "cosmic_aura", "stellar_enhancement", "galaxy_swirl" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "all_stats_multiplier", 1.3f },
                    { "cosmic_damage_bonus", 2.0f },
                    { "energy_regeneration", 3.0f }
                }
            };
            
            _effectData[AdvancedPowerUpType.HolographicDecoys] = new PowerUpEffectData
            {
                Duration = 20f,
                Intensity = 1.0f,
                ParticleEffect = ParticleEffectType.HologramFlicker,
                AudioEffect = "hologram_activate",
                VisualEffects = new List<string> { "holographic_duplicates", "light_refraction" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "decoy_count", 3f },
                    { "confusion_chance", 0.4f },
                    { "evasion_bonus", 0.25f }
                }
            };
            
            _effectData[AdvancedPowerUpType.PowerAmplifier] = new PowerUpEffectData
            {
                Duration = 15f,
                Intensity = 2.5f,
                ParticleEffect = ParticleEffectType.PowerSurge,
                AudioEffect = "power_amplifier",
                VisualEffects = new List<string> { "energy_overflow", "power_crackling", "amplification_field" },
                StatModifiers = new Dictionary<string, float>
                {
                    { "damage_amplification", 3.0f },
                    { "effect_duration_bonus", 1.5f },
                    { "chain_reaction_chance", 0.6f }
                }
            };
        }
        
        private void InitializeChainEffects()
        {
            // Quantum + Evolution chain
            _chainEffects["quantum_evolution"] = new PowerUpChain
            {
                Id = "quantum_evolution",
                Name = "Quantum Evolution Cascade",
                RequiredPowerUps = new List<AdvancedPowerUpType> 
                { 
                    AdvancedPowerUpType.QuantumShield, 
                    AdvancedPowerUpType.WeaponEvolution 
                },
                ChainEffects = new Dictionary<string, float>
                {
                    { "quantum_evolution_burst", 1.0f },
                    { "temporal_weapon_enhancement", 2.0f },
                    { "reality_distortion_field", 1.5f }
                },
                Duration = 20f,
                Rarity = ChainRarity.Rare
            };
            
            // Cosmic + Amplifier chain
            _chainEffects["cosmic_amplification"] = new PowerUpChain
            {
                Id = "cosmic_amplification",
                Name = "Cosmic Power Surge",
                RequiredPowerUps = new List<AdvancedPowerUpType> 
                { 
                    AdvancedPowerUpType.CosmicResonance, 
                    AdvancedPowerUpType.PowerAmplifier 
                },
                ChainEffects = new Dictionary<string, float>
                {
                    { "stellar_explosion", 5.0f },
                    { "cosmic_beam_array", 3.0f },
                    { "gravitational_collapse", 2.5f }
                },
                Duration = 25f,
                Rarity = ChainRarity.Epic
            };
            
            // Nanite + Hologram chain
            _chainEffects["technological_illusion"] = new PowerUpChain
            {
                Id = "technological_illusion",
                Name = "Nanite Hologram Network",
                RequiredPowerUps = new List<AdvancedPowerUpType> 
                { 
                    AdvancedPowerUpType.NaniteIntegration, 
                    AdvancedPowerUpType.HolographicDecoys 
                },
                ChainEffects = new Dictionary<string, float>
                {
                    { "nanite_decoy_generation", 1.0f },
                    { "technological_camouflage", 1.5f },
                    { "adaptive_hologram_ai", 2.0f }
                },
                Duration = 30f,
                Rarity = ChainRarity.Uncommon
            };
            
            // Triple chain: Quantum + Cosmic + Evolution
            _chainEffects["transcendence"] = new PowerUpChain
            {
                Id = "transcendence",
                Name = "Cosmic Transcendence",
                RequiredPowerUps = new List<AdvancedPowerUpType> 
                { 
                    AdvancedPowerUpType.QuantumShield,
                    AdvancedPowerUpType.CosmicResonance,
                    AdvancedPowerUpType.WeaponEvolution
                },
                ChainEffects = new Dictionary<string, float>
                {
                    { "reality_transcendence", 10.0f },
                    { "universal_harmony", 5.0f },
                    { "omniscient_targeting", 3.0f },
                    { "dimensional_mastery", 4.0f }
                },
                Duration = 45f,
                Rarity = ChainRarity.Legendary
            };
        }
        
        public void ActivatePowerUp(AdvancedPowerUpType powerUpType, float intensityMultiplier = 1.0f)
        {
            if (!_effectData.ContainsKey(powerUpType)) return;
            
            var effectData = _effectData[powerUpType];
            
            // Create active effect
            var activeEffect = new ActivePowerUpEffect
            {
                PowerUpType = powerUpType,
                RemainingDuration = effectData.Duration,
                Intensity = effectData.Intensity * intensityMultiplier,
                EffectData = effectData,
                ParticleSystem = CreateParticleSystem(effectData.ParticleEffect),
                ActivationTime = DateTime.Now
            };
            
            // Check for existing effect of same type
            var existingEffect = _activeEffects.FirstOrDefault(e => e.PowerUpType == powerUpType);
            if (existingEffect != null)
            {
                // Stack or refresh effect
                if (ShouldStackEffect(powerUpType))
                {
                    existingEffect.Intensity += activeEffect.Intensity * 0.5f; // Diminishing returns
                    existingEffect.RemainingDuration = Math.Max(existingEffect.RemainingDuration, activeEffect.RemainingDuration);
                }
                else
                {
                    existingEffect.RemainingDuration = activeEffect.RemainingDuration;
                    existingEffect.Intensity = activeEffect.Intensity;
                }
            }
            else
            {
                _activeEffects.Add(activeEffect);
            }
            
            // Check for chain effects
            CheckForChainEffects();
            
            OnPowerUpActivated?.Invoke(powerUpType, activeEffect.Intensity);
        }
        
        private bool ShouldStackEffect(AdvancedPowerUpType powerUpType)
        {
            return powerUpType switch
            {
                AdvancedPowerUpType.PowerAmplifier => true,
                AdvancedPowerUpType.CosmicResonance => true,
                AdvancedPowerUpType.WeaponEvolution => false, // Refresh instead
                AdvancedPowerUpType.QuantumShield => false,
                AdvancedPowerUpType.TeleportDash => false,
                AdvancedPowerUpType.NaniteIntegration => true,
                AdvancedPowerUpType.HolographicDecoys => true,
                _ => false
            };
        }
        
        private void CheckForChainEffects()
        {
            var activePowerUpTypes = _activeEffects.Select(e => e.PowerUpType).ToHashSet();
            
            foreach (var chain in _chainEffects.Values)
            {
                if (chain.IsActive) continue;
                
                bool canActivate = true;
                foreach (var requiredPowerUp in chain.RequiredPowerUps)
                {
                    if (!activePowerUpTypes.Contains(requiredPowerUp))
                    {
                        canActivate = false;
                        break;
                    }
                }
                
                if (canActivate)
                {
                    ActivateChainEffect(chain);
                }
            }
        }
        
        private void ActivateChainEffect(PowerUpChain chain)
        {
            chain.IsActive = true;
            chain.RemainingDuration = chain.Duration;
            
            // Apply chain effect bonuses
            foreach (var effect in chain.ChainEffects)
            {
                ApplyChainEffect(effect.Key, effect.Value);
            }
            
            OnChainEffectTriggered?.Invoke(chain);
        }
        
        private void ApplyChainEffect(string effectName, float intensity)
        {
            switch (effectName)
            {
                case "quantum_evolution_burst":
                    // Instantly evolve all weapons by one level
                    // This would be handled by the weapon system
                    break;
                    
                case "stellar_explosion":
                    // Create massive area damage
                    CreateStellarExplosion(intensity);
                    break;
                    
                case "reality_transcendence":
                    // Grant temporary god-mode with all abilities
                    ActivateTranscendenceMode(intensity);
                    break;
                    
                case "nanite_decoy_generation":
                    // Generate intelligent nanite decoys
                    CreateNaniteDecoys((int)intensity);
                    break;
            }
        }
        
        private AdvancedParticleSystem CreateParticleSystem(ParticleEffectType effectType)
        {
            // This would create appropriate particle systems based on effect type
            return new AdvancedParticleSystem(effectType);
        }
        
        private void CreateStellarExplosion(float intensity)
        {
            // Implementation for stellar explosion effect
            // Would create massive particle explosion and area damage
        }
        
        private void ActivateTranscendenceMode(float intensity)
        {
            // Implementation for transcendence mode
            // Temporary invincibility and enhanced abilities
        }
        
        private void CreateNaniteDecoys(int count)
        {
            // Implementation for nanite decoy creation
            // Generate intelligent holographic decoys with nanite behavior
        }
        
        public void Update(float deltaTime)
        {
            // Update active effects
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.RemainingDuration -= deltaTime;
                
                // Update particle system
                effect.ParticleSystem?.Update(deltaTime);
                
                if (effect.RemainingDuration <= 0)
                {
                    OnPowerUpExpired?.Invoke(effect.PowerUpType);
                    _activeEffects.RemoveAt(i);
                }
            }
            
            // Update chain effects
            foreach (var chain in _chainEffects.Values)
            {
                if (chain.IsActive)
                {
                    chain.RemainingDuration -= deltaTime;
                    if (chain.RemainingDuration <= 0)
                    {
                        chain.IsActive = false;
                    }
                }
            }
        }
        
        public void Render(Vector3 playerPosition)
        {
            // Render all active power-up effects
            foreach (var effect in _activeEffects)
            {
                RenderPowerUpEffect(effect, playerPosition);
            }
            
            // Render chain effects
            foreach (var chain in _chainEffects.Values)
            {
                if (chain.IsActive)
                {
                    RenderChainEffect(chain, playerPosition);
                }
            }
        }
        
        private void RenderPowerUpEffect(ActivePowerUpEffect effect, Vector3 position)
        {
            // Render particle system
            effect.ParticleSystem?.Render(position);
            
            // Render visual effects
            foreach (var visualEffect in effect.EffectData.VisualEffects)
            {
                RenderVisualEffect(visualEffect, position, effect.Intensity);
            }
            
            // Render power-up aura based on type
            var color = GetPowerUpColor(effect.PowerUpType);
            var alpha = (byte)(255 * Math.Sin(DateTime.Now.Millisecond / 1000.0 * Math.PI));
            color.a = alpha;
            
            Raylib.DrawSphereWires(position, 50f * effect.Intensity, 16, 16, color);
        }
        
        private void RenderChainEffect(PowerUpChain chain, Vector3 position)
        {
            // Render special chain effect visuals
            var chainColor = GetChainColor(chain.Rarity);
            var radius = 100f + (float)Math.Sin(DateTime.Now.Millisecond / 200.0) * 20f;
            
            Raylib.DrawSphereWires(position, radius, 32, 32, chainColor);
            
            // Draw connecting lines between power-up effects
            if (_activeEffects.Count > 1)
            {
                for (int i = 0; i < _activeEffects.Count - 1; i++)
                {
                    var startPos = position + new Vector3((float)Math.Cos(i * 2 * Math.PI / _activeEffects.Count) * 75f, 0, 
                                                         (float)Math.Sin(i * 2 * Math.PI / _activeEffects.Count) * 75f);
                    var endPos = position + new Vector3((float)Math.Cos((i + 1) * 2 * Math.PI / _activeEffects.Count) * 75f, 0,
                                                       (float)Math.Sin((i + 1) * 2 * Math.PI / _activeEffects.Count) * 75f);
                    
                    Raylib.DrawLine3D(startPos, endPos, chainColor);
                }
            }
        }
        
        private void RenderVisualEffect(string effectName, Vector3 position, float intensity)
        {
            switch (effectName)
            {
                case "weapon_glow_enhancement":
                    Raylib.DrawSphere(position, 10f * intensity, new Color(255, 255, 0, 128));
                    break;
                    
                case "quantum_barrier":
                    var time = DateTime.Now.Millisecond / 1000.0;
                    for (int i = 0; i < 8; i++)
                    {
                        var angle = i * Math.PI / 4 + time;
                        var offset = new Vector3((float)Math.Cos(angle) * 60f, (float)Math.Sin(time) * 10f, 
                                                (float)Math.Sin(angle) * 60f);
                        Raylib.DrawCube(position + offset, 5f, 5f, 5f, new Color(0, 255, 255, 100));
                    }
                    break;
                    
                case "cosmic_aura":
                    for (int i = 0; i < 12; i++)
                    {
                        var angle = i * Math.PI / 6;
                        var radius = 80f + (float)Math.Sin(DateTime.Now.Millisecond / 200.0 + i) * 20f;
                        var starPos = position + new Vector3((float)Math.Cos(angle) * radius, 0, 
                                                            (float)Math.Sin(angle) * radius);
                        Raylib.DrawSphere(starPos, 3f, new Color(255, 0, 255, 200));
                    }
                    break;
            }
        }
        
        private Color GetPowerUpColor(AdvancedPowerUpType powerUpType)
        {
            return powerUpType switch
            {
                AdvancedPowerUpType.WeaponEvolution => Color.GOLD,
                AdvancedPowerUpType.QuantumShield => Color.SKYBLUE,
                AdvancedPowerUpType.TeleportDash => Color.PURPLE,
                AdvancedPowerUpType.NaniteIntegration => Color.GREEN,
                AdvancedPowerUpType.CosmicResonance => Color.MAGENTA,
                AdvancedPowerUpType.HolographicDecoys => Color.BLUE,
                AdvancedPowerUpType.PowerAmplifier => Color.RED,
                _ => Color.WHITE
            };
        }
        
        private Color GetChainColor(ChainRarity rarity)
        {
            return rarity switch
            {
                ChainRarity.Uncommon => Color.GREEN,
                ChainRarity.Rare => Color.BLUE,
                ChainRarity.Epic => Color.PURPLE,
                ChainRarity.Legendary => Color.GOLD,
                _ => Color.WHITE
            };
        }
        
        public List<ActivePowerUpEffect> GetActiveEffects()
        {
            return new List<ActivePowerUpEffect>(_activeEffects);
        }
        
        public Dictionary<string, float> GetCombinedStatModifiers()
        {
            var combinedModifiers = new Dictionary<string, float>();
            
            foreach (var effect in _activeEffects)
            {
                foreach (var modifier in effect.EffectData.StatModifiers)
                {
                    if (combinedModifiers.ContainsKey(modifier.Key))
                    {
                        // Apply multiplicative stacking for most modifiers
                        if (modifier.Key.Contains("multiplier") || modifier.Key.Contains("amplification"))
                        {
                            combinedModifiers[modifier.Key] *= modifier.Value;
                        }
                        else
                        {
                            combinedModifiers[modifier.Key] += modifier.Value;
                        }
                    }
                    else
                    {
                        combinedModifiers[modifier.Key] = modifier.Value;
                    }
                }
            }
            
            return combinedModifiers;
        }
        
        public bool HasActivePowerUp(AdvancedPowerUpType powerUpType)
        {
            return _activeEffects.Any(e => e.PowerUpType == powerUpType);
        }
        
        public float GetPowerUpIntensity(AdvancedPowerUpType powerUpType)
        {
            var effect = _activeEffects.FirstOrDefault(e => e.PowerUpType == powerUpType);
            return effect?.Intensity ?? 0f;
        }
    }
    
    public class PowerUpEffectData
    {
        public float Duration { get; set; }
        public float Intensity { get; set; }
        public ParticleEffectType ParticleEffect { get; set; }
        public string AudioEffect { get; set; }
        public List<string> VisualEffects { get; set; }
        public Dictionary<string, float> StatModifiers { get; set; }
    }
    
    public class ActivePowerUpEffect
    {
        public AdvancedPowerUpType PowerUpType { get; set; }
        public float RemainingDuration { get; set; }
        public float Intensity { get; set; }
        public PowerUpEffectData EffectData { get; set; }
        public AdvancedParticleSystem ParticleSystem { get; set; }
        public DateTime ActivationTime { get; set; }
    }
    
    public class PowerUpChain
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<AdvancedPowerUpType> RequiredPowerUps { get; set; }
        public Dictionary<string, float> ChainEffects { get; set; }
        public float Duration { get; set; }
        public ChainRarity Rarity { get; set; }
        public bool IsActive { get; set; }
        public float RemainingDuration { get; set; }
    }
    
    public enum ChainRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum ParticleEffectType
    {
        EvolutionSpiral,
        QuantumDistortion,
        TeleportFlash,
        NaniteCloud,
        CosmicEnergy,
        HologramFlicker,
        PowerSurge,
        ChainReaction
    }
    
    public class AdvancedParticleSystem
    {
        private ParticleEffectType _effectType;
        private List<Particle> _particles;
        private Random _random;
        
        public AdvancedParticleSystem(ParticleEffectType effectType)
        {
            _effectType = effectType;
            _particles = new List<Particle>();
            _random = new Random();
        }
        
        public void Update(float deltaTime)
        {
            // Update particle system based on effect type
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(deltaTime);
                if (_particles[i].Life <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
            
            // Emit new particles
            EmitParticles();
        }
        
        private void EmitParticles()
        {
            int emissionRate = _effectType switch
            {
                ParticleEffectType.EvolutionSpiral => 5,
                ParticleEffectType.QuantumDistortion => 8,
                ParticleEffectType.CosmicEnergy => 12,
                _ => 3
            };
            
            for (int i = 0; i < emissionRate; i++)
            {
                _particles.Add(CreateParticle());
            }
        }
        
        private Particle CreateParticle()
        {
            return _effectType switch
            {
                ParticleEffectType.EvolutionSpiral => CreateEvolutionParticle(),
                ParticleEffectType.QuantumDistortion => CreateQuantumParticle(),
                ParticleEffectType.CosmicEnergy => CreateCosmicParticle(),
                _ => new Particle()
            };
        }
        
        private Particle CreateEvolutionParticle()
        {
            return new Particle
            {
                Position = Vector3.Zero,
                Velocity = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 100f,
                    (float)(_random.NextDouble()) * 50f,
                    (float)(_random.NextDouble() - 0.5) * 100f
                ),
                Color = Color.GOLD,
                Life = 2.0f,
                Size = 2.0f
            };
        }
        
        private Particle CreateQuantumParticle()
        {
            return new Particle
            {
                Position = Vector3.Zero,
                Velocity = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 200f,
                    (float)(_random.NextDouble() - 0.5) * 200f,
                    (float)(_random.NextDouble() - 0.5) * 200f
                ),
                Color = Color.SKYBLUE,
                Life = 1.5f,
                Size = 1.5f
            };
        }
        
        private Particle CreateCosmicParticle()
        {
            return new Particle
            {
                Position = Vector3.Zero,
                Velocity = new Vector3(
                    (float)(_random.NextDouble() - 0.5) * 150f,
                    (float)(_random.NextDouble()) * 100f,
                    (float)(_random.NextDouble() - 0.5) * 150f
                ),
                Color = Color.MAGENTA,
                Life = 3.0f,
                Size = 3.0f
            };
        }
        
        public void Render(Vector3 origin)
        {
            foreach (var particle in _particles)
            {
                var worldPos = origin + particle.Position;
                var alpha = (byte)(255 * (particle.Life / 3.0f));
                var color = particle.Color;
                color.a = alpha;
                
                Raylib.DrawSphere(worldPos, particle.Size, color);
            }
        }
    }
    
    public class Particle
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Color Color { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        
        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
            Life -= deltaTime;
            
            // Apply some physics
            Velocity *= 0.98f; // Damping
        }
    }
}