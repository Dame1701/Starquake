// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The two sums behind a nasty draining Blob. The reddening itself can only be judged by
///     looking at it. What is held here is how far gone his energy is, and whether there is any
///     drain to draw at all.
/// </summary>
public class EnergyDrainSettingsTests
{
    /// <summary>
    ///     The counter is the green and blue he has left, so it reads full when he is unhurt. The
    ///     shader wants the opposite, which is why this is not just the counter over its range.
    /// </summary>
    [Fact]
    public void DrainFrom_AcrossTheCounter_RunsTheOppositeWayToIt()
    {
        // Assert
        Assert.Equal(0f, EnergyDrainSettings.DrainFrom(ColourLevels.Full));
        Assert.Equal(0.5f, EnergyDrainSettings.DrainFrom(ColourLevels.Full / 2), 2);
        Assert.Equal(1f, EnergyDrainSettings.DrainFrom(ColourLevels.None));
    }

    /// <summary>
    ///     A drain outside zero to one would push the wash past the colour it is meant to reach, or
    ///     back out the other side of his own grey.
    /// </summary>
    [Theory]
    [InlineData(-50)]
    [InlineData(400)]
    public void DrainFrom_ACounterOutsideItsRange_IsHeldToNothingOrAll(int counter)
        => Assert.InRange(EnergyDrainSettings.DrainFrom(counter), 0f, 1f);

    /// <summary>
    ///     He is untouched nearly all the time, and running a shader over every pixel of him to
    ///     change nothing is the one case worth skipping.
    /// </summary>
    [Fact]
    public void IsDraining_WhenHeIsUntouched_IsFalseSoTheShaderIsSkipped()
    {
        // Assert
        Assert.False(EnergyDrainSettings.IsDraining(ColourLevels.Full));
        Assert.True(EnergyDrainSettings.IsDraining(ColourLevels.Full - 1));
        Assert.True(EnergyDrainSettings.IsDraining(ColourLevels.None));
    }

    /// <summary>
    ///     The two have to agree about the one case that matters. If there is nothing to draw, the
    ///     drain must be zero, or the frame the shader is skipped on would jump.
    /// </summary>
    [Fact]
    public void IsDraining_AndDrainFrom_AgreeAboutHimBeingUntouched()
    {
        // Act
        bool draining = EnergyDrainSettings.IsDraining(ColourLevels.Full);
        float drain = EnergyDrainSettings.DrainFrom(ColourLevels.Full);

        // Assert
        Assert.False(draining);
        Assert.Equal(0f, drain);
    }
}
