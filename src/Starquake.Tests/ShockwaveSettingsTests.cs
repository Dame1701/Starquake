// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The numbers behind the ring a blast throws out. What it looks like can only be judged by
///     watching one go off. What is held here is the travel the shader is driven by, and the spans
///     it is handed as reciprocals, where a radius of zero would divide by zero.
/// </summary>
public class ShockwaveSettingsTests
{
    private const int Frames = 30;

    [Fact]
    public void RadiusAt_AcrossTheBlast_RunsFromTheSmallestRingToTheWholeQuad()
    {
        // Assert - struck, halfway and gone
        Assert.InRange(ShockwaveSettings.RadiusAt(0, Frames), 0f, 0.1f);
        Assert.InRange(ShockwaveSettings.RadiusAt(Frames / 2, Frames), 0.6f, 0.7f);
        Assert.Equal(1f, ShockwaveSettings.RadiusAt(Frames, Frames));
    }

    /// <summary>
    ///     A ring that expanded evenly reads as something being drawn larger, so it slows as it
    ///     goes. Only partly, though: a fully thrown ring puts nearly half its travel into its first
    ///     quarter and is gone before there is anything to watch.
    /// </summary>
    [Fact]
    public void RadiusAt_OverTheTwoHalvesOfTheBlast_SlowsWithoutBoltingFromTheStart()
    {
        // Act
        float halfway = ShockwaveSettings.RadiusAt(Frames / 2, Frames);

        // Assert
        float first = halfway - ShockwaveSettings.RadiusAt(0, Frames);
        float second = ShockwaveSettings.RadiusAt(Frames, Frames) - halfway;

        Assert.True(first > second, $"The first half covered {first} and the second {second}.");
        Assert.True(first < second * 2f, $"The first half covered {first} and the second {second}.");
    }

    /// <summary>The same claim, measured at the quarter rather than the half.</summary>
    [Fact]
    public void RadiusAt_AQuarterOfTheWayThrough_HasNotAlreadyCoveredHalfTheGround()
        => Assert.InRange(ShockwaveSettings.RadiusAt(Frames / 4, Frames), 0.2f, 0.4f);

    /// <summary>
    ///     The front's thickness and the ripple spacing are both measured against the radius, so a
    ///     radius of zero on the frame the blast goes off would hand the shader an infinity.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(Frames)]
    [InlineData(Frames * 2)]
    public void TheSpansTheShaderIsHanded_OnAnyFrameOfTheBlast_AreRealNumbers(int frame)
    {
        // Act
        float radius = ShockwaveSettings.RadiusAt(frame, Frames);

        // Assert
        Assert.True(float.IsFinite(ShockwaveSettings.OverFrontAt(radius)));
        Assert.True(float.IsFinite(ShockwaveSettings.OverTailAt(radius)));
        Assert.True(float.IsFinite(ShockwaveSettings.WaveRateAt(radius)));
    }

    [Fact]
    public void IntensityAt_AcrossTheBlast_BurnsFullAndIsGoneByTheEnd()
    {
        // Assert
        Assert.Equal(ShockwaveSettings.Brightness, ShockwaveSettings.IntensityAt(0, Frames));
        Assert.Equal(0f, ShockwaveSettings.IntensityAt(Frames, Frames));
    }

    /// <summary>
    ///     A ring is stopped on the frame it runs out, but nothing stops the arithmetic being asked
    ///     for a frame past the end, and a negative intensity would subtract light from the room
    ///     through an additive blend.
    /// </summary>
    [Theory]
    [InlineData(-5)]
    [InlineData(Frames * 3)]
    public void TheBlast_AskedForAFrameOutsideItsLife_IsHeldToStruckOrGone(int frame)
    {
        // Assert
        Assert.InRange(ShockwaveSettings.RadiusAt(frame, Frames), 0f, 1f);
        Assert.InRange(ShockwaveSettings.IntensityAt(frame, Frames), 0f, ShockwaveSettings.Brightness);
    }

    /// <summary>
    ///     The ripples travel through the envelope as well as with it, so the phase has to move
    ///     from frame to frame rather than being anchored to the front alone.
    /// </summary>
    [Fact]
    public void WavePhaseAt_FromOneFrameToTheNext_HasMovedOn()
        => Assert.True(ShockwaveSettings.WavePhaseAt(2) > ShockwaveSettings.WavePhaseAt(1));

    /// <summary>
    ///     The two waves running round the rim have to be turning, or the edge is a fixed shape
    ///     rather than a ripple - and they have to be turning differently, or the shape they make
    ///     between them never changes however fast they go.
    /// </summary>
    [Fact]
    public void TheRimWaves_FromOneFrameToTheNext_HaveEachTurnedAndNotTogether()
    {
        // Act - the same blast, a frame apart
        const float attitude = 1.1f;

        Vector2 coarseBefore = ShockwaveSettings.ShimmerTurnAt(10, attitude);
        Vector2 coarseAfter = ShockwaveSettings.ShimmerTurnAt(11, attitude);
        Vector2 fineBefore = ShockwaveSettings.FineShimmerTurnAt(10, attitude);
        Vector2 fineAfter = ShockwaveSettings.FineShimmerTurnAt(11, attitude);

        // Assert
        Assert.NotEqual(coarseBefore, coarseAfter);
        Assert.NotEqual(fineBefore, fineAfter);

        float coarseTurned = coarseAfter.X - coarseBefore.X;
        float fineTurned = fineAfter.X - fineBefore.X;

        Assert.NotEqual(coarseTurned, fineTurned, 4);
    }

    /// <summary>
    ///     Two blasts in the same room start their rim waves in different places, or they ripple
    ///     in step and read as one effect drawn twice.
    /// </summary>
    [Fact]
    public void TheRimWaves_ForTwoBlastsDealtDifferentAttitudes_AreNotInStep()
        => Assert.NotEqual(
            ShockwaveSettings.ShimmerTurnAt(5, 0.4f),
            ShockwaveSettings.ShimmerTurnAt(5, 2.3f));
}
