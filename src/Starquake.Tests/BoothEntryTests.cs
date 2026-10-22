// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Whether walking at a booth's doorway puts Blob in it. Three doorways - a teleport booth and
///     either hand of an access booth - over two reaches and one standing.
/// </summary>
public class BoothEntryTests
{
    private const int Reach = 24;
    private const int RightHandReach = 46;
    private static readonly int OnTheFloor = BoothMetrics.BlobYOffset;

    private static TileDetected At(ActionTiles tile, int xPos, int yPos)
        => new((int)tile, xPos, yPos, 0, 0);

    [Fact]
    public void IntoATeleporter_NoTileThere_IsNotAnEntry()
        => Assert.False(BoothEntry.IntoATeleporter(null));

    [Fact]
    public void IntoATeleporter_StandingInTheDoorway_IsAnEntry()
        => Assert.True(BoothEntry.IntoATeleporter(At(ActionTiles.TeleportBooth, Reach, OnTheFloor)));

    /// <summary>
    ///     The three doorways are three different tiles and each predicate answers for its own only.
    ///     An access booth is not a teleporter. The two do entirely different things to Blob, so one
    ///     answering for the other is worse than neither answering at all.
    /// </summary>
    [Theory]
    [InlineData(ActionTiles.AccessBoothLeft)]
    [InlineData(ActionTiles.AccessBoothRight)]
    [InlineData(ActionTiles.ShuttlePlatform)]
    public void IntoATeleporter_SomeOtherTile_IsNotAnEntry(ActionTiles tile)
        => Assert.False(BoothEntry.IntoATeleporter(At(tile, Reach, OnTheFloor)));

    /// <summary>
    ///     The reach approaching from the left is a little past where a booth stands Blob, so he
    ///     settles back onto the doorway as it takes over rather than having to hit it exactly. One
    ///     pixel short is no entry.
    /// </summary>
    [Theory]
    [InlineData(Reach - 1, false)]
    [InlineData(Reach, true)]
    [InlineData(1000, true)]
    public void IntoATeleporter_AcrossTheDoorway_NeedsHimFarEnoughIn(int xPos, bool entered)
        => Assert.Equal(entered, BoothEntry.IntoATeleporter(At(ActionTiles.TeleportBooth, xPos, OnTheFloor)));

    /// <summary>
    ///     The standing is exact, not a range, so a booth cannot take Blob as he falls past its
    ///     doorway. It is the same offset a booth puts him back down at, which is why
    ///     <see cref="BoothMetrics" /> exists. Walking in and arriving have to agree.
    /// </summary>
    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(0)]
    public void IntoATeleporter_NotStandingOnTheBoothFloor_IsNotAnEntry(int yPos)
        => Assert.False(BoothEntry.IntoATeleporter(At(ActionTiles.TeleportBooth, Reach, yPos)));

    /// <summary>
    ///     The floor the three inline values above are a pixel either side of is the one
    ///     <see cref="BoothMetrics" /> names, not a 32 that happens to match it.
    /// </summary>
    [Fact]
    public void BoothFloor_IsATileLessASprite()
        => Assert.Equal(TileMetrics.TileHeight - SpriteMetrics.SpriteHeight, OnTheFloor);

    [Fact]
    public void IntoALeftHandBooth_StandingInTheDoorway_IsAnEntry()
        => Assert.True(BoothEntry.IntoALeftHandBooth(At(ActionTiles.AccessBoothLeft, Reach, OnTheFloor)));

    /// <summary>
    ///     The left-hand access booth shares the teleporter's reach. Pinned so that if the two ever
    ///     stop agreeing, it is because someone decided they should.
    /// </summary>
    [Theory]
    [InlineData(Reach - 1)]
    [InlineData(Reach)]
    [InlineData(Reach + 1)]
    public void IntoALeftHandBooth_AcrossTheDoorway_AgreesWithTheTeleporter(int xPos)
        => Assert.Equal(
            BoothEntry.IntoATeleporter(At(ActionTiles.TeleportBooth, xPos, OnTheFloor)),
            BoothEntry.IntoALeftHandBooth(At(ActionTiles.AccessBoothLeft, xPos, OnTheFloor)));

    /// <summary>
    ///     The right-hand side is met from the far side of its tile, so its reach is measured
    ///     towards the tile's left edge and is its own number. The two tunnel mouths are asymmetric
    ///     the same way.
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(RightHandReach, true)]
    [InlineData(RightHandReach + 1, false)]
    public void IntoARightHandBooth_AcrossTheDoorway_ReachesTheOtherWay(int xPos, bool entered)
        => Assert.Equal(entered,
            BoothEntry.IntoARightHandBooth(At(ActionTiles.AccessBoothRight, xPos, OnTheFloor)));

    /// <inheritdoc cref="IntoARightHandBooth_AcrossTheDoorway_ReachesTheOtherWay" />
    [Fact]
    public void IntoARightHandBooth_WhereTheLeftHandOneWouldNotHaveHim_StillTakesHim()
    {
        // Arrange - up against the tile's left edge, which is as far out as the left hand reaches
        TileDetected onTheEdge = At(ActionTiles.AccessBoothRight, 0, OnTheFloor);

        // Assert
        Assert.True(BoothEntry.IntoARightHandBooth(onTheEdge));
        Assert.False(BoothEntry.IntoALeftHandBooth(At(ActionTiles.AccessBoothLeft, 0, OnTheFloor)));
    }

    [Fact]
    public void IntoARightHandBooth_TheLeftHandTile_IsNotAnEntry()
        => Assert.False(BoothEntry.IntoARightHandBooth(At(ActionTiles.AccessBoothLeft, 0, OnTheFloor)));

    [Fact]
    public void IntoARightHandBooth_NotStandingOnTheBoothFloor_IsNotAnEntry()
        => Assert.False(BoothEntry.IntoARightHandBooth(At(ActionTiles.AccessBoothRight, 0, 0)));
}
