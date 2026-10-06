// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;
using Starquake.Display;
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
///     <c>BlobFall</c> makes with the sound of a long fall.
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

    /// <summary>
    ///     A jolt is what something exploding does to the picture: short, hard, and over before a
    ///     tremor would have got going.
    /// </summary>
    [Fact]
    public void Jolt_OnceAsked_ShakesHardAndIsOverQuickly()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        shake.Jolt();

        var offsets = new List<int>();
        int frames = 0;

        while (true)
        {
            Shake step = shake.Step(generator, false, false, false)!.Value;

            if (!step.MovesTheViewPort)
            {
                break;
            }

            offsets.Add(Math.Max(step.XOffset, step.YOffset));
            frames++;
        }

        // Assert - harder than a tremor ever is, and gone inside a quarter of a second
        Assert.NotEmpty(offsets);
        Assert.True(offsets.Max() > TestData.LargestOffset,
            $"a jolt peaked at {offsets.Max()}, no harder than a tremor");
        Assert.InRange(frames, 1, LogicClock.Seconds(0.3));
    }

    /// <summary>
    ///     It hits and settles rather than buzzing for a fifth of a second, so the end of a jolt
    ///     has to be gentler than its beginning.
    /// </summary>
    [Fact]
    public void Jolt_OverItsRun_DiesAwayRatherThanHoldingItsForce()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act
        shake.Jolt();

        var offsets = new List<int>();

        for (int frame = 0; frame < 11; frame++)
        {
            Shake step = shake.Step(generator, false, false, false)!.Value;
            offsets.Add(Math.Max(step.XOffset, step.YOffset));
        }

        // Assert - the first third hits harder than the last
        Assert.True(
            offsets.Take(4).Max() > offsets.TakeLast(4).Max(),
            $"a jolt did not die away: {string.Join(", ", offsets)}");
    }

    /// <summary>
    ///     A jolt makes no rumble. The tremor's sound runs for seconds; a blast carries its own
    ///     noise, and a rumble started and stopped inside a fifth of a second would be a click.
    /// </summary>
    [Fact]
    public void Jolt_WhileItRuns_NeverAsksForTheRumble()
    {
        // Arrange
        var shake = new ScreenShake();
        Random generator = TestData.Generator();

        // Act / Assert
        shake.Jolt();

        for (int frame = 0; frame < 11; frame++)
        {
            Assert.False(shake.Step(generator, false, false, false)!.Value.Rumbling);
        }
    }

    /// <summary>
    ///     The view port belongs to a screen effect while one is composing the picture, so a jolt
    ///     waits rather than fighting it for the offset - which is the rule a tremor already keeps.
    /// </summary>
    [Fact]
    public void Jolt_WhileAScreenEffectIsRunning_LeavesTheViewPortAlone()
    {
        // Arrange
        var shake = new ScreenShake();

        // Act
        shake.Jolt();

        // Assert
        Assert.Null(shake.Step(TestData.Generator(), false, false, true));
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
