## Analysis: EnemyManager

### Overview
`EnemyManager.cs` is a high-level manager responsible for the entire lifecycle of enemy ships in the game. It handles spawning new enemies, coordinating their AI, managing collisions, and triggering their shooting behavior. It acts as a central point of control for all enemy-related game logic.

### Entry Points
- `EnemyManager(BulletPool bulletPool, ...)`: The constructor initializes the manager and its dependencies.
- `UpdateEnemies(Player player, float deltaTime, int currentLevel)`: The main update method, called every frame by `GameProgram`. It orchestrates all enemy updates, including AI, physics, and spawning.
- `HandleEnemyCollisions(Player player, IReadOnlyList<PooledBullet> bullets)`: The main entry point for collision detection involving enemies.
- `RenderEnemies(IRenderer renderer)`: Renders all active enemies.
- `ClearAllEnemies()`: Removes all enemies from the game.

### Core Implementation

#### 1. Spawning Logic (`EnemyManager.cs:160-248`)
- `UpdateEnemySpawning(...)` at `:160`:
    - Determines the `_maxEnemiesPerLevel` based on the `currentLevel`.
    - Checks if it's time to spawn a new wave based on `_spawnInterval` and the number of active enemies.
    - If it should spawn, it calls `SpawnEnemyWave`.
- `SpawnEnemyWave(int level)` at `:175`:
    - Calculates how many enemies to spawn in the current wave.
    - Calls `SpawnRandomEnemy` in a loop.
- `SpawnRandomEnemy(int level)` at `:187`:
    - Calls `DetermineEnemyType` to select an enemy type based on the level.
    - Calls `GetRandomSpawnPosition` to find a location just off-screen.
    - Creates a new `EnemyShip` instance and adds it to the `_activeEnemies` list.
- `DetermineEnemyType(int level)` at `:200`:
    - Implements the difficulty curve by controlling which enemy types are available at different levels and adjusting their spawn probabilities.

#### 2. Update and AI Coordination (`EnemyManager.cs:40-78`)
- `UpdateEnemies(...)`:
    - Iterates backwards through the `_activeEnemies` list.
    - Removes inactive (destroyed) enemies.
    - Calls `enemy.Update(deltaTime)` for physics and movement.
    - **Performance Optimization:** It uses a counter (`_aiUpdateCounter`) to update the AI only every `AI_UPDATE_FREQUENCY` frames (e.g., at 30Hz if the game runs at 60FPS), reducing the computational load (`:44-45`).
    - When the AI is updated, it calls `_enemyAI.UpdateAIState(...)`.
    - It also calls `HandleEnemyShooting` to make the enemy fire if appropriate.

#### 3. Collision and Destruction (`EnemyManager.cs:83-141`)
- `HandleEnemyCollisions(...)`: Iterates through active enemies and checks for collisions with the player and player bullets.
- `HandleEnemyPlayerCollision(...)` at `:100`:
    - If the player's shield is active, the enemy is destroyed.
    - Otherwise, both the enemy and player are affected (in the current code, the enemy is destroyed, and a sound is played).
- `HandleEnemyBulletCollision(...)` at `:119`:
    - The enemy takes damage via `enemy.TakeDamage()`.
    - The bullet is deactivated.
    - If the enemy's health drops to zero, `DestroyEnemy` is called.
- `DestroyEnemy(...)` at `:133`:
    - Sets the enemy's `Active` flag to `false`, ensuring it will be removed from the `_activeEnemies` list on the next update.
    - Triggers explosion particle effects and sounds.

#### 4. Shooting Logic (`EnemyManager.cs:146-158`)
- `HandleEnemyShooting(...)`:
    - Checks if the enemy is in the `Attacking` state and if its weapon is off cooldown (`enemy.CanShoot()`).
    - If it can shoot, it uses the `_enemyAI.CalculateInterceptPath` to get a predicted target position.
    - It then spawns a bullet from the `_enemyBulletPool` with the correct velocity to hit the predicted position.

### Data Flow
1.  `GameProgram` creates and holds an instance of `EnemyManager`.
2.  In the game loop, `GameProgram` calls `enemyManager.UpdateEnemies(...)`.
3.  `UpdateEnemies` handles spawning, calls `enemy.Update()` for physics, and calls `_enemyAI.UpdateAIState()` to update the enemy's behavior and velocity.
4.  `GameProgram` also calls `enemyManager.HandleEnemyCollisions(...)`, passing in the player and the list of active bullets.
5.  The collision handler checks for impacts and calls methods like `TakeDamage` on the `EnemyShip` or `DeactivateBullet` on the `BulletPool`.
6.  If an enemy is destroyed, `DestroyEnemy` is called, which triggers effects and marks the enemy for removal.
7.  Finally, `GameProgram` calls `enemyManager.RenderEnemies()` to draw the ships.

### Key Patterns
- **Manager Class:** This is a classic manager pattern. `EnemyManager` encapsulates all the logic related to a specific subsystem (enemies), keeping the main `GameProgram` class cleaner.
- **Object Pool (Dependency):** It depends on a `BulletPool` (passed in via the constructor) to handle enemy projectiles efficiently. This is a form of dependency injection.
- **AI Coordination:** The manager coordinates the high-level actions of the AI, such as when to update and when to shoot. It also orchestrates multi-enemy behaviors by calling `_enemyAI.SetupFormation` (`:75`).
- **Difficulty Scaling:** The logic in `UpdateEnemySpawning` and `DetermineEnemyType` creates a procedural difficulty curve that adapts to the player's progress through the levels.

### Configuration
- `AI_UPDATE_FREQUENCY`: A constant to control how often the AI logic is run (`:19`).
- `_maxEnemiesPerLevel` and `_spawnInterval`: Variables that control the rate and number of enemy spawns (`:22-23`).

### Error Handling
- The manager checks if `player` is `null` before using it (`:42`).
- It logs key events (initialization, spawning, destruction) using `ErrorManager`.
- It relies on the injected `BulletPool` and `AudioManager` to handle their own internal errors.