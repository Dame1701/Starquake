// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Tuning;

/// <summary>
///     The numbers the CRT shader is driven by. Arithmetic on the back buffer's size, kept apart from
///     <c>ScreenPresenter</c> so it can be checked without a graphics device, the same split
///     <see cref="PresentationSettings.TransformFor" /> is written for.
/// </summary>
internal static class CrtSettings
{
    /// <summary>
    ///     How dark the trough of a scanline goes. Nothing resamples this away, so it reads much
    ///     stronger than the same number would in composition space.
    /// </summary>
    public const float ScanlineDepth = 0.35f;

    /// <summary>How deep the phosphor stripes cut, per channel.</summary>
    public const float MaskDepth = 0.18f;

    /// <summary>
    ///     How much of the blur is added back on top of the picture. Additive, so this is how far a
    ///     bright pixel lifts its neighbours rather than how much of the picture is blurred.
    /// </summary>
    public const float GlowAmount = 0.45f;

    /// <summary>
    ///     How far the glow reaches, in output pixels. Taken diagonally, see <c>Crt.fx</c>'s
    ///     <c>Glow</c>, so the halo is about this wide in every direction rather than this wide on
    ///     the axes and further at the corners.
    /// </summary>
    public const float GlowRadius = 2.6f;

    /// <summary>
    ///     How far the tube bows. A tenth is already a fishbowl at this aspect ratio: the picture is
    ///     wide, so the corners travel much further than the sides and the bow reads as stronger than
    ///     the number suggests.
    /// </summary>
    public const float Curvature = 0.022f;

    /// <summary>How much the corners fall away.</summary>
    public const float VignetteAmount = 0.12f;

    /// <summary>
    ///     How long the phosphor takes to fall to about a third of its brightness once the beam has
    ///     moved on. What makes a moving object streak slightly rather than appear in a new place
    ///     every frame.
    /// </summary>
    /// <remarks>
    ///     A little over one logic frame, with the trail gone inside three: a smear behind something
    ///     moving rather than a second copy following it. Much longer and the picture reads as being
    ///     dragged.
    /// </remarks>
    public const double PhosphorDecaySeconds = 0.020;

    /// <summary>
    ///     One red, green and blue stripe span, in output pixels. Three, so each channel owns a whole
    ///     pixel and the grille cannot land between two.
    /// </summary>
    public const float MaskPeriod = 3f;

    /// <summary>How many output pixels one light-and-dark pair spans.</summary>
    /// <remarks>
    ///     Rounded to a whole number of output pixels, which keeps the lines the same thickness
    ///     everywhere on screen. A period in composition space scaled by a fractional amount becomes
    ///     a grey wash with a beat in it. Never less than two, because one on and one off is the
    ///     thinnest a scanline can be.
    /// </remarks>
    public static int ScanlinePeriodFor(float scale)
        => Math.Max(2, (int)Math.Round(2 * scale, MidpointRounding.AwayFromZero));

    /// <summary>
    ///     What the picture is multiplied by to put back the light the scanlines and the grille take
    ///     out, so turning the effect on barely dims the picture rather than costing a third of it.
    /// </summary>
    /// <remarks>
    ///     Both patterns are a <c>lerp</c> from <c>1 - depth</c> to <c>1</c> across a smoothed
    ///     triangle wave, whose mean over one period is exactly a half: the triangle is uniform over
    ///     nought to one, and the mean of <c>x * x * (3 - 2x)</c> over that range is <c>1 - 1/2</c>.
    ///     So each pattern costs <c>depth / 2</c> of the light on average, and the gain is the
    ///     reciprocal of what is left.
    /// </remarks>
    public static float GainFor(CrtMode mode)
    {
        float remaining = 1 - ScanlineDepth / 2;

        if (mode is CrtMode.Phosphor or CrtMode.Tube)
        {
            remaining *= 1 - MaskDepth / 2;
        }

        return 1 / remaining;
    }

    /// <summary>
    ///     Where the composition sits inside the back buffer, in texture coordinates: xy the corner,
    ///     zw the size. The same scale and border <see cref="PresentationSettings.TransformFor" />
    ///     works out, as a fraction of the buffer rather than a matrix, because the shader bows the
    ///     picture about the middle of the composition and not the middle of the window.
    /// </summary>
    public static Vector4 CompositionRectFor(int backBufferWidth, int backBufferHeight)
    {
        float scale = ScaleFor(backBufferWidth, backBufferHeight);

        float width = CompositionSize.Width * scale;
        float height = CompositionSize.Height * scale;

        return new Vector4(
            (backBufferWidth - width) / 2f / backBufferWidth,
            (backBufferHeight - height) / 2f / backBufferHeight,
            width / backBufferWidth,
            height / backBufferHeight);
    }

    /// <inheritdoc cref="PresentationSettings.TransformFor" />
    public static float ScaleFor(int backBufferWidth, int backBufferHeight)
        => Math.Min(
            backBufferWidth / (float)CompositionSize.Width,
            backBufferHeight / (float)CompositionSize.Height);

    /// <summary>
    ///     Which pass of the shader a mode is drawn with. Separate techniques rather than one shader
    ///     with branches, because <c>ps_2_0</c> has no dynamic branching, so each mode compiles to
    ///     only the work it needs.
    /// </summary>
    public static string TechniqueFor(CrtMode mode)
        => mode switch
        {
            CrtMode.Scanlines => "Scanlines",
            CrtMode.Phosphor => "Phosphor",
            _ => "Tube"
        };

    /// <summary>How much of the last picture is still glowing this long afterwards.</summary>
    /// <remarks>
    ///     An exponential decay over elapsed time rather than a fixed fraction per frame, which is
    ///     the only reason the time is taken at all. The game draws once per vertical retrace, so a
    ///     fixed fraction would leave the streak less than half as long at 144 hertz as at 60.
    ///     <para>
    ///         It compounds, which is what makes that work: decaying twice over half an interval
    ///         leaves exactly what decaying once over the whole interval leaves, so a frame rate that
    ///         varies from moment to moment does not change the result.
    ///     </para>
    /// </remarks>
    public static float AfterglowRetainedOver(double seconds)
        => (float)Math.Exp(-seconds / PhosphorDecaySeconds);

    /// <summary>
    ///     Whether a mode glows on after the beam has passed. The plain scanline mode does not: each
    ///     press of F9 adds one thing, and the persistence belongs with the phosphor.
    /// </summary>
    public static bool ShowsAfterglow(CrtMode mode)
        => mode is CrtMode.Phosphor or CrtMode.Tube;

    /// <summary>
    ///     What to call a mode on screen when F9 steps onto it. Capitals and spaces only, because the
    ///     bitmap font has only the characters it was built with.
    /// </summary>
    public static string NameOf(CrtMode mode)
        => mode switch
        {
            CrtMode.None => "CRT OFF",
            CrtMode.Scanlines => "CRT SCANLINES",
            CrtMode.Phosphor => "CRT PHOSPHOR",
            _ => "CRT FULL TUBE"
        };

    /// <summary>
    ///     The next mode F9 steps to. Each press adds one thing and the fourth takes them all away.
    /// </summary>
    public static CrtMode Next(CrtMode mode)
        => mode switch
        {
            CrtMode.None => CrtMode.Scanlines,
            CrtMode.Scanlines => CrtMode.Phosphor,
            CrtMode.Phosphor => CrtMode.Tube,
            _ => CrtMode.None
        };
}
