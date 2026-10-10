// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Constants;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     A value travelling from one end to the other a step per frame. Nothing tells it which way to
///     go: the direction follows from the two ends it is given, which is what these pin.
/// </summary>
public class RampedValueTests
{
    [Fact]
    public void Value_BeforeAnyStep_IsWhereItStarts()
    {
        // Arrange
        var shrinking = new RampedValue(TestData.ElementShrinkFrom, TestData.ElementScaleTo, TestData.ShrinkStep);

        // Assert
        Assert.Equal(TestData.ElementShrinkFrom, shrinking.Value);
    }

    [Fact]
    public void Update_WithTheFinalValueBelowTheStart_TravelsDown()
    {
        // Arrange
        var shrinking = new RampedValue(TestData.ElementShrinkFrom, TestData.ElementScaleTo, TestData.ShrinkStep);

        // Act
        shrinking.Update();

        // Assert
        Assert.Equal(2.9f, shrinking.Value, TestData.Tolerance);
    }

    [Fact]
    public void Update_WithTheFinalValueAboveTheStart_TravelsUp()
    {
        // Arrange
        var growing = new RampedValue(0f, TestData.ElementScaleTo, TestData.GrowStep);

        // Act
        growing.Update();

        // Assert
        Assert.Equal(0.05f, growing.Value, TestData.Tolerance);
    }

    [Fact]
    public void Update_SteppedPastTheEnd_RestsExactlyThere()
    {
        // Arrange
        var growing = new RampedValue(0f, TestData.ElementScaleTo, TestData.GrowStep);

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheRampNeeds; frame++)
        {
            growing.Update();
        }

        // Assert
        Assert.Equal(TestData.ElementScaleTo, growing.Value);
    }

    /// <summary>
    ///     The fade that crosses between the element leaving and the element arriving. Read as a
    ///     whole colour level rather than a float, and it steps through the same levels either way.
    /// </summary>
    [Fact]
    public void ColourLevel_SteppedDownFromFull_CountsOffWholeLevels()
    {
        // Arrange
        var fading = new RampedValue(ColourLevels.Full, ColourLevels.None, TestData.FadeStep);

        // Act
        fading.Update();
        fading.Update();

        // Assert
        Assert.Equal(ColourLevels.Full - 2 * TestData.FadeStep, fading.ColourLevel);
    }

    [Fact]
    public void ColourLevel_SteppedToTheEnd_ReachesNoneAndRestsThere()
    {
        // Arrange
        var fading = new RampedValue(ColourLevels.Full, ColourLevels.None, TestData.FadeStep);

        // Act
        for (int frame = 0; frame < TestData.MoreFramesThanTheRampNeeds; frame++)
        {
            fading.Update();
        }

        // Assert
        Assert.Equal(ColourLevels.None, fading.ColourLevel);
    }

    /// <summary>
    ///     Both ends the same is the one case where there is no direction to derive. It has to be a
    ///     value that does not move, rather than one that drifts off whichever way the test guessed.
    /// </summary>
    [Fact]
    public void Update_WithBothEndsTheSame_NeverMoves()
    {
        // Arrange
        var still = new RampedValue(TestData.ElementScaleTo, TestData.ElementScaleTo, TestData.GrowStep);

        // Act
        still.Update();
        still.Update();

        // Assert
        Assert.Equal(TestData.ElementScaleTo, still.Value);
    }

    private static class TestData
    {
        public const float ElementShrinkFrom = 3f;

        public const float ElementScaleTo = 1f;

        public const float ShrinkStep = 0.1f;

        public const float GrowStep = 0.05f;

        public const int FadeStep = 8;

        public const int MoreFramesThanTheRampNeeds = 100;

        /// <summary>
        ///     Decimal places to compare a float to, for the steps that do not land on a value a
        ///     float holds exactly.
        /// </summary>
        public const int Tolerance = 5;
    }
}
