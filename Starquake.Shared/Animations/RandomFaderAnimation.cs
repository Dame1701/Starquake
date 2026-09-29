// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Starquake.Constants;

namespace Starquake.Animations;

public class RandomFaderAnimation
{
    private readonly int _minimumFade;
    private readonly Random _randomGenerator;
    private readonly int _switchSpeed;
    public int FadeLevel;
    private int _switchCounter;

    public RandomFaderAnimation(int switchSpeed, Random randomGenerator, int minimumFade = 0)
    {
        _minimumFade = minimumFade;
        _switchCounter = 0;
        _randomGenerator = randomGenerator;
        FadeLevel = 0;
        _switchSpeed = switchSpeed;
    }

    public void Update()
    {
        if (_switchCounter < _switchSpeed)
        {
            _switchCounter++;
        }
        else
        {
            _switchCounter = 0;
            FadeLevel = _randomGenerator.Next(_minimumFade, ColourLevels.Full);
        }
    }
}