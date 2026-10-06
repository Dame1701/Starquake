// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
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

        _breakableBridge = resources.GetSpriteCollection(Assets.Graphics.BreakableBridge);
        _breakableBridgeOriginal = resources.GetSpriteCollection(Assets.Graphics.BreakableBridgeOriginal);

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
        if (Finished)
        {
            return;
        }

        ComposeDissolvingBridge();

        _screenEffects.SelectRenderViewPort(ViewPorts.GameArea, false);

        DrawComposedBridge();
    }

    /// <summary>
    ///     Draws the bridge into a render target of its own, with the pixels the dissolve has eaten
    ///     left transparent. A render target is a texture rather than a place on the screen, so the
    ///     sprite goes down at the origin and is put in position when the texture itself is drawn.
    /// </summary>
    private void ComposeDissolvingBridge()
    {
        _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDeviceManager.GraphicsDevice.Clear(Color.Transparent);

        SpriteCollection bridge = _graphicsSet.IsOriginal ? _breakableBridgeOriginal : _breakableBridge;

        bridge.Begin(BlendState.AlphaBlend);
        bridge.DrawSprite(0, 0, 0, _matrix.Matrix);
        bridge.End();
    }

    /// <summary>
    ///     Puts the composed texture on the screen, rotated about its own centre - which is why the
    ///     position is offset by half the sprite as well as the origin being set to it.
    /// </summary>
    private void DrawComposedBridge()
    {
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
