using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace StarquakeXNA.GameComponents
{
    // This will be used for both blob and the nastie's death routines - except for the spiky bugger which will still use the explosion with some bits
    // Nasties will have a new mercury spat effect or just some smoke to enhance the death ray

    // Maybe add an addictive yellow and red effect to blobs sprite to simulate fire....?
    // Need to draw blob's components separately so we can explode them....
    public class TestScreen : GameScreenBase, IGameScreen
    {
        #region IGameScreen Members
        bool keyDown = false;
        BlobDeathAnimation bda;
        public bool Success
        {
            get { return true; }
        }

        public TestScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 0;

            bda = new BlobDeathAnimation(GameEngineReference);
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            gameEngine.GameScreens.ScreenNumber = 3;

            if (Keyboard.GetState().IsKeyDown(Keys.F) && !keyDown)
            {
                keyDown = true;
            }

            if (Keyboard.GetState().IsKeyUp(Keys.F) && keyDown)
            {
                bda.Trigger(300, 250, true);

                keyDown = false;
            }

            if (bda.Triggered)
            {
                for (int a = 0; a < bda.BouncingParticles.Count; a++)
                {
                    if (bda.BouncingParticles[a].YPos > 700)
                    {
                        bda.BouncingParticles[a].YBounce();
                    }

                    if (bda.BouncingParticles[a].YPos < 500)
                    {
                        bda.BouncingParticles[a].YBounce();
                    }

                    if (bda.BouncingParticles[a].XPos < 400)
                    {
                        bda.BouncingParticles[a].XBounce();
                    }

                    if (bda.BouncingParticles[a].XPos > 800)
                    {
                        bda.BouncingParticles[a].XBounce();
                    }
                }
            }

            bda.Update(gameTime);
        }

        void IGameScreen.Reset(object Data)
        {

        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            gameEngine.ScreenEffctEngine.SelectRenderViewPort("EntireScreen");

            bool triggered = bda.Triggered;

            if (keyDown && !triggered)
            {
                // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None, gameTime);
                // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").DrawSprite(0, 600, 500);
                // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").End();
            }


            gameEngine.GameScreens.Draw(Color.White, gameTime);

            if (triggered)
            {
                bda.Draw(gameTime);
            }
            else
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").DrawSprite(0, 300, 250);
                gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").End();
            }

            gameEngine.Font1.Begin(BlendState.Opaque);
            //gameEngine.Font1.WriteText(10,10, bp.XPos.ToString(), Color.White, 255);
            //gameEngine.Font1.WriteText(100, 10, bp.YPos.ToString(), Color.White, 255);
            gameEngine.Font1.End();
        }

        void IGameScreen.PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {

        }

        #endregion
    }
}
