## Analysis: AnimatedHUD

### Overview
`AnimatedHUD.cs` creates a dynamic and animated Heads-Up Display (HUD) for the game. It manages the animation of HUD elements like the score, level, and shield meter, and also handles floating text for events like score increases. It uses a generic `AnimatedValue<T>` class to smoothly interpolate values over time with various easing functions.

### Entry Points
- `AnimatedHUD.SetScore(int newScore)`: Sets the target score, triggering a count-up animation.
- `AnimatedHUD.SetLevel(int newLevel)`: Sets the target level, triggering a bounce animation and a flash effect.
- `AnimatedHUD.SetShieldLevel(float shieldPercent)`: Sets the target shield level, triggering a slide animation.
- `AnimatedHUD.Update(float deltaTime)`: Updates all animated values and effects.
- `AnimatedHUD.DrawHUD(Player player, int level, int score, int lives)`: The main drawing method that renders the entire HUD.
- `AnimatedHUD.OnScoreIncrease(int amount, Vector2 position)`: Creates floating text for score changes.
- `AnimatedHUD.OnPlayerHit()`: Triggers a shake animation on HUD elements.

### Core Implementation

#### 1. AnimatedValue<T> Class (`AnimatedHUD.cs:15-113`)
- A generic class to animate values of type `int`, `float`, or `Vector2`.
- `SetTarget(T newTarget, ...)` at `:31` starts an animation from the `Current` value to the `newTarget`.
- `Update(float deltaTime)` at `:39`:
    - Calculates the animation `progress`.
    - Applies an easing function (e.g., `EaseOutQuad`, `EaseOutBounce`) based on the `AnimationType` (`:46-55`).
    - Calls `InterpolateValue` to update the `Current` value.
- `InterpolateValue(T from, T to, float t)` at `:58`:
    - Performs linear interpolation for `int`, `float`, and `Vector2` types.

#### 2. AnimatedHUD Class (`AnimatedHUD.cs:115-333`)
- **State Management:**
    - Holds `AnimatedValue<T>` instances for score, level, shield, and positions (`:122-126`).
    - Manages state variables for effects like `_scorePulse`, `_shieldCritical`, and `_levelFlash` (`:128-132`).
    - Contains a list of `FloatingText` objects (`:134`).
- **Update Logic (`:163-201`):**
    - `Update(float deltaTime)` calls `Update()` on all its `AnimatedValue` members.
    - It updates the state of pulse and flash effects based on game state (e.g., sets `_scorePulse` if the score is animating).
    - It updates and removes inactive `FloatingText` objects.
- **Drawing Logic (`:203-299`):**
    - `DrawHUD(...)` is the main entry point for rendering. It first updates the target values of the animated elements if they have changed.
    - It calls specialized private drawing methods:
        - `DrawAnimatedScore()`: Draws the score, applying the `_scorePulse` effect and a glow for high scores.
        - `DrawAnimatedLevel()`: Draws the level, applying the `_levelFlash` effect.
        - `DrawShieldMeter()`: Draws the shield bar, using `_animatedShield.Current` for the fill percentage and applying a flashing effect when critical.
        - `DrawLives()`: Draws player lives as small ship icons.
        - `DrawFloatingTexts()`: Renders all active floating text objects.
- **Event Handlers (`:317-328`):**
    - `OnScoreIncrease(...)` and `OnPlayerHit()` are public methods that trigger specific HUD animations and effects in response to game events.

#### 3. FloatingText Class (`AnimatedHUD.cs:335-371`)
- Represents a piece of text that floats up the screen and fades out.
- `Update(float deltaTime)` at `:353` changes the text's `Position` and decreases its `Lifespan`. It also updates the alpha of its `Color` to create the fade-out effect.
- `Draw()` at `:366` renders the text if it's active.

### Data Flow
1.  Game state changes (e.g., score increases) are communicated to the `AnimatedHUD` by calling methods like `SetScore()`.
2.  `SetScore()` calls `_animatedScore.SetTarget()`, which initiates an animation within the `AnimatedValue` object.
3.  On each frame, `GameProgram` calls `AnimatedHUD.Update()`.
4.  `AnimatedHUD.Update()` calls `_animatedScore.Update()`, which interpolates the `Current` score value towards the `Target`. It also updates internal effect states like `_scoreIncreasing`.
5.  `GameProgram` calls `AnimatedHUD.DrawHUD()`.
6.  `DrawHUD()` calls `DrawAnimatedScore()`.
7.  `DrawAnimatedScore()` reads the `_animatedScore.Current` value and the `_scorePulse` state to draw the score text with the appropriate size and color.
8.  Floating text follows a similar flow: `OnScoreIncrease()` creates a `FloatingText` object, `Update()` moves and fades it, and `DrawFloatingTexts()` renders it.

### Key Patterns
- **Observer Pattern (Implicit):** The `AnimatedHUD` observes the game state (score, level, etc.) and updates its display accordingly. The `Set...` methods are the notification mechanism.
- **Strategy Pattern:** The `AnimationType` enum in `AnimatedValue` allows the animation algorithm (the strategy) to be selected dynamically.
- **Composition:** The `AnimatedHUD` is composed of multiple `AnimatedValue` and `FloatingText` objects, delegating the animation and rendering logic to them.
- **State Pattern:** The boolean flags (`_scoreIncreasing`, `_shieldCritical`) and the `_levelFlash` float act as states that modify the drawing behavior in the `Draw...` methods.

### Configuration
- Animation durations and types can be configured when calling `SetTarget` on an `AnimatedValue`.
- HUD element positions are defined as private `Vector2` fields (`_scoreBasePosition`, etc.) at `:137-140`.
- Colors are sourced from `DynamicTheme`.

### Error Handling
- The `InterpolateValue` method in `AnimatedValue` has a fallback for unsupported types, simply returning the `to` value (`:79`).
- There is no other explicit error handling. The class assumes it will be provided with valid data.