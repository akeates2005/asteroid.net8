## Analysis: EnemyAI

### Overview
`EnemyAI.cs` implements the artificial intelligence for enemy ships. It provides a set of sophisticated behaviors, including pursuing, retreating, circling, and intercepting the player. The AI's behavior is state-driven, with transitions based on the enemy's distance to the player and other factors. It also includes logic for formation flying and type-specific behaviors.

### Entry Points
- `EnemyAI()`: The constructor for the AI system.
- `UpdateAIState(EnemyShip enemy, Player player, float deltaTime)`: The main update loop for the AI. This is the primary entry point, called by `EnemyManager` to update an individual enemy's state and velocity.
- `SetupFormation(List<EnemyShip> enemies, Vector2 centerPosition)`: A method to arrange a group of "Destroyer" type enemies into a formation.

### Core Implementation

#### 1. State-Driven Behavior (`EnemyAI.cs:26-63`)
- The core of the AI is the `UpdateAIState` method.
- It first calls `UpdateStateTransitions` (`:30`) to decide if the enemy should change its current behavior (`AIState`).
- **State Transitions (`:68-103`):**
    - Transitions are primarily based on the `distanceToPlayer`.
    - If far away, transition to `Pursuing`.
    - If too close, transition to `Retreating`.
    - If in attack range, randomly choose between `Attacking` and `Circling`.
    - A timer (`StateTimer`) also triggers periodic, random state changes to make the AI less predictable (`:94-103`).
- After determining the state, a `switch` statement (`:33-62`) calls the appropriate behavior update method (e.g., `UpdatePursuitBehavior`, `UpdateRetreatBehavior`).

#### 2. AI Behaviors (`EnemyAI.cs:110-258`)
- Each `Update...Behavior` method modifies the `enemy.Velocity` property to achieve the desired movement.
- `UpdatePursuitBehavior`: Accelerates the enemy towards the player's position.
- `UpdateRetreatBehavior`: Accelerates the enemy away from the player's position.
- `UpdateCirclingBehavior`: Calculates a perpendicular vector to the player and applies force in that direction to orbit the player.
- `UpdateAttackBehavior`: A more aggressive version of pursuit, with random "juke" movements.
- `UpdateInterceptBehavior`: Calls `CalculateInterceptPath` to predict the player's future position and accelerates towards that point.
- `UpdateEvadingBehavior`: A more complex retreat that combines moving away from the player with a perpendicular, zigzag motion.

#### 3. Intercept Calculation (`EnemyAI.cs:263-295`)
- `CalculateInterceptPath(...)` is a key algorithm in this class.
- It uses a quadratic formula to solve for the time it will take for a bullet traveling at `enemySpeed` to intercept a player moving at `playerVel`.
- This allows the AI to "lead" its shots, aiming where the player *will be* rather than where the player *is*.
- The prediction time is clamped to a maximum (`INTERCEPT_PREDICTION_TIME`) to prevent unrealistic long-range predictions.

#### 4. Type-Specific and Formation Behavior (`EnemyAI.cs:300-356`)
- `ApplyTypeSpecificBehavior`: Modifies the base AI behavior depending on the `enemy.Type`. For example, `Scout` types are made more erratic, while `Hunter` types are more persistent.
- `SetupFormation`: This method is called by `EnemyManager` to coordinate multiple `Destroyer` enemies. It calculates a position for each enemy in a circular formation and sets their state to `FormationFlying`.

### Data Flow
1.  `EnemyManager` calls `EnemyAI.UpdateAIState(enemy, player, ...)` for each active enemy ship.
2.  `UpdateAIState` reads the `enemy` and `player` positions to calculate the distance between them.
3.  Based on the distance and timers, `UpdateStateTransitions` may change the `enemy.CurrentState`.
4.  The appropriate `Update...Behavior` method is called based on the `enemy.CurrentState`.
5.  This behavior method calculates a new `enemy.Velocity`.
6.  The `EnemyShip.Update()` method (called separately by `EnemyManager`) then uses this new `Velocity` to update the enemy's `Position`.
7.  When an enemy is in the `Attacking` state, `EnemyManager` will call `CalculateInterceptPath` to determine where to fire a bullet.

### Key Patterns
- **State Pattern:** The `AIState` enum and the `switch` statement in `UpdateAIState` form a classic state pattern, where the object's behavior changes based on its internal state.
- **Strategy Pattern:** Each `Update...Behavior` method can be seen as a different movement strategy. The state pattern is used to select which strategy to apply.
- **Predictive AI:** The `CalculateInterceptPath` function is a good example of predictive AI, making the enemies appear more intelligent than simple reactive AI.

### Configuration
- The AI's behavior is heavily configured by constants defined at the top of the file (`:11-16`), such as `PURSUIT_ACCELERATION`, `CIRCLING_RADIUS`, and `STATE_CHANGE_INTERVAL`.
- Enemy-specific properties like `AttackRange` and `RetreatDistance` are configured on the `EnemyShip` object itself.

### Error Handling
- The AI checks if the `player` object is `null` at the beginning of `UpdateAIState` to prevent null reference exceptions (`:26`).
- There is no other explicit error handling. The code assumes the `EnemyShip` object is valid.