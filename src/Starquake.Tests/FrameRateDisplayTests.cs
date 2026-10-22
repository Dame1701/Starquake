// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     F6's reading. It is counted from the time between draws, so it can be checked by handing
///     it frame lengths - no graphics device, no font.
/// </summary>
public class FrameRateDisplayTests
{
    [Fact]
    public void ASteadySecond_ReadsTheRateAndTheFrameLength()
    {
        // Arrange
        var frameRate = new FrameRateDisplay();

        // Act
        Count(frameRate, TestData.FiftyHertzFrame, 50);

        // Assert
        Assert.Equal("50 FPS   LONGEST 20.0MS", frameRate.Reading);
    }

    [Fact]
    public void ASecondWithHitches_ReadsTheLongestFrame()
    {
        // Arrange
        var frameRate = new FrameRateDisplay();

        // Act
        Count(frameRate, TestData.FiftyHertzFrame, 40);
        Count(frameRate, TestData.DoubledFrame, 5);

        // Assert
        Assert.Equal("45 FPS   LONGEST 40.0MS", frameRate.Reading);
    }

    [Fact]
    public void TheNextSecond_ForgetsTheHitchesOfTheLast()
    {
        // Arrange
        var frameRate = new FrameRateDisplay();
        Count(frameRate, TestData.FiftyHertzFrame, 40);
        Count(frameRate, TestData.DoubledFrame, 5);

        // Act
        Count(frameRate, TestData.FiftyHertzFrame, 50);

        // Assert
        Assert.Equal("50 FPS   LONGEST 20.0MS", frameRate.Reading);
    }

    [Fact]
    public void LessThanASecond_ReadsNothingYet()
    {
        // Arrange
        var frameRate = new FrameRateDisplay();

        // Act
        Count(frameRate, TestData.FiftyHertzFrame, 49);

        // Assert
        Assert.Empty(frameRate.Reading);
    }

    private static void Count(FrameRateDisplay frameRate, TimeSpan frameLength, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            frameRate.Count(frameLength);
        }
    }

    private static class TestData
    {
        public static readonly TimeSpan FiftyHertzFrame = TimeSpan.FromMilliseconds(20);

        public static readonly TimeSpan DoubledFrame = TimeSpan.FromMilliseconds(40);
    }
}
