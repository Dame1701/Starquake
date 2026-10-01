// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.SharedLibs;

namespace Starquake.ActiveSprites;

internal class JumpPlatforms
{
    /// <summary>
    ///     What <see cref="GetNewJumpPlatform" /> answers when there is no room for one. It has to be
    ///     well below any real answer rather than -1, because the position it normally returns can
    ///     itself be negative while the blob is crossing between screens.
    /// </summary>
    public const int NoRoomForPlatform = -1000;

    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly List<JumpPlatform> _platforms;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenEffectEngine _screenEffects;

    public JumpPlatforms(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsDeviceManager graphics,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        ScreenEffectEngine screenEffects)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsDeviceManager = graphics;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;
        _screenEffects = screenEffects;
        _platforms = new List<JumpPlatform>();
    }

    public int GetNewJumpPlatform(int xPos, int yPos, int width, int height, bool checkCollision)
    {
        Point position = JumpPlatform.GetJumpPlatformCoords(xPos, yPos);

        if (checkCollision)
        {
            if (_gameScreens.CheckRectangles(xPos, position.Y - 64, width, height, true) >= 0)
            {
                return NoRoomForPlatform;
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
        // Update jump platform lifetime
        if (_platforms.Count > 0)
        {
            foreach (JumpPlatform platform in _platforms)
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

    public void Draw()
    {
        foreach (JumpPlatform platform in _platforms)
        {
            platform.Draw();
        }
    }
}
