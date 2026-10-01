// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     Where the planet core sits: which screen, and where on it.
/// </summary>
/// <remarks>
///     <para>
///         The one authored position that is not a <see cref="MarkerTile" />, because it does not fit
///         the tile grid. The core is nine elements three across and three down, each a sprite wide, so
///         it covers <see cref="AreaWidth" /> by <see cref="AreaHeight" /> pixels - one and a half tiles
///         across by two down - and it has never been tile-aligned: the position the game shipped with
///         is x 416, which is a quarter of the way through a tile. A marker could not express it.
///     </para>
///     <para>
///         So it is stored as a position on the screen it is on - see
///         <see cref="GameScreenData.Core" /> - at <see cref="PositionStep" /> resolution, which is fine
///         enough to place the core by eye and coarse enough to keep it on the collision grid.
///     </para>
/// </remarks>
public sealed record CoreSite(int ScreenNumber, int XPos, int YPos)
{

    /// <summary>
    ///     How many elements the core is across and down. Nine of them in a square - <c>CoreElements</c>
    ///     reads these rather than keeping its own copy, so the area the editor draws and the area the
    ///     game fills cannot disagree.
    /// </summary>
    public const int SpritesAcross = 3;

    /// <inheritdoc cref="SpritesAcross" />
    public const int SpritesDown = 3;

    /// <summary>
    ///     The size of one core element. The same as the game's standard sprite - a test pins the two
    ///     together, because this has to be said here for the editor and <c>SpriteMetrics</c> is the
    ///     game's own.
    /// </summary>
    public const int SpriteSize = 64;

    /// <summary>
    ///     How far the core reaches from its position, which is what the editor outlines and what the
    ///     author has to leave room for.
    /// </summary>
    public const int AreaWidth = SpritesAcross * SpriteSize;

    /// <inheritdoc cref="AreaWidth" />
    public const int AreaHeight = SpritesDown * SpriteSize;

    /// <summary>
    ///     What the core's position is rounded to. Finer than a tile, which it has to be, and a whole
    ///     number of them to a collision cell so that a core placed by eye still lines up with the
    ///     scenery around it.
    /// </summary>
    public const int PositionStep = 16;

    /// <summary>
    ///     Where the core was before it was on the map: screen 199 at 416, 192. Used for a map saved
    ///     before the position was stored, so an older planet still has a core to finish the game at.
    /// </summary>
    public static CoreSite Original { get; } = new(WorldMap.CoreScreenNumber, 416, 192);

    /// <summary>
    ///     Reads the core off a map. The first screen carrying a position wins, and a map with none
    ///     falls back to where the game used to keep it.
    /// </summary>
    public static CoreSite FindIn(GameScreenData[] screens)
    {
        for (int screen = 0; screen < screens.Length; screen++)
        {
            if (screens[screen].Core is { } core)
            {
                return new CoreSite(screen, core.XPos, core.YPos);
            }
        }

        return Original;
    }

    /// <summary>
    ///     Puts the core on one screen and takes it off every other, because a planet has one core.
    ///     Returns whether anything actually moved.
    /// </summary>
    /// <remarks>
    ///     The rule lives here rather than in the screen editor because it is a rule about the map.
    ///     <see cref="FindIn" /> takes the first screen carrying a core, so a second one would not be
    ///     refused - it would simply never be found, which is a map that looks right in the editor and
    ///     plays wrong.
    /// </remarks>
    public static bool PlaceOn(GameScreenData[] screens, int screen, CorePlacement? placement)
    {
        bool moved = false;

        for (int other = 0; other < screens.Length; other++)
        {
            if (other != screen && screens[other].Core is not null)
            {
                screens[other].Core = null;
                moved = true;
            }
        }

        if (screens[screen].Core != placement)
        {
            screens[screen].Core = placement;
            moved = true;
        }

        return moved;
    }

    /// <summary>
    ///     How many screens carry a core. One on a finished map, nought on one saved before the position
    ///     was stored, and never more - see <see cref="PlaceOn" />.
    /// </summary>
    public static int CountOn(GameScreenData[] screens)
    {
        int found = 0;

        foreach (GameScreenData screen in screens)
        {
            if (screen.Core is not null)
            {
                found++;
            }
        }

        return found;
    }

    /// <summary>
    ///     A position rounded to the step the core is placed on, and held inside the screen so that the
    ///     whole core stays on it.
    /// </summary>
    public static CorePlacement Snap(int xPos, int yPos)
        => new(
            Math.Clamp(Round(xPos), 0, TileMetrics.PlayAreaWidth - AreaWidth),
            Math.Clamp(Round(yPos), 0, TileMetrics.PlayAreaHeight - AreaHeight));

    private static int Round(int value)
        => (value + PositionStep / 2) / PositionStep * PositionStep;
}
