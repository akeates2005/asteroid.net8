using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Centralized manager for all game entities with optimized collision detection and lifecycle management.
    /// Provides spatial partitioning, object pooling, and efficient entity operations.
    /// </summary>
    public class EntityManager
    {
        private readonly Dictionary<int, IGameEntity> _entities;
        private readonly List<IGameEntity> _activeEntities;
        private readonly List<IGameEntity> _entitiesToAdd;
        private readonly List<int> _entitiesToRemove;
        private readonly SpatialGrid _spatialGrid;
        private int _nextEntityId;

        public EntityManager()
        {
            _entities = new Dictionary<int, IGameEntity>();
            _activeEntities = new List<IGameEntity>();
            _entitiesToAdd = new List<IGameEntity>();
            _entitiesToRemove = new List<int>();
            _spatialGrid = new SpatialGrid(GameConstants.SPATIAL_GRID_CELL_SIZE);
            _nextEntityId = 1;
        }

        /// <summary>
        /// Add an entity to the manager (will be processed next frame)
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <returns>Entity ID</returns>
        public int AddEntity(IGameEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _entitiesToAdd.Add(entity);
            return entity.Id;
        }

        /// <summary>
        /// Remove an entity by ID (will be processed next frame)
        /// </summary>
        /// <param name="entityId">ID of entity to remove</param>
        public void RemoveEntity(int entityId)
        {
            if (!_entitiesToRemove.Contains(entityId))
            {
                _entitiesToRemove.Add(entityId);
            }
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <returns>Entity or null if not found</returns>
        public IGameEntity? GetEntity(int entityId)
        {
            return _entities.TryGetValue(entityId, out var entity) ? entity : null;
        }

        /// <summary>
        /// Get all entities of a specific type
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <returns>Collection of entities of the specified type</returns>
        public IEnumerable<T> GetEntitiesOfType<T>() where T : class, IGameEntity
        {
            return _activeEntities.OfType<T>();
        }

        /// <summary>
        /// Find entities within a radius of a position
        /// </summary>
        /// <param name="position">Center position</param>
        /// <param name="radius">Search radius</param>
        /// <returns>Entities within the specified radius</returns>
        public IEnumerable<IGameEntity> GetEntitiesInRadius(Vector2 position, float radius)
        {
            return _spatialGrid.Query(position, radius);
        }

        /// <summary>
        /// Update all active entities
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(float deltaTime)
        {
            // Process additions and removals
            ProcessPendingOperations();

            // Update spatial grid
            _spatialGrid.Clear();
            foreach (var entity in _activeEntities)
            {
                if (entity.Active)
                {
                    _spatialGrid.Insert(entity);
                }
            }

            // Update all active entities
            foreach (var entity in _activeEntities.ToList()) // ToList to avoid modification during iteration
            {
                if (entity.Active)
                {
                    try
                    {
                        entity.Update(deltaTime);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.LogError($"Error updating entity {entity.Id}", ex);
                        RemoveEntity(entity.Id);
                    }
                }
            }

            // Remove inactive entities
            var inactiveEntities = _activeEntities.Where(e => !e.Active).ToList();
            foreach (var entity in inactiveEntities)
            {
                RemoveEntity(entity.Id);
            }
        }

        /// <summary>
        /// Render all active entities
        /// </summary>
        /// <param name="renderer">Renderer to use</param>
        public void Render(IRenderer renderer)
        {
            foreach (var entity in _activeEntities)
            {
                if (entity.Active)
                {
                    try
                    {
                        // Frustum culling is handled by individual renderers
                        entity.Render(renderer);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.LogError($"Error rendering entity {entity.Id}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Check collisions between entities using spatial partitioning
        /// </summary>
        /// <returns>Collection of collision pairs</returns>
        public IEnumerable<(IGameEntity entityA, IGameEntity entityB)> CheckCollisions()
        {
            var collisions = new List<(IGameEntity, IGameEntity)>();

            foreach (var entity in _activeEntities)
            {
                if (!entity.Active) continue;

                var nearbyEntities = _spatialGrid.Query(entity.Position, entity.GetCollisionRadius() * 2);
                foreach (var other in nearbyEntities)
                {
                    if (other == entity || !other.Active) continue;
                    if (entity.Id >= other.Id) continue; // Avoid duplicate pairs

                    float distance = Vector2.Distance(entity.Position, other.Position);
                    float combinedRadius = entity.GetCollisionRadius() + other.GetCollisionRadius();

                    if (distance <= combinedRadius)
                    {
                        collisions.Add((entity, other));
                    }
                }
            }

            return collisions;
        }

        /// <summary>
        /// Get total number of active entities
        /// </summary>
        public int ActiveEntityCount => _activeEntities.Count(e => e.Active);

        /// <summary>
        /// Clear all entities
        /// </summary>
        public void Clear()
        {
            foreach (var entity in _entities.Values)
            {
                try
                {
                    entity.Dispose();
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError($"Error disposing entity {entity.Id}", ex);
                }
            }

            _entities.Clear();
            _activeEntities.Clear();
            _entitiesToAdd.Clear();
            _entitiesToRemove.Clear();
            _spatialGrid.Clear();
            _nextEntityId = 1;

            ErrorManager.LogInfo("EntityManager cleared all entities");
        }

        /// <summary>
        /// Generate next unique entity ID
        /// </summary>
        /// <returns>Unique entity ID</returns>
        public int GetNextEntityId()
        {
            return _nextEntityId++;
        }

        private void ProcessPendingOperations()
        {
            // Add new entities
            foreach (var entity in _entitiesToAdd)
            {
                try
                {
                    entity.Initialize();
                    _entities[entity.Id] = entity;
                    _activeEntities.Add(entity);
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError($"Error adding entity {entity.Id}", ex);
                }
            }
            _entitiesToAdd.Clear();

            // Remove entities
            foreach (var entityId in _entitiesToRemove)
            {
                if (_entities.TryGetValue(entityId, out var entity))
                {
                    try
                    {
                        entity.Dispose();
                        _entities.Remove(entityId);
                        _activeEntities.Remove(entity);
                    }
                    catch (Exception ex)
                    {
                        ErrorManager.LogError($"Error removing entity {entityId}", ex);
                    }
                }
            }
            _entitiesToRemove.Clear();
        }
    }
}