// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Core;
using Starquake.Sprites;

namespace Starquake.Animations;

internal class Bounce
{
    /// <summary>
    ///     How much of a part's life it spends at full size and brightness before starting to go.
    ///     Fading from the first frame leaves the parts small and translucent for nearly all of the
    ///     flight, which makes the explosion hard to read.
    /// </summary>
    private const double TimeAtFullSize = 0.72;

    /// <summary>
    ///     How much of its size a part keeps as it goes out. A part that stays mostly itself and
    ///     fades reads as burning out; one that shrinks to a point reads as being scaled away.
    /// </summary>
    private const double SizeKeptAtTheEnd = 0.62;

    /// <summary>
    ///     How fast a part tumbles, in degrees a logic frame. Fast enough to read as a tumble rather
    ///     than a wobble over the part's whole life.
    /// </summary>
    private const float SlowestTumble = 3f;

    /// <inheritdoc cref="SlowestTumble" />
    private const float FastestTumble = 9f;

    private readonly Random _randomNumberGenerator;

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

    public Bounce(Random randomNumberGenerator, int spriteNo)
    {
        _spriteNo = spriteNo;
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
    ///     A part holds its size and brightness for <see cref="TimeAtFullSize" /> of its life and
    ///     then goes out over what is left.
    /// </summary>
    /// <remarks>
    ///     Size and light do not run down the same line. The light goes out on a curve, so a part
    ///     dies away rather than being turned down like a dimmer, and it keeps most of its size while
    ///     it does. See <see cref="SizeKeptAtTheEnd" />.
    /// </remarks>
    private void FadeAndShrinkOverTheTail()
    {
        int framesAtFullSize = (int)(_effectDuration * TimeAtFullSize);

        if (_effectDurationCounter < framesAtFullSize)
        {
            return;
        }

        // At least one frame, so a very short life still ends at nothing rather than dividing by
        // nought.
        int tail = Math.Max(_effectDuration - framesAtFullSize, 1);
        double throughTheTail = Math.Min((double)(_effectDurationCounter - framesAtFullSize) / tail, 1.0);

        double left = 1.0 - throughTheTail;

        _scale = (float)(1.0 - throughTheTail * (1.0 - SizeKeptAtTheEnd));
        _fade = ColourLevels.Full * left * left;
    }

    public void YBounce()
    {
        if (Triggered)
        {
            _yDelta = -_yDelta;
        }
    }

    public void XBounce()
    {
        if (Triggered)
        {
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
            spriteToDraw.DrawSprite(spriteNumberOverride, xPos, yPos, Colours.WhiteFadedTo((byte)_fade), _scale, _rotation);
        }
        else
        {
            spriteToDraw.DrawSprite(_spriteNo, xPos, yPos, Colours.WhiteFadedTo((byte)_fade), _scale, _rotation);
        }
    }
}
