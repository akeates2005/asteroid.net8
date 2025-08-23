using System;
using System.Collections.Generic;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// QuadTree implementation for hierarchical spatial partitioning
    /// Better than grid for sparse distributions and varying entity densities
    /// </summary>
    public class QuadTree<T>
    {
        private readonly Rectangle _bounds;
        private readonly int _maxEntities;
        private readonly int _maxDepth;
        private readonly int _depth;
        
        private List<QuadTreeEntity<T>> _entities;
        private QuadTree<T>[] _children;
        private bool _isDivided;

        public QuadTree(Rectangle bounds, int maxEntities = 10, int maxDepth = 5, int depth = 0)
        {
            _bounds = bounds;
            _maxEntities = maxEntities;
            _maxDepth = maxDepth;
            _depth = depth;
            _entities = new List<QuadTreeEntity<T>>();
            _children = new QuadTree<T>[4];
            _isDivided = false;
        }

        /// <summary>
        /// Clear all entities from the quadtree
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            _isDivided = false;
            
            for (int i = 0; i < 4; i++)
            {
                _children[i] = null;
            }
        }

        /// <summary>
        /// Insert entity into the quadtree
        /// </summary>
        public bool Insert(T entity, Vector2 position, float radius)
        {
            var circle = new Circle(position, radius);
            
            // Check if the entity intersects with this node's bounds
            if (!Intersects(_bounds, circle))
            {
                return false;
            }

            var quadEntity = new QuadTreeEntity<T>(entity, position, radius);

            // If we have capacity and aren't divided, add to this node
            if (_entities.Count < _maxEntities && !_isDivided)
            {
                _entities.Add(quadEntity);
                return true;
            }

            // If we haven't divided yet and can still divide, do so
            if (!_isDivided && _depth < _maxDepth)
            {
                Subdivide();
            }

            // Try to insert into children
            if (_isDivided)
            {
                foreach (var child in _children)
                {
                    if (child.Insert(entity, position, radius))
                    {
                        return true;
                    }
                }
            }

            // If insertion into children failed, keep in this node
            _entities.Add(quadEntity);
            return true;
        }

        /// <summary>
        /// Query entities that could collide with the given circle
        /// </summary>
        public List<T> Query(Vector2 position, float radius, List<T> result = null)
        {
            if (result == null)
            {
                result = new List<T>();
            }

            var circle = new Circle(position, radius);
            
            // If this node doesn't intersect the query, return empty
            if (!Intersects(_bounds, circle))
            {
                return result;
            }

            // Add entities from this node that could potentially collide
            foreach (var entity in _entities)
            {
                result.Add(entity.Entity);
            }

            // Recursively query children if divided
            if (_isDivided)
            {
                foreach (var child in _children)
                {
                    child.Query(position, radius, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all potential collision pairs from the entire tree
        /// </summary>
        public List<(T entityA, T entityB, Vector2 positionA, Vector2 positionB, float radiusA, float radiusB)> GetPotentialCollisions()
        {
            var pairs = new List<(T, T, Vector2, Vector2, float, float)>();
            var processedPairs = new HashSet<(T, T)>();
            
            GetPotentialCollisionsRecursive(pairs, processedPairs);
            
            return pairs;
        }

        /// <summary>
        /// Get statistics about the quadtree for analysis
        /// </summary>
        public QuadTreeStats GetStats()
        {
            var stats = new QuadTreeStats();
            GetStatsRecursive(ref stats);
            return stats;
        }

        private void Subdivide()
        {
            float halfWidth = _bounds.Width / 2f;
            float halfHeight = _bounds.Height / 2f;
            float x = _bounds.X;
            float y = _bounds.Y;

            // Create four child quadrants
            _children[0] = new QuadTree<T>(new Rectangle(x, y, halfWidth, halfHeight), _maxEntities, _maxDepth, _depth + 1); // NW
            _children[1] = new QuadTree<T>(new Rectangle(x + halfWidth, y, halfWidth, halfHeight), _maxEntities, _maxDepth, _depth + 1); // NE
            _children[2] = new QuadTree<T>(new Rectangle(x, y + halfHeight, halfWidth, halfHeight), _maxEntities, _maxDepth, _depth + 1); // SW
            _children[3] = new QuadTree<T>(new Rectangle(x + halfWidth, y + halfHeight, halfWidth, halfHeight), _maxEntities, _maxDepth, _depth + 1); // SE

            _isDivided = true;

            // Try to move existing entities to children
            var entitiesToKeep = new List<QuadTreeEntity<T>>();
            
            foreach (var entity in _entities)
            {
                bool insertedInChild = false;
                
                foreach (var child in _children)
                {
                    if (child.Insert(entity.Entity, entity.Position, entity.Radius))
                    {
                        insertedInChild = true;
                        break;
                    }
                }
                
                // If entity couldn't be inserted in any child, keep it here
                if (!insertedInChild)
                {
                    entitiesToKeep.Add(entity);
                }
            }
            
            _entities = entitiesToKeep;
        }

        private void GetPotentialCollisionsRecursive(List<(T, T, Vector2, Vector2, float, float)> pairs, HashSet<(T, T)> processedPairs)
        {
            // Check collisions within this node
            for (int i = 0; i < _entities.Count; i++)
            {
                for (int j = i + 1; j < _entities.Count; j++)
                {
                    var entityA = _entities[i];
                    var entityB = _entities[j];
                    
                    var pair = CreateOrderedPair(entityA.Entity, entityB.Entity);
                    
                    if (!processedPairs.Contains(pair))
                    {
                        processedPairs.Add(pair);
                        pairs.Add((entityA.Entity, entityB.Entity, entityA.Position, entityB.Position, entityA.Radius, entityB.Radius));
                    }
                }
            }

            // Recursively check children
            if (_isDivided)
            {
                foreach (var child in _children)
                {
                    child.GetPotentialCollisionsRecursive(pairs, processedPairs);
                }

                // Check collisions between entities in different child nodes
                for (int i = 0; i < 4; i++)
                {
                    for (int j = i + 1; j < 4; j++)
                    {
                        CheckCrossNodeCollisions(_children[i], _children[j], pairs, processedPairs);
                    }
                }
            }
        }

        private void CheckCrossNodeCollisions(QuadTree<T> nodeA, QuadTree<T> nodeB, List<(T, T, Vector2, Vector2, float, float)> pairs, HashSet<(T, T)> processedPairs)
        {
            var entitiesA = new List<QuadTreeEntity<T>>();
            var entitiesB = new List<QuadTreeEntity<T>>();
            
            nodeA.GetAllEntities(entitiesA);
            nodeB.GetAllEntities(entitiesB);

            foreach (var entityA in entitiesA)
            {
                foreach (var entityB in entitiesB)
                {
                    var pair = CreateOrderedPair(entityA.Entity, entityB.Entity);
                    
                    if (!processedPairs.Contains(pair))
                    {
                        processedPairs.Add(pair);
                        pairs.Add((entityA.Entity, entityB.Entity, entityA.Position, entityB.Position, entityA.Radius, entityB.Radius));
                    }
                }
            }
        }

        private void GetAllEntities(List<QuadTreeEntity<T>> result)
        {
            result.AddRange(_entities);
            
            if (_isDivided)
            {
                foreach (var child in _children)
                {
                    child.GetAllEntities(result);
                }
            }
        }

        private void GetStatsRecursive(ref QuadTreeStats stats)
        {
            stats.TotalNodes++;
            stats.TotalEntities += _entities.Count;
            stats.MaxDepthReached = Math.Max(stats.MaxDepthReached, _depth);
            
            if (_entities.Count > 0)
            {
                stats.NodesWithEntities++;
                stats.MaxEntitiesPerNode = Math.Max(stats.MaxEntitiesPerNode, _entities.Count);
            }

            if (_isDivided)
            {
                stats.DividedNodes++;
                foreach (var child in _children)
                {
                    child.GetStatsRecursive(ref stats);
                }
            }
        }

        private (T, T) CreateOrderedPair(T a, T b)
        {
            return a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);
        }

        private bool Intersects(Rectangle rect, Circle circle)
        {
            // Find the closest point on the rectangle to the circle center
            float closestX = Math.Max(rect.X, Math.Min(circle.Center.X, rect.X + rect.Width));
            float closestY = Math.Max(rect.Y, Math.Min(circle.Center.Y, rect.Y + rect.Height));

            // Calculate distance from circle center to closest point
            float dx = circle.Center.X - closestX;
            float dy = circle.Center.Y - closestY;
            float distanceSquared = dx * dx + dy * dy;

            return distanceSquared <= (circle.Radius * circle.Radius);
        }
    }

    /// <summary>
    /// Entity wrapper for quadtree storage
    /// </summary>
    public class QuadTreeEntity<T>
    {
        public T Entity { get; }
        public Vector2 Position { get; }
        public float Radius { get; }

        public QuadTreeEntity(T entity, Vector2 position, float radius)
        {
            Entity = entity;
            Position = position;
            Radius = radius;
        }
    }

    /// <summary>
    /// Simple rectangle struct for quadtree bounds
    /// </summary>
    public struct Rectangle
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public Rectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Simple circle struct for collision queries
    /// </summary>
    public struct Circle
    {
        public Vector2 Center { get; }
        public float Radius { get; }

        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    /// <summary>
    /// Statistics for quadtree performance analysis
    /// </summary>
    public struct QuadTreeStats
    {
        public int TotalNodes;
        public int DividedNodes;
        public int NodesWithEntities;
        public int TotalEntities;
        public int MaxEntitiesPerNode;
        public int MaxDepthReached;

        public readonly float AverageEntitiesPerNode => TotalNodes > 0 ? (float)TotalEntities / TotalNodes : 0;

        public override readonly string ToString()
        {
            return $"QuadTree Stats: {TotalNodes} nodes ({DividedNodes} divided), " +
                   $"{TotalEntities} entities, max depth {MaxDepthReached}, " +
                   $"max {MaxEntitiesPerNode} entities per node, avg {AverageEntitiesPerNode:F1} per node";
        }
    }
}