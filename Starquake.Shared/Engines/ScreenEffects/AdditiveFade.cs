// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.Engines.ScreenEffects;

public class AdditiveFade : ScreenEffectBase
{
    private readonly PresentationSettings _presentation;

    private int _effectDurationCounter;

    public AdditiveFade(PresentationSettings presentation)
    {
        _presentation = presentation;

        ScreenEffectType = ScreenEffectType.AdditiveFade;
    }

    public override void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        base.TriggerEffect(effectSurface, -1, effectDuration);
        _effectDurationCounter = 0;
    }

    public override void Update()
    {
        if (EffectSurface != null)
        {
            if (!EffectFinished)
            {
                if (_effectDurationCounter >= EffectDuration)
                {
                    EffectFinished = true;
                }

                _effectDurationCounter++;
            }
        }
    }

    // We're going to override the default renderer
    public override bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
    {
        renderer.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null,
            _presentation.Transform);

        for (int a = 0; a < 10; a++)
        {
            renderer.Draw(
                renderViewPortToUse.RenderTarget,
                new Rectangle(
                    renderViewPortToUse.XPos,
                    renderViewPortToUse.YPos,
                    renderViewPortToUse.Width,
                    renderViewPortToUse.Height),
                new Rectangle(
                    0,
                    0,
                    renderViewPortToUse.RenderTarget.Width,
                    renderViewPortToUse.RenderTarget.Height
                ),
                Color.White,
                0f,
                new Vector2(0, 0),
                SpriteEffects.None,
                0f);
        }

        renderer.End();

        // No need to run the default renderer
        return false;
    }

    public override void Reset()
    {
        EffectFinished = true;
    }
}
