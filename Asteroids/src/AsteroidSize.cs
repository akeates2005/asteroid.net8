namespace Asteroids
{
    /// <summary>
    /// Defines the size categories for asteroids, affecting collision radius, visual scale, and game mechanics
    /// </summary>
    public enum AsteroidSize
    {
        /// <summary>
        /// Small asteroid with radius 10, fastest movement, lowest point value
        /// </summary>
        Small,
        
        /// <summary>
        /// Medium asteroid with radius 20, moderate movement, medium point value
        /// </summary>
        Medium,
        
        /// <summary>
        /// Large asteroid with radius 40, slower movement, highest point value
        /// </summary>
        Large
    }
}
