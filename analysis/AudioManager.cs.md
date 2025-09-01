## Analysis: AudioManager

### Overview
`AudioManager.cs` provides a centralized system for managing all audio in the game, including sound effects and music. It handles loading, playing, and stopping audio, as well as managing volume levels. It acts as a wrapper around the `Raylib.InitAudioDevice` and related audio functions.

### Entry Points
- `AudioManager()`: The constructor initializes the audio device.
- `LoadSound(string name, string filePath)`: Loads a sound effect from a file and stores it in a dictionary with a given name.
- `LoadMusic(string name, string filePath)`: Loads a music stream from a file.
- `PlaySound(string name, float volume = 1.0f, float pitch = 1.0f)`: Plays a loaded sound effect.
- `PlayMusic(string name, bool looping = true)`: Plays a loaded music stream.
- `Update()`: Must be called every frame to update music streams.
- `Dispose()`: Unloads all sounds and music and closes the audio device.

### Core Implementation

#### 1. State and Initialization (`AudioManager.cs:11-71`)
- **State:**
    - `_sounds`: A `Dictionary<string, Sound>` to store loaded sound effects (`:13`).
    - `_music`: A `Dictionary<string, Music>` to store loaded music streams (`:14`).
    - Volume levels: `_masterVolume`, `_sfxVolume`, `_musicVolume` (`:15-17`).
    - `_audioEnabled`: A boolean to globally enable or disable audio (`:18`).
- **Constructor (`:49`):**
    - Initializes the dictionaries and default volume levels.
    - Calls `Raylib.InitAudioDevice()` to initialize the audio system (`:55`). This is wrapped in a `try-catch` block to handle potential initialization failures.

#### 2. Loading Audio (`AudioManager.cs:76-151`)
- `LoadSound(...)` at `:76`:
    - Checks if the file exists.
    - Calls `Raylib.LoadSound(filePath)`.
    - If a sound with the same `name` already exists, it unloads the old one first.
    - Stores the new `Sound` in the `_sounds` dictionary.
    - Sets the initial volume for the sound.
- `LoadMusic(...)` at `:114`:
    - Similar logic to `LoadSound`, but uses `Raylib.LoadMusicStream(filePath)` and the `_music` dictionary.

#### 3. Playback Control (`AudioManager.cs:156-220`)
- `PlaySound(...)` at `:156`:
    - Retrieves the `Sound` from the `_sounds` dictionary.
    - Sets its volume and pitch.
    - Calls `Raylib.PlaySound(sound)`.
- `PlayMusic(...)` at `:180`:
    - Stops any currently playing music.
    - Retrieves the `Music` stream from the `_music` dictionary.
    - Calls `Raylib.PlayMusicStream(music)`.
- `StopMusic(...)`, `StopAllMusic()`, `StopAllSounds()` provide methods to stop audio playback.

#### 4. Volume Management (`AudioManager.cs:19-47`, `:268-309`)
- Public properties (`MasterVolume`, `SfxVolume`, `MusicVolume`) with setters that clamp the value between 0 and 1.
- The setters call private `Update...Volumes()` methods.
- `UpdateSoundVolumes()` and `UpdateMusicVolumes()` iterate through the loaded sounds and music, respectively, and call `Raylib.SetSoundVolume` or `Raylib.SetMusicVolume` to apply the new calculated volume (`_masterVolume * _sfxVolume`).

#### 5. Lifecycle and Maintenance (`AudioManager.cs:225-263`, `:314-349`)
- `Update()` at `:225`:
    - Must be called each frame.
    - It iterates through the `_music` dictionary and calls `Raylib.UpdateMusicStream` for each stream, which is required by Raylib for music streaming.
- `Dispose()` at `:314`:
    - Unloads all sounds and music streams to free memory.
    - Calls `Raylib.CloseAudioDevice()` to shut down the audio system.

### Data Flow
1.  `GameProgram` creates an `AudioManager` instance during initialization.
2.  The `AudioManager` constructor calls `Raylib.InitAudioDevice()`.
3.  `GameProgram` (or another manager) calls `LoadSound` and `LoadMusic` to load all necessary audio assets, which are stored in dictionaries.
4.  During gameplay, an event (e.g., player shooting) triggers a call to `AudioManager.PlaySound("shoot")`.
5.  `PlaySound` looks up the "shoot" sound in the `_sounds` dictionary and calls the appropriate Raylib function to play it.
6.  `GameProgram` calls `AudioManager.Update()` in its main game loop to keep music streams playing correctly.
7.  When the game closes, `GameProgram` calls `AudioManager.Dispose()` to release all audio resources.

### Key Patterns
- **Manager/Service Class:** `AudioManager` acts as a central service for all audio-related operations, encapsulating the underlying Raylib audio functions.
- **Dictionary/Map:** Dictionaries are used to store and retrieve loaded audio assets by a string key, which is more convenient than passing around file paths or `Sound`/`Music` objects.
- **Facade Pattern:** The `AudioManager` provides a simplified interface for the more complex Raylib audio system.

### Configuration
- **File Paths:** Audio file paths are provided when calling `LoadSound` or `LoadMusic`.
- **Volume:** Volume levels can be adjusted at runtime via the public `MasterVolume`, `SfxVolume`, and `MusicVolume` properties.
- **Global Toggle:** The `AudioEnabled` property can be used to mute/unmute all audio.

### Error Handling
- The constructor wraps `Raylib.InitAudioDevice()` in a `try-catch` block and sets `_audioEnabled` to `false` if it fails (`:57-61`).
- Loading and playback methods are wrapped in `try-catch` blocks to log errors using `ErrorManager` without crashing the game.
- `LoadSound` and `LoadMusic` check for file existence before attempting to load (`:80`, `:118`).