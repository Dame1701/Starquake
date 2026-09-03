using System;

namespace StarquakeXNA;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        // Content and the screen data are opened relative to the working directory, which
        // isn't the output folder when the game is launched from anywhere other than that
        // folder -- from Finder, for instance, where a .app bundle starts at "/".
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        using var game = new Starquake();
        game.Run();
    }
}
