// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Helpers;
using Starquake.Sprites;

namespace Starquake.Animations;

internal class BouncingAnimation
{
    /// <summary>
    ///     How much of a part's life it spends at full size and full brightness before it starts to go.
    ///     It used to begin shrinking and fading on the very first frame, which left the parts small and
    ///     translucent for nearly all of the flight - the explosion was there but barely readable.
    /// </summary>
    private const double TimeAtFullSize = 0.6;

    /// <summary>
    ///     How fast a part tumbles, in degrees a logic frame. It used to be a half-degree either way,
    ///     which over the whole life came to less than a sixth of a turn - a wobble rather than a tumble.
    /// </summary>
    private const float SlowestTumble = 3f;

    /// <inheritdoc cref="SlowestTumble" />
    private const float FastestTumble = 9f;

    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private readonly int _spriteNo;

    private int _effectDuration;
    private int _effectDurationCounter;

    private double _fade = ColourLevels.Full;
    private double _gravity;
    private float _rotation;
    private float _rotationSpeed;

    private float _scale = 1.0f;
    private int _xDelta;
    private double _yDelta;

    public BouncingAnimation(ResourceManager resources, Random randomNumberGenerator, int spriteNo)
    {
        _spriteNo = spriteNo;
        _resources = resources;
        _randomNumberGenerator = randomNumberGenerator;
    }
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
        _fade = ColourLevels.Full;
        _scale = 1.0f;
        _effectDuration = _randomNumberGenerator.Next(effectMinDuration, effectMaxDuration);

        _rotationSpeed = (float)(SlowestTumble + _randomNumberGenerator.NextDouble() * (FastestTumble - SlowestTumble));

        if (_randomNumberGenerator.Next(0, 2) == 0)
        {
            _rotationSpeed = -_rotationSpeed;
        }
    }

    /// <summary>
    ///     A part holds its size and brightness for <see cref="TimeAtFullSize" /> of its life and then goes
    ///     out over what is left, rather than shrinking and fading from the moment it is thrown.
    /// </summary>
    private void FadeAndShrinkOverTheTail()
    {
        int framesAtFullSize = (int)(_effectDuration * TimeAtFullSize);

        if (_effectDurationCounter < framesAtFullSize)
        {
            return;
        }

        // At least one frame, so a very short life still ends at nothing rather than dividing by nought.
        int tail = Math.Max(_effectDuration - framesAtFullSize, 1);
        double throughTheTail = Math.Min((double)(_effectDurationCounter - framesAtFullSize) / tail, 1.0);

        _scale = (float)(1.0 - throughTheTail);
        _fade = ColourLevels.Full * (1.0 - throughTheTail);
    }

    public void YBounce()
    {
        if (Triggered)
        {
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobPartClang, false);
            _yDelta = -_yDelta;
        }
    }

    public void XBounce()
    {
        if (Triggered)
        {
            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobPartClang, false);
            _xDelta = -_xDelta;
        }
    }

    public Point Update()
    {
        if (Triggered)
        {
            _yDelta += _gravity;

            _rotation += _rotationSpeed;

            FadeAndShrinkOverTheTail();

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
            spriteToDraw.DrawSprite(spriteNumberOverride, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_fade), _scale, _rotation);
        }
        else
        {
            spriteToDraw.DrawSprite(_spriteNo, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_fade), _scale, _rotation);
        }
    }
}
