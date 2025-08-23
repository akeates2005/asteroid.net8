# Audio Assets Directory

Place your audio files here for the enhanced Asteroids game.

## Required Audio Files:

### Sound Effects (.wav format recommended):
- `shoot.wav` - Bullet firing sound
- `explosion.wav` - Asteroid destruction sound
- `thrust.wav` - Player engine sound
- `powerup.wav` - Power-up collection sound
- `shield.wav` - Shield activation sound

### Background Music (.ogg format recommended):
- `background.ogg` - Main game background music

## Audio Specifications:
- **Format**: WAV for sound effects, OGG for music
- **Sample Rate**: 44.1kHz recommended
- **Bit Depth**: 16-bit minimum
- **Channels**: Mono or Stereo

## Volume Guidelines:
- Keep sound effects under 1 second duration
- Normalize audio levels to prevent clipping
- Test with different volume settings

## Note:
If audio files are not present, the game will run without sound effects but will log warnings about missing assets. The audio system is designed to gracefully handle missing files.