using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Optimized collision detection system using spatial partitioning.
    /// Replaces O(n²) brute force collision detection with O(n + k) spatial grid approach.
    /// </summary>
    public class CollisionManager
    {
        private readonly EntityManager _entityManager;
        private CollisionStats _stats;

        /// <summary>
        /// Event fired when a collision occurs
        /// </summary>
        public event Action<IGameEntity, IGameEntity>? CollisionDetected;

        public CollisionManager(EntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _stats = new CollisionStats();
        }

        /// <summary>
        /// Process all collision detection for the current frame
        /// </summary>
        public void ProcessCollisions()
        {
            try
            {
                _stats.FrameStartTime = DateTime.UtcNow;
                _stats.CollisionsDetected = 0;
                _stats.CollisionChecks = 0;

                var collisions = _entityManager.CheckCollisions();
                
                foreach (var (entityA, entityB) in collisions)
                {
                    _stats.CollisionChecks++;
                    
                    if (AreEntitiesColliding(entityA, entityB))
                    {
                        _stats.CollisionsDetected++;
                        CollisionDetected?.Invoke(entityA, entityB);
                    }
                }

                _stats.FrameEndTime = DateTime.UtcNow;
                _stats.ProcessingTimeMs = (_stats.FrameEndTime - _stats.FrameStartTime).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error during collision processing", ex);
            }
        }

        /// <summary>
        /// Check if two specific entities are colliding
        /// </summary>
        /// <param name="entityA">First entity</param>
        /// <param name="entityB">Second entity</param>
        /// <returns>True if entities are colliding</returns>
        public bool AreEntitiesColliding(IGameEntity entityA, IGameEntity entityB)
        {
            if (entityA == null || entityB == null || !entityA.Active || !entityB.Active)
                return false;

            // Use distance squared to avoid expensive sqrt operations
            float distanceSquared = Vector2.DistanceSquared(entityA.Position, entityB.Position);
            float radiusA = entityA.GetCollisionRadius();
            float radiusB = entityB.GetCollisionRadius();
            float combinedRadiusSquared = (radiusA + radiusB) * (radiusA + radiusB);

            return distanceSquared <= combinedRadiusSquared;
        }

        /// <summary>
        /// Find all entities colliding with a specific entity
        /// </summary>
        /// <param name="entity">Entity to check collisions for</param>
        /// <returns>Collection of colliding entities</returns>
        public IEnumerable<IGameEntity> GetCollidingEntities(IGameEntity entity)
        {
            if (entity == null || !entity.Active) return Enumerable.Empty<IGameEntity>();

            var nearbyEntities = _entityManager.GetEntitiesInRadius(
                entity.Position, 
                entity.GetCollisionRadius() * 2
            );

            return nearbyEntities.Where(other => 
                other != entity && 
                other.Active && 
                AreEntitiesColliding(entity, other)
            );
        }

        /// <summary>
        /// Check collision between a point and an entity
        /// </summary>
        /// <param name="point">Point position</param>
        /// <param name="entity">Entity to check</param>
        /// <returns>True if point is inside entity</returns>
        public bool IsPointInsideEntity(Vector2 point, IGameEntity entity)
        {
            if (entity == null || !entity.Active) return false;

            float distanceSquared = Vector2.DistanceSquared(point, entity.Position);
            float radiusSquared = entity.GetCollisionRadius() * entity.GetCollisionRadius();

            return distanceSquared <= radiusSquared;
        }

        /// <summary>
        /// Get collision statistics for performance monitoring
        /// </summary>
        /// <returns>Current collision statistics</returns>
        public CollisionStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// Register collision handlers for specific entity type combinations
        /// </summary>
        /// <typeparam name="TEntityA">First entity type</typeparam>
        /// <typeparam name="TEntityB">Second entity type</typeparam>
        /// <param name="handler">Collision handler</param>
        public void RegisterCollisionHandler<TEntityA, TEntityB>(Action<TEntityA, TEntityB> handler)
            where TEntityA : class, IGameEntity
            where TEntityB : class, IGameEntity
        {
            CollisionDetected += (entityA, entityB) =>
            {
                if (entityA is TEntityA typedA && entityB is TEntityB typedB)
                {
                    handler(typedA, typedB);
                }
                else if (entityA is TEntityB typedB2 && entityB is TEntityA typedA2)
                {
                    handler(typedA2, typedB2);
                }
            };
        }
    }

    /// <summary>
    /// Collision detection statistics for performance monitoring
    /// </summary>
    public struct CollisionStats
    {
        public DateTime FrameStartTime { get; set; }
        public DateTime FrameEndTime { get; set; }
        public double ProcessingTimeMs { get; set; }
        public int CollisionChecks { get; set; }
        public int CollisionsDetected { get; set; }
        public float EfficiencyRatio => CollisionChecks > 0 ? (float)CollisionsDetected / CollisionChecks : 0f;
    }
}