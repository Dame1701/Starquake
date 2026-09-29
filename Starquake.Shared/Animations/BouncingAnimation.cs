// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Structure;

namespace Starquake.Animations;

public class BouncingAnimation
{
    private readonly double _friction = 1.00;

    private readonly GameEngine _gameEngine;

    private readonly int _spriteNo;

    private int _effectDuration;
    private int _effectDurationCounter;

    private double _fade = 255;
    private int _fadeSpeed;
    private double _gravity;
    private float _rotation;
    private float _rotationSpeed;

    private float _scale = 1.0f;
    private int _xDelta;
    private double _yDelta;

    public BouncingAnimation(GameEngine gameEngineReference, int spriteNo)
    {
        _spriteNo = spriteNo;
        _gameEngine = gameEngineReference;
    }
    public int Fade => (int)_fade;
    public bool Triggered { get; private set; }

    public void Reset()
    {
        Triggered = false;
    }

    public void Trigger(double gravity, int initialYSpeed, int xSpeed, int effectMinDuration = 50, int effectMaxDuration = 100)
    {
        _yDelta = initialYSpeed;
        _xDelta = xSpeed;
        _gravity = gravity;
        _effectDurationCounter = 0;
        Triggered = true;
        _fade = 255;
        _scale = 1.0f;
        _effectDuration = _gameEngine.RandomNumberGenerator.Next(effectMinDuration, effectMaxDuration);
        _fadeSpeed = _gameEngine.RandomNumberGenerator.Next(10, 30);
        _rotationSpeed = (float)(_gameEngine.RandomNumberGenerator.NextDouble() - 0.5);
    }

    public void YBounce()
    {
        if (Triggered)
        {
            _gameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPartClang", false);
            _yDelta = -_yDelta;
        }
    }

    public void XBounce()
    {
        if (Triggered)
        {
            _gameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPartClang", false);
            _xDelta = -_xDelta;
        }
    }

    public Point Update()
    {
        if (Triggered)
        {
            _yDelta += _gravity;
            _yDelta /= _friction;

            _rotation = _rotation + _rotationSpeed;

            if (_scale > 0)
            {
                _scale = _scale - 1f / _effectDuration;
            }
            else
            {
                _scale = 0;
            }

            if (_fade > 0)
            {
                _fade = _fade - 255 / (double)_effectDuration;
            }
            else
            {
                _fade = 0;
            }

            // Fade the particles out
            if (_effectDurationCounter > _effectDuration)
            {
                Triggered = false;
            }

            _effectDurationCounter++;
        }

        return new Point(_xDelta, (int)_yDelta);
    }

    public void Draw(SpriteCollection spriteToDraw, int xPos, int yPos, int spriteNumberOverride = -1)
    {
        if (spriteNumberOverride >= 0)
        {
            spriteToDraw.DrawSprite(spriteNumberOverride, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_fade), _scale, _rotation);
        }
        else
        {
            spriteToDraw.DrawSprite(_spriteNo, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_fade), _scale, _rotation);
        }
    }
}