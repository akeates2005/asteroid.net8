## Analysis: EnemyShip

### Overview
`EnemyShip.cs` defines the `EnemyShip` entity, which represents an individual AI-controlled opponent in the game. The class encapsulates all the properties of an enemy, including its type, health, AI state, and movement physics. It also implements the `IGameEntity` and `ICollidable` interfaces, allowing it to be managed by the game engine and participate in collision detection.

### Entry Points
- `EnemyShip(EnemyType type, Vector2 position)`: The constructor is the main entry point for creating an enemy ship. It's called by `EnemyManager` when spawning a new enemy.
- `Update(float deltaTime)`: Called by `EnemyManager` every frame to update the enemy's physics, animations, and timers.
- `Render(IRenderer renderer)`: Called by `EnemyManager` to draw the enemy ship using a renderer.
- `TakeDamage(float damage)`: Public method to apply damage to the ship.
- `OnCollision(ICollidable other)`: Handles the collision response when a collision is detected.

### Core Implementation

#### 1. Properties and Initialization (`EnemyShip.cs:8-60`)
- **Interfaces:** Implements `IGameEntity` and `ICollidable`.
- **Properties:** Contains a comprehensive set of properties for an enemy, including `Position`, `Velocity`, `Health`, `Type`, `CurrentState`, `Size`, `AttackRange`, etc.
- **Constructor (`:53`):**
    - Assigns a unique `Id`.
    - Sets the `Type` and `Position`.
    - Calls `InitializeByType()` to set the specific properties for the given `EnemyType`.
- **`InitializeByType()` (`:65-106`):**
    - A `switch` statement sets the `MaxHealth`, `Speed`, `TurnRate`, `AttackRange`, `Size`, `Color`, etc., based on the `EnemyType`. This is where the different enemy archetypes (Scout, Hunter, etc.) are defined.

#### 2. Update and Physics (`EnemyShip.cs:111-141`)
- `Update(float deltaTime)`:
    - Updates timers for AI state and damage flashes.
    - Updates the ship's `Position` based on its `Velocity` (`:122`). The `Velocity` itself is controlled by the `EnemyAI` class.
    - Calls `WrapAroundScreen()` to handle screen wrapping (`:123`).
    - Smoothly rotates the ship to face the direction of its `Velocity` (`:126-137`).
    - Applies a damping factor to the velocity to simulate friction (`:140`).

#### 3. Damage and Combat (`EnemyShip.cs:146-165`)
- `TakeDamage(float damage)`:
    - Reduces the `Health`.
    - Sets a `DamageFlashTimer` to trigger a visual effect.
    - If `Health` drops to or below zero, it sets `Active = false`, marking it for removal by the `EnemyManager`.
- `CanShoot()`: Checks if the `ShotCooldown` has passed since the `LastShotTime`.
- `RecordShot()`: Resets the shot timer.

#### 4. Interface Implementations (`EnemyShip.cs:180-240`)
- `IsCollidingWith(...)`: Implements collision detection logic using a simple radius check.
- `Render(IRenderer renderer)`: Delegates the rendering of the enemy to the provided `IRenderer` instance, passing all necessary state information (position, rotation, type, color, etc.).
- `OnCollision(ICollidable other)`: Defines a basic collision response, causing the enemy to take damage if it collides with a `Player` or a `Bullet`.

### Data Flow
1.  `EnemyManager` instantiates an `EnemyShip`, providing a `type` and `position`. The constructor then calls `InitializeByType` to configure the ship's specific stats.
2.  On each frame, `EnemyManager` calls `EnemyAI.UpdateAIState()`, which reads the enemy's state (and the player's state) and modifies the enemy's `Velocity`.
3.  `EnemyManager` then calls `enemyShip.Update()`, which uses the new `Velocity` to update the ship's `Position` and `Rotation`.
4.  `EnemyManager` calls `enemyShip.Render()`, which passes the ship's data to the `IRenderer` to be drawn.
5.  During collision checks, `EnemyManager` calls `enemyShip.IsCollidingWith()`. If a collision occurs, `OnCollision` might be called, or `TakeDamage` is called directly by the manager.
6.  `TakeDamage` reduces health. If health is depleted, `Active` is set to `false`.
7.  `EnemyManager` detects that the ship is no longer active and removes it from the game.

### Key Patterns
- **Entity Component System (loosely):** While not a full ECS, the design separates concerns. The `EnemyShip` class holds the data (component), the `EnemyAI` class provides the behavior logic (system), and `EnemyManager` orchestrates the updates.
- **State Pattern:** The `AIState` enum is a key property that is managed by the `EnemyAI` but stored on the `EnemyShip`, dictating its behavior.
- **Strategy Pattern:** The `EnemyType` enum acts as a selector for a "strategy". The `InitializeByType` method applies a different set of base stats (the strategy) depending on the type.
- **Interface-based Design:** By implementing `IGameEntity` and `ICollidable`, the `EnemyShip` can be treated generically by systems like the `SpatialGrid` and the main game loop, promoting loose coupling.

### Configuration
- All enemy stats (health, speed, size, etc.) are hardcoded within the `InitializeByType` method (`:65-106`). To change an enemy's stats, this method must be edited.

### Error Handling
- The class itself has no explicit error handling. It relies on its manager (`EnemyManager`) to handle `null` renderers or other issues.