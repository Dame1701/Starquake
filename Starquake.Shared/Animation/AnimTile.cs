// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Structure;

namespace StarquakeXNA;

public class AnimTileEngine : GameComponentBase
{

    public AnimTileEngine(GameEngine GameEngineReference)
        : base(GameEngineReference)
    {
        AnimTilesList = new List<AnimTile>();
    }
    public List<AnimTile> AnimTilesList { get; }

    public bool ContainsTile(int TileNumber)
    {
        for (var a = 0; a < AnimTilesList.Count; a++)
        {
            if (AnimTilesList[a].ScreenTileRef == TileNumber)
            {
                return true;
            }
        }

        return false;
    }

    public override void Update(GameTime gameTime)
    {
        for (var a = 0; a < AnimTilesList.Count; a++)
        {
            AnimTilesList[a].Update(gameTime);
        }
    }

    public void Draw(int TileNumber, int XPos, int YPos, Color Colour, GameTime gameTime)
    {
        for (var a = 0; a < AnimTilesList.Count; a++)
        {
            if (AnimTilesList[a].ScreenTileRef == TileNumber)
            {
                AnimTilesList[a].Draw(XPos, YPos, Colour, gameTime);
            }
        }
    }
}

public class AnimTile : GameComponentBase
{

    private readonly SpriteCollection tileAnimationSpriteSet;

    public AnimTile(GameEngine GameEngineReference, int ScreenTileRef, SpriteCollection TileAnimationSpriteSet, int Delay) : base(GameEngineReference)
    {
        this.ScreenTileRef = ScreenTileRef;
        tileAnimationSpriteSet = TileAnimationSpriteSet;

        TileAnimation = new Animation();
        TileAnimation.FirstFrame = 0;

        TileAnimation.LastFrame = tileAnimationSpriteSet.SpriteCount - 1;
        TileAnimation.AnimateForwards = true;
        TileAnimation.Repeat = true;
        TileAnimation.CurrentFrame = 0;
        TileAnimation.Delay = Delay;
        TileAnimation.Yoyo = true;
    }
    public int ScreenTileRef { get; }

    public Animation TileAnimation { get; }

    public override void Update(GameTime gameTime)
    {
        TileAnimation.GetNextFrame();
    }

    public void Draw(int XPos, int YPos, Color Colour, GameTime gameTime)
    {
        if (!gameEngine.UsingOldGraphics)
        {
            tileAnimationSpriteSet.Begin(BlendState.AlphaBlend, gameTime);
            tileAnimationSpriteSet.DrawSprite(TileAnimation.CurrentFrame, XPos, YPos, Colour);
            tileAnimationSpriteSet.End();
        }
    }
}