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
///     Which tile a probe reports when it covers more than one, which is the only part of the
///     collision rework that changes how the game plays.
/// </summary>
/// <remarks>
///     Everything a probe is for goes through one of five decisions in <c>Blob</c> - the two tunnel
///     mouths, the two sides of an access booth, and the teleporter - and each of them re-checks
///     the tile's identity and where Blob stands inside it before acting. So the question worth
///     asking is not whether the probe reports a different tile, which it does in well over a
///     million places, but whether any of those five decisions comes out differently. Those are
///     measured here against <see cref="ReferenceCollisionMap" />, which still scans top-left
///     first, over every pixel position on every shipped screen that has one of those tiles on it.
///     <para>
///         The answer is that the change is additive: nothing that could be entered before stops
///         being enterable, and what is gained is an eight-pixel band at eighteen places where
///         entry was impossible. See <see cref="TheDeadZone" /> for why the band is eight pixels.
///     </para>
/// </remarks>
public class CheckTileByObjectTests
{
    /// <summary>
    ///     Blob's walking speed, which is what the probes are stretched by, and his size.
    /// </summary>
    private const int Velocity = 8;

    private const int BlobSize = 64;

    /// <summary>
    ///     Every one of the five decisions needs the probe to report that Blob's top sits 32 pixels
    ///     into the tile, so only the rows that can produce that are worth walking.
    /// </summary>
    private const int TriggerYPosition = 32;

    /// <summary>
    ///     What the change is worth, in pixels of Blob's travel, at each of the places it reaches.
    ///     The probe going left runs from eight pixels behind him, so it reaches into the tile on
    ///     his left exactly while he stands in the first eight pixels of his own - and for those
    ///     eight pixels that neighbour used to answer in place of the mouth he was standing in.
    /// </summary>
    private const int TheDeadZone = Velocity;

    /// <summary>
    ///     How far Blob's left edge sits inside the tile to the left of the one being looked for,
    ///     chosen so that he is in both and more of him is in that one.
    /// </summary>
    private const int DeeperIntoTheMarker = 38;

    [Fact]
    public void AcrossTheShippedMap_NothingThatCouldBeEnteredBefore_StopsBeingEnterable()
    {
        // Arrange / Act
        (List<string> gained, List<string> lost) = EntryDecisionsThatChanged();

        // Assert
        Assert.Empty(lost);
        Assert.NotEmpty(gained);
    }

    [Fact]
    public void AcrossTheShippedMap_WhatIsGained_IsAnEightPixelBandAtEighteenPlaces()
    {
        // Arrange / Act
        (List<string> gained, _) = EntryDecisionsThatChanged();

        // Each gained entry is one pixel of one decision at one place, so the places are what is
        // left when the pixel is dropped off the end.
        List<string> places = [.. gained.Select(PlaceOf).Distinct().Order()];

        // Assert
        Assert.Equal(18, places.Count);
        Assert.Equal(18 * TheDeadZone, gained.Count);
        Assert.All(places, place =>
            Assert.Equal(TheDeadZone, gained.Count(entry => PlaceOf(entry) == place)));
    }

    /// <summary>
    ///     The shape of the defect, written out rather than hunted for: a left-hand tunnel mouth
    ///     with scenery beside it, and Blob standing in the first few pixels of the mouth.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(7)]
    public void StandingInTheFirstPixelsOfATunnelMouth_ReportsTheMouthAndNotItsNeighbour(int intoTheMouth)
    {
        // Arrange - scenery in column 0, a left-hand mouth in column 1
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);
        screen.ScreenTiles[1, 0] = new ScreenTile(TunnelTiles.LeftMouth1);

        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        int xPos = TileMetrics.TileWidth + intoTheMouth;

        // Act
        TileDetected? toTheLeft = map.CheckTileByObject(
            Direction.Left, xPos, TriggerYPosition, BlobSize, BlobSize, Velocity);

        // Assert
        Assert.NotNull(toTheLeft);
        Assert.Equal(TunnelTiles.LeftMouth1, toTheLeft.TileNumber);
        Assert.Equal(intoTheMouth, toTheLeft.XPosition);
    }

    /// <summary>
    ///     The other half of the same case: a mouth Blob has walked out of does not follow him. He
    ///     is in the neighbour now, so the neighbour is what answers.
    /// </summary>
    [Fact]
    public void StandingWhollyInTheNeighbour_ReportsTheNeighbourAndNotTheMouth()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile(TestData.SolidTile);
        screen.ScreenTiles[1, 0] = new ScreenTile(TunnelTiles.LeftMouth1);

        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        // Wholly inside column 0, with the probe still reaching into column 1
        int xPos = TileMetrics.TileWidth - BlobSize;

        // Act
        TileDetected? toTheLeft = map.CheckTileByObject(
            Direction.Left, xPos, TriggerYPosition, BlobSize, BlobSize, Velocity);

        // Assert
        Assert.NotNull(toTheLeft);
        Assert.Equal(TestData.SolidTile, toTheLeft.TileNumber);
    }

    /// <summary>
    ///     A marker is a position the author noted rather than scenery, and <c>Rebuild</c> has
    ///     always passed over them. This did not, so one dropped beside a tunnel mouth answered in
    ///     its place - and because every caller then checks the tile's identity, the tunnel simply
    ///     stopped working with nothing to show for it.
    /// </summary>
    [Fact]
    public void AMarkerBesideATunnelMouth_IsPassedOverRatherThanReportedInItsPlace()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        screen.ScreenTiles[0, 0] = new ScreenTile((int)MarkerTile.Spacer);
        screen.ScreenTiles[1, 0] = new ScreenTile(TunnelTiles.LeftMouth1);

        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        // Act - straddling the two, and further into the marker's column than into the mouth, so
        // preferring the tile he stands in picks the marker and only skipping it picks the mouth
        TileDetected? toTheLeft = map.CheckTileByObject(
            Direction.Left, TileMetrics.TileWidth - DeeperIntoTheMarker, TriggerYPosition,
            BlobSize, BlobSize, Velocity);

        // Assert
        Assert.NotNull(toTheLeft);
        Assert.Equal(TunnelTiles.LeftMouth1, toTheLeft.TileNumber);
    }

    [Fact]
    public void AnEmptyScreen_ReportsNothingInAnyDirection()
    {
        // Arrange
        GameScreenData screen = TestData.EmptyScreen();
        var map = new ScreenCollisionMap([screen], TestData.TileCollisionData());
        map.Rebuild(0);

        // Act / Assert
        foreach (Direction direction in TestData.ProbeDirections)
        {
            Assert.Null(map.CheckTileByObject(direction, 256, 256, BlobSize, BlobSize, Velocity));
        }
    }

    /// <summary>
    ///     Every place and pixel where one of Blob's five entry decisions comes out differently
    ///     from the way the top-left-first scan had it.
    /// </summary>
    private static (List<string> Gained, List<string> Lost) EntryDecisionsThatChanged()
    {
        (GameScreenData[] screens, int[,,] masks) = ShippedMap.Load();
        var subject = new ScreenCollisionMap(screens, masks);
        var reference = new ReferenceCollisionMap(screens, masks);

        List<string> gained = [];
        List<string> lost = [];

        for (int screen = 0; screen < screens.Length; screen++)
        {
            if (!HasSomethingToEnter(screens[screen]))
            {
                continue;
            }

            subject.Rebuild(screen);
            reference.Rebuild(screen);

            for (int y = TriggerYPosition; y < TileMetrics.PlayAreaHeight; y += TileMetrics.TileHeight)
            {
                for (int x = 0; x < TileMetrics.PlayAreaWidth; x++)
                {
                    EntryDecisions now = DecisionsFrom(
                        subject.CheckTileByObject(Direction.Left, x, y, BlobSize, BlobSize, Velocity),
                        subject.CheckTileByObject(Direction.Right, x, y, BlobSize, BlobSize, Velocity));

                    EntryDecisions before = DecisionsFrom(
                        reference.CheckTileByObject(Direction.Left, x, y, BlobSize, BlobSize, Velocity),
                        reference.CheckTileByObject(Direction.Right, x, y, BlobSize, BlobSize, Velocity));

                    foreach ((string name, bool fires, bool used) in now.Against(before))
                    {
                        (fires ? gained : lost).Add($"{name} screen {screen} y {y} x {x} was {used}");
                    }
                }
            }
        }

        return (gained, lost);
    }

    /// <summary>
    ///     A gained entry without its pixel, which is what counts as one place.
    /// </summary>
    private static string PlaceOf(string entry)
        => entry[..entry.IndexOf(" x ", StringComparison.Ordinal)];

    private static bool HasSomethingToEnter(GameScreenData screen)
    {
        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tileImage = screen.ScreenTiles[x, y].TileImage;

                if (TunnelTiles.IsLeftMouth(tileImage) ||
                    TunnelTiles.IsRightMouth(tileImage) ||
                    tileImage == (int)ActionTiles.TeleportBooth ||
                    tileImage == (int)ActionTiles.AccessBoothLeft ||
                    tileImage == (int)ActionTiles.AccessBoothRight)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static EntryDecisions DecisionsFrom(TileDetected? toTheLeft, TileDetected? toTheRight)
        => new(
            toTheLeft is not null && TunnelTiles.IsLeftMouth(toTheLeft.TileNumber) &&
            toTheLeft.XPosition <= 32 && toTheLeft.YPosition == TriggerYPosition,
            toTheRight is not null && TunnelTiles.IsRightMouth(toTheRight.TileNumber) &&
            toTheRight.XPosition >= 32 && toTheRight.YPosition == TriggerYPosition,
            toTheRight is { TileNumber: (int)ActionTiles.TeleportBooth } &&
            toTheRight.XPosition >= 24 && toTheRight.YPosition == BoothMetrics.BlobYOffset,
            toTheLeft is { TileNumber: (int)ActionTiles.AccessBoothRight } && toTheRight is not null &&
            toTheLeft.XPosition <= 46 && toTheLeft.YPosition == TriggerYPosition,
            toTheRight is { TileNumber: (int)ActionTiles.AccessBoothLeft } && toTheLeft is not null &&
            toTheRight.XPosition >= 24 && toTheRight.YPosition == TriggerYPosition);

    /// <summary>
    ///     The five things Blob does from a probe, as the five answers themselves - the conditions
    ///     are copied from <c>Blob</c> rather than reached through it, because reaching them means
    ///     building a Blob, and a Blob needs content.
    /// </summary>
    private readonly record struct EntryDecisions(
        bool LeftTunnel,
        bool RightTunnel,
        bool Teleporter,
        bool BoothFromTheLeft,
        bool BoothFromTheRight)
    {
        public IEnumerable<(string Name, bool Fires, bool Used)> Against(EntryDecisions before)
        {
            if (LeftTunnel != before.LeftTunnel)
            {
                yield return (nameof(LeftTunnel), LeftTunnel, before.LeftTunnel);
            }

            if (RightTunnel != before.RightTunnel)
            {
                yield return (nameof(RightTunnel), RightTunnel, before.RightTunnel);
            }

            if (Teleporter != before.Teleporter)
            {
                yield return (nameof(Teleporter), Teleporter, before.Teleporter);
            }

            if (BoothFromTheLeft != before.BoothFromTheLeft)
            {
                yield return (nameof(BoothFromTheLeft), BoothFromTheLeft, before.BoothFromTheLeft);
            }

            if (BoothFromTheRight != before.BoothFromTheRight)
            {
                yield return (nameof(BoothFromTheRight), BoothFromTheRight, before.BoothFromTheRight);
            }
        }
    }

    private static class TestData
    {
        /// <summary>A tile image solid in every cell.</summary>
        public const int SolidTile = 1;

        public static readonly Direction[] ProbeDirections =
            [Direction.Up, Direction.Down, Direction.Left, Direction.Right, Direction.None];

        private const int TestTileImageCount = 512;

        public static GameScreenData EmptyScreen()
            => new(TileMetrics.TilesAcross, TileMetrics.TilesDown);

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
            }

            return data;
        }
    }
}
