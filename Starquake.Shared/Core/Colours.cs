// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Core;

internal static class Colours
{
    public static Color GetXna4Colour(int r, int g, int b, int a) => new Color(r, g, b) * (1f / 255f * a);

    public static Color GetXna4Colour(int r, int g, int b) => new Color(r, g, b) * 1;

    /// <summary>
    ///     White, dimmed to <paramref name="fade" />. Nearly every fade in the game is a sprite or a
    ///     string drawn in its own colours and brought up or down as a whole, which is this.
    /// </summary>
    public static Color WhiteFadedTo(int fade) => GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, fade);
}
