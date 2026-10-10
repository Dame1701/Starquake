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
///     The only authored position that is not a <see cref="MarkerTile" />. The core covers
///     <see cref="AreaWidth" /> by <see cref="AreaHeight" /> pixels, which is one and a half tiles
///     across by two down, and it is not tile-aligned: the shipped position is x 416, a quarter of
///     the way through a tile. A marker cannot express that.
///     <para>
///         So it is stored as a pixel position on its screen (<see cref="GameScreenData.Core" />) at
///         <see cref="PositionStep" /> resolution. Fine enough to place by eye, coarse enough to stay
///         on the collision grid.
///     </para>
/// </remarks>
public sealed record CoreSite(int ScreenNumber, int XPos, int YPos)
{

    /// <summary>
    ///     The core is a three by three square of elements. <c>CoreElements</c> reads these instead of
    ///     keeping its own copy, so the area the editor draws matches the area the game fills.
    /// </summary>
    public const int SpritesAcross = 3;

    /// <inheritdoc cref="SpritesAcross" />
    public const int SpritesDown = 3;

    /// <summary>
    ///     The size of one core element. Matches the game's standard sprite size, which a test pins.
    ///     Stated here because the editor needs it and <c>SpriteMetrics</c> belongs to the game.
    /// </summary>
    public const int SpriteSize = 64;

    /// <summary>
    ///     How far the core reaches from its position. The editor outlines this, and the author has to
    ///     leave room for it.
    /// </summary>
    public const int AreaWidth = SpritesAcross * SpriteSize;

    /// <inheritdoc cref="AreaWidth" />
    public const int AreaHeight = SpritesDown * SpriteSize;

    /// <summary>
    ///     What the core's position is rounded to. Finer than a tile, and a whole number of steps to a
    ///     collision cell, so a core placed by eye still lines up with the scenery.
    /// </summary>
    public const int PositionStep = 16;

    /// <summary>
    ///     The fallback for a map with no core position stored: screen 199 at 416, 192. Keeps an older
    ///     planet finishable.
    /// </summary>
    public static CoreSite Original { get; } = new(WorldMap.CoreScreenNumber, 416, 192);

    /// <summary>
    ///     Reads the core off a map. The first screen carrying a position wins; a map with none falls
    ///     back to <see cref="Original" />.
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
    ///     Puts the core on one screen and clears it from every other, because a planet has one core.
    /// </summary>
    /// <returns>Whether anything moved.</returns>
    /// <remarks>
    ///     <see cref="FindIn" /> takes the first screen carrying a core, so a second core is never
    ///     rejected, just never found. That is a map which looks right in the editor and plays wrong,
    ///     which is why the rule lives here rather than in the editor.
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
    ///     How many screens carry a core. One on a finished map, none on a map with no position
    ///     stored, and never more than one. See <see cref="PlaceOn" />.
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
    ///     Rounds a position to <see cref="PositionStep" /> and clamps it so the whole core stays on
    ///     the screen.
    /// </summary>
    public static CorePlacement Snap(int xPos, int yPos)
        => new(
            Math.Clamp(Round(xPos), 0, TileMetrics.PlayAreaWidth - AreaWidth),
            Math.Clamp(Round(yPos), 0, TileMetrics.PlayAreaHeight - AreaHeight));

    private static int Round(int value)
        => (value + PositionStep / 2) / PositionStep * PositionStep;
}
