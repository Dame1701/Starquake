using System;

namespace StarquakeXNA
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Starquake game = new Starquake())
            {
                game.Run();
            }
        }
    }
}

