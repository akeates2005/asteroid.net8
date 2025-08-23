using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Asteroids.AI.Behaviors;
using Asteroids.AI.Communication;
using Asteroids.AI.Core;
using Asteroids.AI.Formations;
using Asteroids.AI.Navigation;
using Asteroids.AI.Scaling;
using Asteroids.AI.Ships;
using Asteroids.AI.Swarm;
using Asteroids.AI.Tactical;

namespace Asteroids.AI
{
    /// <summary>
    /// Central AI manager that orchestrates all enemy AI behaviors
    /// </summary>
    public class AIManager
    {
        private List<AIEnemyShip> allEnemyShips;
        private List<FormationController> formations;
        private SwarmBehavior swarmBehavior;
        private TacticalAI tacticalAI;
        private DifficultyScaling difficultyScaling;
        
        private AIEnemyShip player; // Reference to player ship for targeting
        private float spawnTimer = 0f;
        private float spawnInterval = 8f;
        private int maxEnemies = 8;
        private Vector3 worldCenter = Vector3.Zero;
        private float worldRadius = 400f;
        
        // AI coordination timers
        private float tacticalUpdateInterval = 3f;
        private float lastTacticalUpdate = 0f;
        private float formationUpdateInterval = 1f;
        private float lastFormationUpdate = 0f;
        private float swarmUpdateInterval = 0.1f;
        private float lastSwarmUpdate = 0f;
        
        public IReadOnlyList<AIEnemyShip> EnemyShips => allEnemyShips.AsReadOnly();
        public IReadOnlyList<FormationController> Formations => formations.AsReadOnly();
        public DifficultyLevel CurrentDifficulty => difficultyScaling.CurrentDifficulty;
        
        public event Action<AIEnemyShip> OnEnemySpawned;
        public event Action<AIEnemyShip> OnEnemyDestroyed;
        public event Action<FormationController> OnFormationCreated;
        public event Action<FormationController> OnFormationDestroyed;
        
        public AIManager()
        {
            allEnemyShips = new List<AIEnemyShip>();
            formations = new List<FormationController>();
            swarmBehavior = new SwarmBehavior();
            tacticalAI = new TacticalAI();
            difficultyScaling = new DifficultyScaling();
            
            // Subscribe to difficulty changes
            difficultyScaling.OnDifficultyChanged += OnDifficultyChanged;
        }
        
        public void Initialize(AIEnemyShip playerShip, Vector3 worldCenter, float worldRadius)
        {
            this.player = playerShip;
            this.worldCenter = worldCenter;
            this.worldRadius = worldRadius;
            
            // Initialize communication hub
            CommunicationHub.Instance.SetCommunicationRange(150f);
        }
        
        public void Update(float deltaTime)
        {
            // Update difficulty scaling
            difficultyScaling.Update(deltaTime, allEnemyShips, player);
            
            // Update all enemy ships
            UpdateEnemyShips(deltaTime);
            
            // Update formations
            UpdateFormations(deltaTime);
            
            // Update swarm behavior
            UpdateSwarmBehavior(deltaTime);
            
            // Update tactical AI
            UpdateTacticalAI(deltaTime);
            
            // Handle spawning
            UpdateSpawning(deltaTime);
            
            // Clean up destroyed ships
            CleanupDestroyedShips();
        }
        
        private void UpdateEnemyShips(float deltaTime)
        {
            foreach (var ship in allEnemyShips.ToList())
            {
                // Update ship AI
                ship.Update(deltaTime);
                
                // Update nearby allies list
                ship.UpdateNearbyAllies(allEnemyShips, 100f);
                
                // Handle target acquisition
                UpdateTargeting(ship);
                
                // Keep ships within world bounds
                ConstrainToWorldBounds(ship);
            }
        }
        
        private void UpdateFormations(float deltaTime)
        {
            lastFormationUpdate += deltaTime;
            
            if (lastFormationUpdate >= formationUpdateInterval)
            {
                foreach (var formation in formations.ToList())
                {
                    formation.Update(deltaTime);
                    
                    // Remove empty formations
                    if (formation.MemberCount == 0)
                    {
                        formations.Remove(formation);
                        OnFormationDestroyed?.Invoke(formation);
                    }
                }
                
                // Create new formations for unassigned ships
                CreateFormationsForUnassignedShips();
                
                lastFormationUpdate = 0f;
            }
        }
        
        private void UpdateSwarmBehavior(float deltaTime)
        {
            lastSwarmUpdate += deltaTime;
            
            if (lastSwarmUpdate >= swarmUpdateInterval)
            {
                swarmBehavior.UpdateSwarmBehavior(allEnemyShips, deltaTime);
                EmergentBehaviors.UpdateEmergentBehaviors(allEnemyShips, deltaTime);
                
                lastSwarmUpdate = 0f;
            }
        }
        
        private void UpdateTacticalAI(float deltaTime)
        {
            lastTacticalUpdate += deltaTime;
            
            if (lastTacticalUpdate >= tacticalUpdateInterval)
            {
                // Group ships by formation for tactical decisions
                var formationGroups = allEnemyShips
                    .Where(s => s.Formation != null)
                    .GroupBy(s => s.Formation)
                    .ToList();
                
                foreach (var group in formationGroups)
                {
                    tacticalAI.Update(group.ToList(), player, deltaTime);
                }
                
                // Handle individual ships
                var individualShips = allEnemyShips.Where(s => s.Formation == null).ToList();
                if (individualShips.Count > 0)
                {
                    tacticalAI.Update(individualShips, player, deltaTime);
                }
                
                lastTacticalUpdate = 0f;
            }
        }
        
        private void UpdateTargeting(AIEnemyShip ship)
        {
            // Check if ship can see player
            if (player != null && ship.CanSee(player.Position))
            {
                ship.SetTarget(player);
                
                // Share target information
                ship.Communication.BroadcastMessage(new AIMessage
                {
                    Type = MessageType.TargetSighted,
                    Sender = ship,
                    Position = player.Position,
                    Data = new { Health = player.Health, Velocity = player.Velocity }
                });
            }
            else if (ship.Target == player && ship.LastPlayerSightingTime > 10f)
            {
                // Lost player for too long, clear target
                ship.SetTarget(null);
            }
        }
        
        private void UpdateSpawning(float deltaTime)
        {
            spawnTimer += deltaTime;
            
            var currentEnemyCount = allEnemyShips.Count;
            var maxEnemiesForDifficulty = GetMaxEnemiesForDifficulty();
            
            if (spawnTimer >= spawnInterval && currentEnemyCount < maxEnemiesForDifficulty)
            {
                SpawnEnemyWave();
                spawnTimer = 0f;
                
                // Adjust spawn interval based on difficulty
                AdjustSpawnInterval();
            }
        }
        
        private void SpawnEnemyWave()
        {
            var waveSize = CalculateWaveSize();
            var spawnPosition = CalculateSpawnPosition();
            
            // Determine ship types based on difficulty and current composition
            var shipTypes = DetermineShipTypesForWave(waveSize);
            
            var newShips = new List<AIEnemyShip>();
            
            for (int i = 0; i < waveSize; i++)
            {
                var shipType = shipTypes[i % shipTypes.Length];
                var ship = CreateEnemyShip(shipType);
                
                // Position ships in formation spawn pattern
                var offset = CalculateFormationSpawnOffset(i, waveSize);
                ship.Position = spawnPosition + offset;
                
                allEnemyShips.Add(ship);
                newShips.Add(ship);
                
                // Register with communication hub
                CommunicationHub.Instance.RegisterShip(ship);
                
                // Subscribe to ship events
                ship.OnDestroyed += OnShipDestroyed;
                
                OnEnemySpawned?.Invoke(ship);
            }
            
            // Create formation for new ships if appropriate
            if (newShips.Count >= 3)
            {
                CreateFormationForShips(newShips);
            }
        }
        
        private AIEnemyShip CreateEnemyShip(EnemyShipType shipType)
        {
            return shipType switch
            {
                EnemyShipType.Scout => new ScoutShip(),
                EnemyShipType.Fighter => new FighterShip(),
                EnemyShipType.Bomber => new BomberShip(),
                EnemyShipType.Interceptor => new InterceptorShip(),
                _ => new FighterShip()
            };
        }
        
        private void CreateFormationForShips(List<AIEnemyShip> ships)
        {
            if (ships.Count < 2) return;
            
            var formationType = ChooseFormationType(ships);
            var centerPosition = ships.Aggregate(Vector3.Zero, (sum, ship) => sum + ship.Position) / ships.Count;
            
            var formation = new FormationController(formationType, centerPosition);
            
            foreach (var ship in ships)
            {
                formation.AddMember(ship);
            }
            
            formations.Add(formation);
            OnFormationCreated?.Invoke(formation);
        }
        
        private FormationType ChooseFormationType(List<AIEnemyShip> ships)
        {
            var shipCount = ships.Count;
            var difficulty = difficultyScaling.CurrentDifficulty;
            
            // Choose formation based on ship count and difficulty
            return (shipCount, difficulty) switch
            {
                (2, _) => FormationType.Line,
                (3, DifficultyLevel.VeryEasy or DifficultyLevel.Easy) => FormationType.VFormation,
                (3, _) => FormationType.VFormation,
                (4, DifficultyLevel.VeryEasy or DifficultyLevel.Easy) => FormationType.Diamond,
                (4, _) => FormationType.Diamond,
                (>= 5, DifficultyLevel.VeryEasy) => FormationType.Line,
                (>= 5, DifficultyLevel.Easy) => FormationType.Box,
                (>= 5, DifficultyLevel.Medium) => FormationType.Diamond,
                (>= 5, DifficultyLevel.Hard) => FormationType.Sphere,
                (>= 5, DifficultyLevel.VeryHard) => FormationType.Helix,
                _ => FormationType.VFormation
            };
        }
        
        private void CreateFormationsForUnassignedShips()
        {
            var unassigned = allEnemyShips.Where(s => s.Formation == null).ToList();
            
            if (unassigned.Count >= 3)
            {
                // Group nearby unassigned ships
                var clusters = ClusterShipsByProximity(unassigned, 80f);
                
                foreach (var cluster in clusters)
                {
                    if (cluster.Count >= 3)
                    {
                        CreateFormationForShips(cluster);
                    }
                }
            }
        }
        
        private List<List<AIEnemyShip>> ClusterShipsByProximity(List<AIEnemyShip> ships, float maxDistance)
        {
            var clusters = new List<List<AIEnemyShip>>();
            var assigned = new HashSet<AIEnemyShip>();
            
            foreach (var ship in ships)
            {
                if (assigned.Contains(ship)) continue;
                
                var cluster = new List<AIEnemyShip> { ship };
                assigned.Add(ship);
                
                // Find nearby ships
                var nearbyShips = ships.Where(s => 
                    !assigned.Contains(s) && 
                    ship.DistanceTo(s) <= maxDistance
                ).ToList();
                
                cluster.AddRange(nearbyShips);
                foreach (var nearby in nearbyShips)
                {
                    assigned.Add(nearby);
                }
                
                if (cluster.Count >= 2)
                {
                    clusters.Add(cluster);
                }
            }
            
            return clusters;
        }
        
        private int CalculateWaveSize()
        {
            var baseSizeForDifficulty = difficultyScaling.CurrentDifficulty switch
            {
                DifficultyLevel.VeryEasy => 1,
                DifficultyLevel.Easy => 2,
                DifficultyLevel.Medium => 3,
                DifficultyLevel.Hard => 4,
                DifficultyLevel.VeryHard => 5,
                _ => 2
            };
            
            // Adjust based on current enemy count
            var currentCount = allEnemyShips.Count;
            var maxEnemies = GetMaxEnemiesForDifficulty();
            
            return Math.Min(baseSizeForDifficulty, maxEnemies - currentCount);
        }
        
        private EnemyShipType[] DetermineShipTypesForWave(int waveSize)
        {
            var shipTypes = new List<EnemyShipType>();
            var difficulty = difficultyScaling.CurrentDifficulty;
            
            // Determine composition based on difficulty
            for (int i = 0; i < waveSize; i++)
            {
                var shipType = difficulty switch
                {
                    DifficultyLevel.VeryEasy => ChooseShipType(new[] { 
                        (EnemyShipType.Scout, 0.6f), 
                        (EnemyShipType.Fighter, 0.4f) 
                    }),
                    DifficultyLevel.Easy => ChooseShipType(new[] { 
                        (EnemyShipType.Scout, 0.4f), 
                        (EnemyShipType.Fighter, 0.6f) 
                    }),
                    DifficultyLevel.Medium => ChooseShipType(new[] { 
                        (EnemyShipType.Scout, 0.3f), 
                        (EnemyShipType.Fighter, 0.5f), 
                        (EnemyShipType.Interceptor, 0.2f) 
                    }),
                    DifficultyLevel.Hard => ChooseShipType(new[] { 
                        (EnemyShipType.Scout, 0.2f), 
                        (EnemyShipType.Fighter, 0.4f), 
                        (EnemyShipType.Interceptor, 0.2f), 
                        (EnemyShipType.Bomber, 0.2f) 
                    }),
                    DifficultyLevel.VeryHard => ChooseShipType(new[] { 
                        (EnemyShipType.Scout, 0.1f), 
                        (EnemyShipType.Fighter, 0.3f), 
                        (EnemyShipType.Interceptor, 0.3f), 
                        (EnemyShipType.Bomber, 0.3f) 
                    }),
                    _ => EnemyShipType.Fighter
                };
                
                shipTypes.Add(shipType);
            }
            
            return shipTypes.ToArray();
        }
        
        private EnemyShipType ChooseShipType((EnemyShipType type, float weight)[] weightedTypes)
        {
            var random = new Random();
            var totalWeight = weightedTypes.Sum(t => t.weight);
            var randomValue = random.NextSingle() * totalWeight;
            
            float currentWeight = 0;
            foreach (var (type, weight) in weightedTypes)
            {
                currentWeight += weight;
                if (randomValue <= currentWeight)
                {
                    return type;
                }
            }
            
            return weightedTypes[0].type; // Fallback
        }
        
        private Vector3 CalculateSpawnPosition()
        {
            var random = new Random();
            
            // Spawn outside player's immediate vicinity but within world bounds
            var angle = random.NextSingle() * 2 * Math.PI;
            var distance = 200f + random.NextSingle() * 100f; // 200-300 units from center
            
            var spawnPosition = worldCenter + new Vector3(
                (float)Math.Cos(angle) * distance,
                (random.NextSingle() - 0.5f) * 100f, // Some vertical variation
                (float)Math.Sin(angle) * distance
            );
            
            return spawnPosition;
        }
        
        private Vector3 CalculateFormationSpawnOffset(int index, int totalShips)
        {
            var spacing = 20f;
            var row = index / 3;
            var col = index % 3;
            
            return new Vector3(
                (col - 1) * spacing,
                0,
                row * spacing
            );
        }
        
        private void ConstrainToWorldBounds(AIEnemyShip ship)
        {
            var distance = Vector3.Distance(ship.Position, worldCenter);
            
            if (distance > worldRadius)
            {
                // Push ship back toward center
                var direction = Vector3.Normalize(worldCenter - ship.Position);
                var pushForce = direction * (distance - worldRadius) * 0.1f;
                ship.Velocity += pushForce;
                
                // If ship is way outside bounds, teleport it back
                if (distance > worldRadius * 1.5f)
                {
                    var newDirection = Vector3.Normalize(ship.Position - worldCenter);
                    ship.Position = worldCenter + newDirection * (worldRadius * 0.8f);
                }
            }
        }
        
        private void CleanupDestroyedShips()
        {
            for (int i = allEnemyShips.Count - 1; i >= 0; i--)
            {
                var ship = allEnemyShips[i];
                if (ship.Health <= 0)
                {
                    RemoveEnemyShip(ship);
                }
            }
        }
        
        private void RemoveEnemyShip(AIEnemyShip ship)
        {
            allEnemyShips.Remove(ship);
            CommunicationHub.Instance.UnregisterShip(ship);
            
            // Remove from formation
            ship.Formation?.RemoveMember(ship);
            
            OnEnemyDestroyed?.Invoke(ship);
        }
        
        private void OnShipDestroyed(AIEnemyShip ship)
        {
            // Record kill for difficulty scaling
            difficultyScaling.GetPlayerPerformance();
            
            RemoveEnemyShip(ship);
        }
        
        private int GetMaxEnemiesForDifficulty()
        {
            return difficultyScaling.CurrentDifficulty switch
            {
                DifficultyLevel.VeryEasy => 3,
                DifficultyLevel.Easy => 4,
                DifficultyLevel.Medium => 6,
                DifficultyLevel.Hard => 8,
                DifficultyLevel.VeryHard => 10,
                _ => 6
            };
        }
        
        private void AdjustSpawnInterval()
        {
            var baseInterval = 10f;
            var difficultyMultiplier = difficultyScaling.CurrentDifficulty switch
            {
                DifficultyLevel.VeryEasy => 1.5f,
                DifficultyLevel.Easy => 1.2f,
                DifficultyLevel.Medium => 1.0f,
                DifficultyLevel.Hard => 0.8f,
                DifficultyLevel.VeryHard => 0.6f,
                _ => 1.0f
            };
            
            spawnInterval = baseInterval * difficultyMultiplier;
        }
        
        private void OnDifficultyChanged(DifficultyLevel oldLevel, DifficultyLevel newLevel)
        {
            // Adjust existing AI ships for new difficulty
            foreach (var ship in allEnemyShips)
            {
                // Apply difficulty scaling would be handled by the DifficultyScaling class
            }
            
            // Adjust formation complexity
            foreach (var formation in formations)
            {
                AdjustFormationForDifficulty(formation, newLevel);
            }
        }
        
        private void AdjustFormationForDifficulty(FormationController formation, DifficultyLevel difficulty)
        {
            formation.FormationScale = difficulty switch
            {
                DifficultyLevel.VeryEasy => 1.5f,
                DifficultyLevel.Easy => 1.2f,
                DifficultyLevel.Medium => 1.0f,
                DifficultyLevel.Hard => 0.8f,
                DifficultyLevel.VeryHard => 0.6f,
                _ => 1.0f
            };
        }
        
        public void SetDifficulty(DifficultyLevel level)
        {
            difficultyScaling.SetDifficulty(level);
        }
        
        public void SetMaxEnemies(int max)
        {
            maxEnemies = max;
        }
        
        public void SetSpawnInterval(float interval)
        {
            spawnInterval = interval;
        }
        
        public void ClearAllEnemies()
        {
            foreach (var ship in allEnemyShips.ToList())
            {
                RemoveEnemyShip(ship);
            }
            
            formations.Clear();
        }
        
        public List<AIEnemyShip> GetEnemiesInRadius(Vector3 center, float radius)
        {
            return allEnemyShips.Where(ship => 
                Vector3.Distance(ship.Position, center) <= radius
            ).ToList();
        }
        
        public FormationController GetLargestFormation()
        {
            return formations.OrderByDescending(f => f.MemberCount).FirstOrDefault();
        }
        
        public void ForceFormationCreation(List<AIEnemyShip> ships, FormationType formationType)
        {
            if (ships.Count >= 2)
            {
                // Remove ships from existing formations
                foreach (var ship in ships)
                {
                    ship.Formation?.RemoveMember(ship);
                }
                
                var centerPosition = ships.Aggregate(Vector3.Zero, (sum, ship) => sum + ship.Position) / ships.Count;
                var formation = new FormationController(formationType, centerPosition);
                
                foreach (var ship in ships)
                {
                    formation.AddMember(ship);
                }
                
                formations.Add(formation);
                OnFormationCreated?.Invoke(formation);
            }
        }
    }
}