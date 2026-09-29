// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace StarquakeXNA.GameComponents;

public class GameScreenBase : GameComponentBase
{
    protected int screenFade;
    protected int screenFadeSpeed;

    public GameScreenBase(GameEngine GameEngineReference)
        : base(GameEngineReference)
    {
        //screenFade = 0;
        // screenFadeSpeed = 5;
    }

    public int ScreenFade
    {
        get => screenFade;
        set => screenFade = value;
    }

    public override void Update(GameTime gameTime)
    {
        // Fade screen back up
        if (screenFade < 255)
        {
            screenFade = screenFade + screenFadeSpeed;
        }

        if (screenFade > 255)
        {
            screenFade = 255;
        }
    }
}