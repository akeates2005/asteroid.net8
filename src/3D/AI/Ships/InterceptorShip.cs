using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Core;

namespace Asteroids.AI.Ships
{
    /// <summary>
    /// Ultra-fast interceptor focused on speed and precision strikes
    /// </summary>
    public class InterceptorShip : AIEnemyShip
    {
        private bool isBoostActive = false;
        private float boostCooldown = 0;
        private const float BOOST_DURATION = 3.0f;
        private const float BOOST_COOLDOWN = 8.0f;
        private const float BOOST_MULTIPLIER = 2.0f;
        
        public override EnemyShipType ShipType => EnemyShipType.Interceptor;
        
        protected override void InitializeAI()
        {
            // Interceptor ship stats
            MaxHealth = 45f;
            Health = MaxHealth;
            Speed = 140f;
            RotationSpeed = 5.0f;
            DetectionRange = 180f;
            AttackRange = 60f;
            Size = 6f;
            
            // Interceptor personality - high aggression, low caution, hit-and-run
            Aggressiveness = 0.9f;
            Caution = 0.2f;
            TeamworkTendency = 0.4f;
            Personality = AIPersonality.Aggressive;
            
            // Start in patrol state
            StateMachine.Initialize(new PatrolState(), this);
        }
        
        public override float GetPreferredEngagementRange()
        {
            return AttackRange * 0.6f; // Prefers close-range attacks
        }
        
        public override bool CanAttack()
        {
            return TimeSinceLastAttack >= 0.8f; // Fast fire rate
        }
        
        public override void Attack(AIEnemyShip target)
        {
            if (!CanAttack() || target == null) return;
            
            var direction = Vector3.Normalize(target.Position - Position);
            
            // Rapid-fire attack with high accuracy
            for (int i = 0; i < 2; i++)
            {
                var spread = (i - 0.5f) * 0.1f;
                var spreadDirection = direction + Right * spread;
                spreadDirection = Vector3.Normalize(spreadDirection);
                
                // Create fast, precise projectile
                // ProjectileManager.CreateProjectile(Position, spreadDirection, ProjectileType.Fast);
            }
            
            TimeSinceLastAttack = 0;
        }
        
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            // Update boost system
            if (boostCooldown > 0)
            {
                boostCooldown -= deltaTime;
            }
            
            // Apply boost speed multiplier
            if (isBoostActive)
            {
                var currentSpeed = Velocity.Length();
                if (currentSpeed > 0)
                {
                    Velocity = Vector3.Normalize(Velocity) * Speed * BOOST_MULTIPLIER;
                }
            }
        }
        
        /// <summary>
        /// Activate speed boost for pursuit or escape
        /// </summary>
        public bool ActivateBoost()
        {
            if (boostCooldown > 0 || isBoostActive) return false;
            
            isBoostActive = true;
            boostCooldown = BOOST_COOLDOWN;
            
            // Start boost timer
            System.Threading.Tasks.Task.Delay((int)(BOOST_DURATION * 1000)).ContinueWith(_ =>
            {
                isBoostActive = false;
            });
            
            return true;
        }
        
        /// <summary>
        /// High-speed intercept maneuver
        /// </summary>
        public void ExecuteInterceptManeuver()
        {
            if (Target == null) return;
            
            // Calculate intercept course
            var targetVelocity = Target.Velocity;
            var relativePosition = Target.Position - Position;
            var speed = isBoostActive ? Speed * BOOST_MULTIPLIER : Speed;
            
            // Solve intercept triangle
            var interceptPoint = CalculateInterceptPoint(Position, Target.Position, targetVelocity, speed);
            
            Navigator.SetDestination(interceptPoint);
            
            // Activate boost if target is far and boost is available
            var distanceToTarget = DistanceTo(Target);
            if (distanceToTarget > 80f && boostCooldown <= 0 && !isBoostActive)
            {
                ActivateBoost();
            }
        }
        
        /// <summary>
        /// Spiral attack pattern for evasion
        /// </summary>
        public void ExecuteSpiralAttack()
        {
            if (Target == null) return;
            
            var distanceToTarget = DistanceTo(Target);
            var optimalDistance = GetPreferredEngagementRange();
            
            if (distanceToTarget > optimalDistance * 1.5f)
            {
                // Close distance in spiral
                ExecuteInterceptManeuver();
            }
            else
            {
                // Spiral around target
                var toTarget = Vector3.Normalize(Target.Position - Position);
                var tangent = Vector3.Cross(toTarget, Up);
                
                // Calculate spiral position
                var time = (float)System.DateTime.Now.Ticks / 10000000f;
                var spiralRadius = optimalDistance;
                var spiralSpeed = 2.0f;
                
                var spiralPosition = Target.Position + 
                    tangent * (float)System.Math.Cos(time * spiralSpeed) * spiralRadius +
                    Vector3.Cross(tangent, toTarget) * (float)System.Math.Sin(time * spiralSpeed) * spiralRadius;
                
                Navigator.SetDestination(spiralPosition);
                
                // Attack when aligned
                if (CanSee(Target.Position) && IsInRange(Target.Position, AttackRange))
                {
                    Attack(Target);
                }
            }
        }
        
        /// <summary>
        /// Lightning fast hit-and-run attack
        /// </summary>
        public void ExecuteHitAndRun()
        {
            if (Target == null) return;
            
            var distanceToTarget = DistanceTo(Target);
            
            if (distanceToTarget > AttackRange)
            {
                // Approach at high speed
                if (!isBoostActive && boostCooldown <= 0)
                {
                    ActivateBoost();
                }
                Navigator.SetDestination(Target.Position);
            }
            else
            {
                // Attack and immediately retreat
                Attack(Target);
                
                var retreatDirection = Vector3.Normalize(Position - Target.Position);
                var retreatPosition = Position + retreatDirection * 80f;
                Navigator.SetDestination(retreatPosition);
            }
        }
        
        /// <summary>
        /// Coordinated strike with other interceptors
        /// </summary>
        public void ExecuteCoordinatedStrike()
        {
            var nearbyInterceptors = NearbyAllies.FindAll(ally => ally is InterceptorShip);
            if (nearbyInterceptors.Count == 0 || Target == null) return;
            
            // Calculate attack vectors
            var attackVectors = new Vector3[nearbyInterceptors.Count + 1];
            var centerPosition = Target.Position;
            
            for (int i = 0; i < attackVectors.Length; i++)
            {
                var angle = (360f / attackVectors.Length) * i;
                var direction = new Vector3(
                    (float)System.Math.Cos(angle * System.Math.PI / 180f),
                    0,
                    (float)System.Math.Sin(angle * System.Math.PI / 180f)
                );
                
                attackVectors[i] = centerPosition + direction * 100f;
            }
            
            // Assign my attack vector (based on formation index or distance)
            var myIndex = FormationIndex >= 0 ? FormationIndex % attackVectors.Length : 0;
            var myAttackPosition = attackVectors[myIndex];
            
            Navigator.SetDestination(myAttackPosition);
            
            // Coordinate timing - attack when all are in position
            var allInPosition = true;
            foreach (var interceptor in nearbyInterceptors)
            {
                var theirIndex = interceptor.FormationIndex >= 0 ? 
                    interceptor.FormationIndex % attackVectors.Length : 1;
                var theirPosition = attackVectors[theirIndex];
                
                if (Vector3.Distance(interceptor.Position, theirPosition) > 20f)
                {
                    allInPosition = false;
                    break;
                }
            }
            
            if (allInPosition && Vector3.Distance(Position, myAttackPosition) < 20f)
            {
                // All interceptors attack simultaneously
                Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.CoordinatedAttack,
                    Sender = this,
                    Position = Target.Position,
                    Data = "interceptor_strike"
                });
                
                ExecuteInterceptManeuver();
            }
        }
        
        private Vector3 CalculateInterceptPoint(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVel, float projectileSpeed)
        {
            var toTarget = targetPos - shooterPos;
            var distance = toTarget.Length();
            var direction = toTarget / distance;
            
            // Quadratic equation to solve intercept
            var a = Vector3.Dot(targetVel, targetVel) - projectileSpeed * projectileSpeed;
            var b = 2 * Vector3.Dot(targetVel, toTarget);
            var c = Vector3.Dot(toTarget, toTarget);
            
            var discriminant = b * b - 4 * a * c;
            
            if (discriminant < 0)
            {
                // No intercept possible, aim at current position
                return targetPos;
            }
            
            var t1 = (-b + (float)System.Math.Sqrt(discriminant)) / (2 * a);
            var t2 = (-b - (float)System.Math.Sqrt(discriminant)) / (2 * a);
            
            var t = t1 > 0 ? (t2 > 0 ? System.Math.Min(t1, t2) : t1) : t2;
            
            if (t < 0)
            {
                return targetPos;
            }
            
            return targetPos + targetVel * t;
        }
    }
}