// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines.ScreenEffects;
using Starquake.SharedLibs;
using Starquake.Sprites;

namespace Starquake.ActiveSprites;

internal class JumpPlatform
{
    /// <summary>
    ///     How long a platform lasts, counted down in logic frames, and the point in that life at
    ///     which it starts coming apart - half way, so it stands whole long enough to be used.
    /// </summary>
    private const int PlatformLifetime = 100;

    /// <inheritdoc cref="PlatformLifetime" />
    private const int DissolveStartsAt = PlatformLifetime / 2;

    /// <summary>
    ///     How many pixels are rubbed out each frame once it starts going.
    /// </summary>
    private const int PixelsDissolvedPerFrame = 50;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;

    private readonly SpriteCollection _jumpPlatform;
    private readonly SpriteCollection _jumpPlatformOriginal;
    private readonly Random _randomNumberGenerator;

    private readonly RenderTarget2D _renderTarget;
    private readonly ScreenEffectEngine _screenEffects;
    private readonly SpriteBatch _textureDrawer;

    private int _dissolveTime;

    private SpritePixelMatrix _matrix;

    public JumpPlatform(
        ResourceManager resources,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        ScreenEffectEngine screenEffects,
        Point position)
    {
        _textureDrawer = new SpriteBatch(graphics.GraphicsDevice);
        _graphicsDeviceManager = graphics;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;
        _screenEffects = screenEffects;

        _jumpPlatform = resources.GetSpriteCollection(Assets.Graphics.JumpPlatforms).Clone();
        _jumpPlatformOriginal = resources.GetSpriteCollection(Assets.Graphics.JumpPlatformsOriginal).Clone();

        XPos = position.X;
        YPos = position.Y;

        // The platform is always drawn at one sprite's size, so the target it is composed
        // onto never has to change; only the pixel matrix eaten away by the dissolve does
        _renderTarget = new RenderTarget2D(graphics.GraphicsDevice, _jumpPlatform.SpriteWidth, _jumpPlatform.SpriteHeight);

        // A platform that has not been allocated to a screen still sits at a position, and
        // JumpPlatforms replaces both tiles every time it hands this platform out again
        LeftCollisionTile = CollisionTile.ForObject(CollisionObjectType.JumpPlatform, XPos, YPos, CollisionTileType.EveryoneHits);
        RightCollisionTile = CollisionTile.ForObject(CollisionObjectType.JumpPlatform, XPos + 32, YPos, CollisionTileType.EveryoneHits);

        Reset();
    }

    public CollisionTile LeftCollisionTile { get; set; }

    public CollisionTile RightCollisionTile { get; set; }

    public int XPos { get; set; }

    public int YPos { get; set; }

    /// <summary>
    ///     Which screen this platform was laid on. Its cells belong to that screen's collision map
    ///     and no other, so it has to be remembered rather than taken as wherever Blob is now -
    ///     he can have crossed to the next screen while the platform is still dissolving.
    /// </summary>
    public int ScreenNumber { get; set; }

    public bool PlatformActive { get; set; }

    public bool Finished => _dissolveTime == 0;

    public bool ReadyToAllocate { get; set; }

    public static Point GetJumpPlatformCoords(int blobXPos, int blobYPos)
    {
        var returnValue = new Point();
        returnValue.X = blobXPos;
        int positionOnScreen = blobYPos;
        returnValue.Y = (positionOnScreen / 32 + 1) * 32;

        return returnValue;
    }

    [MemberNotNull(nameof(_matrix))]
    public void Reset()
    {
        _matrix = new SpritePixelMatrix(_jumpPlatform.SpriteWidth * _jumpPlatform.SpriteHeight, _randomNumberGenerator);
        PlatformActive = false;
        ReadyToAllocate = true;
        _dissolveTime = PlatformLifetime;
    }

    public void Update()
    {
        if (PlatformActive)
        {
            // It stands whole for the first half of its life and only then starts coming apart, so
            // Blob has a moment to use it before it is visibly going.
            if (_dissolveTime < DissolveStartsAt)
            {
                for (int a = 0; a < PixelsDissolvedPerFrame; a++)
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

    public void Draw()
    {
        if (!PlatformActive)
        {
            return;
        }

        ComposeDissolvingPlatform();

        _screenEffects.SelectRenderViewPort(ViewPorts.GameArea, false);

        if (_dissolveTime > 0)
        {
            DrawComposedPlatform();
        }
    }

    /// <summary>
    ///     Draws the platform into a render target of its own, with the pixels the dissolve has eaten
    ///     left transparent. A render target is a texture rather than a place on the screen, so the
    ///     sprite goes down at the origin and is put in position when the texture itself is drawn.
    /// </summary>
    private void ComposeDissolvingPlatform()
    {
        _graphicsDeviceManager.GraphicsDevice.SetRenderTarget(_renderTarget);
        _graphicsDeviceManager.GraphicsDevice.Clear(Color.Transparent);

        SpriteCollection platform = _graphicsSet.IsOriginal ? _jumpPlatformOriginal : _jumpPlatform;

        platform.Begin(BlendState.AlphaBlend);
        platform.DrawSprite(0, 0, 0, _matrix.Matrix);
        platform.End();
    }

    /// <summary>
    ///     Puts the composed texture on the screen, rotated about its own centre - which is why the
    ///     position is offset by half the sprite as well as the origin being set to it.
    /// </summary>
    private void DrawComposedPlatform()
    {
        _textureDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

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
