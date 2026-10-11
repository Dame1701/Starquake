// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Core;
using Xunit;

namespace Starquake.Tests;

/// <summary>F11's switch between the PAL and NTSC speeds.</summary>
public sealed class LogicRateTests
{
    [Fact]
    public void Switching_GoesBetweenTheTwoRates()
    {
        // Arrange
        var rate = new LogicRate(LogicRate.Pal);

        // Act & Assert
        rate.Switch();
        Assert.Equal(LogicRate.Ntsc, rate.Hertz);

        rate.Switch();
        Assert.Equal(LogicRate.Pal, rate.Hertz);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(55)]
    [InlineData(144)]
    public void ARateThatIsNeitherOfTheTwo_StartsAtPal(int hertz)
        => Assert.Equal(LogicRate.Pal, new LogicRate(hertz).Hertz);

    [Theory]
    [InlineData(LogicRate.Pal, 20)]
    [InlineData(LogicRate.Ntsc, 16.666)]
    public void AFrame_TakesTheRightShareOfARealSecond(int hertz, double milliseconds)
        => Assert.Equal(milliseconds, new LogicRate(hertz).FrameLength.TotalMilliseconds, 2);
}
