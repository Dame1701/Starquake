// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.SharedLibs;

namespace Starquake.ActiveSprites;

internal class JumpPlatforms : IDynamicCollisionCells
{
    /// <summary>
    ///     What <see cref="GetNewJumpPlatform" /> answers when there is no room for one. It has to be
    ///     well below any real answer rather than -1, because the position it normally returns can
    ///     itself be negative while Blob is crossing between screens.
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

        // The platforms' cells are not on the screen's tile map, so the collision map has to be able
        // to ask for them again every time it is rebuilt - see IDynamicCollisionCells.
        _gameScreens.Collisions.RegisterDynamicCells(this);
    }

    /// <summary>
    ///     The cells of every platform still standing on one screen. A platform that has dissolved or
    ///     has not been handed out is not active and owns nothing.
    /// </summary>
    public IEnumerable<CollisionTile> CellsOnScreen(int screenNumber)
    {
        foreach (JumpPlatform platform in _platforms)
        {
            if (!platform.PlatformActive || platform.ScreenNumber != screenNumber)
            {
                continue;
            }

            yield return platform.LeftCollisionTile;
            yield return platform.RightCollisionTile;
        }
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
        newPlatform.ScreenNumber = _gameScreens.ScreenNumber;

        // Into the map as it stands, so Blob can be put on the platform this same frame. A
        // rebuild asks CellsOnScreen for them again rather than keeping these two.
        _gameScreens.Collisions.Add(newPlatform.LeftCollisionTile);
        _gameScreens.Collisions.Add(newPlatform.RightCollisionTile);

        return newPlatform.YPos - SpriteMetrics.SpriteHeight;
    }

    public void ResetPlatforms()
    {
        foreach (JumpPlatform platform in _platforms)
        {
            RetirePlatform(platform);
        }
    }

    public void Update()
    {
        if (_platforms.Count > 0)
        {
            foreach (JumpPlatform platform in _platforms)
            {
                if (platform.Finished)
                {
                    RetirePlatform(platform);
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

    /// <summary>
    ///     Takes a platform out of use and its cells out of the map. The removal only reaches the
    ///     screen being shown, which is the only one being asked about collision; cells left behind on
    ///     a screen Blob has since walked off go when that screen is next rebuilt, and
    ///     <see cref="CellsOnScreen" /> stops reporting them the moment the platform is reset.
    /// </summary>
    private void RetirePlatform(JumpPlatform platform)
    {
        _gameScreens.Collisions.Remove(platform.LeftCollisionTile);
        _gameScreens.Collisions.Remove(platform.RightCollisionTile);
        platform.Reset();
    }
}
