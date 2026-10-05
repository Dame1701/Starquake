// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Display;

/// <summary>
///     The shape of the ring a blast throws out. Driven by <c>Shaders/Shockwave.fx</c>; see
///     <see cref="Shockwave" /> for where it is handed over.
/// </summary>
/// <remarks>
///     Two sizes of the one blast: Blob coming apart throws a wider ring than a machine does and
///     holds it a little longer. Everything else about them is shared, because they are the same
///     event at two scales rather than two effects.
/// </remarks>
internal static class ShockwaveSettings
{
    /// <summary>
    ///     How far across Blob's ring reaches, in pixels, and how long it takes to get there.
    /// </summary>
    /// <remarks>
    ///     Wider than the playfield, which is 1024 by 576: the ring is meant to run off the edges
    ///     of the room and be gone, not to stop somewhere inside it and fade. At two thirds of
    ///     this it did the second, which is a ring being switched off rather than a blast leaving.
    ///     Only the part of the quad that lands on the playfield is ever shaded, so the width
    ///     past the edges costs nothing.
    /// </remarks>
    public const int BlobDiameter = 1400;

    /// <inheritdoc cref="BlobDiameter" />
    public static readonly int BlobFrames = LogicClock.Seconds(0.6);

    /// <summary>
    ///     What a machine throws. Smaller and over sooner: a spiky nasty going off is a thing that
    ///     happens in a corner of the room rather than to the whole of it, and several of them can
    ///     be going off at once. Still wide enough to leave the room from anywhere in it.
    /// </summary>
    public const int NastyDiameter = 1100;

    /// <inheritdoc cref="NastyDiameter" />
    public static readonly int NastyFrames = LogicClock.Seconds(0.5);

    /// <summary>
    ///     What the ring burns outside its crest - the ripples, and the outer skirt of the front.
    ///     The orange of the flames the explosion is already throwing, so the ring is the blast's
    ///     own light rather than a second idea about it.
    /// </summary>
    /// <remarks>
    ///     This was the cold blue of the electricity and the jump platforms for a while, on the
    ///     argument that a shockwave is energy. It is not: a shockwave here is fire going out, and
    ///     a blue ring over orange flames read as two unrelated things happening in one place.
    ///     The ring the particles used to draw was warm, and it was right to be.
    /// </remarks>
    public static Vector3 EdgeColour => new(1f, 0.55f, 0.08f);

    /// <summary>
    ///     What the crest of the front burns. Nearly white but still yellow, so the front reads as
    ///     the hot part of the same fire rather than as a white ring with an orange one behind it.
    /// </summary>
    public static Vector3 CoreColour => new(1f, 0.95f, 0.72f);

    /// <summary>
    ///     The difference between the two, which is what the shader is actually handed: it adds a
    ///     share of this to <see cref="EdgeColour" /> rather than mixing between the pair, because
    ///     a lerp costs three of the slots it has none of to spare.
    /// </summary>
    public static Vector3 ColourLift => CoreColour - EdgeColour;

    /// <summary>
    ///     How hard the ring burns at the moment of the blast. Over one on purpose, as the splat's
    ///     rim is: it is meant to bloom in the cathode ray tube rather than merely be bright.
    /// </summary>
    public const float Brightness = 1.4f;

    /// <summary>
    ///     How thick the hot front is, as a fraction of how far out it has got. A thin front with
    ///     a long tail is what reads as something travelling - but at half this it was a filament,
    ///     and a blast that throws a thread is not throwing much.
    /// </summary>
    public const float FrontThickness = 0.075f;

    /// <summary>
    ///     How far back behind the front the ripples still reach, as a fraction of the radius, and
    ///     how many crests are spaced across that. Five: two reads as a mistake in the front, and
    ///     many more than this is corrugation rather than a wake.
    /// </summary>
    public const float Tail = 0.75f;

    /// <inheritdoc cref="Tail" />
    public const int Ripples = 5;

    /// <summary>
    ///     How bright those ripples are against the front. Under one - they are what the blast
    ///     left behind it rather than more fronts - but not by much, or a ring thick enough to
    ///     see leaves nothing behind it worth seeing.
    /// </summary>
    public const float TrailShare = 0.95f;

    /// <summary>
    ///     What the shader is actually handed for that. Its crests are a triangle peaking at a
    ///     half, squared and so peaking at a quarter, and putting that back here costs nothing
    ///     where doing it per pixel costs a slot it has none of to spare.
    /// </summary>
    public static float TrailWeight => TrailShare * TrianglePeakOver;

    /// <inheritdoc cref="TrailWeight" />
    private const float TrianglePeakOver = 4f;

    /// <summary>
    ///     How softly the front arrives, as a fraction of the quad. Two or three pixels at the
    ///     sizes these are drawn at: enough to take the stair-stepping off the circle, and no
    ///     more. It was a tenth of this effect's thickness at first, which put the softening over
    ///     the crest instead of over the edge - the two cancelled, and the brightest the ring ever
    ///     got was two fifths of what it was asked for.
    /// </summary>
    public const float FrontSoftness = 0.0035f;

    /// <summary>
    ///     How much of the quad's outer edge the ring is taken out over, so the last of it fades
    ///     rather than meeting the corner and showing the square it was drawn in.
    /// </summary>
    public const float RimFade = 0.1f;

    /// <summary>
    ///     How far out of round the two waves running round the rim pull the ring, as a fraction
    ///     of its radius. Handed to the shader folded into the waves themselves - see
    ///     <see cref="ShimmerTurnAt" />.
    /// </summary>
    /// <remarks>
    ///     A twentieth of the radius over six and eight undulations was a flower, and before that
    ///     a twentieth over two and four was an oval turning into a rounded square. Sixteen and
    ///     thirty-two at a fifth of that was a shimmer at last, and still read as a cog turning.
    ///     The waves are thirty-two and sixty-four now and this is a third less again: the count
    ///     is what buys the roundness, and the amplitude has to come down with it or fine waves
    ///     only make corrugated iron.
    /// </remarks>
    private const float Wobble = 0.008f;

    /// <summary>
    ///     How fast those waves travel round it, in radians of their own phase per logic frame.
    ///     The two go round at different rates and in opposite directions, so what is seen is the
    ///     one sliding through the other - which is what shimmers. Fast: thirty-two undulations
    ///     turned by this much a frame move the pattern about a hundredth of a turn, and a
    ///     shimmer that crawls is just a bent ring.
    /// </summary>
    private const double EdgeSpeed = 0.8;

    /// <summary>
    ///     How fast the ripples travel through the envelope, in whole crests per logic frame -
    ///     crests rather than radians, because the shader makes the wave with a frac rather than
    ///     a cosine. About four crests pass in the life of a ring.
    /// </summary>
    private const double WaveSpeed = 0.07;

    /// <summary>
    ///     The smallest radius the shader is ever handed. The front's thickness and the ripple
    ///     spacing are both measured against the radius, so a ring of nought would divide by it -
    ///     and a blast is a hole in the air before it is a ring anyway.
    /// </summary>
    private const float SmallestRadius = 0.05f;

    /// <summary>
    ///     How much of the expansion is thrown rather than even, from nought for a ring that grows
    ///     at one rate to one for a ring that does nearly all its travelling in its first quarter.
    /// </summary>
    /// <remarks>
    ///     It was wholly thrown to begin with, which put nearly half the ring's travel into its
    ///     first quarter second - the blast was over before there was anything to watch. Some
    ///     deceleration is still wanted, because a ring that expands evenly reads as a picture
    ///     being drawn larger rather than as something that was thrown.
    /// </remarks>
    private const float Thrown = 0.6f;

    /// <summary>
    ///     How far out the front has got by a given frame, as a fraction of the quad's half-width.
    /// </summary>
    public static float RadiusAt(int frame, int frames)
    {
        float through = Math.Clamp(frame / (float)frames, 0f, 1f);
        float left = 1f - through;

        float thrown = 1f - left * left;

        return Math.Max(thrown * Thrown + through * (1f - Thrown), SmallestRadius);
    }

    /// <summary>
    ///     How hard the ring burns on a given frame. Holds near full for the first part of its
    ///     travel and then goes quickly, so it is a blast dying rather than a light being turned
    ///     down from the moment it is struck.
    /// </summary>
    public static float IntensityAt(int frame, int frames)
    {
        float through = Math.Clamp(frame / (float)frames, 0f, 1f);

        return Brightness * (1f - through * through * through);
    }

    /// <summary>Where the ripples have travelled to by a given frame.</summary>
    public static float WavePhaseAt(int frame) => (float)(frame * WaveSpeed);

    /// <summary>
    ///     Where each of the two waves running round the rim has turned to by a given frame, as a
    ///     cosine and a sine - which is all the shader needs to swing a harmonic it already has to
    ///     the phase it should be at.
    /// </summary>
    /// <param name="attitude">
    ///     Where this blast started them, in radians. Dealt when it goes off, so two rings in the
    ///     same room are not shimmering in step.
    /// </param>
    public static Vector2 ShimmerTurnAt(int frame, float attitude)
        => Turned(attitude + (float)(frame * EdgeSpeed)) * Wobble;

    /// <inheritdoc cref="ShimmerTurnAt" />
    /// <remarks>
    ///     The sixty-four-fold wave: the other way round the rim and at a different rate, so the
    ///     two never come back into step and the edge keeps finding a new shape.
    /// </remarks>
    public static Vector2 FineShimmerTurnAt(int frame, float attitude)
        => Turned(attitude * 1.7f - (float)(frame * EdgeSpeed * FineShimmerRate))
           * (Wobble * FineShimmerShare);

    /// <summary>
    ///     How much of the wobble the finer wave takes, and how fast it goes round against the
    ///     coarser one. Both under the whole, so the finer rides on the coarser rather than
    ///     fighting it - and the rate is what keeps the two from ever coming back into step.
    /// </summary>
    private const float FineShimmerShare = 0.7f;

    /// <inheritdoc cref="FineShimmerShare" />
    private const double FineShimmerRate = 0.7;

    private static Vector2 Turned(float phase)
        => new((float)Math.Cos(phase), (float)Math.Sin(phase));

    /// <summary>
    ///     The reciprocal of the front's thickness at a given radius, worked out here rather than
    ///     costing a divide on every pixel of the quad.
    /// </summary>
    public static float OverFrontAt(float radius) => 1f / (radius * FrontThickness);

    /// <inheritdoc cref="OverFrontAt" />
    public static float OverTailAt(float radius) => 1f / (radius * Tail);

    /// <summary>
    ///     How closely the ripples are spaced at a given radius, in whole crests per unit of
    ///     distance behind the front. Measured against the radius, so the train stretches with
    ///     the ring instead of the crests bunching up into a smear as it grows.
    /// </summary>
    public static float WaveRateAt(float radius) => Ripples / (radius * Tail);
}
