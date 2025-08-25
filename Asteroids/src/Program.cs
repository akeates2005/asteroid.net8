using System;

namespace Asteroids
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Use GameProgram as the main game implementation
                var gameProgram = new GameProgram();
                gameProgram.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}