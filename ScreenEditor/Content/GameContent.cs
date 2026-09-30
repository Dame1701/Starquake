// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.IO;

namespace Starquake.ScreenEditor.Content;

/// <summary>
///     Finds the game's map and tile sheets on disk.
/// </summary>
/// <remarks>
///     The editor only ever edits one map - the one this game ships - so making somebody find it
///     through a file dialog every time they open the tool is friction for nothing. Walking up from
///     the executable to the solution folder finds it in a normal checkout, and the file menu is
///     still there for the times it does not.
/// </remarks>
public static class GameContent
{

    /// <summary>
    ///     The file that marks the top of a checkout.
    /// </summary>
    private const string SolutionFileName = "Starquake.sln";

    private static readonly string[] TileSheetNames =
    [
        "StartilesNew.png",
        "StartilesOriginal.png"
    ];

    /// <summary>
    ///     The solution folder, or null when the editor is running from somewhere else entirely - a
    ///     published copy, say. Everything else here returns nothing in that case, and the editor
    ///     opens empty rather than complaining.
    /// </summary>
    private static string? RepositoryRoot { get; } = FindRepositoryRoot();

    public static string? MapPath => Combine("Starquake.Shared", "Data", ScreenDataFile.DefaultFileName);

    /// <summary>
    ///     The tile sheets the game can draw the world with, best one first. They are alternative
    ///     artwork for the same tile numbers - the redrawn set and the Spectrum original - so either
    ///     can be used to edit the same map, and being able to switch is a quick way to see how a
    ///     screen reads in both.
    /// </summary>
    public static IReadOnlyList<string> TileSheetPaths
    {
        get
        {
            var paths = new List<string>();

            foreach (var name in TileSheetNames)
            {
                var path = Combine("Starquake.Shared", "Content", "Graphics", name);
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

        var path = Path.Combine([RepositoryRoot, .. parts]);

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
