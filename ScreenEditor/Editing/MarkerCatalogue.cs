// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Material.Icons;
using Starquake.Structure;

namespace Starquake.ScreenEditor.Editing;

/// <summary>
///     How the editor shows a position the author marks rather than draws: its icon, the word written
///     across it, and what it is for.
/// </summary>
/// <remarks>
///     <para>
///         One table, because the same marker is shown in two places that have to agree - a button on
///         the toolbar and a panel on the screen being edited. Naming the icon twice would let the
///         button and the square it paints drift apart, which is the one thing that would make the
///         icons worse than no icons.
///     </para>
///     <para>
///         The foot of a tunnel lift is in here as well, though it is not a <see cref="MarkerTile" />:
///         it is a real tile with real collision, but nothing is drawn on it, so the editor has to
///         show something for it or it would be as invisible here as it is in the game. It has no
///         toolbar button for the same reason it is not a marker - it is painted from the sheet like
///         any other tile.
///     </para>
/// </remarks>
internal static class MarkerCatalogue
{

    private static readonly Dictionary<int, MarkerFace> Faces = new()
    {
        [(int)MarkerTile.Spacer] = new MarkerFace(
            MaterialIconKind.SquareOutline,
            "SPACER",
            "Spacer",
            "A gap that stays a gap. No nasties spawn here and no pickups will appear."),

        [(int)MarkerTile.ConveyorNasty] = new MarkerFace(
            MaterialIconKind.Bug,
            "CONVEYOR NASTY",
            "Conveyor nasty",
            "Where a floor nasty walks. One is created for each of these on the screen."),

        [(int)MarkerTile.PreferredCoreLocation] = new MarkerFace(
            MaterialIconKind.Extension,
            "CORE",
            "Core element",
            "Where a core element is put, for a screen that would otherwise scatter one somewhere " +
            "awkward to reach."),

        [(int)MarkerTile.BlobStartPosition] = new MarkerFace(
            MaterialIconKind.Flag,
            "START",
            "Blob's start position",
            "Where Blob begins a new game, and where the game opens. There is one on the whole " +
            "planet, so placing it moves it."),

        [(int)MarkerTile.ShipCrashSite] = new MarkerFace(
            MaterialIconKind.Rocket,
            "CRASH SITE",
            "Ship crash site",
            "Where Blob's ship comes down and the wreck smokes for the rest of the game. Belongs on " +
            "the same screen as the start position."),

        [(int)MarkerTile.CoreExitPosition] = new MarkerFace(
            MaterialIconKind.DoorOpen,
            "CORE EXIT",
            "Core exit",
            "Where Blob steps out of the planet core. The screen this is on is the core's doorstep: it " +
            "is kept running while Blob stands there, and nasties are switched off on it."),

        [(int)ActionTiles.TunnelLiftTile] = new MarkerFace(
            MaterialIconKind.ArrowUpBold,
            "LIFT",
            "Tunnel lift",
            "The foot of a lift shaft. Solid on every cell and drawn as nothing, so Blob meets a wall " +
            "it cannot see - which is what carries it up.")
    };

    /// <summary>
    ///     How to show a tile the game does not draw, or null for anything it does.
    /// </summary>
    public static MarkerFace? Face(int tileImage) => Faces.GetValueOrDefault(tileImage);

    public static bool IsDrawnAsAMarker(int tileImage) => Faces.ContainsKey(tileImage);

    /// <summary>
    ///     The markers that can be painted, in the order the toolbar offers them. Read off
    ///     <see cref="MarkerTiles.All" /> rather than this table, so a marker added to the game cannot
    ///     quietly fail to appear here - it throws instead.
    /// </summary>
    public static IReadOnlyList<(MarkerTile Marker, MarkerFace Face)> Paintable { get; } =
        MarkerTiles.All
            .Select(marker => (marker, Faces[(int)marker]))
            .ToArray();
}

/// <summary>
///     What the editor draws and says for one position the author marks.
/// </summary>
/// <param name="Icon">
///     The glyph, shown on the toolbar button and again on the square itself so that the two read as
///     the same thing.
/// </param>
/// <param name="Label">The word written across the square, kept short enough to fit on one.</param>
/// <param name="Title">What to call it in a tooltip.</param>
/// <param name="Description">What the game does with it.</param>
internal sealed record MarkerFace(MaterialIconKind Icon, string Label, string Title, string Description);
