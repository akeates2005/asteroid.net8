## Analysis: Asteroid

### Overview
The `Asteroid.cs` file defines the `Asteroid` class, which represents an asteroid in the game. It handles the asteroid's movement, screen wrapping, and visual representation. Each asteroid has a size, a procedurally generated shape, and behavior that can be scaled with the game's difficulty level.

### Entry Points
- `Asteroid(Vector2 position, Vector2 velocity, AsteroidSize asteroidSize, Random random, int level)`: The constructor is the main entry point for creating an asteroid. It's called by `GameProgram` or `GameEnhancements` when new asteroids are needed.
- `Update()`: Called every frame by `GameProgram` to update the asteroid's position and behavior.
- `Draw()`: Called every frame by `GameProgram` to render the asteroid.

### Core Implementation

#### 1. Properties and Initialization (`Asteroid.cs:10-60`)
- **Properties:** `Position`, `Velocity`, `AsteroidSize`, `Radius`, and `Active`.
- **Constructor (`:40`):**
    - Sets the initial `Position`, `AsteroidSize`, and `Active` state.
    - Determines the `Radius` based on the `AsteroidSize` enum (`:48-58`).
    - Scales the initial `Velocity` based on the `level` parameter to increase difficulty (`:60`).
    - Sets up a timer (`_timer`, `_changeInterval`) for periodic velocity changes, also scaled by level (`:62-64`).
    - Creates a new `AsteroidShape` object to give the asteroid a unique, procedurally generated polygonal shape (`:66`).

#### 2. Update Logic (`Asteroid.cs:71-91`)
- `Update()` is called each frame.
- It decrements the `_timer` (`:74`).
- When `_timer` reaches zero, it changes the asteroid's `Velocity` to a new random vector, making its movement unpredictable (`:76-79`).
- It updates the `Position` by adding the `Velocity` (`:82`).
- It implements screen wrapping logic: if the asteroid goes off one side of the screen, it reappears on the opposite side (`:85-88`).

#### 3. Drawing Logic (`Asteroid.cs:96-108`)
- `Draw()` is called each frame.
- It iterates through the `Points` of its `_shape` object (`:101`).
- It draws lines between consecutive points (`Raylib.DrawLineV`) to render the asteroid's polygonal outline (`:104`). The color is sourced from `Theme.AsteroidColor`.

### Data Flow
1.  An `Asteroid` object is instantiated by `GameProgram` or another manager class, providing initial parameters like position, velocity, size, and level.
2.  The constructor calculates the asteroid's radius, scales its velocity, and generates its unique `AsteroidShape`.
3.  Every frame, `GameProgram`'s main loop calls the `Update()` method on the asteroid instance.
4.  `Update()` modifies the asteroid's `Position` based on its `Velocity`. It may also randomly change the `Velocity`.
5.  Every frame, `GameProgram`'s render loop calls the `Draw()` method.
6.  `Draw()` reads the asteroid's current `Position` and the `Points` from its `_shape` to draw it on the screen.

### Key Patterns
- **Procedural Generation:** The use of `AsteroidShape` to create a unique polygonal form for each asteroid is a form of procedural content generation.
- **State Machine (Simple):** The `_timer` acts as a simple state machine, switching the asteroid's behavior between "moving in a straight line" and "changing direction."
- **Dependency Injection:** The `Random` object is passed into the constructor, which is a form of dependency injection that allows for more predictable testing if a seeded `Random` instance is used.

### Configuration
- **Size and Radius:** The radius for each `AsteroidSize` is hardcoded in the constructor (`:48-58`).
- **Difficulty Scaling:** The `speedMultiplier` (`:60`) and `changeIntervalMultiplier` (`:62`) scale the asteroid's difficulty based on the `level` passed to the constructor.
- **Color:** The rendering color is taken from the static `Theme.AsteroidColor` (`:105`).

### Error Handling
- There is no explicit error handling in this class. It assumes that it is created with valid parameters and that its dependencies (like `Raylib` and `Theme`) are available.