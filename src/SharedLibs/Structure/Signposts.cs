// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     The signpost that tells the player a teleport code, and where on its board the code is
///     written.
/// </summary>
/// <remarks>
///     A sign is <see cref="ActionTiles.TeleportSignpost" />, painted on whichever screen the player
///     should read it from. The artwork's board is blank and the game writes the code across it, so
///     one tile serves every sign on the planet.
///     <para>
///         A sign stores the screen it advertises, not the code itself
///         (<see cref="GameScreenData.SignpostedScreen" />). Codes can be changed on the screen they
///         belong to, so a stored copy would go stale; the code is looked up each time the sign is
///         drawn.
///     </para>
/// </remarks>
public static class Signposts
{

    /// <summary>
    ///     The box the code is written in, in pixels from the top left of the sign's tile. Measured
    ///     off the board in the artwork, so a five letter code lands on the board rather than over
    ///     the frame around it.
    /// </summary>
    public const int TextLeft = 24;

    /// <inheritdoc cref="TextLeft" />
    public const int TextWidth = 86;

    /// <inheritdoc cref="TextLeft" />
    public const int TextTop = 9;

    /// <inheritdoc cref="TextLeft" />
    public const int TextHeight = 14;

    /// <summary>
    ///     Whether a screen has a signpost on it. Only the editor asks, so it can offer the sign a
    ///     code; the game is already walking the tiles to draw them.
    /// </summary>
    public static bool StandOn(GameScreenData screen)
    {
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (screen.ScreenTiles[x, y].TileImage == (int)ActionTiles.TeleportSignpost)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     What the signs on a screen read, or null if they have nothing to say. Null covers a sign
    ///     not yet pointed at a screen and one pointed at a screen whose code has been removed. Both
    ///     leave the board blank, which is better than naming a code no booth answers to.
    /// </summary>
    public static string? CodeShownOn(GameScreenData[] screens, int screen)
    {
        if (screens[screen].SignpostedScreen is not { } signposted ||
            signposted < 0 ||
            signposted >= screens.Length)
        {
            return null;
        }

        return screens[signposted].TeleportCode;
    }
}
