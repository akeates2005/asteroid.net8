# Asteroids

This is a classic Asteroids game implemented in C# using the Raylib-cs library.

## Features

*   Classic Asteroids gameplay: fly around, shoot asteroids, and avoid collisions.
*   Progressive difficulty: the game gets harder as you advance through levels.
*   Player shield: a temporary shield for protection.
*   Local leaderboard: track your high scores.
*   Retro theme: a retro theme with a grid background and vibrant colors.

## Controls

*   **Left/Right Arrow Keys:** Rotate the ship.
*   **Up Arrow Key:** Apply thrust.
*   **Spacebar:** Shoot bullets.
*   **X Key:** Activate the shield (when available).
*   **P Key:** Pause the game.
*   **Enter Key:** Start the next level or restart the game.

## How to Build and Run

This project is a standard .NET Core application.

1.  **Install .NET:** If you don't have it already, install the [.NET SDK](https://dotnet.microsoft.com/download).
2.  **Restore Dependencies:** Open a terminal in the `Asteroids` directory and run the following command to restore the necessary packages:
    ```bash
    dotnet restore
    ```
3.  **Run the Game:** Run the following command to build and run the game:
    ```bash
    dotnet run
    ```

## Dependencies

*   [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) version 7.0.1: A C# wrapper for the Raylib game library.