// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     One frame of a value moving toward a limit and stopping there. Both directions and both
///     types, because the game ramps ints and floats up and down and all four go through here.
/// </summary>
public class RampTests
{
    [Fact]
    public void Up_FromBlack_BrightensByTheStep()
    {
        // Arrange
        const int fadeSpeed = 32;

        // Act
        int level = Ramp.Up(ColourLevels.None, fadeSpeed, ColourLevels.Full);

        // Assert
        Assert.Equal(32, level);
    }

    [Fact]
    public void Up_PartWayUp_KeepsBrightening()
    {
        // Arrange
        const int partWayUp = 100;

        // Act
        int level = Ramp.Up(partWayUp, 12, ColourLevels.Full);

        // Assert
        Assert.Equal(112, level);
    }

    /// <summary>
    ///     A fade that would overshoot stops at full rather than wrapping a colour byte round to nothing.
    /// </summary>
    [Fact]
    public void Up_WithinOneStepOfTheLimit_StopsAtTheLimit()
    {
        // Arrange
        const int oneShortOfFull = ColourLevels.Full - 1;

        // Act
        int level = Ramp.Up(oneShortOfFull, 64, ColourLevels.Full);

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    [Fact]
    public void Up_AlreadyAtTheLimit_StaysThere()
    {
        // Act
        int level = Ramp.Up(ColourLevels.Full, 32, ColourLevels.Full);

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    /// <summary>
    ///     The clamp is unconditional rather than part of the step, so a value that starts beyond the
    ///     limit is pulled straight back to it instead of easing down to it. Nothing in the game sets
    ///     one, and this pins which of the two a reader should expect.
    /// </summary>
    [Fact]
    public void Up_StartingBeyondTheLimit_SnapsBackToIt()
    {
        // Arrange
        const int beyondFull = ColourLevels.Full + 40;

        // Act
        int level = Ramp.Up(beyondFull, 8, ColourLevels.Full);

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    /// <summary>
    ///     What a value that is not moving holds, and the reason the game can call this every frame
    ///     without a flag saying whether a fade is running.
    /// </summary>
    [Fact]
    public void Up_AtNoStep_ChangesNothing()
    {
        // Act
        int fromBlack = Ramp.Up(ColourLevels.None, 0, ColourLevels.Full);
        int partWay = Ramp.Up(90, 0, ColourLevels.Full);

        // Assert
        Assert.Equal(ColourLevels.None, fromBlack);
        Assert.Equal(90, partWay);
    }

    [Fact]
    public void Up_SteppedRepeatedly_ReachesTheLimitAndRestsThere()
    {
        // Arrange
        int level = ColourLevels.None;

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheFadeNeeds; frame++)
        {
            level = Ramp.Up(level, 12, ColourLevels.Full);
        }

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    [Fact]
    public void Down_FromFull_DarkensByTheStep()
    {
        // Act
        int level = Ramp.Down(ColourLevels.Full, 3, ColourLevels.None);

        // Assert
        Assert.Equal(252, level);
    }

    /// <summary>
    ///     The reason a dying nasty's fade and shrink do not go negative, which an int would happily
    ///     do and which reads as a sprite flipping inside out.
    /// </summary>
    [Fact]
    public void Down_WithinOneStepOfTheLimit_StopsAtTheLimit()
    {
        // Arrange
        const int oneShortOfNothing = 1;

        // Act
        int level = Ramp.Down(oneShortOfNothing, 64, ColourLevels.None);

        // Assert
        Assert.Equal(ColourLevels.None, level);
    }

    [Fact]
    public void Down_AlreadyAtTheLimit_StaysThere()
    {
        // Act
        int level = Ramp.Down(ColourLevels.None, 32, ColourLevels.None);

        // Assert
        Assert.Equal(ColourLevels.None, level);
    }

    [Fact]
    public void Down_StartingBeneathTheLimit_SnapsBackToIt()
    {
        // Arrange
        const int beneathTheFloor = -40;

        // Act
        int level = Ramp.Down(beneathTheFloor, 8, ColourLevels.None);

        // Assert
        Assert.Equal(ColourLevels.None, level);
    }

    [Fact]
    public void Down_SteppedRepeatedly_ReachesTheLimitAndRestsThere()
    {
        // Arrange
        int level = ColourLevels.Full;

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheFadeNeeds; frame++)
        {
            level = Ramp.Down(level, 12, ColourLevels.None);
        }

        // Assert
        Assert.Equal(ColourLevels.None, level);
    }

    /// <summary>
    ///     A core element growing into place. The step does not divide the distance evenly, which is
    ///     the case the integer ramp never meets and the one that would overshoot without the clamp.
    /// </summary>
    [Fact]
    public void UpInFloat_SteppedRepeatedly_RestsExactlyOnTheLimit()
    {
        // Arrange
        float scale = 0f;

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheFadeNeeds; frame++)
        {
            scale = Ramp.Up(scale, TestData.AwkwardStep, TestData.ElementScale);
        }

        // Assert
        Assert.Equal(TestData.ElementScale, scale);
    }

    [Fact]
    public void DownInFloat_SteppedRepeatedly_RestsExactlyOnTheLimit()
    {
        // Arrange
        float scale = TestData.ElementScale;

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheFadeNeeds; frame++)
        {
            scale = Ramp.Down(scale, TestData.AwkwardStep, 0f);
        }

        // Assert
        Assert.Equal(0f, scale);
    }

    [Fact]
    public void DownInFloat_PartWayDown_KeepsShrinking()
    {
        // Arrange
        const float partWayDown = 1.5f;

        // Act
        float scale = Ramp.Down(partWayDown, 0.5f, 0f);

        // Assert
        Assert.Equal(1f, scale);
    }

    private static class TestData
    {
        /// <summary>
        ///     Long enough for every step size used here to have reached its limit several times over.
        /// </summary>
        public const int MoreFramesThanTheFadeNeeds = 100;

        public const float ElementScale = 2f;

        public const float AwkwardStep = 0.3f;
    }
}
