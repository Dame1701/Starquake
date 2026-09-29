// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Starquake_MonoGame.Structure;
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

    public SpriteCollection(GameEngine GameEngineReference, ContentManager Content, string ImageFilePath,
        int SpriteWidth, int SpriteHeight)
    {
        _imageFilePath = ImageFilePath;
        _gameEngineReference = GameEngineReference;

        this.SpriteWidth = SpriteWidth;
        this.SpriteHeight = SpriteHeight;

        _sprites = new SpriteBatch(_gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
        _spriteImages = Content.Load<Texture2D>(ImageFilePath);

        var noHorizontalImages = _spriteImages.Width / SpriteWidth;
        var noVerticalImages = _spriteImages.Height / SpriteHeight;

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

    public void DrawSpriteUnscaled(int SpriteNumber, int XPosition, int YPosition)
    {
        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition, YPosition),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(255, 255, 255, _fade),
            0f,
            new Vector2(0, 0),
            new Vector2(1, 1),
            SpriteEffects.None,
            0);
    }

    public void DrawSpriteUnscaled(int SpriteNumber, int XPosition, int YPosition, int Fade)
    {
        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition, YPosition),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(255, 255, 255, Fade),
            0f,
            new Vector2(0, 0),
            new Vector2(1, 1),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition * hScaling, YPosition * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(255, 255, 255, _fade),
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int SpriteNumber, float XPosition, float YPosition)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition * hScaling, YPosition * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(255, 255, 255, _fade));
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, SpritePixel[] Matrix,
        int BlockSize = 4, SpriteEffects SpriteEffect = SpriteEffects.None)
    {
        if (Matrix.Length == SpriteWidth * SpriteHeight)
        {
            const float hScaling = 1f;
            const float vScaling = 1f;

            var x = 0;
            var y = 0;

            var blockMultiplier = 1;
            if (BlockSize > 1)
            {
                blockMultiplier = 2;
            }

            for (var a = 0; a < Matrix.Length; a += BlockSize * blockMultiplier)
            {
                if (Matrix[a].IsOn)
                {
                    _sprites.Draw(
                        _spriteImages,
                        new Vector2((XPosition + x) * hScaling, (YPosition + y) * vScaling),
                        new Rectangle(_spriteTexturePosX[SpriteNumber] + x, _spriteTexturePosY[SpriteNumber] + y,
                            BlockSize, BlockSize),
                        Matrix[a].Colour,
                        0f,
                        new Vector2(0, 0),
                        new Vector2(hScaling, vScaling),
                        SpriteEffect,
                        0f);
                }

                if (x > SpriteWidth)
                {
                    x = 0;
                    y += BlockSize;
                }
                else
                {
                    x += BlockSize;
                }
            }
        }
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, int Width, int Height, Color SpriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition * hScaling, YPosition * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], Width, Height),
            SpriteColour,
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition * hScaling, YPosition * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            0f,
            new Vector2(0, 0),
            new Vector2(hScaling, vScaling),
            SpriteEffects.None,
            0f);
    }

    public void DrawSpriteSubSection(int SpriteNumber, int XPosition, int YPosition, Rectangle SubSpriteArea,
        Color SpriteColour)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        if (SpriteWidth >= SubSpriteArea.Width &&
            SpriteHeight >= SubSpriteArea.Height &&
            SpriteWidth >= SubSpriteArea.Width + SubSpriteArea.X &&
            SpriteHeight >= SubSpriteArea.Height + SubSpriteArea.Y)
        {
            _sprites.Draw(
                _spriteImages,
                new Vector2(XPosition * hScaling, YPosition * vScaling),
                new Rectangle(
                    _spriteTexturePosX[SpriteNumber] + SubSpriteArea.X,
                    _spriteTexturePosY[SpriteNumber] + SubSpriteArea.Y,
                    SubSpriteArea.Width,
                    SubSpriteArea.Height),
                SpriteColour,
                0f,
                new Vector2(0, 0),
                new Vector2(hScaling, vScaling),
                SpriteEffects.None,
                0);
        }
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale,
        float Rotation)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((float)((XPosition + SpriteWidth / 2.0) * hScaling),
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * Scale, vScaling * Scale),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale,
        float Rotation)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((float)((XPosition + SpriteWidth / 2.0) * hScaling),
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * Scale, vScaling * Scale),
            SpriteEffects.None,
            0);
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale,
        float Rotation, int OriginX, int OriginY)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((XPosition + OriginX) * hScaling, (YPosition + OriginY) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2(OriginX, OriginY),
            new Vector2(hScaling * Scale, vScaling * Scale),
            SpriteEffects.None, 0);
    }


    public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale,
        float Rotation, int OriginX, int OriginY)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _sprites.Draw(_spriteImages,
            new Vector2((XPosition + OriginX) * hScaling, (YPosition + OriginY) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2(OriginX, OriginY),
            new Vector2(hScaling * Scale, vScaling * Scale),
            SpriteEffects.None, 0);
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, float Scale,
        float RotationSpeed, bool Clockwise, bool Paused)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _clockWise = Clockwise;

        if (Paused)
        {
            _rotationSpeed = 0;
        }
        else
        {
            _rotationSpeed = RotationSpeed;
        }

        _sprites.Draw(_spriteImages,
            new Vector2((float)((XPosition + SpriteWidth / 2.0) * hScaling),
                (float)((YPosition + SpriteHeight / 2.0) * vScaling)),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            MathHelper.ToRadians(SpriteRotation),
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * Scale, vScaling * Scale),
            SpriteEffects.None, 0);
    }

    public void DrawSprite(int SpriteNumber, float XPosition, float YPosition, Color SpriteColour, float Scale,
        float RotationSpeed, bool Clockwise, bool Paused)
    {
        const float hScaling = 1f;
        const float vScaling = 1f;

        _clockWise = Clockwise;

        if (Paused)
        {
            _rotationSpeed = 0;
        }
        else
        {
            _rotationSpeed = RotationSpeed;
        }

        _sprites.Draw(_spriteImages,
            new Vector2((float)(XPosition + SpriteWidth / 2.0) * hScaling,
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], SpriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            MathHelper.ToRadians(SpriteRotation),
            new Vector2((float)SpriteWidth / 2, (float)SpriteHeight / 2),
            new Vector2(hScaling * Scale, vScaling * Scale),
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

    public SpriteCollection Clone(GameEngine GameEngineReference)
    {
        var clonedSpriteCollection = new SpriteCollection(
            GameEngineReference,
            GameEngineReference.ContentMgr,
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