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
///     A screen is stored as bare tile numbers, so nothing in the data says that 397 is a teleport
///     booth or that 420 is a tunnel mouth that leads to the previous screen. The game knows because
///     <see cref="ActionTiles" /> and <see cref="TunnelTiles" /> say so, and the editor reads the
///     same two sources rather than keeping its own list that could drift out of step with them.
/// </remarks>
public static class TileCatalogue
{

    /// <summary>
    ///     The tiles the game never actually draws.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         These four mark a position for the game to do something at rather than describing
    ///         scenery: somewhere to put a floor nasty, somewhere a tunnel lift runs, somewhere a core
    ///         element would ideally be seeded, and a gap that should stay a gap. The game's drawing
    ///         code names all four and skips them, so nothing of them is ever on screen.
    ///     </para>
    ///     <para>
    ///         They still need artwork, because the editor has to show something where they are, and
    ///         what the sheet has for them is a flat block of colour with the name written across it.
    ///         That artwork was only ever meant for the editor, and painted across a screen it drowns
    ///         out the tiles that do get drawn. So the editor recognises these four and draws its own
    ///         marker instead - see <see cref="MarkerLabel" />.
    ///     </para>
    /// </remarks>
    private static readonly Dictionary<int, string> MarkerLabels = new()
    {
        [(int)ActionTiles.BlankFiller] = "SPACER",
        [(int)ActionTiles.ConveyerNasty] = "CONVEYER NASTY",
        [(int)ActionTiles.PreferredCoreLocation] = "CORE",
        [(int)ActionTiles.TunnelLiftTile] = "LIFT"
    };

    private static readonly Dictionary<int, string> Names = BuildNames();

    /// <summary>
    ///     What the game does with this tile, or null if it is ordinary scenery.
    /// </summary>
    public static string? SpecialName(int tileImage)
        => Names.GetValueOrDefault(tileImage);

    public static bool IsSpecial(int tileImage) => Names.ContainsKey(tileImage);

    /// <summary>
    ///     A short word for a tile the game does not draw, or null for anything it does. The editor
    ///     draws its own marker for these rather than the placeholder artwork on the sheet.
    /// </summary>
    public static string? MarkerLabel(int tileImage)
        => MarkerLabels.GetValueOrDefault(tileImage);

    public static bool IsMarker(int tileImage) => MarkerLabels.ContainsKey(tileImage);

    /// <summary>
    ///     A tile described for a status bar or a tooltip: its number, and what it is for if that is
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

        for (int pair = 0; pair < TunnelTiles.MouthPairs; pair++)
        {
            int left = TunnelTiles.FirstMouth + pair * TunnelTiles.MouthStride;

            names[left] = "Tunnel mouth " + (pair + 1) + " (west)";
            names[left + 1] = "Tunnel mouth " + (pair + 1) + " (east)";
        }

        return names;
    }

    /// <summary>
    ///     Turns an enum member's name into something readable: "AccessBoothLeft" becomes "Access
    ///     booth left", "TunnelLiftTop1" becomes "Tunnel lift top 1".
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
