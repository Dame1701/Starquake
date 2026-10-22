// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.ContentManagement;
using Starquake.Core;

namespace Starquake.Engines.ScreenEffects;

/// <inheritdoc cref="TunnelEntryEffect" />
internal class LeftTunnelEntryEffect : TunnelEntryEffect
{
    public LeftTunnelEntryEffect(ScreenSnapshot outgoingScreen, PresentationSettings presentation,
        ResourceManager resources)
        : base(outgoingScreen, presentation, resources)
    {
        ScreenEffectType = ScreenEffectType.LeftTunnelEntry;
    }

    /// <summary>
    ///     Leaving to the left, so the picture collapses that way and the next screen expands from its
    ///     right edge, which is the side Blob walks in on.
    /// </summary>
    protected override bool SqueezesTowardsTheLeftEdge => true;
}
