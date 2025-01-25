using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarquakeXNA
{
    public class AnimTileEngine : GameComponentBase
    {
        private List<AnimTile> animTiles;
        public List<AnimTile> AnimTilesList
        {
            get { return animTiles; }
        }

        public AnimTileEngine(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            animTiles = new List<AnimTile>();
        }

        public bool ContainsTile(int TileNumber)
        {
            for (int a = 0; a < animTiles.Count; a++)
            {
                if (animTiles[a].ScreenTileRef == TileNumber)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            for (int a = 0; a < animTiles.Count; a++)
            {
                animTiles[a].Update(gameTime);
            }
        }

        public void Draw(int TileNumber, int XPos, int YPos, Color Colour, GameTime gameTime)
        {
            for (int a = 0; a < animTiles.Count; a++)
            {
                if (animTiles[a].ScreenTileRef == TileNumber)
                {
                    animTiles[a].Draw(XPos, YPos, Colour, gameTime);
                }
            }
        }
    }

    public class AnimTile : GameComponentBase
    {
        private int screenTileRef;
        public int ScreenTileRef
        {
            get { return screenTileRef; }
        }
        private SpriteCollection tileAnimationSpriteSet;

        private Animation tileAnimation;
        public Animation TileAnimation
        {
            get { return tileAnimation; }
        }

        public AnimTile(GameEngine GameEngineReference, int ScreenTileRef, SpriteCollection TileAnimationSpriteSet, int Delay) : base(GameEngineReference)
        {
            screenTileRef = ScreenTileRef;
            tileAnimationSpriteSet = TileAnimationSpriteSet;

            tileAnimation = new Animation();
            tileAnimation.FirstFrame = 0;
            
            tileAnimation.LastFrame = tileAnimationSpriteSet.SpriteCount - 1;
            tileAnimation.AnimateForwards = true;
            tileAnimation.Repeat = true;
            tileAnimation.CurrentFrame = 0;
            tileAnimation.Delay = Delay;
            tileAnimation.Yoyo = true;
        }

        public override void Update(GameTime gameTime)
        {
            tileAnimation.GetNextFrame();
        }

        public void Draw(int XPos, int YPos, Color Colour, GameTime gameTime)
        {
            if (!gameEngine.UsingOldGraphics)
            {
                tileAnimationSpriteSet.Begin(BlendState.AlphaBlend, gameTime);
                tileAnimationSpriteSet.DrawSprite(tileAnimation.CurrentFrame, XPos, YPos, Colour);
                tileAnimationSpriteSet.End();
            }
        }
    }
}
