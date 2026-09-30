// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Animations;

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
        foreach (var animTilesList in AnimTilesList)
        {
            if (animTilesList.ScreenTileRef == tileNumber)
            {
                return true;
            }
        }

        return false;
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var animTilesList in AnimTilesList)
        {
            animTilesList.Update(gameTime);
        }
    }

    public void Draw(int tileNumber, int xPos, int yPos, Color colour, GameTime gameTime)
    {
        foreach (var animTilesList in AnimTilesList)
        {
            if (animTilesList.ScreenTileRef == tileNumber)
            {
                animTilesList.Draw(xPos, yPos, colour, gameTime);
            }
        }
    }
}