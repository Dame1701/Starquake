// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace StarquakeXNA;

public class Animation
{

    private int currentFrame;

    private int delayCounter;

    public int FirstFrame { get; set; }

    public int LastFrame { get; set; }

    public int Delay { get; set; }

    public bool Yoyo { get; set; }

    public bool Repeat { get; set; }

    public bool Finished { get; set; }

    public bool AnimateForwards { get; set; }

    public int CurrentFrame
    {
        get => currentFrame;
        set
        {
            currentFrame = value;
            delayCounter = 0;
        }
    }

    public void GetNextFrame()
    {
        if (delayCounter > Delay)
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
                    if (Repeat)
                    {

                        if (Yoyo)
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
                        Finished = true;
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
                    if (Repeat)
                    {

                        if (Yoyo)
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
                        Finished = true;
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