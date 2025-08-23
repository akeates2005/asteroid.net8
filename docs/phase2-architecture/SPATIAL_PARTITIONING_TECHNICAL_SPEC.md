# Spatial Partitioning System - Technical Specification

## Overview

The Spatial Partitioning System provides efficient spatial organization and queries for game objects in the Asteroids game. It implements multiple data structures optimized for different use cases, enabling fast collision detection, visibility culling, and neighbor queries.

## System Architecture

```
SpatialManager
├── ISpatialStructure (Common interface)
├── Level 0: AdaptiveSpatialGrid (Fast insertion/removal)
├── Level 1: DynamicQuadTree (Hierarchical queries)  
├── Level 2: LooseQuadTree (Moving objects)
├── Level 3: RTree (Complex shapes)
├── Level 4: BSPTree (Scene partitioning)
├── SpatialQueryCache (Query optimization)
└── SpatialMetrics (Performance tracking)
```

## Core Interfaces

### 1. Spatial Structure Interface

```csharp
public interface ISpatialStructure : IDisposable
{
    /// <summary>
    /// Name/type of the spatial structure
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Bounding region covered by this structure
    /// </summary>
    BoundingBox WorldBounds { get; }
    
    /// <summary>
    /// Number of objects currently stored
    /// </summary>
    int ObjectCount { get; }
    
    /// <summary>
    /// Insert object into spatial structure
    /// </summary>
    void Insert(ISpatialObject obj);
    
    /// <summary>
    /// Remove object from spatial structure
    /// </summary>
    bool Remove(ISpatialObject obj);
    
    /// <summary>
    /// Update object position (more efficient than Remove + Insert)
    /// </summary>
    void Update(ISpatialObject obj, BoundingBox previousBounds);
    
    /// <summary>
    /// Clear all objects from structure
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Query objects within region
    /// </summary>
    List<ISpatialObject> Query(BoundingBox region);
    
    /// <summary>
    /// Query objects within radius of point
    /// </summary>
    List<ISpatialObject> Query(Vector2 center, float radius);
    
    /// <summary>
    /// Query objects intersecting with ray
    /// </summary>
    List<RaycastHit> Raycast(Vector2 origin, Vector2 direction, float maxDistance);
    
    /// <summary>
    /// Get nearest object to point
    /// </summary>
    ISpatialObject FindNearest(Vector2 point, float maxDistance = float.MaxValue);
    
    /// <summary>
    /// Get all objects within range, sorted by distance
    /// </summary>
    List<ISpatialObject> FindNearby(Vector2 point, float radius, int maxResults = int.MaxValue);
    
    /// <summary>
    /// Optimize structure (rebalance, cleanup, etc.)
    /// </summary>
    void Optimize();
    
    /// <summary>
    /// Get performance statistics
    /// </summary>
    SpatialStatistics GetStatistics();
}
```

### 2. Spatial Object Interface

```csharp
public interface ISpatialObject
{
    /// <summary>
    /// Unique identifier for the object
    /// </summary>
    uint SpatialId { get; }
    
    /// <summary>
    /// Current position of the object
    /// </summary>
    Vector2 Position { get; }
    
    /// <summary>
    /// Previous position (for movement prediction)
    /// </summary>
    Vector2 PreviousPosition { get; }
    
    /// <summary>
    /// Velocity of the object
    /// </summary>
    Vector2 Velocity { get; }
    
    /// <summary>
    /// Bounding box for broad-phase queries
    /// </summary>
    BoundingBox BoundingBox { get; }
    
    /// <summary>
    /// Bounding radius for circular queries
    /// </summary>
    float BoundingRadius { get; }
    
    /// <summary>
    /// Spatial layer for filtering
    /// </summary>
    SpatialLayer Layer { get; }
    
    /// <summary>
    /// Whether object is currently active
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Whether object is static (doesn't move)
    /// </summary>
    bool IsStatic { get; }
    
    /// <summary>
    /// Predict position at future time
    /// </summary>
    Vector2 PredictPosition(float deltaTime);
    
    /// <summary>
    /// Get predicted bounding box at future time
    /// </summary>
    BoundingBox PredictBounds(float deltaTime);
}

[Flags]
public enum SpatialLayer : uint
{
    None = 0,
    Static = 1 << 0,
    Dynamic = 1 << 1,
    Player = 1 << 2,
    Enemies = 1 << 3,
    Bullets = 1 << 4,
    Asteroids = 1 << 5,
    Particles = 1 << 6,
    PowerUps = 1 << 7,
    Effects = 1 << 8,
    Environment = 1 << 9,
    All = uint.MaxValue
}
```

## Spatial Data Structures

### 1. Adaptive Spatial Grid

```csharp
public class AdaptiveSpatialGrid : ISpatialStructure
{
    private readonly Dictionary<int, HashSet<ISpatialObject>> _cells;
    private readonly Dictionary<uint, List<int>> _objectCells; // Track which cells each object occupies
    private float _cellSize;
    private readonly BoundingBox _worldBounds;
    private readonly int _gridWidth, _gridHeight;
    private readonly SpatialMetrics _metrics;
    
    // Adaptive features
    private readonly Dictionary<int, CellMetrics> _cellMetrics;
    private DateTime _lastOptimization;
    private readonly TimeSpan _optimizationInterval = TimeSpan.FromSeconds(5);
    
    public string Name => "AdaptiveSpatialGrid";
    public BoundingBox WorldBounds => _worldBounds;
    public int ObjectCount { get; private set; }
    
    public AdaptiveSpatialGrid(BoundingBox worldBounds, float initialCellSize)
    {
        _worldBounds = worldBounds;
        _cellSize = initialCellSize;
        _gridWidth = (int)Math.Ceiling(worldBounds.Width / _cellSize);
        _gridHeight = (int)Math.Ceiling(worldBounds.Height / _cellSize);
        
        _cells = new Dictionary<int, HashSet<ISpatialObject>>();
        _objectCells = new Dictionary<uint, List<int>>();
        _cellMetrics = new Dictionary<int, CellMetrics>();
        _metrics = new SpatialMetrics(Name);
        
        _lastOptimization = DateTime.UtcNow;
    }
    
    public void Insert(ISpatialObject obj)
    {
        if (obj == null || !obj.IsActive) return;
        
        _metrics.RecordInsertion();
        var cellIndices = GetCellIndices(obj.BoundingBox);
        
        _objectCells[obj.SpatialId] = cellIndices;
        
        foreach (var cellIndex in cellIndices)
        {
            if (!_cells.ContainsKey(cellIndex))
            {
                _cells[cellIndex] = new HashSet<ISpatialObject>();
                _cellMetrics[cellIndex] = new CellMetrics();
            }
            
            _cells[cellIndex].Add(obj);
            _cellMetrics[cellIndex].ObjectCount++;
        }
        
        ObjectCount++;
    }
    
    public bool Remove(ISpatialObject obj)
    {
        if (obj == null) return false;
        
        _metrics.RecordRemoval();
        
        if (!_objectCells.TryGetValue(obj.SpatialId, out var cellIndices))
            return false;
        
        foreach (var cellIndex in cellIndices)
        {
            if (_cells.TryGetValue(cellIndex, out var cell))
            {
                cell.Remove(obj);
                _cellMetrics[cellIndex].ObjectCount--;
                
                if (cell.Count == 0)
                {
                    _cells.Remove(cellIndex);
                    _cellMetrics.Remove(cellIndex);
                }
            }
        }
        
        _objectCells.Remove(obj.SpatialId);
        ObjectCount--;
        return true;
    }
    
    public void Update(ISpatialObject obj, BoundingBox previousBounds)
    {
        if (obj == null || !obj.IsActive) return;
        
        _metrics.RecordUpdate();
        
        var oldCellIndices = _objectCells.GetValueOrDefault(obj.SpatialId, new List<int>());
        var newCellIndices = GetCellIndices(obj.BoundingBox);
        
        // Find cells to remove from and add to
        var cellsToRemove = oldCellIndices.Except(newCellIndices).ToList();
        var cellsToAdd = newCellIndices.Except(oldCellIndices).ToList();
        
        // Remove from old cells
        foreach (var cellIndex in cellsToRemove)
        {
            if (_cells.TryGetValue(cellIndex, out var cell))
            {
                cell.Remove(obj);
                _cellMetrics[cellIndex].ObjectCount--;
                
                if (cell.Count == 0)
                {
                    _cells.Remove(cellIndex);
                    _cellMetrics.Remove(cellIndex);
                }
            }
        }
        
        // Add to new cells
        foreach (var cellIndex in cellsToAdd)
        {
            if (!_cells.ContainsKey(cellIndex))
            {
                _cells[cellIndex] = new HashSet<ISpatialObject>();
                _cellMetrics[cellIndex] = new CellMetrics();
            }
            
            _cells[cellIndex].Add(obj);
            _cellMetrics[cellIndex].ObjectCount++;
        }
        
        _objectCells[obj.SpatialId] = newCellIndices;
    }
    
    public List<ISpatialObject> Query(BoundingBox region)
    {
        _metrics.RecordQuery();
        
        var results = new HashSet<ISpatialObject>();
        var cellIndices = GetCellIndices(region);
        
        foreach (var cellIndex in cellIndices)
        {
            if (_cells.TryGetValue(cellIndex, out var cell))
            {
                foreach (var obj in cell)
                {
                    if (obj.IsActive && obj.BoundingBox.Intersects(region))
                    {
                        results.Add(obj);
                    }
                }
            }
        }
        
        return results.ToList();
    }
    
    public List<ISpatialObject> Query(Vector2 center, float radius)
    {
        var region = new BoundingBox(
            center - Vector2.One * radius,
            center + Vector2.One * radius);
        
        var candidates = Query(region);
        var results = new List<ISpatialObject>();
        var radiusSquared = radius * radius;
        
        foreach (var obj in candidates)
        {
            var distanceSquared = Vector2.DistanceSquared(obj.Position, center);
            if (distanceSquared <= radiusSquared + obj.BoundingRadius * obj.BoundingRadius)
            {
                results.Add(obj);
            }
        }
        
        return results;
    }
    
    public void Optimize()
    {
        if (DateTime.UtcNow - _lastOptimization < _optimizationInterval)
            return;
        
        _metrics.RecordOptimization();
        
        // Analyze cell density and adjust cell size if needed
        var avgObjectsPerCell = ObjectCount > 0 ? (float)ObjectCount / _cells.Count : 0;
        var targetObjectsPerCell = 8.0f; // Optimal number of objects per cell
        
        if (avgObjectsPerCell > targetObjectsPerCell * 2)
        {
            // Too many objects per cell, make cells smaller
            ResizeGrid(_cellSize * 0.8f);
        }
        else if (avgObjectsPerCell < targetObjectsPerCell * 0.5f && _cellSize < 200)
        {
            // Too few objects per cell, make cells larger
            ResizeGrid(_cellSize * 1.2f);
        }
        
        _lastOptimization = DateTime.UtcNow;
    }
    
    private void ResizeGrid(float newCellSize)
    {
        var oldObjects = new List<ISpatialObject>();
        
        // Collect all objects
        foreach (var cell in _cells.Values)
        {
            oldObjects.AddRange(cell);
        }
        
        // Clear and resize
        Clear();
        _cellSize = newCellSize;
        _gridWidth = (int)Math.Ceiling(_worldBounds.Width / _cellSize);
        _gridHeight = (int)Math.Ceiling(_worldBounds.Height / _cellSize);
        
        // Re-insert objects
        foreach (var obj in oldObjects.Distinct())
        {
            Insert(obj);
        }
        
        _metrics.RecordResize();
    }
    
    private List<int> GetCellIndices(BoundingBox bounds)
    {
        var indices = new List<int>();
        
        int minX = Math.Max(0, (int)((bounds.Min.X - _worldBounds.Min.X) / _cellSize));
        int minY = Math.Max(0, (int)((bounds.Min.Y - _worldBounds.Min.Y) / _cellSize));
        int maxX = Math.Min(_gridWidth - 1, (int)((bounds.Max.X - _worldBounds.Min.X) / _cellSize));
        int maxY = Math.Min(_gridHeight - 1, (int)((bounds.Max.Y - _worldBounds.Min.Y) / _cellSize));
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                indices.Add(y * _gridWidth + x);
            }
        }
        
        return indices;
    }
    
    public SpatialStatistics GetStatistics()
    {
        return new SpatialStatistics
        {
            Name = Name,
            ObjectCount = ObjectCount,
            CellCount = _cells.Count,
            AverageObjectsPerCell = _cells.Count > 0 ? (float)ObjectCount / _cells.Count : 0,
            MaxObjectsInCell = _cells.Values.Count > 0 ? _cells.Values.Max(c => c.Count) : 0,
            CellSize = _cellSize,
            Metrics = _metrics.GetSnapshot()
        };
    }
    
    // ... other methods
}
```

### 2. Dynamic QuadTree

```csharp
public class DynamicQuadTree : ISpatialStructure
{
    private QuadTreeNode _root;
    private readonly int _maxDepth;
    private readonly int _maxObjectsPerNode;
    private readonly BoundingBox _worldBounds;
    private readonly SpatialMetrics _metrics;
    private readonly Dictionary<uint, QuadTreeNode> _objectNodes; // Track which node each object is in
    
    public string Name => "DynamicQuadTree";
    public BoundingBox WorldBounds => _worldBounds;
    public int ObjectCount { get; private set; }
    
    public DynamicQuadTree(BoundingBox worldBounds, int maxDepth = 8, int maxObjectsPerNode = 10)
    {
        _worldBounds = worldBounds;
        _maxDepth = maxDepth;
        _maxObjectsPerNode = maxObjectsPerNode;
        _metrics = new SpatialMetrics(Name);
        _objectNodes = new Dictionary<uint, QuadTreeNode>();
        
        _root = new QuadTreeNode
        {
            Bounds = worldBounds,
            Objects = new List<ISpatialObject>(),
            Depth = 0
        };
    }
    
    private class QuadTreeNode
    {
        public BoundingBox Bounds;
        public List<ISpatialObject> Objects;
        public QuadTreeNode[] Children; // NW, NE, SW, SE
        public int Depth;
        public QuadTreeNode Parent;
        
        public bool IsLeaf => Children == null;
        public bool IsEmpty => Objects.Count == 0 && IsLeaf;
        
        public void Subdivide(int maxDepth, int maxObjectsPerNode)
        {
            if (!IsLeaf || Depth >= maxDepth) return;
            
            var center = Bounds.Center;
            var halfWidth = Bounds.Width * 0.5f;
            var halfHeight = Bounds.Height * 0.5f;
            
            Children = new QuadTreeNode[4];
            
            // Northwest
            Children[0] = CreateChildNode(
                new BoundingBox(Bounds.Min, center), 0);
            
            // Northeast  
            Children[1] = CreateChildNode(
                new BoundingBox(new Vector2(center.X, Bounds.Min.Y), 
                new Vector2(Bounds.Max.X, center.Y)), 1);
            
            // Southwest
            Children[2] = CreateChildNode(
                new BoundingBox(new Vector2(Bounds.Min.X, center.Y),
                new Vector2(center.X, Bounds.Max.Y)), 2);
            
            // Southeast
            Children[3] = CreateChildNode(
                new BoundingBox(center, Bounds.Max), 3);
            
            // Redistribute objects to children
            var objectsToRedistribute = Objects.ToList();
            Objects.Clear();
            
            foreach (var obj in objectsToRedistribute)
            {
                InsertIntoNode(obj, this, maxDepth, maxObjectsPerNode);
            }
        }
        
        private QuadTreeNode CreateChildNode(BoundingBox bounds, int quadrant)
        {
            return new QuadTreeNode
            {
                Bounds = bounds,
                Objects = new List<ISpatialObject>(),
                Depth = Depth + 1,
                Parent = this
            };
        }
        
        public void Merge()
        {
            if (IsLeaf) return;
            
            // Collect all objects from children
            var allObjects = new List<ISpatialObject>();
            foreach (var child in Children)
            {
                if (child != null)
                {
                    CollectAllObjects(child, allObjects);
                }
            }
            
            // Remove children
            Children = null;
            Objects = allObjects;
        }
        
        private void CollectAllObjects(QuadTreeNode node, List<ISpatialObject> objects)
        {
            objects.AddRange(node.Objects);
            
            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        CollectAllObjects(child, objects);
                    }
                }
            }
        }
    }
    
    public void Insert(ISpatialObject obj)
    {
        if (obj == null || !obj.IsActive) return;
        
        _metrics.RecordInsertion();
        InsertIntoNode(obj, _root, _maxDepth, _maxObjectsPerNode);
        ObjectCount++;
    }
    
    private void InsertIntoNode(ISpatialObject obj, QuadTreeNode node, int maxDepth, int maxObjectsPerNode)
    {
        // If node is a leaf
        if (node.IsLeaf)
        {
            node.Objects.Add(obj);
            _objectNodes[obj.SpatialId] = node;
            
            // Check if subdivision is needed
            if (node.Objects.Count > maxObjectsPerNode && node.Depth < maxDepth)
            {
                node.Subdivide(maxDepth, maxObjectsPerNode);
            }
        }
        else
        {
            // Find which child(ren) the object belongs to
            var objBounds = obj.BoundingBox;
            bool inserted = false;
            
            foreach (var child in node.Children)
            {
                if (child.Bounds.Contains(objBounds))
                {
                    InsertIntoNode(obj, child, maxDepth, maxObjectsPerNode);
                    inserted = true;
                    break;
                }
            }
            
            // If object doesn't fit completely in any child, store in current node
            if (!inserted)
            {
                node.Objects.Add(obj);
                _objectNodes[obj.SpatialId] = node;
            }
        }
    }
    
    public bool Remove(ISpatialObject obj)
    {
        if (obj == null) return false;
        
        _metrics.RecordRemoval();
        
        if (!_objectNodes.TryGetValue(obj.SpatialId, out var node))
            return false;
        
        bool removed = node.Objects.Remove(obj);
        if (removed)
        {
            _objectNodes.Remove(obj.SpatialId);
            ObjectCount--;
            
            // Try to merge parent if it becomes sparse
            TryMergeUp(node);
        }
        
        return removed;
    }
    
    private void TryMergeUp(QuadTreeNode node)
    {
        var current = node.Parent;
        
        while (current != null)
        {
            int totalObjects = current.Objects.Count;
            
            if (!current.IsLeaf)
            {
                foreach (var child in current.Children)
                {
                    totalObjects += CountObjects(child);
                }
            }
            
            // If total objects in this subtree is small enough, merge
            if (totalObjects <= _maxObjectsPerNode / 2)
            {
                current.Merge();
            }
            
            current = current.Parent;
        }
    }
    
    private int CountObjects(QuadTreeNode node)
    {
        int count = node.Objects.Count;
        
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                count += CountObjects(child);
            }
        }
        
        return count;
    }
    
    public List<ISpatialObject> Query(BoundingBox region)
    {
        _metrics.RecordQuery();
        
        var results = new List<ISpatialObject>();
        QueryNode(_root, region, results);
        return results;
    }
    
    private void QueryNode(QuadTreeNode node, BoundingBox region, List<ISpatialObject> results)
    {
        if (!node.Bounds.Intersects(region))
            return;
        
        // Check objects in this node
        foreach (var obj in node.Objects)
        {
            if (obj.IsActive && obj.BoundingBox.Intersects(region))
            {
                results.Add(obj);
            }
        }
        
        // Recursively check children
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    QueryNode(child, region, results);
                }
            }
        }
    }
    
    public List<RaycastHit> Raycast(Vector2 origin, Vector2 direction, float maxDistance)
    {
        _metrics.RecordRaycast();
        
        var results = new List<RaycastHit>();
        var normalizedDirection = Vector2.Normalize(direction);
        var endPoint = origin + normalizedDirection * maxDistance;
        
        RaycastNode(_root, origin, endPoint, normalizedDirection, maxDistance, results);
        
        return results.OrderBy(hit => hit.Distance).ToList();
    }
    
    private void RaycastNode(QuadTreeNode node, Vector2 origin, Vector2 endPoint, 
        Vector2 direction, float maxDistance, List<RaycastHit> results)
    {
        if (!RayIntersectsBounds(origin, endPoint, node.Bounds))
            return;
        
        // Test objects in this node
        foreach (var obj in node.Objects)
        {
            if (!obj.IsActive) continue;
            
            var hit = RaycastObject(origin, direction, obj, maxDistance);
            if (hit.HasValue)
            {
                results.Add(hit.Value);
            }
        }
        
        // Recursively test children
        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                if (child != null)
                {
                    RaycastNode(child, origin, endPoint, direction, maxDistance, results);
                }
            }
        }
    }
    
    public SpatialStatistics GetStatistics()
    {
        var nodeCount = CountNodes(_root);
        var maxDepth = GetMaxDepth(_root);
        
        return new SpatialStatistics
        {
            Name = Name,
            ObjectCount = ObjectCount,
            NodeCount = nodeCount,
            MaxDepth = maxDepth,
            AverageObjectsPerNode = nodeCount > 0 ? (float)ObjectCount / nodeCount : 0,
            Metrics = _metrics.GetSnapshot()
        };
    }
    
    // ... additional helper methods
}
```

### 3. Loose QuadTree (for Moving Objects)

```csharp
public class LooseQuadTree : ISpatialStructure
{
    private LooseNode _root;
    private readonly float _looseFactor; // How much larger cells are than objects
    private readonly SpatialMetrics _metrics;
    
    public string Name => "LooseQuadTree";
    public BoundingBox WorldBounds { get; private set; }
    public int ObjectCount { get; private set; }
    
    public LooseQuadTree(BoundingBox worldBounds, float looseFactor = 2.0f)
    {
        WorldBounds = worldBounds;
        _looseFactor = looseFactor;
        _metrics = new SpatialMetrics(Name);
        
        _root = new LooseNode
        {
            TightBounds = worldBounds,
            LooseBounds = worldBounds.Expand(_looseFactor),
            Objects = new List<ISpatialObject>()
        };
    }
    
    private class LooseNode
    {
        public BoundingBox TightBounds;   // Actual node bounds
        public BoundingBox LooseBounds;   // Expanded bounds for loose fitting
        public List<ISpatialObject> Objects;
        public LooseNode[] Children;
        public bool IsLeaf => Children == null;
    }
    
    // Loose quadtrees are particularly good for moving objects
    // because they reduce the need for frequent relocations
    
    public void Insert(ISpatialObject obj)
    {
        if (obj == null || !obj.IsActive) return;
        
        _metrics.RecordInsertion();
        
        // Predict where object will be based on velocity
        var predictedBounds = obj.PredictBounds(0.1f); // 100ms prediction
        
        InsertIntoLooseNode(obj, _root, predictedBounds);
        ObjectCount++;
    }
    
    private void InsertIntoLooseNode(ISpatialObject obj, LooseNode node, BoundingBox predictedBounds)
    {
        // Check if object fits in loose bounds
        if (!node.LooseBounds.Contains(predictedBounds))
        {
            node.Objects.Add(obj);
            return;
        }
        
        if (node.IsLeaf)
        {
            node.Objects.Add(obj);
            
            // Consider subdivision if too many objects
            if (node.Objects.Count > 16) // Higher threshold due to loose nature
            {
                SubdivideLooseNode(node);
            }
        }
        else
        {
            // Try to fit in a child
            bool inserted = false;
            foreach (var child in node.Children)
            {
                if (child.LooseBounds.Contains(predictedBounds))
                {
                    InsertIntoLooseNode(obj, child, predictedBounds);
                    inserted = true;
                    break;
                }
            }
            
            if (!inserted)
            {
                node.Objects.Add(obj);
            }
        }
    }
    
    // ... rest of implementation similar to regular quadtree but with loose bounds
}
```

## Spatial Manager

### 1. Multi-Level Spatial Management

```csharp
public class SpatialManager : IDisposable
{
    private readonly Dictionary<SpatialLayer, ISpatialStructure> _spatialStructures;
    private readonly SpatialQueryCache _queryCache;
    private readonly SpatialMetrics _globalMetrics;
    private readonly Timer _optimizationTimer;
    
    // Default structures for different object types
    private readonly AdaptiveSpatialGrid _gridForStatic;
    private readonly DynamicQuadTree _quadTreeForDynamic;
    private readonly LooseQuadTree _looseTreeForFast;
    
    public SpatialManager(BoundingBox worldBounds)
    {
        _spatialStructures = new Dictionary<SpatialLayer, ISpatialStructure>();
        _queryCache = new SpatialQueryCache();
        _globalMetrics = new SpatialMetrics("SpatialManager");
        
        // Initialize different structures for different needs
        _gridForStatic = new AdaptiveSpatialGrid(worldBounds, 100f);
        _quadTreeForDynamic = new DynamicQuadTree(worldBounds, 8, 10);
        _looseTreeForFast = new LooseQuadTree(worldBounds, 2.5f);
        
        // Map layers to appropriate structures
        _spatialStructures[SpatialLayer.Static | SpatialLayer.Environment] = _gridForStatic;
        _spatialStructures[SpatialLayer.Player | SpatialLayer.Enemies] = _quadTreeForDynamic;
        _spatialStructures[SpatialLayer.Bullets | SpatialLayer.Particles] = _looseTreeForFast;
        _spatialStructures[SpatialLayer.Asteroids] = _quadTreeForDynamic;
        
        // Set up periodic optimization
        _optimizationTimer = new Timer(OptimizeStructures, null,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    public void Insert(ISpatialObject obj)
    {
        var structure = GetBestStructureForObject(obj);
        structure.Insert(obj);
        _globalMetrics.RecordInsertion();
    }
    
    public bool Remove(ISpatialObject obj)
    {
        // Try to remove from all structures (inefficient but safe)
        bool removed = false;
        foreach (var structure in _spatialStructures.Values)
        {
            if (structure.Remove(obj))
            {
                removed = true;
                break;
            }
        }
        
        if (removed)
        {
            _globalMetrics.RecordRemoval();
        }
        
        return removed;
    }
    
    public void Update(ISpatialObject obj, BoundingBox previousBounds)
    {
        var structure = GetBestStructureForObject(obj);
        structure.Update(obj, previousBounds);
        _globalMetrics.RecordUpdate();
    }
    
    public List<ISpatialObject> Query(BoundingBox region, SpatialLayer layerMask = SpatialLayer.All)
    {
        // Check cache first
        var cacheKey = new QueryCacheKey(region, layerMask);
        if (_queryCache.TryGet(cacheKey, out var cachedResult))
        {
            _globalMetrics.RecordCacheHit();
            return cachedResult;
        }
        
        _globalMetrics.RecordQuery();
        
        var results = new HashSet<ISpatialObject>();
        
        foreach (var kvp in _spatialStructures)
        {
            if ((kvp.Key & layerMask) != 0)
            {
                var structureResults = kvp.Value.Query(region);
                foreach (var obj in structureResults)
                {
                    if ((obj.Layer & layerMask) != 0)
                    {
                        results.Add(obj);
                    }
                }
            }
        }
        
        var finalResults = results.ToList();
        _queryCache.Cache(cacheKey, finalResults);
        
        return finalResults;
    }
    
    public List<ISpatialObject> QueryRadius(Vector2 center, float radius, SpatialLayer layerMask = SpatialLayer.All)
    {
        var results = new HashSet<ISpatialObject>();
        
        foreach (var kvp in _spatialStructures)
        {
            if ((kvp.Key & layerMask) != 0)
            {
                var structureResults = kvp.Value.Query(center, radius);
                foreach (var obj in structureResults)
                {
                    if ((obj.Layer & layerMask) != 0)
                    {
                        results.Add(obj);
                    }
                }
            }
        }
        
        return results.ToList();
    }
    
    public List<CollisionPair> GetCollisionPairs(SpatialLayer layerA, SpatialLayer layerB)
    {
        var pairs = new List<CollisionPair>();
        
        // Get objects from layer A
        var objectsA = new HashSet<ISpatialObject>();
        foreach (var kvp in _spatialStructures)
        {
            if ((kvp.Key & layerA) != 0)
            {
                var allObjects = kvp.Value.Query(kvp.Value.WorldBounds);
                foreach (var obj in allObjects)
                {
                    if ((obj.Layer & layerA) != 0)
                    {
                        objectsA.Add(obj);
                    }
                }
            }
        }
        
        // For each object in A, find potential collisions in layer B
        foreach (var objA in objectsA)
        {
            var radius = objA.BoundingRadius * 2; // Expand search radius
            var candidates = QueryRadius(objA.Position, radius, layerB);
            
            foreach (var objB in candidates)
            {
                if (objA.SpatialId != objB.SpatialId && (objB.Layer & layerB) != 0)
                {
                    pairs.Add(new CollisionPair(objA, objB));
                }
            }
        }
        
        return pairs;
    }
    
    private ISpatialStructure GetBestStructureForObject(ISpatialObject obj)
    {
        // Static objects -> Grid
        if (obj.IsStatic || (obj.Layer & SpatialLayer.Static) != 0)
        {
            return _gridForStatic;
        }
        
        // Fast moving objects -> Loose QuadTree  
        var speed = obj.Velocity.Length();
        if (speed > 5.0f || (obj.Layer & (SpatialLayer.Bullets | SpatialLayer.Particles)) != 0)
        {
            return _looseTreeForFast;
        }
        
        // Default to regular QuadTree
        return _quadTreeForDynamic;
    }
    
    private void OptimizeStructures(object state)
    {
        try
        {
            foreach (var structure in _spatialStructures.Values)
            {
                structure.Optimize();
            }
            
            _queryCache.Cleanup();
            _globalMetrics.RecordOptimization();
        }
        catch (Exception ex)
        {
            ErrorManager.LogError("Error during spatial optimization", ex, "SpatialManager");
        }
    }
    
    public SpatialManagerStatistics GetStatistics()
    {
        var structureStats = _spatialStructures.Values.Select(s => s.GetStatistics()).ToList();
        
        return new SpatialManagerStatistics
        {
            TotalObjects = structureStats.Sum(s => s.ObjectCount),
            TotalStructures = _spatialStructures.Count,
            CacheHitRate = _queryCache.HitRate,
            StructureStatistics = structureStats,
            GlobalMetrics = _globalMetrics.GetSnapshot()
        };
    }
    
    public void Dispose()
    {
        _optimizationTimer?.Dispose();
        
        foreach (var structure in _spatialStructures.Values)
        {
            structure.Dispose();
        }
        
        _spatialStructures.Clear();
        _queryCache?.Dispose();
        _globalMetrics?.Dispose();
    }
}
```

### 2. Query Optimization and Caching

```csharp
public class SpatialQueryCache : IDisposable
{
    private readonly Dictionary<QueryCacheKey, CacheEntry> _cache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _entryLifetime = TimeSpan.FromMilliseconds(100);
    private long _hits;
    private long _misses;
    
    public float HitRate => (_hits + _misses) > 0 ? (float)_hits / (_hits + _misses) : 0f;
    
    public SpatialQueryCache()
    {
        _cache = new Dictionary<QueryCacheKey, CacheEntry>();
        _cleanupTimer = new Timer(CleanupExpiredEntries, null,
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public bool TryGet(QueryCacheKey key, out List<ISpatialObject> result)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            result = entry.Results;
            Interlocked.Increment(ref _hits);
            return true;
        }
        
        result = null;
        Interlocked.Increment(ref _misses);
        return false;
    }
    
    public void Cache(QueryCacheKey key, List<ISpatialObject> results)
    {
        _cache[key] = new CacheEntry
        {
            Results = results,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public void Cleanup()
    {
        CleanupExpiredEntries(null);
    }
    
    private void CleanupExpiredEntries(object state)
    {
        var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }
    }
    
    private struct CacheEntry
    {
        public List<ISpatialObject> Results;
        public DateTime Timestamp;
        
        public bool IsExpired => DateTime.UtcNow - Timestamp > TimeSpan.FromMilliseconds(100);
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache.Clear();
    }
}

public struct QueryCacheKey : IEquatable<QueryCacheKey>
{
    public BoundingBox Region;
    public SpatialLayer LayerMask;
    
    public QueryCacheKey(BoundingBox region, SpatialLayer layerMask)
    {
        Region = region;
        LayerMask = layerMask;
    }
    
    public bool Equals(QueryCacheKey other)
    {
        return Region.Equals(other.Region) && LayerMask == other.LayerMask;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Region, LayerMask);
    }
}
```

This spatial partitioning system provides efficient, scalable spatial organization with multiple optimized data structures for different use cases and comprehensive performance monitoring.