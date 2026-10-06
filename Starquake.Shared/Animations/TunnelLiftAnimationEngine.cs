// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;
using Starquake.Structure;
using Starquake.World;

namespace Starquake.Animations;

internal class TunnelLiftAnimationEngine
{

    /// <summary>
    ///     How far apart the arrow animations of neighbouring lift shafts are started, in logic
    ///     frames, so that two shafts on the same screen ripple rather than pulsing in step.
    /// </summary>
    private const int ColumnStartStagger = 15;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    private List<TunnelLiftAnimationColumn>? _columns;

    public TunnelLiftAnimationEngine(ResourceManager resources, GameScreenCollection gameScreens, GraphicsSet graphicsSet)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
    }

    public void PerformPostChangeScreenActions()
    {
        Point[]? tunnelLiftArrowLocations;

        if (_graphicsSet.IsRedrawn)
        {
            tunnelLiftArrowLocations = _gameScreens.GetCoordsOfTiles(new[]
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
            tunnelLiftArrowLocations = _gameScreens.GetCoordsOfTiles(new[]
            {
                (int)ActionTiles.TunnelLiftTile,
                (int)ActionTiles.TunnelLiftShaft1,
                (int)ActionTiles.TunnelLiftShaft2,
                (int)ActionTiles.TunnelLiftShaft3
            });
        }

        _columns = null;
        int initialDelayTime = 0;

        if (tunnelLiftArrowLocations != null)
        {
            _columns = new List<TunnelLiftAnimationColumn>();

            for (int a = 0; a < tunnelLiftArrowLocations.Length; a++)
            {
                TunnelLiftAnimationColumn? existingColumn = _columns.Where(col => col.XPosition == tunnelLiftArrowLocations[a].X).FirstOrDefault();

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

    public void Update()
    {
        if (_columns != null && _columns.Count > 0)
        {
            foreach (TunnelLiftAnimationColumn column in _columns)
            {
                column.Update();
            }
        }
    }

    public void Draw(int screenfade)
    {
        if (_columns != null && _columns.Count > 0)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).Begin(BlendState.AlphaBlend);

            foreach (TunnelLiftAnimationColumn column in _columns)
            {
                column.Draw(_resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles), screenfade);
            }

            _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).End();
        }
    }
}
