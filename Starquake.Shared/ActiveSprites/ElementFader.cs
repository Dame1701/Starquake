// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class ElementFader
{

    private readonly int _fadeSpeed;
    private Direction _fadeDirection;

    public ElementFader(Random randomNumberGenerator, Direction fadeDirection)
    {
        _fadeDirection = fadeDirection;
        _fadeSpeed = randomNumberGenerator.Next(8, 16);
        FadeLevel = randomNumberGenerator.Next(50, 200);
    }
    public int FadeLevel { get; private set; }

    public void SetFadeToZero()
    {
        FadeLevel = 0;
        _fadeDirection = Direction.Up;
    }

    public void Update()
    {
        if (_fadeDirection == Direction.Up)
        {
            FadeLevel = FadeLevel + _fadeSpeed;
        }
        else if (_fadeDirection == Direction.Down)
        {
            FadeLevel = FadeLevel - _fadeSpeed;
        }

        if (FadeLevel > ColourLevels.Full)
        {
            FadeLevel = ColourLevels.Full;
            _fadeDirection = Direction.Down;
        }
        else if (FadeLevel < 0)
        {
            FadeLevel = 0;
            _fadeDirection = Direction.Up;
        }

    }
}
