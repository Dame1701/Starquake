// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Sprites;

namespace Starquake.ActiveSprites;

public class BreakableBridge
{

    private readonly SpriteCollection _breakableBridge;
    private readonly SpriteCollection _breakableBridgeOriginal;
    private readonly GameEngine _gameEngineReference;
    private readonly SpritePixelMatrix _matrix;
    private readonly RenderTarget2D _renderTarget;
    private readonly SpriteBatch _textureDrawer;

    private readonly int _xPos;
    private readonly int _yPos;

    public BreakableBridge(int xPos, int yPos, GameEngine gameEngineReference)
    {
        _gameEngineReference = gameEngineReference;

        _breakableBridge = _gameEngineReference.ContentResourceManager.GetSpriteCollection("BreakableBridge");
        _breakableBridgeOriginal = _gameEngineReference.ContentResourceManager.GetSpriteCollection("BreakableBridgeOriginal");

        _textureDrawer = new SpriteBatch(gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
        _matrix = new SpritePixelMatrix(_breakableBridge.SpriteWidth * _breakableBridge.SpriteHeight, gameEngineReference.RandomNumberGenerator);
        _renderTarget = new RenderTarget2D(gameEngineReference.GraphicsDeviceMgr.GraphicsDevice, _breakableBridge.SpriteWidth, _breakableBridge.SpriteHeight);

        _xPos = xPos;
        _yPos = yPos;
    }

    public bool Finished => _matrix.Finished;

    public void Update()
    {
        for (var a = 0; a < 200; a++)
        {
            _matrix.SetRandomPixel(Color.Transparent);
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (!Finished)
        {
            // The render target is a texture that we draw directly to
            _gameEngineReference.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(_renderTarget);
            _gameEngineReference.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

            if (_gameEngineReference.UsingOldGraphics)
            {
                _breakableBridgeOriginal.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                _breakableBridgeOriginal.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                _breakableBridgeOriginal.End();
            }
            else
            {
                _breakableBridge.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                _breakableBridge.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                _breakableBridge.End();
            }

            // Set the render targer back to the screen
            _gameEngineReference.ScreenEffectEngine.SelectRenderViewPort("GameArea", false);

            _textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            _textureDrawer.Draw(
                _renderTarget,
                new Rectangle(_xPos + _breakableBridge.SpriteWidth / 2, _yPos + _breakableBridge.SpriteHeight / 2,
                    _breakableBridge.SpriteWidth,
                    _breakableBridge.SpriteHeight),
                null,
                Color.White,
                0,
                new Vector2(_breakableBridge.SpriteWidth / 2, _breakableBridge.SpriteHeight / 2),
                SpriteEffects.None, 0);

            _textureDrawer.End();
        }
    }
}