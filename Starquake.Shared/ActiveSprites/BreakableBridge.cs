// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ContentManagement;
using Starquake.Engines.ScreenEffects;
using Starquake.Sprites;

namespace Starquake.ActiveSprites;

internal class BreakableBridge
{

    private readonly SpriteCollection _breakableBridge;
    private readonly SpriteCollection _breakableBridgeOriginal;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly SpritePixelMatrix _matrix;
    private readonly RenderTarget2D _renderTarget;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly SpriteBatch _textureDrawer;

    private readonly int _xPos;
    private readonly int _yPos;

    public BreakableBridge(
        int xPos,
        int yPos,
        ResourceManager resources,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        ScreenEffectEngine screenEffects)
    {
        _graphicsDeviceManager = graphics;
        _graphicsSet = graphicsSet;
        _screenEffects = screenEffects;

        _breakableBridge = resources.GetSpriteCollection("BreakableBridge");
        _breakableBridgeOriginal = resources.GetSpriteCollection("BreakableBridgeOriginal");

        _textureDrawer = new SpriteBatch(graphics.GraphicsDevice);
        _matrix = new SpritePixelMatrix(_breakableBridge.SpriteWidth * _breakableBridge.SpriteHeight, randomNumberGenerator);
        _renderTarget = new RenderTarget2D(graphics.GraphicsDevice, _breakableBridge.SpriteWidth, _breakableBridge.SpriteHeight);

        _xPos = xPos;
        _yPos = yPos;
    }

    public bool Finished => _matrix.Finished;

    public void Update()
    {
        for (int a = 0; a < 200; a++)
        {
            _matrix.SetRandomPixel(Color.Transparent);
        }
    }

    public void Draw()
    {
        if (!Finished)
        {
            // The render target is a texture that we draw directly to
            _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(_renderTarget);
            _graphicsDeviceManager.GraphicsDevice.Clear(Color.Transparent);

            if (_graphicsSet.IsOriginal)
            {
                _breakableBridgeOriginal.Begin(BlendState.AlphaBlend);
                _breakableBridgeOriginal.DrawSprite(0, 0, 0, _matrix.Matrix);
                _breakableBridgeOriginal.End();
            }
            else
            {
                _breakableBridge.Begin(BlendState.AlphaBlend);
                _breakableBridge.DrawSprite(0, 0, 0, _matrix.Matrix);
                _breakableBridge.End();
            }

            // Set the render targer back to the screen
            _screenEffects.SelectRenderViewPort("GameArea", false);

            _textureDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
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
