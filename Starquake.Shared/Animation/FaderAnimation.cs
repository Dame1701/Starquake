// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using SharedLibs;

namespace Starquake_XNA;

public class FaderAnimation
{
    private readonly int minFade;
    private readonly Random randomGenerator;
    public FadeDirection FadeDirection;
    public int FadeLevel;
    public bool FadeOperational;
    private int fadeSpeed;

    public FaderAnimation(int FadeSpeed, Random RandomGenerator = null, int MinFade = 0)
    {
        minFade = MinFade;
        randomGenerator = RandomGenerator;
        FadeOperational = false;
        FadeDirection = FadeDirection.FadeUp;
        FadeLevel = minFade;
        fadeSpeed = FadeSpeed;
    }

    public void Update(int FadeSpeed = -1)
    {
        if (FadeSpeed > 0)
        {
            fadeSpeed = FadeSpeed;
        }

        if (FadeOperational || randomGenerator == null)
        {
            if (FadeDirection == FadeDirection.FadeUp)
            {
                if (FadeLevel < 255)
                {
                    FadeLevel += fadeSpeed;
                }
                else
                {
                    FadeLevel = 255;
                    FadeDirection = FadeDirection.FadeDown;
                }
            }
            else if (FadeDirection == FadeDirection.FadeDown)
            {
                if (FadeLevel > minFade)
                {
                    FadeLevel -= fadeSpeed;
                }
                else
                {
                    FadeLevel = minFade;
                    FadeDirection = FadeDirection.FadeUp;
                    FadeOperational = false;
                }
            }
        }
        else
        {
            if (randomGenerator.Next(0, 110) == 100)
            {
                FadeOperational = true;
            }
        }
    }
}