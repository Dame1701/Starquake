// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The flight computer's display failing. How badly it breaks up can only be judged by looking
///     at it; what is held here is the timing and the shape of a burst, which are functions of the
///     frame.
/// </summary>
public class MalfunctionSettingsTests
{
    /// <summary>A minute at fifty hertz, which is a good deal longer than anybody reads the page.</summary>
    private const int AMinute = 60 * LogicClock.FramesPerSecond;

    /// <summary>
    ///     The page is a report to be read, so most of the time it has to be readable. A faint
    ///     interference throughout and the bursts kept to a small share of the frames.
    /// </summary>
    [Fact]
    public void BreakupAt_OverAMinute_IsQuietFarMoreOftenThanItIsNot()
    {
        // Act
        int breakingUp = 0;

        for (int frame = 0; frame < AMinute; frame++)
        {
            if (MalfunctionSettings.BreakupAt(frame) > MalfunctionSettings.Idle)
            {
                breakingUp++;
            }
        }

        // Assert
        Assert.InRange(breakingUp / (float)AMinute, 0.05f, 0.25f);
    }

    /// <summary>
    ///     Never nought. A display that is perfectly steady between bursts reads as a working set
    ///     with something being done to it, rather than as one that is going.
    /// </summary>
    [Fact]
    public void BreakupAt_OverAMinute_NeverSettlesAndNeverOverruns()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame++)
        {
            Assert.InRange(MalfunctionSettings.BreakupAt(frame), MalfunctionSettings.Idle, 1f);
        }
    }

    /// <summary>
    ///     A signal does not ease in and out. Every burst should reach full, hold there, and then
    ///     fall away without going back up - which is what makes it read as the picture being lost
    ///     and recovered rather than dissolved into something.
    /// </summary>
    [Fact]
    public void BreakupAt_WithinEveryBurst_ReachesFullAndThenOnlyFalls()
    {
        // Act / Assert
        foreach ((int startsAt, int frames) in BurstsOverAMinute())
        {
            Assert.Equal(1f, MalfunctionSettings.BreakupAt(startsAt));

            for (int into = 1; into < frames; into++)
            {
                Assert.True(
                    MalfunctionSettings.BreakupAt(startsAt + into)
                    <= MalfunctionSettings.BreakupAt(startsAt + into - 1),
                    $"the burst at frame {startsAt} grew worse again {into} frames in");
            }
        }
    }

    /// <summary>
    ///     Long enough to be read as the page coming apart and short enough that the report is
    ///     legible again straight after, and the same length every time: a burst whose length
    ///     wandered would be the fault changing its character rather than recurring.
    /// </summary>
    [Fact]
    public void BreakupAt_EveryBurst_LastsAboutAThirdOfASecond()
    {
        // Arrange
        List<(int StartsAt, int Frames)> bursts = BurstsOverAMinute();

        // Assert
        Assert.All(bursts, burst => Assert.InRange(
            burst.Frames, LogicClock.Seconds(0.2), LogicClock.Seconds(0.5)));
        Assert.Single(bursts.Select(burst => burst.Frames).Distinct());
    }

    /// <summary>
    ///     Jittered rather than on a beat. A fault arriving exactly every two seconds reads as
    ///     something being done to the picture rather than as something wrong with it.
    /// </summary>
    [Fact]
    public void BreakupAt_OverAMinute_DoesNotComeRoundOnABeat()
    {
        // Act
        List<int> gaps = GapsBetween(BurstsOverAMinute());

        // Assert
        Assert.True(gaps.Count > 10, "a minute should hold a good many bursts");
        Assert.True(gaps.Distinct().Count() > gaps.Count / 2,
            "the bursts are arriving on a fixed beat");
    }

    /// <summary>
    ///     The picture's own position is the last thing a set gives up. Some bursts take the
    ///     vertical hold with them and most do not.
    /// </summary>
    [Fact]
    public void RollAt_OverAMinute_TakesTheHoldDuringSomeBurstsAndNotAll()
    {
        // Act
        List<(int StartsAt, int Frames)> bursts = BurstsOverAMinute();
        int rolled = bursts.Count(burst => Rolls(burst.StartsAt, burst.Frames));

        // Assert
        Assert.InRange(rolled, 1, bursts.Count - 1);
    }

    /// <summary>
    ///     Between bursts the picture is where it belongs. Anything else would leave the report
    ///     permanently sitting a fraction off the screen it is drawn on.
    /// </summary>
    [Fact]
    public void RollAt_WhileTheDisplayIsQuiet_LeavesThePictureWhereItIs()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame++)
        {
            if (MalfunctionSettings.BreakupAt(frame) <= MalfunctionSettings.Idle)
            {
                Assert.Equal(0f, MalfunctionSettings.RollAt(frame));
            }
        }
    }

    /// <summary>
    ///     A fraction of the height and not a number that grows. The shader wraps what it is
    ///     handed, so a roll outside the range would still draw - it would just have stopped
    ///     meaning what it says.
    /// </summary>
    [Fact]
    public void RollAt_OverAMinute_StaysWithinTheOnePicture()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame++)
        {
            Assert.InRange(MalfunctionSettings.RollAt(frame), 0f, 1f);
        }
    }

    [Fact]
    public void PhaseAt_OverSuccessiveFrames_KeepsTheBandsTravellingOneWay()
    {
        // Act / Assert
        for (int frame = 1; frame < 100; frame++)
        {
            Assert.True(
                MalfunctionSettings.PhaseAt(frame) > MalfunctionSettings.PhaseAt(frame - 1),
                $"the bands stopped at frame {frame}");
        }
    }

    /// <summary>
    ///     Where every burst over the minute starts and how long it runs for, read back out of
    ///     <see cref="MalfunctionSettings.BreakupAt" /> rather than from the period and the jitter -
    ///     which are private, and which are what these tests exist to check the behaviour of.
    /// </summary>
    private static List<(int StartsAt, int Frames)> BurstsOverAMinute()
    {
        var bursts = new List<(int StartsAt, int Frames)>();
        int startedAt = -1;

        for (int frame = 0; frame < AMinute; frame++)
        {
            bool breakingUp = MalfunctionSettings.BreakupAt(frame) > MalfunctionSettings.Idle;

            if (breakingUp && startedAt < 0)
            {
                startedAt = frame;
            }
            else if (!breakingUp && startedAt >= 0)
            {
                bursts.Add((startedAt, frame - startedAt));
                startedAt = -1;
            }
        }

        return bursts;
    }

    private static List<int> GapsBetween(List<(int StartsAt, int Frames)> bursts)
        => bursts
            .Zip(bursts.Skip(1), (earlier, later) => later.StartsAt - earlier.StartsAt)
            .ToList();

    private static bool Rolls(int startsAt, int frames)
        => Enumerable
            .Range(startsAt, frames)
            .Any(frame => MalfunctionSettings.RollAt(frame) > 0f);
}
