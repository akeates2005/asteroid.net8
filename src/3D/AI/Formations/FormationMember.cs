using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Formations
{
    /// <summary>
    /// Represents a ship's membership in a formation
    /// </summary>
    public class FormationMember
    {
        public AIEnemyShip Ship { get; private set; }
        public int FormationIndex { get; set; } = -1;
        public Vector3 TargetPosition { get; set; }
        public Vector3 FormationVelocity { get; set; }
        
        private Vector3 lastTargetPosition;
        private float positionTolerance = 15f;
        private float maxCatchUpSpeed = 1.5f;
        private float smoothingFactor = 0.1f;
        
        public bool IsInPosition => Vector3.Distance(Ship.Position, TargetPosition) <= positionTolerance;
        public float DistanceFromPosition => Vector3.Distance(Ship.Position, TargetPosition);
        
        public FormationMember(AIEnemyShip ship)
        {
            Ship = ship;
            TargetPosition = ship.Position;
            FormationVelocity = Vector3.Zero;
        }
        
        public void Update(float deltaTime)
        {
            if (Ship.Formation == null) return;
            
            // Get formation position
            TargetPosition = Ship.Formation.GetFormationPosition(FormationIndex);
            
            // Calculate formation velocity for smooth movement
            if (lastTargetPosition != Vector3.Zero)
            {
                var targetMovement = TargetPosition - lastTargetPosition;
                FormationVelocity = targetMovement / deltaTime;
            }
            
            lastTargetPosition = TargetPosition;
            
            // Move toward formation position
            MoveToFormationPosition(deltaTime);
        }
        
        private void MoveToFormationPosition(float deltaTime)
        {
            var distanceToTarget = DistanceFromPosition;
            
            if (distanceToTarget <= positionTolerance)
            {
                // Close enough - match formation velocity
                Ship.Velocity = Vector3.Lerp(Ship.Velocity, FormationVelocity, smoothingFactor);
                return;
            }
            
            // Calculate movement toward formation position
            var directionToTarget = Vector3.Normalize(TargetPosition - Ship.Position);
            
            // Speed up if far from formation
            var speedMultiplier = 1f;
            if (distanceToTarget > positionTolerance * 2f)
            {
                speedMultiplier = Math.Min(maxCatchUpSpeed, distanceToTarget / (positionTolerance * 2f));
            }
            
            var targetVelocity = directionToTarget * Ship.Speed * speedMultiplier;
            
            // Blend with formation velocity for smooth movement
            targetVelocity = Vector3.Lerp(targetVelocity, FormationVelocity, 0.3f);
            
            // Smooth velocity changes
            Ship.Velocity = Vector3.Lerp(Ship.Velocity, targetVelocity, smoothingFactor);
            
            // Gradually orient toward formation direction
            if (Ship.Formation.FormationDirection.LengthSquared() > 0.001f)
            {
                var targetForward = Ship.Formation.FormationDirection;
                Ship.Forward = Vector3.Lerp(Ship.Forward, targetForward, smoothingFactor * 0.5f);
                Ship.Forward = Vector3.Normalize(Ship.Forward);
            }
        }
        
        public void SetPositionTolerance(float tolerance)
        {
            positionTolerance = tolerance;
        }
        
        public void SetCatchUpSpeed(float speedMultiplier)
        {
            maxCatchUpSpeed = speedMultiplier;
        }
        
        public void SetSmoothingFactor(float smoothing)
        {
            smoothingFactor = Math.Max(0.01f, Math.Min(1f, smoothing));
        }
        
        /// <summary>
        /// Force ship to specific formation position instantly
        /// </summary>
        public void SnapToFormationPosition()
        {
            if (Ship.Formation != null)
            {
                TargetPosition = Ship.Formation.GetFormationPosition(FormationIndex);
                Ship.Position = TargetPosition;
                Ship.Velocity = FormationVelocity;
            }
        }
        
        /// <summary>
        /// Check if this member can break formation for combat
        /// </summary>
        public bool CanBreakFormation()
        {
            // Leaders and critical formation positions should stay in formation
            if (FormationIndex == 0) return false; // Leader
            
            // Check if breaking formation would compromise overall formation integrity
            var formation = Ship.Formation;
            if (formation != null && formation.MemberCount <= 3) return false;
            
            return true;
        }
        
        /// <summary>
        /// Temporarily leave formation for combat maneuvers
        /// </summary>
        public void BreakFormation(float duration)
        {
            // Implementation would pause formation following for specified duration
            // This could be handled by the formation controller or individual ship AI
        }
        
        /// <summary>
        /// Calculate priority for maintaining formation position
        /// </summary>
        public float GetFormationPriority()
        {
            float priority = 0.5f; // Base priority
            
            // Leaders have higher priority
            if (FormationIndex == 0) priority += 0.3f;
            
            // Ships with specific formation roles
            switch (Ship.Formation?.FormationType)
            {
                case FormationType.VFormation:
                    if (FormationIndex <= 2) priority += 0.2f; // Wing leaders
                    break;
                case FormationType.Diamond:
                    if (FormationIndex <= 3) priority += 0.2f; // Cardinal positions
                    break;
                case FormationType.Sphere:
                    priority += 0.1f; // All positions important for sphere
                    break;
            }
            
            // Adjust based on ship type
            switch (Ship.ShipType)
            {
                case EnemyShipType.Bomber:
                    priority += 0.2f; // Bombers need formation protection
                    break;
                case EnemyShipType.Scout:
                    priority -= 0.1f; // Scouts can operate independently
                    break;
                case EnemyShipType.Interceptor:
                    priority -= 0.2f; // Interceptors prefer independence
                    break;
            }
            
            return Math.Max(0f, Math.Min(1f, priority));
        }
    }
}