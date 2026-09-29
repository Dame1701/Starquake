// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Animations;

public class TunnelLiftAnimationEngine : GameComponentBase
{
    /// <summary>
    ///     How far apart the arrow animations of neighbouring lift shafts are started, in logic
    ///     frames, so that two shafts on the same screen ripple rather than pulsing in step.
    /// </summary>
    private const int ColumnStartStagger = 15;

    private List<TunnelLiftAnimationColumn> _columns;

    public TunnelLiftAnimationEngine(GameEngine engine)
        : base(engine)
    {
        GameEngine = engine;
    }

    public void PerformPostChangeScreenActions()
    {
        Point[] tunnelLiftArrowLocations = null;

        if (!GameEngine.UsingOldGraphics)
        {
            tunnelLiftArrowLocations = GameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                (int)ActionTiles.TunnelLiftTopAlternate,
                (int)ActionTiles.TunnelLiftTop1,
                (int)ActionTiles.TunnelLiftTile,
                (int)ActionTiles.TunnelLiftShaft1,
                (int)ActionTiles.TunnelLiftShaft2,
                (int)ActionTiles.TunnelLiftShaft3,
                (int)ActionTiles.TunnelLiftTop2,
                (int)ActionTiles.TunnelLiftTop3
            });
        }
        else
        {
            tunnelLiftArrowLocations = GameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                (int)ActionTiles.TunnelLiftTile,
                (int)ActionTiles.TunnelLiftShaft1,
                (int)ActionTiles.TunnelLiftShaft2,
                (int)ActionTiles.TunnelLiftShaft3
            });
        }

        _columns = null;
        var initialDelayTime = 0;

        if (tunnelLiftArrowLocations != null)
        {
            _columns = new List<TunnelLiftAnimationColumn>();

            for (var a = 0; a < tunnelLiftArrowLocations.Length; a++)
            {
                var existingColumn = _columns.Where(col => col.XPosition == tunnelLiftArrowLocations[a].X).FirstOrDefault();

                if (existingColumn == null)
                {
                    existingColumn = new TunnelLiftAnimationColumn(initialDelayTime);
                    initialDelayTime += ColumnStartStagger;
                    _columns.Add(existingColumn);
                }

                existingColumn.AddSection(tunnelLiftArrowLocations[a]);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_columns != null && _columns.Count > 0)
        {
            for (var a = 0; a < _columns.Count; a++)
            {
                _columns[a].Update();
            }
        }
    }

    public override void Draw(GameTime gameTime, int screenfade)
    {
        if (_columns != null && _columns.Count > 0)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);

            for (var a = 0; a < _columns.Count; a++)
            {
                _columns[a].Draw(GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName), screenfade);
            }

            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();
        }
    }
}