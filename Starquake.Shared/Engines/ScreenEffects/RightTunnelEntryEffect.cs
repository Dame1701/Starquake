// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

/// <inheritdoc cref="TunnelEntryEffect" />
internal class RightTunnelEntryEffect : TunnelEntryEffect
{
    public RightTunnelEntryEffect(ScreenSnapshot outgoingScreen, PresentationSettings presentation,
        ResourceManager resources)
        : base(outgoingScreen, presentation, resources)
    {
        ScreenEffectType = ScreenEffectType.RightTunnelEntry;
    }

    /// <summary>
    ///     Leaving to the right, so the picture collapses that way and the next screen expands from
    ///     its left edge - which is the side Blob walks in on.
    /// </summary>
    protected override bool SqueezesTowardsTheLeftEdge => false;
}
