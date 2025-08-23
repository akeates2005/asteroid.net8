using System;

namespace Asteroids
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // For now, use SimpleProgram until Enhanced version is fully ready
                var simpleProgram = new SimpleProgram();
                simpleProgram.Run();
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