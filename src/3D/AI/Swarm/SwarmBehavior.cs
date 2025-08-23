using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Swarm
{
    /// <summary>
    /// Implements swarm intelligence behaviors based on boids algorithm
    /// </summary>
    public class SwarmBehavior
    {
        private float separationRadius = 25f;
        private float alignmentRadius = 40f;
        private float cohesionRadius = 60f;
        
        private float separationWeight = 2.0f;
        private float alignmentWeight = 1.0f;
        private float cohesionWeight = 1.5f;
        private float avoidanceWeight = 3.0f;
        
        public void UpdateSwarmBehavior(List<AIEnemyShip> swarm, float deltaTime)
        {
            foreach (var ship in swarm)
            {
                var neighbors = GetNeighbors(ship, swarm);
                
                var separation = CalculateSeparation(ship, neighbors);
                var alignment = CalculateAlignment(ship, neighbors);
                var cohesion = CalculateCohesion(ship, neighbors);
                var avoidance = CalculateObstacleAvoidance(ship);
                
                // Combine behaviors
                var steeringForce = 
                    separation * separationWeight +
                    alignment * alignmentWeight +
                    cohesion * cohesionWeight +
                    avoidance * avoidanceWeight;
                
                // Apply steering force with ship's agility
                ApplySteeringForce(ship, steeringForce, deltaTime);
            }
        }
        
        private List<AIEnemyShip> GetNeighbors(AIEnemyShip ship, List<AIEnemyShip> swarm)
        {
            var neighbors = new List<AIEnemyShip>();
            
            foreach (var other in swarm)
            {
                if (other == ship) continue;
                
                var distance = ship.DistanceTo(other);
                if (distance <= cohesionRadius)
                {
                    neighbors.Add(other);
                }
            }
            
            return neighbors;
        }
        
        private Vector3 CalculateSeparation(AIEnemyShip ship, List<AIEnemyShip> neighbors)
        {
            var steer = Vector3.Zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                var distance = ship.DistanceTo(neighbor);
                if (distance > 0 && distance < separationRadius)
                {
                    var diff = Vector3.Normalize(ship.Position - neighbor.Position);
                    diff /= distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer = Vector3.Normalize(steer);
            }
            
            return steer;
        }
        
        private Vector3 CalculateAlignment(AIEnemyShip ship, List<AIEnemyShip> neighbors)
        {
            var averageVelocity = Vector3.Zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                var distance = ship.DistanceTo(neighbor);
                if (distance > 0 && distance < alignmentRadius)
                {
                    averageVelocity += neighbor.Velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                averageVelocity /= count;
                averageVelocity = Vector3.Normalize(averageVelocity);
            }
            
            return averageVelocity;
        }
        
        private Vector3 CalculateCohesion(AIEnemyShip ship, List<AIEnemyShip> neighbors)
        {
            var centerOfMass = Vector3.Zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                var distance = ship.DistanceTo(neighbor);
                if (distance > 0 && distance < cohesionRadius)
                {
                    centerOfMass += neighbor.Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerOfMass /= count;
                var steer = Vector3.Normalize(centerOfMass - ship.Position);
                return steer;
            }
            
            return Vector3.Zero;
        }
        
        private Vector3 CalculateObstacleAvoidance(AIEnemyShip ship)
        {
            // Implement obstacle avoidance using raycasting
            var avoidanceVector = Vector3.Zero;
            var lookAheadDistance = 50f;
            
            // Cast rays in multiple directions to detect obstacles
            var rayDirections = new[]
            {
                ship.Forward,
                ship.Forward + ship.Right * 0.5f,
                ship.Forward - ship.Right * 0.5f,
                ship.Forward + ship.Up * 0.3f,
                ship.Forward - ship.Up * 0.3f
            };
            
            foreach (var direction in rayDirections)
            {
                var normalizedDirection = Vector3.Normalize(direction);
                var rayEnd = ship.Position + normalizedDirection * lookAheadDistance;
                
                // Check for obstacles (asteroids, other ships, world boundaries)
                if (DetectObstacle(ship.Position, rayEnd, out var obstaclePosition, out var obstacleRadius))
                {
                    // Calculate avoidance vector
                    var toObstacle = obstaclePosition - ship.Position;
                    var avoidDirection = Vector3.Cross(toObstacle, ship.Up);
                    
                    // If cross product is too small, use alternative
                    if (avoidDirection.LengthSquared() < 0.1f)
                    {
                        avoidDirection = Vector3.Cross(toObstacle, ship.Right);
                    }
                    
                    avoidDirection = Vector3.Normalize(avoidDirection);
                    
                    // Weight by proximity
                    var distance = toObstacle.Length();
                    var weight = 1f - (distance / lookAheadDistance);
                    
                    avoidanceVector += avoidDirection * weight;
                }
            }
            
            return Vector3.Normalize(avoidanceVector);
        }
        
        private bool DetectObstacle(Vector3 rayStart, Vector3 rayEnd, out Vector3 obstaclePosition, out float radius)
        {
            obstaclePosition = Vector3.Zero;
            radius = 0f;
            
            // This would integrate with the game's collision detection system
            // For now, implement basic world boundary checking
            
            var worldSize = 500f; // Assuming world boundaries
            
            if (Math.Abs(rayEnd.X) > worldSize || 
                Math.Abs(rayEnd.Y) > worldSize || 
                Math.Abs(rayEnd.Z) > worldSize)
            {
                // Hit world boundary
                obstaclePosition = rayEnd;
                radius = 10f;
                return true;
            }
            
            return false;
        }
        
        private void ApplySteeringForce(AIEnemyShip ship, Vector3 steeringForce, float deltaTime)
        {
            if (steeringForce.LengthSquared() < 0.001f) return;
            
            // Limit steering force
            var maxForce = ship.Speed * 2f;
            if (steeringForce.Length() > maxForce)
            {
                steeringForce = Vector3.Normalize(steeringForce) * maxForce;
            }
            
            // Apply to velocity
            var acceleration = steeringForce / 1f; // Assuming unit mass
            ship.Velocity += acceleration * deltaTime;
            
            // Limit velocity
            if (ship.Velocity.Length() > ship.Speed)
            {
                ship.Velocity = Vector3.Normalize(ship.Velocity) * ship.Speed;
            }
            
            // Update forward direction
            if (ship.Velocity.LengthSquared() > 0.001f)
            {
                var targetForward = Vector3.Normalize(ship.Velocity);
                ship.Forward = Vector3.Lerp(ship.Forward, targetForward, ship.RotationSpeed * deltaTime);
                ship.Forward = Vector3.Normalize(ship.Forward);
            }
        }
        
        public void SetBehaviorWeights(float separation, float alignment, float cohesion, float avoidance)
        {
            separationWeight = separation;
            alignmentWeight = alignment;
            cohesionWeight = cohesion;
            avoidanceWeight = avoidance;
        }
        
        public void SetBehaviorRadii(float separation, float alignment, float cohesion)
        {
            separationRadius = separation;
            alignmentRadius = alignment;
            cohesionRadius = cohesion;
        }
    }
    
    /// <summary>
    /// Emergent behaviors that arise from swarm interactions
    /// </summary>
    public class EmergentBehaviors
    {
        public static void UpdateEmergentBehaviors(List<AIEnemyShip> swarm, float deltaTime)
        {
            DetectAndFormFlocks(swarm);
            HandleLeadershipEmergence(swarm);
            AdaptToThreatLevel(swarm);
            CoordinateGroupActions(swarm);
        }
        
        private static void DetectAndFormFlocks(List<AIEnemyShip> swarm)
        {
            var unassigned = swarm.Where(s => s.Formation == null).ToList();
            if (unassigned.Count < 3) return;
            
            // Group nearby ships into flocks
            var clusters = new List<List<AIEnemyShip>>();
            
            foreach (var ship in unassigned)
            {
                var nearbyShips = unassigned.Where(s => 
                    s != ship && 
                    ship.DistanceTo(s) <= 80f
                ).ToList();
                
                if (nearbyShips.Count >= 2)
                {
                    var cluster = new List<AIEnemyShip> { ship };
                    cluster.AddRange(nearbyShips.Take(5)); // Limit flock size
                    clusters.Add(cluster);
                }
            }
            
            // Create formations for clusters
            foreach (var cluster in clusters)
            {
                if (cluster.Count >= 3)
                {
                    var formation = new Formations.FormationController(
                        Formations.FormationType.VFormation,
                        cluster[0].Position
                    );
                    
                    foreach (var ship in cluster)
                    {
                        formation.AddMember(ship);
                    }
                }
            }
        }
        
        private static void HandleLeadershipEmergence(List<AIEnemyShip> swarm)
        {
            // Natural leaders emerge based on performance and ship type
            foreach (var ship in swarm)
            {
                if (ship.Formation != null && ship.Formation.Leader != ship)
                {
                    var currentLeader = ship.Formation.Leader;
                    
                    // Challenge leadership if this ship is more suitable
                    if (currentLeader != null && IsMoreSuitableLeader(ship, currentLeader))
                    {
                        ship.Formation.SetLeader(ship);
                    }
                }
            }
        }
        
        private static bool IsMoreSuitableLeader(AIEnemyShip candidate, AIEnemyShip currentLeader)
        {
            // Health factor
            var healthAdvantage = (candidate.Health / candidate.MaxHealth) - 
                                 (currentLeader.Health / currentLeader.MaxHealth);
            
            // Ship type factor
            var typeScore = GetLeadershipTypeScore(candidate.ShipType) - 
                           GetLeadershipTypeScore(currentLeader.ShipType);
            
            // Performance factor (based on successful attacks, survival time, etc.)
            var performanceScore = candidate.TeamworkTendency - currentLeader.TeamworkTendency;
            
            return (healthAdvantage * 0.4f + typeScore * 0.4f + performanceScore * 0.2f) > 0.1f;
        }
        
        private static float GetLeadershipTypeScore(EnemyShipType shipType)
        {
            return shipType switch
            {
                EnemyShipType.Fighter => 1.0f,
                EnemyShipType.Bomber => 0.8f,
                EnemyShipType.Scout => 0.6f,
                EnemyShipType.Interceptor => 0.4f,
                _ => 0.5f
            };
        }
        
        private static void AdaptToThreatLevel(List<AIEnemyShip> swarm)
        {
            // Calculate overall threat level
            var totalDamage = swarm.Sum(s => s.MaxHealth - s.Health);
            var averageHealth = swarm.Average(s => s.Health / s.MaxHealth);
            
            var threatLevel = 1f - averageHealth;
            
            // Adapt behaviors based on threat
            foreach (var ship in swarm)
            {
                if (threatLevel > 0.7f) // High threat
                {
                    ship.Caution = Math.Min(1f, ship.Caution + 0.2f);
                    ship.TeamworkTendency = Math.Min(1f, ship.TeamworkTendency + 0.3f);
                }
                else if (threatLevel < 0.3f) // Low threat
                {
                    ship.Aggressiveness = Math.Min(1f, ship.Aggressiveness + 0.1f);
                }
            }
        }
        
        private static void CoordinateGroupActions(List<AIEnemyShip> swarm)
        {
            // Coordinate attacks when multiple ships target the same enemy
            var targetGroups = swarm
                .Where(s => s.Target != null)
                .GroupBy(s => s.Target)
                .Where(g => g.Count() >= 2);
            
            foreach (var group in targetGroups)
            {
                var attackers = group.ToList();
                CoordinateAttack(attackers, group.Key);
            }
        }
        
        private static void CoordinateAttack(List<AIEnemyShip> attackers, AIEnemyShip target)
        {
            // Assign attack roles based on ship types
            var scouts = attackers.Where(s => s.ShipType == EnemyShipType.Scout).ToList();
            var fighters = attackers.Where(s => s.ShipType == EnemyShipType.Fighter).ToList();
            var bombers = attackers.Where(s => s.ShipType == EnemyShipType.Bomber).ToList();
            var interceptors = attackers.Where(s => s.ShipType == EnemyShipType.Interceptor).ToList();
            
            // Scouts harass and report
            foreach (var scout in scouts)
            {
                if (scout is Ships.ScoutShip scoutShip)
                {
                    scoutShip.ExecuteHitAndRun();
                }
            }
            
            // Interceptors close fast for initial strike
            foreach (var interceptor in interceptors)
            {
                if (interceptor is Ships.InterceptorShip interceptorShip)
                {
                    interceptorShip.ExecuteInterceptManeuver();
                }
            }
            
            // Fighters provide sustained attack
            foreach (var fighter in fighters)
            {
                if (fighter is Ships.FighterShip fighterShip)
                {
                    fighterShip.ExecuteCoordinatedAttack();
                }
            }
            
            // Bombers deliver heavy damage from range
            foreach (var bomber in bombers)
            {
                if (bomber is Ships.BomberShip bomberShip)
                {
                    bomberShip.ExecuteCoordinatedBombardment();
                }
            }
        }
    }
}