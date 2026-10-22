// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using System.Runtime.InteropServices;

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

        KeepFullScreenOutOfItsOwnSpace();

        using var game = new GameHost();
        game.Run();
    }

    /// <summary>
    ///     On macOS, makes F12 cover the display in place rather than moving the game to a fullscreen
    ///     Space of its own. In its own Space the game misses the display's refresh again and again,
    ///     dropping to 30 to 45 frames a second on an M2 Pro, while the same picture in a window the
    ///     size of the display holds 60.
    /// </summary>
    /// <remarks>
    ///     SDL reads this when it starts, which is inside <see cref="GameHost" />'s constructor, so it
    ///     has to be set before that. It goes through <c>setenv</c> because
    ///     <see cref="Environment.SetEnvironmentVariable(string, string)" /> only changes .NET's own
    ///     copy of the environment on macOS, and SDL never sees it. A value the player has set
    ///     already is left alone.
    /// </remarks>
    private static void KeepFullScreenOutOfItsOwnSpace()
    {
        if (OperatingSystem.IsMacOS())
        {
            _ = setenv("SDL_VIDEO_MAC_FULLSCREEN_SPACES", "0", 0);
        }
    }

    [DllImport("libc")]
    private static extern int setenv(string name, string value, int overwrite);
}
