// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

public class RightTunnelEntryEffect : ScreenEffectBase
{
    private int _effectState;

    public RightTunnelEntryEffect()
    {
        ScreenEffectType = ScreenEffectType.RightTunnelEntry;
    }

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, effectSpeed, -1);
        _effectState = 0;
    }

    public override void Update()
    {
        if (EffectSurface != null)
        {
            if (!EffectFinished)
            {
                if (_effectState == 0)
                {
                    if (EffectSurface.Width > 0)
                    {
                        EffectSurface.Width = EffectSurface.Width - EffectSpeed;
                        EffectSurface.XPos = EffectSurface.MasterXPos + EffectSurface.MasterWidth - EffectSurface.Width;
                    }

                    if (EffectSurface.Width <= 0)
                    {
                        _effectState = 1;
                        EffectSurface.XPos = EffectSurface.MasterXPos;
                        EffectSurface.Width = 0;
                    }
                }
                else if (_effectState == 1)
                {
                    if (EffectSurface.Width < EffectSurface.MasterWidth)
                    {
                        EffectSurface.Width = EffectSurface.Width + EffectSpeed;
                    }

                    if (EffectSurface.Width >= EffectSurface.MasterWidth)
                    {
                        _effectState = 2;
                        EffectSurface.Width = EffectSurface.MasterWidth;

                        EffectFinished = true;
                    }
                }
            }
        }
    }

    public override void Reset()
    {
        EffectFinished = true;
        _effectState = 0;
    }
}