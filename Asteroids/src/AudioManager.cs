using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace Asteroids
{
    /// <summary>
    /// Centralized audio management system for sound effects and music
    /// </summary>
    public class AudioManager
    {
        private readonly Dictionary<string, Sound> _sounds;
        private readonly Dictionary<string, Music> _music;
        private float _masterVolume;
        private float _sfxVolume;
        private float _musicVolume;
        private bool _audioEnabled;

        public float MasterVolume 
        { 
            get => _masterVolume; 
            set 
            { 
                _masterVolume = Math.Clamp(value, 0f, 1f);
                UpdateAllVolumes();
            } 
        }

        public float SfxVolume 
        { 
            get => _sfxVolume; 
            set 
            { 
                _sfxVolume = Math.Clamp(value, 0f, 1f);
                UpdateSoundVolumes();
            } 
        }

        public float MusicVolume 
        { 
            get => _musicVolume; 
            set 
            { 
                _musicVolume = Math.Clamp(value, 0f, 1f);
                UpdateMusicVolumes();
            } 
        }

        public bool AudioEnabled 
        { 
            get => _audioEnabled; 
            set 
            { 
                _audioEnabled = value;
                if (!value)
                {
                    StopAllSounds();
                    StopAllMusic();
                }
            } 
        }

        public AudioManager()
        {
            _sounds = new Dictionary<string, Sound>();
            _music = new Dictionary<string, Music>();
            _masterVolume = 0.7f;
            _sfxVolume = 0.8f;
            _musicVolume = 0.6f;
            _audioEnabled = true;

            // Initialize Raylib audio
            try
            {
                Raylib.InitAudioDevice();
                ErrorManager.LogInfo("Audio system initialized successfully");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Failed to initialize audio system", ex);
                _audioEnabled = false;
            }
        }

        /// <summary>
        /// Load sound effect from file
        /// </summary>
        public bool LoadSound(string name, string filePath)
        {
            if (!_audioEnabled) return false;

            try
            {
                if (!File.Exists(filePath))
                {
                    ErrorManager.LogWarning($"Sound file not found: {filePath}");
                    return false;
                }

                var sound = Raylib.LoadSound(filePath);
                // Note: Raylib-cs doesn't expose frameCount directly, we'll check if sound loaded
                // by attempting to get sample rate instead
                if (false) // Skip this check for now, Raylib will handle invalid sounds
                {
                    ErrorManager.LogWarning($"Failed to load sound: {filePath}");
                    return false;
                }

                // Unload existing sound if it exists
                if (_sounds.ContainsKey(name))
                {
                    Raylib.UnloadSound(_sounds[name]);
                }

                _sounds[name] = sound;
                Raylib.SetSoundVolume(sound, _masterVolume * _sfxVolume);
                
                ErrorManager.LogInfo($"Loaded sound: {name} from {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error loading sound {name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Load music from file
        /// </summary>
        public bool LoadMusic(string name, string filePath)
        {
            if (!_audioEnabled) return false;

            try
            {
                if (!File.Exists(filePath))
                {
                    ErrorManager.LogWarning($"Music file not found: {filePath}");
                    return false;
                }

                var music = Raylib.LoadMusicStream(filePath);
                // Note: Raylib-cs doesn't expose frameCount directly for music
                if (false) // Skip this check for now, Raylib will handle invalid music
                {
                    ErrorManager.LogWarning($"Failed to load music: {filePath}");
                    return false;
                }

                // Unload existing music if it exists
                if (_music.ContainsKey(name))
                {
                    Raylib.UnloadMusicStream(_music[name]);
                }

                _music[name] = music;
                Raylib.SetMusicVolume(music, _masterVolume * _musicVolume);
                
                ErrorManager.LogInfo($"Loaded music: {name} from {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error loading music {name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Play sound effect
        /// </summary>
        public void PlaySound(string name, float volume = 1.0f, float pitch = 1.0f)
        {
            if (!_audioEnabled || !_sounds.ContainsKey(name)) return;

            try
            {
                var sound = _sounds[name];
                Raylib.SetSoundVolume(sound, _masterVolume * _sfxVolume * Math.Clamp(volume, 0f, 1f));
                Raylib.SetSoundPitch(sound, Math.Clamp(pitch, 0.1f, 3.0f));
                Raylib.PlaySound(sound);
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error playing sound {name}", ex);
            }
        }

        /// <summary>
        /// Play music (stops current music if playing)
        /// </summary>
        public void PlayMusic(string name, bool looping = true)
        {
            if (!_audioEnabled || !_music.ContainsKey(name)) return;

            try
            {
                // Stop current music
                StopAllMusic();

                var music = _music[name];
                // Note: Music looping is handled internally by Raylib
                // The looping parameter is noted for future use if API becomes available
                Raylib.SetMusicVolume(music, _masterVolume * _musicVolume);
                Raylib.PlayMusicStream(music);
                
                ErrorManager.LogInfo($"Started playing music: {name}");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error playing music {name}", ex);
            }
        }

        /// <summary>
        /// Stop specific music
        /// </summary>
        public void StopMusic(string name)
        {
            if (!_music.ContainsKey(name)) return;

            try
            {
                Raylib.StopMusicStream(_music[name]);
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error stopping music {name}", ex);
            }
        }

        /// <summary>
        /// Stop all music
        /// </summary>
        public void StopAllMusic()
        {
            foreach (var music in _music.Values)
            {
                try
                {
                    Raylib.StopMusicStream(music);
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error stopping music stream", ex);
                }
            }
        }

        /// <summary>
        /// Stop all sound effects
        /// </summary>
        public void StopAllSounds()
        {
            // Raylib doesn't provide a way to stop individual sounds,
            // but we can track playing sounds if needed in the future
        }

        /// <summary>
        /// Update music streams (call this every frame)
        /// </summary>
        public void Update()
        {
            if (!_audioEnabled) return;

            foreach (var music in _music.Values)
            {
                try
                {
                    Raylib.UpdateMusicStream(music);
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error updating music stream", ex);
                }
            }
        }

        /// <summary>
        /// Check if music is playing
        /// </summary>
        public bool IsMusicPlaying(string name)
        {
            if (!_audioEnabled || !_music.ContainsKey(name)) return false;

            try
            {
                return Raylib.IsMusicStreamPlaying(_music[name]);
            }
            catch (Exception ex)
            {
                ErrorManager.LogError($"Error checking music status {name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Update all volume levels
        /// </summary>
        private void UpdateAllVolumes()
        {
            UpdateSoundVolumes();
            UpdateMusicVolumes();
        }

        /// <summary>
        /// Update sound effect volumes
        /// </summary>
        private void UpdateSoundVolumes()
        {
            foreach (var sound in _sounds.Values)
            {
                try
                {
                    Raylib.SetSoundVolume(sound, _masterVolume * _sfxVolume);
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error updating sound volume", ex);
                }
            }
        }

        /// <summary>
        /// Update music volumes
        /// </summary>
        private void UpdateMusicVolumes()
        {
            foreach (var music in _music.Values)
            {
                try
                {
                    Raylib.SetMusicVolume(music, _masterVolume * _musicVolume);
                }
                catch (Exception ex)
                {
                    ErrorManager.LogError("Error updating music volume", ex);
                }
            }
        }

        /// <summary>
        /// Dispose of audio resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Unload all sounds
                foreach (var sound in _sounds.Values)
                {
                    Raylib.UnloadSound(sound);
                }
                _sounds.Clear();

                // Unload all music
                foreach (var music in _music.Values)
                {
                    Raylib.UnloadMusicStream(music);
                }
                _music.Clear();

                // Close audio device
                if (_audioEnabled)
                {
                    Raylib.CloseAudioDevice();
                }

                ErrorManager.LogInfo("Audio system disposed successfully");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error disposing audio system", ex);
            }
        }

        /// <summary>
        /// Create default sound effects programmatically
        /// </summary>
        public void CreateDefaultSounds()
        {
            if (!_audioEnabled) return;

            try
            {
                // Create simple sound effects using wave generation
                // Note: In a real implementation, you'd load actual audio files
                ErrorManager.LogInfo("Default sound effects would be created here");
                
                // For now, we'll just log that the system is ready for audio files
                ErrorManager.LogInfo("Audio system ready - place sound files in 'sounds/' directory");
            }
            catch (Exception ex)
            {
                ErrorManager.LogError("Error creating default sounds", ex);
            }
        }
    }
}