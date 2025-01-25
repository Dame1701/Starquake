using SharedLibs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starquake_XNA
{
    public class RandomFaderAnimation
    {
        public int FadeLevel;
        Random randomGenerator;
        int switchSpeed;
        int switchCounter;
        int minimumFade;

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
}
