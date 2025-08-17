using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Asteroids
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialization
            const int screenWidth = 800;
            const int screenHeight = 600;

            Raylib.InitWindow(screenWidth, screenHeight, "Asteroids");
            Raylib.SetTargetFPS(60);

            Player player = new Player(new Vector2(screenWidth / 2, screenHeight / 2), 20);
            List<Bullet> bullets = new List<Bullet>();
            List<Asteroid> asteroids = new List<Asteroid>();
            List<ExplosionParticle> explosions = new List<ExplosionParticle>();

            Random random = new Random();
            int level = 1;
            int score = 0;
            bool gameOver = false;
            bool levelComplete = false;

            StartLevel(level, asteroids, random, screenWidth, screenHeight, player);

            // Main game loop
            while (!Raylib.WindowShouldClose())
            {
                // Update
                if (!gameOver && !levelComplete)
                {
                    player.Update();

                    // Shoot
                    if (Raylib.IsKeyPressed(KeyboardKey.Space))
                    {
                        bullets.Add(new Bullet(player.Position, Vector2.Transform(new Vector2(0, -5), Matrix3x2.CreateRotation(MathF.PI / 180 * player.Rotation))));
                    }

                    // Activate shield
                    if (Raylib.IsKeyPressed(KeyboardKey.X) && !player.IsShieldActive && player.ShieldCooldown <= 0)
                    {
                        player.IsShieldActive = true;
                        player.ShieldDuration = 180; // Set to MaxShieldDuration from Player.cs
                    }

                    foreach (var bullet in bullets)
                    {
                        bullet.Update();
                    }

                    foreach (var asteroid in asteroids)
                    {
                        asteroid.Update();
                    }

                    foreach (var explosion in explosions)
                    {
                        explosion.Update();
                    }

                    // Collision detection (bullets and asteroids)
                    for (int i = bullets.Count - 1; i >= 0; i--)
                    {
                        for (int j = asteroids.Count - 1; j >= 0; j--)
                        {
                            if (asteroids[j].Active && bullets[i].Active && Raylib.CheckCollisionCircles(bullets[i].Position, 2, asteroids[j].Position, asteroids[j].Radius))
                            {
                                bullets[i].Active = false;
                                asteroids[j].Active = false;
                                score += 100;

                                for (int k = 0; k < 10; k++)
                                {
                                    explosions.Add(new ExplosionParticle(
                                        asteroids[j].Position,
                                        new Vector2((float)(random.NextDouble() * 4 - 2), (float)(random.NextDouble() * 4 - 2)),
                                        60,
                                        Theme.ExplosionColor
                                    ));
                                }
                            }
                        }
                    }

                    // Collision detection (asteroids with each other)
                    for (int i = asteroids.Count - 1; i >= 0; i--)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (asteroids[i].Active && asteroids[j].Active && Raylib.CheckCollisionCircles(asteroids[i].Position, asteroids[i].Radius, asteroids[j].Position, asteroids[j].Radius))
                            {
                                // Simple elastic collision response
                                Vector2 tempVelocity = asteroids[i].Velocity;
                                asteroids[i].Velocity = asteroids[j].Velocity;
                                asteroids[j].Velocity = tempVelocity;
                            }
                        }
                    }

                    // Collision detection (player and asteroids)
                    for (int i = asteroids.Count - 1; i >= 0; i--)
                    {
                        if (asteroids[i].Active && Raylib.CheckCollisionCircles(player.Position, player.Size / 2, asteroids[i].Position, asteroids[i].Radius))
                        {
                            if (player.IsShieldActive)
                            {
                                asteroids[i].Active = false; // Destroy asteroid if shield is active
                                // Optionally, add explosion particles for the destroyed asteroid
                                for (int k = 0; k < 10; k++)
                                {
                                    explosions.Add(new ExplosionParticle(
                                        asteroids[i].Position,
                                        new Vector2((float)(random.NextDouble() * 4 - 2), (float)(random.NextDouble() * 4 - 2)),
                                        60,
                                        Theme.ExplosionColor
                                    ));
                                }
                            }
                            else
                            {
                                gameOver = true;
                            }
                        }
                    }

                    bullets.RemoveAll(b => !b.Active);
                    asteroids.RemoveAll(a => !a.Active);
                    explosions.RemoveAll(e => e.Lifespan <= 0);

                    if (asteroids.Count == 0)
                    {
                        levelComplete = true;
                    }
                }
                else
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        if (gameOver)
                        {
                            level = 1;
                            score = 0;
                            gameOver = false;
                        }
                        else if (levelComplete)
                        {
                            level++;
                            levelComplete = false;
                        }
                        StartLevel(level, asteroids, random, screenWidth, screenHeight, player);
                    }
                }


                // Draw
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // Draw grid
                for (int i = 0; i < screenWidth; i += 20)
                {
                    Raylib.DrawLine(i, 0, i, screenHeight, Theme.GridColor);
                }
                for (int i = 0; i < screenHeight; i += 20)
                {
                    Raylib.DrawLine(0, i, screenWidth, i, Theme.GridColor);
                }

                if (!gameOver && !levelComplete)
                {
                    player.Draw();

                    foreach (var bullet in bullets)
                    {
                        bullet.Draw();
                    }

                    foreach (var asteroid in asteroids)
                    {
                        asteroid.Draw();
                    }

                    foreach (var explosion in explosions)
                    {
                        explosion.Draw();
                    }

                    Raylib.DrawText($"Score: {score}", 10, 10, 20, Theme.TextColor);
                    Raylib.DrawText($"Level: {level}", screenWidth - 100, 10, 20, Theme.TextColor);
                }
                else if (levelComplete)
                {
                    Raylib.DrawText($"LEVEL {level} COMPLETE", screenWidth / 2 - 150, screenHeight / 2 - 20, 40, Theme.LevelCompleteColor);
                    Raylib.DrawText("PRESS [ENTER] TO START NEXT LEVEL", screenWidth / 2 - 200, screenHeight / 2 + 20, 20, Theme.TextColor);
                }
                else
                {
                    Raylib.DrawText("GAME OVER", screenWidth / 2 - 100, screenHeight / 2 - 20, 40, Theme.GameOverColor);
                    Raylib.DrawText("PRESS [ENTER] TO RESTART", screenWidth / 2 - 150, screenHeight / 2 + 20, 20, Theme.TextColor);
                }

                Raylib.EndDrawing();
            }

            // De-Initialization
            Raylib.CloseWindow();
        }

        static void StartLevel(int level, List<Asteroid> asteroids, Random random, int screenWidth, int screenHeight, Player player)
        {
            player.Position = new Vector2(screenWidth / 2, screenHeight / 2);
            player.Velocity = Vector2.Zero;
            player.Rotation = 0;

            asteroids.Clear();
            int asteroidCount = 10 + (level - 1) * 2;
            for (int i = 0; i < asteroidCount; i++)
            {
                AsteroidSize size = (AsteroidSize)random.Next(0, 3);
                asteroids.Add(new Asteroid(
                    new Vector2(random.Next(0, screenWidth), random.Next(0, screenHeight)),
                    new Vector2(random.Next(-1, 2), random.Next(-1, 2)),
                    size,
                    random,
                    level
                ));
            }
        }
    }
}