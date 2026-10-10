// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

internal class Animation
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

    /// <summary>
    ///     An animation that runs the same way round forever, which nearly every cycle in the game
    ///     is. <paramref name="animateForwards" /> picks the direction: Blob's left walk is the right
    ///     walk's frames played backwards.
    /// </summary>
    public static Animation Looping(int firstFrame, int lastFrame, int delay, bool animateForwards)
        => new()
        {
            FirstFrame = firstFrame,
            CurrentFrame = firstFrame,
            LastFrame = lastFrame,
            Yoyo = false,
            Delay = delay,
            Repeat = true,
            AnimateForwards = animateForwards
        };

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
