using System;
using System.Collections.Generic;
using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Communication;
using Asteroids.AI.Formations;
using Asteroids.AI.Navigation;

namespace Asteroids.AI.Core
{
    /// <summary>
    /// Base class for all AI-controlled enemy ships
    /// </summary>
    public abstract class AIEnemyShip
    {
        // Core properties
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Forward { get; set; } = Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public Vector3 Right => Vector3.Cross(Forward, Up);
        
        public float Health { get; set; }
        public float MaxHealth { get; protected set; }
        public float Speed { get; protected set; }
        public float RotationSpeed { get; protected set; }
        public float DetectionRange { get; protected set; }
        public float AttackRange { get; protected set; }
        public float Size { get; protected set; }
        
        // AI components
        public AIStateMachine StateMachine { get; private set; }
        public AINavigator Navigator { get; private set; }
        public CommunicationSystem Communication { get; private set; }
        public FormationMember FormationMember { get; private set; }
        
        // Targeting and combat
        public AIEnemyShip Target { get; set; }
        public Vector3 LastKnownPlayerPosition { get; set; }
        public float LastPlayerSightingTime { get; set; }
        public float TimeSinceLastAttack { get; set; }
        
        // Formation and swarm
        public FormationController Formation { get; set; }
        public List<AIEnemyShip> NearbyAllies { get; private set; }
        public int FormationIndex { get; set; } = -1;
        
        // AI behavior parameters
        public float Aggressiveness { get; set; } = 0.5f;
        public float Caution { get; set; } = 0.5f;
        public float TeamworkTendency { get; set; } = 0.7f;
        public AIPersonality Personality { get; set; }
        
        // Events
        public event Action<AIEnemyShip> OnDestroyed;
        public event Action<AIEnemyShip, Vector3> OnDamaged;
        public event Action<AIEnemyShip, AIEnemyShip> OnTargetAcquired;
        
        protected AIEnemyShip()
        {
            StateMachine = new AIStateMachine();
            Navigator = new AINavigator(this);
            Communication = new CommunicationSystem();
            FormationMember = new FormationMember(this);
            NearbyAllies = new List<AIEnemyShip>();
            
            InitializeAI();
        }
        
        protected abstract void InitializeAI();
        public abstract EnemyShipType ShipType { get; }
        public abstract float GetPreferredEngagementRange();
        public abstract bool CanAttack();
        public abstract void Attack(AIEnemyShip target);
        
        public virtual void Update(float deltaTime)
        {
            // Update AI state machine
            StateMachine.Update(this, deltaTime);
            
            // Update navigation
            Navigator.Update(deltaTime);
            
            // Update formation member
            FormationMember.Update(deltaTime);
            
            // Update communication
            Communication.Update(deltaTime);
            
            // Update timers
            TimeSinceLastAttack += deltaTime;
            
            // Apply physics
            Position += Velocity * deltaTime;
            
            // Clamp to world bounds (implement as needed)
            // ClampToWorldBounds();
        }
        
        public virtual void TakeDamage(float damage, Vector3 damageSource)
        {
            Health -= damage;
            OnDamaged?.Invoke(this, damageSource);
            
            if (Health <= 0)
            {
                Destroy();
            }
            
            // React to taking damage
            if (StateMachine.CurrentState is not FleeState)
            {
                // Consider fleeing if heavily damaged
                if (Health / MaxHealth < 0.3f && Caution > 0.5f)
                {
                    StateMachine.ChangeState(new FleeState(), this);
                }
                // Or become more aggressive
                else if (Aggressiveness > 0.7f)
                {
                    Aggressiveness = Math.Min(1.0f, Aggressiveness + 0.2f);
                }
            }
        }
        
        public virtual void Destroy()
        {
            OnDestroyed?.Invoke(this);
            
            // Notify formation of destruction
            Formation?.RemoveMember(this);
            
            // Notify allies
            foreach (var ally in NearbyAllies)
            {
                ally.Communication.SendMessage(new AIMessage
                {
                    Type = MessageType.AllyDestroyed,
                    Sender = this,
                    Position = Position,
                    Data = this
                });
            }
        }
        
        public void SetTarget(AIEnemyShip newTarget)
        {
            if (Target != newTarget)
            {
                Target = newTarget;
                OnTargetAcquired?.Invoke(this, newTarget);
                
                if (newTarget != null)
                {
                    LastKnownPlayerPosition = newTarget.Position;
                    LastPlayerSightingTime = 0;
                }
            }
        }
        
        public virtual void LookAt(Vector3 targetPosition)
        {
            var direction = Vector3.Normalize(targetPosition - Position);
            if (direction.LengthSquared() > 0.001f)
            {
                Forward = Vector3.Lerp(Forward, direction, RotationSpeed * 0.016f);
                Forward = Vector3.Normalize(Forward);
                
                // Update up vector to maintain orthogonality
                Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
            }
        }
        
        public virtual void MoveToward(Vector3 targetPosition, float speedMultiplier = 1.0f)
        {
            var direction = Vector3.Normalize(targetPosition - Position);
            Velocity = direction * Speed * speedMultiplier;
        }
        
        public virtual void Stop()
        {
            Velocity = Vector3.Zero;
        }
        
        public float DistanceTo(Vector3 point)
        {
            return Vector3.Distance(Position, point);
        }
        
        public float DistanceTo(AIEnemyShip other)
        {
            return other != null ? DistanceTo(other.Position) : float.MaxValue;
        }
        
        public bool IsInRange(Vector3 point, float range)
        {
            return DistanceTo(point) <= range;
        }
        
        public bool CanSee(Vector3 point, float maxDistance = -1)
        {
            var distance = DistanceTo(point);
            var range = maxDistance > 0 ? maxDistance : DetectionRange;
            
            if (distance > range) return false;
            
            // Check if point is within field of view
            var direction = Vector3.Normalize(point - Position);
            var dot = Vector3.Dot(Forward, direction);
            
            return dot > 0.3f; // ~70 degree field of view
        }
        
        public void UpdateNearbyAllies(List<AIEnemyShip> allAllies, float searchRadius = 100f)
        {
            NearbyAllies.Clear();
            
            foreach (var ally in allAllies)
            {
                if (ally != this && DistanceTo(ally) <= searchRadius)
                {
                    NearbyAllies.Add(ally);
                }
            }
        }
        
        public Vector3 GetAverageAllyPosition()
        {
            if (NearbyAllies.Count == 0) return Position;
            
            var sum = Vector3.Zero;
            foreach (var ally in NearbyAllies)
            {
                sum += ally.Position;
            }
            
            return sum / NearbyAllies.Count;
        }
        
        public Vector3 GetAverageAllyVelocity()
        {
            if (NearbyAllies.Count == 0) return Vector3.Zero;
            
            var sum = Vector3.Zero;
            foreach (var ally in NearbyAllies)
            {
                sum += ally.Velocity;
            }
            
            return sum / NearbyAllies.Count;
        }
    }
    
    public enum EnemyShipType
    {
        Scout,
        Fighter,
        Bomber,
        Interceptor,
        Commander
    }
    
    public enum AIPersonality
    {
        Aggressive,
        Defensive,
        Tactical,
        Reckless,
        Cautious,
        Balanced
    }
}