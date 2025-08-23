using System;
using System.Collections.Generic;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Spatial grid for efficient collision detection using uniform grid partitioning
    /// Optimized for dynamic objects with frequent position updates
    /// </summary>
    public class SpatialGrid<T>
    {
        private readonly int _cellSize;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        private readonly Dictionary<int, List<SpatialGridEntity<T>>> _grid;

        public SpatialGrid(int screenWidth, int screenHeight, int cellSize = 64)
        {
            _cellSize = cellSize;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _gridWidth = (int)MathF.Ceiling((float)screenWidth / cellSize);
            _gridHeight = (int)MathF.Ceiling((float)screenHeight / cellSize);
            _grid = new Dictionary<int, List<SpatialGridEntity<T>>>();
        }

        /// <summary>
        /// Clear all entities from the grid
        /// </summary>
        public void Clear()
        {
            foreach (var cell in _grid.Values)
            {
                cell.Clear();
            }
        }

        /// <summary>
        /// Insert entity into the spatial grid
        /// </summary>
        public void Insert(T entity, Vector2 position, float radius)
        {
            var gridEntity = new SpatialGridEntity<T>(entity, position, radius);
            
            // Calculate which cells this entity spans
            int minCellX = Math.Max(0, (int)((position.X - radius) / _cellSize));
            int maxCellX = Math.Min(_gridWidth - 1, (int)((position.X + radius) / _cellSize));
            int minCellY = Math.Max(0, (int)((position.Y - radius) / _cellSize));
            int maxCellY = Math.Min(_gridHeight - 1, (int)((position.Y + radius) / _cellSize));

            // Add to all cells that the entity spans
            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    int cellKey = GetCellKey(x, y);
                    
                    if (!_grid.ContainsKey(cellKey))
                    {
                        _grid[cellKey] = new List<SpatialGridEntity<T>>();
                    }
                    
                    _grid[cellKey].Add(gridEntity);
                }
            }
        }

        /// <summary>
        /// Query potential collision candidates for a given entity
        /// </summary>
        public List<T> Query(Vector2 position, float radius)
        {
            var candidates = new HashSet<T>(); // Use HashSet to avoid duplicates
            
            // Calculate which cells to check
            int minCellX = Math.Max(0, (int)((position.X - radius) / _cellSize));
            int maxCellX = Math.Min(_gridWidth - 1, (int)((position.X + radius) / _cellSize));
            int minCellY = Math.Max(0, (int)((position.Y - radius) / _cellSize));
            int maxCellY = Math.Min(_gridHeight - 1, (int)((position.Y + radius) / _cellSize));

            // Check all relevant cells
            for (int x = minCellX; x <= maxCellX; x++)
            {
                for (int y = minCellY; y <= maxCellY; y++)
                {
                    int cellKey = GetCellKey(x, y);
                    
                    if (_grid.ContainsKey(cellKey))
                    {
                        foreach (var gridEntity in _grid[cellKey])
                        {
                            candidates.Add(gridEntity.Entity);
                        }
                    }
                }
            }

            return new List<T>(candidates);
        }

        /// <summary>
        /// Get all entity pairs that could potentially collide
        /// This is more efficient than querying for each entity individually
        /// </summary>
        public List<(T entityA, T entityB, Vector2 positionA, Vector2 positionB, float radiusA, float radiusB)> GetPotentialCollisions()
        {
            var pairs = new List<(T, T, Vector2, Vector2, float, float)>();
            var processedPairs = new HashSet<(T, T)>();

            foreach (var cell in _grid.Values)
            {
                // Check all pairs within each cell
                for (int i = 0; i < cell.Count; i++)
                {
                    for (int j = i + 1; j < cell.Count; j++)
                    {
                        var entityA = cell[i];
                        var entityB = cell[j];
                        
                        // Create ordered pair to avoid duplicates
                        var pair = CreateOrderedPair(entityA.Entity, entityB.Entity);
                        
                        if (!processedPairs.Contains(pair))
                        {
                            processedPairs.Add(pair);
                            pairs.Add((entityA.Entity, entityB.Entity, entityA.Position, entityB.Position, entityA.Radius, entityB.Radius));
                        }
                    }
                }
            }

            return pairs;
        }

        /// <summary>
        /// Get statistics about grid usage for optimization
        /// </summary>
        public SpatialGridStats GetStats()
        {
            int totalEntities = 0;
            int occupiedCells = 0;
            int maxEntitiesPerCell = 0;

            foreach (var cell in _grid.Values)
            {
                if (cell.Count > 0)
                {
                    occupiedCells++;
                    totalEntities += cell.Count;
                    maxEntitiesPerCell = Math.Max(maxEntitiesPerCell, cell.Count);
                }
            }

            return new SpatialGridStats
            {
                TotalCells = _gridWidth * _gridHeight,
                OccupiedCells = occupiedCells,
                TotalEntities = totalEntities,
                MaxEntitiesPerCell = maxEntitiesPerCell,
                AverageEntitiesPerCell = occupiedCells > 0 ? (float)totalEntities / occupiedCells : 0,
                CellSize = _cellSize,
                GridDimensions = new Vector2(_gridWidth, _gridHeight)
            };
        }

        private int GetCellKey(int x, int y)
        {
            return y * _gridWidth + x;
        }

        private (T, T) CreateOrderedPair(T a, T b)
        {
            // Use hash codes to create consistent ordering
            return a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);
        }
    }

    /// <summary>
    /// Entity wrapper for spatial grid storage
    /// </summary>
    public class SpatialGridEntity<T>
    {
        public T Entity { get; }
        public Vector2 Position { get; }
        public float Radius { get; }

        public SpatialGridEntity(T entity, Vector2 position, float radius)
        {
            Entity = entity;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>
    /// Statistics for spatial grid performance analysis
    /// </summary>
    public struct SpatialGridStats
    {
        public int TotalCells;
        public int OccupiedCells;
        public int TotalEntities;
        public int MaxEntitiesPerCell;
        public float AverageEntitiesPerCell;
        public int CellSize;
        public Vector2 GridDimensions;

        public override string ToString()
        {
            return $"Grid Stats: {OccupiedCells}/{TotalCells} cells occupied, " +
                   $"{TotalEntities} entities, max {MaxEntitiesPerCell} per cell, " +
                   $"avg {AverageEntitiesPerCell:F1} per occupied cell";
        }
    }
}