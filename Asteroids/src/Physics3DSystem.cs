using System;
using System.Collections.Generic;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Advanced 3D physics system with collision detection, response, and spatial optimization
    /// </summary>
    public class Physics3DSystem
    {
        private readonly List<IPhysicsBody3D> _bodies;
        private readonly Dictionary<IPhysicsBody3D, Vector3> _forces;
        private Vector3 _gravity;
        private BoundingBox3D? _worldBounds;
        private bool _isInitialized;
        private readonly SpatialHash3D _spatialHash;
        private float _timeStep = 1.0f / 60.0f; // Fixed timestep for stability
        
        // Physics constants
        private const float COLLISION_TOLERANCE = 0.001f;
        private const float RESTITUTION = 0.8f;
        private const float DAMPING = 0.99f;
        
        public Physics3DSystem()
        {
            _bodies = new List<IPhysicsBody3D>();
            _forces = new Dictionary<IPhysicsBody3D, Vector3>();
            _gravity = new Vector3(0, -9.81f, 0);
            _spatialHash = new SpatialHash3D(10.0f); // 10 unit grid cells
        }
        
        public bool Initialize()
        {
            try
            {
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize Physics3DSystem", ex);
                return false;
            }
        }
        
        public void AddBody(IPhysicsBody3D body)
        {
            if (!_bodies.Contains(body))
            {
                _bodies.Add(body);
                _spatialHash.AddBody(body);
            }
        }
        
        public void RemoveBody(IPhysicsBody3D body)
        {
            _bodies.Remove(body);
            _spatialHash.RemoveBody(body);
            _forces.Remove(body);
        }
        
        public void Update(float deltaTime)
        {
            if (!_isInitialized) return;
            
            // Use fixed timestep for stability
            float accumulator = deltaTime;
            
            while (accumulator >= _timeStep)
            {
                UpdatePhysicsStep(_timeStep);
                accumulator -= _timeStep;
            }
            
            // Handle remaining time with interpolation
            if (accumulator > 0)
            {
                UpdatePhysicsStep(accumulator);
            }
        }
        
        private void UpdatePhysicsStep(float deltaTime)
        {
            // Update spatial partitioning
            _spatialHash.Clear();
            foreach (var body in _bodies)
            {
                if (body.IsActive)
                {
                    _spatialHash.AddBody(body);
                }
            }
            
            // Apply forces and integrate motion
            IntegrateMotion(deltaTime);
            
            // Detect and resolve collisions
            DetectAndResolveCollisions();
            
            // Apply world bounds
            ApplyWorldBounds();
            
            // Clear forces for next frame
            _forces.Clear();
        }
        
        private void IntegrateMotion(float deltaTime)
        {
            foreach (var body in _bodies)
            {
                if (!body.IsActive || body.IsStatic) continue;
                
                // Apply gravity
                Vector3 totalForce = _gravity * body.Mass;
                
                // Add any additional forces
                if (_forces.TryGetValue(body, out Vector3 additionalForce))
                {
                    totalForce += additionalForce;
                }
                
                // Calculate acceleration
                body.Acceleration = totalForce / body.Mass;
                
                // Integrate velocity (Verlet integration)
                body.Velocity += body.Acceleration * deltaTime;
                
                // Apply damping
                body.Velocity *= DAMPING;
                
                // Integrate position
                body.Position += body.Velocity * deltaTime;
            }
        }
        
        private void DetectAndResolveCollisions()
        {
            var collisionPairs = new HashSet<(IPhysicsBody3D, IPhysicsBody3D)>();
            
            foreach (var body in _bodies)
            {
                if (!body.IsActive) continue;
                
                var nearbyBodies = _spatialHash.GetNearbyBodies(body);
                
                foreach (var other in nearbyBodies)
                {
                    if (body == other || !other.IsActive) continue;
                    
                    // Avoid duplicate collision pairs
                    var pair = body.GetHashCode() < other.GetHashCode() ? (body, other) : (other, body);
                    if (collisionPairs.Contains(pair)) continue;
                    
                    if (AreColliding(body, other))
                    {
                        ResolveCollision(body, other);
                        collisionPairs.Add(pair);
                        
                        // Calculate contact point and normal
                        Vector3 contactPoint = (body.Position + other.Position) * 0.5f;
                        Vector3 contactNormal = Vector3.Normalize(body.Position - other.Position);
                        
                        // Notify bodies of collision
                        body.OnCollision(other, contactPoint, contactNormal);
                        other.OnCollision(body, contactPoint, -contactNormal);
                    }
                }
            }
        }
        
        public bool AreColliding(IPhysicsBody3D body1, IPhysicsBody3D body2)
        {
            float distance = Vector3.Distance(body1.Position, body2.Position);
            float combinedRadius = body1.Radius + body2.Radius;
            return distance <= combinedRadius + COLLISION_TOLERANCE;
        }
        
        private void ResolveCollision(IPhysicsBody3D body1, IPhysicsBody3D body2)
        {
            Vector3 direction = Vector3.Normalize(body1.Position - body2.Position);
            float distance = Vector3.Distance(body1.Position, body2.Position);
            float combinedRadius = body1.Radius + body2.Radius;
            float overlap = combinedRadius - distance + COLLISION_TOLERANCE;
            
            if (overlap <= 0) return;
            
            // Separate bodies
            Vector3 separation = direction * (overlap * 0.5f);
            
            if (!body1.IsStatic)
            {
                body1.Position += separation;
            }
            
            if (!body2.IsStatic)
            {
                body2.Position -= separation;
            }
            
            // Calculate collision response
            if (!body1.IsStatic || !body2.IsStatic)
            {
                Vector3 relativeVelocity = body1.Velocity - body2.Velocity;
                float velocityAlongNormal = Vector3.Dot(relativeVelocity, direction);
                
                // Don't resolve if velocities are separating
                if (velocityAlongNormal > 0) return;
                
                // Calculate restitution
                float e = RESTITUTION;
                
                // Calculate impulse scalar
                float j = -(1 + e) * velocityAlongNormal;
                j /= (1 / body1.Mass + 1 / body2.Mass);
                
                // Apply impulse
                Vector3 impulse = j * direction;
                
                if (!body1.IsStatic)
                {
                    body1.Velocity += impulse / body1.Mass;
                }
                
                if (!body2.IsStatic)
                {
                    body2.Velocity -= impulse / body2.Mass;
                }
            }
        }
        
        private void ApplyWorldBounds()
        {
            if (!_worldBounds.HasValue) return;
            
            var bounds = _worldBounds.Value;
            
            foreach (var body in _bodies)
            {
                if (!body.IsActive || body.IsStatic) continue;
                
                var pos = body.Position;
                var vel = body.Velocity;
                bool bounced = false;
                
                // Check X bounds
                if (pos.X - body.Radius < bounds.Min.X)
                {
                    pos.X = bounds.Min.X + body.Radius;
                    vel.X = Math.Abs(vel.X) * RESTITUTION;
                    bounced = true;
                }
                else if (pos.X + body.Radius > bounds.Max.X)
                {
                    pos.X = bounds.Max.X - body.Radius;
                    vel.X = -Math.Abs(vel.X) * RESTITUTION;
                    bounced = true;
                }
                
                // Check Y bounds
                if (pos.Y - body.Radius < bounds.Min.Y)
                {
                    pos.Y = bounds.Min.Y + body.Radius;
                    vel.Y = Math.Abs(vel.Y) * RESTITUTION;
                    bounced = true;
                }
                else if (pos.Y + body.Radius > bounds.Max.Y)
                {
                    pos.Y = bounds.Max.Y - body.Radius;
                    vel.Y = -Math.Abs(vel.Y) * RESTITUTION;
                    bounced = true;
                }
                
                // Check Z bounds
                if (pos.Z - body.Radius < bounds.Min.Z)
                {
                    pos.Z = bounds.Min.Z + body.Radius;
                    vel.Z = Math.Abs(vel.Z) * RESTITUTION;
                    bounced = true;
                }
                else if (pos.Z + body.Radius > bounds.Max.Z)
                {
                    pos.Z = bounds.Max.Z - body.Radius;
                    vel.Z = -Math.Abs(vel.Z) * RESTITUTION;
                    bounced = true;
                }
                
                if (bounced)
                {
                    body.Position = pos;
                    body.Velocity = vel;
                }
            }
        }
        
        public void ApplyForce(IPhysicsBody3D body, Vector3 force)
        {
            if (_forces.ContainsKey(body))
            {
                _forces[body] += force;
            }
            else
            {
                _forces[body] = force;
            }
        }
        
        public void ApplyImpulse(IPhysicsBody3D body, Vector3 impulse)
        {
            if (!body.IsStatic && body.IsActive)
            {
                body.Velocity += impulse / body.Mass;
            }
        }
        
        public RaycastHit3D? Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            RaycastHit3D? closestHit = null;
            float closestDistance = float.MaxValue;
            
            foreach (var body in _bodies)
            {
                if (!body.IsActive) continue;
                
                // Simple sphere-ray intersection
                Vector3 toSphere = body.Position - origin;
                float projectionLength = Vector3.Dot(toSphere, direction);
                
                // Skip if sphere is behind ray
                if (projectionLength < 0) continue;
                
                Vector3 closestPoint = origin + direction * projectionLength;
                float distanceToCenter = Vector3.Distance(closestPoint, body.Position);
                
                // Check if ray intersects sphere
                if (distanceToCenter <= body.Radius)
                {
                    float intersectionDistance = projectionLength - MathF.Sqrt(body.Radius * body.Radius - distanceToCenter * distanceToCenter);
                    
                    if (intersectionDistance <= maxDistance && intersectionDistance < closestDistance)
                    {
                        Vector3 hitPoint = origin + direction * intersectionDistance;
                        Vector3 normal = Vector3.Normalize(hitPoint - body.Position);
                        
                        closestHit = new RaycastHit3D
                        {
                            Body = body,
                            Point = hitPoint,
                            Normal = normal,
                            Distance = intersectionDistance
                        };
                        closestDistance = intersectionDistance;
                    }
                }
            }
            
            return closestHit;
        }
        
        public void SetGravity(Vector3 gravity)
        {
            _gravity = gravity;
        }
        
        public void SetWorldBounds(BoundingBox3D bounds)
        {
            _worldBounds = bounds;
        }
        
        public int GetBodyCount() => _bodies.Count;
        public bool IsInitialized => _isInitialized;
        
        public void Cleanup()
        {
            _bodies.Clear();
            _forces.Clear();
            _spatialHash.Clear();
            _isInitialized = false;
        }
    }
    
    /// <summary>
    /// Spatial hash grid for efficient collision detection
    /// </summary>
    public class SpatialHash3D
    {
        private readonly Dictionary<Vector3, List<IPhysicsBody3D>> _grid;
        private readonly float _cellSize;
        
        public SpatialHash3D(float cellSize)
        {
            _cellSize = cellSize;
            _grid = new Dictionary<Vector3, List<IPhysicsBody3D>>();
        }
        
        public void AddBody(IPhysicsBody3D body)
        {
            var cellKey = GetCellKey(body.Position);
            
            if (!_grid.ContainsKey(cellKey))
            {
                _grid[cellKey] = new List<IPhysicsBody3D>();
            }
            
            _grid[cellKey].Add(body);
        }
        
        public void RemoveBody(IPhysicsBody3D body)
        {
            var cellKey = GetCellKey(body.Position);
            
            if (_grid.ContainsKey(cellKey))
            {
                _grid[cellKey].Remove(body);
                
                if (_grid[cellKey].Count == 0)
                {
                    _grid.Remove(cellKey);
                }
            }
        }
        
        public List<IPhysicsBody3D> GetNearbyBodies(IPhysicsBody3D body)
        {
            var result = new List<IPhysicsBody3D>();
            var cellKey = GetCellKey(body.Position);
            
            // Check current cell and adjacent cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var adjacentKey = cellKey + new Vector3(x, y, z);
                        
                        if (_grid.ContainsKey(adjacentKey))
                        {
                            result.AddRange(_grid[adjacentKey]);
                        }
                    }
                }
            }
            
            return result;
        }
        
        public void Clear()
        {
            _grid.Clear();
        }
        
        private Vector3 GetCellKey(Vector3 position)
        {
            return new Vector3(
                MathF.Floor(position.X / _cellSize),
                MathF.Floor(position.Y / _cellSize),
                MathF.Floor(position.Z / _cellSize)
            );
        }
    }
}