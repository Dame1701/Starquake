// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Animations;

public class TunnelLiftAnimationSection
{
    private readonly int _delayStart;
    private int _delayCounter;

    private int _lightDirection;
    public int Intensity;
    public int X;
    public int Y;

    public TunnelLiftAnimationSection(int delayStartTime)
    {
        _delayStart = delayStartTime;
        _delayCounter = delayStartTime;
    }

    public void Update()
    {
        if (_delayCounter > 0)
        {
            _delayCounter--;
            Intensity = 0;
        }
        else
        {
            if (_lightDirection == 0)
            {
                if (Intensity < ColourLevels.Full)
                {
                    Intensity += 8;
                }
                else
                {
                    _lightDirection = 1;
                }
            }
            else if (_lightDirection == 1)
            {
                if (Intensity > 0)
                {
                    Intensity -= 8;
                }
                else
                {
                    _lightDirection = 0;
                    //DelayCounter = DelayStart;
                }
            }
        }
    }

    public override string ToString()
        => "X: " + X + ", Y:" + Y + ", D:" + _delayStart;
}
