// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     When the wreck of Blob's ship spits a spark and where from. The sparks themselves are a
///     particle effect and can only be judged by looking at them. What is held here is the timing,
///     which is a function of the frame.
/// </summary>
public class ShipWreckSettingsTests
{
    private const int HullWidth = 156;

    private const int HullHeight = 128;

    /// <summary>A minute at 50 frames a second, which is long enough for a good many sparks.</summary>
    private const int AMinute = 60 * 50;

    /// <summary>
    ///     It is a wreck, not a firework. Each spark is one frame's trigger and the burst lives its
    ///     own half second after that, so this should fire rarely and never twice running.
    /// </summary>
    [Fact]
    public void SparksThisFrame_OverAMinute_FiresRarelyAndNeverTwiceRunning()
    {
        // Act
        int sparks = 0;

        for (int frame = 1; frame < AMinute; frame++)
        {
            if (!ShipWreckSettings.SparksThisFrame(frame))
            {
                continue;
            }

            sparks++;

            Assert.False(
                ShipWreckSettings.SparksThisFrame(frame - 1),
                $"two sparks in a row at frame {frame}");
        }

        // Assert - a couple of dozen in a minute, not hundreds
        Assert.InRange(sparks, 10, 40);
    }

    /// <summary>
    ///     A wreck sparking on a fixed beat reads as a machine working rather than one broken, so
    ///     the gaps between sparks must not all be the same.
    /// </summary>
    [Fact]
    public void SparksThisFrame_AcrossSeveralSparks_DoesNotKeepAFixedBeat()
    {
        // Act
        List<int> gaps = [];
        int lastSpark = -1;

        for (int frame = 0; frame < AMinute; frame++)
        {
            if (!ShipWreckSettings.SparksThisFrame(frame))
            {
                continue;
            }

            if (lastSpark >= 0)
            {
                gaps.Add(frame - lastSpark);
            }

            lastSpark = frame;
        }

        // Assert
        Assert.True(gaps.Count > 10, $"only {gaps.Count} sparks in a minute");
        Assert.True(gaps.Distinct().Count() > 1, "every gap was the same length");
    }

    /// <summary>
    ///     The jitter is held below the period so that every window holds exactly one spark. A
    ///     jitter as long as the period could push one into the next window and leave a gap.
    /// </summary>
    [Fact]
    public void SparksThisFrame_OverAMinute_LeavesNoLongSilences()
    {
        // Act
        int longestSilence = 0;
        int since = 0;

        for (int frame = 0; frame < AMinute; frame++)
        {
            since = ShipWreckSettings.SparksThisFrame(frame) ? 0 : since + 1;
            longestSilence = Math.Max(longestSilence, since);
        }

        // Assert - never more than a few seconds without one
        Assert.InRange(longestSilence, 1, LogicClock.Seconds(5));
    }

    /// <summary>Sparks come off the ship, not the air beside it.</summary>
    [Fact]
    public void SparkAt_AcrossTheSparks_ComesFromTheHull()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame += 7)
        {
            Point from = ShipWreckSettings.SparkAt(frame, HullWidth, HullHeight);

            Assert.InRange(from.X, -HullWidth / 2, HullWidth / 2);
            Assert.InRange(from.Y, -HullHeight / 2, HullHeight / 2);
        }
    }

    /// <summary>
    ///     Every spark coming from the same spot would read as one thing failing rather than a
    ///     wreck full of them.
    /// </summary>
    [Fact]
    public void SparkAt_AcrossTheSparks_ComesFromDifferentPlaces()
    {
        // Act
        HashSet<Point> from = [];

        for (int frame = 0; frame < AMinute; frame++)
        {
            if (ShipWreckSettings.SparksThisFrame(frame))
            {
                from.Add(ShipWreckSettings.SparkAt(frame, HullWidth, HullHeight));
            }
        }

        // Assert
        Assert.True(from.Count > 5, $"sparks came from only {from.Count} places in a minute");
    }
}
