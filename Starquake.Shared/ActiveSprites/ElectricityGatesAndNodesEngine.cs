// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class ElectricityGatesAndNodesEngine : GameComponentBase
{

    /// <summary>
    ///     The live part of an electricity node, as an inset from the tile it is drawn on. The arc
    ///     hangs below the node itself and does not reach the tile's edges, so the blob can pass under
    ///     one corner of the tile without being struck.
    /// </summary>
    private const int NodeArcLeft = 40;

    /// <inheritdoc cref="NodeArcLeft" />
    private const int NodeArcRight = 88;

    /// <inheritdoc cref="NodeArcLeft" />
    private const int NodeArcTop = 0;

    /// <inheritdoc cref="NodeArcLeft" />
    private const int NodeArcBottom = 32;

    /// <summary>
    ///     The live part of an electricity gate. A gate is wider than a node and its arc sits lower,
    ///     so it has its own inset.
    /// </summary>
    private const int GateArcLeft = 32;

    /// <inheritdoc cref="GateArcLeft" />
    private const int GateArcRight = 96;

    /// <inheritdoc cref="GateArcLeft" />
    private const int GateArcTop = 40;

    /// <inheritdoc cref="GateArcLeft" />
    private const int GateArcBottom = 56;

    /// <summary>
    ///     Where the original artwork's electricity arc is drawn relative to its tile - centred
    ///     across it, and raised or dropped depending on whether it hangs from a node or stands in a
    ///     gate.
    /// </summary>
    private const int OriginalArcXOffset = 32;

    /// <inheritdoc cref="OriginalArcXOffset" />
    private const int OriginalNodeArcYRise = 16;

    /// <inheritdoc cref="OriginalArcXOffset" />
    private const int OriginalGateArcYOffset = 16;

    /// <summary>
    ///     Where the indicator lamps sit on a node, on a gate, and on a conveyer nasty's gate.
    /// </summary>
    private const int NodeLightXOffset = 75;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int NodeLightYRise = 14;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int GateLightXOffset = 78;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int GateLightYOffset = 27;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int ConveyerGateLightXOffset = 33;

    /// <summary>
    ///     The near light of each pair, which sits to the left of the tile's own position rather than
    ///     along it. These were written inline as bare numbers next to the far offsets above.
    /// </summary>
    private const int NodeLightNearXOffset = 12;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int GateLightNearXOffset = 8;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int ConveyerGateLightNearXOffset = 6;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int ArcXOffset = 28;

    /// <summary>
    ///     How the arc that plays over a conveyer nasty is drawn: dropped a little so it meets the
    ///     nasty, and at half size.
    /// </summary>
    private const int ConveyerArcYOffset = 10;

    /// <inheritdoc cref="ConveyerArcYOffset" />
    private const float ConveyerArcScale = 0.5f;
    private readonly Blob _blob;

    private readonly ConveyerNasties _conveyerNasties;
    private readonly Animation _electricityAnimation;
    private readonly Animation _electricityAnimation2;
    private readonly Animation _electricityAnimationOriginal;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private List<SecondLayerTile>? _secondLayerTiles;
    public ElectricityRandomiser[]? ElectricityNodeControllers { get; private set; }

    #region IGameComponent Members

    public event EventHandler? OnBlobStruckByElectricity;

    public ElectricityGatesAndNodesEngine(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        ConveyerNasties convyrNasties)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;

        _conveyerNasties = convyrNasties;

        _electricityAnimationOriginal = new Animation();
        _electricityAnimationOriginal.FirstFrame = 0;
        _electricityAnimationOriginal.CurrentFrame = 0;
        _electricityAnimationOriginal.LastFrame = 3;
        _electricityAnimationOriginal.Yoyo = false;
        _electricityAnimationOriginal.Delay = 1;
        _electricityAnimationOriginal.Repeat = true;
        _electricityAnimationOriginal.AnimateForwards = true;

        _electricityAnimation = new Animation();
        _electricityAnimation.Repeat = true;
        _electricityAnimation.Yoyo = true;
        _electricityAnimation.FirstFrame = 0;
        _electricityAnimation.LastFrame = 9;
        _electricityAnimation.Delay = 1;
        _electricityAnimation.AnimateForwards = true;

        _electricityAnimation2 = new Animation();
        _electricityAnimation2.Repeat = true;
        _electricityAnimation2.Yoyo = false;
        _electricityAnimation2.FirstFrame = 0;
        _electricityAnimation2.LastFrame = 9;
        _electricityAnimation2.Delay = 2;
        _electricityAnimation2.AnimateForwards = false;
    }

    // Turn off electricity on all nodes and gates
    public void Reset()
    {
        if (_secondLayerTiles != null)
        {
            int electricityNodeCounter = 0;

            foreach (SecondLayerTile secondLayerTile in _secondLayerTiles)
            {
                if (secondLayerTile.TileType == TileType.ElectricityGate ||
                    secondLayerTile.TileType == TileType.ElectricityNode)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            ElectricityNodeControllers[electricityNodeCounter].Reset();
                            electricityNodeCounter++;
                        }
                    }
                }
            }
        }
    }

    public void Update()
    {
        if (ElectricityNodeControllers != null)
        {
            int electricityNodeCounter = 0;
            _secondLayerTiles = _gameScreens.GetListOfSecondLayerTiles();

            if (_secondLayerTiles != null)
            {
                foreach (SecondLayerTile secondLayerTile in _secondLayerTiles)
                {

                    if (secondLayerTile.TileType == TileType.ElectricityNode)
                    {
                        if (_blob.BlobXPos + _blob.BlobWidth > ElectricityNodeControllers[electricityNodeCounter].Location.X + NodeArcLeft &&
                            _blob.BlobXPos < ElectricityNodeControllers[electricityNodeCounter].Location.X + NodeArcRight &&
                            _blob.BlobYPos + SpriteMetrics.SpriteHeight > ElectricityNodeControllers[electricityNodeCounter].Location.Y + NodeArcTop &&
                            _blob.BlobYPos < ElectricityNodeControllers[electricityNodeCounter].Location.Y + NodeArcBottom)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (OnBlobStruckByElectricity != null)
                                {
                                    OnBlobStruckByElectricity(this, EventArgs.Empty);
                                }
                            }
                        }
                    }
                    else if (secondLayerTile.TileType == TileType.ElectricityGate)
                    {
                        if (_blob.BlobXPos + _blob.BlobWidth > ElectricityNodeControllers[electricityNodeCounter].Location.X + GateArcLeft &&
                            _blob.BlobXPos < ElectricityNodeControllers[electricityNodeCounter].Location.X + GateArcRight &&
                            _blob.BlobYPos + SpriteMetrics.SpriteHeight > ElectricityNodeControllers[electricityNodeCounter].Location.Y + GateArcTop &&
                            _blob.BlobYPos < ElectricityNodeControllers[electricityNodeCounter].Location.Y + GateArcBottom)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (OnBlobStruckByElectricity != null)
                                {
                                    OnBlobStruckByElectricity(this, EventArgs.Empty);
                                }
                            }
                        }
                    }

                    // Update the nodes
                    ElectricityNodeControllers[electricityNodeCounter].Update();

                    electricityNodeCounter++;
                }
            }

            if (_graphicsSet.IsRedrawn)
            {
                _electricityAnimation.GetNextFrame();
                _electricityAnimation2.GetNextFrame();
            }
            else
            {
                _electricityAnimationOriginal.GetNextFrame();
            }
        }
        else if (_conveyerNasties != null)
        {
            if (_graphicsSet.IsRedrawn)
            {
                _electricityAnimation.GetNextFrame();
                _electricityAnimation2.GetNextFrame();
            }
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // Reset Electricity Node Controller list
        ElectricityNodeControllers = null;
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (_secondLayerTiles != null && _secondLayerTiles.Count > 0)
        {
            int electricityNodeCounter = 0;

            foreach (SecondLayerTile secondLayerTile in _secondLayerTiles)
            {
                if (secondLayerTile.TileType == TileType.ElectricityNode)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (_graphicsSet.IsRedrawn)
                                {
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive);
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(_electricityAnimation.CurrentFrame, secondLayerTile.TileLocation.X + 28, secondLayerTile.TileLocation.Y - 18, Color.White);
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                }
                                else
                                {
                                    _resources.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive);
                                    _resources.GetSpriteCollection("ElectricityOriginal").DrawSprite(_electricityAnimationOriginal.CurrentFrame, secondLayerTile.TileLocation.X + OriginalArcXOffset, secondLayerTile.TileLocation.Y - OriginalNodeArcYRise, Color.White);
                                    _resources.GetSpriteCollection("ElectricityOriginal").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;

                }
                else if (secondLayerTile.TileType == TileType.ElectricityGate)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (_graphicsSet.IsRedrawn)
                                {
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive);
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(_electricityAnimation.CurrentFrame, secondLayerTile.TileLocation.X + 28, secondLayerTile.TileLocation.Y + 14, Color.White);
                                    _resources.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                }
                                else
                                {
                                    _resources.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive);
                                    _resources.GetSpriteCollection("ElectricityOriginal").DrawSprite(_electricityAnimationOriginal.CurrentFrame, secondLayerTile.TileLocation.X + OriginalArcXOffset, secondLayerTile.TileLocation.Y + OriginalGateArcYOffset, Color.White);
                                    _resources.GetSpriteCollection("ElectricityOriginal").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;
                }
            }
        }
    }

    public void DrawUpperLayer()
    {
        // Draw electricity nodes etc bottom layer
        List<SecondLayerTile>? secondLayerTiles = _gameScreens.GetListOfSecondLayerTiles();

        if (secondLayerTiles != null && secondLayerTiles.Count > 0)
        {
            int electricityNodeCounter = 0;

            foreach (SecondLayerTile secondLayerTile in secondLayerTiles)
            {
                if (secondLayerTile.TileType is not (TileType.ElectricityNode or TileType.ElectricityGate))
                {
                    continue;
                }

                if (NodeIsLit(electricityNodeCounter))
                {
                    if (secondLayerTile.TileType == TileType.ElectricityNode)
                    {
                        DrawLitNode(secondLayerTile.TileLocation);
                    }
                    else
                    {
                        DrawLitGate(secondLayerTile.TileLocation);
                    }
                }

                electricityNodeCounter++;
            }

            ElectricityNodeControllers ??= CreateNodeControllers(electricityNodeCounter, secondLayerTiles);
        }

        if (_conveyerNasties != null)
        {
            DrawConveyerNastyArcs();
        }
    }

    /// <summary>
    ///     Whether the node at this position along the screen's second layer is currently switched on.
    ///     The old graphics have no lights or arcs at all, so they count as unlit.
    /// </summary>
    private bool NodeIsLit(int electricityNodeCounter)
        => ElectricityNodeControllers != null &&
           ElectricityNodeControllers.Length > electricityNodeCounter &&
           ElectricityNodeControllers[electricityNodeCounter].ElectricityOn &&
           _graphicsSet.IsRedrawn;

    private void DrawLitNode(Point tileLocation)
    {
        DrawLightPair(
            "NodeLights",
            new Point(tileLocation.X - NodeLightNearXOffset, tileLocation.Y - NodeLightYRise),
            new Point(tileLocation.X + NodeLightXOffset, tileLocation.Y - NodeLightYRise));

        DrawArc(
            "ElectricityNodeElectricity2",
            _electricityAnimation2.CurrentFrame,
            new Point(tileLocation.X + ArcXOffset, tileLocation.Y - NodeLightYRise));
    }

    /// <summary>
    ///     A gate draws its arc before its lights and a node draws it after, which is the only thing
    ///     that stops the two sharing one method.
    /// </summary>
    private void DrawLitGate(Point tileLocation)
    {
        DrawArc(
            "ElectricityNodeElectricity2",
            _electricityAnimation2.CurrentFrame,
            new Point(tileLocation.X + ArcXOffset, tileLocation.Y + NodeLightYRise));

        DrawLightPair(
            "GateLights",
            new Point(tileLocation.X + GateLightNearXOffset, tileLocation.Y + GateLightYOffset),
            new Point(tileLocation.X + GateLightXOffset, tileLocation.Y + GateLightYOffset));
    }

    private void DrawConveyerNastyArcs()
    {
        foreach (ConveyerNasty nasty in _conveyerNasties!.Nasties)
        {
            if (!nasty.NastyActive)
            {
                continue;
            }

            DrawLightPair(
                "GateLights",
                new Point(nasty.XLocation - ConveyerGateLightNearXOffset, nasty.YLocation + GateLightYOffset),
                new Point(nasty.XLocation + ConveyerGateLightXOffset, nasty.YLocation + GateLightYOffset));

            _resources.GetSpriteCollection("ElectricityNodeElectricity1")
                .Begin(BlendState.Additive);
            _resources.GetSpriteCollection("ElectricityNodeElectricity1")
                .DrawSprite(
                    _electricityAnimation.CurrentFrame,
                    nasty.XLocation,
                    nasty.YLocation + ConveyerArcYOffset,
                    Color.White,
                    ConveyerArcScale,
                    0);
            _resources.GetSpriteCollection("ElectricityNodeElectricity1").End();
        }
    }

    /// <summary>
    ///     The two lights at either end of a node or a gate. Each is drawn blue and then cyan over the
    ///     top, which is what gives it a hot centre, and all three callers wanted the same four draws.
    /// </summary>
    private void DrawLightPair(string spriteName, Point left, Point right)
    {
        SpriteCollection lights = _resources.GetSpriteCollection(spriteName);

        lights.Begin(BlendState.Additive);

        foreach (Point at in new[]
                 {
                     left,
                     right
                 })
        {
            lights.DrawSprite(0, at.X, at.Y, Color.Blue);
            lights.DrawSprite(0, at.X, at.Y, Color.Cyan);
        }

        lights.End();
    }

    private void DrawArc(string spriteName, int frame, Point at)
    {
        SpriteCollection arc = _resources.GetSpriteCollection(spriteName);

        arc.Begin(BlendState.Additive);
        arc.DrawSprite(frame, at.X, at.Y, Color.White);
        arc.End();
    }

    private ElectricityRandomiser[] CreateNodeControllers(int count, List<SecondLayerTile> secondLayerTiles)
    {
        var controllers = new ElectricityRandomiser[count];

        for (int a = 0; a < count; a++)
        {
            controllers[a] = new ElectricityRandomiser(_resources, _randomNumberGenerator)
            {
                Location = new Point(secondLayerTiles[a].TileLocation.X, secondLayerTiles[a].TileLocation.Y)
            };
        }

        return controllers;
    }

    #endregion

}
