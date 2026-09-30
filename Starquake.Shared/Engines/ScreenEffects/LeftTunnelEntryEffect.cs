// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

/// <inheritdoc cref="TunnelEntryEffect" />
public class LeftTunnelEntryEffect : TunnelEntryEffect
{
    public LeftTunnelEntryEffect()
    {
        ScreenEffectType = ScreenEffectType.LeftTunnelEntry;
    }

    /// <summary>
    ///     Leaving to the left, so the picture collapses that way and the next screen expands from
    ///     its right edge - which is the side the blob walks in on.
    /// </summary>
    protected override bool SqueezesTowardsTheLeftEdge => true;
}
