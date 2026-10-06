// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.Core;
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
        Populate(first, 12345);
        Populate(second, 12345);

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
        Populate(first, 12345);
        Populate(second, 54321);

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
        Assert.Equal(TestData.MaxCoreElements,
            world.Count(screen => screen.PickUps.Any(TestData.IsScattered)));
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
    ///     The random scatter still never rolls either item: the type draw stops one short of
    ///     <see cref="Collectables.AccessCard" /> (28) and <see cref="Collectables.Key" /> (29). They are
    ///     placed deliberately instead, which is what guarantees exactly one of each rather than however
    ///     many a dice roll happened to produce.
    /// </summary>
    [Fact]
    public void Populate_PlacesExactlyOneKeyAndOneAccessCard()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        List<InteractiveItem> placed = world.SelectMany(screen => screen.PickUps).ToList();

        Assert.Single(placed, pickUp => pickUp.Type == (int)Collectables.Key);
        Assert.Single(placed, pickUp => pickUp.Type == (int)Collectables.AccessCard);
    }

    /// <summary>
    ///     The rule that keeps the game winnable: neither item may be put anywhere that can only be
    ///     reached by passing the very gate it opens.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(99)]
    public void Populate_PutsNeitherItemBehindTheGateItOpens(int seed)
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world, seed);

        // Assert
        AssertItemIsOnAGateFreeScreen(world, Collectables.Key, Gate.KeyDoor);
        AssertItemIsOnAGateFreeScreen(world, Collectables.AccessCard, Gate.AccessBooth);
    }

    /// <summary>
    ///     Neither item is dropped on top of something already lying there - a power-up, a pyramid, or a
    ///     pick-up, which is what a core element is.
    /// </summary>
    [Fact]
    public void Populate_PutsNeitherItemOnASquareSomethingElseIsOn()
    {
        // Arrange
        GameScreenData[] world = TestData.World();

        // Act
        Populate(world);

        // Assert
        foreach (GameScreenData screen in world)
        {
            var squares = new List<(int X, int Y)>();

            if (screen.PowerUp is { } powerUp)
            {
                squares.Add((powerUp.XLocation, powerUp.YLocation));
            }

            if (screen.CheopsPyramid is { } pyramid)
            {
                squares.Add((pyramid.XLocation, pyramid.YLocation));
            }

            squares.AddRange(screen.PickUps.Select(pickUp => (pickUp.XLocation, pickUp.YLocation)));

            Assert.Equal(squares.Count, squares.Distinct().Count());
        }
    }

    private static void AssertItemIsOnAGateFreeScreen(
        GameScreenData[] world,
        Collectables item,
        Gate gate)
    {
        HashSet<int> safe = GateFreeScreens.Find(world, MapFor(world), gate);

        for (int screen = 0; screen < world.Length; screen++)
        {
            if (world[screen].PickUps.Any(pickUp => pickUp.Type == (int)item))
            {
                Assert.Contains(screen, safe);
                return;
            }
        }

        Assert.Fail(item + " was never placed at all");
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
        => AssertEveryPickUpLandsOnTheTileMarkedAt(3);

    /// <summary>
    ///     The scan for the marker used to start at row 1, borrowed from the scan for somewhere to
    ///     stand an item - which has to start there, because it places on top of the tile it finds and
    ///     reads the row above it. This one places inside the marked tile and reads nothing else, so
    ///     the top row is as good a place to mark as any other.
    /// </summary>
    [Fact]
    public void Populate_TheMarkedTileIsOnTheTopRow_IsStillPreferred()
        => AssertEveryPickUpLandsOnTheTileMarkedAt(0);

    /// <summary>
    ///     Marks the same tile position on every screen and then checks that every pick-up the scatter
    ///     placed is on a marked tile.
    /// </summary>
    /// <remarks>
    ///     Which screens get a pick-up is the generator's own business - it draws them at random and
    ///     then thins them down - so marking one screen and hoping proves nothing under most seeds.
    ///     Marking all of them means whichever screens it chose are marked ones.
    /// </remarks>
    private static void AssertEveryPickUpLandsOnTheTileMarkedAt(int row)
    {
        // Arrange
        const int markedColumn = 5;

        GameScreenData[] world = TestData.World();
        foreach (GameScreenData screen in world)
        {
            screen.ScreenTiles[markedColumn, row] = new ScreenTile((int)MarkerTile.PreferredCoreLocation);
        }

        // Act
        Populate(world);

        // Assert - the key and the card are placed wherever there is room, not on the marked tile, so
        // this is about the scattered pick-ups only
        List<InteractiveItem> placed = world
            .SelectMany(screen => screen.PickUps)
            .Where(TestData.IsScattered)
            .ToList();

        Assert.NotEmpty(placed);
        Assert.All(placed, pickUp =>
        {
            Assert.Equal(markedColumn * TileMetrics.TileWidth + TileMetrics.CollisionCellWidth, pickUp.XLocation);
            Assert.Equal(row * TileMetrics.TileHeight + TileMetrics.CollisionCellHeight, pickUp.YLocation);
        });
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
        Assert.Equal(TestData.MaxCoreElements,
            world.Count(screen => screen.PickUps.Any(TestData.IsScattered)));
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
        ///     Whether a pick-up is one the scatter placed. The key and the access card are put on
        ///     deliberately once the scatter has finished, so a test about scattering should not count
        ///     them - and before they were placed at all, every pick-up was a scattered one.
        /// </summary>
        public static bool IsScattered(InteractiveItem pickUp)
            => pickUp.Type != (int)Collectables.Key &&
               pickUp.Type != (int)Collectables.AccessCard;

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
            int[,,] data = new int[
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
