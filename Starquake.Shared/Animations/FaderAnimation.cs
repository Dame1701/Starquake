// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Animations;

internal class FaderAnimation
{
    private readonly int _minFade;
    private readonly Random? _randomGenerator;
    private FadeDirection _fadeDirection;
    private bool _fadeOperational;
    private int _fadeSpeed;
    public int FadeLevel;

    public FaderAnimation(int fadeSpeed, Random? randomGenerator = null, int minFade = 0)
    {
        _minFade = minFade;
        _randomGenerator = randomGenerator;
        _fadeOperational = false;
        _fadeDirection = FadeDirection.FadeUp;
        FadeLevel = _minFade;
        _fadeSpeed = fadeSpeed;
    }

    public void Update(int fadeSpeed = -1)
    {
        if (fadeSpeed > 0)
        {
            _fadeSpeed = fadeSpeed;
        }

        if (_fadeOperational || _randomGenerator == null)
        {
            if (_fadeDirection == FadeDirection.FadeUp)
            {
                if (FadeLevel < ColourLevels.Full)
                {
                    FadeLevel += _fadeSpeed;
                }
                else
                {
                    FadeLevel = ColourLevels.Full;
                    _fadeDirection = FadeDirection.FadeDown;
                }
            }
            else if (_fadeDirection == FadeDirection.FadeDown)
            {
                if (FadeLevel > _minFade)
                {
                    FadeLevel -= _fadeSpeed;
                }
                else
                {
                    FadeLevel = _minFade;
                    _fadeDirection = FadeDirection.FadeUp;
                    _fadeOperational = false;
                }
            }
        }
        else
        {
            if (_randomGenerator.Next(0, 110) == 100)
            {
                _fadeOperational = true;
            }
        }
    }
}
