// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The numbers behind Blob overloading: how far the charge has got, and what colour each shell
///     of the discharge burns. The burning itself can only be judged by looking at it.
/// </summary>
public class OverloadSettingsTests
{
    [Fact]
    public void ChargeFrom_AcrossTheAlarm_RunsFromNothingToFull()
    {
        // Assert - the drain counts down from full, so the charge counts up
        Assert.Equal(0f, OverloadSettings.ChargeFrom(ColourLevels.Full));
        Assert.Equal(0.5f, OverloadSettings.ChargeFrom(ColourLevels.Full / 2), 2);
        Assert.Equal(1f, OverloadSettings.ChargeFrom(ColourLevels.None));
    }

    /// <summary>
    ///     A charge outside zero to one would push the heat ramp off either end of itself, so an
    ///     unclamped drain counter must not be able to take the colour with it.
    /// </summary>
    [Theory]
    [InlineData(-30)]
    [InlineData(400)]
    public void ChargeFrom_ADrainOutsideItsRange_IsHeldToNothingOrFull(int drain)
        => Assert.InRange(OverloadSettings.ChargeFrom(drain), 0f, 1f);

    [Fact]
    public void ShellColour_TheInnermostShell_IsTheHottest()
        => Assert.Equal(
            OverloadSettings.HotColour,
            OverloadSettings.ShellColour(0, BlobOverload.FlareCopies));

    [Fact]
    public void ShellColour_TheOutermostShell_HasCooledToTheArcingColour()
        => Assert.Equal(
            OverloadSettings.RimColour,
            OverloadSettings.ShellColour(
                BlobOverload.FlareCopies - 1, BlobOverload.FlareCopies));

    /// <summary>
    ///     The five have to read as one front expanding rather than as five separate things, which
    ///     means each shell is cooler than the one inside it all the way out.
    /// </summary>
    [Fact]
    public void ShellColour_FromTheMiddleOutward_CoolsAllTheWay()
    {
        // Act
        List<Vector3> shells = [];

        for (int copy = 0; copy < BlobOverload.FlareCopies; copy++)
        {
            shells.Add(OverloadSettings.ShellColour(copy, BlobOverload.FlareCopies));
        }

        // Assert - red falls away and blue comes up, every step of the way out
        for (int copy = 1; copy < shells.Count; copy++)
        {
            Assert.True(shells[copy].X <= shells[copy - 1].X, $"red rose at shell {copy}");
            Assert.True(shells[copy].Z >= shells[copy - 1].Z, $"blue fell at shell {copy}");
        }
    }

    [Fact]
    public void ShakeAt_BeforeTheCracksOpen_IsStillAndGrowsAsTheyWiden()
    {
        // Act
        Vector2 atTheStart = OverloadSettings.ShakeAt(7, 0f);
        float early = OverloadSettings.ShakeAt(7, 0.25f).Length();
        float late = OverloadSettings.ShakeAt(7, 1f).Length();

        // Assert
        Assert.Equal(Vector2.Zero, atTheStart);
        Assert.True(late > early, $"shake did not grow: {early} then {late}");
    }

    /// <summary>
    ///     A shake that traced a line rather than scattering would read as Blob sliding about. The
    ///     two axes run at offset rates that do not divide one another, so the ratio between them
    ///     has to keep changing.
    /// </summary>
    [Fact]
    public void ShakeAt_OverSuccessiveFrames_MovesOnBothAxesIndependently()
    {
        // Act
        List<float> ratios = [];

        for (int frame = 0; frame < 20; frame++)
        {
            Vector2 shake = OverloadSettings.ShakeAt(frame, 1f);

            if (Math.Abs(shake.X) > 0.05f)
            {
                ratios.Add(shake.Y / shake.X);
            }
        }

        // Assert - the two axes are not locked to one another
        Assert.True(ratios.Distinct().Count() > 10, "the shake traced a line");
    }

    /// <summary>
    ///     He must not wander off while he shakes - it is a vibration, not a walk.
    /// </summary>
    [Fact]
    public void ShakeAt_HoweverLongItRuns_StaysCloseToWhereHeIs()
    {
        // Act / Assert
        for (int frame = 0; frame < 500; frame++)
        {
            Assert.InRange(OverloadSettings.ShakeAt(frame, 1f).Length(), 0f, 6f);
        }
    }

    [Fact]
    public void BeamAt_BeforeTheCracksOpen_SitsOnHimAndIsThrownOutAsTheyWiden()
    {
        // Act
        (Vector2 atTheStart, _) = OverloadSettings.BeamAt(4, 0, 0f);
        (Vector2 halfway, _) = OverloadSettings.BeamAt(4, 0, 0.5f);
        (Vector2 comeApart, _) = OverloadSettings.BeamAt(4, 0, 1f);

        // Assert
        Assert.Equal(Vector2.Zero, atTheStart);
        Assert.True(comeApart.Length() > halfway.Length(), "the beams did not open out");
    }

    /// <summary>
    ///     They have to be spread round him rather than bunched, or they read as one spark rather
    ///     than a field whipping about.
    /// </summary>
    [Fact]
    public void BeamAt_AcrossTheBeams_SpreadsThemEvenlyAroundHim()
    {
        // Act
        List<float> angles = [];

        for (int beam = 0; beam < OverloadSettings.BeamCount; beam++)
        {
            (_, float angle) = OverloadSettings.BeamAt(0, beam, 1f);
            angles.Add(angle);
        }

        // Assert - each one the same step round from the last
        float step = angles[1] - angles[0];

        for (int beam = 2; beam < angles.Count; beam++)
        {
            Assert.Equal(step, angles[beam] - angles[beam - 1], 4);
        }

        // ...and a whole turn between them, so the fifth does not land back on the first
        Assert.Equal(2 * Math.PI, step * OverloadSettings.BeamCount, 4);
    }

    [Fact]
    public void BeamAt_FromOneFrameToTheNext_TurnsAllOfThemTogether()
    {
        // Act
        (_, float firstNow) = OverloadSettings.BeamAt(10, 0, 1f);
        (_, float firstNext) = OverloadSettings.BeamAt(11, 0, 1f);
        (_, float lastNow) = OverloadSettings.BeamAt(10, 1, 1f);
        (_, float lastNext) = OverloadSettings.BeamAt(11, 1, 1f);

        // Assert - both moved, and by the same amount
        Assert.NotEqual(firstNow, firstNext);
        Assert.Equal(firstNext - firstNow, lastNext - lastNow, 4);
    }

    /// <summary>
    ///     They are sparks off him, so they must not fly away across the screen however long the
    ///     cracking runs.
    /// </summary>
    [Fact]
    public void BeamAt_HoweverLongItRuns_StaysCloseToHim()
    {
        // Act / Assert
        for (int frame = 0; frame < 200; frame++)
        {
            for (int beam = 0; beam < OverloadSettings.BeamCount; beam++)
            {
                (Vector2 offset, _) = OverloadSettings.BeamAt(frame, beam, 1f);

                Assert.InRange(offset.Length(), 0f, 64f);
            }
        }
    }

    /// <summary>
    ///     A flare of one would divide by nought working out how far out that shell is.
    /// </summary>
    [Fact]
    public void ShellColour_ASingleShell_IsTheHottestRatherThanUndefined()
    {
        // Act
        Vector3 only = OverloadSettings.ShellColour(0, 1);

        // Assert
        Assert.Equal(OverloadSettings.HotColour, only);
    }
}
