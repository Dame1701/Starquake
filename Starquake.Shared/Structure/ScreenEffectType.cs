// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Structure;

internal enum ScreenEffectType
{
    AdditiveFade,
    LeftTunnelEntry,
    RightTunnelEntry,

    /// <summary>
    ///     The room settling after Blob has arrived somewhere by teleport.
    /// </summary>
    TeleportArrival,

    /// <summary>
    ///     The room being wrung round a slot after a part has gone into the planet's core.
    /// </summary>
    CoreSurge,

    /// <summary>
    ///     Olly's screen, flooded. The only effect that does not end on its own.
    /// </summary>
    Water
}
