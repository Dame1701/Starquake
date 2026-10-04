// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How Blob's six parts are thrown when he comes apart. They used to be a fountain - every one
///     upward, differing only in how far it leaned - and are the points of a star now.
/// </summary>
public class BouncingParticleTests
{
    private const int Points = 6;

    private const int Speed = 24;

    /// <summary>
    ///     How far off a perfect star the rounding puts things. A velocity is whole pixels a frame,
    ///     so each axis can be half a pixel out, which at this speed is about seven tenths of a
    ///     pixel of length and three hundredths of a radian of direction. The star is as even as
    ///     integers allow and the tests ask for no more than that.
    /// </summary>
    private const double Rounding = 0.08;

    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsThemEvenlyAroundTheCircle()
    {
        // Act
        List<double> angles = [];

        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 0);
            angles.Add(Math.Atan2(thrown.Y, thrown.X));
        }

        // Assert - each the same step round from the last, allowing for the wrap
        for (int point = 1; point < Points; point++)
        {
            double step = angles[point] - angles[point - 1];

            if (step < 0)
            {
                step += 2 * Math.PI;
            }

            Assert.InRange(step, 2 * Math.PI / Points - Rounding, 2 * Math.PI / Points + Rounding);
        }
    }

    /// <summary>
    ///     The thing that makes it a burst rather than a fountain: some parts go up and some go
    ///     down, where every one of them used to go up.
    /// </summary>
    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsSomeUpAndSomeDown()
    {
        // Act
        List<Point> thrown = [];

        for (int point = 0; point < Points; point++)
        {
            thrown.Add(BouncingParticle.StarburstVelocity(point, Points, Speed, 0));
        }

        // Assert - y counts downward, so negative is up
        Assert.Contains(thrown, part => part.Y < 0);
        Assert.Contains(thrown, part => part.Y > 0);
        Assert.Contains(thrown, part => part.X < 0);
        Assert.Contains(thrown, part => part.X > 0);
    }

    /// <summary>
    ///     Every part is thrown as hard as every other - the star has no short points.
    /// </summary>
    [Fact]
    public void StarburstVelocity_AcrossThePoints_ThrowsThemAllAtTheSameSpeed()
    {
        // Act / Assert
        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 0.7);

            Assert.InRange(
                Math.Sqrt(thrown.X * thrown.X + thrown.Y * thrown.Y), Speed - 1, Speed + 1);
        }
    }

    /// <summary>
    ///     Turning the whole star turns every point of it by the same amount, so the parts stay
    ///     evenly spread however the burst is oriented.
    /// </summary>
    [Fact]
    public void StarburstVelocity_WithTheStarTurned_KeepsThePointsEvenlySpread()
    {
        // Act
        List<double> angles = [];

        for (int point = 0; point < Points; point++)
        {
            Point thrown = BouncingParticle.StarburstVelocity(point, Points, Speed, 1.9);
            angles.Add(Math.Atan2(thrown.Y, thrown.X));
        }

        // Assert
        for (int point = 1; point < Points; point++)
        {
            double step = angles[point] - angles[point - 1];

            if (step < 0)
            {
                step += 2 * Math.PI;
            }

            Assert.InRange(step, 2 * Math.PI / Points - Rounding, 2 * Math.PI / Points + Rounding);
        }
    }

    /// <summary>
    ///     The sideways speed is the one thing nothing afterwards ever changes - no gravity acts
    ///     along it and nothing damps it - so a part thrown with none of it has none for the whole
    ///     of its life: straight up, straight back down the same line, and bouncing on that spot
    ///     until its time runs out. The ship crash is where that showed, because it throws the
    ///     parts without drawing them and all that is seen is the fire each one trails.
    /// </summary>
    /// <remarks>
    ///     Every turn of the star rather than a handful: a point lands near enough to the vertical
    ///     to round away about one crash in twenty, so a test of three spins would have passed on
    ///     the code this was written against.
    /// </remarks>
    [Fact]
    public void StarburstVelocity_HoweverTheStarIsTurned_ThrowsNoPartStraightUpOrStraightDown()
    {
        // Act / Assert - a tenth of a degree at a time, all the way round
        for (int tenths = 0; tenths < 3600; tenths++)
        {
            double spin = tenths * Math.PI / 1800;

            for (int point = 0; point < Points; point++)
            {
                Assert.NotEqual(0, BouncingParticle.StarburstVelocity(point, Points, Speed, spin).X);
            }
        }
    }

    /// <summary>
    ///     And it is thrown the way it was aimed. Rounding a cosine of nearly nothing up to one
    ///     whole pixel must not throw a part that was aimed leftwards off to the right.
    /// </summary>
    [Theory]
    [InlineData(0.5 * Math.PI - 0.001, 1)]
    [InlineData(0.5 * Math.PI + 0.001, -1)]
    public void StarburstVelocity_AimedWithinADegreeOfTheVertical_KeepsTheSideItWasAimedAt(
        double spin, int sideways)
        => Assert.Equal(sideways, BouncingParticle.StarburstVelocity(0, Points, Speed, spin).X);

    /// <summary>
    ///     A star of one point would divide by nought working out the step between them.
    /// </summary>
    [Fact]
    public void StarburstVelocity_WithNoPointsAtAll_IsStillThrownSomewhere()
    {
        // Act
        Point thrown = BouncingParticle.StarburstVelocity(0, 0, Speed, 0);

        // Assert
        Assert.InRange(Math.Sqrt(thrown.X * thrown.X + thrown.Y * thrown.Y), Speed - 1, Speed + 1);
    }
}
