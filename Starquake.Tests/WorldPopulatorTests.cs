// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.Linq;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.WorldGeneration;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The scatter that makes every game of Starquake a different game on the same map. None of this
///     could be tested before two things happened: the generator stopped building its own
///     <see cref="Random" /> from the clock, so a world became reproducible from a seed, and
///     <see cref="WorldPopulator" /> came out of <c>GameScreenCollection</c>, so reaching it stopped
///     meaning loading the game's content.
/// </summary>
public class WorldPopulatorTests
{
    [Fact]
    public void Populate_TheSameSeedTwice_BuildsTheSameWorld()
    {
        // Arrange
        GameScreenData[] first = TestData.World();
        GameScreenData[] second = TestData.World();

        // Act
        Populate(first, seed: 12345);
        Populate(second, seed: 12345);

        // Assert
        Assert.Equal(TestData.Describe(first), TestData.Describe(second));
    }

    [Fact]
    public void Populate_ADifferentSeed_BuildsADifferentWorld()
    {
        // Arrange
        GameScreenData[] first = TestData.World();
        GameScreenData[] second = TestData.World();

        // Act
        Populate(first, seed: 12345);
        Populate(second, seed: 54321);

        // Assert
        Assert.NotEqual(TestData.Describe(first), TestData.Describe(second));
    }

    [Fact]
    public void Populate_LeavesTheCoreScreenAlone()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.Null(world[WorldMap.CoreScreenNumber].PowerUp);
        Assert.Null(world[WorldMap.CoreScreenNumber].CheopsPyramid);
        Assert.Empty(world[WorldMap.CoreScreenNumber].PickUps);
    }

    [Fact]
    public void Populate_ThinsThePyramidsDownToTheirCeiling()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.Equal(TestData.MaxCheops, world.Count(screen => screen.CheopsPyramid != null));
    }

    [Fact]
    public void Populate_ThinsTheCoreElementsDownToTheirCeiling()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.Equal(TestData.MaxCoreElements, world.Count(screen => screen.PickUps.Count > 0));
    }

    [Fact]
    public void Populate_PutsAPowerUpOnEveryScreenWithRoomForOne()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert - every screen but the core, which is left out of the scatter
        Assert.Equal(world.Length - 1, world.Count(screen => screen.PowerUp != null));
    }

    [Fact]
    public void Populate_DrawsOnlyPowerUpsTheStatusPanelKnowsAbout()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.All(
            world.Where(screen => screen.PowerUp != null),
            screen => Assert.InRange(screen.PowerUp!.Type, 0, TestData.PowerUpTypeCount - 1));
    }

    /// <summary>
    ///     The draw stops one short of <see cref="Collectables.AccessCard" /> (28) and
    ///     <see cref="Collectables.Key" /> (29), which is why neither has ever appeared on the map.
    ///     This pins the behaviour as it is; phase 6 is where the two get placed deliberately.
    /// </summary>
    [Fact]
    public void Populate_NeverScattersTheKeyOrTheAccessCard()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.All(
            world.SelectMany(screen => screen.PickUps),
            pickUp =>
            {
                Assert.NotEqual((int)Collectables.AccessCard, pickUp.Type);
                Assert.NotEqual((int)Collectables.Key, pickUp.Type);
            });
    }

    [Fact]
    public void Populate_GivesEveryPyramidATwoDigitCodeWithNoDigitRepeated()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.All(
            world.Where(screen => screen.CheopsPyramid != null),
            screen =>
            {
                Assert.Equal(2, screen.CheopsAccessCode!.Length);
                Assert.Equal(screen.CheopsAccessCode.Length, screen.CheopsAccessCode.Distinct().Count());
            });
    }

    [Fact]
    public void Populate_GivesAScreenWithABoothAThreeDigitCodeWithNoDigitRepeated()
    {
        // Arrange - one screen carrying a booth
        GameScreenData[] world = TestData.World();
        world[3].ScreenTiles[0, 0] = new ScreenTile((int)ActionTiles.AccessBoothLeft);

        // Act
        Populate(world);

        // Assert
        Assert.NotNull(world[3].AccessCode);
        Assert.Equal(3, world[3].AccessCode!.Length);
        Assert.Equal(world[3].AccessCode!.Length, world[3].AccessCode!.Distinct().Count());
    }

    [Fact]
    public void Populate_AScreenWithNoBooth_IsGivenNoAccessCode()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        Assert.All(world, screen => Assert.Null(screen.AccessCode));
    }

    [Fact]
    public void Populate_NeverPlacesAnythingOnATileThatAlreadyMeansSomething()
    {
        // Arrange - a screen whose only standable row is breakable bridge, which is standable but
        // is on the list of tiles that already mean something
        GameScreenData[] world = TestData.World();
        world[3] = TestData.BareScreen();
        for (int x = 0; x < TileMetrics.TilesAcross; x++)
        {
            world[3].ScreenTiles[x, 3] = new ScreenTile((int)ActionTiles.BreakableBridges);
        }

        // Act
        Populate(world);

        // Assert
        Assert.Null(world[3].PowerUp);
        Assert.Null(world[3].CheopsPyramid);
    }

    [Fact]
    public void Populate_NeverPlacesAnythingOnATileWithSomethingDirectlyAboveIt()
    {
        // Arrange - the screen's one standable row is the top one, with the row above it filled in
        GameScreenData[] world = TestData.World();
        world[3] = TestData.BareScreen();
        for (int x = 0; x < TileMetrics.TilesAcross; x++)
        {
            world[3].ScreenTiles[x, 0] = new ScreenTile(TestData.PlatformTile);
            world[3].ScreenTiles[x, 1] = new ScreenTile(TestData.PlatformTile);
        }

        // Act
        Populate(world);

        // Assert
        Assert.Null(world[3].PowerUp);
    }

    [Fact]
    public void Populate_PrefersTheTileTheAuthorMarkedForACoreElement()
    {
        // Arrange - a screen with a marked tile among its platforms, and a seed under which it
        // draws a pick-up
        GameScreenData[] world = TestData.World();
        world[3].ScreenTiles[5, 3] = new ScreenTile((int)ActionTiles.PreferredCoreLocation);

        // Act
        Populate(world);

        // Assert - if this screen kept a pick-up, it is on the marked tile
        if (world[3].PickUps.Count > 0)
        {
            Assert.Equal(
                5 * TileMetrics.TileWidth + TileMetrics.CollisionCellWidth,
                world[3].PickUps[0].XLocation);
            Assert.Equal(
                3 * TileMetrics.TileHeight + TileMetrics.CollisionCellHeight,
                world[3].PickUps[0].YLocation);
        }
    }

    [Fact]
    public void Populate_AskedForNoCoreElements_StillFinishesRatherThanIndexingPastTheEnd()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        var populator = new WorldPopulator(world, MapFor(world), new Random(1));

        // Act
        populator.Populate([]);

        // Assert
        Assert.Equal(TestData.MaxCoreElements, world.Count(screen => screen.PickUps.Count > 0));
    }

    [Fact]
    public void Populate_MatchesSomeOfTheScatteredElementsToWhatTheCoreWants()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        var populator = new WorldPopulator(world, MapFor(world), new Random(7));

        int[] wanted = [101, 102, 103, 104, 105];

        // Act
        populator.Populate(wanted);

        // Assert - between two and five of them, and each used at most once
        List<int> matched = world
            .SelectMany(screen => screen.PickUps)
            .Select(pickUp => pickUp.Type)
            .Where(wanted.Contains)
            .ToList();

        Assert.InRange(matched.Count, 2, 5);
        Assert.Equal(matched.Count, matched.Distinct().Count());
    }

    private static void Populate(GameScreenData[] world, int seed = 1)
        => new WorldPopulator(world, MapFor(world), new Random(seed)).Populate(TestData.CoreElementsWanted);

    private static ScreenCollisionMap MapFor(GameScreenData[] world)
        => new(world, TestData.TileCollisionData());

    private static class TestData
    {
        public const int MaxCheops = 40;

        public const int MaxCoreElements = 18;

        public const int PowerUpTypeCount = 8;

        /// <summary>A tile image solid along its top row - something an item can stand on.</summary>
        public const int PlatformTile = 2;

        public static readonly int[] CoreElementsWanted = [1, 2, 3, 4, 5, 6, 7, 8, 9];

        /// <summary>
        ///     A whole world of identical screens, each with a row of platforms across the middle and
        ///     clear air above them, so every screen has somewhere an item could go. The shipped map is
        ///     deliberately not used: a test that says what a screen looks like is a test whose failure
        ///     can be read.
        /// </summary>
        /// <summary>A screen of nothing at all - no tile an item could be put on.</summary>
        public static GameScreenData BareScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

        public static GameScreenData[] World()
        {
            var world = new GameScreenData[WorldMap.ScreenCount];

            for (int screen = 0; screen < world.Length; screen++)
            {
                world[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);

                for (int x = 0; x < TileMetrics.TilesAcross; x++)
                {
                    world[screen].ScreenTiles[x, 3] = new ScreenTile(PlatformTile);
                }
            }

            return world;
        }

        public static int[,,] TileCollisionData()
        {
            var data = new int[
                512,
                TileMetrics.CollisionCellsAcrossTile,
                TileMetrics.CollisionCellsDownTile];

            for (int across = 0; across < TileMetrics.CollisionCellsAcrossTile; across++)
            {
                data[PlatformTile, across, 0] = 1;

                // Standable, so that the only reason nothing is placed on one is the exclusion list
                data[(int)ActionTiles.BreakableBridges, across, 0] = 1;
            }

            return data;
        }

        /// <summary>
        ///     Everything the scatter decided, as one string, so two worlds can be compared whole.
        /// </summary>
        public static string Describe(GameScreenData[] world)
            => string.Join(
                "|",
                world.Select(screen =>
                    $"{screen.PowerUp?.XLocation},{screen.PowerUp?.YLocation},{screen.PowerUp?.Type}" +
                    $";{screen.CheopsPyramid?.XLocation},{screen.CheopsAccessCode}" +
                    $";{screen.AccessCode}" +
                    $";{string.Join(",", screen.PickUps.Select(p => $"{p.XLocation}:{p.YLocation}:{p.Type}"))}"));
    }
}
