// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.Core;

namespace Starquake.Sprites;

internal class SpriteCollection
{
    private readonly ContentManager _contentManager;

    private readonly GraphicsDeviceManager _graphicsDeviceManager;

    private readonly string _imageFilePath;

    private readonly Texture2D _spriteImages;

    private readonly int[] _spriteTexturePosX;

    private readonly int[] _spriteTexturePosY;

    private byte _fade;

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

        // Where each frame sits in the sheet, worked out once so drawing one is an index rather than
        // a multiplication.
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


    /// <summary>
    ///     The batch this collection draws through, built on first use rather than at construction.
    ///     <para>
    ///         Content loads on a background thread, and MonoGame marshals texture creation back to
    ///         the main thread. <c>SpriteBatch</c> is not marshalled: its constructor builds a
    ///         <c>SpriteEffect</c> and a <c>SpriteBatcher</c> directly, so building one off the main
    ///         thread touches the graphics context from the wrong thread. Every draw call is on the
    ///         main thread, so deferring to the first one puts it where it belongs.
    ///     </para>
    /// </summary>
    private SpriteBatch Sprites
        => field ??= new SpriteBatch(_graphicsDeviceManager.GraphicsDevice);

    /// <summary>
    ///     Opens a batch. Everything drawn until <see cref="End" /> is queued and issued in the order
    ///     the calls were made.
    /// </summary>
    /// <remarks>
    ///     The sort mode is always <c>Deferred</c>, and deliberately not a parameter. Nothing in the
    ///     game sets a layer depth, so <c>BackToFront</c> would sort by a constant key and leave the
    ///     order among equal keys unspecified, while <c>Immediate</c> flushes a draw call per sprite.
    /// </remarks>
    /// <param name="blendState">The blend state to draw with.</param>
    /// <param name="transform">
    ///     Only wanted when drawing straight to the back buffer, where it scales and centres the
    ///     composition. See PresentationSettings.Transform. Left null when drawing into a render
    ///     target, which the game composes at its own fixed size.
    /// </param>
    /// <param name="effect">
    ///     A pixel shader to put every sprite in this batch through, or null for the ordinary path.
    ///     <c>Nasty</c>'s birth is the only caller that passes one. Anything that does must also hand
    ///     the shader <see cref="SpriteRectIn01" />, because a sprite drawn from a sheet has its
    ///     neighbours either side of it.
    /// </param>
    public void Begin(BlendState blendState, Matrix? transform, Effect? effect)
    {
        Sprites.Begin(SpriteSortMode.Deferred, blendState, null, null, null, effect, transform);
    }

    public void Begin(BlendState blendState, Matrix? transform = null)
    {
        Sprites.Begin(SpriteSortMode.Deferred, blendState, null, null, null, null, transform);
    }

    /// <summary>
    ///     Where one sprite sits in its sheet, as a fraction of the sheet: xy the corner, zw the
    ///     size. What a shader needs to keep its sampling inside the sprite it was given.
    /// </summary>
    public Vector4 SpriteRectIn01(int spriteNumber) => RectIn01(
        _spriteImages.Width,
        _spriteImages.Height,
        _spriteTexturePosX[spriteNumber],
        _spriteTexturePosY[spriteNumber],
        SpriteWidth,
        SpriteHeight);

    /// <summary>
    ///     The arithmetic of <see cref="SpriteRectIn01" />, kept apart from the sheet so it can be
    ///     checked without loading one.
    /// </summary>
    public static Vector4 RectIn01(
        int sheetWidth,
        int sheetHeight,
        int xPos,
        int yPos,
        int spriteWidth,
        int spriteHeight)
        => new(
            xPos / (float)sheetWidth,
            yPos / (float)sheetHeight,
            spriteWidth / (float)sheetWidth,
            spriteHeight / (float)sheetHeight);

    public void DrawSpriteUnscaled(int spriteNumber, int xPosition, int yPosition, int fade)
    {
        Sprites.Draw(
            _spriteImages,
            new Vector2(xPosition, yPosition),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            Colours.WhiteFadedTo(fade),
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
            Colours.WhiteFadedTo(_fade),
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
            Colours.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
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

    /// <summary>Draws one sprite at an angle, in radians, about its own centre.</summary>
    /// <remarks>
    ///     The angle belongs to the caller. Holding one here and advancing it while drawing would run
    ///     the spin at the display's refresh rate, spin twice as fast for a sheet drawn twice in a
    ///     frame, and share one angle across everything using a collection the resource manager hands
    ///     out.
    /// </remarks>
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
            Colours.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            rotation,
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None,
            0);
    }


    /// <inheritdoc cref="DrawSprite(int,int,int,Microsoft.Xna.Framework.Color,float,float)" />
    public void DrawSprite(
        int spriteNumber, float xPosition, float yPosition, Color spriteColour, float scale, float rotation)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        Sprites.Draw(_spriteImages,
            new Vector2((float)(xPosition + SpriteWidth / 2.0) * hScaling,
                (float)(yPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            Colours.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            rotation,
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None, 0);
    }

    public void End() => Sprites.End();

    /// <summary>
    ///     A second collection over the same sheet, carrying this one's fade, rotation and spin.
    /// </summary>
    /// <remarks>
    ///     The constructor already computes both sprite position arrays from the sheet's dimensions
    ///     and loads the same texture, so none of that needs copying across.
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
            _fade = _fade
        };

        return clonedSpriteCollection;
    }
}
