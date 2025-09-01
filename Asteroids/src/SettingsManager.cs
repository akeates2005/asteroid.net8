using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Asteroids
{
    /// <summary>
    /// Game settings data structure matching the flat JSON structure
    /// </summary>
    public class GameSettings
    {
        [JsonPropertyName("graphics")]
        public BasicGraphicsSettings Graphics { get; set; } = new();

        [JsonPropertyName("graphics3D")]
        public Graphics3DSettings Graphics3D { get; set; } = new();

        [JsonPropertyName("audio")]
        public AudioSettings Audio { get; set; } = new();

        [JsonPropertyName("controls")]
        public ControlSettings Controls { get; set; } = new();

        [JsonPropertyName("gameplay")]
        public GameplaySettings Gameplay { get; set; } = new();
    }

    public class BasicGraphicsSettings
    {
        [JsonPropertyName("defaultRenderMode")]
        public string DefaultRenderMode { get; set; } = "auto";

        [JsonPropertyName("fullscreen")]
        public bool Fullscreen { get; set; } = false;

        [JsonPropertyName("vsync")]
        public bool VSync { get; set; } = true;

        [JsonPropertyName("showGrid")]
        public bool ShowGrid { get; set; } = true;

        [JsonPropertyName("showParticles")]
        public bool ShowParticles { get; set; } = true;

        [JsonPropertyName("showFPS")]
        public bool ShowFPS { get; set; } = false;

        [JsonPropertyName("qualityLevel")]
        public string QualityLevel { get; set; } = "balanced";

        [JsonPropertyName("maxLODLevel")]
        public int MaxLODLevel { get; set; } = 2;

        [JsonPropertyName("enableFrustumCulling")]
        public bool EnableFrustumCulling { get; set; } = true;

        [JsonPropertyName("enableBatching")]
        public bool EnableBatching { get; set; } = true;
    }

    public class Graphics3DSettings
    {
        [JsonPropertyName("enableAntiAliasing")]
        public bool EnableAntiAliasing { get; set; } = true;

        [JsonPropertyName("shadowQuality")]
        public string ShadowQuality { get; set; } = "medium";

        [JsonPropertyName("textureQuality")]
        public string TextureQuality { get; set; } = "high";

        [JsonPropertyName("camera")]
        public CameraSettings Camera { get; set; } = new();

        [JsonPropertyName("performance")]
        public PerformanceSettings Performance { get; set; } = new();
    }

    public class CameraSettings
    {
        [JsonPropertyName("fov")]
        public float FOV { get; set; } = 75.0f;

        [JsonPropertyName("nearPlane")]
        public float NearPlane { get; set; } = 0.1f;

        [JsonPropertyName("farPlane")]
        public float FarPlane { get; set; } = 1000.0f;

        [JsonPropertyName("smoothingSpeed")]
        public float SmoothingSpeed { get; set; } = 5.0f;

        [JsonPropertyName("enableShake")]
        public bool EnableShake { get; set; } = true;

        [JsonPropertyName("defaultMode")]
        public string DefaultMode { get; set; } = "followPlayer";
    }

    public class PerformanceSettings
    {
        [JsonPropertyName("targetFrameRate")]
        public float TargetFrameRate { get; set; } = 60.0f;

        [JsonPropertyName("adaptiveQuality")]
        public bool AdaptiveQuality { get; set; } = true;

        [JsonPropertyName("memoryLimit")]
        public int MemoryLimit { get; set; } = 67108864;

        [JsonPropertyName("autoAdjustLOD")]
        public bool AutoAdjustLOD { get; set; } = true;
    }

    public class AudioSettings
    {
        [JsonPropertyName("masterVolume")]
        public float MasterVolume { get; set; } = 0.7f;

        [JsonPropertyName("sfxVolume")]
        public float SfxVolume { get; set; } = 0.8f;

        [JsonPropertyName("musicVolume")]
        public float MusicVolume { get; set; } = 0.6f;

        [JsonPropertyName("audioEnabled")]
        public bool AudioEnabled { get; set; } = true;
    }

    public class ControlSettings
    {
        [JsonPropertyName("mouseControlEnabled")]
        public bool MouseControlEnabled { get; set; } = false;

        [JsonPropertyName("keyRepeatDelay")]
        public float KeyRepeatDelay { get; set; } = 0.1f;
    }

    public class GameplaySettings
    {
        [JsonPropertyName("difficulty")]
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

        [JsonPropertyName("showHints")]
        public bool ShowHints { get; set; } = true;

        [JsonPropertyName("pauseOnFocusLoss")]
        public bool PauseOnFocusLoss { get; set; } = true;
    }

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    /// <summary>
    /// Manages loading, saving, and applying game settings
    /// </summary>
    public class SettingsManager
    {
        private const string SettingsFileName = "settings.json";
        private readonly string _settingsPath;
        private GameSettings _currentSettings;

        public GameSettings Current => _currentSettings;

        public event EventHandler<GameSettings> SettingsChanged;

        public SettingsManager()
        {
            var settingsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            Directory.CreateDirectory(settingsDir);
            _settingsPath = Path.Combine(settingsDir, SettingsFileName);
            
            _currentSettings = new GameSettings();
            LoadSettings();
        }

        /// <summary>
        /// Load settings from file or create defaults
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(json);
                    
                    if (settings != null)
                    {
                        _currentSettings = settings;
                        ValidateSettings();
                        ErrorManager.LogInfo("Settings loaded successfully");
                    }
                    else
                    {
                        ErrorManager.LogWarning("Failed to deserialize settings, using defaults");
                        CreateDefaultSettings();
                    }
                }
                else
                {
                    ErrorManager.LogInfo("Settings file not found, creating defaults");
                    CreateDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error loading settings", ex);
                CreateDefaultSettings();
            }
        }

        /// <summary>
        /// Save current settings to file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(_currentSettings, options);
                File.WriteAllText(_settingsPath, json);
                
                ErrorManager.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error saving settings", ex);
            }
        }

        /// <summary>
        /// Update settings and notify listeners
        /// </summary>
        public void UpdateSettings(GameSettings newSettings)
        {
            if (newSettings == null) return;

            _currentSettings = newSettings;
            ValidateSettings();
            SaveSettings();
            
            SettingsChanged?.Invoke(this, _currentSettings);
        }

        /// <summary>
        /// Update specific graphics settings
        /// </summary>
        public void UpdateGraphicsSettings(BasicGraphicsSettings graphics)
        {
            if (graphics == null) return;

            _currentSettings.Graphics = graphics;
            ValidateSettings();
            SaveSettings();
            
            SettingsChanged?.Invoke(this, _currentSettings);
        }
        
        /// <summary>
        /// Get the current default render mode
        /// </summary>
        public string GetDefaultRenderMode()
        {
            return _currentSettings.Graphics.DefaultRenderMode ?? "auto";
        }

        /// <summary>
        /// Update the default render mode and save settings
        /// </summary>
        public void SetDefaultRenderMode(string renderMode)
        {
            if (string.IsNullOrEmpty(renderMode)) return;

            var validModes = new[] { "auto", "2D", "3D" };
            if (!validModes.Contains(renderMode.ToLowerInvariant()))
            {
                renderMode = "auto";
            }

            _currentSettings.Graphics.DefaultRenderMode = renderMode.ToLowerInvariant();
            ValidateSettings();
            SaveSettings();
            
            SettingsChanged?.Invoke(this, _currentSettings);
            ErrorManager.LogInfo($"Default render mode updated to: {renderMode}");
        }

        /// <summary>
        /// Update specific audio settings
        /// </summary>
        public void UpdateAudioSettings(AudioSettings audio)
        {
            if (audio == null) return;

            _currentSettings.Audio = audio;
            ValidateSettings();
            SaveSettings();
            
            SettingsChanged?.Invoke(this, _currentSettings);
        }

        /// <summary>
        /// Apply settings to game systems
        /// </summary>
        public void ApplySettings(AudioManager audioManager)
        {
            try
            {
                // Apply audio settings
                if (audioManager != null)
                {
                    audioManager.AudioEnabled = _currentSettings.Audio.AudioEnabled;
                    audioManager.MasterVolume = _currentSettings.Audio.MasterVolume;
                    audioManager.SfxVolume = _currentSettings.Audio.SfxVolume;
                    audioManager.MusicVolume = _currentSettings.Audio.MusicVolume;
                }

                // Graphics settings would be applied here
                // (fullscreen, vsync, etc.)

                ErrorManager.LogInfo("Settings applied to game systems");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error applying settings", ex);
            }
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            CreateDefaultSettings();
            SaveSettings();
            SettingsChanged?.Invoke(this, _currentSettings);
        }

        /// <summary>
        /// Create default settings
        /// </summary>
        private void CreateDefaultSettings()
        {
            _currentSettings = new GameSettings();
            ValidateSettings();
        }

        /// <summary>
        /// Validate and clamp setting values
        /// </summary>
        private void ValidateSettings()
        {
            // Validate audio settings
            _currentSettings.Audio.MasterVolume = Math.Clamp(_currentSettings.Audio.MasterVolume, 0f, 1f);
            _currentSettings.Audio.SfxVolume = Math.Clamp(_currentSettings.Audio.SfxVolume, 0f, 1f);
            _currentSettings.Audio.MusicVolume = Math.Clamp(_currentSettings.Audio.MusicVolume, 0f, 1f);

            // Validate control settings
            _currentSettings.Controls.KeyRepeatDelay = Math.Clamp(_currentSettings.Controls.KeyRepeatDelay, 0.01f, 1f);

            // Validate difficulty
            if (!Enum.IsDefined(typeof(DifficultyLevel), _currentSettings.Gameplay.Difficulty))
            {
                _currentSettings.Gameplay.Difficulty = DifficultyLevel.Normal;
            }
        }

        /// <summary>
        /// Get difficulty multipliers
        /// </summary>
        public (float speed, float count, float score) GetDifficultyMultipliers()
        {
            return _currentSettings.Gameplay.Difficulty switch
            {
                DifficultyLevel.Easy => (0.8f, 0.8f, 0.5f),
                DifficultyLevel.Normal => (1.0f, 1.0f, 1.0f),
                DifficultyLevel.Hard => (1.3f, 1.2f, 1.5f),
                DifficultyLevel.Expert => (1.6f, 1.5f, 2.0f),
                _ => (1.0f, 1.0f, 1.0f)
            };
        }
    }
}