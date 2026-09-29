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