using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Core;

namespace Asteroids.AI.Ships
{
    /// <summary>
    /// Heavy assault ship with powerful but slow attacks
    /// </summary>
    public class BomberShip : AIEnemyShip
    {
        private bool isChargingAttack = false;
        private float chargeTime = 0;
        private const float CHARGE_DURATION = 2.0f;
        private Vector3 chargeStartPosition;
        
        public override EnemyShipType ShipType => EnemyShipType.Bomber;
        
        protected override void InitializeAI()
        {
            // Bomber ship stats
            MaxHealth = 150f;
            Health = MaxHealth;
            Speed = 50f;
            RotationSpeed = 1.5f;
            DetectionRange = 120f;
            AttackRange = 120f;
            Size = 18f;
            
            // Bomber personality - high damage, defensive, needs support
            Aggressiveness = 0.4f;
            Caution = 0.7f;
            TeamworkTendency = 0.9f;
            Personality = AIPersonality.Defensive;
            
            // Start in patrol state
            StateMachine.Initialize(new PatrolState(), this);
        }
        
        public override float GetPreferredEngagementRange()
        {
            return AttackRange * 0.9f; // Prefers to stay at maximum range
        }
        
        public override bool CanAttack()
        {
            return TimeSinceLastAttack >= 4.0f && !isChargingAttack;
        }
        
        public override void Attack(AIEnemyShip target)
        {
            if (!CanAttack() || target == null) return;
            
            // Start charging heavy attack
            isChargingAttack = true;
            chargeTime = 0;
            chargeStartPosition = Position;
            Stop(); // Stop moving while charging
        }
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            if (isChargingAttack)
            {
                UpdateChargeAttack(deltaTime);
            }
        }
        
        private void UpdateChargeAttack(float deltaTime)
        {
            chargeTime += deltaTime;
            
            if (chargeTime >= CHARGE_DURATION)
            {
                // Fire heavy projectile
                FireHeavyProjectile();
                isChargingAttack = false;
                TimeSinceLastAttack = 0;
            }
            else
            {
                // Visual/audio charging effects would go here
                // ParticleSystem.EmitChargeEffect(Position);
            }
        }
        
        private void FireHeavyProjectile()
        {
            if (Target == null) return;
            
            var direction = Vector3.Normalize(Target.Position - Position);
            
            // Predict target movement for slow projectile
            var timeToTarget = DistanceTo(Target) / 100f; // Slower projectile
            var predictedPosition = Target.Position + Target.Velocity * timeToTarget;
            var adjustedDirection = Vector3.Normalize(predictedPosition - Position);
            
            // Create heavy projectile with splash damage
            // ProjectileManager.CreateProjectile(Position, adjustedDirection, ProjectileType.Heavy);
            
            // Apply recoil
            Velocity -= adjustedDirection * 20f;
        }
        
        /// <summary>
        /// Bombers prefer to attack from cover
        /// </summary>
        public void SeekCoverPosition()
        {
            if (Target == null) return;
            
            // Find position behind allies or obstacles
            var idealDistance = GetPreferredEngagementRange();
            var toTarget = Vector3.Normalize(Target.Position - Position);
            
            // Look for cover behind allies
            Vector3? coverPosition = null;
            
            foreach (var ally in NearbyAllies)
            {
                var allyToTarget = Vector3.Normalize(Target.Position - ally.Position);
                var behindAlly = ally.Position - allyToTarget * 30f;
                
                // Check if this position provides good firing angle
                var distanceToTarget = Vector3.Distance(behindAlly, Target.Position);
                if (distanceToTarget <= AttackRange * 1.1f && distanceToTarget >= AttackRange * 0.8f)
                {
                    coverPosition = behindAlly;
                    break;
                }
            }
            
            // If no ally cover, find alternative position
            if (!coverPosition.HasValue)
            {
                var perpendicular = Vector3.Cross(toTarget, Up);
                coverPosition = Position + perpendicular * 40f - toTarget * 20f;
            }
            
            Navigator.SetDestination(coverPosition.Value);
        }
        
        /// <summary>
        /// Suppressive fire to control enemy movement
        /// </summary>
        public void ExecuteSuppressiveFire()
        {
            if (Target == null) return;
            
            // Predict where target will be and fire there
            var timeToTarget = DistanceTo(Target) / 100f;
            var leadPosition = Target.Position + Target.Velocity * timeToTarget;
            
            // Add area denial by firing slightly ahead
            var leadDirection = Vector3.Normalize(Target.Velocity);
            var suppressionPosition = leadPosition + leadDirection * 50f;
            
            var direction = Vector3.Normalize(suppressionPosition - Position);
            
            // Create suppression projectile
            // ProjectileManager.CreateProjectile(Position, direction, ProjectileType.Suppression);
        }
        
        /// <summary>
        /// Coordinated bombardment with other bombers
        /// </summary>
        public void ExecuteCoordinatedBombardment()
        {
            if (Target == null) return;
            
            var nearbyBombers = NearbyAllies.FindAll(ally => ally is BomberShip);
            if (nearbyBombers.Count == 0) return;
            
            // Synchronize attack timing
            var allReadyToAttack = true;
            foreach (var bomber in nearbyBombers)
            {
                if (!bomber.CanAttack())
                {
                    allReadyToAttack = false;
                    break;
                }
            }
            
            if (allReadyToAttack && CanAttack())
            {
                // Send coordination message
                Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.CoordinatedAttack,
                    Sender = this,
                    Position = Target.Position,
                    Data = "bombardment_ready"
                });
                
                Attack(Target);
            }
        }
        
        /// <summary>
        /// Emergency retreat when shields are low
        /// </summary>
        public void ExecuteEmergencyRetreat()
        {
            // Find safest retreat direction (away from target, toward allies)
            var retreatDirection = Vector3.Zero;
            
            if (Target != null)
            {
                retreatDirection += Vector3.Normalize(Position - Target.Position) * 2f;
            }
            
            if (NearbyAllies.Count > 0)
            {
                var allyCenter = GetAverageAllyPosition();
                retreatDirection += Vector3.Normalize(allyCenter - Position);
            }
            
            retreatDirection = Vector3.Normalize(retreatDirection);
            var retreatPosition = Position + retreatDirection * 100f;
            
            Navigator.SetDestination(retreatPosition);
            
            // Request escort
            Communication.BroadcastMessage(new Communication.AIMessage
            {
                Type = Communication.MessageType.RequestEscort,
                Sender = this,
                Position = Position,
                Data = Health / MaxHealth
            });
        }
    }
}