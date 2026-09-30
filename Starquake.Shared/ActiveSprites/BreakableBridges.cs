// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines.ScreenEffects;
using Starquake.Engines;

namespace Starquake.ActiveSprites;

public class BreakableBridges
{
    private readonly List<BreakableBridge> _bridges;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsDeviceManager _graphicsDeviceManager;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenEffectEngine _screenEffects;

    public BreakableBridges(
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
        _bridges = new List<BreakableBridge>();
    }

    public void AddBridge(int tileLocationX, int tileLocationY)
    {
        _bridges.Add(new BreakableBridge(tileLocationX, tileLocationY, _resources, _graphicsDeviceManager, _graphicsSet, _randomNumberGenerator, _screenEffects));
    }

    public void AddBridge(Point tileLocation)
    {
        _bridges.Add(new BreakableBridge(tileLocation.X, tileLocation.Y, _resources, _graphicsDeviceManager, _graphicsSet, _randomNumberGenerator, _screenEffects));
    }

    public void Update()
    {
        if (_bridges.Count > 0)
        {
            for (var a = 0; a < _bridges.Count; a++)
            {
                if (!_bridges[a].Finished)
                {
                    _bridges[a].Update();
                }
                else
                {
                    // Update the collision map to remove the bridge collision once we've finished dissolving
                    _gameScreens.UpdateCollisionMap(false);

                    _bridges.Remove(_bridges[a]);
                    a--;
                }
            }
        }
    }

    public void Reset()
    {
        _bridges.Clear();
    }

    public void Draw(GameTime gameTime)
    {
        if (_bridges.Count > 0)
        {
            foreach (var bridge in _bridges)
            {
                bridge.Draw(gameTime);
            }
        }
    }
}