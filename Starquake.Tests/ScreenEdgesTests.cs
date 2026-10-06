// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Rules;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Walking off the edge of a screen. The planet is one flat array stitched together by index, so
///     every one of these is arithmetic on a screen number against the shape of the map - and none of
///     it could be reached before, because it was four methods inside <c>Blob</c>, which needs a
///     resource manager, a graphics device and the planet to construct.
/// </summary>
public class ScreenEdgesTests
{
    private const int BlobWidth = SpriteMetrics.SpriteWidth;
    private const int BlobHeight = SpriteMetrics.SpriteHeight;
    private const int ScreenWidth = TileMetrics.PlayAreaWidth;
    private const int ScreenHeight = TileMetrics.PlayAreaHeight;

    /// <summary>A screen with a column either side of it and a row above and below.</summary>
    private const int MiddleScreen = WorldMap.ScreensAcross + 1;

    [Fact]
    public void OffLeft_StillOnTheScreen_IsNotACrossing()
        => Assert.Null(ScreenEdges.OffLeft(0, 100, BlobWidth, ScreenWidth, MiddleScreen));

    [Fact]
    public void OffLeft_PastTheEdge_ArrivesAgainstTheFarWallOfThePreviousScreen()
    {
        // Act
        ScreenCrossing? crossing = ScreenEdges.OffLeft(-8, 100, BlobWidth, ScreenWidth, MiddleScreen);

        // Assert
        Assert.NotNull(crossing);
        Assert.Equal(MiddleScreen - 1, crossing.Value.ScreenNumber);
        Assert.Equal(ScreenWidth - BlobWidth, crossing.Value.XPos);
        Assert.Equal(100, crossing.Value.YPos);
    }

    [Fact]
    public void OffLeft_OnTheFirstScreenOfTheMap_HasNowhereToGo()
        => Assert.Null(ScreenEdges.OffLeft(-8, 100, BlobWidth, ScreenWidth, 0));

    [Fact]
    public void OffRight_PastTheEdge_ArrivesAtTheNearSideOfTheNextScreen()
    {
        // Act
        ScreenCrossing? crossing =
            ScreenEdges.OffRight(ScreenWidth - BlobWidth + 8, 100, BlobWidth, ScreenWidth, MiddleScreen);

        // Assert
        Assert.NotNull(crossing);
        Assert.Equal(MiddleScreen + 1, crossing.Value.ScreenNumber);
        Assert.Equal(0, crossing.Value.XPos);
        Assert.Equal(100, crossing.Value.YPos);
    }

    [Fact]
    public void OffRight_OnTheLastScreenOfTheMap_HasNowhereToGo()
        => Assert.Null(ScreenEdges.OffRight(
            ScreenWidth - BlobWidth + 8, 100, BlobWidth, ScreenWidth, WorldMap.LastScreenNumber));

    /// <summary>
    ///     The pair of equalities that stop one step across a boundary being taken as two. Blob's four
    ///     edges are tested in turn and without short-circuiting, so the position a sideways crossing
    ///     leaves him in is immediately offered to the opposite edge - and in both directions it lands
    ///     exactly on the one value that edge reads as still being on the screen. A single pixel either
    ///     way and walking left would carry him left for ever.
    /// </summary>
    [Fact]
    public void OffRight_WhereGoingLeftLandsHim_IsNotAFurtherCrossing()
    {
        // Arrange - the arrival the left edge hands back
        ScreenCrossing landed =
            ScreenEdges.OffLeft(-8, 100, BlobWidth, ScreenWidth, MiddleScreen)!.Value;

        // Assert
        Assert.Null(ScreenEdges.OffRight(
            landed.XPos, landed.YPos, BlobWidth, ScreenWidth, landed.ScreenNumber));
    }

    /// <inheritdoc cref="OffRight_WhereGoingLeftLandsHim_IsNotAFurtherCrossing" />
    [Fact]
    public void OffLeft_WhereGoingRightLandsHim_IsNotAFurtherCrossing()
    {
        // Arrange
        ScreenCrossing landed = ScreenEdges
            .OffRight(ScreenWidth - BlobWidth + 8, 100, BlobWidth, ScreenWidth, MiddleScreen)!.Value;

        // Assert
        Assert.Null(ScreenEdges.OffLeft(
            landed.XPos, landed.YPos, BlobWidth, ScreenWidth, landed.ScreenNumber));
    }

    /// <inheritdoc cref="OffRight_WhereGoingLeftLandsHim_IsNotAFurtherCrossing" />
    [Fact]
    public void OffTop_WhereComingThroughTheBottomLandsHim_IsNotAFurtherCrossing()
    {
        // Arrange
        ScreenCrossing landed = ScreenEdges
            .OffBottom(100, ScreenHeight - BlobHeight + 8, BlobHeight, ScreenHeight, MiddleScreen)!.Value;

        // Assert
        Assert.Null(ScreenEdges.OffTop(landed.XPos, landed.YPos, landed.ScreenNumber));
    }

    [Fact]
    public void OffTop_PastTheEdge_IsAWholeRowBackAndLeavesTheArrivalToBlob()
    {
        // Act
        ScreenCrossing? crossing = ScreenEdges.OffTop(100, -8, MiddleScreen);

        // Assert
        Assert.NotNull(crossing);
        Assert.Equal(MiddleScreen - WorldMap.ScreensAcross, crossing.Value.ScreenNumber);
        Assert.Equal(100, crossing.Value.XPos);

        // Unchanged: what he lands on depends on whether a shuttle or a lift is carrying him.
        Assert.Equal(-8, crossing.Value.YPos);
    }

    [Fact]
    public void OffTop_OnTheTopRowOfTheMap_HasNowhereToGo()
        => Assert.Null(ScreenEdges.OffTop(100, -8, WorldMap.ScreensAcross - 1));

    [Fact]
    public void OffBottom_PastTheEdge_IsAWholeRowOnAndArrivesAtTheTop()
    {
        // Act
        ScreenCrossing? crossing = ScreenEdges.OffBottom(
            100, ScreenHeight - BlobHeight + 8, BlobHeight, ScreenHeight, MiddleScreen);

        // Assert
        Assert.NotNull(crossing);
        Assert.Equal(MiddleScreen + WorldMap.ScreensAcross, crossing.Value.ScreenNumber);
        Assert.Equal(100, crossing.Value.XPos);
        Assert.Equal(0, crossing.Value.YPos);
    }

    [Fact]
    public void OffBottom_OnTheBottomRowOfTheMap_HasNowhereToGo()
        => Assert.Null(ScreenEdges.OffBottom(
            100, ScreenHeight - BlobHeight + 8, BlobHeight, ScreenHeight,
            WorldMap.LastScreenNumber - WorldMap.ScreensAcross + 1));

    /// <summary>
    ///     Riding a shuttle makes Blob taller, and his full height is what decides whether his feet
    ///     have left the screen - so the same position is a crossing on a shuttle and is not on foot.
    /// </summary>
    [Fact]
    public void OffBottom_TallerOnAShuttle_LeavesTheScreenFromAPositionThatWouldNotOnFoot()
    {
        // Arrange - feet exactly on the floor on foot, through it by the shuttle's extra height
        const int onTheFloor = ScreenHeight - BlobHeight;
        const int ridingHeight = BlobHeight + 8;

        // Assert
        Assert.Null(ScreenEdges.OffBottom(100, onTheFloor, BlobHeight, ScreenHeight, MiddleScreen));
        Assert.NotNull(ScreenEdges.OffBottom(100, onTheFloor, ridingHeight, ScreenHeight, MiddleScreen));
    }

    /// <summary>
    ///     The diagonal. Walking off the left edge while something carries Blob up through the top is
    ///     two crossings in one frame, and the second is asked about the position the first left him
    ///     in - so he arrives a screen left and a whole row back. This is why the four are asked in
    ///     turn rather than collapsed into one "which edge".
    /// </summary>
    [Fact]
    public void OffLeftThenOffTop_CarriedUpWhileWalkingOff_CrossesBothInOneFrame()
    {
        // Act
        ScreenCrossing sideways =
            ScreenEdges.OffLeft(-8, -8, BlobWidth, ScreenWidth, MiddleScreen)!.Value;
        ScreenCrossing? upwards =
            ScreenEdges.OffTop(sideways.XPos, sideways.YPos, sideways.ScreenNumber);

        // Assert
        Assert.NotNull(upwards);
        Assert.Equal(MiddleScreen - 1 - WorldMap.ScreensAcross, upwards.Value.ScreenNumber);
    }
}
