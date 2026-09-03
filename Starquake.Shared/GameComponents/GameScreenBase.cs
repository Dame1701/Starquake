using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace StarquakeXNA.GameComponents
{
    public class GameScreenBase : GameComponentBase
    {
        protected int screenFadeSpeed;
        protected int screenFade;

        public int ScreenFade
        {
            get { return screenFade; }
            set { screenFade = value; }
        }

        public GameScreenBase(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            //screenFade = 0;
           // screenFadeSpeed = 5;
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
}
