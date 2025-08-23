using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Core;

namespace Asteroids.AI.Ships
{
    /// <summary>
    /// Fast, agile scout ship focused on reconnaissance and harassment
    /// </summary>
    public class ScoutShip : AIEnemyShip
    {
        private float lastReportTime = 0;
        private const float REPORT_INTERVAL = 5.0f;
        
        public override EnemyShipType ShipType => EnemyShipType.Scout;
        
        protected override void InitializeAI()
        {
            // Scout ship stats
            MaxHealth = 30f;
            Health = MaxHealth;
            Speed = 120f;
            RotationSpeed = 4.0f;
            DetectionRange = 200f;
            AttackRange = 80f;
            Size = 8f;
            
            // Scout personality - fast, evasive, reports intel
            Aggressiveness = 0.3f;
            Caution = 0.8f;
            TeamworkTendency = 0.9f;
            Personality = AIPersonality.Cautious;
            
            // Start in patrol state
            StateMachine.Initialize(new PatrolState(), this);
        }
        
        public override float GetPreferredEngagementRange()
        {
            return AttackRange * 0.8f; // Prefers to stay at edge of range
        }
        
        public override bool CanAttack()
        {
            return TimeSinceLastAttack >= 2.0f; // Scouts have slower fire rate
        }
        
        public override void Attack(AIEnemyShip target)
        {
            if (!CanAttack() || target == null) return;
            
            // Scout fires light projectiles
            var direction = Vector3.Normalize(target.Position - Position);
            
            // Create light projectile (implement projectile system as needed)
            // ProjectileManager.CreateProjectile(Position, direction, ProjectileType.Light);
            
            TimeSinceLastAttack = 0;
        }
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            lastReportTime += deltaTime;
            
            // Scouts regularly report intel to allies
            if (lastReportTime >= REPORT_INTERVAL)
            {
                ReportIntelligence();
                lastReportTime = 0;
            }
        }
        
        private void ReportIntelligence()
        {
            if (Target != null)
            {
                // Report player position to nearby allies
                Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.TargetSighted,
                    Sender = this,
                    Position = Target.Position,
                    Data = new
                    {
                        TargetVelocity = Target.Velocity,
                        TargetHealth = Target.Health,
                        Confidence = 0.9f
                    }
                });
            }
        }
        
        /// <summary>
        /// Scouts prefer hit-and-run tactics
        /// </summary>
        public void ExecuteHitAndRun()
        {
            if (Target == null) return;
            
            var distanceToTarget = DistanceTo(Target);
            
            if (distanceToTarget < AttackRange * 1.2f)
            {
                // Attack if in range
                if (CanAttack())
                {
                    Attack(Target);
                }
                
                // Then retreat
                var retreatDirection = Vector3.Normalize(Position - Target.Position);
                var retreatPosition = Position + retreatDirection * 50f;
                Navigator.SetDestination(retreatPosition);
            }
            else
            {
                // Move closer for attack
                Navigator.SetDestination(Target.Position);
            }
        }
        
        /// <summary>
        /// Scouts excel at evasive maneuvers
        /// </summary>
        public void ExecuteEvasiveManeuvers()
        {
            if (Target == null) return;
            
            // Calculate evasion vector perpendicular to target direction
            var toTarget = Vector3.Normalize(Target.Position - Position);
            var evasionVector = Vector3.Cross(toTarget, Up);
            
            // Add some randomness
            var random = new System.Random();
            evasionVector += new Vector3(
                (float)(random.NextDouble() - 0.5) * 0.5f,
                (float)(random.NextDouble() - 0.5) * 0.5f,
                (float)(random.NextDouble() - 0.5) * 0.5f
            );
            
            evasionVector = Vector3.Normalize(evasionVector);
            
            // Set evasion destination
            var evasionDestination = Position + evasionVector * 30f;
            Navigator.SetDestination(evasionDestination);
            
            // Increase speed during evasion
            Velocity = Vector3.Normalize(Velocity) * Speed * 1.2f;
        }
    }
}