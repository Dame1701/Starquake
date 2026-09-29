// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Display;
using Starquake.Engines;
using Starquake.SharedLibs;
using Starquake.Sprites;

namespace Starquake.ActiveSprites;

internal class JumpPlatform
{

    private readonly GameEngine _gameEngine;

    private readonly SpriteCollection _jumpPlatform;
    private readonly SpriteCollection _jumpPlatformOriginal;
    private readonly SpriteBatch _textureDrawer;

    private int _dissolveTime;

    private SpritePixelMatrix _matrix;

    private RenderTarget2D _renderTarget;

    public JumpPlatform(GameEngine gameEngineRef, ContentManager content, Point position)
    {
        _textureDrawer = new SpriteBatch(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice);
        _gameEngine = gameEngineRef;

        _jumpPlatform = gameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatforms").Clone(_gameEngine);
        _jumpPlatformOriginal = gameEngineRef.ContentResourceManager.GetSpriteCollection("JumpPlatformsOriginal").Clone(_gameEngine);

        XPos = position.X;
        YPos = position.Y;

        Reset();
    }

    public CollisionTile LeftCollisionTile { get; set; }

    public CollisionTile RightCollisionTile { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    public bool PlatformActive { get; set; }

    public bool Finished => _dissolveTime == 0;

    public bool ReadyToAllocate { get; set; }

    public static Point GetJumpPlatformCoords(int blobXPos, int blobYPos, GameScreenCollection gsc)
    {
        var returnValue = new Point();
        returnValue.X = blobXPos;
        var positionOnScreen = blobYPos;
        returnValue.Y = (positionOnScreen / 32 + 1) * 32;

        return returnValue;
    }

    public void Reset()
    {
        _matrix = new SpritePixelMatrix(_jumpPlatform.SpriteWidth * _jumpPlatform.SpriteHeight);
        _renderTarget = new RenderTarget2D(GameEngine.GetInstance().GraphicsDeviceMgr.GraphicsDevice, _jumpPlatform.SpriteWidth, _jumpPlatform.SpriteHeight);
        PlatformActive = false;
        ReadyToAllocate = true;
        _dissolveTime = 100;
    }

    public void Update()
    {
        if (PlatformActive)
        {
            // Only disintegrate after a while
            if (_dissolveTime < 50)
            {
                for (var a = 0; a < 50; a++)
                {
                    _matrix.SetRandomPixel(Color.Transparent);
                }
            }

            if (_dissolveTime > 0)
            {
                _dissolveTime--;
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (PlatformActive)
        {
            // The render target is a texture that we draw directly to
            _gameEngine.GraphicsDeviceMgr.GraphicsDevice.SetRenderTarget(_renderTarget);
            _gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(Color.Transparent);

            if (_gameEngine.UsingOldGraphics)
            {
                _jumpPlatformOriginal.Begin(BlendState.AlphaBlend, gameTime);
                _jumpPlatformOriginal.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                _jumpPlatformOriginal.End();
            }
            else
            {
                _jumpPlatform.Begin(BlendState.AlphaBlend, gameTime);
                _jumpPlatform.DrawSprite(0, 0, 0, Color.White, _matrix.Matrix);
                _jumpPlatform.End();
            }


            // Set the render targer back to the screen
            _gameEngine.ScreenEffectEngine.SelectRenderViewPort("GameArea", false);

            if (_dissolveTime > 0)
            {
                _textureDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

                _textureDrawer.Draw(
                    _renderTarget,
                    new Rectangle(XPos + _jumpPlatformOriginal.SpriteWidth / 2, YPos + _jumpPlatformOriginal.SpriteHeight / 2,
                        _jumpPlatformOriginal.SpriteWidth,
                        _jumpPlatformOriginal.SpriteHeight),
                    null,
                    Color.White,
                    0,
                    new Vector2(_jumpPlatformOriginal.SpriteWidth / 2, _jumpPlatformOriginal.SpriteHeight / 2),
                    SpriteEffects.None, 0);

                _textureDrawer.End();
            }
        }
    }
}
