// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.GameComponents;

public class GameScreenBase : GameComponentBase
{
    protected int ScreenFadeSpeed;

    public GameScreenBase(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        //ScreenFade = 0;
        // screenFadeSpeed = 5;
    }

    public int ScreenFade { get; set; }

    public override void Update(GameTime gameTime)
    {
        // Fade screen back up
        if (ScreenFade < ColourLevels.Full)
        {
            ScreenFade = ScreenFade + ScreenFadeSpeed;
        }

        if (ScreenFade > ColourLevels.Full)
        {
            ScreenFade = ColourLevels.Full;
        }
    }
}