using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Core;

namespace Asteroids.AI.Ships
{
    /// <summary>
    /// Balanced combat ship with good offense and defense
    /// </summary>
    public class FighterShip : AIEnemyShip
    {
        private float burstFireTimer = 0;
        private int burstCount = 0;
        private const int MAX_BURST_COUNT = 3;
        private const float BURST_INTERVAL = 0.3f;
        
        public override EnemyShipType ShipType => EnemyShipType.Fighter;
        
        protected override void InitializeAI()
        {
            // Fighter ship stats
            MaxHealth = 80f;
            Health = MaxHealth;
            Speed = 80f;
            RotationSpeed = 2.5f;
            DetectionRange = 150f;
            AttackRange = 100f;
            Size = 12f;
            
            // Fighter personality - balanced aggression and tactics
            Aggressiveness = 0.6f;
            Caution = 0.4f;
            TeamworkTendency = 0.7f;
            Personality = AIPersonality.Balanced;
            
            // Start in patrol state
            StateMachine.Initialize(new PatrolState(), this);
        }
        
        public override float GetPreferredEngagementRange()
        {
            return AttackRange * 0.7f; // Prefers medium range combat
        }
        
        public override bool CanAttack()
        {
            return TimeSinceLastAttack >= 1.2f || (burstCount < MAX_BURST_COUNT && burstFireTimer >= BURST_INTERVAL);
        }
        
        public override void Attack(AIEnemyShip target)
        {
            if (!CanAttack() || target == null) return;
            
            var direction = Vector3.Normalize(target.Position - Position);
            
            // Predict target movement
            var timeToTarget = DistanceTo(target) / 200f; // Projectile speed
            var predictedPosition = target.Position + target.Velocity * timeToTarget;
            var adjustedDirection = Vector3.Normalize(predictedPosition - Position);
            
            // Create medium projectile
            // ProjectileManager.CreateProjectile(Position, adjustedDirection, ProjectileType.Medium);
            
            // Handle burst fire
            if (burstCount == 0)
            {
                TimeSinceLastAttack = 0;
                burstCount = 1;
                burstFireTimer = 0;
            }
            else if (burstCount < MAX_BURST_COUNT)
            {
                burstCount++;
                burstFireTimer = 0;
                
                if (burstCount >= MAX_BURST_COUNT)
                {
                    burstCount = 0;
                }
            }
        }
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            burstFireTimer += deltaTime;
        }
        
        /// <summary>
        /// Fighters use flanking maneuvers
        /// </summary>
        public void ExecuteFlankingManeuver()
        {
            if (Target == null) return;
            
            var toTarget = Vector3.Normalize(Target.Position - Position);
            var flankDirection = Vector3.Cross(toTarget, Up);
            
            // Choose left or right flank based on other allies
            var rightFlankPosition = Target.Position + flankDirection * GetPreferredEngagementRange();
            var leftFlankPosition = Target.Position - flankDirection * GetPreferredEngagementRange();
            
            // Check which flank has fewer allies
            var rightFlankAllies = CountAlliesNear(rightFlankPosition, 30f);
            var leftFlankAllies = CountAlliesNear(leftFlankPosition, 30f);
            
            var flankPosition = rightFlankAllies <= leftFlankAllies ? rightFlankPosition : leftFlankPosition;
            Navigator.SetDestination(flankPosition);
        }
        
        /// <summary>
        /// Coordinated attack with nearby fighters
        /// </summary>
        public void ExecuteCoordinatedAttack()
        {
            if (Target == null || NearbyAllies.Count == 0) return;
            
            // Calculate optimal attack positions around target
            var attackRadius = GetPreferredEngagementRange();
            var anglePerShip = 360f / (NearbyAllies.Count + 1);
            var myAngle = FormationIndex * anglePerShip;
            
            var attackPosition = Target.Position + new Vector3(
                (float)System.Math.Cos(myAngle * System.Math.PI / 180f) * attackRadius,
                0,
                (float)System.Math.Sin(myAngle * System.Math.PI / 180f) * attackRadius
            );
            
            Navigator.SetDestination(attackPosition);
            
            // Attack when in position
            if (DistanceTo(attackPosition) < 20f && IsInRange(Target.Position, AttackRange))
            {
                Attack(Target);
            }
        }
        
        private int CountAlliesNear(Vector3 position, float radius)
        {
            int count = 0;
            foreach (var ally in NearbyAllies)
            {
                if (Vector3.Distance(ally.Position, position) <= radius)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Defensive positioning when under heavy fire
        /// </summary>
        public void ExecuteDefensivePosition()
        {
            if (NearbyAllies.Count == 0) return;
            
            // Move toward center of allied formation for protection
            var allyCenter = GetAverageAllyPosition();
            var defensivePosition = Vector3.Lerp(Position, allyCenter, 0.3f);
            
            Navigator.SetDestination(defensivePosition);
            
            // Reduce speed to maintain formation
            if (Vector3.Distance(Position, defensivePosition) < 25f)
            {
                Velocity *= 0.7f;
            }
        }
    }
}