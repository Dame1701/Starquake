// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Starquake.Constants;
using Starquake.SharedLibs;

namespace Starquake.Animations;

public class FaderAnimation
{
    private readonly int _minFade;
    private readonly Random? _randomGenerator;
    private FadeDirection FadeDirection;
    public int FadeLevel;
    private bool FadeOperational;
    private int _fadeSpeed;

    public FaderAnimation(int fadeSpeed, Random? randomGenerator = null, int minFade = 0)
    {
        _minFade = minFade;
        _randomGenerator = randomGenerator;
        FadeOperational = false;
        FadeDirection = FadeDirection.FadeUp;
        FadeLevel = _minFade;
        _fadeSpeed = fadeSpeed;
    }

    public void Update(int fadeSpeed = -1)
    {
        if (fadeSpeed > 0)
        {
            _fadeSpeed = fadeSpeed;
        }

        if (FadeOperational || _randomGenerator == null)
        {
            if (FadeDirection == FadeDirection.FadeUp)
            {
                if (FadeLevel < ColourLevels.Full)
                {
                    FadeLevel += _fadeSpeed;
                }
                else
                {
                    FadeLevel = ColourLevels.Full;
                    FadeDirection = FadeDirection.FadeDown;
                }
            }
            else if (FadeDirection == FadeDirection.FadeDown)
            {
                if (FadeLevel > _minFade)
                {
                    FadeLevel -= _fadeSpeed;
                }
                else
                {
                    FadeLevel = _minFade;
                    FadeDirection = FadeDirection.FadeUp;
                    FadeOperational = false;
                }
            }
        }
        else
        {
            if (_randomGenerator.Next(0, 110) == 100)
            {
                FadeOperational = true;
            }
        }
    }
}