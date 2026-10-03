// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     The signpost that tells the player a teleport code, and where on its board the code is
///     written.
/// </summary>
/// <remarks>
///     <para>
///         A code is no use to somebody who has not been told it, and the planet tells it on a
///         signpost - <see cref="ActionTiles.TeleportSignpost" />, painted on whichever screen the
///         player is meant to read it from. The board the artwork draws is blank and the game writes
///         the code across it, so one tile serves every sign on the planet rather than one tile per
///         code having to be drawn.
///     </para>
///     <para>
///         A sign names the screen it advertises rather than the code itself - see
///         <see cref="GameScreenData.SignpostedScreen" />. A code belongs to the screen it reaches
///         and can be changed there, so a sign holding a copy of one would go stale the moment it
///         was; a screen number cannot, which is why that is what is stored and the code is looked
///         up afresh every time the sign is drawn.
///     </para>
/// </remarks>
public static class Signposts
{

    /// <summary>
    ///     The box the code is written in, in pixels from the top left of the tile the sign is
    ///     painted on. Measured off the board in the artwork: the text is centred across
    ///     <see cref="TextLeft" /> to <see cref="TextWidth" /> and its capitals fill
    ///     <see cref="TextHeight" />, which is what keeps a five letter code on the board rather
    ///     than over the frame around it.
    /// </summary>
    public const int TextLeft = 24;

    /// <inheritdoc cref="TextLeft" />
    public const int TextWidth = 86;

    /// <inheritdoc cref="TextLeft" />
    public const int TextTop = 9;

    /// <inheritdoc cref="TextLeft" />
    public const int TextHeight = 14;

    /// <summary>
    ///     Whether there is a signpost on a screen. The editor asks so that it can offer the sign a
    ///     code; the game does not, because it is already walking the tiles to draw them.
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
    ///     What the signs on a screen read, or null if there is nothing for them to say: a sign that
    ///     has not been pointed at a screen yet, or one pointed at a screen whose code has since been
    ///     taken away. A blank board is the right answer to both - it is what the artwork draws, and
    ///     a sign that named a code no booth answers to would be worse than one that names none.
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
