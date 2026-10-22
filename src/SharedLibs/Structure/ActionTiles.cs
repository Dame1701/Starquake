// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Structure;

/// <summary>
///     Tiles the game recognises by number rather than only drawing. A screen is stored as a grid of
///     tile sheet indices, so the index painted at a position is the only thing identifying what
///     Blob can interact with there.
///     <para>
///         These are all real tiles on the sheet. Positions the author marks that are never drawn
///         are <see cref="MarkerTile" /> instead.
///     </para>
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
    Spike3 = 416,
    ShuttleTag2 = 418,
    ShuttleTag3 = 417,
    BreakableBridges = 410,

    /// <summary>
    ///     The teleporter booth. Walking into one from the left switches to the teleporter screen.
    /// </summary>
    TeleportBooth = 397,

    /// <summary>
    ///     The signpost that shows a teleport code. The artwork is a blank board and the game writes
    ///     the code across it, so it needs recognising by number even though walking into it does
    ///     nothing. See <see cref="Starquake.Structure.Signposts" />.
    /// </summary>
    TeleportSignpost = 364,

    /// <summary>
    ///     The two halves of an access booth. Separate tiles because which one Blob walks into
    ///     decides which side of the booth Blob appears on.
    /// </summary>
    AccessBoothLeft = 399,

    /// <inheritdoc cref="AccessBoothLeft" />
    AccessBoothRight = 400,

    /// <summary>
    ///     The foot of a tunnel lift, and the only tile on the sheet with nothing drawn on it. Every
    ///     cell is solid, so it acts as an invisible wall. Not a <see cref="MarkerTile" />, because a
    ///     marker is passable as well as invisible.
    /// </summary>
    TunnelLiftTile = 408,

    /// <summary>
    ///     The body of a tunnel lift, the tiles that carry Blob upwards from underneath.
    ///     <see cref="TunnelLiftTile" /> is one of these too.
    /// </summary>
    TunnelLiftShaft1 = 409,

    /// <inheritdoc cref="TunnelLiftShaft1" />
    TunnelLiftShaft2 = 412,

    /// <inheritdoc cref="TunnelLiftShaft1" />
    TunnelLiftShaft3 = 413,

    /// <summary>
    ///     The top of a tunnel lift. Reaching one ends the ride.
    ///     <see cref="TunnelLiftTopAlternate" /> exists only in the redrawn artwork, so the original
    ///     graphics never look for it.
    /// </summary>
    TunnelLiftTop1 = 407,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTopAlternate = 396,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTop2 = 414,

    /// <inheritdoc cref="TunnelLiftTop1" />
    TunnelLiftTop3 = 415,

    /// <summary>
    ///     The force field across an access booth. Blob can walk through it but cannot use the
    ///     inventory while standing in one, which stops a key being produced inside the booth it is
    ///     meant to open.
    /// </summary>
    AccessForceField = 419
}
