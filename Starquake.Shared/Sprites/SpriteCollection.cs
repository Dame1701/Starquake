// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Helpers;

namespace Starquake.Sprites;

internal class SpriteCollection
{
    private readonly ContentManager _contentManager;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;

    private readonly string _imageFilePath;

    private readonly Texture2D _spriteImages;

    private readonly int[] _spriteTexturePosX;

    private readonly int[] _spriteTexturePosY;

    private bool _clockWise = true;

    private byte _fade;

    private float _rotationSpeed = 1f;

    private SpriteBatch? _sprites;

    public SpriteCollection(GraphicsDeviceManager graphics, ContentManager content, string imageFilePath,
        int spriteWidth, int spriteHeight)
    {
        _imageFilePath = imageFilePath;
        _graphicsDeviceManager = graphics;
        _contentManager = content;

        SpriteWidth = spriteWidth;
        SpriteHeight = spriteHeight;

        _spriteImages = content.Load<Texture2D>(imageFilePath);

        int noHorizontalImages = _spriteImages.Width / spriteWidth;
        int noVerticalImages = _spriteImages.Height / spriteHeight;

        SpriteCount = noHorizontalImages * noVerticalImages;

        _spriteTexturePosX = new int[SpriteCount];
        _spriteTexturePosY = new int[SpriteCount];

        int counter = 0;
        _fade = ColourLevels.Full;

        // Generate Coordinates for the textures of the tiles found
        for (int y = 0; y < noVerticalImages; y++)
        {
            for (int x = 0; x < noHorizontalImages; x++)
            {
                _spriteTexturePosX[counter] = x * SpriteWidth;
                _spriteTexturePosY[counter] = y * SpriteHeight;
                counter++;
            }
        }
    }

    public float SpriteRotation
    {
        get;
        private set;
    }

    public int SpriteWidth
    {
        get;
    }

    public int SpriteHeight
    {
        get;
    }

    public int SpriteCount
    {
        get;
    }

    /// <param name="blendState">
    ///     Blend State for the device
    /// </param>
    /// <param name="transform">
    ///     Only wanted when drawing straight to the back buffer, where it scales and centres the
    ///     composition - see PresentationSettings.Transform. Drawing into a render target the
    ///     game composes at its own fixed size, so there it is left null.
    /// </param>
    /// <summary>
    ///     Opens a batch. Everything drawn until <see cref="End" /> is queued and issued in the order
    ///     the calls were made.
    /// </summary>
    /// <remarks>
    ///     There was a second overload taking a <see cref="SpriteSortMode" />, and 37 of its 39
    ///     callers asked for <c>BackToFront</c>. Nothing in the game sets a layer depth - every
    ///     <c>Draw</c> below passes nought - so that was a sort by a constant key, which costs a sort
    ///     and leaves the order among equal keys unspecified. The other two asked for
    ///     <c>Immediate</c>, which flushes a draw call per sprite. Neither was wanted, so neither can
    ///     be asked for.
    /// </remarks>
    public void Begin(BlendState blendState, Matrix? transform = null)
    {
        Sprites.Begin(SpriteSortMode.Deferred, blendState, null, null, null, null, transform);
    }

    public void DrawSpriteUnscaled(int spriteNumber, int xPosition, int yPosition, int fade)
    {
        Sprites.Draw(
            _spriteImages,
            new Vector2(xPosition, yPosition),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, fade),
            0f,
            new Vector2(0, 0),
            new Vector2(1, 1),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        Sprites.Draw(
            _spriteImages,
            new Vector2(xPosition * hScaling, yPosition * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, _fade),
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, SpritePixel[] matrix,
        int blockSize = 4, SpriteEffects spriteEffect = SpriteEffects.None)
    {
        if (matrix.Length == SpriteWidth * SpriteHeight)
        {
            const float hScaling = 1f;
            const float vScaling = 1f;

            int x = 0;
            int y = 0;

            int blockMultiplier = 1;
            if (blockSize > 1)
            {
                blockMultiplier = 2;
            }

            for (int a = 0; a < matrix.Length; a += blockSize * blockMultiplier)
            {
                if (matrix[a].IsOn)
                {
                    Sprites.Draw(
                        _spriteImages,
                        new Vector2((xPosition + x) * hScaling, (yPosition + y) * vScaling),
                        new Rectangle(_spriteTexturePosX[spriteNumber] + x, _spriteTexturePosY[spriteNumber] + y,
                            blockSize, blockSize),
                        matrix[a].Colour,
                        0f,
                        new Vector2(0, 0),
                        new Vector2(hScaling, vScaling),
                        spriteEffect,
                        0f);
                }

                if (x > SpriteWidth)
                {
                    x = 0;
                    y += blockSize;
                }
                else
                {
                    x += blockSize;
                }
            }
        }
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, int width, int height, Color spriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        Sprites.Draw(
            _spriteImages,
            new Vector2(xPosition * hScaling, yPosition * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], width, height),
            spriteColour,
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, Color spriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        Sprites.Draw(
            _spriteImages,
            new Vector2(xPosition * hScaling, yPosition * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0f);
    }

    public void DrawSpriteSubSection(int spriteNumber, int xPosition, int yPosition, Rectangle subSpriteArea,
        Color spriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        if (SpriteWidth >= subSpriteArea.Width &&
            SpriteHeight >= subSpriteArea.Height &&
            SpriteWidth >= subSpriteArea.Width + subSpriteArea.X &&
            SpriteHeight >= subSpriteArea.Height + subSpriteArea.Y)
        {
            Sprites.Draw(
                _spriteImages,
                new Vector2(xPosition * hScaling, yPosition * vScaling),
                new Rectangle(
                    _spriteTexturePosX[spriteNumber] + subSpriteArea.X,
                    _spriteTexturePosY[spriteNumber] + subSpriteArea.Y,
                    subSpriteArea.Width,
                    subSpriteArea.Height),
                spriteColour,
                0f,
                new Vector2(0, 0),
                new Vector2(hScaling, vScaling),
                SpriteEffects.None,
                0);
        }
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, Color spriteColour, float scale,
        float rotation)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        Sprites.Draw(_spriteImages,
            new Vector2((float)((xPosition + SpriteWidth / 2.0) * hScaling),
                (float)(yPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            rotation,
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None,
            0);
    }


    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, Color spriteColour, float scale,
        float rotationSpeed, bool clockwise, bool paused)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _clockWise = clockwise;

        if (paused)
        {
            _rotationSpeed = 0;
        }
        else
        {
            _rotationSpeed = rotationSpeed;
        }

        Sprites.Draw(_spriteImages,
            new Vector2((float)((xPosition + SpriteWidth / 2.0) * hScaling),
                (float)((yPosition + SpriteHeight / 2.0) * vScaling)),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            MathHelper.ToRadians(SpriteRotation),
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None, 0);
    }

    public void DrawSprite(int spriteNumber, float xPosition, float yPosition, Color spriteColour, float scale,
        float rotationSpeed, bool clockwise, bool paused)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _clockWise = clockwise;

        if (paused)
        {
            _rotationSpeed = 0;
        }
        else
        {
            _rotationSpeed = rotationSpeed;
        }

        Sprites.Draw(_spriteImages,
            new Vector2((float)(xPosition + SpriteWidth / 2.0) * hScaling,
                (float)(yPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            MathHelper.ToRadians(SpriteRotation),
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None, 0);
    }

    public void End()
    {
        Sprites.End();
        Update();
    }

    private void Update()
    {
        if (_clockWise)
        {
            if (SpriteRotation < 360f)
            {
                SpriteRotation += _rotationSpeed;
            }

            if (SpriteRotation >= 360f)
            {
                SpriteRotation = 0f;
            }
        }
        else
        {
            if (SpriteRotation > 0f)
            {
                SpriteRotation -= _rotationSpeed;
            }

            if (SpriteRotation <= 0f)
            {
                SpriteRotation = 360f;
            }
        }
    }

    /// <summary>
    ///     The batch this collection draws through, built on first use rather than on construction.
    ///     <para>
    ///         Content is loaded on a background thread, and MonoGame marshals texture creation to
    ///         the main thread for us - <c>Texture2D</c> does its GL work through
    ///         <c>Threading.BlockOnUIThread</c>. <c>SpriteBatch</c> does not: its constructor builds
    ///         a <c>SpriteEffect</c> and a <c>SpriteBatcher</c> directly, so constructing one off the
    ///         main thread touches the graphics context from the wrong thread. Every draw call is on
    ///         the main thread, so deferring it to the first one puts it where it belongs.
    ///     </para>
    /// </summary>
    private SpriteBatch Sprites
        => _sprites ??= new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);

    /// <summary>
    ///     A second collection over the same sheet, carrying this one's fade, rotation and spin.
    /// </summary>
    /// <remarks>
    ///     The two sprite position arrays used to be copied here by round-tripping them through
    ///     MessagePack - a deep-clone-via-BinaryFormatter idiom carried forward - and the texture was
    ///     loaded a second time. None of the three was needed: the constructor on the line above
    ///     computes both arrays from the same sheet's dimensions and loads the same texture, so all
    ///     three assigned a value over an identical one.
    /// </remarks>
    public SpriteCollection Clone()
    {
        var clonedSpriteCollection = new SpriteCollection(
            _graphicsDeviceManager,
            _contentManager,
            _imageFilePath,
            SpriteWidth,
            SpriteHeight)
        {
            _fade = _fade,
            _clockWise = _clockWise,
            _rotationSpeed = _rotationSpeed,
            SpriteRotation = SpriteRotation
        };

        return clonedSpriteCollection;
    }
}
