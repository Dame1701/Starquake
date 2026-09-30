// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.SharedLibs;

namespace Starquake.ActiveSprites;

public class JumpPlatforms
{
    private readonly ContentManager _contentManager;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly List<JumpPlatform> _platforms;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenEffectEngine _screenEffects;

    public JumpPlatforms(
        ResourceManager resources,
        ContentManager content,
        GameScreenCollection gameScreens,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        ScreenEffectEngine screenEffects)
    {
        _resources = resources;
        _contentManager = content;
        _gameScreens = gameScreens;
        _graphicsDeviceManager = graphics;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;
        _screenEffects = screenEffects;
        _platforms = new List<JumpPlatform>();
    }

    public int GetNewJumpPlatform(int xPos, int yPos, int width, int height, bool checkCollision)
    {
        Point position = JumpPlatform.GetJumpPlatformCoords(xPos, yPos, _gameScreens);

        if (checkCollision)
        {
            if (_gameScreens.CheckRectangles(xPos, position.Y - 64, width, height, true) >= 0)
            {
                // If there is a collision, return a failure int i.e. -1000, needs to be a big number because we can have minus amounts when changing screen
                return -1000;
            }
        }

        JumpPlatform? newPlatform = null;

        foreach (JumpPlatform platform in _platforms)
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
            newPlatform = new JumpPlatform(
                _resources,
                _contentManager,
                _graphicsDeviceManager,
                _graphicsSet,
                _randomNumberGenerator,
                _screenEffects,
                position);
            _platforms.Add(newPlatform);
        }

        newPlatform.LeftCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.RightCollisionTile = new CollisionTile((int)CollisionObjectType.JumpPlatform, newPlatform.XPos + 32, newPlatform.YPos, CollisionTileType.EveryoneHits);
        newPlatform.PlatformActive = true;
        newPlatform.ReadyToAllocate = false;

        _gameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.LeftCollisionTile);
        _gameScreens.CurrentGameScreenData.CollisionMap.Add(newPlatform.RightCollisionTile);

        // Returns blob's new YPosition
        return newPlatform.YPos - 64;
    }

    public void ResetPlatforms()
    {
        foreach (JumpPlatform platform in _platforms)
        {
            _gameScreens.CurrentGameScreenData.CollisionMap.Remove(platform.LeftCollisionTile);
            _gameScreens.CurrentGameScreenData.CollisionMap.Remove(platform.RightCollisionTile);
            platform.Reset();
        }
    }

    public void Update(GameScreenCollection gameScreensReference)
    {
        // Update jumpplatform lifetime
        if (_platforms.Count > 0)
        {
            foreach (JumpPlatform platform in _platforms)
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
        foreach (JumpPlatform platform in _platforms)
        {
            platform.Draw(gameTime);
        }
    }
}
