// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Input;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     <see cref="AnalogueWalk" /> reads the stick as eight directions and turns its lean into a
///     speed, spread evenly so every walk still stops on the exact positions the tunnel lift, the
///     shuttle pad and a gap overhead need.
/// </summary>
public class AnalogueWalkTests
{
    private const int FullStep = 8;

    private const int Cell = TileMetrics.CollisionCellWidth;

    /// <summary>
    ///     The diagonal that went wrong: after the pad's dead zone a push a little shallower than 45°
    ///     falls short of half-way on its vertical axis, so the digital stick read it as sideways only.
    /// </summary>
    [Fact]
    public void Leans_AShallowDiagonal_CountsAsBothDirections()
    {
        Assert.True(AnalogueWalk.Leans(0.43f, 0.76f));
        Assert.True(AnalogueWalk.Leans(0.76f, 0.43f));
    }

    /// <summary>A push mostly down must not also walk, or it would not lay a platform.</summary>
    [Fact]
    public void Leans_APushMostlyAlongTheOtherAxis_DoesNotCount()
        => Assert.False(AnalogueWalk.Leans(0.3f, 0.9f));

    [Theory]
    [InlineData(0.05f)]
    [InlineData(-0.5f)]
    public void Leans_TooLittleOrTheOtherWay_DoesNotCount(float along)
        => Assert.False(AnalogueWalk.Leans(along, 0f));

    [Theory]
    [InlineData(0.1f, 1)]
    [InlineData(0.9f, FullStep)]
    [InlineData(1f, FullStep)]
    [InlineData(-1f, FullStep)]
    public void StepFor_FromTheSmallestLeanToNearlyTheRim_RunsFromOnePixelToTheFullStep(float lean, int expected)
        => Assert.Equal(expected, AnalogueWalk.StepFor(lean, 0, false, FullStep));

    /// <summary>
    ///     The smoothness. Whatever the speed, the steps across a cell differ by a pixel at most, so the
    ///     walk does not lurch.
    /// </summary>
    [Theory]
    [InlineData(0.3f)]
    [InlineData(0.5f)]
    [InlineData(0.65f)]
    [InlineData(0.8f)]
    public void StepFor_AcrossACell_TheStepsDifferByAPixelAtMost(float lean)
    {
        List<int> steps = Walk(lean, 0, false, Cell);

        Assert.InRange(steps.Max() - steps.Min(), 0, 1);
    }

    [Theory]
    [InlineData(0.3f)]
    [InlineData(0.5f)]
    [InlineData(0.65f)]
    [InlineData(0.8f)]
    public void StepFor_LeaningFurther_NeverWalksSlower(float lean)
        => Assert.True(Walk(lean + 0.1f, 0, false, Cell).Count <= Walk(lean, 0, false, Cell).Count);

    /// <summary>
    ///     The headline behaviour. From anywhere and at any speed, the walk stops on every cell edge
    ///     it passes, which is where the lift and the shuttle pad take Blob.
    /// </summary>
    [Theory]
    [InlineData(3, 0.4f, false)]
    [InlineData(61, 0.7f, true)]
    [InlineData(5, 1f, false)]
    [InlineData(100, 0.55f, true)]
    public void StepFor_FromAnywhereAtAnySpeed_StopsOnEveryCellEdgePassed(int start, float lean, bool leftwards)
    {
        var visited = new List<int>();
        int xPos = start;

        for (int frame = 0; frame < 60; frame++)
        {
            int step = AnalogueWalk.StepFor(lean, xPos, leftwards, FullStep);
            xPos += leftwards ? -step : step;
            visited.Add(xPos);
        }

        int low = Math.Min(start, xPos);
        int high = Math.Max(start, xPos);

        for (int edge = (low / Cell + 1) * Cell; edge < high; edge += Cell)
        {
            Assert.Contains(edge, visited);
        }
    }

    /// <summary>The steps taken to walk <paramref name="distance" /> from <paramref name="start" />.</summary>
    private static List<int> Walk(float lean, int start, bool leftwards, int distance)
    {
        var steps = new List<int>();
        int walked = 0;

        while (walked < distance)
        {
            int step = AnalogueWalk.StepFor(lean, start + (leftwards ? -walked : walked), leftwards, FullStep);
            steps.Add(step);
            walked += step;
        }

        return steps;
    }
}
