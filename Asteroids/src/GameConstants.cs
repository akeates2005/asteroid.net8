namespace Asteroids
{
    /// <summary>
    /// Centralized game constants to eliminate magic numbers
    /// </summary>
    public static class GameConstants
    {
        // Display Settings
        public const int SCREEN_WIDTH = 800;
        public const int SCREEN_HEIGHT = 600;
        public const int TARGET_FPS = 60;
        public const int GRID_SIZE = 20;

        // Player Settings
        public const float PLAYER_SIZE = 20f;
        public const float PLAYER_ROTATION_SPEED = 5f;
        public const float PLAYER_THRUST = 0.1f;
        public const float PLAYER_FRICTION = 0.99f;

        // Shield Settings
        public const float MAX_SHIELD_DURATION = 180f; // 3 seconds at 60 FPS
        public const float MAX_SHIELD_COOLDOWN = 300f; // 5 seconds at 60 FPS
        public const float SHIELD_RADIUS_MULTIPLIER = 1.5f;

        // Bullet Settings
        public const float BULLET_SPEED = 400f; // pixels per second
        public const float BULLET_SIZE = 2f; // radius
        public const float BULLET_RADIUS = 2f; // backward compatibility
        public const float BULLET_LIFETIME = 2.5f; // seconds
        public const int BULLET_SCORE_VALUE = 100;

        // Asteroid Settings
        public const float LARGE_ASTEROID_RADIUS = 40f;
        public const float MEDIUM_ASTEROID_RADIUS = 20f;
        public const float SMALL_ASTEROID_RADIUS = 10f;
        public const float ASTEROID_SPEED_MULTIPLIER = 0.2f;
        public const float ASTEROID_CHANGE_INTERVAL_MULTIPLIER = 0.1f;
        public const int MIN_ASTEROID_POINTS = 8;
        public const int MAX_ASTEROID_POINTS = 13;

        // Level Settings
        public const int BASE_ASTEROIDS_PER_LEVEL = 10;
        public const int ASTEROIDS_INCREMENT_PER_LEVEL = 2;

        // Lives Settings
        public const int STARTING_LIVES = 3;
        public const float RESPAWN_DELAY = 2.0f;
        public const float RESPAWN_INVULNERABILITY_TIME = 3.0f;

        // Particle Settings
        public const int EXPLOSION_PARTICLE_COUNT = 10;
        public const int ENGINE_PARTICLE_LIFESPAN = 20;
        public const int EXPLOSION_PARTICLE_LIFESPAN = 60;
        public const float PARTICLE_VELOCITY_RANGE = 4f;

        // Performance Settings
        public const int MAX_BULLETS = 100;
        public const int MAX_PARTICLES = 1000;
        public const int MAX_ASTEROIDS = 500;

        // Physics Settings
        public const float COLLISION_DETECTION_MARGIN = 1f;
        public const float VELOCITY_DAMPING = 0.999f;
        public const float MIN_VELOCITY_THRESHOLD = 0.01f;

        // File Settings
        public const string LEADERBOARD_FILENAME = "leaderboard.txt";
        public const string CONFIG_DIRECTORY = "config";
        public const string SAVE_DIRECTORY = "saves";

        // Audio Settings
        public const float DEFAULT_MASTER_VOLUME = 0.7f;
        public const float DEFAULT_SFX_VOLUME = 0.8f;
        public const float DEFAULT_MUSIC_VOLUME = 0.6f;

        // UI Settings
        public const int FONT_SIZE_SMALL = 16;
        public const int FONT_SIZE_MEDIUM = 20;
        public const int FONT_SIZE_LARGE = 40;
        public const int UI_PADDING = 10;
        public const int LEADERBOARD_MAX_ENTRIES = 10;

        // 3D Rendering Settings
        public const float CAMERA_HEIGHT = 15f;
        public const float CAMERA_DISTANCE = 20f;
        public const float CAMERA_FOLLOW_SPEED = 2f;
        public const float CAMERA_FOV = 60f;
        public const float RENDER_DISTANCE = 1000f;
        
        // 3D Object Settings
        public const float PLAYER_3D_SIZE = 1.5f;
        public const float BULLET_3D_SIZE = 0.3f;
        public const float ASTEROID_3D_SIZE_MULTIPLIER = 1.2f;
        
        // 3D Lighting Settings
        public const float AMBIENT_LIGHT_INTENSITY = 0.2f;
        public const float DIRECTIONAL_LIGHT_INTENSITY = 0.8f;
        public const float POINT_LIGHT_RANGE = 25f;
        
        // 3D Performance Settings
        public const int MAX_RENDER_ITEMS = 2000;
        public const int MAX_LIGHTS = 8;
        public const bool ENABLE_FRUSTUM_CULLING = true;
        public const bool ENABLE_DEPTH_SORTING = true;
        
        // Spatial Partitioning Settings
        public const float SPATIAL_GRID_CELL_SIZE = 50f;
        
        // Explosion Settings
        public const float EXPLOSION_MAX_RADIUS = 50f;
        
        // Power-up Settings
        public const float POWERUP_RADIUS = 15f;
        public const int POWERUP_SPAWN_CHANCE = 15; // 15% chance to spawn on asteroid destruction
    }
}