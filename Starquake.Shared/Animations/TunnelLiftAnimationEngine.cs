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
                396,
                407,
                408,
                409,
                412,
                413,
                414,
                415
            });
        }
        else
        {
            tunnelLiftArrowLocations = GameEngine.GameScreens.GetCoordsOfTiles(new[]
            {
                408,
                409,
                412,
                413
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
                    initialDelayTime += 15;
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