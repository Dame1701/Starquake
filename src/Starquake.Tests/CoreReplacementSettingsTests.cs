// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The beats either side of a part going into the planet's core: the glow and shake while it
///     is picked out, and the room being wrung round the slot once it is in.
/// </summary>
public class CoreReplacementSettingsTests
{
    [Fact]
    public void PulseAt_OverItsRun_RisesAndFallsAcrossTheWholeRange()
    {
        // Act
        List<float> pulses = [];

        for (int frame = 0; frame < 120; frame++)
        {
            pulses.Add(CoreReplacementSettings.PulseAt(frame));
        }

        // Assert - a pulse rather than a steady light, and never outside what a fade can take
        Assert.All(pulses, pulse => Assert.InRange(pulse, 0f, 1f));
        Assert.True(pulses.Max() > 0.95f, "the glow never reached full");
        Assert.True(pulses.Min() < 0.05f, "the glow never dropped away");
    }

    /// <summary>
    ///     A shake that traced a line would read as the part sliding rather than rattling.
    /// </summary>
    [Fact]
    public void ShakeAt_OverSuccessiveFrames_MovesOnBothAxesIndependently()
    {
        // Act
        List<float> ratios = [];

        for (int frame = 0; frame < 30; frame++)
        {
            Vector2 shake = CoreReplacementSettings.ShakeAt(frame);

            if (Math.Abs(shake.X) > 0.05f)
            {
                ratios.Add(shake.Y / shake.X);
            }
        }

        // Assert
        Assert.True(ratios.Distinct().Count() > 15, "the shake traced a line");
    }

    /// <summary>It rattles in its slot, and must not wander out of it however long the core takes.</summary>
    [Fact]
    public void ShakeAt_HoweverLongItRuns_StaysInItsSlot()
    {
        // Act / Assert
        for (int frame = 0; frame < 400; frame++)
        {
            Assert.InRange(CoreReplacementSettings.ShakeAt(frame).Length(), 0f, 4f);
        }
    }

    [Fact]
    public void SurgeStrengthAt_TheMomentItLands_IsTheRoomFullyWrung()
        => Assert.Equal(1f, CoreReplacementSettings.SurgeStrengthAt(0));

    /// <summary>
    ///     The room has to end up exactly as it would have been drawn. A strength that never quite
    ///     reached zero would leave the core room permanently wrung a fraction out of true.
    /// </summary>
    [Fact]
    public void SurgeStrengthAt_OnceItHasPassed_IsTheRoomUntouched()
    {
        // Assert
        Assert.Equal(0f, CoreReplacementSettings.SurgeStrengthAt(CoreReplacementSettings.SurgeFrames));
        Assert.Equal(0f, CoreReplacementSettings.SurgeStrengthAt(
            CoreReplacementSettings.SurgeFrames * 4));
    }

    [Fact]
    public void SurgeStrengthAt_FromLandingToRecovered_OnlyEverSpringsBack()
    {
        // Act / Assert
        for (int frame = 1; frame <= CoreReplacementSettings.SurgeFrames; frame++)
        {
            Assert.True(
                CoreReplacementSettings.SurgeStrengthAt(frame)
                <= CoreReplacementSettings.SurgeStrengthAt(frame - 1),
                $"the room wrung further at frame {frame}");
        }
    }

    [Fact]
    public void RippleAt_OverSuccessiveFrames_KeepsTravellingOutward()
    {
        // Act / Assert
        for (int frame = 1; frame < 100; frame++)
        {
            Assert.True(
                CoreReplacementSettings.RippleAt(frame) > CoreReplacementSettings.RippleAt(frame - 1),
                $"the ripple stopped at frame {frame}");
        }
    }

    /// <summary>
    ///     The beat has to be long enough to be read as the core choosing something, and the two
    ///     beats together have to stay short enough that nine of them are not a chore.
    /// </summary>
    [Fact]
    public void TheWholeSequence_PerPart_IsLongEnoughToSeeAndShortEnoughToSitThrough()
    {
        // Act
        int perPart = CoreReplacementSettings.IdentifyFrames + CoreReplacementSettings.SurgeFrames;

        // Assert
        Assert.InRange(CoreReplacementSettings.IdentifyFrames, LogicClock.Seconds(0.6), LogicClock.Seconds(2));
        Assert.InRange(perPart, LogicClock.Seconds(1), LogicClock.Seconds(3));
    }
}
