using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Advanced AI system for enemy ships with sophisticated behaviors
    /// Implements pursuit, intercept, formation flying, and tactical AI
    /// </summary>
    public class EnemyAI
    {
        private readonly Random _random;
        private const float PURSUIT_ACCELERATION = 100f;
        private const float RETREAT_ACCELERATION = 120f;
        private const float CIRCLING_RADIUS = 80f;
        private const float FORMATION_SPACING = 60f;
        private const float INTERCEPT_PREDICTION_TIME = 2f;
        private const float STATE_CHANGE_INTERVAL = 3f;

        public EnemyAI()
        {
            _random = new Random();
        }

        /// <summary>
        /// Main AI update method that orchestrates all AI behaviors
        /// </summary>
        public void UpdateAIState(EnemyShip enemy, Player player, float deltaTime)
        {
            if (!enemy.Active || player == null) return;

            Vector2 toPlayer = player.Position - enemy.Position;
            float distanceToPlayer = toPlayer.Length();

            // Update state based on distance and current behavior
            UpdateStateTransitions(enemy, distanceToPlayer, deltaTime);

            // Execute current state behavior
            switch (enemy.CurrentState)
            {
                case AIState.Idle:
                    UpdateIdleBehavior(enemy, deltaTime);
                    break;
                    
                case AIState.Pursuing:
                    UpdatePursuitBehavior(enemy, player.Position, deltaTime);
                    break;
                    
                case AIState.Retreating:
                    UpdateRetreatBehavior(enemy, player.Position, deltaTime);
                    break;
                    
                case AIState.Circling:
                    UpdateCirclingBehavior(enemy, player.Position, deltaTime);
                    break;
                    
                case AIState.Attacking:
                    UpdateAttackBehavior(enemy, player, deltaTime);
                    break;
                    
                case AIState.FormationFlying:
                    UpdateFormationBehavior(enemy, deltaTime);
                    break;
                    
                case AIState.Intercepting:
                    UpdateInterceptBehavior(enemy, player.Position, player.Velocity, deltaTime);
                    break;
                    
                case AIState.Evading:
                    UpdateEvadingBehavior(enemy, player.Position, deltaTime);
                    break;
            }

            // Apply type-specific behavior modifications
            ApplyTypeSpecificBehavior(enemy, player, deltaTime);
        }

        /// <summary>
        /// Handle state transitions based on distance and timing
        /// </summary>
        private void UpdateStateTransitions(EnemyShip enemy, float distanceToPlayer, float deltaTime)
        {
            // State transition logic based on distance thresholds
            if (distanceToPlayer > enemy.AttackRange * 1.5f)
            {
                if (enemy.CurrentState != AIState.Pursuing && enemy.CurrentState != AIState.Intercepting)
                {
                    TransitionToState(enemy, AIState.Pursuing);
                }
            }
            else if (distanceToPlayer < enemy.RetreatDistance)
            {
                if (enemy.CurrentState != AIState.Retreating && enemy.CurrentState != AIState.Evading)
                {
                    TransitionToState(enemy, AIState.Retreating);
                }
            }
            else if (distanceToPlayer <= enemy.AttackRange)
            {
                if (enemy.CurrentState != AIState.Attacking && enemy.CurrentState != AIState.Circling)
                {
                    // Randomly choose between attacking and circling
                    AIState nextState = _random.NextDouble() < 0.6 ? AIState.Attacking : AIState.Circling;
                    TransitionToState(enemy, nextState);
                }
            }

            // Periodic state changes for variety
            if (enemy.StateTimer > STATE_CHANGE_INTERVAL)
            {
                ConsiderStateChange(enemy, distanceToPlayer);
            }
        }

        /// <summary>
        /// Consider changing state for behavioral variety
        /// </summary>
        private void ConsiderStateChange(EnemyShip enemy, float distanceToPlayer)
        {
            // 30% chance to change state for unpredictability
            if (_random.NextDouble() < 0.3)
            {
                AIState newState = enemy.CurrentState;

                switch (enemy.CurrentState)
                {
                    case AIState.Pursuing:
                        newState = AIState.Intercepting;
                        break;
                    case AIState.Attacking:
                        newState = AIState.Circling;
                        break;
                    case AIState.Circling:
                        newState = AIState.Attacking;
                        break;
                    case AIState.Retreating:
                        newState = AIState.Evading;
                        break;
                }

                if (newState != enemy.CurrentState)
                {
                    TransitionToState(enemy, newState);
                }
            }
        }

        /// <summary>
        /// Transition to a new AI state
        /// </summary>
        private void TransitionToState(EnemyShip enemy, AIState newState)
        {
            enemy.CurrentState = newState;
            enemy.StateTimer = 0f;
        }

        /// <summary>
        /// Idle behavior - random movement
        /// </summary>
        private void UpdateIdleBehavior(EnemyShip enemy, float deltaTime)
        {
            // Random movement when idle
            if (_random.NextDouble() < 0.02) // 2% chance per frame to change direction
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                enemy.Velocity += direction * enemy.Speed * 0.3f * deltaTime;
            }
        }

        /// <summary>
        /// Pursuit behavior - move toward player
        /// </summary>
        public void UpdatePursuitBehavior(EnemyShip enemy, Vector2 playerPos, float deltaTime)
        {
            Vector2 toPlayer = playerPos - enemy.Position;
            float distance = toPlayer.Length();

            if (distance > 0)
            {
                Vector2 direction = Vector2.Normalize(toPlayer);
                enemy.Velocity += direction * PURSUIT_ACCELERATION * deltaTime;

                // Limit velocity to enemy's max speed
                if (enemy.Velocity.Length() > enemy.Speed)
                {
                    enemy.Velocity = Vector2.Normalize(enemy.Velocity) * enemy.Speed;
                }
            }
        }

        /// <summary>
        /// Retreat behavior - move away from player
        /// </summary>
        private void UpdateRetreatBehavior(EnemyShip enemy, Vector2 playerPos, float deltaTime)
        {
            Vector2 awayFromPlayer = enemy.Position - playerPos;
            float distance = awayFromPlayer.Length();

            if (distance > 0)
            {
                Vector2 direction = Vector2.Normalize(awayFromPlayer);
                enemy.Velocity += direction * RETREAT_ACCELERATION * deltaTime;

                // Limit velocity
                if (enemy.Velocity.Length() > enemy.Speed)
                {
                    enemy.Velocity = Vector2.Normalize(enemy.Velocity) * enemy.Speed;
                }
            }
        }

        /// <summary>
        /// Circling behavior - orbit around player
        /// </summary>
        private void UpdateCirclingBehavior(EnemyShip enemy, Vector2 playerPos, float deltaTime)
        {
            Vector2 toPlayer = playerPos - enemy.Position;
            float distance = toPlayer.Length();

            if (distance > 0)
            {
                // Calculate perpendicular direction for circling
                Vector2 perpendicular = new Vector2(-toPlayer.Y, toPlayer.X);
                perpendicular = Vector2.Normalize(perpendicular);

                // Combine perpendicular movement with slight inward/outward movement
                float radiusAdjustment = (distance - CIRCLING_RADIUS) * 0.5f;
                Vector2 radiusDirection = Vector2.Normalize(toPlayer) * radiusAdjustment;

                Vector2 circleDirection = perpendicular + radiusDirection * 0.3f;
                enemy.Velocity += circleDirection * enemy.Speed * deltaTime;

                // Limit velocity
                if (enemy.Velocity.Length() > enemy.Speed)
                {
                    enemy.Velocity = Vector2.Normalize(enemy.Velocity) * enemy.Speed;
                }
            }
        }

        /// <summary>
        /// Attack behavior - aggressive pursuit with shooting
        /// </summary>
        private void UpdateAttackBehavior(EnemyShip enemy, Player player, float deltaTime)
        {
            // Aggressive pursuit
            UpdatePursuitBehavior(enemy, player.Position, deltaTime);

            // Add some unpredictable movement
            if (_random.NextDouble() < 0.05) // 5% chance to juke
            {
                Vector2 randomDirection = new Vector2(
                    (float)(_random.NextDouble() - 0.5) * 2f,
                    (float)(_random.NextDouble() - 0.5) * 2f
                );
                randomDirection = Vector2.Normalize(randomDirection);
                enemy.Velocity += randomDirection * enemy.Speed * 0.5f * deltaTime;
            }
        }

        /// <summary>
        /// Formation flying behavior for destroyer type enemies
        /// </summary>
        public void UpdateFormationBehavior(EnemyShip enemy, float deltaTime)
        {
            if (enemy.FormationIndex < 0) return;

            Vector2 targetPosition = enemy.FormationPosition;
            Vector2 toTarget = targetPosition - enemy.Position;
            float distance = toTarget.Length();

            if (distance > 10f) // Move to formation position
            {
                Vector2 direction = Vector2.Normalize(toTarget);
                enemy.Velocity += direction * enemy.Speed * 0.8f * deltaTime;
            }
            else
            {
                // Maintain formation position with slight drift
                enemy.Velocity *= 0.95f;
            }
        }

        /// <summary>
        /// Intercept behavior - calculate intercept trajectory
        /// </summary>
        public void UpdateInterceptBehavior(EnemyShip enemy, Vector2 playerPos, Vector2 playerVel, float deltaTime)
        {
            Vector2 interceptPoint = CalculateInterceptPath(enemy.Position, playerPos, playerVel, enemy.Speed);
            
            Vector2 toIntercept = interceptPoint - enemy.Position;
            float distance = toIntercept.Length();

            if (distance > 0)
            {
                Vector2 direction = Vector2.Normalize(toIntercept);
                enemy.Velocity += direction * PURSUIT_ACCELERATION * deltaTime;

                // Limit velocity
                if (enemy.Velocity.Length() > enemy.Speed)
                {
                    enemy.Velocity = Vector2.Normalize(enemy.Velocity) * enemy.Speed;
                }
            }
        }

        /// <summary>
        /// Evading behavior - sophisticated evasion maneuvers
        /// </summary>
        private void UpdateEvadingBehavior(EnemyShip enemy, Vector2 playerPos, float deltaTime)
        {
            Vector2 awayFromPlayer = enemy.Position - playerPos;
            float distance = awayFromPlayer.Length();

            if (distance > 0)
            {
                // Combine retreat with perpendicular movement for evasion
                Vector2 retreatDirection = Vector2.Normalize(awayFromPlayer);
                Vector2 perpendicular = new Vector2(-awayFromPlayer.Y, awayFromPlayer.X);
                perpendicular = Vector2.Normalize(perpendicular);

                // Alternate perpendicular direction based on time for zigzag motion
                float zigzag = MathF.Sin(enemy.StateTimer * 4f);
                Vector2 evasionDirection = retreatDirection + perpendicular * zigzag * 0.7f;
                evasionDirection = Vector2.Normalize(evasionDirection);

                enemy.Velocity += evasionDirection * enemy.Speed * deltaTime;

                // Limit velocity
                if (enemy.Velocity.Length() > enemy.Speed)
                {
                    enemy.Velocity = Vector2.Normalize(enemy.Velocity) * enemy.Speed;
                }
            }
        }

        /// <summary>
        /// Calculate intercept trajectory to where player will be
        /// </summary>
        public Vector2 CalculateInterceptPath(Vector2 enemyPos, Vector2 playerPos, Vector2 playerVel, float enemySpeed)
        {
            // Predict where player will be based on current velocity
            Vector2 relativePosition = playerPos - enemyPos;
            float relativeDistance = relativePosition.Length();

            if (relativeDistance < 0.1f) return playerPos;

            // Calculate time to intercept using quadratic formula
            float a = Vector2.Dot(playerVel, playerVel) - (enemySpeed * enemySpeed);
            float b = 2f * Vector2.Dot(playerVel, relativePosition);
            float c = Vector2.Dot(relativePosition, relativePosition);

            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0 || Math.Abs(a) < 0.001f)
            {
                // No intercept solution or player is stationary, aim at current position
                return playerPos;
            }

            float timeToIntercept = (-b - MathF.Sqrt(discriminant)) / (2f * a);
            
            if (timeToIntercept < 0)
                timeToIntercept = (-b + MathF.Sqrt(discriminant)) / (2f * a);

            // Clamp intercept time to reasonable bounds
            timeToIntercept = Math.Max(0, Math.Min(timeToIntercept, INTERCEPT_PREDICTION_TIME));

            return playerPos + playerVel * timeToIntercept;
        }

        /// <summary>
        /// Apply type-specific AI behavior modifications
        /// </summary>
        private void ApplyTypeSpecificBehavior(EnemyShip enemy, Player player, float deltaTime)
        {
            switch (enemy.Type)
            {
                case EnemyType.Scout:
                    // Scouts are more erratic and change direction frequently
                    if (_random.NextDouble() < 0.08) // 8% chance per frame
                    {
                        Vector2 randomJuke = new Vector2(
                            (float)(_random.NextDouble() - 0.5) * 2f,
                            (float)(_random.NextDouble() - 0.5) * 2f
                        );
                        enemy.Velocity += Vector2.Normalize(randomJuke) * enemy.Speed * 0.3f * deltaTime;
                    }
                    break;

                case EnemyType.Hunter:
                    // Hunters are more persistent in pursuit
                    if (enemy.CurrentState == AIState.Pursuing)
                    {
                        Vector2 toPlayer = player.Position - enemy.Position;
                        if (toPlayer.Length() > 0)
                        {
                            Vector2 direction = Vector2.Normalize(toPlayer);
                            enemy.Velocity += direction * PURSUIT_ACCELERATION * 0.2f * deltaTime;
                        }
                    }
                    break;

                case EnemyType.Destroyer:
                    // Destroyers prefer formation flying when multiple are present
                    // This would be handled by the EnemyManager for coordination
                    break;

                case EnemyType.Interceptor:
                    // Interceptors prefer intercept behavior
                    if (enemy.CurrentState == AIState.Pursuing && _random.NextDouble() < 0.3)
                    {
                        TransitionToState(enemy, AIState.Intercepting);
                    }
                    break;
            }
        }

        /// <summary>
        /// Calculate formation positions for multiple enemies
        /// </summary>
        public void SetupFormation(List<EnemyShip> enemies, Vector2 centerPosition)
        {
            if (enemies == null || enemies.Count == 0) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Type == EnemyType.Destroyer)
                {
                    float angle = (float)(i * 2 * Math.PI / enemies.Count);
                    Vector2 offset = new Vector2(
                        MathF.Cos(angle) * FORMATION_SPACING,
                        MathF.Sin(angle) * FORMATION_SPACING
                    );
                    
                    enemies[i].FormationPosition = centerPosition + offset;
                    enemies[i].FormationIndex = i;
                    enemies[i].CurrentState = AIState.FormationFlying;
                }
            }
        }
    }
}