// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;

namespace Starquake_XNA;

public class RandomFaderAnimation
{
    private readonly int minimumFade;
    private readonly Random randomGenerator;
    private readonly int switchSpeed;
    public int FadeLevel;
    private int switchCounter;

    public RandomFaderAnimation(int SwitchSpeed, Random RandomGenerator, int MinimumFade = 0)
    {
        minimumFade = MinimumFade;
        switchCounter = 0;
        randomGenerator = RandomGenerator;
        FadeLevel = 0;
        switchSpeed = SwitchSpeed;
    }

    public void Update()
    {
        if (switchCounter < switchSpeed)
        {
            switchCounter++;
        }
        else
        {
            switchCounter = 0;
            FadeLevel = randomGenerator.Next(minimumFade, 255);
        }
    }
}