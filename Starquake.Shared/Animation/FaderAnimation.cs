using SharedLibs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starquake_XNA
{
    public class FaderAnimation
    {
        public int FadeLevel;
        public FadeDirection FadeDirection;
        public bool FadeOperational;
        Random randomGenerator;
        int fadeSpeed;
        int minFade;

        public FaderAnimation(int FadeSpeed, Random RandomGenerator = null, int MinFade = 0)
        {
            minFade = MinFade;
            randomGenerator = RandomGenerator;
            FadeOperational = false;
            FadeDirection = FadeDirection.FadeUp;
            FadeLevel = minFade;
            fadeSpeed = FadeSpeed;
        }

        public void Update(int FadeSpeed =-1)
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
}
