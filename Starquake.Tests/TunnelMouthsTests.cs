// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Whether walking at a tunnel mouth takes Blob through it. <c>TunnelTilesTests</c> already
///     covers which tiles are mouths; this is the standing and the reach, which were two bare 32s
///     inside <c>Blob</c> and so could not be reached at all.
/// </summary>
public class TunnelMouthsTests
{
    /// <summary>Blob's top edge when he is standing on the tunnel's floor.</summary>
    private const int OnTheFloor = 32;

    private const int Reach = 32;

    private static TileDetected At(int tileNumber, int xPos, int yPos)
        => new(tileNumber, xPos, yPos, 0, 0);

    [Fact]
    public void IntoALeftMouth_NoTileThere_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoALeftMouth(null));

    [Fact]
    public void IntoALeftMouth_StandingInOneWithinReach_TakesHim()
        => Assert.True(TunnelMouths.IntoALeftMouth(At(TunnelTiles.LeftMouth1, Reach, OnTheFloor)));

    [Fact]
    public void IntoALeftMouth_ATileThatIsNotAMouth_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoALeftMouth(At(TunnelTiles.LeftMouth1 - 1, 0, OnTheFloor)));

    /// <summary>
    ///     A right-hand mouth is not a left-hand one, which matters because the two are interleaved
    ///     in one run of tile numbers - a left mouth is always even and its partner the next number
    ///     up - so getting this wrong would send Blob the wrong way rather than nowhere.
    /// </summary>
    [Fact]
    public void IntoALeftMouth_ARightHandMouth_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoALeftMouth(At(TunnelTiles.RightMouth1, 0, OnTheFloor)));

    /// <summary>
    ///     Walking leftwards Blob meets the tile from its far side, so the reach is measured towards
    ///     the tile's left edge: one pixel short of it and the mouth does not have him.
    /// </summary>
    [Fact]
    public void IntoALeftMouth_NotYetWithinReach_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoALeftMouth(At(TunnelTiles.LeftMouth1, Reach + 1, OnTheFloor)));

    /// <summary>
    ///     The standing is exact, not a range. A mouth cut into the wall of a shaft Blob is falling
    ///     past must not swallow him on the way down, and one he is standing on the lip of above must
    ///     not either - so a pixel either side of the floor is no entry at all.
    /// </summary>
    [Theory]
    [InlineData(OnTheFloor - 1)]
    [InlineData(OnTheFloor + 1)]
    [InlineData(0)]
    public void IntoALeftMouth_NotStandingOnTheFloorOfIt_TakesHimNowhere(int yPos)
        => Assert.False(TunnelMouths.IntoALeftMouth(At(TunnelTiles.LeftMouth1, 0, yPos)));

    [Fact]
    public void IntoARightMouth_StandingInOneFarEnoughIn_TakesHim()
        => Assert.True(TunnelMouths.IntoARightMouth(At(TunnelTiles.RightMouth1, Reach, OnTheFloor)));

    /// <summary>
    ///     And the reach the other way round. Walking rightwards Blob meets the tile from its left,
    ///     so he has to have got into it rather than up to it - which is the one asymmetry between
    ///     the two mouths and was a <c>&lt;</c> against a <c>&gt;</c> in two conditions ten lines apart.
    /// </summary>
    [Fact]
    public void IntoARightMouth_NotYetFarEnoughIn_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoARightMouth(At(TunnelTiles.RightMouth1, Reach - 1, OnTheFloor)));

    [Fact]
    public void IntoARightMouth_ALeftHandMouth_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoARightMouth(At(TunnelTiles.LeftMouth1, Reach, OnTheFloor)));

    [Fact]
    public void IntoARightMouth_NoTileThere_TakesHimNowhere()
        => Assert.False(TunnelMouths.IntoARightMouth(null));

    /// <summary>
    ///     Every one of the ten pairs behaves the same way, which is the whole claim
    ///     <see cref="TunnelTiles" />'s remarks make about the run being contiguous and interleaved.
    /// </summary>
    [Fact]
    public void BothMouths_AcrossAllTenPairs_TakeHimOnTheirOwnSideOnly()
    {
        for (int pair = 0; pair < TunnelTiles.MouthPairs; pair++)
        {
            int left = TunnelTiles.FirstMouth + pair * TunnelTiles.MouthStride;
            int right = left + 1;

            Assert.True(TunnelMouths.IntoALeftMouth(At(left, Reach, OnTheFloor)), $"left mouth {left}");
            Assert.False(TunnelMouths.IntoARightMouth(At(left, Reach, OnTheFloor)), $"left mouth {left}");

            Assert.True(TunnelMouths.IntoARightMouth(At(right, Reach, OnTheFloor)), $"right mouth {right}");
            Assert.False(TunnelMouths.IntoALeftMouth(At(right, Reach, OnTheFloor)), $"right mouth {right}");
        }
    }
}
