// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     A screen coming back up out of black. Four lines, and they were written twice - once in
///     <c>GameScreenBase</c> for the five full-screen pages and once in <c>MainGameScreen</c> for the game -
///     so they are one function now, and this is what pins it.
/// </summary>
public class ScreenFadeUpTests
{
    [Fact]
    public void Advanced_FromBlack_BrightensByTheSpeed()
    {
        // Act
        int level = ScreenFadeUp.Advanced(0, 32);

        // Assert
        Assert.Equal(32, level);
    }

    [Fact]
    public void Advanced_PartWayUp_KeepsBrightening()
    {
        // Act
        int level = ScreenFadeUp.Advanced(100, 12);

        // Assert
        Assert.Equal(112, level);
    }

    /// <summary>
    ///     A fade that would overshoot stops at full rather than wrapping a colour byte round to nothing.
    /// </summary>
    [Fact]
    public void Advanced_WithinOneStepOfFull_StopsAtFull()
    {
        // Act
        int level = ScreenFadeUp.Advanced(ColourLevels.Full - 1, 64);

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    [Fact]
    public void Advanced_AlreadyAtFull_StaysThere()
    {
        // Act
        int level = ScreenFadeUp.Advanced(ColourLevels.Full, 32);

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }

    /// <summary>
    ///     What a screen that is not fading holds, and the reason the game can call this every frame
    ///     without a flag saying whether a fade is running.
    /// </summary>
    [Fact]
    public void Advanced_AtNoSpeed_ChangesNothing()
    {
        // Act
        int fromBlack = ScreenFadeUp.Advanced(0, 0);
        int partWay = ScreenFadeUp.Advanced(90, 0);

        // Assert
        Assert.Equal(0, fromBlack);
        Assert.Equal(90, partWay);
    }

    [Fact]
    public void Advanced_SteppedRepeatedly_ReachesFullAndRestsThere()
    {
        // Act
        int level = 0;

        for (int frame = 0; frame < 100; frame++)
        {
            level = ScreenFadeUp.Advanced(level, 12);
        }

        // Assert
        Assert.Equal(ColourLevels.Full, level);
    }
}
