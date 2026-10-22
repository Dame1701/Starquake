// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The arithmetic behind the core room's loop: where its copies sit, how loud they are and how
///     far they bend. Whether it sounds like a room can only be judged by listening to it.
/// </summary>
public class CoreSoundSettingsTests
{
    /// <summary>A minute, which is longer than Blob is ever left in the core room.</summary>
    private const int AMinute = 60 * LogicClock.FramesPerSecond;

    /// <summary>Each copy has to be heard somewhere, and none can be asked for more than full.</summary>
    [Fact]
    public void PanAndVolume_OverAMinute_StayWithinWhatAnInstanceAccepts()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame++)
        {
            for (int voice = 0; voice < CoreSoundSettings.Voices; voice++)
            {
                Assert.InRange(CoreSoundSettings.PanOf(voice, frame), -1f, 1f);
                Assert.InRange(CoreSoundSettings.VolumeOf(voice, frame), 0.01f, 1f);
            }
        }
    }

    /// <summary>
    ///     The copies are on opposite sides of the listener. Together on one side, the loop is no
    ///     wider than one copy.
    /// </summary>
    [Fact]
    public void PanOf_TheTwoVoices_AreAlwaysOnOppositeSides()
    {
        // Act / Assert
        for (int frame = 0; frame < AMinute; frame++)
        {
            Assert.Equal(
                -CoreSoundSettings.PanOf(0, frame),
                CoreSoundSettings.PanOf(1, frame),
                3);
        }
    }

    /// <summary>The copies have to go round, or the loop just sits still a little wider.</summary>
    [Fact]
    public void PanOf_OverAMinute_ReachesBothSpeakers()
    {
        // Act
        float leftmost = 0f;
        float rightmost = 0f;

        for (int frame = 0; frame < AMinute; frame++)
        {
            leftmost = Math.Min(leftmost, CoreSoundSettings.PanOf(0, frame));
            rightmost = Math.Max(rightmost, CoreSoundSettings.PanOf(0, frame));
        }

        // Assert
        Assert.True(leftmost < -0.5f);
        Assert.True(rightmost > 0.5f);
    }

    /// <summary>
    ///     Played at the same pitch the two copies stay in time with each other and add back up to
    ///     one voice in the middle.
    /// </summary>
    [Fact]
    public void PitchOf_TheTwoVoices_AreOutOfTuneWithEachOther()
    {
        // Act / Assert
        Assert.NotEqual(CoreSoundSettings.PitchOf(0, 0f), CoreSoundSettings.PitchOf(1, 0f));
    }

    /// <summary>The bend goes up with the passed colour and down with the failed one.</summary>
    [Fact]
    public void PitchOf_FlashingPassed_IsHigherThanFlashingFailed()
    {
        // Act / Assert
        for (int voice = 0; voice < CoreSoundSettings.Voices; voice++)
        {
            Assert.True(CoreSoundSettings.PitchOf(voice, 1f) > CoreSoundSettings.PitchOf(voice, -1f));
        }
    }

    [Fact]
    public void FlashSwing_WithNothingFlashing_IsTrue()
    {
        // Act / Assert
        Assert.Equal(0f, CoreSoundSettings.FlashSwing([]));
    }

    /// <summary>
    ///     A whole core flashing the same way bends no further than the swing allows, however many
    ///     elements are left in it.
    /// </summary>
    [Fact]
    public void FlashSwing_EveryElementFullyPassed_IsHeldAtOne()
    {
        // Act / Assert
        Assert.Equal(1f, CoreSoundSettings.FlashSwing([1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f]));
    }

    /// <summary>
    ///     Nine elements out of step mostly cancel. The swing must still be heard with the core
    ///     untouched, not only once it is all but finished.
    /// </summary>
    [Fact]
    public void FlashSwing_NineElementsLeaningAlike_SwingsAsFarAsOneOnItsOwn()
    {
        // Arrange
        float[] nine = [0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f, 0.3f];

        // Act / Assert
        Assert.True(CoreSoundSettings.FlashSwing(nine) >= CoreSoundSettings.FlashSwing([0.3f]));
    }
}
