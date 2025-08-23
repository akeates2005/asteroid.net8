using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Asteroids.AI.Core;

namespace Asteroids.AI.Tactical
{
    /// <summary>
    /// Advanced tactical AI for strategic decision making
    /// </summary>
    public class TacticalAI
    {
        private TacticalSituation currentSituation;
        private List<TacticalOption> availableOptions;
        private TacticalMemory memory;
        private float evaluationInterval = 2.0f;
        private float lastEvaluation = 0;
        
        public TacticalAI()
        {
            memory = new TacticalMemory();
            availableOptions = new List<TacticalOption>();
        }
        
        public void Update(List<AIEnemyShip> allies, AIEnemyShip target, float deltaTime)
        {
            lastEvaluation += deltaTime;
            
            if (lastEvaluation >= evaluationInterval)
            {
                EvaluateTacticalSituation(allies, target);
                GenerateTacticalOptions(allies, target);
                ExecuteBestTactic(allies, target);
                
                lastEvaluation = 0;
            }
        }
        
        private void EvaluateTacticalSituation(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            currentSituation = new TacticalSituation
            {
                AllyCount = allies.Count,
                AllyHealthAverage = allies.Average(a => a.Health / a.MaxHealth),
                TargetDistance = target != null ? allies.Average(a => a.DistanceTo(target)) : float.MaxValue,
                FormationIntegrity = CalculateFormationIntegrity(allies),
                ThreatLevel = CalculateThreatLevel(allies, target),
                TerrainAdvantage = EvaluateTerrainAdvantage(allies, target)
            };
            
            // Store situation for learning
            memory.RecordSituation(currentSituation);
        }
        
        private void GenerateTacticalOptions(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            availableOptions.Clear();
            
            // Generate different tactical options based on situation
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.DirectAssault,
                Effectiveness = EvaluateDirectAssault(allies, target),
                Requirements = new TacticalRequirements { MinAllies = 2, MinHealthRatio = 0.6f }
            });
            
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.FlankingManeuver,
                Effectiveness = EvaluateFlankingManeuver(allies, target),
                Requirements = new TacticalRequirements { MinAllies = 3, RequiredShipTypes = new[] { EnemyShipType.Interceptor, EnemyShipType.Fighter } }
            });
            
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.PincerMovement,
                Effectiveness = EvaluatePincerMovement(allies, target),
                Requirements = new TacticalRequirements { MinAllies = 4, MinHealthRatio = 0.5f }
            });
            
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.DefensiveFormation,
                Effectiveness = EvaluateDefensiveFormation(allies, target),
                Requirements = new TacticalRequirements { MinAllies = 1, MinHealthRatio = 0.3f }
            });
            
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.HitAndRun,
                Effectiveness = EvaluateHitAndRun(allies, target),
                Requirements = new TacticalRequirements { RequiredShipTypes = new[] { EnemyShipType.Scout, EnemyShipType.Interceptor } }
            });
            
            availableOptions.Add(new TacticalOption
            {
                Type = TacticType.SuppressionBombardment,
                Effectiveness = EvaluateSuppressionBombardment(allies, target),
                Requirements = new TacticalRequirements { RequiredShipTypes = new[] { EnemyShipType.Bomber } }
            });
            
            // Filter options by requirements
            availableOptions = availableOptions.Where(option => 
                MeetsRequirements(option.Requirements, allies)).ToList();
            
            // Sort by effectiveness
            availableOptions = availableOptions.OrderByDescending(o => o.Effectiveness).ToList();
        }
        
        private void ExecuteBestTactic(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (availableOptions.Count == 0) return;
            
            var bestTactic = availableOptions[0];
            
            // Execute the chosen tactic
            switch (bestTactic.Type)
            {
                case TacticType.DirectAssault:
                    ExecuteDirectAssault(allies, target);
                    break;
                case TacticType.FlankingManeuver:
                    ExecuteFlankingManeuver(allies, target);
                    break;
                case TacticType.PincerMovement:
                    ExecutePincerMovement(allies, target);
                    break;
                case TacticType.DefensiveFormation:
                    ExecuteDefensiveFormation(allies, target);
                    break;
                case TacticType.HitAndRun:
                    ExecuteHitAndRun(allies, target);
                    break;
                case TacticType.SuppressionBombardment:
                    ExecuteSuppressionBombardment(allies, target);
                    break;
            }
            
            // Record tactic execution for learning
            memory.RecordTacticExecution(bestTactic, currentSituation);
        }
        
        #region Tactic Evaluation
        
        private float EvaluateDirectAssault(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return 0f;
            
            var totalDamage = allies.Sum(a => EstimateDPS(a));
            var averageHealth = allies.Average(a => a.Health / a.MaxHealth);
            var numberAdvantage = allies.Count > 1 ? 1.2f : 0.8f;
            
            return totalDamage * averageHealth * numberAdvantage * 0.1f;
        }
        
        private float EvaluateFlankingManeuver(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return 0f;
            
            var fastShips = allies.Count(a => a.ShipType == EnemyShipType.Interceptor || a.ShipType == EnemyShipType.Scout);
            var coordination = CalculateCoordinationLevel(allies);
            var spacing = CalculateFormationSpacing(allies);
            
            return (fastShips * 0.3f + coordination * 0.4f + spacing * 0.3f) * 0.8f;
        }
        
        private float EvaluatePincerMovement(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null || allies.Count < 4) return 0f;
            
            var coordination = CalculateCoordinationLevel(allies);
            var positioning = EvaluateEncirclementPotential(allies, target);
            var health = allies.Average(a => a.Health / a.MaxHealth);
            
            return coordination * positioning * health * 0.9f;
        }
        
        private float EvaluateDefensiveFormation(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            var damagedShips = allies.Count(a => a.Health / a.MaxHealth < 0.5f);
            var formationIntegrity = CalculateFormationIntegrity(allies);
            var defensivePotential = allies.Count(a => a.ShipType == EnemyShipType.Bomber || a.ShipType == EnemyShipType.Fighter);
            
            return (damagedShips * 0.4f + formationIntegrity * 0.3f + defensivePotential * 0.3f) * 0.6f;
        }
        
        private float EvaluateHitAndRun(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            var fastShips = allies.Count(a => a.ShipType == EnemyShipType.Interceptor || a.ShipType == EnemyShipType.Scout);
            var mobility = allies.Average(a => a.Speed / 100f); // Normalize speed
            var health = allies.Average(a => a.Health / a.MaxHealth);
            
            return (fastShips * 0.5f + mobility * 0.3f + health * 0.2f) * 0.7f;
        }
        
        private float EvaluateSuppressionBombardment(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            var bombers = allies.Count(a => a.ShipType == EnemyShipType.Bomber);
            var protection = allies.Count(a => a.ShipType != EnemyShipType.Bomber);
            var range = target != null ? allies.Average(a => a.DistanceTo(target)) : 0f;
            
            var rangeEffectiveness = range > 80f ? 1f : range / 80f;
            
            return bombers * 0.5f + (protection * 0.2f) + rangeEffectiveness * 0.3f;
        }
        
        #endregion
        
        #region Tactic Execution
        
        private void ExecuteDirectAssault(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return;
            
            foreach (var ally in allies)
            {
                ally.SetTarget(target);
                ally.StateMachine.ChangeState(new Behaviors.AttackState(), ally);
                
                // Broadcast coordination message
                ally.Communication.BroadcastMessage(new Communication.AIMessage
                {
                    Type = Communication.MessageType.TacticalOrder,
                    Sender = ally,
                    Position = target.Position,
                    Data = "direct_assault"
                });
            }
        }
        
        private void ExecuteFlankingManeuver(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return;
            
            var fastShips = allies.Where(a => 
                a.ShipType == EnemyShipType.Interceptor || 
                a.ShipType == EnemyShipType.Scout).ToList();
            
            var slowShips = allies.Except(fastShips).ToList();
            
            // Fast ships flank
            var flankPositions = CalculateFlankPositions(target.Position, 2);
            for (int i = 0; i < Math.Min(fastShips.Count, flankPositions.Length); i++)
            {
                fastShips[i].Navigator.SetDestination(flankPositions[i]);
                fastShips[i].SetTarget(target);
            }
            
            // Slow ships frontal attack
            foreach (var ship in slowShips)
            {
                ship.SetTarget(target);
                ship.Navigator.SetDestination(target.Position);
            }
        }
        
        private void ExecutePincerMovement(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return;
            
            var half = allies.Count / 2;
            var leftWing = allies.Take(half).ToList();
            var rightWing = allies.Skip(half).ToList();
            
            var pincerPositions = CalculatePincerPositions(target.Position, target.Velocity);
            
            // Left wing
            foreach (var ship in leftWing)
            {
                ship.Navigator.SetDestination(pincerPositions[0]);
                ship.SetTarget(target);
            }
            
            // Right wing
            foreach (var ship in rightWing)
            {
                ship.Navigator.SetDestination(pincerPositions[1]);
                ship.SetTarget(target);
            }
        }
        
        private void ExecuteDefensiveFormation(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            // Form protective sphere formation
            var formation = new Formations.FormationController(
                Formations.FormationType.Sphere,
                allies[0].Position
            );
            
            foreach (var ally in allies)
            {
                formation.AddMember(ally);
            }
            
            // Set conservative AI parameters
            foreach (var ally in allies)
            {
                ally.Caution = Math.Min(1f, ally.Caution + 0.3f);
                ally.Aggressiveness = Math.Max(0f, ally.Aggressiveness - 0.2f);
            }
        }
        
        private void ExecuteHitAndRun(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            foreach (var ally in allies)
            {
                if (ally is Ships.ScoutShip scout)
                {
                    scout.ExecuteHitAndRun();
                }
                else if (ally is Ships.InterceptorShip interceptor)
                {
                    interceptor.ExecuteHitAndRun();
                }
                else
                {
                    // Generic hit and run
                    ally.SetTarget(target);
                }
            }
        }
        
        private void ExecuteSuppressionBombardment(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            var bombers = allies.Where(a => a.ShipType == EnemyShipType.Bomber).ToList();
            var escorts = allies.Except(bombers).ToList();
            
            // Position bombers for bombardment
            foreach (var bomber in bombers)
            {
                if (bomber is Ships.BomberShip bomberShip)
                {
                    bomberShip.SeekCoverPosition();
                    bomber.SetTarget(target);
                }
            }
            
            // Escorts protect bombers
            foreach (var escort in escorts)
            {
                escort.StateMachine.ChangeState(new Behaviors.SupportState(), escort);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private float CalculateFormationIntegrity(List<AIEnemyShip> allies)
        {
            if (allies.Count < 2) return 1f;
            
            var averagePosition = allies.Aggregate(Vector3.Zero, (sum, ship) => sum + ship.Position) / allies.Count;
            var averageDistance = allies.Average(ship => Vector3.Distance(ship.Position, averagePosition));
            
            // Lower distance means higher integrity
            return Math.Max(0f, 1f - (averageDistance / 100f));
        }
        
        private float CalculateThreatLevel(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null) return 0f;
            
            var averageHealth = allies.Average(a => a.Health / a.MaxHealth);
            var distanceToTarget = allies.Average(a => a.DistanceTo(target));
            
            var healthThreat = 1f - averageHealth;
            var proximityThreat = Math.Max(0f, 1f - (distanceToTarget / 200f));
            
            return (healthThreat + proximityThreat) / 2f;
        }
        
        private float EvaluateTerrainAdvantage(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            // Evaluate positioning advantages
            // This would integrate with the game's terrain/obstacle system
            return 0.5f; // Neutral for now
        }
        
        private float CalculateCoordinationLevel(List<AIEnemyShip> allies)
        {
            var averageTeamwork = allies.Average(a => a.TeamworkTendency);
            var formationBonus = allies.Any(a => a.Formation != null) ? 0.2f : 0f;
            
            return Math.Min(1f, averageTeamwork + formationBonus);
        }
        
        private float CalculateFormationSpacing(List<AIEnemyShip> allies)
        {
            if (allies.Count < 2) return 1f;
            
            var totalDistance = 0f;
            var pairs = 0;
            
            for (int i = 0; i < allies.Count; i++)
            {
                for (int j = i + 1; j < allies.Count; j++)
                {
                    totalDistance += allies[i].DistanceTo(allies[j]);
                    pairs++;
                }
            }
            
            var averageDistance = totalDistance / pairs;
            var optimalDistance = 40f; // Ideal formation spacing
            
            return 1f - Math.Abs(averageDistance - optimalDistance) / optimalDistance;
        }
        
        private float EvaluateEncirclementPotential(List<AIEnemyShip> allies, AIEnemyShip target)
        {
            if (target == null || allies.Count < 3) return 0f;
            
            // Calculate how well allies can surround target
            var targetPosition = target.Position;
            var angles = new List<float>();
            
            foreach (var ally in allies)
            {
                var direction = ally.Position - targetPosition;
                var angle = (float)Math.Atan2(direction.Z, direction.X);
                angles.Add(angle);
            }
            
            angles.Sort();
            
            var maxGap = 0f;
            for (int i = 0; i < angles.Count; i++)
            {
                var nextIndex = (i + 1) % angles.Count;
                var gap = angles[nextIndex] - angles[i];
                if (gap < 0) gap += 2 * (float)Math.PI;
                maxGap = Math.Max(maxGap, gap);
            }
            
            // Lower max gap means better encirclement
            return 1f - (maxGap / (2 * (float)Math.PI));
        }
        
        private float EstimateDPS(AIEnemyShip ship)
        {
            return ship.ShipType switch
            {
                EnemyShipType.Scout => 15f,
                EnemyShipType.Fighter => 25f,
                EnemyShipType.Bomber => 40f,
                EnemyShipType.Interceptor => 20f,
                _ => 20f
            };
        }
        
        private bool MeetsRequirements(TacticalRequirements requirements, List<AIEnemyShip> allies)
        {
            if (allies.Count < requirements.MinAllies) return false;
            
            var averageHealth = allies.Average(a => a.Health / a.MaxHealth);
            if (averageHealth < requirements.MinHealthRatio) return false;
            
            if (requirements.RequiredShipTypes != null)
            {
                foreach (var requiredType in requirements.RequiredShipTypes)
                {
                    if (!allies.Any(a => a.ShipType == requiredType))
                        return false;
                }
            }
            
            return true;
        }
        
        private Vector3[] CalculateFlankPositions(Vector3 targetPosition, int count)
        {
            var positions = new Vector3[count];
            var radius = 80f;
            
            for (int i = 0; i < count; i++)
            {
                var angle = (360f / count) * i + 90f; // Start from side
                positions[i] = targetPosition + new Vector3(
                    (float)Math.Cos(angle * Math.PI / 180f) * radius,
                    0,
                    (float)Math.Sin(angle * Math.PI / 180f) * radius
                );
            }
            
            return positions;
        }
        
        private Vector3[] CalculatePincerPositions(Vector3 targetPosition, Vector3 targetVelocity)
        {
            var positions = new Vector3[2];
            var distance = 100f;
            
            // Predict where target will be
            var predictedPosition = targetPosition + targetVelocity * 3f;
            
            // Calculate perpendicular positions
            var perpendicular = Vector3.Cross(Vector3.Normalize(targetVelocity), Vector3.UnitY);
            if (perpendicular.LengthSquared() < 0.1f)
            {
                perpendicular = Vector3.UnitX;
            }
            
            positions[0] = predictedPosition + perpendicular * distance;
            positions[1] = predictedPosition - perpendicular * distance;
            
            return positions;
        }
        
        #endregion
    }
    
    public class TacticalSituation
    {
        public int AllyCount { get; set; }
        public float AllyHealthAverage { get; set; }
        public float TargetDistance { get; set; }
        public float FormationIntegrity { get; set; }
        public float ThreatLevel { get; set; }
        public float TerrainAdvantage { get; set; }
    }
    
    public class TacticalOption
    {
        public TacticType Type { get; set; }
        public float Effectiveness { get; set; }
        public TacticalRequirements Requirements { get; set; }
    }
    
    public class TacticalRequirements
    {
        public int MinAllies { get; set; } = 1;
        public float MinHealthRatio { get; set; } = 0f;
        public EnemyShipType[] RequiredShipTypes { get; set; }
    }
    
    public enum TacticType
    {
        DirectAssault,
        FlankingManeuver,
        PincerMovement,
        DefensiveFormation,
        HitAndRun,
        SuppressionBombardment
    }
    
    /// <summary>
    /// Stores tactical knowledge for AI learning
    /// </summary>
    public class TacticalMemory
    {
        private List<TacticalMemoryEntry> entries;
        
        public TacticalMemory()
        {
            entries = new List<TacticalMemoryEntry>();
        }
        
        public void RecordSituation(TacticalSituation situation)
        {
            // Record situation for analysis
        }
        
        public void RecordTacticExecution(TacticalOption tactic, TacticalSituation situation)
        {
            entries.Add(new TacticalMemoryEntry
            {
                Situation = situation,
                TacticUsed = tactic,
                Timestamp = DateTime.Now
            });
            
            // Limit memory size
            if (entries.Count > 100)
            {
                entries.RemoveAt(0);
            }
        }
        
        public float GetTacticSuccessRate(TacticType tacticType)
        {
            var tacticEntries = entries.Where(e => e.TacticUsed.Type == tacticType).ToList();
            if (tacticEntries.Count == 0) return 0.5f; // Default success rate
            
            return tacticEntries.Average(e => e.Success ? 1f : 0f);
        }
    }
    
    public class TacticalMemoryEntry
    {
        public TacticalSituation Situation { get; set; }
        public TacticalOption TacticUsed { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }
}