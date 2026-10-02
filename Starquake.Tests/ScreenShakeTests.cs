// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Display;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The planet's tremors: when the ground starts moving, how long for, and what the picture and the
///     rumble do while it does. All of it was inside <c>MainGameScreen.ShakeScreen</c>, which needs the
///     whole engine.
/// </summary>
/// <remarks>
///     <see cref="ScreenShake" /> takes a <see cref="Random" /> and three flags and hands back a
///     <see cref="Shake" />, so a test can seed the generator and step it. The rumble and the view port
///     stayed with the screen, which is the trade that makes this reachable - the same one
///     <see cref="Starquake.ActiveSprites.BlobFall" /> makes with the sound of a long fall.
/// </remarks>
public class ScreenShakeTests
{
    [Fact]
    public void Step_WhileAScreenEffectIsRunning_SaysToLeaveEverythingAlone()
    {
        // Arrange
        var shake = new ScreenShake();

        // Act
        Shake? result = shake.Step(TestData.Generator(), false, false, true);

        // Assert. Null rather than Still: a screen effect owns the view port, and the rumble is left
        // exactly as it was rather than being stopped.
        Assert.Null(result);
    }

    [Fact]
    public void Step_WithNothingHappening_IsStillAndNotRumbling()
    {
        // Arrange - a seed that does not roll the trigger on its first frame
        var shake = new ScreenShake();

        // Act
        Shake? result = shake.Step(TestData.Generator(), false, false, false);

        // Assert
        Assert.Equal(Shake.Still, result);
        Assert.False(result!.Value.Rumbling);
    }

    /// <summary>
    ///     The intro's own tremor is the ship coming down, and it is started deliberately rather than
    ///     rolled for.
    /// </summary>
    [Fact]
    public void Step_AsTheIntroReachesItsEndPause_StartsShakingAtOnce()
    {
        // Arrange
        var shake = new ScreenShake();

        // Act
        Shake? result = shake.Step(TestData.Generator(), true, true, false);

        // Assert
        Assert.True(result!.Value.Rumbling);
    }

    /// <summary>
    ///     No random tremor during the intro: the ship coming down should be the only shake the opening
    ///     has.
    /// </summary>
    [Fact]
    public void Step_ThroughoutTheIntro_NeverStartsOneOfItsOwn()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        bool everRumbled = false;

        for (int frame = 0; frame < TestData.LongEnoughToRollMany; frame++)
        {
            everRumbled |= shake.Step(generator, true, false, false)!.Value.Rumbling;
        }

        // Assert
        Assert.False(everRumbled);
    }

    [Fact]
    public void Step_LeftRunning_StartsATremorEventuallyAndStopsItAgain()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        var rumbling = new List<bool>();

        for (int frame = 0; frame < TestData.LongEnoughToRollMany; frame++)
        {
            rumbling.Add(shake.Step(generator, false, false, false)!.Value.Rumbling);
        }

        // Assert
        Assert.Contains(true, rumbling);
        Assert.Contains(false, rumbling);
    }

    /// <summary>
    ///     A tremor runs for between three and six seconds at fifty frames a second. Checked as a range
    ///     because which length it picks is the generator's business.
    /// </summary>
    /// <remarks>
    ///     An unbroken run of shaking is not always one tremor. The frame that ends a tremor still shakes,
    ///     and the roll for the next one can come up on the very next frame, so two can run back to back
    ///     with no still frame between them - which a run of 471 frames out of this seed turned out to
    ///     be. The shortest run observed cannot be two, because two of even the shortest tremor would be
    ///     360 frames and no single one reaches that, so that is what the upper bound is asserted against.
    /// </remarks>
    [Fact]
    public void Step_ATremorOnceStarted_RunsForBetweenThreeAndSixSeconds()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        var lengths = new List<int>();
        int run = 0;

        for (int frame = 0; frame < TestData.LongEnoughToRollMany; frame++)
        {
            if (shake.Step(generator, false, false, false)!.Value.Rumbling)
            {
                run++;
            }
            else if (run > 0)
            {
                lengths.Add(run);
                run = 0;
            }
        }

        // Assert
        Assert.NotEmpty(lengths);
        Assert.All(lengths, length => Assert.True(length >= TestData.ShortestRun));
        Assert.InRange(lengths.Min(), TestData.ShortestRun, TestData.LongestRun);
    }

    /// <summary>
    ///     Both offsets are rolled from nought, so some frames of a tremor leave the picture where it is.
    ///     That unevenness is what makes it read as the ground moving rather than as a buzz.
    /// </summary>
    [Fact]
    public void MovesTheViewPort_OnlyWhenBothOffsetsAreSomething()
    {
        // Assert
        Assert.True(new Shake(1, 1, true).MovesTheViewPort);
        Assert.False(new Shake(0, 3, true).MovesTheViewPort);
        Assert.False(new Shake(3, 0, true).MovesTheViewPort);
        Assert.False(Shake.Still.MovesTheViewPort);
    }

    [Fact]
    public void Step_WhileShaking_PushesThePictureByNoMoreThanFourPixels()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        var offsets = new List<int>();

        for (int frame = 0; frame < TestData.LongEnoughToRollMany; frame++)
        {
            Shake step = shake.Step(generator, false, false, false)!.Value;

            if (step.Rumbling)
            {
                offsets.Add(step.XOffset);
                offsets.Add(step.YOffset);
            }
        }

        // Assert
        Assert.NotEmpty(offsets);
        Assert.All(offsets, offset => Assert.InRange(offset, 0, TestData.LargestOffset));
    }

    private static class TestData
    {
        /// <summary>
        ///     A tremor is one roll in two thousand, so a few hundred thousand frames is enough to see
        ///     several start and finish.
        /// </summary>
        public const int LongEnoughToRollMany = 200_000;

        /// <summary>The two ends of a tremor's length, as <see cref="ScreenShake" /> sets them.</summary>
        public const int ShortestRun = 180;

        /// <inheritdoc cref="ShortestRun" />
        public const int LongestRun = 301;

        /// <summary>The largest offset it will push by - the roll is exclusive of five.</summary>
        public const int LargestOffset = 4;

        /// <summary>
        ///     Seeded, so a failure is the same failure twice. The game seeds its own from the clock.
        /// </summary>
        public static Random Generator() => new(20_260_102);
    }
}
