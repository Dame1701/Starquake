// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Display;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Where the water on Olly's screen sits. How it looks can only be judged by watching it; the
///     waterline is arithmetic and is held here, because it is placed against Olly rather than
///     chosen by eye.
/// </summary>
public class WaterSettingsTests
{
    /// <summary>Olly's artwork runs between these, measured off the sprites he is drawn from.</summary>
    private const int OllyTop = 172;

    /// <inheritdoc cref="OllyTop" />
    private const int OllyBottom = 330;

    /// <summary>
    ///     The whole point of where the line is: the top of his head is in the air and the rest of
    ///     him is under.
    /// </summary>
    [Fact]
    public void WaterlineY_SitsJustBelowTheTopOfOllysHead()
    {
        // Assert
        Assert.True(WaterSettings.WaterlineY > OllyTop, "Olly is entirely above the water");
        Assert.True(WaterSettings.WaterlineY < OllyBottom, "Olly is entirely under the water");

        // ...and only just below, so it is the top of his head showing and not his shoulders
        Assert.InRange(WaterSettings.WaterlineY - OllyTop, 1, 40);
    }

    [Fact]
    public void WaterlineIn01_IsTheWaterlineAsAFractionOfThePlayfield()
        => Assert.Equal(
            WaterSettings.WaterlineY / (float)TileMetrics.PlayAreaHeight,
            WaterSettings.WaterlineIn01,
            5);

    /// <summary>
    ///     The shader works in fractions of the picture, so both of these have to land inside it or
    ///     the water would either start off the top of the screen or never reach its full colour.
    /// </summary>
    [Fact]
    public void WaterlineIn01_IsInsideThePicture()
        => Assert.InRange(WaterSettings.WaterlineIn01, 0f, 1f);

    [Fact]
    public void OverFullDepthIn01_ReachesFullColourBeforeTheBottomOfTheScreen()
    {
        // Act - how far below the surface, as a fraction, the water is fully coloured
        float fullAt = WaterSettings.WaterlineIn01 + 1f / WaterSettings.OverFullDepthIn01;

        // Assert
        Assert.InRange(fullAt, WaterSettings.WaterlineIn01, 1f);
    }

    [Fact]
    public void PhaseAt_OverSuccessiveFrames_KeepsTheWavesMovingOneWay()
    {
        // Act / Assert
        for (int frame = 1; frame < 200; frame++)
        {
            Assert.True(
                WaterSettings.PhaseAt(frame) > WaterSettings.PhaseAt(frame - 1),
                $"the water stopped moving at frame {frame}");
        }
    }

    /// <summary>
    ///     The push is a fraction of the picture, and a small one: a hundredth of the screen is
    ///     already a pronounced wobble and much more reads as a funhouse mirror rather than water.
    /// </summary>
    [Fact]
    public void Amplitude_IsASmallFractionOfThePicture()
        => Assert.InRange(WaterSettings.Amplitude, 0f, 0.02f);
}
