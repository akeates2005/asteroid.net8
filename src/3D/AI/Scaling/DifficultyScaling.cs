using System;
using System.Collections.Generic;
using System.Linq;
using Asteroids.AI.Core;
using Asteroids.AI.Formations;

namespace Asteroids.AI.Scaling
{
    /// <summary>
    /// Dynamic difficulty scaling system for AI opponents
    /// </summary>
    public class DifficultyScaling
    {
        private DifficultyLevel currentDifficulty;
        private float difficultyScore = 0.5f; // 0 = easiest, 1 = hardest
        private PlayerPerformanceTracker performanceTracker;
        private AIEnhancementSettings enhancementSettings;
        
        private float adaptationRate = 0.1f;
        private float evaluationInterval = 10f; // seconds
        private float lastEvaluation = 0f;
        
        public DifficultyLevel CurrentDifficulty => currentDifficulty;
        public float DifficultyScore => difficultyScore;
        
        public event Action<DifficultyLevel, DifficultyLevel> OnDifficultyChanged;
        
        public DifficultyScaling()
        {
            performanceTracker = new PlayerPerformanceTracker();
            enhancementSettings = new AIEnhancementSettings();
            currentDifficulty = DifficultyLevel.Medium;
        }
        
        public void Update(float deltaTime, List<AIEnemyShip> aiShips, object player)
        {
            lastEvaluation += deltaTime;
            
            // Track player performance
            performanceTracker.Update(deltaTime, player);
            
            // Evaluate and adjust difficulty periodically
            if (lastEvaluation >= evaluationInterval)
            {
                EvaluateAndAdjustDifficulty(aiShips);
                lastEvaluation = 0f;
            }
            
            // Apply current difficulty settings to AI
            ApplyDifficultyToAI(aiShips, deltaTime);
        }
        
        private void EvaluateAndAdjustDifficulty(List<AIEnemyShip> aiShips)
        {
            var performance = performanceTracker.GetCurrentPerformance();
            var targetDifficulty = CalculateTargetDifficulty(performance);
            
            // Gradually adjust difficulty
            var difficultyChange = (targetDifficulty - difficultyScore) * adaptationRate;
            difficultyScore = Math.Max(0f, Math.Min(1f, difficultyScore + difficultyChange));
            
            // Update difficulty level
            var newDifficultyLevel = GetDifficultyLevel(difficultyScore);
            if (newDifficultyLevel != currentDifficulty)
            {
                var oldDifficulty = currentDifficulty;
                currentDifficulty = newDifficultyLevel;
                OnDifficultyChanged?.Invoke(oldDifficulty, currentDifficulty);
                
                // Update AI enhancement settings
                UpdateEnhancementSettings();
            }
        }
        
        private float CalculateTargetDifficulty(PlayerPerformance performance)
        {
            var targetDifficulty = 0.5f; // Start neutral
            
            // Adjust based on survival time
            if (performance.AverageSurvivalTime > 120f) // 2 minutes
            {
                targetDifficulty += 0.2f; // Player surviving well, increase difficulty
            }
            else if (performance.AverageSurvivalTime < 30f) // 30 seconds
            {
                targetDifficulty -= 0.3f; // Player dying quickly, decrease difficulty
            }
            
            // Adjust based on kill/death ratio
            if (performance.KillDeathRatio > 2f)
            {
                targetDifficulty += 0.3f; // Player dominating, increase difficulty
            }
            else if (performance.KillDeathRatio < 0.5f)
            {
                targetDifficulty -= 0.2f; // Player struggling, decrease difficulty
            }
            
            // Adjust based on accuracy
            if (performance.Accuracy > 0.8f)
            {
                targetDifficulty += 0.1f; // High accuracy, slight increase
            }
            else if (performance.Accuracy < 0.3f)
            {
                targetDifficulty -= 0.1f; // Low accuracy, slight decrease
            }
            
            // Adjust based on recent death streak
            if (performance.RecentDeathStreak >= 3)
            {
                targetDifficulty -= 0.4f; // Multiple recent deaths, significant decrease
            }
            
            // Adjust based on time since last death
            if (performance.TimeSinceLastDeath > 180f) // 3 minutes
            {
                targetDifficulty += 0.2f; // No recent deaths, increase challenge
            }
            
            return Math.Max(0f, Math.Min(1f, targetDifficulty));
        }
        
        private DifficultyLevel GetDifficultyLevel(float score)
        {
            return score switch
            {
                <= 0.2f => DifficultyLevel.VeryEasy,
                <= 0.4f => DifficultyLevel.Easy,
                <= 0.6f => DifficultyLevel.Medium,
                <= 0.8f => DifficultyLevel.Hard,
                _ => DifficultyLevel.VeryHard
            };
        }
        
        private void UpdateEnhancementSettings()
        {
            enhancementSettings = currentDifficulty switch
            {
                DifficultyLevel.VeryEasy => CreateVeryEasySettings(),
                DifficultyLevel.Easy => CreateEasySettings(),
                DifficultyLevel.Medium => CreateMediumSettings(),
                DifficultyLevel.Hard => CreateHardSettings(),
                DifficultyLevel.VeryHard => CreateVeryHardSettings(),
                _ => CreateMediumSettings()
            };
        }
        
        private void ApplyDifficultyToAI(List<AIEnemyShip> aiShips, float deltaTime)
        {
            foreach (var ship in aiShips)
            {
                ApplyEnhancementsToShip(ship);
            }
        }
        
        private void ApplyEnhancementsToShip(AIEnemyShip ship)
        {
            // Apply speed modifications
            ship.Speed *= enhancementSettings.SpeedMultiplier;
            
            // Apply health modifications
            ship.MaxHealth *= enhancementSettings.HealthMultiplier;
            ship.Health = Math.Min(ship.Health * enhancementSettings.HealthMultiplier, ship.MaxHealth);
            
            // Apply accuracy modifications
            ship.RotationSpeed *= enhancementSettings.AccuracyMultiplier;
            
            // Apply reaction time modifications
            ship.DetectionRange *= enhancementSettings.DetectionRangeMultiplier;
            
            // Apply aggression modifications
            ship.Aggressiveness = Math.Max(0f, Math.Min(1f, 
                ship.Aggressiveness * enhancementSettings.AggressionMultiplier));
            
            // Apply teamwork modifications
            ship.TeamworkTendency = Math.Max(0f, Math.Min(1f, 
                ship.TeamworkTendency * enhancementSettings.TeamworkMultiplier));
            
            // Apply formation complexity
            if (ship.Formation != null)
            {
                ApplyFormationComplexity(ship.Formation);
            }
        }
        
        private void ApplyFormationComplexity(FormationController formation)
        {
            // Adjust formation based on difficulty
            switch (currentDifficulty)
            {
                case DifficultyLevel.VeryEasy:
                    formation.FormationScale = 1.5f; // Loose formation
                    break;
                    
                case DifficultyLevel.Easy:
                    formation.FormationScale = 1.2f;
                    break;
                    
                case DifficultyLevel.Medium:
                    formation.FormationScale = 1.0f; // Normal
                    break;
                    
                case DifficultyLevel.Hard:
                    formation.FormationScale = 0.8f; // Tight formation
                    if (formation.FormationType == FormationType.VFormation)
                    {
                        formation.ChangeFormation(FormationType.Diamond); // More complex
                    }
                    break;
                    
                case DifficultyLevel.VeryHard:
                    formation.FormationScale = 0.6f; // Very tight
                    if (formation.FormationType != FormationType.Sphere && formation.FormationType != FormationType.Helix)
                    {
                        formation.ChangeFormation(FormationType.Sphere); // Most complex
                    }
                    break;
            }
        }
        
        #region Difficulty Settings Creation
        
        private AIEnhancementSettings CreateVeryEasySettings()
        {
            return new AIEnhancementSettings
            {
                SpeedMultiplier = 0.7f,
                HealthMultiplier = 0.6f,
                AccuracyMultiplier = 0.5f,
                DetectionRangeMultiplier = 0.8f,
                AggressionMultiplier = 0.6f,
                TeamworkMultiplier = 0.5f,
                ReactionTimeMultiplier = 1.5f,
                FormationComplexity = FormationComplexity.Simple,
                MaxSimultaneousEnemies = 2
            };
        }
        
        private AIEnhancementSettings CreateEasySettings()
        {
            return new AIEnhancementSettings
            {
                SpeedMultiplier = 0.8f,
                HealthMultiplier = 0.8f,
                AccuracyMultiplier = 0.7f,
                DetectionRangeMultiplier = 0.9f,
                AggressionMultiplier = 0.8f,
                TeamworkMultiplier = 0.7f,
                ReactionTimeMultiplier = 1.3f,
                FormationComplexity = FormationComplexity.Simple,
                MaxSimultaneousEnemies = 3
            };
        }
        
        private AIEnhancementSettings CreateMediumSettings()
        {
            return new AIEnhancementSettings
            {
                SpeedMultiplier = 1.0f,
                HealthMultiplier = 1.0f,
                AccuracyMultiplier = 1.0f,
                DetectionRangeMultiplier = 1.0f,
                AggressionMultiplier = 1.0f,
                TeamworkMultiplier = 1.0f,
                ReactionTimeMultiplier = 1.0f,
                FormationComplexity = FormationComplexity.Medium,
                MaxSimultaneousEnemies = 4
            };
        }
        
        private AIEnhancementSettings CreateHardSettings()
        {
            return new AIEnhancementSettings
            {
                SpeedMultiplier = 1.2f,
                HealthMultiplier = 1.3f,
                AccuracyMultiplier = 1.3f,
                DetectionRangeMultiplier = 1.2f,
                AggressionMultiplier = 1.2f,
                TeamworkMultiplier = 1.3f,
                ReactionTimeMultiplier = 0.8f,
                FormationComplexity = FormationComplexity.Complex,
                MaxSimultaneousEnemies = 6
            };
        }
        
        private AIEnhancementSettings CreateVeryHardSettings()
        {
            return new AIEnhancementSettings
            {
                SpeedMultiplier = 1.4f,
                HealthMultiplier = 1.5f,
                AccuracyMultiplier = 1.5f,
                DetectionRangeMultiplier = 1.4f,
                AggressionMultiplier = 1.4f,
                TeamworkMultiplier = 1.5f,
                ReactionTimeMultiplier = 0.6f,
                FormationComplexity = FormationComplexity.VeryComplex,
                MaxSimultaneousEnemies = 8
            };
        }
        
        #endregion
        
        public void SetDifficulty(DifficultyLevel level)
        {
            var oldDifficulty = currentDifficulty;
            currentDifficulty = level;
            difficultyScore = level switch
            {
                DifficultyLevel.VeryEasy => 0.1f,
                DifficultyLevel.Easy => 0.3f,
                DifficultyLevel.Medium => 0.5f,
                DifficultyLevel.Hard => 0.7f,
                DifficultyLevel.VeryHard => 0.9f,
                _ => 0.5f
            };
            
            UpdateEnhancementSettings();
            OnDifficultyChanged?.Invoke(oldDifficulty, currentDifficulty);
        }
        
        public void SetAdaptationRate(float rate)
        {
            adaptationRate = Math.Max(0.01f, Math.Min(1f, rate));
        }
        
        public PlayerPerformance GetPlayerPerformance()
        {
            return performanceTracker.GetCurrentPerformance();
        }
    }
    
    /// <summary>
    /// Tracks player performance metrics for difficulty adjustment
    /// </summary>
    public class PlayerPerformanceTracker
    {
        private PlayerPerformance currentPerformance;
        private List<float> survivalTimes;
        private List<DateTime> deathTimes;
        private int totalKills;
        private int totalDeaths;
        private int totalShots;
        private int totalHits;
        
        public PlayerPerformanceTracker()
        {
            currentPerformance = new PlayerPerformance();
            survivalTimes = new List<float>();
            deathTimes = new List<DateTime>();
        }
        
        public void Update(float deltaTime, object player)
        {
            // Update performance metrics
            currentPerformance.CurrentSurvivalTime += deltaTime;
            
            // Calculate moving averages
            CalculateAverages();
        }
        
        public void RecordDeath(float survivalTime)
        {
            survivalTimes.Add(survivalTime);
            deathTimes.Add(DateTime.Now);
            totalDeaths++;
            
            // Limit history size
            if (survivalTimes.Count > 10)
            {
                survivalTimes.RemoveAt(0);
            }
            
            if (deathTimes.Count > 10)
            {
                deathTimes.RemoveAt(0);
            }
            
            // Reset current survival time
            currentPerformance.CurrentSurvivalTime = 0f;
            
            // Update death streak
            UpdateDeathStreak();
        }
        
        public void RecordKill()
        {
            totalKills++;
            currentPerformance.RecentDeathStreak = 0; // Reset death streak on kill
        }
        
        public void RecordShot(bool hit)
        {
            totalShots++;
            if (hit) totalHits++;
        }
        
        private void CalculateAverages()
        {
            // Calculate average survival time
            if (survivalTimes.Count > 0)
            {
                currentPerformance.AverageSurvivalTime = survivalTimes.Average();
            }
            
            // Calculate kill/death ratio
            if (totalDeaths > 0)
            {
                currentPerformance.KillDeathRatio = (float)totalKills / totalDeaths;
            }
            else
            {
                currentPerformance.KillDeathRatio = totalKills > 0 ? 10f : 1f;
            }
            
            // Calculate accuracy
            if (totalShots > 0)
            {
                currentPerformance.Accuracy = (float)totalHits / totalShots;
            }
            
            // Calculate time since last death
            if (deathTimes.Count > 0)
            {
                var lastDeath = deathTimes.Last();
                currentPerformance.TimeSinceLastDeath = (float)(DateTime.Now - lastDeath).TotalSeconds;
            }
            else
            {
                currentPerformance.TimeSinceLastDeath = float.MaxValue;
            }
        }
        
        private void UpdateDeathStreak()
        {
            var recentDeaths = deathTimes.Where(d => 
                (DateTime.Now - d).TotalSeconds < 60f // Deaths within last minute
            ).Count();
            
            currentPerformance.RecentDeathStreak = recentDeaths;
        }
        
        public PlayerPerformance GetCurrentPerformance()
        {
            CalculateAverages();
            return currentPerformance;
        }
        
        public void Reset()
        {
            currentPerformance = new PlayerPerformance();
            survivalTimes.Clear();
            deathTimes.Clear();
            totalKills = 0;
            totalDeaths = 0;
            totalShots = 0;
            totalHits = 0;
        }
    }
    
    public class PlayerPerformance
    {
        public float CurrentSurvivalTime { get; set; }
        public float AverageSurvivalTime { get; set; }
        public float KillDeathRatio { get; set; } = 1f;
        public float Accuracy { get; set; } = 0.5f;
        public int RecentDeathStreak { get; set; }
        public float TimeSinceLastDeath { get; set; } = float.MaxValue;
    }
    
    public class AIEnhancementSettings
    {
        public float SpeedMultiplier { get; set; } = 1f;
        public float HealthMultiplier { get; set; } = 1f;
        public float AccuracyMultiplier { get; set; } = 1f;
        public float DetectionRangeMultiplier { get; set; } = 1f;
        public float AggressionMultiplier { get; set; } = 1f;
        public float TeamworkMultiplier { get; set; } = 1f;
        public float ReactionTimeMultiplier { get; set; } = 1f;
        public FormationComplexity FormationComplexity { get; set; } = FormationComplexity.Medium;
        public int MaxSimultaneousEnemies { get; set; } = 4;
    }
    
    public enum DifficultyLevel
    {
        VeryEasy,
        Easy,
        Medium,
        Hard,
        VeryHard
    }
    
    public enum FormationComplexity
    {
        Simple,      // Line, basic V
        Medium,      // Diamond, Box
        Complex,     // Sphere, coordinated maneuvers
        VeryComplex  // Helix, advanced tactics
    }
}