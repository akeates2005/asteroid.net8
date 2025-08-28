# Lives System Improvement Plan

## Problem Analysis

### Current Implementation Issues

The Asteroids game currently has a **broken lives system** with the following critical problems:

1. **No Lives Tracking**: The game does not maintain a proper lives counter
   - `GameProgram.cs:593` shows hardcoded `int lives = 3; // Simplified - could be tracked properly`
   - No actual lives field in the game state

2. **Immediate Game Over**: When player-asteroid collision occurs without shield:
   - Line `GameProgram.cs:415` immediately sets `_gameOver = true`
   - No check for remaining lives before ending game
   - Player should respawn if lives remain

3. **No Lives Decrement Logic**: No mechanism to decrease lives on player death

4. **No Player Respawn**: No system to respawn the player after losing a life

### Key Code Locations

**Collision Detection** (`GameProgram.cs:388-429`):
```csharp
// Player-asteroid collisions using spatial partitioning
if (distance <= _player.Size / 2 + asteroid.Radius)
{
    if (_player.IsShieldActive)
    {
        // Shield protects - asteroid destroyed
    }
    else
    {
        _gameOver = true; // ❌ PROBLEM: Immediate game over
        // Should check lives first!
    }
}
```

**HUD Display** (`AnimatedHUD.cs:337-350`):
- DrawLives() method exists but receives hardcoded value
- Visual system ready for proper lives display

**Game State** (`GameProgram.cs:48-52`):
- Has `_gameOver`, `_levelComplete`, `_gamePaused` flags
- Missing `_lives` field

## Improvement Plan

### Phase 1: Add Lives System Infrastructure

#### 1.1 Add Lives Field to GameProgram.cs
```csharp
// Add to field declarations around line 48
private int _lives;
private const int STARTING_LIVES = 3;
private bool _playerRespawning = false;
private float _respawnTimer = 0f;
private const float RESPAWN_DELAY = 2.0f; // 2 seconds respawn delay
```

#### 1.2 Update GameConstants.cs
```csharp
// Add to GameConstants.cs
public const int STARTING_LIVES = 3;
public const float RESPAWN_DELAY = 2.0f;
public const float RESPAWN_INVULNERABILITY_TIME = 3.0f;
```

### Phase 2: Implement Lives Logic

#### 2.1 Initialize Lives System
Update `ResetGame()` method in `GameProgram.cs`:
```csharp
private void ResetGame()
{
    _score = 0;
    _level = 1;
    _gameOver = false;
    _levelComplete = false;
    _gamePaused = false;
    _lives = STARTING_LIVES; // ✅ Initialize lives
    _playerRespawning = false;
    _respawnTimer = 0f;
    
    // Reset player state...
}
```

#### 2.2 Add Player Death Handler
Create new method in `GameProgram.cs`:
```csharp
private void HandlePlayerDeath()
{
    _lives--;
    
    if (_lives <= 0)
    {
        _gameOver = true;
        _visualEffects?.OnGameOver();
        if (_audioManager != null) _audioManager.PlaySound("explosion", 1.0f);
    }
    else
    {
        // Start respawn sequence
        _playerRespawning = true;
        _respawnTimer = RESPAWN_DELAY;
        
        // Visual/audio feedback for life lost
        _visualEffects?.OnPlayerDeath();
        if (_audioManager != null) _audioManager.PlaySound("playerDeath", 0.8f);
        
        // Hide player temporarily
        _player = null;
    }
    
    // Always create explosion at death location
    CreateExplosionAt(_player?.Position ?? Vector2.Zero);
    
    // Add camera shake
    if (Renderer3DIntegration.Is3DEnabled)
    {
        Renderer3DIntegration.AddCameraShake(5f, 1f);
    }
}
```

### Phase 3: Update Collision Handling

#### 3.1 Modify Player-Asteroid Collision
Update collision detection in `CheckCollisions()` method:
```csharp
// Replace lines 414-425 in GameProgram.cs
if (distance <= _player.Size / 2 + asteroid.Radius)
{
    if (_player.IsShieldActive)
    {
        // Shield collision - destroy asteroid
        asteroid.Active = false;
        CreateExplosionAt(asteroid.Position);
        if (_audioManager != null) _audioManager.PlaySound("explosion", 0.6f);
        
        // Spawn power-up with chance
        if (_powerUpManager != null && _random != null && _random.Next(0, 100) < GameConstants.POWERUP_SPAWN_CHANCE)
        {
            var powerUpType = (PowerUpType)_random.Next(0, 5);
            _powerUpManager.SpawnPowerUp(asteroid.Position, powerUpType);
        }
    }
    else
    {
        // ✅ NEW: Handle player death properly
        HandlePlayerDeath();
    }
    break; // Player collision processed
}
```

### Phase 4: Implement Respawn System

#### 4.1 Add Respawn Logic to Update Loop
Update `UpdateGameLogic()` method:
```csharp
private void UpdateGameLogic()
{
    // Handle respawning player
    if (_playerRespawning)
    {
        _respawnTimer -= Raylib.GetFrameTime();
        if (_respawnTimer <= 0)
        {
            RespawnPlayer();
        }
        // Skip normal player update during respawn
        return;
    }
    
    if (_player == null) return; // No player during respawn
    
    // ... rest of existing update logic
}
```

#### 4.2 Add Respawn Method
```csharp
private void RespawnPlayer()
{
    // Find safe spawn location away from asteroids
    Vector2 spawnPosition = FindSafeSpawnLocation();
    
    // Recreate player
    _player = new Player(spawnPosition, GameConstants.PLAYER_SIZE);
    _playerEntity = new PlayerSpatialEntity(_player);
    
    // Add temporary invulnerability
    _player.IsShieldActive = true;
    _player.ShieldDuration = GameConstants.RESPAWN_INVULNERABILITY_TIME * GameConstants.TARGET_FPS;
    
    // Visual effects for respawn
    _visualEffects?.OnPlayerRespawn(spawnPosition);
    if (_audioManager != null) _audioManager.PlaySound("respawn", 0.7f);
    
    _playerRespawning = false;
}

private Vector2 FindSafeSpawnLocation()
{
    Vector2 center = new Vector2(GameConstants.SCREEN_WIDTH / 2, GameConstants.SCREEN_HEIGHT / 2);
    
    // Try center first
    if (IsLocationSafe(center, 100f)) return center;
    
    // Try quadrants if center not safe
    Vector2[] fallbackPositions = new Vector2[]
    {
        new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.25f),
        new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.25f),
        new Vector2(GameConstants.SCREEN_WIDTH * 0.25f, GameConstants.SCREEN_HEIGHT * 0.75f),
        new Vector2(GameConstants.SCREEN_WIDTH * 0.75f, GameConstants.SCREEN_HEIGHT * 0.75f),
    };
    
    foreach (var position in fallbackPositions)
    {
        if (IsLocationSafe(position, 80f)) return position;
    }
    
    // Last resort - return center anyway
    return center;
}

private bool IsLocationSafe(Vector2 position, float safeRadius)
{
    if (_asteroids == null) return true;
    
    foreach (var asteroid in _asteroids)
    {
        if (asteroid.Active)
        {
            float distance = Vector2.Distance(position, asteroid.Position);
            if (distance < safeRadius + asteroid.Radius)
            {
                return false;
            }
        }
    }
    return true;
}
```

### Phase 5: Update UI Display

#### 5.1 Update HUD Calls
Modify rendering in `GameProgram.cs` around line 593:
```csharp
// Replace hardcoded lives with actual lives count
if (_animatedHUD != null && _player != null)
{
    _animatedHUD.DrawHUD(_player, _level, _score, _lives); // ✅ Use actual lives
}
```

#### 5.2 Add Respawn Visual Feedback
Update rendering to show respawn countdown:
```csharp
// Add to Render() method after HUD rendering
if (_playerRespawning && _respawnTimer > 0)
{
    string respawnText = $"RESPAWNING IN {Math.Ceiling(_respawnTimer):F0}";
    Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), respawnText, 24, 1);
    Vector2 textPos = new Vector2(
        (GameConstants.SCREEN_WIDTH - textSize.X) / 2,
        GameConstants.SCREEN_HEIGHT / 2 + 50
    );
    Raylib.DrawTextEx(Raylib.GetFontDefault(), respawnText, textPos, 24, 1, DynamicTheme.GetTextColor());
}
```

### Phase 6: Enhanced Features (Optional)

#### 6.1 Lives Power-Up
Extend `PowerUpManager.cs` to handle Health power-ups:
```csharp
case PowerUpType.Health:
    if (_lives < MAX_LIVES) // Add max lives constant
    {
        _lives++;
        // Visual feedback for extra life gained
    }
    break;
```

#### 6.2 Difficulty Scaling
Reduce starting lives on higher levels:
```csharp
// In StartLevel() method
if (level > 5)
{
    // Harder levels start with fewer lives
    _lives = Math.Max(1, STARTING_LIVES - (level - 5));
}
```

## Implementation Order

1. **Phase 1**: Add lives infrastructure (fields, constants)
2. **Phase 2**: Implement basic lives decrement and game over logic  
3. **Phase 3**: Update collision handling to use new death handler
4. **Phase 4**: Implement respawn system with safe positioning
5. **Phase 5**: Update UI to show actual lives count
6. **Phase 6**: Add enhanced features (optional)

## Files to Modify

### Required Changes
- `src/GameProgram.cs` - Main lives logic implementation
- `src/GameConstants.cs` - Add lives-related constants

### Optional Enhancements  
- `src/PowerUpManager.cs` - Extra life power-up
- `src/AdvancedEffectsManager.cs` - Death/respawn effects

## Testing Strategy

1. **Collision Test**: Verify player loses life on asteroid collision (without shield)
2. **Lives Count Test**: Confirm lives decrease from 3→2→1→0
3. **Game Over Test**: Ensure game ends only when lives reach 0
4. **Respawn Test**: Verify player respawns in safe location
5. **UI Test**: Confirm HUD shows correct lives count
6. **Invulnerability Test**: Check temporary shield after respawn

## Risk Assessment

- **LOW RISK**: Changes are localized to collision handling and game state
- **MINIMAL BREAKING**: Existing shield/power-up systems unaffected  
- **BACKWARDS COMPATIBLE**: No changes to save system or external interfaces

## Expected Outcome

After implementation:
- ✅ Player collision with asteroid decrements lives instead of ending game
- ✅ Game continues until all lives are exhausted
- ✅ Player respawns safely with temporary invulnerability
- ✅ HUD displays actual lives count with ship icons
- ✅ Proper game flow: lives → death → respawn → continue → repeat until lives exhausted