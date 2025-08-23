using System;
using System.Numerics;
using Raylib_cs;

namespace Asteroids.Weapons
{
    /// <summary>
    /// Individual nanite unit for nanite swarm weapons
    /// </summary>
    public class NaniteUnit
    {
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool IsActive { get; private set; }
        public float Energy { get; private set; }
        public NaniteBehaviorState State { get; private set; }
        
        private Random _random;
        private Vector3? _targetPosition;
        private Vector3 _swarmCenter;
        private float _orbitRadius;
        private float _orbitSpeed;
        private float _orbitAngle;
        private float _maxEnergy;
        private float _replicationCooldown;
        private float _attackCooldown;
        private Color _currentColor;
        
        public NaniteUnit(Vector3 initialPosition, Random random)
        {
            Position = initialPosition;
            Velocity = Vector3.Zero;
            IsActive = true;
            _maxEnergy = 100f;
            Energy = _maxEnergy;
            State = NaniteBehaviorState.Swarming;
            
            _random = random;
            _swarmCenter = initialPosition;
            _orbitRadius = (float)(_random.NextDouble() * 2f + 1f); // 1-3 unit radius
            _orbitSpeed = (float)(_random.NextDouble() * 2f + 1f); // 1-3 rad/sec
            _orbitAngle = (float)(_random.NextDouble() * Math.PI * 2f);
            _replicationCooldown = 0f;
            _attackCooldown = 0f;
            _currentColor = Color.Green;
        }
        
        public void Update(float deltaTime, Vector3 swarmCenter)
        {
            if (!IsActive) return;
            
            _swarmCenter = swarmCenter;
            UpdateBehaviorState(deltaTime);
            UpdateMovement(deltaTime);
            UpdateEnergy(deltaTime);
            UpdateCooldowns(deltaTime);
            UpdateVisuals(deltaTime);
        }
        
        private void UpdateBehaviorState(float deltaTime)
        {
            switch (State)
            {
                case NaniteBehaviorState.Swarming:
                    // Default behavior - orbit around swarm center
                    if (_targetPosition.HasValue)
                    {
                        State = NaniteBehaviorState.Hunting;
                    }
                    break;
                    
                case NaniteBehaviorState.Hunting:
                    // Move towards target
                    if (!_targetPosition.HasValue)
                    {
                        State = NaniteBehaviorState.Swarming;
                    }
                    else if (Vector3.Distance(Position, _targetPosition.Value) < 1f)
                    {
                        State = NaniteBehaviorState.Attacking;
                    }
                    break;
                    
                case NaniteBehaviorState.Attacking:
                    // Attack target, then return to swarming
                    if (_attackCooldown <= 0)
                    {
                        PerformAttack();
                        _attackCooldown = 0.5f; // Half second attack cooldown
                    }
                    
                    // Return to swarming after attack
                    if (_attackCooldown <= 0.3f)
                    {
                        State = NaniteBehaviorState.Swarming;
                        _targetPosition = null;
                    }
                    break;
                    
                case NaniteBehaviorState.Replicating:
                    // Prepare for replication
                    if (_replicationCooldown <= 0)
                    {
                        State = NaniteBehaviorState.Swarming;
                    }
                    break;
                    
                case NaniteBehaviorState.Depleted:
                    // Low energy, move slowly
                    if (Energy > 20f)
                    {
                        State = NaniteBehaviorState.Swarming;
                    }
                    break;
            }
        }
        
        private void UpdateMovement(float deltaTime)
        {
            switch (State)
            {
                case NaniteBehaviorState.Swarming:
                    UpdateSwarmMovement(deltaTime);
                    break;
                    
                case NaniteBehaviorState.Hunting:
                    UpdateHuntingMovement(deltaTime);
                    break;
                    
                case NaniteBehaviorState.Attacking:
                    UpdateAttackMovement(deltaTime);
                    break;
                    
                case NaniteBehaviorState.Replicating:
                    UpdateReplicationMovement(deltaTime);
                    break;
                    
                case NaniteBehaviorState.Depleted:
                    UpdateDepletedMovement(deltaTime);
                    break;
            }
            
            // Apply movement
            Position += Velocity * deltaTime;
            
            // Add some random jitter
            Vector3 jitter = new Vector3(
                (float)(_random.NextDouble() - 0.5) * 0.1f,
                (float)(_random.NextDouble() - 0.5) * 0.1f,
                (float)(_random.NextDouble() - 0.5) * 0.1f
            );
            Position += jitter * deltaTime;
        }
        
        private void UpdateSwarmMovement(float deltaTime)
        {
            // Orbit around swarm center
            _orbitAngle += _orbitSpeed * deltaTime;
            
            Vector3 targetPosition = _swarmCenter + new Vector3(
                MathF.Cos(_orbitAngle) * _orbitRadius,
                MathF.Sin(_orbitAngle * 0.7f) * _orbitRadius * 0.3f,
                MathF.Sin(_orbitAngle) * _orbitRadius
            );
            
            Vector3 direction = Vector3.Normalize(targetPosition - Position);
            float speed = 8f;
            Velocity = direction * speed;
        }
        
        private void UpdateHuntingMovement(float deltaTime)
        {
            if (!_targetPosition.HasValue) return;
            
            Vector3 direction = Vector3.Normalize(_targetPosition.Value - Position);
            float speed = 12f;
            Velocity = direction * speed;
        }
        
        private void UpdateAttackMovement(float deltaTime)
        {
            // Rapid, erratic movement during attack
            Vector3 attackDirection = new Vector3(
                (float)(_random.NextDouble() - 0.5),
                (float)(_random.NextDouble() - 0.5),
                (float)(_random.NextDouble() - 0.5)
            );
            
            float speed = 15f;
            Velocity = Vector3.Normalize(attackDirection) * speed;
        }
        
        private void UpdateReplicationMovement(float deltaTime)
        {
            // Slow, stable movement during replication
            Vector3 direction = Vector3.Normalize(_swarmCenter - Position);
            float speed = 3f;
            Velocity = direction * speed;
        }
        
        private void UpdateDepletedMovement(float deltaTime)
        {
            // Very slow movement when depleted
            Vector3 direction = Vector3.Normalize(_swarmCenter - Position);
            float speed = 1f;
            Velocity = direction * speed;
        }
        
        private void UpdateEnergy(float deltaTime)
        {
            // Energy consumption based on state
            float energyConsumption = State switch
            {
                NaniteBehaviorState.Swarming => 5f,
                NaniteBehaviorState.Hunting => 8f,
                NaniteBehaviorState.Attacking => 15f,
                NaniteBehaviorState.Replicating => 20f,
                NaniteBehaviorState.Depleted => 2f,
                _ => 5f
            };
            
            Energy -= energyConsumption * deltaTime;
            Energy = Math.Max(0f, Energy);
            
            // Check for depletion
            if (Energy <= 10f && State != NaniteBehaviorState.Depleted)
            {
                State = NaniteBehaviorState.Depleted;
            }
            
            // Gradual energy regeneration
            if (State == NaniteBehaviorState.Swarming)
            {
                Energy = Math.Min(_maxEnergy, Energy + 3f * deltaTime);
            }
            
            // Deactivate if completely depleted
            if (Energy <= 0f)
            {
                IsActive = false;
            }
        }
        
        private void UpdateCooldowns(float deltaTime)
        {
            _replicationCooldown = Math.Max(0f, _replicationCooldown - deltaTime);
            _attackCooldown = Math.Max(0f, _attackCooldown - deltaTime);
        }
        
        private void UpdateVisuals(float deltaTime)
        {
            // Color changes based on state
            _currentColor = State switch
            {
                NaniteBehaviorState.Swarming => Color.Green,
                NaniteBehaviorState.Hunting => Color.Orange,
                NaniteBehaviorState.Attacking => Color.Red,
                NaniteBehaviorState.Replicating => Color.Blue,
                NaniteBehaviorState.Depleted => Color.Gray,
                _ => Color.White
            };
            
            // Alpha based on energy level
            float energyRatio = Energy / _maxEnergy;
            _currentColor.A = (byte)(255 * Math.Max(0.3f, energyRatio));
        }
        
        private void PerformAttack()
        {
            // Consume energy for attack
            Energy -= 20f;
            
            // Create attack effect
            // This would interface with the game's damage system
        }
        
        public void SetTargetPosition(Vector3? target)
        {
            _targetPosition = target;
            if (target.HasValue && State == NaniteBehaviorState.Swarming)
            {
                State = NaniteBehaviorState.Hunting;
            }
        }
        
        public void StartReplication()
        {
            if (Energy >= 50f && _replicationCooldown <= 0)
            {
                State = NaniteBehaviorState.Replicating;
                _replicationCooldown = 2f;
                Energy -= 30f; // Cost of replication
            }
        }
        
        public bool CanReplicate()
        {
            return Energy >= 50f && _replicationCooldown <= 0 && IsActive;
        }
        
        public NaniteUnit CreateReplica()
        {
            if (!CanReplicate()) return null;
            
            var replica = new NaniteUnit(Position + GetRandomOffset(1f), _random);
            replica.Energy = Energy * 0.6f; // Replica starts with 60% of parent energy
            Energy *= 0.4f; // Parent retains 40% of energy
            
            StartReplication();
            
            return replica;
        }
        
        public void Draw(Camera3D camera)
        {
            if (!IsActive) return;
            
            // Draw nanite body
            float size = 0.1f + (Energy / _maxEnergy) * 0.1f; // Size based on energy
            Raylib.DrawSphere(Position, size, _currentColor);
            
            // Draw state indicator
            switch (State)
            {
                case NaniteBehaviorState.Attacking:
                    DrawAttackIndicator(camera);
                    break;
                    
                case NaniteBehaviorState.Replicating:
                    DrawReplicationIndicator(camera);
                    break;
                    
                case NaniteBehaviorState.Hunting:
                    DrawHuntingIndicator(camera);
                    break;
            }
            
            // Draw energy level as small bar above nanite
            DrawEnergyBar(camera);
        }
        
        private void DrawAttackIndicator(Camera3D camera)
        {
            // Draw small spikes around nanite
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)i / 6 * MathF.PI * 2f;
                Vector3 spikeEnd = Position + new Vector3(
                    MathF.Cos(angle) * 0.3f,
                    MathF.Sin(angle) * 0.3f,
                    0f
                );
                Raylib.DrawLine3D(Position, spikeEnd, Color.Red);
            }
        }
        
        private void DrawReplicationIndicator(Camera3D camera)
        {
            // Draw pulsing ring around nanite
            float pulseRadius = 0.2f + MathF.Sin(GetTime() * 8f) * 0.1f;
            Raylib.DrawSphereWires(Position, pulseRadius, Color.Blue);
        }
        
        private void DrawHuntingIndicator(Camera3D camera)
        {
            // Draw direction line to target
            if (_targetPosition.HasValue)
            {
                Vector3 lineEnd = Position + Vector3.Normalize(_targetPosition.Value - Position) * 0.5f;
                Raylib.DrawLine3D(Position, lineEnd, Color.Orange);
            }
        }
        
        private void DrawEnergyBar(Camera3D camera)
        {
            Vector3 barStart = Position + new Vector3(-0.1f, 0.2f, 0f);
            Vector3 barEnd = Position + new Vector3(0.1f, 0.2f, 0f);
            
            // Background bar
            Raylib.DrawLine3D(barStart, barEnd, Color.Gray);
            
            // Energy level
            float energyRatio = Energy / _maxEnergy;
            Vector3 energyEnd = Vector3.Lerp(barStart, barEnd, energyRatio);
            
            Color energyColor = energyRatio > 0.5f ? Color.Green : 
                               energyRatio > 0.2f ? Color.Yellow : Color.Red;
            
            Raylib.DrawLine3D(barStart, energyEnd, energyColor);
        }
        
        private Vector3 GetRandomOffset(float magnitude)
        {
            return new Vector3(
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude,
                (float)(_random.NextDouble() - 0.5) * magnitude
            );
        }
        
        private float GetTime() => (float)Raylib.GetTime();
    }
    
    /// <summary>
    /// Behavioral states for nanite units
    /// </summary>
    public enum NaniteBehaviorState
    {
        Swarming,      // Default state - orbit around swarm center
        Hunting,       // Moving towards a target
        Attacking,     // Actively attacking a target
        Replicating,   // Creating new nanites
        Depleted       // Low energy, reduced functionality
    }
}