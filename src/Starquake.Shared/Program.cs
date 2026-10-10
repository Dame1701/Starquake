// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake;

internal static class Program
{
    /// <summary>The main entry point for the application.</summary>
    [STAThread]
    private static void Main()
    {
        // Content and the screen data are opened relative to the working directory, which is not the
        // output folder when the game is launched from elsewhere. From Finder, for instance, a .app
        // bundle starts at "/".
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        using var game = new GameHost();
        game.Run();
    }
}
