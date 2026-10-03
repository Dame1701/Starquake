// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

/// <inheritdoc cref="TunnelEntryEffect" />
internal class LeftTunnelEntryEffect : TunnelEntryEffect
{
    public LeftTunnelEntryEffect(ScreenSnapshot outgoingScreen, PresentationSettings presentation)
        : base(outgoingScreen, presentation)
    {
        ScreenEffectType = ScreenEffectType.LeftTunnelEntry;
    }

    /// <summary>
    ///     Leaving to the left, so the picture collapses that way and the next screen expands from
    ///     its right edge - which is the side Blob walks in on.
    /// </summary>
    protected override bool SqueezesTowardsTheLeftEdge => true;
}
