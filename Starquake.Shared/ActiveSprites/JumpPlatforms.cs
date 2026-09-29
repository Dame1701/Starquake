// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Display;
using Starquake.Engines;
using Starquake.SharedLibs;

namespace Starquake.ActiveSprites;

public class JumpPlatforms
{
    private readonly GameEngine _gameEngine;
    private readonly List<JumpPlatform> _platforms;

    public JumpPlatforms(GameEngine gameEngineRef)
    {
        _gameEngine = gameEngineRef;
        _platforms = new List<JumpPlatform>();
    }

    public int GetNewJumpPlatform(int xPos, int yPos, int width, int height, bool checkCollision)
    {
        var position = JumpPlatform.GetJumpPlatformCoords(xPos, yPos, _gameEngine.GameScreens);

        if (checkCollision)
        {
            if (_gameEngine.GameScreens.CheckRectangles(xPos, position.Y - 64, width, height, true) >= 0)
            {
                // If there is a collision, return a failure int i.e. -1000, needs to be a big number because we can have minus amounts when changing screen
                return -1000;
            }
        }

        JumpPlatform newPlatform = null;

        for (var a = 0; a < _platforms.Count; a++)
        {
            if (_platforms[a].ReadyToAllocate)
            {
                newPlatform = _platforms[a];
                newPlatform.Reset();
                newPlatform.XPos = position.X;
                newPlatform.YPos = position.Y;
                break;
            }
        }

        if (newPlatform == null)
        {
            newPlatform = new JumpPlatform(_gameEngine, _gameEngine.ContentMgr, position);
            _platforms.Add(newPlatform);
        }

        newPlatform.LeftCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.RightCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos + 32, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.PlatformActive = true;
        newPlatform.ReadyToAllocate = false;

        _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.LeftCollisionTile);
        _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.RightCollisionTile);

        // Returns blob's new YPosition
        return newPlatform.YPos - 64;
    }

    public void ResetPlatforms()
    {
        for (var a = 0; a < _platforms.Count; a++)
        {
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].LeftCollisionTile);
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].RightCollisionTile);
            _platforms[a].Reset();
        }
    }

    public void Update(GameScreenCollection gameScreensReference)
    {
        // Update jumpplatform lifetime
        if (_platforms.Count > 0)
        {
            for (var a = 0; a < _platforms.Count; a++)
            {
                if (_platforms[a].LeftCollisionTile != null && _platforms[a].RightCollisionTile != null)
                {
                    if (_platforms[a].Finished)
                    {
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].LeftCollisionTile);
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(_platforms[a].RightCollisionTile);
                        _platforms[a].Reset();
                    }
                    else
                    {
                        _platforms[a].Update();
                    }
                }
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        for (var a = 0; a < _platforms.Count; a++)
        {
            _platforms[a].Draw(gameTime);
        }
    }
}