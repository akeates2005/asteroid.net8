## Analysis: DynamicTheme

### Overview
`DynamicTheme.cs` implements a theming system that changes the game's color palette based on the current level. It allows for smooth transitions between themes and provides methods to get colors for various game elements, which can also be modified by game state (e.g., player health, pulsing effects).

### Entry Points
- `DynamicTheme.UpdateLevel(int newLevel)`: The primary entry point to change the theme. It triggers a transition to the new level's color palette.
- `DynamicTheme.Update(float deltaTime)`: Called every frame to handle the smooth transition between color palettes.
- `DynamicTheme.GetPlayerColor(float healthPercent = 1.0f)`: Gets the current color for the player, optionally blended with a damage color.
- `DynamicTheme.GetAsteroidColor(AsteroidSize size)`: Gets the color for an asteroid, with intensity based on its size.
- Other `Get...Color()` methods: Provide access to the current theme's colors for various game elements (bullets, explosions, text, etc.).

### Core Implementation

#### 1. ColorPalette Class (`DynamicTheme.cs:7-41`)
- A simple data class that holds a set of `Color` properties for all themable game elements (e.g., `PlayerColor`, `AsteroidColor`, `BulletColor`).
- The constructor (`:24`) takes primary, secondary, and accent colors and derives other colors from them using a `BlendColors` helper method.

#### 2. DynamicTheme State and Initialization (`DynamicTheme.cs:43-53`)
- `_levelPalettes`: A `static Dictionary<int, ColorPalette>` that maps level numbers to specific `ColorPalette` instances (`:45`). This defines the themes for the entire game.
- `_currentLevel`, `_currentPalette`, `_previousPalette`: Store the state for the current theme and the theme being transitioned from.
- `_transitionProgress`, `_isTransitioning`: State variables that control the color transition animation.

#### 3. Theme Transition Logic (`DynamicTheme.cs:55-83`)
- `UpdateLevel(int newLevel)` at `:55`:
    - If the `newLevel` is different from the `_currentLevel`, it sets up a transition.
    - It stores the current palette in `_previousPalette`.
    - It finds the appropriate new palette for the `newLevel` by calling `GetPaletteForLevel`.
    - It resets `_transitionProgress` to 0 and sets `_isTransitioning` to `true`.
- `Update(float deltaTime)` at `:67`:
    - If `_isTransitioning` is true, it increments `_transitionProgress`.
    - When the transition is complete (`_transitionProgress >= 1.0f`), it sets `_isTransitioning` to `false`.
- `GetPaletteForLevel(int level)` at `:79`:
    - Finds the correct palette from `_levelPalettes` for a given level. It selects the palette with the highest key that is less than or equal to the current level, allowing themes to persist across multiple levels.

#### 4. Color Retrieval (`DynamicTheme.cs:92-169`)
- The public `Get...Color()` methods are the main interface for the rest of the game to get theme colors.
- Each method calls `GetCurrentColor(Color currentColor, Color previousColor)` at `:158`.
- `GetCurrentColor` checks if a transition is in progress.
    - If not, it simply returns the `currentColor` from the `_currentPalette`.
    - If it is, it calls `LerpColor` (`:165`) to linearly interpolate between the `previousColor` and `currentColor` based on `_transitionProgress`, creating the smooth transition effect.
- Some `Get...Color` methods have additional logic, e.g.:
    - `GetPlayerColor` blends the base color with a damage color based on `healthPercent`.
    - `GetAsteroidColor` blends the base color with a gray color based on the asteroid's `size`.

#### 5. Special Effects (`DynamicTheme.cs:200-220`)
- `GetPulsingColor(...)` and `GetFlashingColor(...)` are utility methods that take a base color and return a modified version based on time (`Raylib.GetTime()`), creating dynamic visual effects.

### Data Flow
1.  At the start of a level, `GameProgram` calls `DynamicTheme.UpdateLevel(level)`.
2.  This sets up a transition from the previous level's palette to the new one.
3.  Every frame, `GameProgram` calls `DynamicTheme.Update(deltaTime)`, which advances the transition animation.
4.  During the rendering process, various `Draw()` methods (e.g., `Player.Draw`, `Asteroid.Draw`) call the `DynamicTheme.Get...Color()` methods to get the color they should use.
5.  The `Get...Color()` method calculates the correct interpolated color if a transition is active, or returns the static color otherwise.
6.  The `Draw()` method then uses this returned color to render the object.

### Key Patterns
- **State Pattern:** The `DynamicTheme` class manages the state of the game's visual theme (`_currentPalette`, `_isTransitioning`, etc.) and changes its behavior (the colors it returns) based on this state.
- **Strategy Pattern:** The different `ColorPalette` instances can be seen as different strategies for coloring the game. `GetPaletteForLevel` selects the appropriate strategy based on the level.
- **Singleton (Static Class):** As a static class, `DynamicTheme` provides a global access point for color information, ensuring that all parts of the game use a consistent theme.

### Configuration
- The entire theme system is configured in the `_levelPalettes` dictionary (`:45`), where `ColorPalette` objects are mapped to level numbers. New themes can be added by inserting new entries into this dictionary.

### Error Handling
- There is no explicit error handling. The code assumes that a palette will always be found for a given level (it defaults to the highest available palette lower than the current level).