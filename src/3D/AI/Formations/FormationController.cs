using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Formations
{
    /// <summary>
    /// Controls and manages formation patterns for groups of AI ships
    /// </summary>
    public class FormationController
    {
        public FormationType FormationType { get; private set; }
        public Vector3 FormationCenter { get; set; }
        public Vector3 FormationDirection { get; set; } = Vector3.UnitZ;
        public float FormationScale { get; set; } = 1.0f;
        public float FormationSpeed { get; set; } = 50f;
        
        private List<FormationMember> members;
        private AIEnemyShip leader;
        private List<Vector3> formationPositions;
        private bool isDynamic = true;
        
        public int MemberCount => members.Count;
        public AIEnemyShip Leader => leader;
        public IReadOnlyList<FormationMember> Members => members.AsReadOnly();
        
        public event Action<FormationController, AIEnemyShip> OnLeaderChanged;
        public event Action<FormationController, FormationMember> OnMemberAdded;
        public event Action<FormationController, FormationMember> OnMemberRemoved;
        
        public FormationController(FormationType formationType, Vector3 center)
        {
            FormationType = formationType;
            FormationCenter = center;
            members = new List<FormationMember>();
            formationPositions = new List<Vector3>();
        }
        
        public void Update(float deltaTime)
        {
            // Update formation center based on leader
            if (leader != null)
            {
                FormationCenter = leader.Position;
                FormationDirection = leader.Forward;
            }
            
            // Calculate formation positions
            CalculateFormationPositions();
            
            // Update all members
            foreach (var member in members)
            {
                member.Update(deltaTime);
            }
            
            // Handle dynamic formation adjustments
            if (isDynamic)
            {
                AdjustFormationDynamically();
            }
        }
        
        public bool AddMember(AIEnemyShip ship)
        {
            if (ship == null || members.Any(m => m.Ship == ship)) 
                return false;
            
            var member = new FormationMember(ship);
            member.FormationIndex = members.Count;
            members.Add(member);
            
            ship.Formation = this;
            ship.FormationIndex = member.FormationIndex;
            
            // Set leader if this is the first member or no leader exists
            if (leader == null || members.Count == 1)
            {
                SetLeader(ship);
            }
            
            CalculateFormationPositions();
            OnMemberAdded?.Invoke(this, member);
            
            return true;
        }
        
        public bool RemoveMember(AIEnemyShip ship)
        {
            var member = members.FirstOrDefault(m => m.Ship == ship);
            if (member == null) return false;
            
            members.Remove(member);
            ship.Formation = null;
            ship.FormationIndex = -1;
            
            // Reassign indices
            for (int i = 0; i < members.Count; i++)
            {
                members[i].FormationIndex = i;
                members[i].Ship.FormationIndex = i;
            }
            
            // Handle leader change
            if (leader == ship)
            {
                SelectNewLeader();
            }
            
            CalculateFormationPositions();
            OnMemberRemoved?.Invoke(this, member);
            
            return true;
        }
        
        public void SetLeader(AIEnemyShip newLeader)
        {
            if (newLeader == null || !members.Any(m => m.Ship == newLeader))
                return;
            
            var oldLeader = leader;
            leader = newLeader;
            
            // Move leader to front of formation
            var leaderMember = members.First(m => m.Ship == newLeader);
            members.Remove(leaderMember);
            members.Insert(0, leaderMember);
            
            // Reassign indices
            for (int i = 0; i < members.Count; i++)
            {
                members[i].FormationIndex = i;
                members[i].Ship.FormationIndex = i;
            }
            
            OnLeaderChanged?.Invoke(this, newLeader);
        }
        
        public void ChangeFormation(FormationType newType)
        {
            FormationType = newType;
            CalculateFormationPositions();
        }
        
        public Vector3 GetFormationPosition(int index)
        {
            if (index < 0 || index >= formationPositions.Count)
                return FormationCenter;
            
            return formationPositions[index];
        }
        
        public void SetDestination(Vector3 destination)
        {
            var direction = Vector3.Normalize(destination - FormationCenter);
            FormationDirection = direction;
            
            // Leader moves to destination, others follow formation
            leader?.Navigator.SetDestination(destination);
        }
        
        public void SetFormationSpeed(float speed)
        {
            FormationSpeed = speed;
            
            foreach (var member in members)
            {
                member.Ship.Speed = speed;
            }
        }
        
        private void CalculateFormationPositions()
        {
            formationPositions.Clear();
            
            if (members.Count == 0) return;
            
            switch (FormationType)
            {
                case FormationType.VFormation:
                    CalculateVFormation();
                    break;
                case FormationType.Diamond:
                    CalculateDiamondFormation();
                    break;
                case FormationType.Sphere:
                    CalculateSphereFormation();
                    break;
                case FormationType.Helix:
                    CalculateHelixFormation();
                    break;
                case FormationType.Line:
                    CalculateLineFormation();
                    break;
                case FormationType.Box:
                    CalculateBoxFormation();
                    break;
                case FormationType.Wedge:
                    CalculateWedgeFormation();
                    break;
                case FormationType.Circle:
                    CalculateCircleFormation();
                    break;
            }
            
            // Apply formation transformations
            ApplyFormationTransform();
        }
        
        private void CalculateVFormation()
        {
            var spacing = 25f * FormationScale;
            var angle = 30f; // V angle in degrees
            
            for (int i = 0; i < members.Count; i++)
            {
                Vector3 position;
                
                if (i == 0) // Leader at front
                {
                    position = Vector3.Zero;
                }
                else
                {
                    var side = (i % 2 == 1) ? -1 : 1; // Alternate sides
                    var rank = (i + 1) / 2; // Distance from leader
                    
                    var offset = new Vector3(
                        side * (float)Math.Sin(angle * Math.PI / 180f) * spacing * rank,
                        0,
                        -(float)Math.Cos(angle * Math.PI / 180f) * spacing * rank
                    );
                    
                    position = offset;
                }
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateDiamondFormation()
        {
            var spacing = 30f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                Vector3 position;
                
                switch (i)
                {
                    case 0: // Leader at front
                        position = new Vector3(0, 0, spacing);
                        break;
                    case 1: // Left wing
                        position = new Vector3(-spacing, 0, 0);
                        break;
                    case 2: // Right wing
                        position = new Vector3(spacing, 0, 0);
                        break;
                    case 3: // Rear
                        position = new Vector3(0, 0, -spacing);
                        break;
                    default: // Additional ships in expanding diamond
                        var ring = (i - 4) / 4 + 2;
                        var sideIndex = (i - 4) % 4;
                        var ringSpacing = spacing * ring * 0.7f;
                        
                        switch (sideIndex)
                        {
                            case 0: position = new Vector3(0, 0, ringSpacing); break;
                            case 1: position = new Vector3(-ringSpacing, 0, 0); break;
                            case 2: position = new Vector3(ringSpacing, 0, 0); break;
                            case 3: position = new Vector3(0, 0, -ringSpacing); break;
                            default: position = Vector3.Zero; break;
                        }
                        break;
                }
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateSphereFormation()
        {
            var radius = 40f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                Vector3 position;
                
                if (i == 0) // Leader at center
                {
                    position = Vector3.Zero;
                }
                else
                {
                    // Distribute ships evenly on sphere surface
                    var phi = Math.Acos(1 - 2.0 * (i - 1) / (members.Count - 1.0));
                    var theta = Math.PI * (1 + Math.Sqrt(5)) * (i - 1);
                    
                    position = new Vector3(
                        radius * (float)(Math.Sin(phi) * Math.Cos(theta)),
                        radius * (float)(Math.Sin(phi) * Math.Sin(theta)),
                        radius * (float)Math.Cos(phi)
                    );
                }
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateHelixFormation()
        {
            var radius = 20f * FormationScale;
            var pitch = 15f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                var angle = i * 60f * Math.PI / 180f; // 60 degrees between ships
                var height = i * pitch;
                
                var position = new Vector3(
                    radius * (float)Math.Cos(angle),
                    height,
                    radius * (float)Math.Sin(angle) - i * 10f // Move back as well
                );
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateLineFormation()
        {
            var spacing = 20f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                var position = new Vector3(
                    (i - members.Count / 2f) * spacing,
                    0,
                    0
                );
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateBoxFormation()
        {
            var spacing = 25f * FormationScale;
            var shipsPerSide = (int)Math.Ceiling(Math.Sqrt(members.Count));
            
            for (int i = 0; i < members.Count; i++)
            {
                var row = i / shipsPerSide;
                var col = i % shipsPerSide;
                
                var position = new Vector3(
                    (col - shipsPerSide / 2f) * spacing,
                    0,
                    (row - shipsPerSide / 2f) * spacing
                );
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateWedgeFormation()
        {
            var spacing = 20f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                var row = 0;
                var col = i;
                
                // Find which row this ship belongs to
                var shipsInRow = 1;
                var totalShips = 0;
                
                while (totalShips + shipsInRow <= i)
                {
                    totalShips += shipsInRow;
                    row++;
                    shipsInRow += 2; // Each row has 2 more ships than the previous
                }
                
                col = i - totalShips;
                
                var position = new Vector3(
                    (col - shipsInRow / 2f + 0.5f) * spacing,
                    0,
                    -row * spacing
                );
                
                formationPositions.Add(position);
            }
        }
        
        private void CalculateCircleFormation()
        {
            var radius = 30f * FormationScale;
            
            for (int i = 0; i < members.Count; i++)
            {
                if (i == 0) // Leader at center
                {
                    formationPositions.Add(Vector3.Zero);
                }
                else
                {
                    var angle = (i - 1) * 2 * Math.PI / (members.Count - 1);
                    var position = new Vector3(
                        radius * (float)Math.Cos(angle),
                        0,
                        radius * (float)Math.Sin(angle)
                    );
                    
                    formationPositions.Add(position);
                }
            }
        }
        
        private void ApplyFormationTransform()
        {
            // Transform all positions relative to formation center and direction
            var right = Vector3.Normalize(Vector3.Cross(FormationDirection, Vector3.UnitY));
            var up = Vector3.Cross(right, FormationDirection);
            
            for (int i = 0; i < formationPositions.Count; i++)
            {
                var localPos = formationPositions[i];
                var worldPos = FormationCenter +
                    right * localPos.X +
                    up * localPos.Y +
                    FormationDirection * localPos.Z;
                
                formationPositions[i] = worldPos;
            }
        }
        
        private void AdjustFormationDynamically()
        {
            // Adjust formation based on combat situation
            var inCombat = members.Any(m => m.Ship.Target != null);
            
            if (inCombat)
            {
                // Spread out formation for combat
                FormationScale = Math.Max(FormationScale, 1.5f);
                
                // Switch to more defensive formations if taking heavy casualties
                var casualtyRate = 1f - (float)members.Count / 8f; // Assuming max 8 ships
                if (casualtyRate > 0.5f && FormationType != FormationType.Sphere)
                {
                    ChangeFormation(FormationType.Sphere);
                }
            }
            else
            {
                // Tighten formation for travel
                FormationScale = Math.Max(FormationScale - 0.1f, 0.8f);
            }
        }
        
        private void SelectNewLeader()
        {
            if (members.Count == 0)
            {
                leader = null;
                return;
            }
            
            // Prioritize by ship type and health
            var bestCandidate = members
                .Where(m => m.Ship.Health > m.Ship.MaxHealth * 0.3f) // At least 30% health
                .OrderByDescending(m => GetLeadershipScore(m.Ship))
                .FirstOrDefault();
            
            if (bestCandidate != null)
            {
                SetLeader(bestCandidate.Ship);
            }
            else
            {
                // Fallback to first available ship
                SetLeader(members[0].Ship);
            }
        }
        
        private float GetLeadershipScore(AIEnemyShip ship)
        {
            float score = 0;
            
            // Health factor
            score += (ship.Health / ship.MaxHealth) * 30f;
            
            // Ship type factor
            switch (ship.ShipType)
            {
                case EnemyShipType.Fighter:
                    score += 20f;
                    break;
                case EnemyShipType.Bomber:
                    score += 15f;
                    break;
                case EnemyShipType.Scout:
                    score += 10f;
                    break;
                case EnemyShipType.Interceptor:
                    score += 5f;
                    break;
            }
            
            // Experience/survival factor
            score += ship.TeamworkTendency * 10f;
            
            return score;
        }
    }
    
    public enum FormationType
    {
        VFormation,
        Diamond,
        Sphere,
        Helix,
        Line,
        Box,
        Wedge,
        Circle
    }
}