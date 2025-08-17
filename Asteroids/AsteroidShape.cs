using System;
using System.Numerics;

namespace Asteroids
{
    class AsteroidShape
    {
        public Vector2[] Points { get; private set; }

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