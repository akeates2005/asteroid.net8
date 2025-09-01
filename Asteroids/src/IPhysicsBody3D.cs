using System.Numerics;

namespace Asteroids
{
    /// <summary>
    /// Interface for 3D physics bodies in the physics system
    /// </summary>
    public interface IPhysicsBody3D
    {
        /// <summary>
        /// Current position in 3D space
        /// </summary>
        Vector3 Position { get; set; }
        
        /// <summary>
        /// Current velocity vector
        /// </summary>
        Vector3 Velocity { get; set; }
        
        /// <summary>
        /// Current acceleration vector
        /// </summary>
        Vector3 Acceleration { get; set; }
        
        /// <summary>
        /// Mass of the body (affects physics calculations)
        /// </summary>
        float Mass { get; set; }
        
        /// <summary>
        /// Collision radius for sphere collision detection
        /// </summary>
        float Radius { get; set; }
        
        /// <summary>
        /// Whether this body is static (immovable)
        /// </summary>
        bool IsStatic { get; set; }
        
        /// <summary>
        /// Whether this body is active in physics calculations
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// User data for game-specific information
        /// </summary>
        object UserData { get; set; }
        
        /// <summary>
        /// Get the axis-aligned bounding box for this body
        /// </summary>
        /// <returns>Bounding box structure</returns>
        BoundingBox3D GetBoundingBox();
        
        /// <summary>
        /// Called when this body collides with another
        /// </summary>
        /// <param name="other">The other body in the collision</param>
        /// <param name="contactPoint">Point of contact</param>
        /// <param name="contactNormal">Normal vector at contact point</param>
        void OnCollision(IPhysicsBody3D other, Vector3 contactPoint, Vector3 contactNormal);
    }
    
    /// <summary>
    /// 3D bounding box structure for collision detection
    /// </summary>
    public struct BoundingBox3D
    {
        public Vector3 Min;
        public Vector3 Max;
        
        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Size => Max - Min;
        
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }
        
        public bool Intersects(BoundingBox3D other)
        {
            return Max.X >= other.Min.X && Min.X <= other.Max.X &&
                   Max.Y >= other.Min.Y && Min.Y <= other.Max.Y &&
                   Max.Z >= other.Min.Z && Min.Z <= other.Max.Z;
        }
    }
    
    /// <summary>
    /// Result of a raycast operation
    /// </summary>
    public struct RaycastHit3D
    {
        public IPhysicsBody3D Body;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
    }
}