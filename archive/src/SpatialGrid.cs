using System;
using System.Collections.Generic;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Spatial partitioning grid for optimized collision detection and spatial queries.
    /// Replaces O(n²) brute force collision with O(n + k) spatial partitioning.
    /// </summary>
    public class SpatialGrid
    {
        private readonly Dictionary<(int x, int y), HashSet<IGameEntity>> _grid;
        private readonly float _cellSize;
        private readonly int _gridWidth;
        private readonly int _gridHeight;

        public SpatialGrid(float cellSize)
        {
            _cellSize = cellSize;
            _gridWidth = (int)Math.Ceiling(GameConstants.SCREEN_WIDTH / cellSize) + 2; // +2 for screen wrapping
            _gridHeight = (int)Math.Ceiling(GameConstants.SCREEN_HEIGHT / cellSize) + 2;
            _grid = new Dictionary<(int, int), HashSet<IGameEntity>>();
        }

        /// <summary>
        /// Insert an entity into the spatial grid
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        public void Insert(IGameEntity entity)
        {
            if (entity == null || !entity.Active) return;

            var cells = GetCells(entity.Position, entity.GetCollisionRadius());
            foreach (var cell in cells)
            {
                if (!_grid.TryGetValue(cell, out var entities))
                {
                    entities = new HashSet<IGameEntity>();
                    _grid[cell] = entities;
                }
                entities.Add(entity);
            }
        }

        /// <summary>
        /// Query entities within a radius of a position
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="radius">Query radius</param>
        /// <returns>Entities within the specified radius</returns>
        public IEnumerable<IGameEntity> Query(Vector2 position, float radius)
        {
            var result = new HashSet<IGameEntity>();
            var cells = GetCells(position, radius);

            foreach (var cell in cells)
            {
                if (_grid.TryGetValue(cell, out var entities))
                {
                    foreach (var entity in entities)
                    {
                        if (entity.Active)
                        {
                            result.Add(entity);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clear the spatial grid
        /// </summary>
        public void Clear()
        {
            foreach (var cellEntities in _grid.Values)
            {
                cellEntities.Clear();
            }
        }

        /// <summary>
        /// Get grid statistics for debugging
        /// </summary>
        /// <returns>Grid statistics</returns>
        public SpatialGridStats GetStats()
        {
            int totalCells = _grid.Count;
            int occupiedCells = 0;
            int totalEntities = 0;
            int maxEntitiesPerCell = 0;

            foreach (var entities in _grid.Values)
            {
                if (entities.Count > 0)
                {
                    occupiedCells++;
                    totalEntities += entities.Count;
                    maxEntitiesPerCell = Math.Max(maxEntitiesPerCell, entities.Count);
                }
            }

            return new SpatialGridStats
            {
                TotalCells = totalCells,
                OccupiedCells = occupiedCells,
                TotalEntities = totalEntities,
                MaxEntitiesPerCell = maxEntitiesPerCell,
                AverageEntitiesPerCell = occupiedCells > 0 ? (float)totalEntities / occupiedCells : 0f,
                CellSize = _cellSize
            };
        }

        private IEnumerable<(int x, int y)> GetCells(Vector2 position, float radius)
        {
            // Calculate bounding box for the entity
            float minX = position.X - radius;
            float maxX = position.X + radius;
            float minY = position.Y - radius;
            float maxY = position.Y + radius;

            // Convert to grid coordinates
            int startX = Math.Max(0, (int)(minX / _cellSize));
            int endX = Math.Min(_gridWidth - 1, (int)(maxX / _cellSize));
            int startY = Math.Max(0, (int)(minY / _cellSize));
            int endY = Math.Min(_gridHeight - 1, (int)(maxY / _cellSize));

            // Handle screen wrapping by also checking edge cells
            var cells = new List<(int x, int y)>();

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    cells.Add((x, y));
                }
            }

            // Add wrapped cells for screen edges
            if (position.X - radius < 0) // Left edge wrapping
            {
                int wrappedX = _gridWidth - 1;
                for (int y = startY; y <= endY; y++)
                {
                    cells.Add((wrappedX, y));
                }
            }

            if (position.X + radius > GameConstants.SCREEN_WIDTH) // Right edge wrapping
            {
                int wrappedX = 0;
                for (int y = startY; y <= endY; y++)
                {
                    cells.Add((wrappedX, y));
                }
            }

            if (position.Y - radius < 0) // Top edge wrapping
            {
                int wrappedY = _gridHeight - 1;
                for (int x = startX; x <= endX; x++)
                {
                    cells.Add((x, wrappedY));
                }
            }

            if (position.Y + radius > GameConstants.SCREEN_HEIGHT) // Bottom edge wrapping
            {
                int wrappedY = 0;
                for (int x = startX; x <= endX; x++)
                {
                    cells.Add((x, wrappedY));
                }
            }

            return cells;
        }
    }

    /// <summary>
    /// Statistics for spatial grid performance monitoring
    /// </summary>
    public struct SpatialGridStats
    {
        public int TotalCells { get; set; }
        public int OccupiedCells { get; set; }
        public int TotalEntities { get; set; }
        public int MaxEntitiesPerCell { get; set; }
        public float AverageEntitiesPerCell { get; set; }
        public float CellSize { get; set; }
    }
}