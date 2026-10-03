// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Helpers;

internal static class ColourHelper
{
    public static Color GetXna4Colour(int r, int g, int b, int a)
        => new Color(r, g, b) * (1f / 255f * a);

    public static Color GetXna4Colour(int r, int g, int b)
        => new Color(r, g, b) * 1;

    /// <summary>
    ///     White, dimmed to <paramref name="fade" />. Nearly every fade in the game is a sprite or a
    ///     string drawn in its own colours and brought up or down as a whole, which is this.
    /// </summary>
    /// <remarks>
    ///     It was written out as GetXna4Colour(ColourLevels.Full, ColourLevels.Full,
    ///     ColourLevels.Full, fade) at forty-seven sites across seventeen files - three full
    ///     screens' worth of the same three arguments. Nothing else in the codebase passes all three
    ///     channels full and varies only the fourth.
    /// </remarks>
    public static Color WhiteFadedTo(int fade)
        => GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, fade);
}
