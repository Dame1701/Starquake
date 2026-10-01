// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The markers, and the one of them the game cannot start without: where the blob's ship came
///     down.
/// </summary>
/// <remarks>
///     The screen a game opens on, the square the blob is put down on and the square the wreck rests
///     in used to be three numbers written into three different files. They are painted on the map
///     now, so a typo in the screen editor is the only thing between an authored landing site and a
///     game that opens somewhere nobody meant - which is what these are here to catch.
/// </remarks>
public class LandingSiteTests
{

    /// <summary>
    ///     The point of the numbering: a marker is not on either tile sheet, so nothing can draw one by
    ///     indexing a sheet with it and nothing can read a collision mask for one.
    /// </summary>
    [Fact]
    public void All_EveryMarker_IsNumberedPastTheEndOfTheTileSheet()
        => Assert.All(MarkerTiles.All, marker => Assert.True((int)marker > TunnelTiles.LastMouth));

    /// <summary>
    ///     Both sets are painted into the same grid of numbers, so one number cannot mean a tile to
    ///     draw and a position to act on at the same time.
    /// </summary>
    [Fact]
    public void All_NoMarker_SharesANumberWithATileTheGameDraws()
    {
        IEnumerable<int> markers = MarkerTiles.All.Select(marker => (int)marker);
        IEnumerable<int> tiles = Enum.GetValues<ActionTiles>().Select(tile => (int)tile);

        Assert.Empty(markers.Intersect(tiles));
    }

    [Fact]
    public void Contains_AMarker_IsRecognised()
        => Assert.True(MarkerTiles.Contains((int)MarkerTile.Spacer));

    /// <summary>
    ///     The foot of a lift shaft is the near miss: nothing is drawn on it either, but it is a tile on
    ///     the sheet and it is solid, so treating it as a marker would take a lift's floor away.
    /// </summary>
    [Theory]
    [InlineData((int)ActionTiles.TunnelLiftTile)]
    [InlineData((int)ActionTiles.TeleportBooth)]
    [InlineData(TunnelTiles.LastMouth)]
    [InlineData(EmptySquare)]
    public void Contains_AnythingThatIsNotAMarker_IsNot(int tileImage)
        => Assert.False(MarkerTiles.Contains(tileImage));

    [Fact]
    public void IsOnePerMap_TheTwoHalvesOfTheLandingSite_AreOnePerMap()
    {
        Assert.True(MarkerTiles.IsOnePerMap(MarkerTile.BlobStartPosition));
        Assert.True(MarkerTiles.IsOnePerMap(MarkerTile.ShipCrashSite));
    }

    [Theory]
    [InlineData(MarkerTile.Spacer)]
    [InlineData(MarkerTile.ConveyerNasty)]
    [InlineData(MarkerTile.PreferredCoreLocation)]
    public void IsOnePerMap_AMarkerThatMayBePaintedAnywhere_IsNot(MarkerTile marker)
        => Assert.False(MarkerTiles.IsOnePerMap(marker));

    [Fact]
    public void Locate_AMarkerOnTheMap_FindsItsScreenAndSquare()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        world[31].ScreenTiles[6, 2] = new ScreenTile((int)MarkerTile.ShipCrashSite);

        // Act
        MarkerPlacement? found = MarkerTiles.Locate(world, MarkerTile.ShipCrashSite);

        // Assert
        Assert.Equal(new MarkerPlacement(31, 6, 2), found);
    }

    [Fact]
    public void Locate_AMarkerNowhereOnTheMap_FindsNothing()
        => Assert.Null(MarkerTiles.Locate(TestData.World(), MarkerTile.BlobStartPosition));

    [Fact]
    public void FindIn_AMapWithBothMarkersOnOneScreen_ReadsTheSiteOffIt()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        TestData.MarkLandingSite(world, 274, 1, 5, 4, 2);

        // Act
        LandingSite site = LandingSite.FindIn(world);

        // Assert
        Assert.Equal(new LandingSite(274, 1, 5, 4, 2), site);
    }

    /// <summary>
    ///     A map saved before the markers existed, which is every map in the world until one is saved
    ///     from the editor again. The site the game used to have written into it is a better answer
    ///     than starting the blob at the top left of screen zero.
    /// </summary>
    [Fact]
    public void FindIn_AMapWithNeitherMarker_FallsBackToTheSiteTheGameShippedWith()
        => Assert.Equal(LandingSite.Original, LandingSite.FindIn(TestData.World()));

    /// <summary>
    ///     Half a landing site is a screen somebody is part way through authoring, not a decision. The
    ///     editor says so and lets it be saved, so this is what the game does when it meets one.
    /// </summary>
    [Fact]
    public void FindIn_AMapWithAStartPositionAndNoCrashSite_FallsBackToTheSiteTheGameShippedWith()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        world[100].ScreenTiles[2, 2] = new ScreenTile((int)MarkerTile.BlobStartPosition);

        // Act and assert
        Assert.Equal(LandingSite.Original, LandingSite.FindIn(world));
    }

    /// <inheritdoc cref="FindIn_AMapWithAStartPositionAndNoCrashSite_FallsBackToTheSiteTheGameShippedWith" />
    [Fact]
    public void FindIn_AMapWithACrashSiteAndNoStartPosition_FallsBackToTheSiteTheGameShippedWith()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        world[100].ScreenTiles[2, 2] = new ScreenTile((int)MarkerTile.ShipCrashSite);

        // Act and assert
        Assert.Equal(LandingSite.Original, LandingSite.FindIn(world));
    }

    /// <summary>
    ///     The game opens on one screen, so a wreck on another one could never be seen. Taking the
    ///     start position and ignoring the crash site would put the blob on a screen with no wreck to
    ///     have climbed out of, which is worse than not believing the map at all.
    /// </summary>
    [Fact]
    public void FindIn_TheTwoMarkersOnDifferentScreens_FallsBackToTheSiteTheGameShippedWith()
    {
        // Arrange
        GameScreenData[] world = TestData.World();
        world[100].ScreenTiles[2, 2] = new ScreenTile((int)MarkerTile.BlobStartPosition);
        world[101].ScreenTiles[3, 3] = new ScreenTile((int)MarkerTile.ShipCrashSite);

        // Act and assert
        Assert.Equal(LandingSite.Original, LandingSite.FindIn(world));
    }

    /// <summary>
    ///     What <see cref="MarkerTiles.Contains" /> is asked about a square nothing has been painted on.
    /// </summary>
    private const int EmptySquare = -1;

    private static class TestData
    {
        /// <summary>
        ///     A whole planet of blank screens. Blank is the interesting state here: these tests are
        ///     about which marker is where, so anything else painted on a screen is noise.
        /// </summary>
        public static GameScreenData[] World()
        {
            var world = new GameScreenData[WorldMap.ScreenCount];

            for (int screen = 0; screen < world.Length; screen++)
            {
                world[screen] = new GameScreenData(TileMetrics.TilesAcross, TileMetrics.TilesDown);
            }

            return world;
        }

        public static void MarkLandingSite(
            GameScreenData[] world,
            int screen,
            int startX,
            int startY,
            int crashX,
            int crashY)
        {
            world[screen].ScreenTiles[startX, startY] = new ScreenTile((int)MarkerTile.BlobStartPosition);
            world[screen].ScreenTiles[crashX, crashY] = new ScreenTile((int)MarkerTile.ShipCrashSite);
        }
    }
}
