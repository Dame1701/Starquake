// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -


namespace Starquake.Animations;

public class Animation
{

    private int _currentFrame;

    private int _delayCounter;

    public int FirstFrame { get; set; }

    public int LastFrame { get; set; }

    public int Delay { get; set; }

    public bool Yoyo { get; set; }

    public bool Repeat { get; set; }

    public bool Finished { get; set; }

    public bool AnimateForwards { get; set; }

    public int CurrentFrame
    {
        get => _currentFrame;
        set
        {
            _currentFrame = value;
            _delayCounter = 0;
        }
    }

    public void GetNextFrame()
    {
        if (_delayCounter > Delay)
        {
            _delayCounter = 0;

            if (AnimateForwards)
            {
                if (_currentFrame < LastFrame)
                {
                    _currentFrame++;
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
                            _currentFrame = FirstFrame;
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
                if (_currentFrame > FirstFrame)
                {
                    _currentFrame--;
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
                            _currentFrame = LastFrame;
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
            _delayCounter++;
        }
    }
}