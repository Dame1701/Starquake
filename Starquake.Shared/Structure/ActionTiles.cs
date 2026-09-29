// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

/// <summary>
///     Tiles the game has to recognise by number rather than just draw. A screen is stored as a grid
///     of indices into the tile sheet, so anything the blob can interact with - a platform that can
///     be taken away, a spike that kills, a tunnel mouth that leads to the next screen - is only
///     distinguishable by the index that was painted there.
/// </summary>
public enum ActionTiles
{
    ShuttlePlatform = 395,
    RemovedShuttlePlatform = 398,
    ElectricityNode = 394,
    ShuttleTag1 = 401,
    Spike1 = 404,
    Spike2 = 405,
    ElectricityGate = 406,
    TunnelLiftTile = 408,
    Spike3 = 416,
    ShuttleTag2 = 418,
    ShuttleTag3 = 417,
    BlankFiller = 390,
    ConveyerNasty = 391,
    BreakableBridges = 410,
    PreferredCoreLocation = 392,

    /// <summary>
    ///     The teleporter booth. Walking into one from the left switches the game to the teleporter
    ///     screen.
    /// </summary>
    TeleportBooth = 397,

    /// <summary>
    ///     The two halves of an access booth. Which one the blob walks into decides which side of the
    ///     booth it appears on, so they are distinct tiles rather than one tile tested twice.
    /// </summary>
    AccessBoothLeft = 399,

    /// <inheritdoc cref="AccessBoothLeft" />
    AccessBoothRight = 400,

    /// <summary>
    ///     The body of a tunnel lift - the tiles that carry the blob upwards while it stands under
    ///     them. <see cref="TunnelLiftTile" /> is one of these too.
    /// </summary>
    TunnelLiftShaft1 = 409,

    /// <inheritdoc cref="TunnelLiftShaft1" />
    TunnelLiftShaft2 = 412,

    /// <inheritdoc cref="TunnelLiftShaft1" />
    TunnelLiftShaft3 = 413,

    /// <summary>
    ///     The top of a tunnel lift. Reaching one of these ends the ride.
    ///     <see cref="TunnelLiftTopAlternate" /> is another of them that only the redrawn artwork
    ///     has, which is why the original graphics never look for it.
    /// </summary>
    TunnelLiftTop1 = 407,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTopAlternate = 396,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTop2 = 414,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTop3 = 415,

    /// <summary>
    ///     The force field across an access booth. The blob can walk through it but cannot use its
    ///     inventory while standing in one, which is what stops a key being produced from inside the
    ///     booth it is meant to open.
    /// </summary>
    AccessForceField = 419
}
