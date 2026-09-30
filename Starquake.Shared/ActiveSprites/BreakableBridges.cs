// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Starquake.Engines;

namespace Starquake.ActiveSprites;

public class BreakableBridges
{
    private readonly List<BreakableBridge> _bridges;
    private readonly GameEngine _gameEngineReference;

    public BreakableBridges(GameEngine gameEngineReference)
    {
        _gameEngineReference = gameEngineReference;
        _bridges = new List<BreakableBridge>();
    }

    public void AddBridge(int tileLocationX, int tileLocationY)
    {
        _bridges.Add(new BreakableBridge(tileLocationX, tileLocationY, _gameEngineReference));
    }

    public void AddBridge(Point tileLocation)
    {
        _bridges.Add(new BreakableBridge(tileLocation.X, tileLocation.Y, _gameEngineReference));
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
                    _gameEngineReference.GameScreens.UpdateCollisionMap(false);

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