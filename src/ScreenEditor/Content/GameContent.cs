// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.ScreenEditor.Content;

/// <summary>
///     Finds the game's map and tile sheets on disk.
/// </summary>
/// <remarks>
///     The editor only edits the one map this game ships, so it locates it by walking up from the
///     executable to the solution folder rather than asking through a file dialog. The file menu
///     still covers the cases where that fails.
/// </remarks>
internal static class GameContent
{

    /// <summary>The file that marks the top of a checkout.</summary>
    private const string SolutionFileName = "Starquake.sln";

    private static readonly string[] TileSheetNames =
    [
        Path.Combine("Enhanced", "LowerScreenTiles.png"),
        Path.Combine("Original", "LowerScreenTiles.png")
    ];

    /// <summary>
    ///     The solution folder, or null when the editor runs from outside a checkout, such as a
    ///     published copy. Everything else here then returns null and the editor opens empty.
    /// </summary>
    private static string? RepositoryRoot { get; } = FindRepositoryRoot();

    public static string? MapPath => Combine("Starquake.Shared", "Data", ScreenDataFile.DefaultFileName);

    /// <summary>
    ///     The tile sheets the game can draw with, preferred one first. The redrawn set and the
    ///     Spectrum original are alternative artwork for the same tile numbers, so either can edit
    ///     the same map and switching shows how a screen reads in both.
    /// </summary>
    public static IReadOnlyList<string> TileSheetPaths
    {
        get
        {
            var paths = new List<string>();

            foreach (string name in TileSheetNames)
            {
                string? path = Combine("Starquake.Shared", "Content", "Graphics", name);
                if (path is not null)
                {
                    paths.Add(path);
                }
            }

            return paths;
        }
    }

    private static string? Combine(params string[] parts)
    {
        if (RepositoryRoot is null)
        {
            return null;
        }

        string path = Path.Combine([RepositoryRoot, .. parts]);

        return File.Exists(path) ? path : null;
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
