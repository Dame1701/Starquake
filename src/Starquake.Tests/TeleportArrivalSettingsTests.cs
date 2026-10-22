// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The settle behind a teleport arrival. The shimmer itself can only be judged by looking at it.
///     What is held here is that it starts unsettled, ends at the picture untouched, and gets there
///     without going backwards.
/// </summary>
public class TeleportArrivalSettingsTests
{
    [Fact]
    public void StrengthAt_TheMomentHeArrives_IsFullyUnsettled()
        => Assert.Equal(1f, TeleportArrivalSettings.StrengthAt(0));

    /// <summary>
    ///     The room has to end up exactly as it would have been drawn without any of this. A
    ///     strength that never quite reached nought would leave the picture permanently pushed a
    ///     fraction off where it belongs, for the rest of the game.
    /// </summary>
    [Fact]
    public void StrengthAt_OnceItHasSettled_IsTheRoomUntouched()
    {
        // Assert
        Assert.Equal(0f, TeleportArrivalSettings.StrengthAt(TeleportArrivalSettings.Frames));
        Assert.Equal(0f, TeleportArrivalSettings.StrengthAt(TeleportArrivalSettings.Frames * 3));
    }

    [Fact]
    public void StrengthAt_FromArrivalToSettled_OnlyEverDiesDown()
    {
        // Act / Assert
        for (int frame = 1; frame <= TeleportArrivalSettings.Frames; frame++)
        {
            Assert.True(
                TeleportArrivalSettings.StrengthAt(frame)
                <= TeleportArrivalSettings.StrengthAt(frame - 1),
                $"the room grew more unsettled at frame {frame}");
        }
    }

    /// <summary>
    ///     It falls away quickly at first and then lingers, rather than running down evenly. A
    ///     linear settle reads as the effect being switched off at a moment.
    /// </summary>
    [Fact]
    public void StrengthAt_Halfway_HasAlreadyLostMoreThanHalfOfItself()
    {
        // Act
        float halfway = TeleportArrivalSettings.StrengthAt(TeleportArrivalSettings.Frames / 2);

        // Assert
        Assert.InRange(halfway, 0.1f, 0.4f);
    }

    [Fact]
    public void PhaseAt_OverSuccessiveFrames_KeepsTheWavesTravellingOneWay()
    {
        // Act / Assert
        for (int frame = 1; frame < 100; frame++)
        {
            Assert.True(
                TeleportArrivalSettings.PhaseAt(frame) > TeleportArrivalSettings.PhaseAt(frame - 1),
                $"the waves stopped at frame {frame}");
        }
    }

    /// <summary>
    ///     Two frequencies that shared a factor would stand still against one another and read as
    ///     stripes rather than as a shimmer.
    /// </summary>
    [Fact]
    public void Frequency_OnTheTwoAxes_ShareNoCommonFactor()
    {
        // Act
        int across = (int)TeleportArrivalSettings.Frequency.X;
        int down = (int)TeleportArrivalSettings.Frequency.Y;

        // Assert
        Assert.Equal(1, GreatestCommonFactorOf(across, down));
    }

    private static int GreatestCommonFactorOf(int first, int second)
        => second == 0 ? first : GreatestCommonFactorOf(second, first % second);
}
