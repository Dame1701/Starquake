// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The flicker behind the electricity. The burning itself can only be judged by looking at it;
///     this is the one part of it that is a function and can be held to what it promises.
/// </summary>
public class ElectricSettingsTests
{
    [Fact]
    public void FlickerAt_OverASecondOfFrames_StaysNearItsUsualBrightness()
    {
        // Act / Assert - never dark, never more than half again as bright
        for (int frame = 0; frame < 500; frame++)
        {
            Assert.InRange(ElectricSettings.FlickerAt(frame), 0.5f, 1.5f);
        }
    }

    /// <summary>
    ///     It has to actually move, or there is no flicker - and it has to move within a frame or
    ///     two rather than drifting, or it reads as the arc breathing instead of crackling.
    /// </summary>
    [Fact]
    public void FlickerAt_FromOneFrameToTheNext_Changes()
    {
        // Act
        int framesThatMoved = 0;

        for (int frame = 0; frame < 50; frame++)
        {
            if (Math.Abs(ElectricSettings.FlickerAt(frame) - ElectricSettings.FlickerAt(frame + 1)) > 0.02f)
            {
                framesThatMoved++;
            }
        }

        // Assert - almost every frame, rather than the odd one
        Assert.True(framesThatMoved > 40, $"only {framesThatMoved} of 50 frames moved");
    }

    /// <summary>
    ///     Two sines at rates that do not divide one another, so it must not come back to where it
    ///     started on any period short enough to be seen as a loop.
    /// </summary>
    [Fact]
    public void FlickerAt_OverALongRun_DoesNotRepeatItselfQuickly()
    {
        // Act
        float first = ElectricSettings.FlickerAt(0);

        int matches = 0;

        for (int frame = 1; frame < 400; frame++)
        {
            if (Math.Abs(ElectricSettings.FlickerAt(frame) - first) < 0.001f)
            {
                matches++;
            }
        }

        // Assert - it passes through the same level now and then, as any wave does, but it is not
        // sitting on a short cycle
        Assert.True(matches < 20, $"came back to its starting level {matches} times in 400 frames");
    }

    /// <summary>
    ///     A function of the frame and nothing else, so the arc crackles the same way on every
    ///     machine and a test can say what it will do.
    /// </summary>
    [Fact]
    public void FlickerAt_TheSameFrameTwice_IsTheSameBrightness()
        => Assert.Equal(ElectricSettings.FlickerAt(123), ElectricSettings.FlickerAt(123));
}
