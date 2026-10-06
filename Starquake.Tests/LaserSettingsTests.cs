// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The one rule behind Blob's shot that is not a matter of looking at it: a shuttle pad's fire
///     is the powered one, and what he has in his own hands is weaker in every respect.
/// </summary>
public class LaserSettingsTests
{
    [Fact]
    public void BeamFor_OffTheShuttle_IsShorterThinnerAndDimmerThanOnIt()
    {
        // Act
        (int length, int thickness, float intensity) = LaserSettings.BeamFor(false);
        (int padLength, int padThickness, float padIntensity) = LaserSettings.BeamFor(true);

        // Assert
        Assert.True(length < padLength, $"on foot it was {length} long against the pad's {padLength}");
        Assert.True(thickness < padThickness, "on foot it was no thinner");
        Assert.True(intensity < padIntensity, "on foot it burned no dimmer");
    }

    /// <summary>
    ///     Weaker, but still there. A beam rounded down to nothing would leave firing on foot
    ///     looking broken rather than looking weak.
    /// </summary>
    [Fact]
    public void BeamFor_OffTheShuttle_IsStillABeam()
    {
        // Act
        (int length, int thickness, float intensity) = LaserSettings.BeamFor(false);

        // Assert
        Assert.True(length > 0, "the on-foot beam had no length");
        Assert.True(thickness > 0, "the on-foot beam had no thickness");
        Assert.True(intensity > 0, "the on-foot beam had no light in it");
    }

    /// <summary>
    ///     The beam is the light of the shot, and the shot is thirty-two long. Much longer than
    ///     that and it reads as a bar flying along beside him rather than as the shot burning.
    /// </summary>
    [Fact]
    public void BeamFor_EitherWay_IsNotMuchLongerThanTheShotItself()
    {
        // Assert
        Assert.InRange(LaserSettings.BeamFor(true).Length, 16, 64);
        Assert.InRange(LaserSettings.BeamFor(false).Length, 8, 64);
    }

    /// <summary>
    ///     It is a beam and not a slab: longer than it is thick, however it is being fired.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BeamFor_EitherWay_IsLongerThanItIsThick(bool onShuttle)
    {
        // Act
        (int length, int thickness, _) = LaserSettings.BeamFor(onShuttle);

        // Assert
        Assert.True(length > thickness * 2, $"{length} by {thickness} is not a beam");
    }
}
