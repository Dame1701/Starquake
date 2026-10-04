// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     The water Olly sits in. Driven by <c>Shaders/Water.fx</c>; see <c>WaterEffect</c>.
/// </summary>
/// <remarks>
///     Olly's artwork runs from y 172 to y 330 of the playfield, so the waterline is set just
///     below the top of him - the measurement is what decides it rather than a number chosen by
///     eye, and it is why the surface lands around two thirds up the screen rather than exactly
///     half.
/// </remarks>
internal static class WaterSettings
{
    /// <summary>
    ///     Where the surface sits, in playfield pixels from the top. Olly's head starts at 172, so
    ///     this leaves the top of it in the air and the rest of him under.
    /// </summary>
    public const int WaterlineY = 200;

    /// <summary>
    ///     How far below the surface the water reaches its full colour, in playfield pixels. The
    ///     deeper the pixel the greener and darker it reads, which is what gives the water a
    ///     bottom rather than an even wash.
    /// </summary>
    public const int FullDepth = 230;

    /// <summary>
    ///     How far the water pushes what is behind it, as a fraction of the picture, and how
    ///     closely the waves are spaced. Small: a tenth of a percent of the screen is already a
    ///     visible wobble, and much more reads as a funhouse mirror.
    /// </summary>
    public const float Amplitude = 0.0045f;

    /// <inheritdoc cref="Amplitude" />
    public const float Frequency = 46f;

    /// <summary>How fast the waves travel, in radians per logic frame.</summary>
    private const double WaveSpeed = 0.075;

    /// <summary>
    ///     What the water does to the colour of what is behind it, and how much of that is
    ///     applied at full depth. A multiplier rather than a wash, so the room keeps its own
    ///     lightness and only loses its reds.
    /// </summary>
    public static Vector3 WaterColour => new(0.35f, 0.8f, 0.95f);

    /// <inheritdoc cref="WaterColour" />
    public const float Tint = 0.85f;

    /// <summary>What the line at the surface burns, and how thick it is as a fraction.</summary>
    public static Vector3 SurfaceColour => new(0.3f, 0.55f, 0.6f);

    /// <inheritdoc cref="SurfaceColour" />
    public const float SurfaceThickness = 0.007f;

    /// <summary>
    ///     Where the waves have travelled to by a given logic frame. A logic frame, so the water
    ///     moves at the same rate whatever the display is doing.
    /// </summary>
    public static float PhaseAt(int logicFrame) => (float)(logicFrame * WaveSpeed);

    /// <summary>
    ///     The waterline as a fraction of the picture, which is what the shader works in.
    /// </summary>
    public static float WaterlineIn01 => WaterlineY / (float)TileMetrics.PlayAreaHeight;

    /// <inheritdoc cref="WaterlineIn01" />
    public static float OverFullDepthIn01 => TileMetrics.PlayAreaHeight / (float)FullDepth;
}
