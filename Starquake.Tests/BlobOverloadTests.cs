// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How Blob comes apart: the alarm that sounds when he runs out of energy, the swell and shrink,
///     and the flare drawn over it. All of it was inside <c>Blob</c>, so none of it could be reached - the
///     only way to see a death was to play one.
/// </summary>
/// <remarks>
///     This needs nothing at all to construct, which is why it is the most thoroughly covered of the five
///     pieces taken out of Blob. The explosion's sound, its fragments and putting Blob back where
///     he came from stayed behind, and each of those is the reason: all three need something the shape of
///     a death has no business holding.
/// </remarks>
public class BlobOverloadTests
{
    /// <summary>
    ///     Running out of energy is the one death with a warning. Everything else - spikes, electricity, a
    ///     nasty - begins already exploded, and that is the whole mechanism by which the alarm is skipped.
    /// </summary>
    [Fact]
    public void Begin_WithTheAlarm_SoundsItAndDoesNotExplodeYet()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act
        overload.Begin(true);

        // Assert
        Assert.True(overload.AlarmSounding);
        Assert.Equal(1f, overload.Scale);
    }

    [Fact]
    public void Begin_WithoutTheAlarm_SkipsStraightToTheExplosion()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act
        overload.Begin(false);

        // Assert
        Assert.False(overload.AlarmSounding);
        Assert.Equal(OverloadStage.Exploded, overload.Swell());
    }

    /// <summary>
    ///     The sound is asked for on the frame the count reads one, which is the frame after the death -
    ///     the count is advanced at the end of a frame, so it reads nought on the first.
    /// </summary>
    [Fact]
    public void AlarmJustStarted_IsTrueOnTheSecondFrameOnly()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        bool onTheFirstFrame = overload.AlarmJustStarted;
        overload.CountAnAlarmFrame();
        bool onTheSecond = overload.AlarmJustStarted;
        overload.CountAnAlarmFrame();
        bool onTheThird = overload.AlarmJustStarted;

        // Assert
        Assert.False(onTheFirstFrame);
        Assert.True(onTheSecond);
        Assert.False(onTheThird);
    }

    [Fact]
    public void AlarmSounding_AfterASecondOfIt_StopsAndLetsTheExplosionBegin()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        int frames = 0;

        while (overload.AlarmSounding && frames < TestData.LongerThanAnyAlarm)
        {
            overload.CountAnAlarmFrame();
            frames++;
        }

        // Assert
        Assert.Equal(TestData.AlarmFrames, frames);
        Assert.False(overload.AlarmSounding);
    }

    /// <summary>
    ///     He flickers through the alarm rather than being drawn solidly, which is what makes it read as a
    ///     warning.
    /// </summary>
    [Fact]
    public void Visible_ThroughTheAlarm_AlternatesEveryFrame()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        var drawn = new List<bool>();

        for (int frame = 0; frame < 6; frame++)
        {
            drawn.Add(overload.Visible);
            overload.CountAnAlarmFrame();
        }

        // Assert
        Assert.Equal(new[] { true, false, true, false, true, false }, drawn);
    }

    [Fact]
    public void ShowingTheAlarm_OnceTheAlarmHasRun_HandsOverToTheFlare()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        bool duringTheAlarm = overload.ShowingTheAlarm;
        TestData.SoundTheWholeAlarm(overload);

        // Assert
        Assert.True(duringTheAlarm);
        Assert.False(overload.ShowingTheAlarm);
    }

    [Fact]
    public void Swell_FromTheStart_GrowsToItsLargestAndThenTurnsBack()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        var scales = new List<float>();
        OverloadStage stage;

        do
        {
            stage = overload.Swell();
            scales.Add(overload.Scale);
        }
        while (stage != OverloadStage.Exploded && scales.Count < TestData.LongerThanAnyDeath);

        // Assert
        Assert.Equal(TestData.LargestScale, scales.Max());
        Assert.Equal(TestData.SmallestScale, scales[^1]);
        Assert.Equal(OverloadStage.Exploded, stage);
    }

    /// <summary>
    ///     He is drawn at a negative scale on the way out, which is what turns him inside out. A floor of
    ///     zero would have him simply vanish.
    /// </summary>
    [Fact]
    public void Swell_OnTheWayOut_PassesThroughZeroAndGoesNegative()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        var scales = new List<float>();

        while (overload.Swell() != OverloadStage.Exploded && scales.Count < TestData.LongerThanAnyDeath)
        {
            scales.Add(overload.Scale);
        }

        scales.Add(overload.Scale);

        // Assert
        Assert.Contains(scales, scale => scale < 0f);
        Assert.Equal(TestData.SmallestScale, scales[^1]);
    }

    [Fact]
    public void Swell_AfterExploding_StaysExploded()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(false);

        // Act
        OverloadStage first = overload.Swell();
        OverloadStage second = overload.Swell();

        // Assert
        Assert.Equal(OverloadStage.Exploded, first);
        Assert.Equal(OverloadStage.Exploded, second);
    }

    [Fact]
    public void Reset_AfterADeath_PutsHimBackToHisOwnSize()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        while (overload.Swell() != OverloadStage.Exploded)
        {
            // run the death out
        }

        // Act
        overload.Reset();

        // Assert
        Assert.Equal(1f, overload.Scale);
        Assert.NotEqual(OverloadStage.Exploded, overload.Swell());
    }

    /// <summary>
    ///     The flare is a stack of copies of him growing outward from whatever size he is. Copies that
    ///     would come out inside out are left out rather than drawn.
    /// </summary>
    [Fact]
    public void FlareScales_AtFullSize_AreFiveCopiesGrowingOutward()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act
        float[] scales = overload.FlareScales().ToArray();

        // Assert
        Assert.Equal(5, scales.Length);
        Assert.Equal(scales.OrderBy(scale => scale), scales);
        Assert.All(scales, scale => Assert.True(scale >= 0f));
    }

    [Fact]
    public void FlareScales_OnceHeHasShrunkPastZero_AreLeftOutRatherThanDrawnInsideOut()
    {
        // Arrange - run the death down to its smallest
        var overload = new BlobOverload();
        overload.Begin(true);

        while (overload.Swell() != OverloadStage.Exploded)
        {
            // run the death out
        }

        // Act
        float[] scales = overload.FlareScales().ToArray();

        // Assert
        Assert.Empty(scales);
    }

    private static class TestData
    {
        /// <summary>A second of alarm at fifty hertz, as <see cref="BlobOverload" /> sets it.</summary>
        public const int AlarmFrames = 60;

        /// <summary>The two ends of the swell, as it sets those.</summary>
        public const float LargestScale = 1.6f;

        /// <inheritdoc cref="LargestScale" />
        public const float SmallestScale = -1f;

        /// <summary>Bounds on the loops, so a test fails rather than hangs if a progression stops ending.</summary>
        public const int LongerThanAnyAlarm = 600;

        /// <inheritdoc cref="LongerThanAnyAlarm" />
        public const int LongerThanAnyDeath = 100;

        public static void SoundTheWholeAlarm(BlobOverload overload)
        {
            for (int frame = 0; frame < AlarmFrames; frame++)
            {
                overload.CountAnAlarmFrame();
            }
        }
    }
}
