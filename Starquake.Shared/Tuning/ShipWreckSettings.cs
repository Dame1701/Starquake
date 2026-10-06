// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The wreck of Blob's ship spitting the occasional spark, which is all that happens to it for
///     the rest of the game beyond the smoke.
/// </summary>
/// <remarks>
///     Sparks rather than arcs. Arcs were tried and are the wrong thing: an arc is electricity
///     doing its job - two points with something live between them, which is what a node or a gate
///     has - where a wreck has no circuit left to speak of and only spits.
///     <para>
///         Everything here is a function of the frame and nothing else, so it sparks the same way
///         on every machine and the timing can be checked without running the game. The frames are
///         logic frames: <c>GameHost</c> draws once per vertical retrace, so a spark counted in
///         drawn frames would come round faster on a better monitor.
///     </para>
/// </remarks>
internal static class ShipWreckSettings
{
    /// <summary>
    ///     Roughly how long between sparks, in logic frames, and how far that wanders either side.
    /// </summary>
    /// <remarks>
    ///     A wreck sparking on a fixed beat reads as a machine working rather than one broken, so
    ///     the gap is jittered - by the spark's own number rather than at random, which keeps it
    ///     the same every time anybody plays. The jitter is kept below the period, which is what
    ///     makes each window hold exactly one spark.
    /// </remarks>
    private static readonly int SparkPeriod = LogicClock.Seconds(2.4);

    /// <inheritdoc cref="SparkPeriod" />
    private static readonly int SparkJitter = LogicClock.Seconds(1.6);

    /// <summary>
    ///     How far across and down the hull a spark can come from, as a fraction of it. Short of
    ///     the whole, so they come off the ship rather than the air beside it.
    /// </summary>
    private const float AcrossTheHull = 0.55f;

    /// <summary>
    ///     Whether this is the frame a spark goes off on. True for one frame at a time - the burst
    ///     itself then lives its own half second as a particle effect.
    /// </summary>
    public static bool SparksThisFrame(int logicFrame)
    {
        int spark = logicFrame / SparkPeriod;

        return logicFrame == spark * SparkPeriod + spark * 37 % SparkJitter;
    }

    /// <summary>
    ///     Where on the hull this spark comes from, relative to the middle of it.
    /// </summary>
    public static Point SparkAt(int logicFrame, int hullWidth, int hullHeight)
    {
        int spark = logicFrame / SparkPeriod;
        int seed = spark * 7919;

        float across = (seed % 1000 / 1000f - 0.5f) * AcrossTheHull;
        float down = (seed / 1000 % 1000 / 1000f - 0.5f) * AcrossTheHull;

        return new Point((int)(across * hullWidth), (int)(down * hullHeight));
    }
}
