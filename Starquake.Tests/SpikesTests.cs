// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Core;
using Starquake.Structure;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The three spiked tiles and how far each of them reaches. Seven numbers decide three hitboxes,
///     and all seven were private constants of <c>Blob</c> beside a private static method - pure
///     already, and unreachable only because of where it lived.
/// </summary>
/// <remarks>
///     Positions are Blob's relative to the tile, so a negative one means the tile starts before he
///     does. The tests name the edges rather than guessing at the middle: a hitbox is only ever
///     wrong at its boundary.
/// </remarks>
public class SpikesTests
{
    private static TileDetected At(ActionTiles tile, int xPos, int yPos)
        => new((int)tile, xPos, yPos, 0, 0);

    [Fact]
    public void HaveGotHim_ATileWithNoSpikesOnIt_LetsHimPass()
        => Assert.False(Spikes.HaveGotHim(At(ActionTiles.ShuttlePlatform, 0, 0)));

    // --- the spike standing in the middle of its tile -------------------------------------------

    [Fact]
    public void HaveGotHim_StandingOnTheCentralSpike_GetsHim()
        => Assert.True(Spikes.HaveGotHim(At(ActionTiles.Spike2, 0, 0)));

    /// <summary>
    ///     Both edges across are exclusive, so a position exactly on either is a miss. -16 and 64
    ///     are the numbers; -15 and 63 are the first and last that touch him.
    /// </summary>
    [Theory]
    [InlineData(-16, false)]
    [InlineData(-15, true)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public void HaveGotHim_AcrossTheCentralSpike_ReachesBetweenItsEdgesAndNotOnThem(int xPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike2, xPos, 0)));

    /// <summary>
    ///     It reaches a little above the top of its own tile - the artwork stands proud of it - and
    ///     has no bottom edge at all, because Blob cannot be below a tile he is intersecting.
    /// </summary>
    [Theory]
    [InlineData(-8, false)]
    [InlineData(-7, true)]
    [InlineData(1000, true)]
    public void HaveGotHim_AboveTheCentralSpike_ReachesProudOfItsTile(int yPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike2, 0, yPos)));

    // --- the spike mounted on the right-hand wall ------------------------------------------------

    /// <summary>
    ///     The wall spike is the odd one out: it is measured down the tile rather than across it,
    ///     and it has a bottom edge where the other two have none. Walk under it and it misses.
    /// </summary>
    [Theory]
    [InlineData(-32, false)]
    [InlineData(-31, true)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public void HaveGotHim_DownTheWallSpike_ReachesBetweenItsEdgesAndNotOnThem(int yPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike1, 17, yPos)));

    /// <summary>
    ///     And Blob has to have got into the tile before the wall it is mounted on can touch him -
    ///     the one reach of the three that is not centred on the tile.
    /// </summary>
    [Theory]
    [InlineData(16, false)]
    [InlineData(17, true)]
    public void HaveGotHim_ApproachingTheWallSpike_NeedsHimInsideTheTile(int xPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike1, xPos, 0)));

    // --- the broken glass on the floor -----------------------------------------------------------

    /// <summary>
    ///     Broken glass lies flat, so it is the central spike's width with a shallower reach up: -16
    ///     against the spike's -8. Standing a dozen pixels above a spike is a miss and above glass
    ///     is a hit, which is the one place these two differ.
    /// </summary>
    [Theory]
    [InlineData(-16, false)]
    [InlineData(-15, true)]
    public void HaveGotHim_AboveTheBrokenGlass_ReachesLessFarUpThanASpike(int yPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike3, 0, yPos)));

    /// <inheritdoc cref="HaveGotHim_AboveTheBrokenGlass_ReachesLessFarUpThanASpike" />
    [Fact]
    public void HaveGotHim_TwelvePixelsAbove_IsGlassButNotASpike()
    {
        Assert.True(Spikes.HaveGotHim(At(ActionTiles.Spike3, 0, -12)));
        Assert.False(Spikes.HaveGotHim(At(ActionTiles.Spike2, 0, -12)));
    }

    /// <summary>
    ///     The glass takes its width from the same pair of numbers as the central spike, so the two
    ///     agree sideways and differ only upwards.
    /// </summary>
    [Theory]
    [InlineData(-16, false)]
    [InlineData(-15, true)]
    [InlineData(63, true)]
    [InlineData(64, false)]
    public void HaveGotHim_AcrossTheBrokenGlass_MatchesTheCentralSpike(int xPos, bool caught)
        => Assert.Equal(caught, Spikes.HaveGotHim(At(ActionTiles.Spike3, xPos, 0)));
}
