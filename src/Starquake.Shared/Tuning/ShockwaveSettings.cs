// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The shape of the ring a blast throws out. Driven by <c>Shaders/Shockwave.fx</c>; see
///     <c>Shockwave</c> for where it is handed over.
/// </summary>
/// <remarks>
///     Two sizes of one blast: Blob coming apart throws a wider ring than a machine and holds it
///     slightly longer. Everything else is shared, because they are the same event at two scales
///     rather than two effects.
/// </remarks>
internal static class ShockwaveSettings
{
    /// <summary>
    ///     How far across Blob's ring reaches, in pixels, and how long it takes to get there.
    /// </summary>
    /// <remarks>
    ///     Deliberately wider than the playfield's 1024 by 576. The ring should run off the edges of
    ///     the room and be gone; narrow it and it stops inside the room and fades, which reads as a
    ///     ring being switched off rather than a blast leaving. Only the part of the quad landing on
    ///     the playfield is shaded, so the extra width costs nothing.
    /// </remarks>
    public const int BlobDiameter = 1400;

    /// <inheritdoc cref="BlobDiameter" />
    public static readonly int BlobFrames = LogicClock.Seconds(0.6);

    /// <summary>
    ///     What a machine throws. Smaller and over sooner, because a spiky nasty going off happens in
    ///     a corner of the room rather than to the whole of it and several can go off at once. Still
    ///     wide enough to leave the room from anywhere in it.
    /// </summary>
    public const int NastyDiameter = 1100;

    /// <inheritdoc cref="NastyDiameter" />
    public static readonly int NastyFrames = LogicClock.Seconds(0.5);

    /// <summary>
    ///     What the ring burns outside its crest: the ripples and the outer skirt of the front. The
    ///     orange of the flames the explosion already throws, so the ring is the blast's own light
    ///     rather than a second idea about it.
    /// </summary>
    /// <remarks>
    ///     Warm rather than the cold blue of the electricity and the jump platforms. A shockwave here
    ///     is fire going out, and a blue ring over orange flames reads as two unrelated things
    ///     happening in one place.
    /// </remarks>
    public static Vector3 EdgeColour => new(1f, 0.55f, 0.08f);

    /// <summary>
    ///     What the crest of the front burns. Nearly white but still yellow, so the front reads as
    ///     the hot part of the same fire rather than a white ring with an orange one behind it.
    /// </summary>
    public static Vector3 CoreColour => new(1f, 0.95f, 0.72f);

    /// <summary>
    ///     The difference between the two, which is what the shader is handed. It adds a share of
    ///     this to <see cref="EdgeColour" /> rather than mixing between the pair, because a lerp
    ///     costs three instruction slots the pass cannot spare.
    /// </summary>
    public static Vector3 ColourLift => CoreColour - EdgeColour;

    /// <summary>
    ///     How hard the ring burns at the moment of the blast. Over one on purpose, like the splat's
    ///     rim, so it blooms in the cathode ray tube rather than merely being bright.
    /// </summary>
    public const float Brightness = 1.4f;

    /// <summary>
    ///     How thick the hot front is, as a fraction of how far out it has got. A thin front with a
    ///     long tail reads as something travelling, but halve this and it becomes a filament: a blast
    ///     that throws a thread is not throwing much.
    /// </summary>
    public const float FrontThickness = 0.075f;

    /// <summary>
    ///     How far behind the front the ripples reach, as a fraction of the radius, and how many
    ///     crests span that. Five: two reads as a mistake in the front, and many more is corrugation
    ///     rather than a wake.
    /// </summary>
    public const float Tail = 0.75f;

    /// <inheritdoc cref="Tail" />
    public const int Ripples = 5;

    /// <summary>
    ///     How bright those ripples are against the front. Under one, since they are what the blast
    ///     left behind rather than more fronts, but not by much, or a ring thick enough to see leaves
    ///     nothing behind it worth seeing.
    /// </summary>
    public const float TrailShare = 0.95f;

    /// <summary>
    ///     What the shader is handed for that. Its crests are a triangle peaking at a half, squared
    ///     and so peaking at a quarter, and correcting for that here costs nothing where doing it per
    ///     pixel costs a slot the pass cannot spare.
    /// </summary>
    public static float TrailWeight => TrailShare * TrianglePeakOver;

    /// <inheritdoc cref="TrailWeight" />
    private const float TrianglePeakOver = 4f;

    /// <summary>
    ///     How softly the front arrives, as a fraction of the quad. Two or three pixels at the sizes
    ///     these are drawn at: enough to take the stair-stepping off the circle and no more. Scale it
    ///     to the front's thickness instead and the softening lands over the crest rather than the
    ///     edge, where the two cancel and the ring never reaches the brightness it was asked for.
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
    ///     The wave count buys the roundness and the amplitude has to come down with it, or fine
    ///     waves only make corrugated iron. Too few undulations at too great an amplitude gives a
    ///     flower, an oval, or a turning cog rather than a shimmer.
    /// </remarks>
    private const float Wobble = 0.008f;

    /// <summary>
    ///     How fast those waves travel round the rim, in radians of their own phase per logic frame.
    ///     The two go round at different rates in opposite directions, so what is seen is one sliding
    ///     through the other, which is what shimmers. Fast, because a shimmer that crawls is just a
    ///     bent ring.
    /// </summary>
    private const double EdgeSpeed = 0.8;

    /// <summary>
    ///     How fast the ripples travel through the envelope, in whole crests per logic frame. Crests
    ///     rather than radians, because the shader makes the wave with a frac rather than a cosine.
    ///     About four crests pass in the life of a ring.
    /// </summary>
    private const double WaveSpeed = 0.07;

    /// <summary>
    ///     The smallest radius the shader is ever handed. The front's thickness and the ripple
    ///     spacing are both measured against the radius, so a ring of nought would divide by it.
    /// </summary>
    private const float SmallestRadius = 0.05f;

    /// <summary>
    ///     How much of the expansion is thrown rather than even, from nought for a ring that grows
    ///     at one rate to one for a ring that does nearly all its travelling in its first quarter.
    /// </summary>
    /// <remarks>
    ///     Some deceleration is wanted, because a ring expanding evenly reads as a picture being
    ///     drawn larger rather than something thrown. Fully thrown is too much: it puts nearly half
    ///     the travel into the first quarter second, so the blast is over before there is anything to
    ///     watch.
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
    ///     How hard the ring burns on a given frame. Holds near full for the first part of its travel
    ///     then goes quickly, so it reads as a blast dying rather than a light turned down from the
    ///     moment it is struck.
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
    ///     cosine and a sine, which is all the shader needs to swing a harmonic it already has to the
    ///     right phase.
    /// </summary>
    /// <param name="attitude">
    ///     Where this blast started them, in radians. Dealt when it goes off, so two rings in the
    ///     same room do not shimmer in step.
    /// </param>
    public static Vector2 ShimmerTurnAt(int frame, float attitude)
        => Turned(attitude + (float)(frame * EdgeSpeed)) * Wobble;

    /// <inheritdoc cref="ShimmerTurnAt" />
    /// <remarks>
    ///     The finer wave, running the other way round the rim at a different rate, so the two never
    ///     come back into step and the edge keeps finding a new shape.
    /// </remarks>
    public static Vector2 FineShimmerTurnAt(int frame, float attitude)
        => Turned(attitude * 1.7f - (float)(frame * EdgeSpeed * FineShimmerRate))
           * (Wobble * FineShimmerShare);

    /// <summary>
    ///     How much of the wobble the finer wave takes, and how fast it travels against the coarser
    ///     one. Both under the whole, so the finer rides on the coarser rather than fighting it, and
    ///     the differing rate keeps the two from coming back into step.
    /// </summary>
    private const float FineShimmerShare = 0.7f;

    /// <inheritdoc cref="FineShimmerShare" />
    private const double FineShimmerRate = 0.7;

    private static Vector2 Turned(float phase)
        => new((float)Math.Cos(phase), (float)Math.Sin(phase));

    /// <summary>
    ///     The reciprocal of the front's thickness at a given radius, worked out here to save a
    ///     divide on every pixel of the quad.
    /// </summary>
    public static float OverFrontAt(float radius) => 1f / (radius * FrontThickness);

    /// <inheritdoc cref="OverFrontAt" />
    public static float OverTailAt(float radius) => 1f / (radius * Tail);

    /// <summary>
    ///     How closely the ripples are spaced at a given radius, in whole crests per unit of distance
    ///     behind the front. Measured against the radius, so the train stretches with the ring
    ///     instead of the crests bunching into a smear as it grows.
    /// </summary>
    public static float WaveRateAt(float radius) => Ripples / (radius * Tail);
}
