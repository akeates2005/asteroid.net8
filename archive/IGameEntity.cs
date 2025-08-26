using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Core interface for all game entities providing standardized lifecycle management.
    /// Enables consistent handling of position, state, and behavior across all game objects.
    /// </summary>
    public interface IGameEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Current position in world space
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Whether the entity is active and should be processed
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// Update entity state (called every frame)
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        void Update(float deltaTime);

        /// <summary>
        /// Render the entity using the provided renderer
        /// </summary>
        /// <param name="renderer">Renderer to use for drawing</param>
        void Render(IRenderer renderer);

        /// <summary>
        /// Get collision radius for this entity
        /// </summary>
        /// <returns>Collision radius in world units</returns>
        float GetCollisionRadius();

        /// <summary>
        /// Initialize entity (called when first created)
        /// </summary>
        void Initialize();

        /// <summary>
        /// Cleanup entity resources (called when destroyed)
        /// </summary>
        void Dispose();
    }
}