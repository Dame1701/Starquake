// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The numbers behind an organic nasty coming apart. The dissolve itself can only be judged by
///     looking at it. What is held here is the decay the shader is driven by and the colour its rim
///     takes.
/// </summary>
public class SplatSettingsTests
{
    [Fact]
    public void DecayFrom_AcrossTheDeathFade_RunsFromWholeToGone()
    {
        // Assert - the fade counts down from full, so the decay counts up
        Assert.Equal(0f, SplatSettings.DecayFrom(ColourLevels.Full, ColourLevels.Full));
        Assert.Equal(0.5f, SplatSettings.DecayFrom(ColourLevels.Full / 2, ColourLevels.Full), 2);
        Assert.Equal(1f, SplatSettings.DecayFrom(ColourLevels.None, ColourLevels.Full));
    }

    /// <summary>
    ///     A decay outside nought to one would hand the shader a threshold past either end of its
    ///     field, which would show as a corpse already part eaten the moment it was shot, or a
    ///     scrap of one left behind after it should have gone.
    /// </summary>
    [Theory]
    [InlineData(-40)]
    [InlineData(400)]
    public void DecayFrom_AFadeOutsideItsRange_IsHeldToWholeOrGone(int fadeLevel)
        => Assert.InRange(SplatSettings.DecayFrom(fadeLevel, ColourLevels.Full), 0f, 1f);

    /// <summary>
    ///     The rim takes the colour that kind of nasty already throws from its left side, so the
    ///     dissolve and the particles playing over it are the same colour.
    /// </summary>
    [Theory]
    [InlineData(NastyType.FriedEgg)]
    [InlineData(NastyType.Snail)]
    [InlineData(NastyType.Centipede)]
    internal void SplatColourFor_ANastyWithColours_IsTheOneItAlreadySplatters(NastyType nastyType)
    {
        // Act
        Vector3 rim = NastyColours.SplatColourFor(nastyType);

        // Assert - a real colour from the table rather than the white fallback
        Assert.NotEqual(Vector3.One, rim);
        Assert.InRange(rim.X, 0f, 1f);
        Assert.InRange(rim.Y, 0f, 1f);
        Assert.InRange(rim.Z, 0f, 1f);
    }

    /// <summary>
    ///     The mechanical nasties are not in the colour table and never reach the dissolve, so
    ///     asking anyway has to answer rather than throw.
    /// </summary>
    [Theory]
    [InlineData(NastyType.Spiky)]
    [InlineData(NastyType.Conveyor)]
    internal void SplatColourFor_AMechanicalNasty_FallsBackToWhiteRatherThanThrowing(
        NastyType nastyType)
        => Assert.Equal(Vector3.One, NastyColours.SplatColourFor(nastyType));
}
