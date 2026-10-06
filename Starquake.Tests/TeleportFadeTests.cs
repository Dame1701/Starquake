// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Constants;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Blob dissolving into a teleport beam and coming back out of one. Two mutable fields inside
///     <c>Blob</c>, one of them cleared by a particle effect's completion callback - which is why
///     nothing could reach either.
/// </summary>
public class TeleportFadeTests
{
    private const int Step = 8;

    [Fact]
    public void Level_BeforeHeHasEverTeleported_IsFullySolid()
        => Assert.Equal(ColourLevels.Full, new TeleportFade().Level);

    [Fact]
    public void BeamPlaying_BeforeHeHasEverTeleported_IsNot()
        => Assert.False(new TeleportFade().BeamPlaying);

    [Fact]
    public void Begin_SteppingIntoABooth_TakesHimDownToNothingAndStartsTheBeam()
    {
        // Arrange
        var teleport = new TeleportFade();

        // Act
        teleport.Begin();

        // Assert
        Assert.Equal(ColourLevels.None, teleport.Level);
        Assert.True(teleport.BeamPlaying);
    }

    [Fact]
    public void Step_AfterArriving_ClimbsBackAStepAtATime()
    {
        // Arrange
        var teleport = new TeleportFade();
        teleport.Begin();

        // Act
        teleport.Step();

        // Assert
        Assert.Equal(Step, teleport.Level);
    }

    [Fact]
    public void Step_LongEnough_IsSolidAgainAndStopsThere()
    {
        // Arrange
        var teleport = new TeleportFade();
        teleport.Begin();

        // Act
        for (int frame = 0; frame < 1000; frame++)
        {
            teleport.Step();
        }

        // Assert
        Assert.Equal(ColourLevels.Full, teleport.Level);
    }

    /// <summary>
    ///     Blob is stepped every frame whether or not he has teleported, so a level already at full
    ///     has to sit still rather than run past it.
    /// </summary>
    [Fact]
    public void Step_WhenHeHasNotTeleported_LeavesHimSolid()
    {
        // Arrange
        var teleport = new TeleportFade();

        // Act
        teleport.Step();

        // Assert
        Assert.Equal(ColourLevels.Full, teleport.Level);
    }

    /// <summary>
    ///     The beam and the fade are two things, not two stages of one. The beam runs for as long as
    ///     the particle effect runs and says when it is done; the fade climbs at a fixed rate. Either
    ///     can finish first, so neither may end the other.
    /// </summary>
    [Fact]
    public void BeamFinished_WhileHeIsStillFadingIn_DoesNotFinishTheFade()
    {
        // Arrange
        var teleport = new TeleportFade();
        teleport.Begin();
        teleport.Step();

        // Act
        teleport.BeamFinished();

        // Assert
        Assert.False(teleport.BeamPlaying);
        Assert.Equal(Step, teleport.Level);
    }

    /// <inheritdoc cref="BeamFinished_WhileHeIsStillFadingIn_DoesNotFinishTheFade" />
    [Fact]
    public void Step_ToSolidWhileTheBeamRuns_DoesNotStopTheBeam()
    {
        // Arrange
        var teleport = new TeleportFade();
        teleport.Begin();

        // Act
        for (int frame = 0; frame < 1000; frame++)
        {
            teleport.Step();
        }

        // Assert
        Assert.Equal(ColourLevels.Full, teleport.Level);
        Assert.True(teleport.BeamPlaying);
    }

    /// <summary>
    ///     Teleporting again from part way through the last one starts over rather than carrying on,
    ///     which is what makes a booth-to-booth hop look the same every time.
    /// </summary>
    [Fact]
    public void Begin_PartWayThroughAFadeIn_StartsOverFromNothing()
    {
        // Arrange
        var teleport = new TeleportFade();
        teleport.Begin();
        teleport.Step();
        teleport.Step();

        // Act
        teleport.Begin();

        // Assert
        Assert.Equal(ColourLevels.None, teleport.Level);
        Assert.True(teleport.BeamPlaying);
    }
}
