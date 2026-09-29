// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Engines;

public abstract class ScreenEffectBase
{
    protected int EffectDuration;
    protected int EffectSpeed;

    protected RenderViewPort EffectSurface;
    protected GameEngine GameEngine;

    public ScreenEffectBase(GameEngine gameEngineReference)
    {
        GameEngine = gameEngineReference;
    }
    public ScreenEffectType ScreenEffectType { get; protected set; }
    public bool EffectFinished { get; protected set; } = true;

    public virtual void TriggerEffect(RenderViewPort effectSurface, int effectSpeed, int effectDuration)
    {
        EffectSpeed = effectSpeed;
        EffectDuration = effectDuration;
        EffectFinished = false;
        EffectSurface = effectSurface;
    }

    public abstract void Reset();

    public abstract void Update();

    // If this returns true the we want to use the default renderer
    public virtual bool Draw(SpriteBatch renderer, RenderViewPort renderViewPortToUse)
        => true;
}

public class LeftTunnelEntryEffect : ScreenEffectBase
{
    private int _effectState;

    public LeftTunnelEntryEffect(GameEngine gameEngineReference) : base(gameEngineReference)
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

public class RightTunnelEntryEffect : ScreenEffectBase
{
    private int _effectState;

    public RightTunnelEntryEffect(GameEngine gameEngineReference) : base(gameEngineReference)
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

public class AddictiveFade : ScreenEffectBase
{
    private int _effectDurationCounter;

    public AddictiveFade(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenEffectType = ScreenEffectType.AddictiveFade;
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
            GameEngine.PresentationTransform);

        for (var a = 0; a < 10; a++)
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