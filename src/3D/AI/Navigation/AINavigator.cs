using System;
using System.Collections.Generic;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Navigation
{
    /// <summary>
    /// 3D navigation and pathfinding system for AI ships
    /// </summary>
    public class AINavigator
    {
        private AIEnemyShip ship;
        private Vector3 destination;
        private List<Vector3> currentPath;
        private int pathIndex;
        private float pathFindingRadius = 200f;
        private float obstacleAvoidanceRadius = 30f;
        private float arrivalThreshold = 15f;
        
        public Vector3 Destination => destination;
        public bool HasPath => currentPath != null && currentPath.Count > 0;
        public bool HasReachedDestination => Vector3.Distance(ship.Position, destination) <= arrivalThreshold;
        
        public AINavigator(AIEnemyShip ship)
        {
            this.ship = ship;
            currentPath = new List<Vector3>();
        }
        
        public void Update(float deltaTime)
        {
            if (!HasPath) return;
            
            // Follow current path
            FollowPath(deltaTime);
            
            // Check for dynamic obstacles and adjust
            CheckForObstacles(deltaTime);
        }
        
        public void SetDestination(Vector3 newDestination)
        {
            destination = newDestination;
            
            // Calculate new path
            CalculatePath(ship.Position, destination);
        }
        
        public void Stop()
        {
            currentPath.Clear();
            ship.Stop();
        }
        
        private void CalculatePath(Vector3 start, Vector3 end)
        {
            currentPath.Clear();
            pathIndex = 0;
            
            // Use A* pathfinding for complex navigation
            var path = AStar3D.FindPath(start, end, pathFindingRadius, obstacleAvoidanceRadius);
            
            if (path != null && path.Count > 0)
            {
                currentPath = path;
            }
            else
            {
                // Fallback to direct path
                currentPath.Add(start);
                currentPath.Add(end);
            }
        }
        
        private void FollowPath(float deltaTime)
        {
            if (pathIndex >= currentPath.Count) return;
            
            var targetWaypoint = currentPath[pathIndex];
            var distanceToWaypoint = Vector3.Distance(ship.Position, targetWaypoint);
            
            // Move toward current waypoint
            ship.MoveToward(targetWaypoint);
            ship.LookAt(targetWaypoint);
            
            // Check if reached waypoint
            if (distanceToWaypoint <= arrivalThreshold)
            {
                pathIndex++;
                
                // If reached final waypoint
                if (pathIndex >= currentPath.Count)
                {
                    // Arrival behavior
                    OnDestinationReached();
                }
            }
        }
        
        private void CheckForObstacles(float deltaTime)
        {
            // Perform obstacle avoidance
            var avoidanceVector = CalculateObstacleAvoidance();
            
            if (avoidanceVector.LengthSquared() > 0.001f)
            {
                // Apply avoidance to current movement
                var currentVelocity = ship.Velocity;
                var avoidanceForce = avoidanceVector * ship.Speed * 0.5f;
                
                ship.Velocity = Vector3.Lerp(currentVelocity, avoidanceForce, 0.3f);
                
                // Recalculate path if heavily obstructed
                if (avoidanceVector.Length() > 0.7f)
                {
                    RecalculatePathAroundObstacles();
                }
            }
        }
        
        private Vector3 CalculateObstacleAvoidance()
        {
            var avoidanceVector = Vector3.Zero;
            var lookAheadDistance = ship.Speed * 0.5f + 20f;
            
            // Cast rays to detect obstacles
            var rayDirections = new[]
            {
                ship.Forward,
                ship.Forward + ship.Right * 0.5f,
                ship.Forward - ship.Right * 0.5f,
                ship.Forward + ship.Up * 0.3f,
                ship.Forward - ship.Up * 0.3f,
                ship.Right,
                -ship.Right,
                ship.Up,
                -ship.Up
            };
            
            foreach (var direction in rayDirections)
            {
                var normalizedDirection = Vector3.Normalize(direction);
                var rayEnd = ship.Position + normalizedDirection * lookAheadDistance;
                
                if (DetectObstacle(ship.Position, rayEnd, out var obstaclePosition, out var obstacleRadius))
                {
                    // Calculate avoidance direction
                    var toObstacle = obstaclePosition - ship.Position;
                    var distance = toObstacle.Length();
                    
                    if (distance < obstacleAvoidanceRadius + obstacleRadius)
                    {
                        var avoidDirection = CalculateAvoidanceDirection(toObstacle, normalizedDirection);
                        var weight = 1f - (distance / (obstacleAvoidanceRadius + obstacleRadius));
                        
                        avoidanceVector += avoidDirection * weight;
                    }
                }
            }
            
            return Vector3.Normalize(avoidanceVector);
        }
        
        private Vector3 CalculateAvoidanceDirection(Vector3 toObstacle, Vector3 rayDirection)
        {
            // Calculate perpendicular avoidance direction
            var avoidDirection = Vector3.Cross(toObstacle, ship.Up);
            
            // If cross product is too small, use alternative
            if (avoidDirection.LengthSquared() < 0.1f)
            {
                avoidDirection = Vector3.Cross(toObstacle, ship.Right);
            }
            
            // Choose best side to avoid
            var leftAvoid = Vector3.Normalize(avoidDirection);
            var rightAvoid = Vector3.Normalize(-avoidDirection);
            
            // Prefer the side that's more aligned with our destination
            if (HasPath && pathIndex < currentPath.Count)
            {
                var toDestination = Vector3.Normalize(currentPath[pathIndex] - ship.Position);
                var leftDot = Vector3.Dot(leftAvoid, toDestination);
                var rightDot = Vector3.Dot(rightAvoid, toDestination);
                
                return leftDot > rightDot ? leftAvoid : rightAvoid;
            }
            
            return leftAvoid;
        }
        
        private bool DetectObstacle(Vector3 rayStart, Vector3 rayEnd, out Vector3 obstaclePosition, out float radius)
        {
            obstaclePosition = Vector3.Zero;
            radius = 0f;
            
            // Check for other AI ships
            foreach (var otherShip in ship.NearbyAllies)
            {
                if (otherShip == ship) continue;
                
                var shipRadius = otherShip.Size;
                if (RayIntersectsSphere(rayStart, rayEnd, otherShip.Position, shipRadius))
                {
                    obstaclePosition = otherShip.Position;
                    radius = shipRadius;
                    return true;
                }
            }
            
            // Check for world boundaries
            var worldSize = 500f;
            if (Math.Abs(rayEnd.X) > worldSize || 
                Math.Abs(rayEnd.Y) > worldSize || 
                Math.Abs(rayEnd.Z) > worldSize)
            {
                obstaclePosition = rayEnd;
                radius = 10f;
                return true;
            }
            
            // Check for asteroids or other game objects
            // This would integrate with the game's collision system
            // For now, return false
            return false;
        }
        
        private bool RayIntersectsSphere(Vector3 rayStart, Vector3 rayEnd, Vector3 sphereCenter, float sphereRadius)
        {
            var rayDirection = Vector3.Normalize(rayEnd - rayStart);
            var rayLength = Vector3.Distance(rayStart, rayEnd);
            
            var toSphere = sphereCenter - rayStart;
            var projectionLength = Vector3.Dot(toSphere, rayDirection);
            
            // Clamp projection to ray length
            projectionLength = Math.Max(0, Math.Min(rayLength, projectionLength));
            
            var closestPoint = rayStart + rayDirection * projectionLength;
            var distanceToSphere = Vector3.Distance(closestPoint, sphereCenter);
            
            return distanceToSphere <= sphereRadius;
        }
        
        private void RecalculatePathAroundObstacles()
        {
            // Recalculate path with current obstacles in mind
            CalculatePath(ship.Position, destination);
        }
        
        private void OnDestinationReached()
        {
            ship.Stop();
            
            // Notify that destination has been reached
            // This could trigger state changes or other behaviors
        }
        
        public void SetArrivalThreshold(float threshold)
        {
            arrivalThreshold = threshold;
        }
        
        public void SetPathfindingRadius(float radius)
        {
            pathFindingRadius = radius;
        }
        
        public void SetObstacleAvoidanceRadius(float radius)
        {
            obstacleAvoidanceRadius = radius;
        }
        
        public Vector3 GetCurrentWaypoint()
        {
            if (pathIndex < currentPath.Count)
                return currentPath[pathIndex];
            
            return destination;
        }
        
        public float GetDistanceToDestination()
        {
            return Vector3.Distance(ship.Position, destination);
        }
        
        public float GetEstimatedTimeToDestination()
        {
            if (ship.Speed <= 0) return float.MaxValue;
            
            var distance = GetDistanceToDestination();
            return distance / ship.Speed;
        }
    }
    
    /// <summary>
    /// 3D A* pathfinding implementation
    /// </summary>
    public static class AStar3D
    {
        public static List<Vector3> FindPath(Vector3 start, Vector3 end, float searchRadius, float obstacleRadius)
        {
            // Simplified A* implementation for 3D space
            var path = new List<Vector3>();
            
            // For now, implement simple waypoint navigation
            // A full A* implementation would require a 3D grid or node system
            
            var distance = Vector3.Distance(start, end);
            var direction = Vector3.Normalize(end - start);
            
            path.Add(start);
            
            // Add intermediate waypoints for long distances
            if (distance > 100f)
            {
                var waypointCount = (int)(distance / 80f);
                for (int i = 1; i < waypointCount; i++)
                {
                    var t = (float)i / waypointCount;
                    var waypoint = Vector3.Lerp(start, end, t);
                    
                    // Adjust waypoint to avoid known obstacles
                    waypoint = AdjustWaypointForObstacles(waypoint, obstacleRadius);
                    
                    path.Add(waypoint);
                }
            }
            
            path.Add(end);
            
            return path;
        }
        
        private static Vector3 AdjustWaypointForObstacles(Vector3 waypoint, float avoidanceRadius)
        {
            // Simple obstacle avoidance for waypoints
            // In a full implementation, this would check against all known obstacles
            
            return waypoint;
        }
    }
}