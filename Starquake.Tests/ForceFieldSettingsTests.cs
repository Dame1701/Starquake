// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Tuning;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The sums behind a key-locked door's force field. The sheet of light itself can only be
///     judged by looking at it; these are the numbers handed to it, and what they have to get
///     right is that a door says which of the two things it is - barring Blob's way, or letting
///     him through.
/// </summary>
public class ForceFieldSettingsTests
{
    /// <summary>
    ///     A door at the top left of a screen. The gap is the middle-right column of its tile - see
    ///     <c>KeyLockDoor.Doorway</c> - so these are the coordinates everything else is measured
    ///     against.
    /// </summary>
    private static readonly Rectangle Doorway = new(48, 0, 32, TileMetrics.TileHeight);

    [Fact]
    public void OpennessAfter_AShutDoorBeingOpened_MovesTowardsOpen()
    {
        // Act
        float openness = ForceFieldSettings.OpennessAfter(0f, true);

        // Assert
        Assert.True(openness > 0f);
        Assert.True(openness < 1f);
    }

    /// <summary>
    ///     A number past one would start pulling the sheet further back than the open door's
    ///     setting, and past nothing would take it past the shut one - so both ends hold.
    /// </summary>
    [Fact]
    public void OpennessAfter_AtEitherEnd_StaysThere()
    {
        Assert.Equal(1f, ForceFieldSettings.OpennessAfter(1f, true));
        Assert.Equal(0f, ForceFieldSettings.OpennessAfter(0f, false));
    }

    [Fact]
    public void OpennessAfter_AnOpenDoorLosingTheKey_ShutsAgain()
        => Assert.True(ForceFieldSettings.OpennessAfter(1f, false) < 1f);

    /// <summary>
    ///     The travel is short on purpose - it is a door being told to open, not a drawbridge - but
    ///     it has to be over within a second, or Blob walks through a field still drawn as though
    ///     it were holding him.
    /// </summary>
    [Fact]
    public void OpennessAfter_StepAfterStep_IsFullyOpenInsideASecond()
    {
        // Arrange
        float openness = 0f;

        // Act
        for (int frame = 0; frame < LogicClock.FramesPerSecond; frame++)
        {
            openness = ForceFieldSettings.OpennessAfter(openness, true);
        }

        // Assert
        Assert.Equal(1f, openness);
    }

    [Fact]
    public void FlashAfter_TheFrameAfterADoorGivesWay_IsLessOfIt()
    {
        // Act
        float left = ForceFieldSettings.FlashAfter(ForceFieldSettings.NewFlash());

        // Assert
        Assert.True(left > 0f);
        Assert.True(left < ForceFieldSettings.NewFlash());
    }

    /// <summary>
    ///     The flash decays by a multiply rather than a count, so without a floor it would approach
    ///     nothing for ever and leave the door burning a shade brighter than it should for the rest
    ///     of the visit.
    /// </summary>
    [Fact]
    public void FlashAfter_LongEnough_IsNothingAtAll()
    {
        // Arrange
        float flash = ForceFieldSettings.NewFlash();

        // Act
        for (int frame = 0; frame < LogicClock.FramesPerSecond; frame++)
        {
            flash = ForceFieldSettings.FlashAfter(flash);
        }

        // Assert
        Assert.Equal(0f, flash);
    }

    /// <summary>
    ///     The one thing the shader cannot be given wrong. Under one the sheet is lit all the way
    ///     across the gap, which is a door barring the way; over one the middle empties, which is a
    ///     door Blob can walk through. The two states are those two sides of one.
    /// </summary>
    [Fact]
    public void DrawBackAt_ShutAndOpen_AreEitherSideOfTheWholeGap()
    {
        Assert.True(ForceFieldSettings.DrawBackAt(0f) < 1f);
        Assert.True(ForceFieldSettings.DrawBackAt(1f) > 1f);
    }

    /// <summary>
    ///     Channel by channel to five places rather than vector against vector: a crossfade all the
    ///     way over lands a hair short of its far end in single precision, and that is the
    ///     crossfade working rather than the colour being wrong.
    /// </summary>
    [Fact]
    public void ColourAt_ShutAndOpen_AreTheTwoColoursThemselves()
    {
        AssertSameColour(ForceFieldSettings.LockedColour, ForceFieldSettings.ColourAt(0f));
        AssertSameColour(ForceFieldSettings.OpenColour, ForceFieldSettings.ColourAt(1f));
    }

    /// <inheritdoc cref="ColourAt_ShutAndOpen_AreTheTwoColoursThemselves" />
    private static void AssertSameColour(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 5);
        Assert.Equal(expected.Y, actual.Y, 5);
        Assert.Equal(expected.Z, actual.Z, 5);
    }

    /// <summary>
    ///     Halfway through opening, the door is neither colour. That is the whole point of easing
    ///     it: the field used to switch from one to the other on the frame Blob picked the key up,
    ///     wherever on the planet he was standing.
    /// </summary>
    [Fact]
    public void ColourAt_Halfway_IsNeitherOfThem()
    {
        // Act
        Vector3 halfway = ForceFieldSettings.ColourAt(0.5f);

        // Assert
        Assert.NotEqual(ForceFieldSettings.LockedColour, halfway);
        Assert.NotEqual(ForceFieldSettings.OpenColour, halfway);
    }

    [Fact]
    public void BraceAgainst_BlobStandingInTheGap_IsAsHardAsItGets()
        => Assert.Equal(1f, ForceFieldSettings.BraceAgainst(Doorway, 40, 32));

    [Fact]
    public void BraceAgainst_BlobAcrossTheScreen_IsNothingAtAll()
        => Assert.Equal(0f, ForceFieldSettings.BraceAgainst(Doorway, 600, 32));

    /// <summary>
    ///     And on the way in it rises. Measured from Blob's own square rather than from the corner
    ///     the planet is told about: his corner is sixty-four pixels from the side of him that is
    ///     doing the leaning, which is two thirds of the reach.
    /// </summary>
    [Fact]
    public void BraceAgainst_BlobWalkingUpToTheDoor_RisesAsHeCloses()
    {
        // Act
        float farOff = ForceFieldSettings.BraceAgainst(Doorway, -40, 32);
        float closer = ForceFieldSettings.BraceAgainst(Doorway, -20, 32);

        // Assert
        Assert.True(closer > farOff);
        Assert.True(farOff > 0f);
    }

    /// <summary>
    ///     A door that has let Blob through is holding nothing back, so leaning on it does nothing.
    ///     Without this an open door burns hardest at exactly the moment he walks through it.
    /// </summary>
    [Fact]
    public void IntensityAt_AnOpenDoor_DoesNotBraceAgainstHim()
        => Assert.Equal(
            ForceFieldSettings.IntensityAt(1f, 0f, 0),
            ForceFieldSettings.IntensityAt(1f, 1f, 0));

    [Fact]
    public void IntensityAt_AShutDoorWithBlobOnIt_BurnsHarderThanOneHeIsAwayFrom()
        => Assert.True(
            ForceFieldSettings.IntensityAt(0f, 1f, 0) > ForceFieldSettings.IntensityAt(0f, 0f, 0));

    /// <summary>
    ///     The scanner works its way down the gap and starts again, so it has to run from the top
    ///     to the bottom and never past it - a bar drawn outside the doorway is a bar nobody sees.
    /// </summary>
    [Fact]
    public void ScanAt_OverItsTravel_RunsDownTheGapAndStartsAgain()
    {
        // Arrange
        float furthest = 0f;
        var started = false;

        // Act - two seconds, which is more than one trip down
        for (int frame = 0; frame < LogicClock.FramesPerSecond * 2; frame++)
        {
            float at = ForceFieldSettings.ScanAt(frame);

            Assert.InRange(at, 0f, 1f);

            started |= at < furthest;
            furthest = Math.Max(furthest, at);
        }

        // Assert
        Assert.True(started);
        Assert.Equal(0f, ForceFieldSettings.ScanAt(0));
    }

    /// <summary>
    ///     The bands travel, which is the whole of why the phase is taken off the door's own frame
    ///     count rather than being a constant.
    /// </summary>
    [Fact]
    public void PhaseAt_LaterInTheVisit_HasMovedOn()
        => Assert.True(ForceFieldSettings.PhaseAt(10) > ForceFieldSettings.PhaseAt(0));
}
