using System;
using System.Collections.Generic;
using System.Text;

namespace StarquakeXNA
{
    public class Animation
    {
        int firstFrame;
        public int FirstFrame
        {
            get { return firstFrame; }
            set { firstFrame = value; }
        }

        int lastFrame;
        public int LastFrame
        {
            get { return lastFrame; }
            set { lastFrame = value; }
        }

        int delay;
        public int Delay
        {
            get { return delay; }
            set { delay = value; }
        }

        bool yoyo;
        public bool Yoyo
        {
            get { return yoyo; }
            set { yoyo = value; }
        }

        bool repeat;
        public bool Repeat
        {
            get { return repeat; }
            set { repeat = value; }
        }

        bool finished;
        public bool Finished
        {

            get { return finished; }
            set { finished = value; }
        }

        bool animateForwards;
        public bool AnimateForwards
        {
            get { return animateForwards; }
            set { animateForwards = value; }
        }
        
        int currentFrame = 0;
        public int CurrentFrame
        {
            get { return currentFrame; }
            set 
            { 
                currentFrame = value;
                delayCounter = 0;
            }
        }

        int delayCounter = 0;

        public void GetNextFrame()
        {
            if (delayCounter > delay)
            {
                delayCounter = 0;

                if (AnimateForwards)
                {
                    if (currentFrame < LastFrame)
                    {
                        currentFrame++;
                    }
                    else
                    {
                        if (repeat)
                        {

                            if (yoyo)
                            {
                                AnimateForwards = false;
                            }
                            else
                            {
                                currentFrame = FirstFrame;
                            }
                        }
                        else
                        {
                            finished = true;
                        }
                    }
                }
                else
                {
                    if (currentFrame > FirstFrame)
                    {
                        currentFrame--;
                    }
                    else
                    {
                        if (repeat)
                        {

                            if (yoyo)
                            {
                                AnimateForwards = true;
                            }
                            else
                            {
                                currentFrame = LastFrame;
                            }
                        }
                        else
                        {
                            finished = true;
                        }
                    }
                }
            }
            else
            {
                delayCounter++;
            }
        }

    }
}
