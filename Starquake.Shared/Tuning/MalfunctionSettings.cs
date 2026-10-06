// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The flight computer's display failing, which is what the report it is printing says has
///     happened to it. Driven by <c>Shaders/Malfunction.fx</c>; see <c>ComputerReportScreen</c>.
/// </summary>
/// <remarks>
///     A faint interference the whole time, and every two seconds or so a burst where the page
///     breaks up badly for a third of a second. The floor matters as much as the bursts: a display
///     that is perfectly steady between them reads as a working set with something being done to
///     it, where one that never quite settles reads as a set that is going.
///     <para>
///         Everything here is a function of the frame and nothing else, so it fails the same way
///         every time anybody sees it and the timing can be checked without running the game. The
///         frames are logic frames: <c>GameHost</c> draws once per vertical retrace, so a burst
///         counted in drawn frames would come round faster on a better monitor.
///     </para>
/// </remarks>
internal static class MalfunctionSettings
{
    /// <summary>
    ///     Roughly how long between bursts, in logic frames, and how far that wanders either side.
    /// </summary>
    /// <remarks>
    ///     A fault arriving on a fixed beat reads as something being done to the picture rather
    ///     than as something wrong with it, so the gap is jittered - by the burst's own number
    ///     rather than at random, which keeps it the same every time. The jitter is kept below the
    ///     period, which is what makes each window hold exactly one burst.
    /// </remarks>
    /// <seealso cref="JitterFor" />
    private static readonly int BurstPeriod = LogicClock.Seconds(2.1);

    /// <inheritdoc cref="BurstPeriod" />
    private static readonly int BurstJitter = LogicClock.Seconds(1.3);

    /// <summary>
    ///     How long a burst lasts. A third of a second: long enough to be read as the page coming
    ///     apart, short enough that the report is legible again before anybody gives up on it.
    /// </summary>
    private static readonly int BurstFrames = LogicClock.Seconds(0.34);

    /// <summary>
    ///     What the display does between bursts. Small, but not nought - see the remarks above.
    /// </summary>
    public const float Idle = 0.08f;

    /// <summary>
    ///     How many bands the page is cut into, and how closely the fine interference lines are
    ///     spaced, both counted down the page.
    /// </summary>
    /// <remarks>
    ///     The lines are nearly as close as the composition's own rows, so they read as
    ///     interference in the signal rather than as stripes laid over the page - the bands are the
    ///     coarse fault and these are the texture of it.
    /// </remarks>
    public static Vector2 BandScale => new(26f, 900f);

    /// <summary>
    ///     How far a band is thrown sideways at the worst, as a fraction of the width, and how far
    ///     the red and blue are pulled either side of it.
    /// </summary>
    public const float Slip = 0.085f;

    /// <inheritdoc cref="Slip" />
    public const float ChromaSplit = 0.006f;

    /// <summary>
    ///     What proportion of the bands lose their signal altogether at the worst, and the cold
    ///     grey a band that has lost it is left showing.
    /// </summary>
    public const float Dropout = 0.17f;

    /// <inheritdoc cref="Dropout" />
    public static Vector3 SignalColour => new(0.09f, 0.1f, 0.13f);

    /// <summary>How fast the bands travel and reshuffle, in bands per logic frame.</summary>
    private const double BandDrift = 0.37;

    /// <summary>
    ///     How far the picture slides in a frame once the vertical hold has let go, as a fraction
    ///     of the height.
    /// </summary>
    private const float RollSpeed = 0.055f;

    /// <summary>
    ///     How badly the computer is failing on a given frame, <see cref="Idle" /> between bursts
    ///     and one at the height of them.
    /// </summary>
    /// <remarks>
    ///     Flat at the top for the first two thirds of a burst and falling away over the last
    ///     third, so it snaps on and lets go, which is what a signal does. A burst that eased in
    ///     and out would read as the page being dissolved into something.
    /// </remarks>
    public static float BreakupAt(int logicFrame)
    {
        int into = FramesIntoBurst(logicFrame);

        if (into < 0)
        {
            return Idle;
        }

        float left = 1f - into / (float)BurstFrames;

        return Math.Max(Idle, Math.Min(1f, left * 3f));
    }

    /// <summary>
    ///     How far the picture has slid down by a given frame, as a fraction of its height, with
    ///     whatever runs off the bottom coming back round at the top.
    /// </summary>
    /// <remarks>
    ///     Only some bursts take the hold with them. One that always went would make the roll part
    ///     of what a burst is, and the picture's own position is the last thing a set gives up -
    ///     losing it every two seconds reads as a different fault from the one the bands are.
    /// </remarks>
    public static float RollAt(int logicFrame)
    {
        int into = FramesIntoBurst(logicFrame);

        if (into < 0 || !TheHoldGoesDuring(BurstContaining(logicFrame)))
        {
            return 0f;
        }

        // Wrapped here as well as in the shader, so what is handed over is the fraction it will
        // actually use rather than a number that grows for as long as the page is on screen.
        return into * RollSpeed % 1f;
    }

    /// <summary>
    ///     Where the bands have travelled to by a given frame, which is also what reshuffles which
    ///     of them are thrown and which have lost their signal.
    /// </summary>
    public static float PhaseAt(int logicFrame) => (float)(logicFrame * BandDrift);

    /// <summary>
    ///     Which burst a frame belongs to - the one whose window it falls in, whether or not that
    ///     burst has started by then.
    /// </summary>
    private static int BurstContaining(int logicFrame) => logicFrame / BurstPeriod;

    /// <summary>
    ///     How far into its window a burst arrives, in logic frames.
    /// </summary>
    /// <remarks>
    ///     A hash of the burst's number and not a multiple of it, which is the whole of why there
    ///     is a function here rather than an expression. A multiple taken modulo the jitter can
    ///     only ever step by two amounts - the multiplier, or the multiplier less the jitter - so
    ///     the gap between bursts alternates between two values and the fault is back on a beat,
    ///     a slower one with a limp in it. This is the same hash <c>Shaders/Malfunction.fx</c>
    ///     picks its bands with, and the two constants are that hash's.
    /// </remarks>
    private static int JitterFor(int burst)
    {
        double hash = Math.Sin(burst * 12.9898) * 43758.5453;

        return (int)((hash - Math.Floor(hash)) * BurstJitter);
    }

    /// <summary>
    ///     How many frames into its burst a frame is, or minus one if the display is between
    ///     bursts.
    /// </summary>
    private static int FramesIntoBurst(int logicFrame)
    {
        int burst = BurstContaining(logicFrame);
        int startsAt = burst * BurstPeriod + JitterFor(burst);
        int into = logicFrame - startsAt;

        return into >= 0 && into < BurstFrames ? into : -1;
    }

    /// <summary>
    ///     Whether this is one of the bursts that takes the vertical hold with it. Roughly a third
    ///     of them, picked by the burst's own number so it is the same every time.
    /// </summary>
    private static bool TheHoldGoesDuring(int burst) => burst * 7919 % 100 < 32;
}
