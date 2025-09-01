## Analysis: Bullet

### Overview
The `Bullet.cs` file defines a simple `Bullet` class. This class represents a projectile fired by the player. It manages its own position, velocity, and active state, and is responsible for deactivating itself when it goes off-screen.

### Entry Points
- `Bullet(Vector2 position, Vector2 velocity)`: The constructor, used to create a new bullet instance. It's likely called when the player fires.
- `Update()`: Called every frame, presumably by `GameProgram`, to move the bullet.
- `Draw()`: Called every frame to render the bullet.

### Core Implementation

#### 1. Properties and Initialization (`Bullet.cs:8-26`)
- **Properties:** `Position`, `Velocity`, and `Active`.
- **Constructor (`:20`):**
    - Takes an initial `position` and `velocity` as parameters.
    - Sets `Active` to `true` upon creation.

#### 2. Update Logic (`Bullet.cs:31-42`)
- `Update()` is called each frame.
- It updates the `Position` by adding the `Velocity` (`:34`).
- It checks if the bullet has gone off-screen by comparing its `Position` to the screen dimensions obtained from `Raylib.GetScreenWidth()` and `Raylib.GetScreenHeight()` (`:37-38`).
- If the bullet is off-screen, it sets its `Active` property to `false` (`:40`).

#### 3. Drawing Logic (`Bullet.cs:47-54`)
- `Draw()` is called each frame.
- It checks if the bullet is `Active` before drawing (`:49`).
- It renders the bullet as a small circle using `Raylib.DrawCircle` (`:51`). The color is sourced from the static `Theme.BulletColor`.

### Data Flow
1.  A `Bullet` object is instantiated, likely by the `Player` or `GameProgram` when the fire button is pressed. Its initial position and velocity are set.
2.  The new `Bullet` object is added to a list of active bullets managed by `GameProgram`.
3.  Every frame, `GameProgram` iterates through its list of active bullets and calls the `Update()` method on each one.
4.  The `Update()` method moves the bullet. If it moves off-screen, it marks itself as inactive by setting `Active = false`.
5.  In the same loop, `GameProgram` calls the `Draw()` method for each active bullet.
6.  `GameProgram` is responsible for cleaning up inactive bullets from its list.

### Key Patterns
- **Self-Management/Autonomous Object:** The bullet manages its own lifecycle to a degree. It knows when it should be deactivated (when it goes off-screen) and updates its own state accordingly. The external system (`GameProgram`) is then responsible for acting on that state change (i.e., removing it from the game).

### Configuration
- **Color:** The rendering color is taken from the static `Theme.BulletColor` (`:51`).
- **Size:** The bullet's radius is hardcoded to `2` in the `Draw()` method (`:51`).
- **Screen Boundaries:** The screen dimensions are retrieved directly from Raylib.

### Error Handling
- This class has no explicit error handling. It's a simple data-like object whose logic is straightforward. It assumes it will be managed correctly by an external system.

**Note:** This class is likely superseded or used in conjunction with `PooledBullet` and `BulletPool.cs` for better performance, as creating new objects frequently in a game loop can lead to garbage collection issues. The `BulletPool.cs` file suggests a more advanced implementation exists.