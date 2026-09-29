using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarquakeXNA.Structure;

namespace StarquakeXNA
{
    public abstract class ScreenEffectBase
    {
        protected enScreenEffectType screenEffectType;
        public enScreenEffectType ScreenEffectType
        {
            get { return screenEffectType; }
        }

        protected RenderViewPort effectSurface = null;
        protected GameEngine gameEngine = null;
        protected bool effectFinished = true;
        public bool EffectFinished
        {
            get { return effectFinished; }
        }
        protected int effectDuration = 0;
        protected int effectSpeed = 0;

        public ScreenEffectBase(GameEngine GameEngineReference)
        {
            gameEngine = GameEngineReference;
        }

        public virtual void TriggerEffect(RenderViewPort EffectSurface, int EffectSpeed, int EffectDuration)
        {
            effectSpeed = EffectSpeed;
            effectDuration = EffectDuration;
            effectFinished = false;
            effectSurface = EffectSurface;
        }

        public abstract void Reset();

        public abstract void Update();

        // If this returns true the we want to use the default renderer
        public virtual bool Draw(SpriteBatch Renderer, RenderViewPort RenderViewPortToUse)
        {
            return true;
        }
    }

    public class LeftTunnelEntryEffect : ScreenEffectBase
    {
        private int effectState = 0;

        public LeftTunnelEntryEffect(GameEngine GameEngineReference) : base(GameEngineReference)
        {
            screenEffectType = enScreenEffectType.LeftTunnelEntry;
        }

        public override void TriggerEffect(RenderViewPort EffectSurface, int EffectSpeed, int EffectDuration)
        {
            base.TriggerEffect(EffectSurface, EffectSpeed, -1);
            effectState = 0;
        }

        public override void Update()
        {
            if (effectSurface !=null)
            {
                if (effectFinished == false)
                {
                    if (effectState == 0)
                    {
                        if (effectSurface.Width > 0)
                        {
                            effectSurface.Width = effectSurface.Width - effectSpeed;
                        }
                        
                        if (effectSurface.Width <= 0)
                        {
                            effectState = 1;
                            effectSurface.Width = 0;
                        }                        
                    }
                    else if (effectState == 1)
                    {              
                        if (effectSurface.Width < effectSurface.MasterWidth)
                        {
                            effectSurface.Width = effectSurface.Width + effectSpeed;
                            effectSurface.XPos = (effectSurface.MasterXPos + effectSurface.MasterWidth) - effectSurface.Width;
                        }

                        if (effectSurface.Width >= effectSurface.MasterWidth)
                        {
                            effectState = 2;
                            effectSurface.Width = effectSurface.MasterWidth;
                            
                            effectFinished = true;
                        }
                    }
                }
            }
        }

        public override void Reset()
        {
            effectFinished = true;
            effectState = 0;
        }
    }

    public class RightTunnelEntryEffect : ScreenEffectBase
    {
        private int effectState = 0;

        public RightTunnelEntryEffect(GameEngine GameEngineReference) : base(GameEngineReference)
        {
            screenEffectType = enScreenEffectType.RightTunnelEntry;
        }

        public override void TriggerEffect(RenderViewPort EffectSurface, int EffectSpeed, int EffectDuration)
        {
            base.TriggerEffect(EffectSurface, EffectSpeed, -1);
            effectState = 0;
        }

        public override void Update()
        {
            if (effectSurface != null)
            {
                if (effectFinished == false)
                {
                    if (effectState == 0)
                    {
                        if (effectSurface.Width > 0)
                        {
                            effectSurface.Width = effectSurface.Width - effectSpeed;
                            effectSurface.XPos = (effectSurface.MasterXPos + effectSurface.MasterWidth) - effectSurface.Width;
                        }

                        if (effectSurface.Width <= 0)
                        {
                            effectState = 1;
                            effectSurface.XPos = effectSurface.MasterXPos;
                            effectSurface.Width = 0;
                        }
                    }
                    else if (effectState == 1)
                    {
                        if (effectSurface.Width < effectSurface.MasterWidth)
                        {
                            effectSurface.Width = effectSurface.Width + effectSpeed;
                        }

                        if (effectSurface.Width >= effectSurface.MasterWidth)
                        {
                            effectState = 2;
                            effectSurface.Width = effectSurface.MasterWidth;

                            effectFinished = true;
                        }
                    }
                }
            }
        }

        public override void Reset()
        {
            effectFinished = true;
            effectState = 0;
        }
    }

    public class AddictiveFade : ScreenEffectBase
    {
        private int effectDurationCounter = 0;

        public AddictiveFade(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenEffectType = enScreenEffectType.AddictiveFade;
        }

        public override void TriggerEffect(RenderViewPort EffectSurface, int EffectSpeed, int EffectDuration)
        {
            base.TriggerEffect(EffectSurface, -1, EffectDuration);
            effectDurationCounter = 0;
        }

        public override void Update()
        {
            if (effectSurface != null)
            {
                if (effectFinished == false)
                {
                    if (effectDurationCounter >= effectDuration)
                    {
                        effectFinished = true;
                    }

                    effectDurationCounter++;
                }
            }
        }

        // We're going to override the default renderer
        public override bool Draw(SpriteBatch Renderer, RenderViewPort RenderViewPortToUse)
        {
            Renderer.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null,
                gameEngine.PresentationTransform);

            for (int a = 0; a < 10; a++)
            {
                Renderer.Draw(
                    RenderViewPortToUse.RenderTarget,
                    new Rectangle(
                        RenderViewPortToUse.XPos,
                        RenderViewPortToUse.YPos,
                        RenderViewPortToUse.Width,
                        RenderViewPortToUse.Height),
                    new Rectangle(
                        0,
                        0,
                        RenderViewPortToUse.RenderTarget.Width,
                        RenderViewPortToUse.RenderTarget.Height
                        ),
                        Color.White,
                        0f,
                        new Vector2(0, 0),
                        SpriteEffects.None,
                        0f);
            }

            Renderer.End();

            // No need to run the default renderer
            return false;
        }

        public override void Reset()
        {
            effectFinished = true;
        }
    }
}
