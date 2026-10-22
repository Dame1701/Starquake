// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;

namespace Starquake.Tuning;

/// <summary>
///     The flight computer's display failing, which is what the report it is printing says has
///     happened to it. Driven by <c>Shaders/Malfunction.fx</c>; see <c>ComputerReportScreen</c>.
///     The music on that screen is bent by the same bursts: see <see cref="MusicPitchAt" />.
/// </summary>
/// <remarks>
///     A faint interference the whole time, with a burst every two seconds or so where the page
///     breaks up badly for a third of a second. The floor matters as much as the bursts: a display
///     perfectly steady between them reads as a working set being interfered with, where one that
///     never quite settles reads as a set that is failing.
///     <para>
///         Everything here is a function of the frame alone, so it fails the same way every time and
///         the timing can be checked without running the game. Logic frames, because
///         <c>GameHost</c> draws once per vertical retrace and a burst counted in drawn frames would
///         come round faster on a better monitor.
///     </para>
/// </remarks>
internal static class MalfunctionSettings
{

    /// <summary>
    ///     What the display does between bursts. Small but deliberately not nought, so the set reads
    ///     as failing rather than as being interfered with.
    /// </summary>
    public const float Idle = 0.08f;

    /// <summary>
    ///     How far a band is thrown sideways at worst, as a fraction of the width, and how far the
    ///     red and blue are pulled either side of it.
    /// </summary>
    public const float Slip = 0.085f;

    /// <inheritdoc cref="Slip" />
    public const float ChromaSplit = 0.006f;

    /// <summary>
    ///     What proportion of the bands lose their signal altogether at worst, and the cold grey a
    ///     band that has lost it is left showing.
    /// </summary>
    public const float Dropout = 0.17f;

    /// <summary>How fast the bands travel and reshuffle, in bands per logic frame.</summary>
    private const double BandDrift = 0.37;

    /// <summary>
    ///     How far the picture slides in a frame once the vertical hold has let go, as a fraction of
    ///     the height.
    /// </summary>
    private const float RollSpeed = 0.055f;

    /// <summary>
    ///     How far flat the music is pulled at the height of a burst, where one would be an octave
    ///     down. The playback speed goes with the pitch, so the piece drags as it sags.
    /// </summary>
    private const float PitchDive = 0.45f;

    /// <summary>
    ///     How far the pitch wanders either side of true the rest of the time, and how long a round
    ///     trip takes. The sound's answer to <see cref="Idle" />. A piece playing perfectly steady
    ///     between bursts is coming off a machine that works.
    /// </summary>
    private const float Flutter = 0.015f;

    /// <summary>
    ///     How loud the music is left at the height of a burst, and at the height of one that takes
    ///     the vertical hold with it. Those are the worse fault, so they take more of the sound.
    /// </summary>
    private const float DuckedVolume = 0.4f;

    /// <inheritdoc cref="DuckedVolume" />
    private const float LostVolume = 0.08f;

    /// <summary>
    ///     How far towards one speaker the music is thrown at the height of a burst, where one would
    ///     be entirely in it. <see cref="ChromaSplit" /> in sound.
    /// </summary>
    private const float PanSmear = 0.6f;
    /// <summary>
    ///     Roughly how long between bursts, in logic frames, and how far that wanders either side.
    /// </summary>
    /// <remarks>
    ///     A fault arriving on a fixed beat reads as something being done to the picture rather than
    ///     something wrong with it, so the gap is jittered by the burst's own number rather than at
    ///     random, which keeps it identical every time. The jitter stays below the period, which is
    ///     what makes each window hold exactly one burst.
    /// </remarks>
    /// <seealso cref="JitterFor" />
    private static readonly int BurstPeriod = LogicClock.Seconds(2.1);

    /// <inheritdoc cref="BurstPeriod" />
    private static readonly int BurstJitter = LogicClock.Seconds(1.3);

    /// <summary>
    ///     How long a burst lasts. A third of a second: long enough to read as the page coming apart,
    ///     short enough that the report is legible again before anybody gives up on it.
    /// </summary>
    private static readonly int BurstFrames = LogicClock.Seconds(0.34);

    /// <inheritdoc cref="Flutter" />
    private static readonly int FlutterPeriod = LogicClock.Seconds(1.7);

    /// <summary>
    ///     How many bands the page is cut into, and how closely the fine interference lines are
    ///     spaced, both counted down the page.
    /// </summary>
    /// <remarks>
    ///     The lines are nearly as close as the composition's own rows, so they read as interference
    ///     in the signal rather than stripes laid over the page. The bands are the coarse fault and
    ///     these are its texture.
    /// </remarks>
    public static Vector2 BandScale => new(26f, 900f);

    /// <inheritdoc cref="Dropout" />
    public static Vector3 SignalColour => new(0.09f, 0.1f, 0.13f);

    /// <summary>
    ///     How badly the computer is failing on a given frame, <see cref="Idle" /> between bursts
    ///     and one at the height of them.
    /// </summary>
    /// <remarks>
    ///     Flat at the top for the first two thirds of a burst and falling away over the last third,
    ///     so it snaps on and lets go the way a signal does. A burst eased in and out would read as
    ///     the page being dissolved into something.
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
    ///     Only some bursts take the hold with them. If every one did, the roll would become part of
    ///     what a burst is, and the picture's position is the last thing a set gives up: losing it
    ///     every two seconds reads as a different fault from the bands.
    /// </remarks>
    public static float RollAt(int logicFrame)
    {
        int into = FramesIntoBurst(logicFrame);

        if (into < 0 || !TheHoldGoesDuring(BurstContaining(logicFrame)))
        {
            return 0f;
        }

        // Wrapped here as well as in the shader, so what is handed over is the fraction actually
        // used rather than a number growing for as long as the page is on screen.
        return into * RollSpeed % 1f;
    }

    /// <summary>
    ///     Where the bands have travelled to by a given frame, which also reshuffles which are
    ///     thrown and which have lost their signal.
    /// </summary>
    public static float PhaseAt(int logicFrame) => (float)(logicFrame * BandDrift);

    /// <summary>
    ///     How far off true the music plays on a given frame, for a sound effect instance's pitch:
    ///     flat through a burst and flickering either side of true between them.
    /// </summary>
    public static float MusicPitchAt(int logicFrame)
        => Flutter * (float)Math.Sin(logicFrame * 2 * Math.PI / FlutterPeriod)
           - PitchDive * FaultAt(logicFrame);

    /// <summary>How loud the music is on a given frame, from nought to one.</summary>
    public static float MusicVolumeAt(int logicFrame)
    {
        float duckedTo = TheHoldGoesDuring(BurstContaining(logicFrame)) ? LostVolume : DuckedVolume;

        return 1f - (1f - duckedTo) * FaultAt(logicFrame);
    }

    /// <summary>
    ///     Which speaker the music is thrown towards on a given frame, from minus one for the left
    ///     to one for the right.
    /// </summary>
    public static float MusicPanAt(int logicFrame)
        => PanSmear * FaultAt(logicFrame) * SideOfTheBurst(BurstContaining(logicFrame));

    /// <summary>
    ///     How far into a fault a frame is: nought between bursts and one at the height of them.
    ///     <see cref="BreakupAt" /> with its floor taken off, which is what the sound wants. The
    ///     floor is a texture on the picture, where music permanently flat and quiet is just wrong.
    /// </summary>
    private static float FaultAt(int logicFrame) => (BreakupAt(logicFrame) - Idle) / (1f - Idle);

    /// <summary>
    ///     Which burst a frame belongs to: the one whose window it falls in, whether or not that
    ///     burst has started by then.
    /// </summary>
    private static int BurstContaining(int logicFrame) => logicFrame / BurstPeriod;

    /// <summary>How far into its window a burst arrives, in logic frames.</summary>
    /// <remarks>
    ///     A hash of the burst's number rather than a multiple of it, which is why this is a function
    ///     and not an expression. A multiple taken modulo the jitter can only step by two amounts,
    ///     the multiplier or the multiplier less the jitter, so the gap alternates between two values
    ///     and the fault is back on a beat with a limp in it. The same hash
    ///     <c>Shaders/Malfunction.fx</c> picks its bands with, using that hash's constants.
    /// </remarks>
    private static int JitterFor(int burst)
    {
        double hash = Math.Sin(burst * 12.9898) * 43758.5453;

        return (int)((hash - Math.Floor(hash)) * BurstJitter);
    }

    /// <summary>
    ///     How many frames into its burst a frame is, or minus one between bursts.
    /// </summary>
    private static int FramesIntoBurst(int logicFrame)
    {
        int burst = BurstContaining(logicFrame);
        int startsAt = burst * BurstPeriod + JitterFor(burst);
        int into = logicFrame - startsAt;

        return into >= 0 && into < BurstFrames ? into : -1;
    }

    /// <summary>
    ///     Whether this is one of the bursts that takes the vertical hold with it. Roughly a third of
    ///     them, picked by the burst's own number so it is the same every time.
    /// </summary>
    private static bool TheHoldGoesDuring(int burst) => burst * 7919 % 100 < 32;

    /// <summary>
    ///     Which side a burst throws the sound: minus one for the left, one for the right. Picked by
    ///     the burst's own number, so it is the same every time.
    /// </summary>
    /// <remarks>
    ///     A different multiplier from <see cref="TheHoldGoesDuring" />. The same one would tie the
    ///     two together and throw every rolling burst the same way.
    /// </remarks>
    private static int SideOfTheBurst(int burst) => burst * 104729 % 100 < 50 ? -1 : 1;
}
