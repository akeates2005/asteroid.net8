using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Manages all status effects applied to entities in the game
    /// </summary>
    public class StatusEffectManager
    {
        private Dictionary<int, List<StatusEffect>> _entityStatusEffects;
        private List<AreaStatusEffect> _areaEffects;
        private Dictionary<StatusEffectType, StatusEffectDefinition> _effectDefinitions;
        private Random _random;
        
        // Visual effect tracking
        private Dictionary<int, List<StatusVisualEffect>> _visualEffects;
        
        public event Action<int, StatusEffectType> OnStatusEffectApplied;
        public event Action<int, StatusEffectType> OnStatusEffectRemoved;
        public event Action<int, StatusEffectType, float> OnStatusEffectDamage;
        
        public StatusEffectManager()
        {
            _entityStatusEffects = new Dictionary<int, List<StatusEffect>>();
            _areaEffects = new List<AreaStatusEffect>();
            _effectDefinitions = new Dictionary<StatusEffectType, StatusEffectDefinition>();
            _visualEffects = new Dictionary<int, List<StatusVisualEffect>>();
            _random = new Random();
            
            InitializeEffectDefinitions();
        }
        
        private void InitializeEffectDefinitions()
        {
            // Define all status effects and their behaviors
            _effectDefinitions[StatusEffectType.Burning] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Burning,
                Category = StatusEffectCategory.Debuff,
                DamagePerSecond = 10f,
                StackingBehavior = StackingType.Intensity,
                MaxStacks = 5,
                VisualEffect = StatusVisualType.Fire,
                Color = Color.Orange,
                IsDispellable = true,
                Priority = 3
            };
            
            _effectDefinitions[StatusEffectType.Frozen] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Frozen,
                Category = StatusEffectCategory.Debuff,
                MovementSpeedMultiplier = 0f,
                AttackSpeedMultiplier = 0f,
                StackingBehavior = StackingType.Duration,
                MaxStacks = 1,
                VisualEffect = StatusVisualType.Ice,
                Color = Color.LightBlue,
                IsDispellable = true,
                Priority = 8
            };
            
            _effectDefinitions[StatusEffectType.Slowed] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Slowed,
                Category = StatusEffectCategory.Debuff,
                MovementSpeedMultiplier = 0.5f,
                AttackSpeedMultiplier = 0.7f,
                StackingBehavior = StackingType.Intensity,
                MaxStacks = 3,
                VisualEffect = StatusVisualType.Slow,
                Color = Color.Blue,
                IsDispellable = true,
                Priority = 2
            };
            
            _effectDefinitions[StatusEffectType.Regeneration] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Regeneration,
                Category = StatusEffectCategory.Buff,
                HealingPerSecond = 15f,
                StackingBehavior = StackingType.Intensity,
                MaxStacks = 3,
                VisualEffect = StatusVisualType.Healing,
                Color = Color.Green,
                IsDispellable = false,
                Priority = 5
            };
            
            _effectDefinitions[StatusEffectType.DamageBoost] = new StatusEffectDefinition
            {
                Type = StatusEffectType.DamageBoost,
                Category = StatusEffectCategory.Buff,
                DamageMultiplier = 1.5f,
                StackingBehavior = StackingType.Intensity,
                MaxStacks = 3,
                VisualEffect = StatusVisualType.Power,
                Color = Color.Red,
                IsDispellable = false,
                Priority = 6
            };
            
            _effectDefinitions[StatusEffectType.Quantum] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Quantum,
                Category = StatusEffectCategory.Special,
                StackingBehavior = StackingType.Independent,
                MaxStacks = 1,
                VisualEffect = StatusVisualType.Quantum,
                Color = Color.Purple,
                IsDispellable = false,
                Priority = 9,
                HasSpecialBehavior = true
            };
            
            _effectDefinitions[StatusEffectType.Electrified] = new StatusEffectDefinition
            {
                Type = StatusEffectType.Electrified,
                Category = StatusEffectCategory.Debuff,
                DamagePerSecond = 8f,
                StackingBehavior = StackingType.Duration,
                MaxStacks = 1,
                VisualEffect = StatusVisualType.Electric,
                Color = Color.Yellow,
                IsDispellable = true,
                Priority = 4,
                HasChainEffect = true,
                ChainRadius = 10f,
                ChainDamage = 0.6f
            };
            
            // Add more effect definitions as needed...
        }
        
        public void Update(float deltaTime)
        {
            UpdateEntityStatusEffects(deltaTime);
            UpdateAreaStatusEffects(deltaTime);
            UpdateVisualEffects(deltaTime);
        }
        
        private void UpdateEntityStatusEffects(float deltaTime)
        {
            var entitiesToRemove = new List<int>();
            
            foreach (var entityEntry in _entityStatusEffects)
            {
                int entityId = entityEntry.Key;
                var effects = entityEntry.Value;
                
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];
                    effect.Update(deltaTime);
                    
                    // Apply effect damage/healing
                    ApplyEffectTick(entityId, effect, deltaTime);
                    
                    // Handle special behaviors
                    ProcessSpecialBehavior(entityId, effect, deltaTime);
                    
                    if (!effect.IsActive)
                    {
                        OnStatusEffectRemoved?.Invoke(entityId, effect.Type);
                        RemoveVisualEffect(entityId, effect.Type);
                        effects.RemoveAt(i);
                    }
                }
                
                if (effects.Count == 0)
                {
                    entitiesToRemove.Add(entityId);
                }
            }
            
            foreach (int entityId in entitiesToRemove)
            {
                _entityStatusEffects.Remove(entityId);
                _visualEffects.Remove(entityId);
            }
        }
        
        private void UpdateAreaStatusEffects(float deltaTime)
        {
            for (int i = _areaEffects.Count - 1; i >= 0; i--)
            {
                _areaEffects[i].Update(deltaTime);
                if (!_areaEffects[i].IsActive)
                {
                    _areaEffects.RemoveAt(i);
                }
            }
        }
        
        private void UpdateVisualEffects(float deltaTime)
        {
            foreach (var entityEffects in _visualEffects.Values)
            {
                for (int i = entityEffects.Count - 1; i >= 0; i--)
                {
                    entityEffects[i].Update(deltaTime);
                    if (!entityEffects[i].IsActive)
                    {
                        entityEffects.RemoveAt(i);
                    }
                }
            }
        }
        
        public void ApplyStatusEffect(int entityId, StatusEffect effect)
        {
            if (!_entityStatusEffects.ContainsKey(entityId))
            {
                _entityStatusEffects[entityId] = new List<StatusEffect>();
            }
            
            var existingEffects = _entityStatusEffects[entityId];
            var definition = _effectDefinitions[effect.Type];
            
            // Handle stacking behavior
            var existingEffect = existingEffects.FirstOrDefault(e => e.Type == effect.Type);
            
            if (existingEffect != null)
            {
                switch (definition.StackingBehavior)
                {
                    case StackingType.Refresh:
                        existingEffect.RefreshDuration(effect.Duration);
                        break;
                        
                    case StackingType.Duration:
                        existingEffect.AddDuration(effect.Duration);
                        break;
                        
                    case StackingType.Intensity:
                        if (existingEffect.StackCount < definition.MaxStacks)
                        {
                            existingEffect.AddStack(effect.Strength);
                        }
                        else
                        {
                            existingEffect.RefreshDuration(effect.Duration);
                        }
                        break;
                        
                    case StackingType.Independent:
                        existingEffects.Add(effect);
                        break;
                        
                    case StackingType.Replace:
                        existingEffects.Remove(existingEffect);
                        existingEffects.Add(effect);
                        break;
                }
            }
            else
            {
                existingEffects.Add(effect);
            }
            
            // Sort effects by priority
            existingEffects.Sort((a, b) => _effectDefinitions[b.Type].Priority.CompareTo(_effectDefinitions[a.Type].Priority));
            
            OnStatusEffectApplied?.Invoke(entityId, effect.Type);
            AddVisualEffect(entityId, effect.Type, definition);
        }
        
        public void ApplyStatusEffect(StatusEffect effect)
        {
            ApplyStatusEffect(0, effect); // Apply to default entity (player)
        }
        
        public void ApplyAreaStatusEffect(Vector3 center, float radius, StatusEffectType effectType, 
                                        float duration, float strength, bool affectsPlayer = false)
        {
            var areaEffect = new AreaStatusEffect
            {
                Center = center,
                Radius = radius,
                EffectType = effectType,
                Duration = duration,
                Strength = strength,
                AffectsPlayer = affectsPlayer,
                RemainingTime = duration,
                IsActive = true,
                TickRate = 0.5f, // Apply effect every 0.5 seconds
                LastTickTime = GetTime()
            };
            
            _areaEffects.Add(areaEffect);
        }
        
        private void ApplyEffectTick(int entityId, StatusEffect effect, float deltaTime)
        {
            var definition = _effectDefinitions[effect.Type];
            
            // Damage over time
            if (definition.DamagePerSecond > 0)
            {
                float damage = definition.DamagePerSecond * effect.GetEffectiveStrength() * deltaTime;
                OnStatusEffectDamage?.Invoke(entityId, effect.Type, damage);
            }
            
            // Healing over time
            if (definition.HealingPerSecond > 0)
            {
                float healing = -definition.HealingPerSecond * effect.GetEffectiveStrength() * deltaTime;
                OnStatusEffectDamage?.Invoke(entityId, effect.Type, healing); // Negative damage = healing
            }
        }
        
        private void ProcessSpecialBehavior(int entityId, StatusEffect effect, float deltaTime)
        {
            var definition = _effectDefinitions[effect.Type];
            
            if (!definition.HasSpecialBehavior) return;
            
            switch (effect.Type)
            {
                case StatusEffectType.Quantum:
                    ProcessQuantumBehavior(entityId, effect, deltaTime);
                    break;
                    
                case StatusEffectType.Electrified:
                    if (definition.HasChainEffect)
                    {
                        ProcessChainLightning(entityId, effect, definition);
                    }
                    break;
                    
                case StatusEffectType.Burning:
                    ProcessBurningSpread(entityId, effect, deltaTime);
                    break;
            }
        }
        
        private void ProcessQuantumBehavior(int entityId, StatusEffect effect, float deltaTime)
        {
            // Quantum effects cause random teleportation and phase shifts
            if (_random.NextSingle() < 0.1f * deltaTime) // 10% chance per second
            {
                // Trigger quantum event
                CreateQuantumEffect(entityId, effect.SourcePosition);
            }
        }
        
        private void ProcessChainLightning(int entityId, StatusEffect effect, StatusEffectDefinition definition)
        {
            // Chain lightning spreads to nearby entities
            var nearbyEntities = GetNearbyEntities(effect.SourcePosition, definition.ChainRadius);
            
            foreach (int nearbyEntity in nearbyEntities)
            {
                if (nearbyEntity != entityId && !HasStatusEffect(nearbyEntity, StatusEffectType.Electrified))
                {
                    var chainEffect = new StatusEffect(
                        StatusEffectType.Electrified,
                        effect.Duration * 0.8f,
                        effect.Strength * definition.ChainDamage,
                        effect.SourcePosition
                    );
                    
                    ApplyStatusEffect(nearbyEntity, chainEffect);
                    break; // Only chain to one entity per tick
                }
            }
        }
        
        private void ProcessBurningSpread(int entityId, StatusEffect effect, float deltaTime)
        {
            // Burning can spread to nearby flammable objects
            if (_random.NextSingle() < 0.05f * deltaTime) // 5% chance per second
            {
                var nearbyEntities = GetNearbyEntities(effect.SourcePosition, 5f);
                
                foreach (int nearbyEntity in nearbyEntities)
                {
                    if (nearbyEntity != entityId && _random.NextSingle() < 0.3f)
                    {
                        var spreadEffect = new StatusEffect(
                            StatusEffectType.Burning,
                            effect.Duration * 0.6f,
                            effect.Strength * 0.7f,
                            effect.SourcePosition
                        );
                        
                        ApplyStatusEffect(nearbyEntity, spreadEffect);
                    }
                }
            }
        }
        
        public void RemoveStatusEffect(int entityId, StatusEffectType effectType)
        {
            if (_entityStatusEffects.TryGetValue(entityId, out var effects))
            {
                var effectToRemove = effects.FirstOrDefault(e => e.Type == effectType);
                if (effectToRemove != null)
                {
                    effects.Remove(effectToRemove);
                    OnStatusEffectRemoved?.Invoke(entityId, effectType);
                    RemoveVisualEffect(entityId, effectType);
                }
            }
        }
        
        public void RemoveAllStatusEffects(int entityId)
        {
            if (_entityStatusEffects.TryGetValue(entityId, out var effects))
            {
                foreach (var effect in effects)
                {
                    OnStatusEffectRemoved?.Invoke(entityId, effect.Type);
                }
                effects.Clear();
                
                if (_visualEffects.TryGetValue(entityId, out var visualEffects))
                {
                    visualEffects.Clear();
                }
            }
        }
        
        public void DispelDebuffs(int entityId)
        {
            if (!_entityStatusEffects.TryGetValue(entityId, out var effects)) return;
            
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                var definition = _effectDefinitions[effect.Type];
                
                if (definition.Category == StatusEffectCategory.Debuff && definition.IsDispellable)
                {
                    OnStatusEffectRemoved?.Invoke(entityId, effect.Type);
                    RemoveVisualEffect(entityId, effect.Type);
                    effects.RemoveAt(i);
                }
            }
        }
        
        public bool HasStatusEffect(int entityId, StatusEffectType effectType)
        {
            return _entityStatusEffects.TryGetValue(entityId, out var effects) &&
                   effects.Any(e => e.Type == effectType && e.IsActive);
        }
        
        public StatusEffect GetStatusEffect(int entityId, StatusEffectType effectType)
        {
            if (_entityStatusEffects.TryGetValue(entityId, out var effects))
            {
                return effects.FirstOrDefault(e => e.Type == effectType && e.IsActive);
            }
            return null;
        }
        
        public List<StatusEffect> GetAllStatusEffects(int entityId)
        {
            return _entityStatusEffects.TryGetValue(entityId, out var effects) 
                ? effects.Where(e => e.IsActive).ToList() 
                : new List<StatusEffect>();
        }
        
        public StatusEffectModifiers CalculateModifiers(int entityId)
        {
            var modifiers = new StatusEffectModifiers();
            
            if (!_entityStatusEffects.TryGetValue(entityId, out var effects)) 
                return modifiers;
            
            foreach (var effect in effects.Where(e => e.IsActive))
            {
                var definition = _effectDefinitions[effect.Type];
                float effectStrength = effect.GetEffectiveStrength();
                
                // Apply modifiers
                if (definition.DamageMultiplier != 1f)
                {
                    modifiers.DamageMultiplier *= MathF.Pow(definition.DamageMultiplier, effectStrength);
                }
                
                if (definition.MovementSpeedMultiplier != 1f)
                {
                    modifiers.MovementSpeedMultiplier *= MathF.Pow(definition.MovementSpeedMultiplier, effectStrength);
                }
                
                if (definition.AttackSpeedMultiplier != 1f)
                {
                    modifiers.AttackSpeedMultiplier *= MathF.Pow(definition.AttackSpeedMultiplier, effectStrength);
                }
            }
            
            return modifiers;
        }
        
        private void AddVisualEffect(int entityId, StatusEffectType effectType, StatusEffectDefinition definition)
        {
            if (!_visualEffects.ContainsKey(entityId))
            {
                _visualEffects[entityId] = new List<StatusVisualEffect>();
            }
            
            var visualEffect = new StatusVisualEffect
            {
                Type = effectType,
                VisualType = definition.VisualEffect,
                Color = definition.Color,
                Intensity = 1f,
                IsActive = true
            };
            
            _visualEffects[entityId].Add(visualEffect);
        }
        
        private void RemoveVisualEffect(int entityId, StatusEffectType effectType)
        {
            if (_visualEffects.TryGetValue(entityId, out var effects))
            {
                var visualEffect = effects.FirstOrDefault(e => e.Type == effectType);
                if (visualEffect != null)
                {
                    visualEffect.IsActive = false;
                }
            }
        }
        
        public void Draw(Camera3D camera)
        {
            // Draw area effects
            foreach (var areaEffect in _areaEffects)
            {
                DrawAreaEffect(camera, areaEffect);
            }
            
            // Draw entity visual effects
            foreach (var entityEffects in _visualEffects)
            {
                int entityId = entityEffects.Key;
                var effects = entityEffects.Value;
                
                // Get entity position (this would come from game's entity system)
                Vector3 entityPosition = GetEntityPosition(entityId);
                
                foreach (var effect in effects.Where(e => e.IsActive))
                {
                    DrawStatusEffect(camera, entityPosition, effect);
                }
            }
        }
        
        private void DrawAreaEffect(Camera3D camera, AreaStatusEffect areaEffect)
        {
            var definition = _effectDefinitions[areaEffect.EffectType];
            Color effectColor = definition.Color;
            effectColor.A = (byte)(100 * (areaEffect.RemainingTime / areaEffect.Duration));
            
            // Draw area circle
            Raylib.DrawSphereWires(areaEffect.Center, areaEffect.Radius, effectColor);
            
            // Draw effect-specific visuals
            switch (definition.VisualEffect)
            {
                case StatusVisualType.Fire:
                    DrawFireAreaEffect(camera, areaEffect.Center, areaEffect.Radius, effectColor);
                    break;
                    
                case StatusVisualType.Ice:
                    DrawIceAreaEffect(camera, areaEffect.Center, areaEffect.Radius, effectColor);
                    break;
                    
                case StatusVisualType.Electric:
                    DrawElectricAreaEffect(camera, areaEffect.Center, areaEffect.Radius, effectColor);
                    break;
            }
        }
        
        private void DrawStatusEffect(Camera3D camera, Vector3 position, StatusVisualEffect effect)
        {
            switch (effect.VisualType)
            {
                case StatusVisualType.Fire:
                    DrawFireEffect(camera, position, effect.Color, effect.Intensity);
                    break;
                    
                case StatusVisualType.Ice:
                    DrawIceEffect(camera, position, effect.Color, effect.Intensity);
                    break;
                    
                case StatusVisualType.Electric:
                    DrawElectricEffect(camera, position, effect.Color, effect.Intensity);
                    break;
                    
                case StatusVisualType.Healing:
                    DrawHealingEffect(camera, position, effect.Color, effect.Intensity);
                    break;
                    
                case StatusVisualType.Power:
                    DrawPowerEffect(camera, position, effect.Color, effect.Intensity);
                    break;
                    
                case StatusVisualType.Quantum:
                    DrawQuantumEffect(camera, position, effect.Color, effect.Intensity);
                    break;
            }
        }
        
        // Visual effect drawing methods (implementations would depend on specific requirements)
        private void DrawFireEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        private void DrawIceEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        private void DrawElectricEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        private void DrawHealingEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        private void DrawPowerEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        private void DrawQuantumEffect(Camera3D camera, Vector3 position, Color color, float intensity) { }
        
        private void DrawFireAreaEffect(Camera3D camera, Vector3 center, float radius, Color color) { }
        private void DrawIceAreaEffect(Camera3D camera, Vector3 center, float radius, Color color) { }
        private void DrawElectricAreaEffect(Camera3D camera, Vector3 center, float radius, Color color) { }
        
        // Helper methods
        private List<int> GetNearbyEntities(Vector3 position, float radius)
        {
            // This would interface with the game's entity system
            return new List<int>();
        }
        
        private Vector3 GetEntityPosition(int entityId)
        {
            // This would interface with the game's entity system
            return Vector3.Zero;
        }
        
        private void CreateQuantumEffect(int entityId, Vector3 position)
        {
            // Create quantum-specific effects
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    // Supporting data structures and enums...
    public enum StatusEffectCategory
    {
        Buff,
        Debuff,
        Special,
        Neutral
    }
    
    public enum StackingType
    {
        Refresh,      // Refreshes duration, no stacking
        Duration,     // Stacks duration
        Intensity,    // Stacks intensity/strength
        Independent,  // Each application is independent
        Replace       // New application replaces old
    }
    
    public enum StatusVisualType
    {
        Fire,
        Ice,
        Electric,
        Healing,
        Power,
        Quantum,
        Slow,
        Default
    }
    
    public struct StatusEffectDefinition
    {
        public StatusEffectType Type;
        public StatusEffectCategory Category;
        public float DamagePerSecond;
        public float HealingPerSecond;
        public float DamageMultiplier;
        public float MovementSpeedMultiplier;
        public float AttackSpeedMultiplier;
        public StackingType StackingBehavior;
        public int MaxStacks;
        public StatusVisualType VisualEffect;
        public Color Color;
        public bool IsDispellable;
        public int Priority;
        public bool HasSpecialBehavior;
        public bool HasChainEffect;
        public float ChainRadius;
        public float ChainDamage;
    }
    
    public struct StatusEffectModifiers
    {
        public float DamageMultiplier;
        public float MovementSpeedMultiplier;
        public float AttackSpeedMultiplier;
        public float AccuracyMultiplier;
        public float RangeMultiplier;
        
        public StatusEffectModifiers()
        {
            DamageMultiplier = 1f;
            MovementSpeedMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            AccuracyMultiplier = 1f;
            RangeMultiplier = 1f;
        }
    }
    
    public class AreaStatusEffect
    {
        public Vector3 Center { get; set; }
        public float Radius { get; set; }
        public StatusEffectType EffectType { get; set; }
        public float Duration { get; set; }
        public float Strength { get; set; }
        public bool AffectsPlayer { get; set; }
        public float RemainingTime { get; set; }
        public bool IsActive { get; set; }
        public float TickRate { get; set; }
        public float LastTickTime { get; set; }
        
        public void Update(float deltaTime)
        {
            if (!IsActive) return;
            
            RemainingTime -= deltaTime;
            if (RemainingTime <= 0)
            {
                IsActive = false;
            }
        }
    }
    
    public class StatusVisualEffect
    {
        public StatusEffectType Type { get; set; }
        public StatusVisualType VisualType { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public bool IsActive { get; set; }
        
        public void Update(float deltaTime)
        {
            // Update visual effect properties like intensity pulsing
            if (IsActive)
            {
                Intensity = 0.8f + MathF.Sin((float)Raylib.GetTime() * 4f) * 0.2f;
            }
        }
    }
}