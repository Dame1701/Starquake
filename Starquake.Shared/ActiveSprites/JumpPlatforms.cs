// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
#nullable enable
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

        JumpPlatform? newPlatform = null;

        foreach (var platform in _platforms)
        {
            if (platform.ReadyToAllocate)
            {
                newPlatform = platform;
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
        foreach (var platform in _platforms)
        {
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(platform.LeftCollisionTile);
            _gameEngine.GameScreens.CurrentGameScreenData.CollisionMap.Remove(platform.RightCollisionTile);
            platform.Reset();
        }
    }

    public void Update(GameScreenCollection gameScreensReference)
    {
        // Update jumpplatform lifetime
        if (_platforms.Count > 0)
        {
            foreach (var platform in _platforms)
            {
                if (platform.LeftCollisionTile != null && platform.RightCollisionTile != null)
                {
                    if (platform.Finished)
                    {
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(platform.LeftCollisionTile);
                        gameScreensReference.CurrentGameScreenData.CollisionMap.Remove(platform.RightCollisionTile);
                        platform.Reset();
                    }
                    else
                    {
                        platform.Update();
                    }
                }
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (var platform in _platforms)
        {
            platform.Draw(gameTime);
        }
    }
}