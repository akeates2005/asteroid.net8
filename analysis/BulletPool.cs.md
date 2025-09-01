## Analysis: BulletPool

### Overview
`BulletPool.cs` provides a high-performance object pooling system specifically for bullets. It's designed to prevent the performance issues (stuttering from garbage collection) that can arise from frequently creating and destroying `Bullet` objects. It manages the entire lifecycle of `PooledBullet` objects, from spawning to deactivation and reuse.

### Entry Points
- `BulletPool(int maxBullets = 50)`: The constructor initializes the pool.
- `SpawnBullet(Vector2 position, Vector2 velocity)`: The primary method for getting a bullet from the pool and activating it.
- `Update()`: Updates all active bullets in the pool.
- `Draw()`: Renders all active bullets.
- `GetActiveBullets()`: Returns a read-only list of active bullets for collision detection.
- `DeactivateBullet(PooledBullet bullet)`: Marks a bullet as inactive, to be cleaned up on the next update.
- `Dispose()`: Clears the pool and releases resources.

### Core Implementation

#### 1. PooledBullet Class (`BulletPool.cs:168-238`)
- Implements the `IPoolable` interface, which requires a `Reset()` method.
- It's more advanced than the simple `Bullet` class. It includes a `TimeToLive` (TTL) property to ensure bullets are eventually removed even if they don't go off-screen.
- `Initialize(...)` at `:179`: Sets the bullet's state when it's rented from the pool.
- `Update()` at `:188`:
    - Moves the bullet.
    - Decrements its `TimeToLive`.
    - Deactivates itself (`Active = false`) if it goes off-screen OR its `TimeToLive` expires.
- `Draw()` at `:210`:
    - Renders the bullet.
    - Includes a fade-out effect where the bullet's alpha decreases as its `TimeToLive` gets low.
- `Reset()` at `:231`: Resets the bullet's state to default values when it's returned to the pool.

#### 2. BulletPool Class (`BulletPool.cs:10-166`)
- **State and Initialization:**
    - `_bulletPool`: An `ObjectPool<PooledBullet>` instance that holds the inactive, ready-to-use bullets (`:12`).
    - `_activeBullets`: A `List<PooledBullet>` that tracks the bullets currently in the game world (`:13`).
    - The constructor (`:20`) creates the `ObjectPool` and preloads it with a number of bullets for immediate use.
- **Spawning Logic (`:32-53`):**
    - `SpawnBullet(...)` is called to get a bullet.
    - It first checks if the active bullet limit (`_maxBullets`) has been reached.
    - It calls `_bulletPool.Rent()` to get a `PooledBullet` instance. This either retrieves an existing object from the pool or creates a new one if the pool is empty.
    - It calls the bullet's `Initialize()` method.
    - It adds the bullet to the `_activeBullets` list.
- **Update and Cleanup (`:58-78`):**
    - `Update()` iterates backwards through the `_activeBullets` list.
    - It calls `bullet.Update()` on each one.
    - If a bullet becomes inactive (`!bullet.Active`), it is removed from the `_activeBullets` list and returned to the object pool via `_bulletPool.Return(bullet)`. This is the core of the pooling pattern.
- **Drawing and Access (`:83-100`):**
    - `Draw()` iterates through the `_activeBullets` and calls `Draw()` on each.
    - `GetActiveBullets()` provides other systems (like collision detection) with access to the active bullets.

### Data Flow
1.  `GameProgram` creates a `BulletPool` instance during initialization.
2.  When the player fires, `GameProgram` calls `bulletPool.SpawnBullet(...)`.
3.  `SpawnBullet` rents a `PooledBullet` from the internal `ObjectPool`, initializes it, and adds it to the `_activeBullets` list.
4.  In the main game loop, `GameProgram` calls `bulletPool.Update()`.
5.  The `Update` method moves all active bullets. If a bullet deactivates itself (due to TTL or going off-screen), the `Update` method removes it from `_activeBullets` and returns it to the `_bulletPool` for reuse.
6.  `GameProgram` calls `bulletPool.Draw()` to render the bullets.
7.  `GameProgram` calls `bulletPool.GetActiveBullets()` and passes the list to the collision detection system.
8.  When a collision with a bullet is detected, the collision system calls `bulletPool.DeactivateBullet(bullet)`, which sets `Active = false`, ensuring it will be cleaned up on the next `Update()` call.

### Key Patterns
- **Object Pool Pattern:** This is the central pattern of the class. It uses a generic `ObjectPool<T>` to efficiently manage and reuse `PooledBullet` objects, which is critical for performance in games where many short-lived objects are created.
- **Manager/Service Class:** `BulletPool` acts as a manager for all bullet-related operations, abstracting away the details of object creation, destruction, and reuse.

### Configuration
- `maxBullets`: The maximum number of bullets allowed in the pool and in the game at one time, configured in the constructor (`:20`).
- `DEFAULT_TTL`: A constant defining the default lifespan of a bullet in frames (`:172`).

### Error Handling
- The class uses `ErrorManager.SafeExecute` to wrap most of its public methods, preventing exceptions from crashing the game and logging any errors that occur.
- `SpawnBullet` checks if the pool is full (`_activeBullets.Count >= _maxBullets`) before trying to spawn a new bullet (`:38`).
- It also checks if renting a bullet from the pool failed (`bullet == null`) (`:44`).