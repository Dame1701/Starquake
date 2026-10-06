// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Tuning;
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
        Assert.Equal(new[]
        {
            true,
            false,
            true,
            false,
            true,
            false
        }, drawn);
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

    /// <summary>
    ///     He swells as the cracks open, rather than being held still and then bursting. The two
    ///     run off the same count, so by the time he is at his widest he is also at his most
    ///     broken - which is what makes it read as one thing coming apart instead of two things
    ///     one after the other.
    /// </summary>
    [Fact]
    public void Swell_WhileTheCracksOpen_GrowsInStepWithThem()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        var splits = new List<float>();
        var scales = new List<float>();

        while (overload.Stage == OverloadStage.Cracking)
        {
            overload.Swell();
            splits.Add(overload.Split);
            scales.Add(overload.Scale);
        }

        // Assert - both climbed, neither went backwards, and he ends bigger than he started
        Assert.True(scales[^1] > scales[0], "he did not grow while cracking");
        Assert.True(scales[^1] > 1f, $"he ended the cracking at {scales[^1]}");

        for (int frame = 1; frame < scales.Count - 1; frame++)
        {
            Assert.True(scales[frame] >= scales[frame - 1], $"he shrank at frame {frame}");
            Assert.True(splits[frame] >= splits[frame - 1], $"the cracks closed at frame {frame}");
        }
    }

    /// <summary>
    ///     The cracking has to hand over to a burst that still has somewhere to go, or the swell
    ///     would be over before it started.
    /// </summary>
    [Fact]
    public void Swell_OnceTheCracksAreOpen_HandsOverToTheBlast()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        while (overload.Stage == OverloadStage.Cracking)
        {
            overload.Swell();
        }

        // Assert - come apart and bigger for it, with the blast still to come
        Assert.Equal(OverloadStage.Discharging, overload.Stage);
        Assert.True(overload.Scale > 1f, $"he came apart without growing: {overload.Scale}");
    }

    /// <summary>
    ///     Only running out of energy cracks him open. Spikes, electricity, a nasty and the debug
    ///     key all begin already exploded, and must still skip straight past it.
    /// </summary>
    [Fact]
    public void Begin_WithoutTheAlarm_DoesNotCrackHimOpenEither()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act
        overload.Begin(false);

        // Assert
        Assert.Equal(OverloadStage.Exploded, overload.Stage);
        Assert.Equal(0f, overload.Split);
    }

    /// <summary>
    ///     He only ever gets bigger now. There used to be a swell to one and a half times his size
    ///     and a shrink back through zero that drew him inside out, and the pair of them lasted
    ///     eleven frames - a fifth of a second, too quick to be read as a shape at all.
    /// </summary>
    [Fact]
    public void Swell_FromTheStart_OnlyEverGrows()
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
        } while (stage != OverloadStage.Exploded && scales.Count < TestData.LongerThanAnyDeath);

        // Assert
        Assert.Equal(OverloadStage.Exploded, stage);
        Assert.Equal(scales.OrderBy(scale => scale), scales);
        Assert.All(scales, scale => Assert.True(scale > 0f, $"drawn inside out at {scale}"));
    }

    /// <summary>
    ///     The whole death has to be quick - it is a charge letting go. Half a second from the
    ///     alarm ending to the fragments, cracking and blast together.
    /// </summary>
    [Fact]
    public void Swell_FromTheStartToTheFragments_IsOverInUnderHalfASecond()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act
        int frames = 0;

        while (overload.Swell() != OverloadStage.Exploded && frames < TestData.LongerThanAnyDeath)
        {
            frames++;
        }

        // Assert
        Assert.InRange(frames, 1, LogicClock.Seconds(0.5));
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

    /// <summary>
    ///     Nothing in the flare is ever drawn inside out. <c>FlareScales</c> keeps the guard that
    ///     leaves a negative copy out, from when he shrank back through zero on his way out; this
    ///     holds the thing that guard was there for, now that nothing can reach one.
    /// </summary>
    [Fact]
    public void FlareScales_AtEveryPointOfTheDeath_AreNeverNegative()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.Begin(true);

        // Act / Assert
        int frames = 0;

        do
        {
            Assert.All(overload.FlareScales(), scale => Assert.True(scale >= 0f));

            frames++;
        } while (overload.Swell() != OverloadStage.Exploded && frames < TestData.LongerThanAnyDeath);
    }

    private static class TestData
    {
        /// <summary>A second of alarm at fifty hertz, as <see cref="BlobOverload" /> sets it.</summary>
        public const int AlarmFrames = 60;

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

    /// <summary>
    ///     The frame counter the flicker on his skin and the shake of the picture are both read off.
    ///     It was two of Blob's fields, stepped at the top of his overload update.
    /// </summary>
    [Fact]
    public void Frame_Stepped_CountsEveryOne()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act
        overload.CountAFrame();
        overload.CountAFrame();
        overload.CountAFrame();

        // Assert
        Assert.Equal(3, overload.Frame);
    }

    /// <summary>
    ///     Deliberately carried across lives rather than reset by Begin, so two deaths start at
    ///     different points of the shake's sines and do not look identical. Nothing reading it cares
    ///     where it started: the shake's size comes from the split and the flicker is a clock.
    /// </summary>
    [Fact]
    public void Frame_ABeginAfterAnEarlierOverload_CarriesOnRatherThanStartingOver()
    {
        // Arrange
        var overload = new BlobOverload();
        overload.CountAFrame();
        overload.CountAFrame();

        // Act
        overload.Begin(true);

        // Assert
        Assert.Equal(2, overload.Frame);
    }

    /// <summary>
    ///     The arc frame is the frame wrapped to the length of the electricity run, so the beams
    ///     crawl as well as turn - and it is derived rather than held, so it cannot drift from the
    ///     frame it comes off.
    /// </summary>
    [Fact]
    public void ArcFrame_AcrossMoreThanOneRunOfTheArtwork_WrapsToTheRunRatherThanRunningOff()
    {
        // Arrange
        var overload = new BlobOverload();

        // Act and assert
        for (int frame = 0; frame < OverloadSettings.BeamArcFrames * 3; frame++)
        {
            Assert.Equal(frame % OverloadSettings.BeamArcFrames, overload.ArcFrame);
            Assert.InRange(overload.ArcFrame, 0, OverloadSettings.BeamArcFrames - 1);
            overload.CountAFrame();
        }
    }
}
