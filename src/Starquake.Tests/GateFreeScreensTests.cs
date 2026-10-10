// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.WorldGeneration;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Which screens Blob can reach without going through a gate, which is what decides where the
///     key and the access card may be put.
/// </summary>
/// <remarks>
///     These matter more than most. An item placed behind the gate it opens makes the game
///     unwinnable, and nothing in play would say so until somebody had walked the whole planet
///     looking for a key that was behind a door.
///     <para>
///         <see cref="Find_ADoorSealingTheOnlyWayThrough_StrandsWhatIsBehindIt" /> and its booth
///         twin are the two where getting the answer wrong costs the game.
///     </para>
/// </remarks>
public class GateFreeScreensTests
{

    [Fact]
    public void Find_AnOpenPlanet_ReachesEveryScreenItCanWalkTo()
    {
        // Arrange
        GameScreenData[] world = TestData.OpenWorld();

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert - nothing is in the way, so the whole planet is fair game
        Assert.Equal(WorldMap.ScreenCount, reached.Count);
    }

    [Fact]
    public void Find_TheLandingSiteScreen_IsAlwaysReachable()
    {
        // Arrange
        GameScreenData[] world = TestData.OpenWorld();

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert
        Assert.Contains(LandingSite.Original.ScreenNumber, reached);
    }

    /// <summary>
    ///     The case this exists for. A door across the one corridor out of a screen means everything
    ///     past it needs the key, so the key cannot be there.
    /// </summary>
    [Fact]
    public void Find_ADoorSealingTheOnlyWayThrough_StrandsWhatIsBehindIt()
    {
        // Arrange - a corridor of screens, with a door shutting the one on the far side off
        GameScreenData[] world = TestData.CorridorWorld(9, ActionTiles.AccessForceField);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert - the screens before the door are reachable, the ones after it are not
        Assert.Contains(8, reached);
        Assert.DoesNotContain(10, reached);
        Assert.DoesNotContain(11, reached);
    }

    /// <inheritdoc cref="Find_ADoorSealingTheOnlyWayThrough_StrandsWhatIsBehindIt" />
    [Fact]
    public void Find_AnAccessBoothSealingTheOnlyWayThrough_StrandsWhatIsBehindIt()
    {
        // Arrange
        GameScreenData[] world = TestData.CorridorWorld(9, ActionTiles.AccessBoothLeft);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.AccessBooth);

        // Assert
        Assert.Contains(8, reached);
        Assert.DoesNotContain(10, reached);
    }

    /// <summary>
    ///     Each gate is only shut for the item that opens it. A door does not stop the access card
    ///     being put behind it, because the card is not what opens a door.
    /// </summary>
    [Fact]
    public void Find_ADoor_DoesNotStandInTheWayOfTheAccessCard()
    {
        // Arrange
        GameScreenData[] world = TestData.CorridorWorld(9, ActionTiles.AccessForceField);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.AccessBooth);

        // Assert
        Assert.Contains(10, reached);
    }

    /// <inheritdoc cref="Find_ADoor_DoesNotStandInTheWayOfTheAccessCard" />
    [Fact]
    public void Find_AnAccessBooth_DoesNotStandInTheWayOfTheKey()
    {
        // Arrange
        GameScreenData[] world = TestData.CorridorWorld(9, ActionTiles.AccessBoothLeft);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert
        Assert.Contains(10, reached);
    }

    /// <summary>
    ///     A door blocks one cell column of its tile rather than the whole tile, which is how the
    ///     game builds it. A screen with a door on it is still reachable itself. Only what lies past
    ///     the door is not.
    /// </summary>
    [Fact]
    public void Find_AScreenWithADoorOnIt_IsItselfStillReachable()
    {
        // Arrange
        GameScreenData[] world = TestData.CorridorWorld(9, ActionTiles.AccessForceField);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert
        Assert.Contains(9, reached);
    }

    /// <summary>
    ///     A marker is not scenery and nothing collides with one, so a wall of them is not a wall.
    /// </summary>
    [Fact]
    public void Find_AScreenFilledWithMarkers_IsWalkedStraightThrough()
    {
        // Arrange
        GameScreenData[] world = TestData.OpenWorld();
        for (int x = 0; x < TileMetrics.TilesAcross; x++)
        {
            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                world[9].ScreenTiles[x, y] = new ScreenTile((int)MarkerTile.Spacer);
            }
        }

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert
        Assert.Contains(9, reached);
        Assert.Contains(10, reached);
    }

    /// <summary>
    ///     Walking off the right of a screen adds one to its number whatever column it was in, so the
    ///     end of a row leads to the start of the next. That is Blob's own rule and not what a grid
    ///     would do, so it is worth pinning.
    /// </summary>
    [Fact]
    public void Find_TheEndOfARowOfScreens_LeadsToTheStartOfTheNext()
    {
        // Arrange - a door on the screen that follows the end of a row, which a grid model would place
        // at the far side of the map rather than next door
        GameScreenData[] world = TestData.CorridorWorld(
            WorldMap.ScreensAcross,
            ActionTiles.AccessForceField);

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, TestData.MapFor(world), Gate.KeyDoor);

        // Assert - screen 16 is the first of row 1 and is reached by walking off the end of row 0
        Assert.Contains(WorldMap.ScreensAcross, reached);
    }

    /// <summary>
    ///     The shipped planet, rather than a world built for a test. This is the one that would notice if
    ///     an edit to the map, or to the model, left nowhere safe to put the two items.
    /// </summary>
    // Internal rather than public: the parameter is one of the game's own types, and those are
    // internal - this project only sees them through InternalsVisibleTo, which is not enough to let a
    // public signature here name one. xunit requires the test class to be public, not its methods.
    [Theory]
    [InlineData(Gate.KeyDoor)]
    [InlineData(Gate.AccessBooth)]
    internal void Find_TheShippedPlanet_LeavesPlentyOfSafeScreens(Gate gate)
    {
        // Arrange
        (GameScreenData[] world, int[,,] masks) = ShippedMap.Load();

        // Act
        HashSet<int> reached = GateFreeScreens.Find(world, new ScreenCollisionMap(world, masks), gate);

        // Assert - a generous pool, so that the placement has a real choice to make. Deliberately a
        // range rather than the exact count: the map is edited, and the point of working this out
        // rather than writing it down is that an edit is allowed to change it.
        Assert.InRange(reached.Count, 50, WorldMap.ScreenCount);
        Assert.Contains(LandingSite.FindIn(world).ScreenNumber, reached);
    }

    private static class TestData
    {
        /// <summary>A tile image solid in every cell - a wall.</summary>
        public const int WallTile = 1;

        /// <summary>A tile image with no solid cell at all, so Blob walks through it.</summary>
        public const int AirTile = 3;

        public static ScreenCollisionMap MapFor(GameScreenData[] world)
            => new(world, CollisionData());

        /// <summary>
        ///     A planet of empty screens. Empty means passable, so Blob can walk anywhere and the
        ///     only thing that can stop it is a gate a test puts in the way.
        /// </summary>
        public static GameScreenData[] OpenWorld()
        {
            var world = new GameScreenData[WorldMap.ScreenCount];

            for (int screen = 0; screen < world.Length; screen++)
            {
                world[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);
            }

            return world;
        }

        /// <summary>
        ///     A planet walled in solid except for one corridor of screens running from the landing site,
        ///     with a gate across the middle of the named screen. Everything past the gate is reachable
        ///     only through it, which is the shape the rule is about.
        /// </summary>
        public static GameScreenData[] CorridorWorld(int gateScreen, ActionTiles gate)
        {
            GameScreenData[] world = OpenWorld();

            // Wall every screen in, so the corridor is the only way about.
            for (int screen = 0; screen < world.Length; screen++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    for (int y = 0; y < TileMetrics.TilesDown; y++)
                    {
                        world[screen].ScreenTiles[x, y] = new ScreenTile(WallTile);
                    }
                }
            }

            // Cut the corridor along one row of tiles, through the screens either side of the gate.
            const int corridorRow = 4;
            for (int screen = LandingSite.Original.ScreenNumber; screen <= gateScreen + 2; screen++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    world[screen].ScreenTiles[x, corridorRow] = new ScreenTile(AirTile);
                    world[screen].ScreenTiles[x, corridorRow + 1] = new ScreenTile(AirTile);
                }
            }

            // And put the gate across it.
            world[gateScreen].ScreenTiles[4, corridorRow] = new ScreenTile((int)gate);
            world[gateScreen].ScreenTiles[4, corridorRow + 1] = new ScreenTile((int)gate);

            return world;
        }

        /// <summary>
        ///     Which cells of each tile image are solid, indexed image then cell across then cell down,
        ///     exactly as the shipped data is.
        /// </summary>
        private static int[,,] CollisionData()
        {
            int[,,] data = new int[
                512,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

            for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
            {
                for (int down = 0; down < TileMetrics.CollisionCellsDownTile; down++)
                {
                    data[WallTile, across, down] = 1;
                }
            }

            return data;
        }
    }
}
