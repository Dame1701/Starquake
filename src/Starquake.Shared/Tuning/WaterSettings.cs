// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The water Olly sits in. Driven by <c>Shaders/Water.fx</c>; see <c>WaterEffect</c>.
/// </summary>
/// <remarks>
///     Olly's artwork runs from y 172 to y 330 of the playfield, which is what sets the waterline
///     just below the top of him rather than a number chosen by eye. That is why the surface lands
///     about two thirds up the screen rather than exactly half.
/// </remarks>
internal static class WaterSettings
{
    /// <summary>
    ///     Where the surface sits, in playfield pixels from the top. Olly's head starts at 172, so
    ///     this leaves the top of it in the air and the rest of him under water.
    /// </summary>
    public const int WaterlineY = 200;

    /// <summary>
    ///     How far below the surface the water reaches full colour, in playfield pixels. Deeper
    ///     pixels read greener and darker, which gives the water a bottom rather than an even wash.
    /// </summary>
    public const int FullDepth = 230;

    /// <summary>
    ///     How far the water pushes what is behind it, as a fraction of the picture, and how closely
    ///     the waves are spaced. Small: a tenth of a percent of the screen is already a visible
    ///     wobble, and much more reads as a funhouse mirror.
    /// </summary>
    public const float Amplitude = 0.0045f;

    /// <inheritdoc cref="Amplitude" />
    public const float Frequency = 46f;

    /// <summary>How fast the waves travel, in radians per logic frame.</summary>
    private const double WaveSpeed = 0.075;

    /// <inheritdoc cref="WaterColour" />
    public const float Tint = 0.85f;

    /// <summary>
    ///     How much of that is already present at the surface, before any depth brings it on. Without
    ///     it the first stretch below the line looks like dry land with a wobble, and Blob in the
    ///     shallows looks no different from Blob beside them.
    /// </summary>
    public const float AtTheSurface = 0.7f;

    /// <inheritdoc cref="SurfaceColour" />
    public const float SurfaceThickness = 0.007f;

    /// <summary>
    ///     What the water does to the colour behind it, and how much of that is applied at full
    ///     depth. A multiplier rather than a wash, so the room keeps its own lightness and only
    ///     loses its reds.
    /// </summary>
    public static Vector3 WaterColour => new(0.35f, 0.8f, 0.95f);

    /// <summary>
    ///     The blue laid over everything under the water. <see cref="WaterColour" /> is a multiplier
    ///     and can only take colour away, which turns something bright cyan and does nothing at all
    ///     to something already dark. This is what actually makes the water look blue.
    /// </summary>
    public static Vector3 BlueCast => new(0.02f, 0.1f, 0.22f);

    /// <summary>What the line at the surface burns, and how thick it is as a fraction.</summary>
    public static Vector3 SurfaceColour => new(0.3f, 0.55f, 0.6f);

    /// <summary>The waterline as a fraction of the picture, which is what the shader works in.</summary>
    public static float WaterlineIn01 => WaterlineY / (float)TileMetrics.PlayAreaHeight;

    /// <inheritdoc cref="WaterlineIn01" />
    public static float OverFullDepthIn01 => TileMetrics.PlayAreaHeight / (float)FullDepth;

    /// <summary>
    ///     Where the waves have travelled to by a given logic frame. Logic frames, so the water moves
    ///     at the same rate whatever the display is doing.
    /// </summary>
    public static float PhaseAt(int logicFrame) => (float)(logicFrame * WaveSpeed);
}
