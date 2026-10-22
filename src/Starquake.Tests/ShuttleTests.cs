// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.ActiveSprites;
using Starquake.Core;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Taking the shuttle and stepping off it.
/// </summary>
/// <remarks>
///     Nothing here needs a screen, a sound or a graphics device. The world keeps its own record of
///     which pads have been taken, and the collision map follows from that, so the one thing these
///     tests cannot see is the caller acting on what <see cref="Shuttle.Step" /> returns.
/// </remarks>
public class ShuttleTests
{
    /// <summary>
    ///     Squarely on the pad. 32 pixels in, which is a 64-wide Blob centred on a 128-wide tile,
    ///     with his feet exactly on top of it.
    /// </summary>
    private const int SquarelyOn = 32;

    private static TileDetected Pad(
        ActionTiles tile = ActionTiles.ShuttlePlatform,
        int xPosition = SquarelyOn,
        int yPosition = 0)
        => new((int)tile, xPosition, yPosition, 0, 0);

    [Fact]
    public void BlobHeight_OffTheShuttle_IsTheSpriteAlone()
    {
        // Arrange
        var shuttle = new Shuttle();

        // Assert
        Assert.False(shuttle.Riding);
        Assert.Equal(64, shuttle.BlobHeight);
    }

    /// <summary>
    ///     The height is derived, not stored, so no sequence of calls can leave it disagreeing with
    ///     <see cref="Shuttle.Riding" />. That is why this class exists: a flag and a height written
    ///     separately, one without the other, leaves Blob standing half inside the floor.
    /// </summary>
    [Fact]
    public void BlobHeight_Riding_IncludesTheShuttleUnderneath()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());

        // Act
        shuttle.Step(Pad(), true, false, false);

        // Assert
        Assert.True(shuttle.Riding);
        Assert.Equal(96, shuttle.BlobHeight);
    }

    [Theory]
    [InlineData(ActionTiles.ShuttlePlatform)]
    [InlineData(ActionTiles.RemovedShuttlePlatform)]
    public void NoticeThePadUnderfoot_StandingSquarelyOnEitherState_IsOnThePad(ActionTiles tile)
    {
        // Arrange
        var shuttle = new Shuttle();

        // Act
        shuttle.NoticeThePadUnderfoot(Pad(tile));

        // Assert
        Assert.True(shuttle.OnThePad);
    }

    /// <summary>
    ///     The gap a taken shuttle leaves counts as much as the pad itself, because standing on it
    ///     is how Blob puts the shuttle back.
    /// </summary>
    [Fact]
    public void NoticeThePadUnderfoot_NothingUnderfoot_IsNotOnThePad()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());

        // Act
        shuttle.NoticeThePadUnderfoot(null);

        // Assert
        Assert.False(shuttle.OnThePad);
    }

    [Fact]
    public void NoticeThePadUnderfoot_SomeOtherTile_IsNotOnThePad()
    {
        // Arrange
        var shuttle = new Shuttle();

        // Act
        shuttle.NoticeThePadUnderfoot(Pad(ActionTiles.BreakableBridges));

        // Assert
        Assert.False(shuttle.OnThePad);
    }

    /// <summary>
    ///     He has to be centred on the tile and standing on top of it. Anywhere else on the same tile
    ///     and the pad is not his to take - he is walking past it, or standing below it.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(64, 0)]
    [InlineData(SquarelyOn, 32)]
    [InlineData(SquarelyOn, -32)]
    public void NoticeThePadUnderfoot_NotSquarelyOnIt_IsNotOnThePad(int xPosition, int yPosition)
    {
        // Arrange
        var shuttle = new Shuttle();

        // Act
        shuttle.NoticeThePadUnderfoot(Pad(xPosition: xPosition, yPosition: yPosition));

        // Assert
        Assert.False(shuttle.OnThePad);
    }

    [Fact]
    public void Step_PressingUpOnAnIntactPad_Boards()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());

        // Act
        ShuttleBoarding boarding = shuttle.Step(Pad(), true, false, false);

        // Assert
        Assert.Equal(ShuttleBoarding.Boarded, boarding);
        Assert.True(shuttle.Riding);
    }

    /// <summary>
    ///     Standing on a pad without pressing up is still reported, rather than being folded in with
    ///     not being on one: the caller rebuilds the collision map on it, which is what the original
    ///     did and what keeps a pad taken on another screen from being walked through here.
    /// </summary>
    [Fact]
    public void Step_StandingOnAPadDoingNothing_IsReportedSeparatelyFromBeingOffIt()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());

        // Act
        ShuttleBoarding boarding = shuttle.Step(Pad(), false, false, false);

        // Assert
        Assert.Equal(ShuttleBoarding.Standing, boarding);
        Assert.False(shuttle.Riding);
    }

    /// <summary>
    ///     The pad is acted on from the previous frame's reading, so Blob does nothing when the flag
    ///     says he is not on one, even when handed a pad tile.
    /// </summary>
    [Fact]
    public void Step_NotNoticedOnAPad_DoesNothingEvenWithAPadUnderfoot()
    {
        // Arrange
        var shuttle = new Shuttle();

        // Act
        ShuttleBoarding boarding = shuttle.Step(Pad(), true, false, false);

        // Assert
        Assert.Equal(ShuttleBoarding.NotOnThePad, boarding);
        Assert.False(shuttle.Riding);
    }

    [Fact]
    public void Step_OnAPadWithNothingUnderfootThisFrame_DoesNothing()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());

        // Act
        ShuttleBoarding boarding = shuttle.Step(null, true, false, false);

        // Assert
        Assert.Equal(ShuttleBoarding.NotOnThePad, boarding);
        Assert.False(shuttle.Riding);
    }

    /// <summary>
    ///     Pressing up where the shuttle has already been taken does nothing - there is nothing left
    ///     on the pad to board.
    /// </summary>
    [Fact]
    public void Step_PressingUpOnAnEmptyPad_DoesNotBoard()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad(ActionTiles.RemovedShuttlePlatform));

        // Act
        ShuttleBoarding boarding = shuttle.Step(Pad(ActionTiles.RemovedShuttlePlatform), true, false, false);

        // Assert
        Assert.Equal(ShuttleBoarding.Standing, boarding);
        Assert.False(shuttle.Riding);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Step_SteeringOffAnEmptyPad_StepsOff(bool movedLeft, bool movedRight)
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());
        shuttle.Step(Pad(), true, false, false);
        shuttle.NoticeThePadUnderfoot(Pad(ActionTiles.RemovedShuttlePlatform));

        // Act
        ShuttleBoarding boarding = shuttle.Step(
            Pad(ActionTiles.RemovedShuttlePlatform),
            false,
            movedLeft,
            movedRight);

        // Assert
        Assert.Equal(ShuttleBoarding.SteppedOff, boarding);
        Assert.False(shuttle.Riding);
        Assert.Equal(64, shuttle.BlobHeight);
    }

    /// <summary>
    ///     He only gets off over the gap the shuttle came from. Steering anywhere else leaves him on
    ///     it, which is what makes the shuttle worth taking.
    /// </summary>
    [Fact]
    public void Step_SteeringOverAnIntactPad_KeepsHimRiding()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.NoticeThePadUnderfoot(Pad());
        shuttle.Step(Pad(), true, false, false);

        // Act
        ShuttleBoarding boarding = shuttle.Step(Pad(), false, true, false);

        // Assert
        Assert.Equal(ShuttleBoarding.Standing, boarding);
        Assert.True(shuttle.Riding);
    }

    /// <summary>
    ///     A death by spikes puts him back as he arrived, shuttle and all, and the height has to come
    ///     back with it.
    /// </summary>
    [Fact]
    public void PutBack_Riding_RestoresTheHeightWithIt()
    {
        // Arrange
        var shuttle = new Shuttle();

        // Act
        shuttle.PutBack(true);

        // Assert
        Assert.True(shuttle.Riding);
        Assert.Equal(96, shuttle.BlobHeight);
    }

    [Fact]
    public void Reset_Riding_LeavesHimOffItAtHisOwnHeight()
    {
        // Arrange
        var shuttle = new Shuttle();
        shuttle.PutBack(true);

        // Act
        shuttle.Reset();

        // Assert
        Assert.False(shuttle.Riding);
        Assert.Equal(64, shuttle.BlobHeight);
    }

    /// <summary>
    ///     The shuttle's animation runs off the walk rather than the clock, so it is still only on its
    ///     first frame until he is steered somewhere.
    /// </summary>
    [Fact]
    public void Frame_NeverAdvanced_IsTheFirstFrame()
    {
        // Arrange
        var shuttle = new Shuttle();

        // Assert
        Assert.Equal(0, shuttle.Frame);
    }

    [Fact]
    public void Advance_Repeatedly_CyclesTheShuttleThroughItsFramesAndBack()
    {
        // Arrange
        var shuttle = new Shuttle();
        var seen = new List<int>();

        // Act
        for (int frame = 0; frame < 20; frame++)
        {
            shuttle.Advance();
            seen.Add(shuttle.Frame);
        }

        // Assert
        Assert.Equal([0, 1, 2, 3], seen.Distinct().Order());
        Assert.Contains(0, seen.Skip(seen.LastIndexOf(3)));
    }
}
