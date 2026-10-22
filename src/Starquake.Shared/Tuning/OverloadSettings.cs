// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     What Blob overloading looks like. Driven by <c>Shaders/Overload.fx</c>; see
///     <c>Blob.DrawOverloadAlarmState</c> and <c>Blob.DrawOverloadFlare</c> for where it is handed
///     over.
/// </summary>
/// <remarks>
///     Blob's artwork is greyscale, with red, green and blue equal at every texel, which is why the
///     game has always tinted him and why a heat ramp has no hue to fight. The alarm runs from ember
///     through orange to white-hot as the charge climbs.
/// </remarks>
internal static class OverloadSettings
{

    /// <summary>How hard the rim burns.</summary>
    public const float RimAmount = 2.2f;

    /// <summary>How far out the rim is looked for, in the sprite's own pixels.</summary>
    public const float RimRadius = 1.6f;

    /// <summary>
    ///     How much of the inside of a discharge shell shows through its front. Low on purpose: five
    ///     copies of a solid Blob read as five Blobs, where five outlines read as a blast.
    /// </summary>
    public const float InteriorWash = 0.18f;

    /// <summary>How hard the light out of the cracks burns.</summary>
    public const float CrackBrightness = 2.6f;

    /// <summary>How wide a crack is once fully open.</summary>
    public const float CrackWidth = 0.55f;

    /// <summary>
    ///     How many cracks run across Blob. They are contours of one wandering field, so this is how
    ///     closely those contours are taken rather than a count of separate lines.
    /// </summary>
    public const float CrackDensity = 0.42f;

    /// <summary>How many beams whip around Blob as he comes apart.</summary>
    public const int BeamCount = 5;

    /// <summary>
    ///     How big the arc artwork is drawn as a beam. Smaller than a node's, which spans a whole
    ///     tile, because these are sparks off Blob rather than a gate.
    /// </summary>
    public const float BeamScale = 0.7f;

    /// <summary>How many frames the electricity artwork has, so the beams crawl as well as turn.</summary>
    public const int BeamArcFrames = 10;

    /// <summary>How fast they go round, in radians per logic frame.</summary>
    private const double BeamSpin = 0.42;

    /// <summary>How far out they are thrown by the time Blob has come apart, in pixels.</summary>
    private const float BeamReach = 46f;

    /// <summary>How far Blob shakes at worst, in pixels.</summary>
    private const float ShakeDistance = 3.5f;
    /// <summary>The coldest stop of the heat ramp: Blob before any charge has built.</summary>
    public static Vector3 EmberColour => new(0.5f, 0.12f, 0.06f);

    /// <inheritdoc cref="EmberColour" />
    public static Vector3 MidColour => new(1f, 0.5f, 0.1f);

    /// <inheritdoc cref="EmberColour" />
    public static Vector3 HotColour => new(1f, 0.97f, 0.85f);

    /// <summary>
    ///     The skin of charge crawling over Blob. Electric blue against the heat, because the two
    ///     must be told apart at a glance: he burns from within and arcs on the outside.
    /// </summary>
    public static Vector3 RimColour => new(0.55f, 0.85f, 1f);

    /// <summary>What comes out of the cracks as he splits.</summary>
    public static Vector3 CrackColour => new(1f, 0.95f, 0.8f);

    /// <summary>
    ///     Where one beam sits relative to Blob's middle, and which way it points.
    /// </summary>
    /// <remarks>
    ///     Spread evenly around Blob and all turning together, so they read as a field whipping round
    ///     rather than separate sparks. Thrown further out as the split runs, so the beams open with
    ///     the cracks instead of staying where they started.
    ///     <para>
    ///         A function of the logic frame and the split alone, so they are identical on every
    ///         machine, can be checked without running the game, and turn at the same rate whatever
    ///         the display is doing.
    ///     </para>
    /// </remarks>
    public static (Vector2 Offset, float Angle) BeamAt(int logicFrame, int beam, float split)
    {
        float turned = (float)(logicFrame * BeamSpin + beam * (2 * Math.PI / BeamCount));
        float reach = BeamReach * Math.Clamp(split, 0f, 1f);

        return (
            new Vector2((float)Math.Cos(turned) * reach, (float)Math.Sin(turned) * reach),
            turned);
    }

    /// <summary>
    ///     How hard Blob is shaking on a given logic frame, growing as the cracks widen.
    /// </summary>
    /// <remarks>
    ///     Two sines at rates that do not divide one another, one per axis and offset from each
    ///     other, so the two do not trace a line. A function of the logic frame and the split alone,
    ///     so it is identical on every machine, can be checked without running the game, and shakes
    ///     at the same rate whatever the display is doing.
    /// </remarks>
    public static Vector2 ShakeAt(int logicFrame, float split)
    {
        float distance = ShakeDistance * Math.Clamp(split, 0f, 1f);

        return new Vector2(
            (float)Math.Sin(logicFrame * 2.7) * distance,
            (float)Math.Sin(logicFrame * 3.9 + 2.1) * distance);
    }

    /// <summary>
    ///     How far through the overload the alarm has got, taken from the energy drain the alarm
    ///     state already counts down. That runs from full to nothing, so the charge runs the other
    ///     way.
    /// </summary>
    public static float ChargeFrom(int energyDrainCounter)
        => Math.Clamp(1f - energyDrainCounter / (float)ColourLevels.Full, 0f, 1f);

    /// <summary>
    ///     What colour the shell at a given place in the flare burns. The innermost is white-hot and
    ///     they cool straight out to the arcing blue, so the five read as one front expanding rather
    ///     than five separate things.
    /// </summary>
    /// <remarks>
    ///     Deliberately not routed through <see cref="MidColour" />. The orange belongs to the heat
    ///     building inside Blob, where ember to orange to white is what something getting hotter
    ///     does. A discharge is electrical rather than hot: white at the core and blue at the edge,
    ///     like any spark. Going through orange makes blue fall and then rise again, so the five read
    ///     as two unrelated things. A test asserts they cool all the way out.
    /// </remarks>
    public static Vector3 ShellColour(int copy, int copies)
        => Vector3.Lerp(HotColour, RimColour, copies > 1 ? copy / (float)(copies - 1) : 0f);
}
