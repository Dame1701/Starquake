// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

/// <inheritdoc cref="TunnelEntryEffect" />
public class RightTunnelEntryEffect : TunnelEntryEffect
{
    public RightTunnelEntryEffect()
    {
        ScreenEffectType = ScreenEffectType.RightTunnelEntry;
    }

    /// <inheritdoc cref="LeftTunnelEntryEffect.SqueezesTowardsTheLeftEdge" />
    protected override bool SqueezesTowardsTheLeftEdge => false;
}
