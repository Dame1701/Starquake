// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The halo thrown by a pulsing tile that asks for one - which is the three shuttle tags, the
///     lights that mark where a pad used to be. Driven by <c>Shaders/Halo.fx</c>.
/// </summary>
/// <remarks>
///     A pulse alone repaints the tile in a colour, which is light held inside the tile's own shape:
///     it never spreads, and a light that does not spread does not read as glowing. The halo is drawn
///     underneath, one per light and centred on it, at a size measured against the light rather than
///     the tile. Underneath, because a glow laid over a light smears across it instead of sitting
///     behind it.
/// </remarks>
internal static class HaloSettings
{
    /// <summary>
    ///     How tall the halo is drawn, in pixels, which is the whole of how far it spreads: the
    ///     shader has no texture and fills whatever quad it is given. Measured against the light
    ///     rather than the tile, so with lights of 28 by 36 this leaves a skirt of about half a light
    ///     around each.
    /// </summary>
    public const int Diameter = 64;

    /// <summary>
    ///     What that is as a scale on the tile the quad is cut from, and the shape of the quad it
    ///     leaves. The tile is 128 by 96, so a quad <see cref="Diameter" /> tall is a third again as
    ///     wide, which is why the shader is told the ratio and keeps the glow round inside it.
    /// </summary>
    public static float Scale => Diameter / (float)TileMetrics.TileHeight;

    /// <inheritdoc cref="Scale" />
    public static float YOverX => TileMetrics.TileHeight / (float)TileMetrics.TileWidth;

    /// <summary>
    ///     How far across that square the glow reaches before it is gone, as a fraction. Short of the
    ///     whole, so it fades before the corners rather than showing a square edge.
    /// </summary>
    public const float Reach = 0.82f;

    /// <summary>
    ///     How sharply the glow falls away: nought for a broad soft one, one for a tight core with a
    ///     thin skirt.
    /// </summary>
    public const float Falloff = 0.45f;

    /// <summary>
    ///     How many times the halo is laid down. Additive, so this is how hard it burns against a
    ///     pulse that has already drawn the tile over itself two to six times. A single pass against
    ///     six is lost entirely.
    /// </summary>
    public const int Passes = 3;

    /// <inheritdoc cref="Reach" />
    public static float OverReach => 1f / Reach;
}
