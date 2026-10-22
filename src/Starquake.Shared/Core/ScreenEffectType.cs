// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Core;

internal enum ScreenEffectType
{
    LeftTunnelEntry,
    RightTunnelEntry,

    /// <summary>The room settling after Blob arrives somewhere by teleport.</summary>
    TeleportArrival,

    /// <summary>The room wrung round a slot after a part goes into the planet's core.</summary>
    CoreSurge,

    /// <summary>Olly's screen, flooded. The only effect that does not end on its own.</summary>
    Water
}
