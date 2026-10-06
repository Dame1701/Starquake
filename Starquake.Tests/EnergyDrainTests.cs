// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Constants;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     How far gone Blob's energy is while a nasty has hold of him, and how it comes back. Three
///     mutable fields and a sixteen-line branch inside <c>Blob</c> until now, which is why none of
///     it was reachable.
/// </summary>
/// <remarks>
///     The level is the green and blue he has left, so full is unhurt and nought is a nasty having
///     taken all of it.
/// </remarks>
public class EnergyDrainTests
{
    private const int Step = 16;

    private static EnergyDrain Touched(int frames)
    {
        var drain = new EnergyDrain();

        for (int frame = 0; frame < frames; frame++)
        {
            drain.Step(true);
        }

        return drain;
    }

    [Fact]
    public void Level_Untouched_StartsUnhurt()
        => Assert.Equal(ColourLevels.Full, new EnergyDrain().Level);

    [Fact]
    public void Draining_Untouched_HasNothingToDraw()
        => Assert.False(new EnergyDrain().Draining);

    /// <summary>
    ///     The shader is skipped entirely when nothing is draining him, which is nearly all the
    ///     time - so the first frame of a touch is what has to turn it on.
    /// </summary>
    [Fact]
    public void Draining_OneFrameOfATouch_HasSomethingToDraw()
        => Assert.True(Touched(1).Draining);

    [Fact]
    public void Level_OneFrameOfATouch_IsOneStepDown()
        => Assert.Equal(ColourLevels.Full - Step, Touched(1).Level);

    /// <summary>
    ///     It stops at nought rather than going past it - the level is a colour channel and the
    ///     shader divides by the full range.
    /// </summary>
    [Fact]
    public void Level_HeldLongEnoughToTakeEverything_StopsAtNought()
        => Assert.Equal(ColourLevels.None, Touched(1000).Level);

    [Fact]
    public void Level_LetGoOf_ClimbsBackAStepAtATime()
    {
        // Arrange - most of the way down
        EnergyDrain drain = Touched(8);
        int drained = drain.Level;

        // Act
        drain.Step(false);

        // Assert
        Assert.Equal(drained + Step, drain.Level);
    }

    [Fact]
    public void Level_LeftAloneLongEnough_IsUnhurtAgainAndStopsThere()
    {
        // Arrange
        EnergyDrain drain = Touched(1000);

        // Act
        for (int frame = 0; frame < 1000; frame++)
        {
            drain.Step(false);
        }

        // Assert
        Assert.Equal(ColourLevels.Full, drain.Level);
        Assert.False(drain.Draining);
    }

    /// <summary>
    ///     The two directions are not symmetrical, and this is the whole of the feel. Being touched
    ///     snaps the level back to full before taking it down, so a drain always starts from the
    ///     same place however drained he already was.
    /// </summary>
    [Fact]
    public void Level_TouchedAgainBeforeHeHasRecovered_StartsTheDrainOverRatherThanContinuingIt()
    {
        // Arrange - drained a long way, then let go of for a single frame
        EnergyDrain drain = Touched(8);
        drain.Step(false);

        // Act - caught again
        drain.Step(true);

        // Assert - one step down from unhurt, not one step down from where he was
        Assert.Equal(ColourLevels.Full - Step, drain.Level);
    }

    /// <summary>
    ///     Without that snap, a nasty catching Blob twice in quick succession would finish him far
    ///     faster than one touch should - so holding on is strictly worse for him than letting go
    ///     and catching him again.
    /// </summary>
    [Fact]
    public void Level_HeldWithoutLettingGo_DrainsFurtherThanTwoSeparateTouches()
    {
        // Arrange
        EnergyDrain held = Touched(6);

        EnergyDrain caughtTwice = Touched(3);
        caughtTwice.Step(false);
        for (int frame = 0; frame < 3; frame++)
        {
            caughtTwice.Step(true);
        }

        // Assert
        Assert.True(held.Level < caughtTwice.Level,
            $"held to {held.Level}, caught twice to {caughtTwice.Level}");
    }

    /// <summary>
    ///     The frame counter is for the flicker on the wash, and it runs whether or not anything is
    ///     draining him - it is a clock, not a measure of how hurt he is.
    /// </summary>
    [Fact]
    public void Frame_WhateverIsHappening_CountsEveryStep()
    {
        // Arrange
        var drain = new EnergyDrain();

        // Act
        drain.Step(true);
        drain.Step(false);
        drain.Step(true);

        // Assert
        Assert.Equal(3, drain.Frame);
    }

    [Fact]
    public void Reset_AfterANastyHasHadHim_IsUnhurtAgain()
    {
        // Arrange
        EnergyDrain drain = Touched(1000);

        // Act
        drain.Reset();

        // Assert
        Assert.Equal(ColourLevels.Full, drain.Level);
        Assert.False(drain.Draining);
    }

    /// <summary>
    ///     And it forgets that a nasty was holding him, so the first frame after a new game is the
    ///     first frame of a touch rather than the middle of the old one.
    /// </summary>
    [Fact]
    public void Reset_WhileANastyStillHasHim_StartsTheNextDrainFromUnhurt()
    {
        // Arrange
        EnergyDrain drain = Touched(8);

        // Act
        drain.Reset();
        drain.Step(true);

        // Assert
        Assert.Equal(ColourLevels.Full - Step, drain.Level);
    }
}
