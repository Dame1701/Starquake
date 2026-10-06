// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -

using Starquake.Core;
using Starquake.Structure;
namespace Starquake.ActiveSprites;

/// <summary>
///     Whether Blob is far enough into a spiked tile for its spikes to reach him.
/// </summary>
/// <remarks>
///     Each of the three spike tiles is given the reach its own artwork suggests rather than the
///     whole tile, so the three cases below are three reaches and not three copies of one test. The
///     numbers are Blob's position relative to the tile, which is why several are negative: a
///     negative one means the tile starts before Blob does.
///     <para>
///         This was already a pure static method inside <c>Blob</c> and needed nothing but a home
///         to be testable. Seven numbers decide three hitboxes and none of them was covered.
///     </para>
/// </remarks>
internal static class Spikes
{
    /// <summary>
    ///     The spike standing in the middle of its tile. Reaches across most of the tile's width and
    ///     a little above its top edge.
    /// </summary>
    private const int SpikeReachLeft = -16;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int SpikeReachRight = 64;

    /// <inheritdoc cref="SpikeReachLeft" />
    private const int CentralSpikeReachUp = -8;

    /// <summary>
    ///     The spike mounted on the right-hand wall. Measured down the tile rather than across it,
    ///     and Blob has to have got some way in before it can touch him.
    /// </summary>
    private const int WallSpikeReachDown = 64;

    /// <inheritdoc cref="WallSpikeReachDown" />
    private const int WallSpikeReachUp = -32;

    /// <inheritdoc cref="WallSpikeReachDown" />
    private const int WallSpikeReachIn = 16;

    /// <summary>
    ///     The field of broken glass on the floor. The same width as the central spike and a shallower
    ///     reach upwards, because it lies flat.
    /// </summary>
    private const int BrokenGlassReachUp = -16;

    /// <inheritdoc cref="Spikes" />
    public static bool HaveGotHim(TileDetected tile)
        => tile.TileNumber switch
        {
            (int)ActionTiles.Spike2 => tile.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                                       tile.YPosition > CentralSpikeReachUp,
            (int)ActionTiles.Spike1 => tile.YPosition is < WallSpikeReachDown and > WallSpikeReachUp &&
                                       tile.XPosition > WallSpikeReachIn,
            (int)ActionTiles.Spike3 => tile.XPosition is > SpikeReachLeft and < SpikeReachRight &&
                                       tile.YPosition > BrokenGlassReachUp,
            _ => false
        };
}
