// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The parts of the map the shipped planet cannot exercise, held against
///     <see cref="ReferenceCollisionMap" /> on screens built by hand.
/// </summary>
/// <remarks>
///     <see cref="CollisionMapDifferentialTests" /> sweeps the real map, but a screen as it is
///     stored carries no pick-ups, no power-up and no pyramid - those are placed by
///     <c>WorldPopulator</c> when a game starts - and nothing there has laid a platform or opened a
///     door. Those are the cells that are not on the grid and so are the ones the rework left in a
///     list, which makes them exactly the ones worth measuring.
/// </remarks>
public class CollisionMapParityTests
{
    private const int MoverSize = 64;

    private const int Velocity = 8;

    private static readonly Direction[] Directions =
        [Direction.Up, Direction.Down, Direction.Left, Direction.Right];

    [Fact]
    public void ItemsLyingOnAScreen_StopANastyAndNotTheBlob_ExactlyAsTheListDid()
    {
        // Arrange
        GameScreenData screen = TestData.ScreenWithAFloor();
        screen.PowerUp = new InteractiveItem(200, 100, 0);
        screen.PickUps =
        [
            new InteractiveItem(416, 288, 0),
            new InteractiveItem(73, 311, 0)
        ];
        screen.CheopsPyramid = new InteractiveItem(640, 192, 0);

        // Act / Assert
        AssertAgreesEverywhere(screen, []);
    }

    [Fact]
    public void CellsFromADynamicSource_AreAskedForAndAnsweredTheSameWay()
    {
        // Arrange - a platform, a core element and a door's force field, none of them grid-aligned
        CollisionTile[] cells =
        [
            CollisionTile.ForObject(CollisionObjectType.JumpPlatform, 148, 203, CollisionTileType.EveryoneHits),
            CollisionTile.ForObject(CollisionObjectType.CoreElement, 311, 97, CollisionTileType.NotBlob),
            CollisionTile.ForObject(CollisionObjectType.KeyAccessDoor, 528, 416, CollisionTileType.None)
        ];

        // Act / Assert
        AssertAgreesEverywhere(TestData.ScreenWithAFloor(), cells);
    }

    /// <summary>
    ///     A key-locked door flips its cells between stopping everyone and stopping only the
    ///     nasties while Blob carries the key, writing to the cell it already handed over rather
    ///     than giving the map a new one. The map has to be reading the same object for that to
    ///     land.
    /// </summary>
    [Fact]
    public void ADoorCellFlippedInPlace_ChangesWhatTheMapAnswersWithoutARebuild()
    {
        // Arrange - a cell directly in Blob's way
        GameScreenData screen = TestData.EmptyScreen();
        CollisionTile forceField = CollisionTile.ForObject(
            CollisionObjectType.KeyAccessDoor, 64, 0, CollisionTileType.EveryoneHits);

        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);
        map.Add(forceField);

        int blocked = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        // Act
        forceField.CollisionTileType = CollisionTileType.NotBlob;
        int afterTheKey = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        // Assert
        Assert.Equal(0, blocked);
        Assert.Equal(Velocity, afterTheKey);
    }

    /// <summary>
    ///     One cell position carrying both kinds at once. A teleport booth lays a nasty-only cell
    ///     over every cell of its tile, and the tile's own mask may already have made part of it
    ///     solid, so the two sit on top of one another. A map holding one kind per cell would lose
    ///     whichever it wrote second.
    /// </summary>
    [Fact]
    public void ACellThatIsBothSolidAndNastyOnly_KeepsBothAnswers()
    {
        // Arrange - a booth tile whose own mask is solid everywhere
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.TeleportBooth);

        int[,,] masks = TestData.TileCollisionData();
        for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
        {
            for (int down = 0; down < TileMetrics.CollisionCellsDownTile; down++)
            {
                masks[(int)ActionTiles.TeleportBooth, across, down] = 1;
            }
        }

        var map = new ScreenCollisionMap([screen], masks);
        var reference = new ReferenceCollisionMap([screen], masks);
        map.Rebuild(0);
        reference.Rebuild(0);

        // Assert - solid to Blob despite the booth cells, and still nasty-only underneath
        Assert.True(map.HasNastyOnlyCellAt(0, 0));
        Assert.Equal(reference.HasNastyOnlyCellAt(0, 0), map.HasNastyOnlyCellAt(0, 0));
        Assert.Equal(
            reference.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true),
            map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true));
    }

    /// <summary>
    ///     The trap the baked layer was most likely to fall into. Tiles do change while the game is
    ///     running - a breakable bridge sets its tile to -1 and later puts it back, and a shuttle
    ///     pad swaps between two numbers - so a bake kept from the last rebuild has to notice. It
    ///     decides by comparing the tiles rather than by being told, which is why this test writes
    ///     the tile directly, exactly as <c>Blob</c> and <c>GameScreenCollection</c> do.
    /// </summary>
    [Fact]
    public void ATileChangedUnderTheMap_IsNoticedByTheNextRebuild()
    {
        // Arrange - a floor Blob is standing on
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 1] = new ScreenTile(TestData.SolidTile);

        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        int standingOnIt = map.CheckCollision(
            Direction.Down, 0, TileMetrics.TileHeight - MoverSize, MoverSize, MoverSize, Velocity, true);

        // Act - the bridge breaks, which is a direct write to the tile and then a rebuild
        screen.ScreenTiles[0, 1].TileImage = -1;
        map.Rebuild(0);

        int afterItBroke = map.CheckCollision(
            Direction.Down, 0, TileMetrics.TileHeight - MoverSize, MoverSize, MoverSize, Velocity, true);

        // And back again, which is what the reset does
        screen.ScreenTiles[0, 1].TileImage = TestData.SolidTile;
        map.Rebuild(0);

        int afterItCameBack = map.CheckCollision(
            Direction.Down, 0, TileMetrics.TileHeight - MoverSize, MoverSize, MoverSize, Velocity, true);

        // Assert
        Assert.Equal(0, standingOnIt);
        Assert.Equal(Velocity, afterItBroke);
        Assert.Equal(0, afterItCameBack);
    }

    /// <summary>
    ///     A rebuild of a different screen and back again must not hand back the first screen's
    ///     bake, which is the other way a kept bake goes wrong.
    /// </summary>
    [Fact]
    public void RebuildingAnotherScreenAndReturning_DoesNotReuseTheWrongBake()
    {
        // Arrange
        GameScreenData solid = TestData.EmptyScreen();
        solid.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);
        GameScreenData empty = TestData.EmptyScreen();

        var map = new ScreenCollisionMap([solid, empty], TestData.TileCollisionData());

        // Act
        map.Rebuild(0);
        int onTheSolidScreen = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        map.Rebuild(1);
        int onTheEmptyScreen = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        map.Rebuild(0);
        int backOnTheSolidScreen = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        // Assert
        Assert.Equal(0, onTheSolidScreen);
        Assert.Equal(Velocity, onTheEmptyScreen);
        Assert.Equal(0, backOnTheSolidScreen);
    }

    /// <summary>
    ///     Two screens whose tiles happen to be identical. The bake is kept per screen, so the
    ///     second must still be built rather than the first's being handed over - which it would
    ///     be if staleness were decided by the tiles alone and not by the screen number too.
    /// </summary>
    [Fact]
    public void TwoScreensWithTheSameTiles_AreStillTheirOwnMaps()
    {
        // Arrange
        GameScreenData first = TestData.EmptyScreen();
        GameScreenData second = TestData.EmptyScreen();

        var map = new ScreenCollisionMap([first, second], TestData.TileCollisionData());
        map.RegisterDynamicCells(new CellsOn(1,
            CollisionTile.ForObject(CollisionObjectType.JumpPlatform, 64, 0, CollisionTileType.EveryoneHits)));

        // Act
        map.Rebuild(0);
        int withoutThePlatform = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        map.Rebuild(1);
        int withThePlatform = map.CheckCollision(Direction.Right, 0, 0, MoverSize, MoverSize, Velocity, true);

        // Assert
        Assert.Equal(Velocity, withoutThePlatform);
        Assert.Equal(0, withThePlatform);
    }

    /// <summary>
    ///     Builds both maps over one screen and compares every answer at every position, in every
    ///     direction, for Blob and for a nasty.
    /// </summary>
    private static void AssertAgreesEverywhere(GameScreenData screen, CollisionTile[] dynamicCells)
    {
        int[,,] masks = TestData.TileCollisionData();

        var map = new ScreenCollisionMap([screen], masks);
        var reference = new ReferenceCollisionMap([screen], masks);

        map.RegisterDynamicCells(new CellsOn(0, dynamicCells));
        reference.RegisterDynamicCells(new ReferenceCellsOn(0, dynamicCells));

        map.Rebuild(0);
        reference.Rebuild(0);

        List<string> disagreements = [];

        for (int y = -20; y < TileMetrics.PlayAreaHeight; y += 13)
        {
            for (int x = -20; x < TileMetrics.PlayAreaWidth; x += 13)
            {
                foreach (bool isBlob in (bool[])[true, false])
                {
                    Compare(disagreements, "CheckRectangles", x, y, isBlob,
                        map.CheckRectangles(x, y, MoverSize, MoverSize, isBlob) >= 0 ? 1 : 0,
                        reference.CheckRectangles(x, y, MoverSize, MoverSize, isBlob) >= 0 ? 1 : 0);

                    foreach (Direction direction in Directions)
                    {
                        Compare(disagreements, $"CheckCollision {direction}", x, y, isBlob,
                            map.CheckCollision(direction, x, y, MoverSize, MoverSize, Velocity, isBlob),
                            reference.CheckCollision(direction, x, y, MoverSize, MoverSize, Velocity, isBlob));
                    }
                }
            }
        }

        for (int cellY = 0; cellY < TileMetrics.PlayAreaHeight; cellY += TileMetrics.CollisionCellHeight)
        {
            for (int cellX = 0; cellX < TileMetrics.PlayAreaWidth; cellX += TileMetrics.CollisionCellWidth)
            {
                Compare(disagreements, "HasNastyOnlyCellAt", cellX, cellY, false,
                    map.HasNastyOnlyCellAt(cellX, cellY) ? 1 : 0,
                    reference.HasNastyOnlyCellAt(cellX, cellY) ? 1 : 0);
            }
        }

        Assert.Empty(disagreements);
    }

    private static void Compare(
        List<string> disagreements, string what, int x, int y, bool isBlob, int fromMap, int fromReference)
    {
        if (fromMap != fromReference && disagreements.Count < 20)
        {
            disagreements.Add($"{what} at ({x},{y}) isBlob={isBlob}: map {fromMap}, reference {fromReference}");
        }
    }

    private sealed class CellsOn(int screenNumber, params CollisionTile[] cells) : IDynamicCollisionCells
    {
        public IEnumerable<CollisionTile> CellsOnScreen(int screen)
            => screen == screenNumber ? cells : [];
    }

    private sealed class ReferenceCellsOn(int screenNumber, params CollisionTile[] cells) : IReferenceDynamicCells
    {
        public IEnumerable<CollisionTile> CellsOnScreen(int screen)
            => screen == screenNumber ? cells : [];
    }

    private static class TestData
    {
        public const int SolidTile = 1;

        private const int PlatformTile = 2;

        private const int TestTileImageCount = 512;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        /// <summary>
        ///     Something for the sweeps to run into: a floor along the bottom, a wall, a spike and a
        ///     booth, so both the solid and the nasty-only rules are in play.
        /// </summary>
        public static GameScreenData ScreenWithAFloor()
        {
            GameScreenData screen = EmptyScreen();

            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                screen.ScreenTiles[x, TileMetrics.TilesDown - 1] = new ScreenTile(PlatformTile);
            }

            screen.ScreenTiles[0, 2] = new ScreenTile(SolidTile);
            screen.ScreenTiles[3, 1] = new ScreenTile((int)ActionTiles.Spike2);
            screen.ScreenTiles[5, 3] = new ScreenTile((int)ActionTiles.TeleportBooth);
            screen.ScreenTiles[6, 2] = new ScreenTile(TunnelTiles.LeftMouth1);

            return screen;
        }

        public static int[,,] TileCollisionData()
        {
            int[,,] data = new int[
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
    }
}
