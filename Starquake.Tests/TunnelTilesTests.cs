// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     Which tunnel mouths step the screen number which way. The ten of each direction were named
///     out in full at four separate sites - twice in <c>Blob</c>, deciding whether Blob walks
///     through, and twice in <c>ScreenRenderer</c>, deciding which sprite covers the tile - while
///     the constants describing the run as contiguous and alternating sat unused beside them.
/// </summary>
/// <remarks>
///     Deriving the answer rather than listing it is only safe if the layout really is what
///     <see cref="TunnelTiles" /> says, so these pin both ends of the run, both sides of each end,
///     and every mouth in between against the named constant it should agree with.
/// </remarks>
public class TunnelTilesTests
{
    [Theory]
    [InlineData(TunnelTiles.LeftMouth1)]
    [InlineData(TunnelTiles.LeftMouth2)]
    [InlineData(TunnelTiles.LeftMouth3)]
    [InlineData(TunnelTiles.LeftMouth4)]
    [InlineData(TunnelTiles.LeftMouth5)]
    [InlineData(TunnelTiles.LeftMouth6)]
    [InlineData(TunnelTiles.LeftMouth7)]
    [InlineData(TunnelTiles.LeftMouth8)]
    [InlineData(TunnelTiles.LeftMouth9)]
    [InlineData(TunnelTiles.LeftMouth10)]
    public void IsLeftMouth_ForEveryNamedLeftMouth_IsTrue(int tileImage)
    {
        // Assert
        Assert.True(TunnelTiles.IsLeftMouth(tileImage));
    }

    [Theory]
    [InlineData(TunnelTiles.RightMouth1)]
    [InlineData(TunnelTiles.RightMouth2)]
    [InlineData(TunnelTiles.RightMouth3)]
    [InlineData(TunnelTiles.RightMouth4)]
    [InlineData(TunnelTiles.RightMouth5)]
    [InlineData(TunnelTiles.RightMouth6)]
    [InlineData(TunnelTiles.RightMouth7)]
    [InlineData(TunnelTiles.RightMouth8)]
    [InlineData(TunnelTiles.RightMouth9)]
    [InlineData(TunnelTiles.RightMouth10)]
    public void IsRightMouth_ForEveryNamedRightMouth_IsTrue(int tileImage)
    {
        // Assert
        Assert.True(TunnelTiles.IsRightMouth(tileImage));
    }

    /// <summary>
    ///     The two directions are interleaved, so getting the parity the wrong way round would send
    ///     Blob through every tunnel backwards - which is the mistake this refactor could have made
    ///     and nothing else would have caught.
    /// </summary>
    [Theory]
    [InlineData(TunnelTiles.RightMouth1)]
    [InlineData(TunnelTiles.RightMouth10)]
    public void IsLeftMouth_ForARightMouth_IsFalse(int tileImage)
    {
        // Assert
        Assert.False(TunnelTiles.IsLeftMouth(tileImage));
    }

    [Theory]
    [InlineData(TunnelTiles.LeftMouth1)]
    [InlineData(TunnelTiles.LeftMouth10)]
    public void IsRightMouth_ForALeftMouth_IsFalse(int tileImage)
    {
        // Assert
        Assert.False(TunnelTiles.IsRightMouth(tileImage));
    }

    /// <summary>
    ///     The tiles either side of the run. Scenery, and an even one on each side, so a range
    ///     checked only for parity would call the one below a left mouth.
    /// </summary>
    [Theory]
    [InlineData(TunnelTiles.FirstMouth - 1)]
    [InlineData(TunnelTiles.FirstMouth - 2)]
    [InlineData(TunnelTiles.LastMouth + 1)]
    [InlineData(TunnelTiles.LastMouth + 2)]
    public void NeitherPredicate_ForATileOutsideTheRun_IsTrue(int tileImage)
    {
        // Assert
        Assert.False(TunnelTiles.IsLeftMouth(tileImage));
        Assert.False(TunnelTiles.IsRightMouth(tileImage));
    }

    /// <summary>
    ///     The run's own ends, which the range check has to include rather than stop short of.
    /// </summary>
    [Fact]
    public void ThePredicates_AtTheEndsOfTheRun_ClaimBothEnds()
    {
        // Assert
        Assert.True(TunnelTiles.IsLeftMouth(TunnelTiles.FirstMouth));
        Assert.True(TunnelTiles.IsRightMouth(TunnelTiles.LastMouth));
    }

    /// <summary>
    ///     Every tile in the run is a mouth of one direction or the other, and none is both.
    /// </summary>
    [Fact]
    public void ThePredicates_AcrossTheWholeRun_SplitItExactlyInTwo()
    {
        // Arrange
        int left = 0;
        int right = 0;

        // Act
        for (int tileImage = TunnelTiles.FirstMouth; tileImage <= TunnelTiles.LastMouth; tileImage++)
        {
            Assert.NotEqual(TunnelTiles.IsLeftMouth(tileImage), TunnelTiles.IsRightMouth(tileImage));

            if (TunnelTiles.IsLeftMouth(tileImage))
            {
                left++;
            }
            else
            {
                right++;
            }
        }

        // Assert
        Assert.Equal(TunnelTiles.MouthPairs, left);
        Assert.Equal(TunnelTiles.MouthPairs, right);
    }
}
