// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Engines;
using StarquakeXNA.Structure;

namespace StarquakeXNA.Animations;

public class AnimTileEngine : GameComponentBase
{

    public AnimTileEngine(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        AnimTilesList = new List<AnimTile>();
    }
    public List<AnimTile> AnimTilesList { get; }

    public bool ContainsTile(int tileNumber)
    {
        for (var a = 0; a < AnimTilesList.Count; a++)
        {
            if (AnimTilesList[a].ScreenTileRef == tileNumber)
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

    public void Draw(int tileNumber, int xPos, int yPos, Color colour, GameTime gameTime)
    {
        for (var a = 0; a < AnimTilesList.Count; a++)
        {
            if (AnimTilesList[a].ScreenTileRef == tileNumber)
            {
                AnimTilesList[a].Draw(xPos, yPos, colour, gameTime);
            }
        }
    }
}

public class AnimTile : GameComponentBase
{

    private readonly SpriteCollection _tileAnimationSpriteSet;

    public AnimTile(GameEngine gameEngineReference, int screenTileRef, SpriteCollection tileAnimationSpriteSet, int delay) : base(gameEngineReference)
    {
        this.ScreenTileRef = screenTileRef;
        _tileAnimationSpriteSet = tileAnimationSpriteSet;

        TileAnimation = new Animation();
        TileAnimation.FirstFrame = 0;

        TileAnimation.LastFrame = _tileAnimationSpriteSet.SpriteCount - 1;
        TileAnimation.AnimateForwards = true;
        TileAnimation.Repeat = true;
        TileAnimation.CurrentFrame = 0;
        TileAnimation.Delay = delay;
        TileAnimation.Yoyo = true;
    }
    public int ScreenTileRef { get; }

    public Animation TileAnimation { get; }

    public override void Update(GameTime gameTime)
    {
        TileAnimation.GetNextFrame();
    }

    public void Draw(int xPos, int yPos, Color colour, GameTime gameTime)
    {
        if (!GameEngine.UsingOldGraphics)
        {
            _tileAnimationSpriteSet.Begin(BlendState.AlphaBlend, gameTime);
            _tileAnimationSpriteSet.DrawSprite(TileAnimation.CurrentFrame, xPos, yPos, colour);
            _tileAnimationSpriteSet.End();
        }
    }
}