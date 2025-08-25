using System;
using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Generates procedural polygon shapes for asteroids to create visual variety.
    /// Creates randomized vertices around a circular pattern for natural-looking asteroid outlines.
    /// </summary>
    public class AsteroidShape
    {
        /// <summary>
        /// Array of vertices defining the asteroid's polygonal shape for rendering
        /// </summary>
        public Vector2[] Points { get; private set; }

        /// <summary>
        /// Creates a new procedural asteroid shape with randomized vertices
        /// </summary>
        /// <param name="numPoints">Number of vertices to generate around the perimeter</param>
        /// <param name="radius">Base radius for the shape (vertices will vary around this)</param>
        /// <param name="random">Random number generator for vertex position variation</param>
        public AsteroidShape(int numPoints, float radius, Random random)
        {
            Points = new Vector2[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                float angle = (float)i / numPoints * 2 * MathF.PI;
                float randomRadius = radius + (float)(random.NextDouble() * 10 - 5);
                Points[i] = new Vector2(MathF.Cos(angle) * randomRadius, MathF.Sin(angle) * randomRadius);
            }
        }
    }
}