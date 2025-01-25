using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace StarquakeXNA
{
    public abstract class GameComponentBase
    {
        protected GameEngine gameEngine;

        public GameComponentBase(GameEngine GameEngineReference)
        {
            gameEngine = GameEngineReference;
        }

        public virtual void LoadContent()
        {
        }

        public virtual void Initialise(object Data)
        {
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(GameTime gameTime)
        {
        }

        public virtual void Draw(GameTime gameTime, int screenFade, object Parameters)
        {
        }

        public virtual void Draw(GameTime gameTime, int screenFade)
        {
        }
    }
}
