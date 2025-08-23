# Advanced Collision Detection System - Technical Specification

## Overview

The Advanced Collision Detection System provides high-performance, scalable collision detection for the Asteroids game. It implements a two-phase detection approach with multiple broad-phase algorithms and precise narrow-phase collision resolution.

## System Architecture

### Core Components

```
CollisionSystem
├── CollisionManager (Central coordinator)
├── BroadPhaseManager (Phase 1: Culling)
│   ├── SpatialGridBroadPhase
│   ├── QuadTreeBroadPhase
│   └── SweepAndPruneBroadPhase
├── NarrowPhaseDetector (Phase 2: Precise detection)
│   ├── CircleCircleDetector
│   ├── CirclePolygonDetector
│   └── PolygonPolygonDetector
├── CollisionResolver (Response handling)
├── ContactCache (Persistent collision data)
└── CollisionEventSystem (Event dispatch)
```

## Interface Specifications

### 1. Core Collision Interface

```csharp
public interface ICollidableAdvanced
{
    // Position and movement
    Vector2 Position { get; }
    Vector2 Velocity { get; }
    Vector2 PreviousPosition { get; }
    
    // Shape and bounds
    ICollisionShape Shape { get; }
    BoundingBox BoundingBox { get; }
    float Radius { get; }  // Bounding circle for broad phase
    
    // Collision properties
    CollisionLayer Layer { get; }
    CollisionMask Mask { get; }
    float Mass { get; }
    float Restitution { get; }
    bool IsTrigger { get; }
    bool IsKinematic { get; }
    
    // State
    bool IsActive { get; }
    uint CollisionId { get; }
    
    // Events
    void OnCollisionEnter(CollisionInfo collision);
    void OnCollisionStay(CollisionInfo collision);
    void OnCollisionExit(CollisionInfo collision);
    void OnTriggerEnter(ICollidableAdvanced other);
    void OnTriggerExit(ICollidableAdvanced other);
}
```

### 2. Collision Shape Interface

```csharp
public interface ICollisionShape
{
    ShapeType Type { get; }
    Vector2 Center { get; }
    BoundingBox BoundingBox { get; }
    float Area { get; }
    
    bool Contains(Vector2 point);
    bool Intersects(ICollisionShape other);
    CollisionInfo GetCollisionInfo(ICollisionShape other);
    ICollisionShape Transform(Matrix3x2 transform);
    float GetMomentOfInertia(float mass);
}

public enum ShapeType
{
    Circle,
    AABB,
    OrientedBox,
    Polygon,
    ConvexHull,
    Compound
}
```

### 3. Collision Info Structure

```csharp
public struct CollisionInfo
{
    public ICollidableAdvanced ObjectA;
    public ICollidableAdvanced ObjectB;
    public Vector2 ContactPoint;
    public Vector2 ContactNormal;
    public float PenetrationDepth;
    public float RelativeVelocity;
    public float CombinedRestitution;
    public float CombinedFriction;
    public bool IsSeparating;
    public uint FrameNumber;
    public ContactManifold Manifold;
}

public struct ContactManifold
{
    public Vector2[] ContactPoints;
    public Vector2[] ContactNormals;
    public float[] PenetrationDepths;
    public int ContactCount;
    public uint ManifoldId;
    public float PersistentTime;
}
```

## Broad Phase Detection

### 1. Spatial Grid Implementation

```csharp
public class SpatialGridBroadPhase : IBroadPhaseDetector
{
    private readonly Dictionary<int, HashSet<ICollidableAdvanced>> _grid;
    private readonly float _cellSize;
    private readonly int _worldWidth, _worldHeight;
    private readonly int _gridWidth, _gridHeight;
    
    // Adaptive cell sizing based on object distribution
    private float CalculateOptimalCellSize(IEnumerable<ICollidableAdvanced> objects)
    {
        var avgRadius = objects.Average(obj => obj.Radius);
        var objectDensity = objects.Count() / (float)(_worldWidth * _worldHeight);
        
        // Optimal cell size is typically 2-4x average object radius
        return Math.Max(avgRadius * 2.5f, Math.Min(avgRadius * 4.0f, 
            1.0f / Math.Sqrt(objectDensity)));
    }
    
    public List<CollisionPair> GetPotentialCollisions(IEnumerable<ICollidableAdvanced> objects)
    {
        Clear();
        
        // Insert all objects
        foreach (var obj in objects.Where(o => o.IsActive))
        {
            Insert(obj);
        }
        
        return GenerateCollisionPairs();
    }
    
    private void Insert(ICollidableAdvanced obj)
    {
        var bounds = obj.BoundingBox;
        var cells = GetCellsForBounds(bounds);
        
        foreach (var cellIndex in cells)
        {
            if (!_grid.ContainsKey(cellIndex))
                _grid[cellIndex] = new HashSet<ICollidableAdvanced>();
            
            _grid[cellIndex].Add(obj);
        }
    }
}
```

### 2. QuadTree Implementation

```csharp
public class QuadTreeBroadPhase : IBroadPhaseDetector
{
    private QuadTreeNode _root;
    private readonly int _maxDepth;
    private readonly int _maxObjectsPerNode;
    
    private class QuadTreeNode
    {
        public BoundingBox Bounds;
        public List<ICollidableAdvanced> Objects;
        public QuadTreeNode[] Children; // NW, NE, SW, SE
        public int Depth;
        public bool IsLeaf => Children == null;
        
        public void Subdivide()
        {
            if (!IsLeaf) return;
            
            var center = (Bounds.Min + Bounds.Max) * 0.5f;
            var halfSize = (Bounds.Max - Bounds.Min) * 0.5f;
            
            Children = new QuadTreeNode[4];
            
            // Northwest
            Children[0] = new QuadTreeNode
            {
                Bounds = new BoundingBox(Bounds.Min, center),
                Objects = new List<ICollidableAdvanced>(),
                Depth = Depth + 1
            };
            
            // Northeast
            Children[1] = new QuadTreeNode
            {
                Bounds = new BoundingBox(
                    new Vector2(center.X, Bounds.Min.Y),
                    new Vector2(Bounds.Max.X, center.Y)),
                Objects = new List<ICollidableAdvanced>(),
                Depth = Depth + 1
            };
            
            // Southwest
            Children[2] = new QuadTreeNode
            {
                Bounds = new BoundingBox(
                    new Vector2(Bounds.Min.X, center.Y),
                    new Vector2(center.X, Bounds.Max.Y)),
                Objects = new List<ICollidableAdvanced>(),
                Depth = Depth + 1
            };
            
            // Southeast
            Children[3] = new QuadTreeNode
            {
                Bounds = new BoundingBox(center, Bounds.Max),
                Objects = new List<ICollidableAdvanced>(),
                Depth = Depth + 1
            };
        }
    }
}
```

### 3. Sweep and Prune Implementation

```csharp
public class SweepAndPruneBroadPhase : IBroadPhaseDetector
{
    private struct Endpoint
    {
        public float Value;
        public ICollidableAdvanced Object;
        public bool IsMin;
        public int Axis; // 0 = X, 1 = Y
    }
    
    private List<Endpoint> _endpointsX;
    private List<Endpoint> _endpointsY;
    private HashSet<CollisionPair> _activePairs;
    
    public List<CollisionPair> GetPotentialCollisions(IEnumerable<ICollidableAdvanced> objects)
    {
        UpdateEndpoints(objects);
        
        var potentialPairs = new HashSet<CollisionPair>();
        
        // Sweep X axis
        SweepAxis(_endpointsX, potentialPairs);
        
        // Filter with Y axis
        var finalPairs = new HashSet<CollisionPair>();
        foreach (var pair in potentialPairs)
        {
            if (OverlapsOnAxis(pair.ObjectA, pair.ObjectB, 1)) // Y axis
            {
                finalPairs.Add(pair);
            }
        }
        
        return finalPairs.ToList();
    }
    
    private void SweepAxis(List<Endpoint> endpoints, HashSet<CollisionPair> pairs)
    {
        var activeObjects = new HashSet<ICollidableAdvanced>();
        
        foreach (var endpoint in endpoints.OrderBy(e => e.Value))
        {
            if (endpoint.IsMin)
            {
                // Object is starting, check against all active objects
                foreach (var activeObj in activeObjects)
                {
                    if (CanCollide(endpoint.Object, activeObj))
                    {
                        pairs.Add(new CollisionPair(endpoint.Object, activeObj));
                    }
                }
                activeObjects.Add(endpoint.Object);
            }
            else
            {
                // Object is ending
                activeObjects.Remove(endpoint.Object);
            }
        }
    }
}
```

## Narrow Phase Detection

### 1. Circle-Circle Collision

```csharp
public class CircleCircleDetector : INarrowPhaseDetector
{
    public bool CheckCollision(ICollisionShape shapeA, ICollisionShape shapeB)
    {
        var circleA = (CircleShape)shapeA;
        var circleB = (CircleShape)shapeB;
        
        var distance = Vector2.Distance(circleA.Center, circleB.Center);
        var radiusSum = circleA.Radius + circleB.Radius;
        
        return distance <= radiusSum;
    }
    
    public CollisionInfo GetCollisionInfo(ICollidableAdvanced objA, ICollidableAdvanced objB)
    {
        var circleA = (CircleShape)objA.Shape;
        var circleB = (CircleShape)objB.Shape;
        
        var direction = circleB.Center - circleA.Center;
        var distance = direction.Length();
        var radiusSum = circleA.Radius + circleB.Radius;
        
        if (distance > radiusSum || distance == 0)
            return CollisionInfo.None;
        
        var normal = direction / distance;
        var penetration = radiusSum - distance;
        var contactPoint = circleA.Center + normal * (circleA.Radius - penetration * 0.5f);
        
        return new CollisionInfo
        {
            ObjectA = objA,
            ObjectB = objB,
            ContactPoint = contactPoint,
            ContactNormal = normal,
            PenetrationDepth = penetration,
            RelativeVelocity = Vector2.Dot(objB.Velocity - objA.Velocity, normal),
            CombinedRestitution = Math.Min(objA.Restitution, objB.Restitution),
            IsSeparating = Vector2.Dot(objB.Velocity - objA.Velocity, normal) > 0
        };
    }
}
```

### 2. Circle-Polygon Collision

```csharp
public class CirclePolygonDetector : INarrowPhaseDetector
{
    public CollisionInfo GetCollisionInfo(ICollidableAdvanced circle, ICollidableAdvanced polygon)
    {
        var circleShape = (CircleShape)circle.Shape;
        var polyShape = (PolygonShape)polygon.Shape;
        
        // Find closest point on polygon to circle center
        var closestPoint = FindClosestPointOnPolygon(circleShape.Center, polyShape);
        var direction = circleShape.Center - closestPoint;
        var distance = direction.Length();
        
        if (distance > circleShape.Radius)
            return CollisionInfo.None;
        
        Vector2 normal;
        float penetration;
        
        if (distance == 0)
        {
            // Circle center is inside polygon, find deepest penetration
            var penetrationInfo = FindDeepestPenetration(circleShape.Center, polyShape);
            normal = penetrationInfo.Normal;
            penetration = circleShape.Radius + penetrationInfo.Depth;
        }
        else
        {
            normal = direction / distance;
            penetration = circleShape.Radius - distance;
        }
        
        return new CollisionInfo
        {
            ObjectA = circle,
            ObjectB = polygon,
            ContactPoint = closestPoint,
            ContactNormal = normal,
            PenetrationDepth = penetration,
            // ... other properties
        };
    }
    
    private Vector2 FindClosestPointOnPolygon(Vector2 point, PolygonShape polygon)
    {
        var closestPoint = polygon.Vertices[0];
        var minDistance = float.MaxValue;
        
        // Check each edge of the polygon
        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            var v1 = polygon.Vertices[i];
            var v2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length];
            
            var pointOnEdge = ClosestPointOnLineSegment(point, v1, v2);
            var distance = Vector2.DistanceSquared(point, pointOnEdge);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = pointOnEdge;
            }
        }
        
        return closestPoint;
    }
}
```

## Collision Resolution

### 1. Impulse-Based Resolution

```csharp
public class ImpulseBasedResolver : ICollisionResolver
{
    public void ResolveCollision(CollisionInfo collision)
    {
        var objA = collision.ObjectA;
        var objB = collision.ObjectB;
        
        if (objA.IsKinematic && objB.IsKinematic)
            return; // Both kinematic, no resolution needed
        
        // Calculate relative velocity
        var relativeVelocity = objB.Velocity - objA.Velocity;
        var separatingVelocity = Vector2.Dot(relativeVelocity, collision.ContactNormal);
        
        // Objects separating, no resolution needed
        if (separatingVelocity > 0)
            return;
        
        // Calculate impulse magnitude
        var restitution = collision.CombinedRestitution;
        var impulseNumerator = -(1 + restitution) * separatingVelocity;
        var impulseDenominator = (1 / objA.Mass) + (1 / objB.Mass);
        var impulseMagnitude = impulseNumerator / impulseDenominator;
        
        // Apply impulse
        var impulse = collision.ContactNormal * impulseMagnitude;
        
        if (!objA.IsKinematic)
        {
            objA.Velocity -= impulse / objA.Mass;
        }
        
        if (!objB.IsKinematic)
        {
            objB.Velocity += impulse / objB.Mass;
        }
        
        // Position correction to prevent penetration
        CorrectPosition(collision);
    }
    
    private void CorrectPosition(CollisionInfo collision)
    {
        const float correctionPercentage = 0.8f;
        const float correctionThreshold = 0.01f;
        
        if (collision.PenetrationDepth <= correctionThreshold)
            return;
        
        var correction = collision.ContactNormal * 
            (collision.PenetrationDepth * correctionPercentage / 
            ((1 / collision.ObjectA.Mass) + (1 / collision.ObjectB.Mass)));
        
        if (!collision.ObjectA.IsKinematic)
        {
            collision.ObjectA.Position -= correction / collision.ObjectA.Mass;
        }
        
        if (!collision.ObjectB.IsKinematic)
        {
            collision.ObjectB.Position += correction / collision.ObjectB.Mass;
        }
    }
}
```

## Performance Optimizations

### 1. Contact Manifold Caching

```csharp
public class ContactCache
{
    private Dictionary<uint, ContactManifold> _manifolds = new();
    private uint _frameNumber;
    
    public ContactManifold GetOrCreateManifold(ICollidableAdvanced objA, ICollidableAdvanced objB)
    {
        var key = GenerateManifoldKey(objA.CollisionId, objB.CollisionId);
        
        if (_manifolds.TryGetValue(key, out var existingManifold))
        {
            existingManifold.PersistentTime += Time.DeltaTime;
            return existingManifold;
        }
        
        var newManifold = new ContactManifold
        {
            ManifoldId = key,
            PersistentTime = 0f
        };
        
        _manifolds[key] = newManifold;
        return newManifold;
    }
    
    public void CleanupOldManifolds()
    {
        var keysToRemove = _manifolds.Where(kvp => 
            _frameNumber - kvp.Value.LastUpdateFrame > 3).Select(kvp => kvp.Key).ToList();
        
        foreach (var key in keysToRemove)
        {
            _manifolds.Remove(key);
        }
    }
}
```

### 2. Collision Layer System

```csharp
[Flags]
public enum CollisionLayer : uint
{
    Default = 1 << 0,
    Player = 1 << 1,
    Enemies = 1 << 2,
    Bullets = 1 << 3,
    Asteroids = 1 << 4,
    PowerUps = 1 << 5,
    Particles = 1 << 6,
    UI = 1 << 7,
    Environment = 1 << 8,
    Triggers = 1 << 9
}

public static class CollisionMatrix
{
    private static readonly Dictionary<CollisionLayer, CollisionLayer> _collisionMatrix = new()
    {
        [CollisionLayer.Player] = CollisionLayer.Enemies | CollisionLayer.Asteroids | CollisionLayer.PowerUps | CollisionLayer.Triggers,
        [CollisionLayer.Bullets] = CollisionLayer.Enemies | CollisionLayer.Asteroids | CollisionLayer.Environment,
        [CollisionLayer.Enemies] = CollisionLayer.Player | CollisionLayer.Bullets | CollisionLayer.Asteroids,
        [CollisionLayer.Asteroids] = CollisionLayer.Player | CollisionLayer.Bullets | CollisionLayer.Enemies | CollisionLayer.Asteroids,
        // ... other layer interactions
    };
    
    public static bool CanCollide(CollisionLayer layerA, CollisionLayer layerB)
    {
        return _collisionMatrix.ContainsKey(layerA) && 
               (_collisionMatrix[layerA] & layerB) != 0;
    }
}
```

## Integration with Game Systems

### 1. Game Loop Integration

```csharp
public class CollisionSystem : IGameSystem
{
    public void Update(float deltaTime)
    {
        // 1. Broad phase: Get potential collision pairs
        var potentialPairs = _broadPhaseDetector.GetPotentialCollisions(_collidables);
        
        // 2. Narrow phase: Test actual collisions
        var collisions = new List<CollisionInfo>();
        foreach (var pair in potentialPairs)
        {
            var collision = _narrowPhaseDetector.TestCollision(pair.ObjectA, pair.ObjectB);
            if (collision.HasValue)
            {
                collisions.Add(collision.Value);
            }
        }
        
        // 3. Resolve collisions
        foreach (var collision in collisions)
        {
            _collisionResolver.ResolveCollision(collision);
        }
        
        // 4. Fire collision events
        _eventSystem.ProcessCollisionEvents(collisions);
        
        // 5. Cleanup old contact manifolds
        _contactCache.CleanupOldManifolds();
    }
}
```

## Configuration and Tuning

```csharp
public class CollisionSystemConfig
{
    // Broad phase settings
    public BroadPhaseType PreferredBroadPhase { get; set; } = BroadPhaseType.SpatialGrid;
    public float SpatialGridCellSize { get; set; } = 50f;
    public int QuadTreeMaxDepth { get; set; } = 6;
    public int QuadTreeMaxObjects { get; set; } = 8;
    
    // Narrow phase settings
    public bool UseContactManifolds { get; set; } = true;
    public float ContactThreshold { get; set; } = 0.01f;
    public int MaxContactPoints { get; set; } = 8;
    
    // Resolution settings
    public float PositionCorrectionPercentage { get; set; } = 0.8f;
    public float VelocityThreshold { get; set; } = 0.01f;
    public int MaxResolutionIterations { get; set; } = 8;
    
    // Performance settings
    public int MaxCollisionPairsPerFrame { get; set; } = 1000;
    public bool EnableTemporalCoherence { get; set; } = true;
    public bool EnableCollisionCaching { get; set; } = true;
}
```

This collision detection system provides high performance, accuracy, and flexibility for the Asteroids game while maintaining extensibility for future enhancements.