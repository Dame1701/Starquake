// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The map the blob and the nasties are stepped against. This is the thing most likely to break
///     when level data changes, and until <see cref="ScreenCollisionMap" /> came out of
///     <c>GameScreenCollection</c> none of it could be reached without loading the game's content.
/// </summary>
/// <remarks>
///     The screens here are built by hand rather than read from <c>SQScreenData.dat</c>, so a test
///     says what it needs - one solid tile, one spike, one tunnel mouth - instead of hunting the
///     shipped map for a screen that happens to have one.
/// </remarks>
public class ScreenCollisionMapTests
{
    [Fact]
    public void Rebuild_AnEmptyScreen_HasNothingToWalkInto()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Empty(screen.CollisionMap);
    }

    [Fact]
    public void Rebuild_ASolidTile_FillsEveryCellOfIt()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Equal(
            TileMetrics.CollisionCellsAcrossTile * TileMetrics.CollisionCellsDownTile,
            screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell => Assert.Equal(CollisionTileType.EveryoneHits, cell.CollisionTileType));
    }

    [Fact]
    public void Rebuild_ASolidTileAwayFromTheOrigin_PutsItsCellsAtThatTilesOffset()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[3, 2] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.All(screen.CollisionMap, cell =>
        {
            Assert.InRange(cell.XPos, 3 * TileMetrics.TileWidth, 4 * TileMetrics.TileWidth - 1);
            Assert.InRange(cell.YPos, 2 * TileMetrics.TileHeight, 3 * TileMetrics.TileHeight - 1);
        });
    }

    [Fact]
    public void Rebuild_ATileSolidOnlyAlongItsTop_AddsThatRowAndNoMore()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.PlatformTile);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Equal(TileMetrics.CollisionCellsAcrossTile, screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell => Assert.Equal(0, cell.YPos));
    }

    [Fact]
    public void Rebuild_ASpike_IsSomethingTheBlobWalksThroughAndANastyDoesNot()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.Spike2);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert - the four middle cells, none of which stops the blob
        Assert.Equal(4, screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell => Assert.Equal(CollisionTileType.NotBlob, cell.CollisionTileType));
    }

    [Fact]
    public void Rebuild_AWallSpike_RunsDownTheRightOfItsTileRatherThanAcrossTheMiddle()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.Spike1);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Equal(4, screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell =>
            Assert.True(cell.XPos >= TileMetrics.CollisionCellWidth * 2));
    }

    [Fact]
    public void Rebuild_ATeleportBooth_IsSolidToNastiesRightAcrossTheTile()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.TeleportBooth);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Equal(
            TileMetrics.CollisionCellsAcrossTile * TileMetrics.CollisionCellsDownTile,
            screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell => Assert.Equal(CollisionTileType.NotBlob, cell.CollisionTileType));
    }

    [Fact]
    public void Rebuild_AnItemLyingOnTheScreen_IsFourCellsTheBlobAloneCollidesWith()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.PowerUp = new InteractiveItem(100, 200, 0);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Equal(4, screen.CollisionMap.Count);
        Assert.All(screen.CollisionMap, cell => Assert.Equal(CollisionTileType.NotBlob, cell.CollisionTileType));
        Assert.Contains(screen.CollisionMap, cell => cell is { XPos: 100, YPos: 200 });
    }

    [Fact]
    public void Rebuild_Twice_DoesNotLeaveTheFirstMapBehind()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);

        // Act
        map.Rebuild(0);
        int afterFirst = screen.CollisionMap.Count;
        map.Rebuild(0);

        // Assert
        Assert.Equal(afterFirst, screen.CollisionMap.Count);
    }

    [Fact]
    public void Rebuild_ASecondScreen_BecomesTheMapOfThatOne()
    {
        // Arrange
        GameScreenData empty = TestData.EmptyScreen();
        GameScreenData solid = TestData.EmptyScreen();
        solid.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);

        var map = new ScreenCollisionMap([empty, solid], TestData.TileCollisionData());

        // Act
        map.Rebuild(1);

        // Assert
        Assert.Equal(1, map.ScreenNumber);
        Assert.NotEmpty(solid.CollisionMap);
        Assert.Empty(empty.CollisionMap);
    }

    [Fact]
    public void CheckCollision_NothingInTheWay_AllowsTheFullVelocity()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        int allowed = map.CheckCollision(
            Direction.Right, 0, 0, TestData.MoverSize, TestData.MoverSize, TestData.Velocity, true);

        // Assert
        Assert.Equal(TestData.Velocity, allowed);
    }

    [Fact]
    public void CheckCollision_AWallAhead_StopsTheMoverShortOfIt()
    {
        // Arrange - a solid tile in column 1, with the mover sitting in column 0
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[1, 0] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        int justShortOfTheWall = TileMetrics.TileWidth - TestData.MoverSize;

        // Act
        int allowed = map.CheckCollision(
            Direction.Right, justShortOfTheWall - 4, 0,
            TestData.MoverSize, TestData.MoverSize, TestData.Velocity, true);

        // Assert
        Assert.True(allowed < TestData.Velocity);
    }

    [Fact]
    public void CheckCollision_ANastyOnASpike_IsStoppedWhereTheBlobWouldNotBe()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[1, 0] = new ScreenTile((int)ActionTiles.Spike2);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        int approach = TileMetrics.TileWidth + TileMetrics.CollisionCellWidth - TestData.MoverSize - 1;

        // Act
        int blobAllowed = map.CheckCollision(
            Direction.Right, approach, TileMetrics.CollisionCellHeight,
            TestData.MoverSize, TestData.MoverSize, TestData.Velocity, true);
        int nastyAllowed = map.CheckCollision(
            Direction.Right, approach, TileMetrics.CollisionCellHeight,
            TestData.MoverSize, TestData.MoverSize, TestData.Velocity, false);

        // Assert
        Assert.Equal(TestData.Velocity, blobAllowed);
        Assert.True(nastyAllowed < TestData.Velocity);
    }

    /// <summary>
    ///     A nasty is held inside the screen and the blob is not, because walking off an edge is how
    ///     the blob changes screens.
    /// </summary>
    [Fact]
    public void CheckCollision_AtTheLeftEdge_ClampsANastyAndLetsTheBlobLeave()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        int blobAllowed = map.CheckCollision(
            Direction.Left, 2, 0, TestData.MoverSize, TestData.MoverSize, TestData.Velocity, true);
        int nastyAllowed = map.CheckCollision(
            Direction.Left, 2, 0, TestData.MoverSize, TestData.MoverSize, TestData.Velocity, false);

        // Assert
        Assert.Equal(TestData.Velocity, blobAllowed);
        Assert.Equal(2, nastyAllowed);
    }

    [Fact]
    public void CheckRectangles_OverlappingASolidTile_ReportsThatTilesImage()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        int hit = map.CheckRectangles(0, 0, TestData.MoverSize, TestData.MoverSize, true);

        // Assert
        Assert.Equal(TestData.SolidTile, hit);
    }

    [Fact]
    public void CheckRectangles_OverlappingNothing_ReportsMinusOne()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        int hit = map.CheckRectangles(0, 0, TestData.MoverSize, TestData.MoverSize, true);

        // Assert
        Assert.Equal(-1, hit);
    }

    [Fact]
    public void CheckRectangles_TheBlobOverANastyOnlyCell_PassesStraightThrough()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.TeleportBooth);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        int blobHit = map.CheckRectangles(0, 0, TestData.MoverSize, TestData.MoverSize, true);
        int nastyHit = map.CheckRectangles(0, 0, TestData.MoverSize, TestData.MoverSize, false);

        // Assert
        Assert.Equal(-1, blobHit);
        Assert.NotEqual(-1, nastyHit);
    }

    [Fact]
    public void CheckTileByObject_ATileUnderfoot_ReportsItAndWhereTheFootSitsInIt()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 1] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        int standingOnTheTileAbove = TileMetrics.TileHeight - TestData.MoverSize;

        // Act
        TileDetected? found = map.CheckTileByObject(
            Direction.Down, 0, standingOnTheTileAbove, TestData.MoverSize, TestData.MoverSize, TestData.Velocity);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(TestData.SolidTile, found.TileNumber);
        Assert.Equal(0, found.MapXPosition);
        Assert.Equal(1, found.MapYPosition);
    }

    [Fact]
    public void CheckTileByObject_NothingThere_ReportsNothing()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        TileDetected? found = map.CheckTileByObject(
            Direction.Down, 0, 0, TestData.MoverSize, TestData.MoverSize, TestData.Velocity);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void CheckTileByObject_ADiagonal_IsNotADirectionThatProbes()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);

        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        TileDetected? found = map.CheckTileByObject(
            Direction.UpLeft, 0, 0, TestData.MoverSize, TestData.MoverSize, TestData.Velocity);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void CanStandOnTopOf_TellsAPlatformFromEmptyAir()
    {
        // Arrange
        ScreenCollisionMap map = MapOf(TestData.EmptyScreen());

        // Act and assert
        Assert.True(map.CanStandOnTopOf(TestData.PlatformTile));
        Assert.True(map.CanStandOnTopOf(TestData.SolidTile));
        Assert.False(map.CanStandOnTopOf(TestData.HollowTile));
    }

    [Fact]
    public void Add_APlatformTheBlobHasJustLaid_JoinsTheMapWithoutARebuild()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        ScreenCollisionMap map = MapOf(screen);
        map.Rebuild(0);

        // Act
        map.Add(new CollisionTile(
            (int)CollisionObjectType.JumpPlatform, 64, 64, CollisionTileType.EveryoneHits));

        // Assert
        Assert.Single(screen.CollisionMap);
    }

    /// <summary>
    ///     A marker is a position the author noted rather than a tile, so there is no mask to read for
    ///     one - and its number is past the end of the mask data, which is why reading one anyway would
    ///     not merely add a stray cell but throw.
    /// </summary>
    [Fact]
    public void Rebuild_AMarker_IsNeitherSolidNorReadFromTheMaskData()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)MarkerTile.BlobStartPosition);

        // The shipped file covers the tile sheet and no more, so the marker is past its end here too.
        var map = new ScreenCollisionMap([screen], TestData.SheetSizedCollisionData());

        // Act
        map.Rebuild(0);

        // Assert
        Assert.Empty(screen.CollisionMap);
    }

    private static ScreenCollisionMap MapOf(GameScreenData screen)
        => new([screen], TestData.TileCollisionData());

    private static class TestData
    {
        /// <summary>A tile image solid in every cell.</summary>
        public const int SolidTile = 1;

        /// <summary>A tile image solid along its top row only - what the blob walks on.</summary>
        public const int PlatformTile = 2;

        /// <summary>A tile image with no solid cell at all - drawn, but walked through.</summary>
        public const int HollowTile = 3;

        public const int MoverSize = 32;

        public const int Velocity = 8;

        /// <summary>
        ///     A screen of nothing. The map builds into the screen record rather than owning a list
        ///     of its own, so a test reads its work back off the screen it handed over.
        /// </summary>
        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        /// <summary>
        ///     Which cells of each tile image are solid. Indexed image, then cell across, then cell
        ///     down, exactly as the shipped data is.
        /// </summary>
        public static int[,,] TileCollisionData()
        {
            var data = new int[
                TestTileImageCount,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

            for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
            {
                for (int down = 0; down < TileMetrics.CollisionCellsDownTile; down++)
                {
                    data[SolidTile, across, down] = 1;
                }

                data[PlatformTile, across, 0] = 1;
            }

            return data;
        }

        /// <summary>
        ///     Collision data the size the shipped file is: one mask per tile on the sheet, and nothing
        ///     beyond it. <see cref="TileCollisionData" /> is deliberately roomier than that, so only a
        ///     test that cares about the end of the array asks for this.
        /// </summary>
        public static int[,,] SheetSizedCollisionData()
            => new int[
                TunnelTiles.LastMouth + 1,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

        /// <summary>
        ///     Big enough to index every action tile the tests place, since the map reads the collision
        ///     data by tile image and the spikes and booths are numbered in the four hundreds.
        /// </summary>
        private const int TestTileImageCount = 512;
    }
}
