// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Text;
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     Names for the tiles the game treats as more than scenery.
/// </summary>
/// <remarks>
///     A screen is stored as bare tile numbers, so nothing in the data says 397 is a teleport booth.
///     <see cref="ActionTiles" />, <see cref="TunnelTiles" /> and <see cref="MarkerTile" /> say so,
///     and the editor reads those rather than keeping a list that could drift out of step. How a
///     marker is drawn belongs to <see cref="MarkerCatalogue" />.
/// </remarks>
internal static class TileCatalogue
{

    private static readonly Dictionary<int, string> Names = BuildNames();

    /// <summary>What the game does with this tile, or null if it is ordinary scenery.</summary>
    public static string? SpecialName(int tileImage)
        => Names.GetValueOrDefault(tileImage);

    public static bool IsSpecial(int tileImage) => Names.ContainsKey(tileImage);

    /// <summary>
    ///     Whether the editor draws this tile itself rather than from the sheet.
    ///     <see cref="MarkerCatalogue" /> holds what it draws.
    /// </summary>
    public static bool IsMarker(int tileImage) => MarkerCatalogue.IsDrawnAsAMarker(tileImage);

    /// <summary>
    ///     Describes a tile for a status bar or tooltip: its number, and what it is for if it is
    ///     anything in particular.
    /// </summary>
    public static string Describe(int tileImage)
    {
        if (tileImage < 0)
        {
            return "Empty";
        }

        string? name = SpecialName(tileImage);

        return name is null ? "Tile " + tileImage : "Tile " + tileImage + "  ·  " + name;
    }

    private static Dictionary<int, string> BuildNames()
    {
        var names = new Dictionary<int, string>();

        foreach (ActionTiles tile in Enum.GetValues<ActionTiles>())
        {
            names[(int)tile] = Humanise(tile.ToString());
        }

        foreach (MarkerTile marker in MarkerTiles.All)
        {
            names[(int)marker] = Humanise(marker.ToString());
        }

        for (int pair = 0; pair < TunnelTiles.MouthPairs; pair++)
        {
            int left = TunnelTiles.FirstMouth + pair * TunnelTiles.MouthStride;

            names[left] = "Tunnel mouth " + (pair + 1) + " (west)";
            names[left + 1] = "Tunnel mouth " + (pair + 1) + " (east)";
        }

        return names;
    }

    /// <summary>
    ///     Turns an enum member's name into readable text. "AccessBoothLeft" becomes "Access booth
    ///     left"; "TunnelLiftTop1" becomes "Tunnel lift top 1".
    /// </summary>
    private static string Humanise(string name)
    {
        var text = new StringBuilder(name.Length + 8);

        for (int index = 0; index < name.Length; index++)
        {
            char character = name[index];
            bool startsWord = index > 0 &&
                              (char.IsUpper(character) || char.IsDigit(character) != char.IsDigit(name[index - 1]));

            if (startsWord)
            {
                text.Append(' ');
            }

            text.Append(index == 0 ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character));
        }

        return text.ToString();
    }
}
