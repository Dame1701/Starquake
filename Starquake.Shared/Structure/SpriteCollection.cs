// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;

namespace StarquakeXNA.Structure;

[Serializable]
public class SpriteCollection
{

    private bool _clockWise = true;

    private byte _fade;

    [NonSerialized]
    private GameEngine _gameEngineReference;
    private string _imageFilePath;

    private float _rotationSpeed = 1f;

    [NonSerialized]
    private Texture2D _spriteImages;

    [NonSerialized]
    private SpriteBatch _sprites;

    private int[] _spriteTexturePosX;

    private int[] _spriteTexturePosY;

    public SpriteCollection(GameEngine gameEngineReference, ContentManager content, string imageFilePath,
        int spriteWidth, int spriteHeight)
    {
        _imageFilePath = imageFilePath;
        _gameEngineReference = gameEngineReference;

        this.SpriteWidth = spriteWidth;
        this.SpriteHeight = spriteHeight;

        _sprites = new SpriteBatch(_gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
        _spriteImages = content.Load<Texture2D>(imageFilePath);

        var noHorizontalImages = _spriteImages.Width / spriteWidth;
        var noVerticalImages = _spriteImages.Height / spriteHeight;

        SpriteCount = noHorizontalImages * noVerticalImages;

        _spriteTexturePosX = new int[SpriteCount];
        _spriteTexturePosY = new int[SpriteCount];

        var counter = 0;
        _fade = 255;

        // Generate Coordinates for the textures of the tiles found
        for (var y = 0; y < noVerticalImages; y++)
        {
            for (var x = 0; x < noHorizontalImages; x++)
            {
                _spriteTexturePosX[counter] = x * this.SpriteWidth;
                _spriteTexturePosY[counter] = y * this.SpriteHeight;
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
        private set;
    }

    private void SetSpriteEffect()
    {
        _fade = 255;
    }

    public void ResetEffect()
    {
        _fade = 0;
    }

    /// <param name="transform">
    ///     Only wanted when drawing straight to the back buffer, where it scales and centres the
    ///     composition - see GameEngine.PresentationTransform. Drawing into a render target the
    ///     game composes at its own fixed size, so there it is left null.
    /// </param>
    public void Begin(BlendState blendState, GameTime gameTime, Matrix? transform = null)
    {
        _sprites.Begin(SpriteSortMode.Deferred, blendState, null, null, null, null, transform);
    }

    public void Begin(BlendState blendState, SpriteSortMode sortMode, GameTime gameTime, Matrix? transform = null)
    {
        _sprites.Begin(sortMode, blendState, null, null, null, null, transform);
    }

    public void DrawSpriteUnscaled(int spriteNumber, int xPosition, int yPosition)
    {
        _sprites.Draw(
            _spriteImages,
            new Vector2(xPosition, yPosition),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(255, 255, 255, _fade),
            0f,
            new Vector2(0, 0),
            new Vector2(1, 1),
            SpriteEffects.None,
            0);
    }

    public void DrawSpriteUnscaled(int spriteNumber, int xPosition, int yPosition, int fade)
    {
        _sprites.Draw(
            _spriteImages,
            new Vector2(xPosition, yPosition),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(255, 255, 255, fade),
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

        _sprites.Draw(
            _spriteImages,
            new Vector2(xPosition * hScaling, yPosition * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(255, 255, 255, _fade),
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int spriteNumber, float xPosition, float yPosition)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(
            _spriteImages,
            new Vector2(xPosition * hScaling, yPosition * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(255, 255, 255, _fade));
    }

    public void DrawSprite(int spriteNumber, int xPosition, int yPosition, Color spriteColour, SpritePixel[] matrix,
        int blockSize = 4, SpriteEffects spriteEffect = SpriteEffects.None)
    {
        if (matrix.Length == SpriteWidth * SpriteHeight)
        {
            const float hScaling = 1f;
            const float vScaling = 1f;

            var x = 0;
            var y = 0;

            var blockMultiplier = 1;
            if (blockSize > 1)
            {
                blockMultiplier = 2;
            }

            for (var a = 0; a < matrix.Length; a += blockSize * blockMultiplier)
            {
                if (matrix[a].IsOn)
                {
                    _sprites.Draw(
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

        _sprites.Draw(
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

        _sprites.Draw(
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
            _sprites.Draw(
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

        _sprites.Draw(_spriteImages,
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

    public void DrawSprite(int spriteNumber, float xPosition, float yPosition, Color spriteColour, float scale,
        float rotation)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
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
        float rotation, int originX, int originY)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((xPosition + originX) * hScaling, (yPosition + originY) * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            rotation,
            new Vector2(originX, originY),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None, 0);
    }


    public void DrawSprite(int spriteNumber, float xPosition, float yPosition, Color spriteColour, float scale,
        float rotation, int originX, int originY)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((xPosition + originX) * hScaling, (yPosition + originY) * vScaling),
            new Rectangle(_spriteTexturePosX[spriteNumber], _spriteTexturePosY[spriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXna4Colour(spriteColour.R, spriteColour.G, spriteColour.B, spriteColour.A),
            rotation,
            new Vector2(originX, originY),
            new Vector2(hScaling * scale, vScaling * scale),
            SpriteEffects.None, 0);
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

        _sprites.Draw(_spriteImages,
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

        _sprites.Draw(_spriteImages,
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
        _sprites.End();
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

    public SpriteCollection Clone(GameEngine gameEngineReference)
    {
        var clonedSpriteCollection = new SpriteCollection(
            gameEngineReference,
            gameEngineReference.ContentMgr,
            _imageFilePath,
            SpriteWidth,
            SpriteHeight)
        {
            _fade = _fade,
            _clockWise = _clockWise,
            _rotationSpeed = _rotationSpeed,
            _spriteTexturePosX = MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(_spriteTexturePosX)),
            _spriteTexturePosY = MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(_spriteTexturePosY)),
            SpriteRotation = SpriteRotation,
            _spriteImages = _gameEngineReference.ContentMgr.Load<Texture2D>(_imageFilePath)
        };

        return clonedSpriteCollection;
    }
}