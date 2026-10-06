// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;

namespace Starquake.ContentManagement;

internal class AnimTiles
{
    public AnimTiles()
    {
        AnimTilesList = [];
    }
    public List<AnimTile> AnimTilesList { get; }

    public void Update()
    {
        foreach (AnimTile animTilesList in AnimTilesList)
        {
            animTilesList.Update();
        }
    }

    public void Draw(int tileNumber, int xPos, int yPos, Color colour)
    {
        foreach (AnimTile animTilesList in AnimTilesList)
        {
            if (animTilesList.ScreenTileRef == tileNumber)
            {
                animTilesList.Draw(xPos, yPos, colour);
            }
        }
    }
}
