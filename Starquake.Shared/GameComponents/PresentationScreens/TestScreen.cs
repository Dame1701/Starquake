// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.ActiveSprites;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;
// This will be used for both blob and the nastie's death routines - except for the spiky bugger which will still use the explosion with some bits
// Nasties will have a new mercury spat effect or just some smoke to enhance the death ray

// Maybe add an addictive yellow and red effect to blobs sprite to simulate fire....?
// Need to draw blob's components separately so we can explode them....
public class TestScreen : GameScreenBase, IGameScreen
{

    #region IGameScreen Members

    private bool _keyDown;
    private readonly BlobDeathAnimation _bda;
    public bool Success => true;

    public TestScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 0;

        _bda = new BlobDeathAnimation(gameEngineReference);
    }

    public override void Update(GameTime gameTime)
    {
        GameEngine.GameScreens.ScreenNumber = 3;

        if (Keyboard.GetState().IsKeyDown(Keys.F) && !_keyDown)
        {
            _keyDown = true;
        }

        if (Keyboard.GetState().IsKeyUp(Keys.F) && _keyDown)
        {
            _bda.Trigger(300, 250, true);

            _keyDown = false;
        }

        if (_bda.Triggered)
        {
            for (var a = 0; a < _bda.BouncingParticles.Count; a++)
            {
                if (_bda.BouncingParticles[a].YPos > 700)
                {
                    _bda.BouncingParticles[a].YBounce();
                }

                if (_bda.BouncingParticles[a].YPos < 500)
                {
                    _bda.BouncingParticles[a].YBounce();
                }

                if (_bda.BouncingParticles[a].XPos < 400)
                {
                    _bda.BouncingParticles[a].XBounce();
                }

                if (_bda.BouncingParticles[a].XPos > 800)
                {
                    _bda.BouncingParticles[a].XBounce();
                }
            }
        }

        _bda.Update(gameTime);
    }

    void IGameScreen.Reset(object data)
    {

    }

    public override void Draw(GameTime gameTime)
    {
        GameEngine.ScreenEffctEngine.SelectRenderViewPort("EntireScreen");

        var triggered = _bda.Triggered;

        if (_keyDown && !triggered)
        {
            // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None, gameTime);
            // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").DrawSprite(0, 600, 500);
            // gameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").End();
        }


        GameEngine.GameScreens.Draw(Color.White, gameTime);

        if (triggered)
        {
            _bda.Draw(gameTime);
        }
        else
        {
            GameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").DrawSprite(0, 300, 250);
            GameEngine.ContentResourceManager.GetSpriteCollection("BlobSprites").End();
        }

        GameEngine.Font1.Begin(BlendState.Opaque);
        //gameEngine.Font1.WriteText(10,10, bp.XPos.ToString(), Color.White, 255);
        //gameEngine.Font1.WriteText(100, 10, bp.YPos.ToString(), Color.White, 255);
        GameEngine.Font1.End();
    }

    void IGameScreen.PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {

    }

    #endregion

}