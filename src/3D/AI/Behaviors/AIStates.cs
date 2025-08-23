using System;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Behaviors
{
    /// <summary>
    /// Patrol state - ship moves along patrol route looking for targets
    /// </summary>
    public class PatrolState : AIState
    {
        public override string StateName => "Patrol";
        
        private Vector3[] patrolPoints;
        private int currentPatrolIndex = 0;
        private float patrolRadius = 100f;
        private float detectionCheckInterval = 0.5f;
        private float lastDetectionCheck = 0;
        
        public override void OnEnter(AIEnemyShip ship)
        {
            // Generate patrol points around current position
            GeneratePatrolRoute(ship.Position);
            ship.Navigator.SetDestination(patrolPoints[currentPatrolIndex]);
        }
        
        public override void Update(AIEnemyShip ship, float deltaTime)
        {
            lastDetectionCheck += deltaTime;
            
            // Check for targets periodically
            if (lastDetectionCheck >= detectionCheckInterval)
            {
                // Target detection would be handled by AI Manager
                lastDetectionCheck = 0;
            }
            
            // Move along patrol route
            if (Vector3.Distance(ship.Position, patrolPoints[currentPatrolIndex]) < 20f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                ship.Navigator.SetDestination(patrolPoints[currentPatrolIndex]);
            }
            
            // Maintain formation if part of one
            if (ship.Formation != null && ship.FormationMember.GetFormationPriority() > 0.5f)
            {
                ship.FormationMember.Update(deltaTime);
            }
        }
        
        public override AIState CheckTransitions(AIEnemyShip ship)
        {
            // Transition to attack if target found
            if (ship.Target != null && ship.CanSee(ship.Target.Position))
            {
                return new AttackState();
            }
            
            // Transition to investigate if heard ally combat
            // This would be triggered by communication system
            
            return null; // Stay in patrol
        }
        
        private void GeneratePatrolRoute(Vector3 center)
        {
            patrolPoints = new Vector3[4];
            var random = new Random();
            
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var angle = (360f / patrolPoints.Length) * i + random.Next(-30, 30);
                var distance = patrolRadius * (0.7f + random.NextSingle() * 0.6f);
                
                patrolPoints[i] = center + new Vector3(
                    (float)Math.Cos(angle * Math.PI / 180f) * distance,
                    (random.NextSingle() - 0.5f) * 50f, // Some vertical variation
                    (float)Math.Sin(angle * Math.PI / 180f) * distance
                );
            }
        }
    }
    
    /// <summary>
    /// Attack state - ship engages target with appropriate tactics
    /// </summary>
    public class AttackState : AIState
    {
        public override string StateName => "Attack";
        
        private float lastAttackTime = 0;
        private float engagementTime = 0;
        private float tacticalTimer = 0;
        private Vector3 lastKnownTargetPosition;
        
        public override void OnEnter(AIEnemyShip ship)
        {
            if (ship.Target != null)
            {
                lastKnownTargetPosition = ship.Target.Position;
                
                // Notify allies of combat
                ship.Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.EngagingTarget,
                    Sender = ship,
                    Position = ship.Target.Position,
                    Data = ship.Target
                });
            }
        }
        
        public override void Update(AIEnemyShip ship, float deltaTime)
        {
            lastAttackTime += deltaTime;
            engagementTime += deltaTime;
            tacticalTimer += deltaTime;
            
            if (ship.Target == null) return;
            
            // Update last known position if target is visible
            if (ship.CanSee(ship.Target.Position))
            {
                lastKnownTargetPosition = ship.Target.Position;
                ship.LastKnownPlayerPosition = ship.Target.Position;
                ship.LastPlayerSightingTime = 0;
            }
            else
            {
                ship.LastPlayerSightingTime += deltaTime;
            }
            
            // Execute ship-specific attack behavior
            ExecuteAttackBehavior(ship, deltaTime);
            
            // Tactical decision making every 2 seconds
            if (tacticalTimer >= 2.0f)
            {
                MakeTacticalDecision(ship);
                tacticalTimer = 0;
            }
        }
        
        public override AIState CheckTransitions(AIEnemyShip ship)
        {
            // Transition to flee if heavily damaged and cautious
            if (ship.Health / ship.MaxHealth < 0.25f && ship.Caution > 0.6f)
            {
                return new FleeState();
            }
            
            // Transition to pursue if target lost
            if (ship.Target == null || ship.LastPlayerSightingTime > 5.0f)
            {
                return new PursueState();
            }
            
            // Transition to support if ally needs help
            foreach (var ally in ship.NearbyAllies)
            {
                if (ally.Health / ally.MaxHealth < 0.3f && ship.TeamworkTendency > 0.7f)
                {
                    ship.SetTarget(ally.Target);
                    return new SupportState();
                }
            }
            
            return null; // Stay in attack
        }
        
        private void ExecuteAttackBehavior(AIEnemyShip ship, float deltaTime)
        {
            var distanceToTarget = ship.DistanceTo(ship.Target);
            var preferredRange = ship.GetPreferredEngagementRange();
            
            // Position based on ship type and preferred range
            if (distanceToTarget > preferredRange * 1.2f)
            {
                // Too far - close distance
                ship.Navigator.SetDestination(ship.Target.Position);
            }
            else if (distanceToTarget < preferredRange * 0.8f)
            {
                // Too close - back away while attacking
                var retreatDirection = Vector3.Normalize(ship.Position - ship.Target.Position);
                var retreatPosition = ship.Position + retreatDirection * 30f;
                ship.Navigator.SetDestination(retreatPosition);
            }
            else
            {
                // Good range - execute ship-specific maneuvers
                ExecuteShipSpecificManeuvers(ship);
            }
            
            // Attack if in range and ready
            if (ship.CanAttack() && ship.IsInRange(ship.Target.Position, ship.AttackRange))
            {
                ship.Attack(ship.Target);
            }
        }
        
        private void ExecuteShipSpecificManeuvers(AIEnemyShip ship)
        {
            switch (ship.ShipType)
            {
                case EnemyShipType.Scout:
                    if (ship is Ships.ScoutShip scout)
                        scout.ExecuteHitAndRun();
                    break;
                    
                case EnemyShipType.Fighter:
                    if (ship is Ships.FighterShip fighter)
                        fighter.ExecuteFlankingManeuver();
                    break;
                    
                case EnemyShipType.Bomber:
                    if (ship is Ships.BomberShip bomber)
                        bomber.SeekCoverPosition();
                    break;
                    
                case EnemyShipType.Interceptor:
                    if (ship is Ships.InterceptorShip interceptor)
                        interceptor.ExecuteSpiralAttack();
                    break;
            }
        }
        
        private void MakeTacticalDecision(AIEnemyShip ship)
        {
            // Analyze tactical situation
            var allyCount = ship.NearbyAllies.Count;
            var healthRatio = ship.Health / ship.MaxHealth;
            var distanceToTarget = ship.DistanceTo(ship.Target);
            
            // Request support if outnumbered
            if (allyCount < 2 && ship.TeamworkTendency > 0.5f)
            {
                ship.Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.RequestSupport,
                    Sender = ship,
                    Position = ship.Position,
                    Data = ship.Target
                });
            }
            
            // Coordinate attacks if multiple allies present
            if (allyCount >= 2)
            {
                switch (ship.ShipType)
                {
                    case EnemyShipType.Fighter:
                        if (ship is Ships.FighterShip fighter)
                            fighter.ExecuteCoordinatedAttack();
                        break;
                        
                    case EnemyShipType.Bomber:
                        if (ship is Ships.BomberShip bomber)
                            bomber.ExecuteCoordinatedBombardment();
                        break;
                        
                    case EnemyShipType.Interceptor:
                        if (ship is Ships.InterceptorShip interceptor)
                            interceptor.ExecuteCoordinatedStrike();
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Pursue state - ship searches for lost target
    /// </summary>
    public class PursueState : AIState
    {
        public override string StateName => "Pursue";
        
        private Vector3 searchCenter;
        private float searchRadius = 80f;
        private float searchTime = 0;
        private float maxSearchTime = 10f;
        
        public override void OnEnter(AIEnemyShip ship)
        {
            searchCenter = ship.LastKnownPlayerPosition;
            searchTime = 0;
            
            // Start search pattern
            ship.Navigator.SetDestination(searchCenter);
        }
        
        public override void Update(AIEnemyShip ship, float deltaTime)
        {
            searchTime += deltaTime;
            
            // Expand search area over time
            searchRadius += deltaTime * 20f;
            
            // Move in search pattern
            if (Vector3.Distance(ship.Position, ship.Navigator.Destination) < 15f)
            {
                var random = new Random();
                var searchPoint = searchCenter + new Vector3(
                    (random.NextSingle() - 0.5f) * searchRadius,
                    (random.NextSingle() - 0.5f) * searchRadius * 0.5f,
                    (random.NextSingle() - 0.5f) * searchRadius
                );
                
                ship.Navigator.SetDestination(searchPoint);
            }
        }
        
        public override AIState CheckTransitions(AIEnemyShip ship)
        {
            // Found target - return to attack
            if (ship.Target != null && ship.CanSee(ship.Target.Position))
            {
                return new AttackState();
            }
            
            // Give up search after timeout
            if (searchTime > maxSearchTime)
            {
                return new PatrolState();
            }
            
            return null; // Continue searching
        }
    }
    
    /// <summary>
    /// Flee state - ship retreats to safety
    /// </summary>
    public class FleeState : AIState
    {
        public override string StateName => "Flee";
        
        private Vector3 safetyPosition;
        private float fleeTime = 0;
        private float minFleeTime = 5f;
        
        public override void OnEnter(AIEnemyShip ship)
        {
            // Find safe position away from threats
            safetyPosition = FindSafetyPosition(ship);
            ship.Navigator.SetDestination(safetyPosition);
            fleeTime = 0;
            
            // Request assistance
            ship.Communication.BroadcastMessage(new Communication.AIMessage
            {
                Type = Communication.MessageType.RequestEscort,
                Sender = ship,
                Position = ship.Position,
                Data = ship.Health / ship.MaxHealth
            });
        }
        
        public override void Update(AIEnemyShip ship, float deltaTime)
        {
            fleeTime += deltaTime;
            
            // Execute evasive maneuvers
            if (ship is Ships.ScoutShip scout)
            {
                scout.ExecuteEvasiveManeuvers();
            }
            else if (ship is Ships.BomberShip bomber)
            {
                bomber.ExecuteEmergencyRetreat();
            }
            else
            {
                // Generic evasion
                if (ship.Target != null)
                {
                    var evasionVector = Vector3.Normalize(ship.Position - ship.Target.Position);
                    var evasionDestination = ship.Position + evasionVector * 50f;
                    ship.Navigator.SetDestination(evasionDestination);
                }
            }
        }
        
        public override AIState CheckTransitions(AIEnemyShip ship)
        {
            // Return to combat if health recovered and allies present
            if (fleeTime > minFleeTime && 
                ship.Health / ship.MaxHealth > 0.6f && 
                ship.NearbyAllies.Count >= 2)
            {
                return new AttackState();
            }
            
            // Return to patrol if safe and no immediate threats
            if (ship.Target == null || 
                ship.DistanceTo(ship.Target) > ship.DetectionRange * 1.5f)
            {
                return new PatrolState();
            }
            
            return null; // Continue fleeing
        }
        
        private Vector3 FindSafetyPosition(AIEnemyShip ship)
        {
            var fleeDirection = Vector3.Zero;
            
            // Flee away from target
            if (ship.Target != null)
            {
                fleeDirection += Vector3.Normalize(ship.Position - ship.Target.Position) * 2f;
            }
            
            // Flee toward allies
            if (ship.NearbyAllies.Count > 0)
            {
                var allyCenter = ship.GetAverageAllyPosition();
                fleeDirection += Vector3.Normalize(allyCenter - ship.Position);
            }
            
            fleeDirection = Vector3.Normalize(fleeDirection);
            return ship.Position + fleeDirection * 100f;
        }
    }
    
    /// <summary>
    /// Support state - ship assists allies in combat
    /// </summary>
    public class SupportState : AIState
    {
        public override string StateName => "Support";
        
        private AIEnemyShip allyToSupport;
        private float supportTime = 0;
        
        public override void OnEnter(AIEnemyShip ship)
        {
            // Find ally most in need of support
            allyToSupport = FindAllyNeedingSupport(ship);
            supportTime = 0;
        }
        
        public override void Update(AIEnemyShip ship, float deltaTime)
        {
            supportTime += deltaTime;
            
            if (allyToSupport == null) return;
            
            // Position to support ally
            var supportPosition = CalculateSupportPosition(ship, allyToSupport);
            ship.Navigator.SetDestination(supportPosition);
            
            // Attack ally's target if possible
            if (allyToSupport.Target != null && 
                ship.IsInRange(allyToSupport.Target.Position, ship.AttackRange))
            {
                ship.SetTarget(allyToSupport.Target);
                if (ship.CanAttack())
                {
                    ship.Attack(ship.Target);
                }
            }
        }
        
        public override AIState CheckTransitions(AIEnemyShip ship)
        {
            // Ally no longer needs support
            if (allyToSupport == null || 
                allyToSupport.Health / allyToSupport.MaxHealth > 0.7f ||
                supportTime > 15f)
            {
                return new AttackState();
            }
            
            return null; // Continue supporting
        }
        
        private AIEnemyShip FindAllyNeedingSupport(AIEnemyShip ship)
        {
            AIEnemyShip mostNeedyAlly = null;
            float lowestHealthRatio = 1f;
            
            foreach (var ally in ship.NearbyAllies)
            {
                var healthRatio = ally.Health / ally.MaxHealth;
                if (healthRatio < lowestHealthRatio && healthRatio < 0.5f)
                {
                    lowestHealthRatio = healthRatio;
                    mostNeedyAlly = ally;
                }
            }
            
            return mostNeedyAlly;
        }
        
        private Vector3 CalculateSupportPosition(AIEnemyShip ship, AIEnemyShip ally)
        {
            if (ally.Target == null) return ally.Position;
            
            // Position between ally and their target for protection
            var allyToTarget = Vector3.Normalize(ally.Target.Position - ally.Position);
            var supportOffset = Vector3.Cross(allyToTarget, Vector3.UnitY) * 30f;
            
            return ally.Position + supportOffset;
        }
    }
}