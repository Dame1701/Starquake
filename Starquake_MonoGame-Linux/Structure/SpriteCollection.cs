using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StarquakeXNA.Helpers;
using Starquake_MonoGame.Structure;

namespace StarquakeXNA;

[Serializable]
public class SpriteCollection
{
    private int[] _spriteTexturePosX;
    private int[] _spriteTexturePosY;

    private float _spriteRotation;

    public float SpriteRotation
    {
        get => _spriteRotation;
        set => _spriteRotation = value;
    }

    private bool _clockWise = true;
    private float _rotationSpeed = 1f;

    [NonSerialized] private SpriteBatch _sprites;

    [NonSerialized] private Texture2D _spriteImages;

    private int _spriteWidth;

    public int SpriteWidth => _spriteWidth;

    public int SpriteHeight { get; }

    private byte _fade;

    public int SpriteCount { get; }

    private string _imageFilePath;

    [NonSerialized] private GameEngine _gameEngineReference;

    public SpriteCollection(GameEngine GameEngineReference, ContentManager Content, string ImageFilePath,
        int SpriteWidth, int SpriteHeight)
    {
        _imageFilePath = ImageFilePath;
        _gameEngineReference = GameEngineReference;

        _spriteWidth = SpriteWidth;
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
        for (int y = 0; y < noVerticalImages; y++)
        {
            for (int x = 0; x < noHorizontalImages; x++)
            {
                _spriteTexturePosX[counter] = x * _spriteWidth;
                _spriteTexturePosY[counter] = y * this.SpriteHeight;
                counter++;
            }
        }
    }

    private void SetSpriteEffect()
    {
        _fade = 255;
    }

    public void ResetEffect()
    {
        _fade = 0;
    }

    public void Begin(BlendState blendState, GameTime gameTime)
    {
        _sprites.Begin(SpriteSortMode.Deferred, blendState);
    }

    public void Begin(BlendState blendState, SpriteSortMode sortMode, GameTime gameTime)
    {
        _sprites.Begin(sortMode, blendState);
    }

    public void DrawSpriteUnscaled(int SpriteNumber, int XPosition, int YPosition)
    {
        _sprites.Draw(
            _spriteImages,
            new Vector2(XPosition, YPosition),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(255, 255, 255, _fade));
    }

    public void DrawSprite(int SpriteNumber, int XPosition, int YPosition, Color SpriteColour, SpritePixel[] Matrix,
        int BlockSize = 4, SpriteEffects SpriteEffect = SpriteEffects.None)
    {
        if (Matrix.Length == _spriteWidth * SpriteHeight)
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

            for (var a = 0; a < Matrix.Length; a += (BlockSize * blockMultiplier))
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

                if (x > _spriteWidth)
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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

        if (_spriteWidth >= SubSpriteArea.Width &&
            SpriteHeight >= SubSpriteArea.Height &&
            _spriteWidth >= SubSpriteArea.Width + SubSpriteArea.X &&
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
            new Vector2((float)((XPosition + _spriteWidth / 2.0) * hScaling),
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2((float)_spriteWidth / 2, (float)SpriteHeight / 2),
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
            new Vector2((float)((XPosition + _spriteWidth / 2.0) * hScaling),
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            Rotation,
            new Vector2((float)_spriteWidth / 2, (float)SpriteHeight / 2),
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
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
            new Vector2((float)((XPosition + _spriteWidth / 2.0) * hScaling),
                (float)((YPosition + SpriteHeight / 2.0) * vScaling)),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            MathHelper.ToRadians(_spriteRotation),
            new Vector2((float)_spriteWidth / 2, (float)SpriteHeight / 2),
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
            new Vector2((float)(XPosition + _spriteWidth / 2.0) * hScaling,
                (float)(YPosition + SpriteHeight / 2.0) * vScaling),
            new Rectangle(_spriteTexturePosX[SpriteNumber], _spriteTexturePosY[SpriteNumber], _spriteWidth,
                SpriteHeight),
            ColourHelper.GetXNA4Colour(SpriteColour.R, SpriteColour.G, SpriteColour.B, SpriteColour.A),
            MathHelper.ToRadians(_spriteRotation),
            new Vector2((float)_spriteWidth / 2, (float)SpriteHeight / 2),
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
            if (_spriteRotation < 360f)
            {
                _spriteRotation += _rotationSpeed;
            }

            if (_spriteRotation >= 360f)
            {
                _spriteRotation = 0f;
            }
        }
        else
        {
            if (_spriteRotation > 0f)
            {
                _spriteRotation -= _rotationSpeed;
            }

            if (_spriteRotation <= 0f)
            {
                _spriteRotation = 360f;
            }
        }
    }

    public SpriteCollection Clone(GameEngine GameEngineReference)
    {
        var clonedSprites = SharedLibs.BinarySerialiser.SerialiseCloneBinary(this);

        clonedSprites._gameEngineReference = GameEngineReference;
        clonedSprites._sprites = new SpriteBatch(_gameEngineReference.GraphicsDeviceMgr.GraphicsDevice);
        clonedSprites._spriteImages = _gameEngineReference.ContentMgr.Load<Texture2D>(clonedSprites._imageFilePath);

        return clonedSprites;
    }
}