// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

public class LeftTunnelEntryEffect : ScreenEffectBase
{
    private int _effectState;

    public LeftTunnelEntryEffect()
    {
        ScreenEffectType = ScreenEffectType.LeftTunnelEntry;
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
                    }

                    if (EffectSurface.Width <= 0)
                    {
                        _effectState = 1;
                        EffectSurface.Width = 0;
                    }
                }
                else if (_effectState == 1)
                {
                    if (EffectSurface.Width < EffectSurface.MasterWidth)
                    {
                        EffectSurface.Width = EffectSurface.Width + EffectSpeed;
                        EffectSurface.XPos = EffectSurface.MasterXPos + EffectSurface.MasterWidth - EffectSurface.Width;
                    }

                    if (EffectSurface.Width >= EffectSurface.MasterWidth)
                    {
                        _effectState = 2;

                        // The opening step overshoots whenever the speed does not divide the width,
                        // and the frame that overshoots has already pushed XPos past MasterXPos by
                        // the overshoot. Clamping the width without putting the position back leaves
                        // the play area sitting that far to the left for good.
                        EffectSurface.Width = EffectSurface.MasterWidth;
                        EffectSurface.XPos = EffectSurface.MasterXPos;

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
