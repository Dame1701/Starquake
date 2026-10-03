// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class ElectricityGatesAndNodesEngine
{

    /// <summary>
    ///     The live part of an electricity node, as an inset from the tile it is drawn on. The arc
    ///     hangs below the node itself and does not reach the tile's edges, so Blob can pass under
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
    ///     Where the indicator lamps sit on a node, on a gate, and on a conveyor nasty's gate.
    /// </summary>
    private const int NodeLightXOffset = 75;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int NodeLightYRise = 14;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int GateLightXOffset = 78;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int GateLightYOffset = 27;

    /// <inheritdoc cref="NodeLightXOffset" />
    private const int ConveyorGateLightXOffset = 33;

    /// <summary>
    ///     The near light of each pair, which sits to the left of the tile's own position rather than
    ///     along it. These were written inline as bare numbers next to the far offsets above.
    /// </summary>
    private const int NodeLightNearXOffset = 12;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int GateLightNearXOffset = 8;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int ConveyorGateLightNearXOffset = 6;

    /// <inheritdoc cref="NodeLightNearXOffset" />
    private const int ArcXOffset = 28;

    /// <summary>
    ///     How the arc that plays over a conveyor nasty is drawn: dropped a little so it meets the
    ///     nasty, and at half size.
    /// </summary>
    private const int ConveyorArcYOffset = 10;

    /// <inheritdoc cref="ConveyorArcYOffset" />
    private const float ConveyorArcScale = 0.5f;
    private readonly Blob _blob;

    private readonly ConveyorNasties _conveyorNasties;
    private readonly Animation _electricityAnimation;
    private readonly Animation _electricityAnimation2;
    private readonly Animation _electricityAnimationOriginal;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private List<SecondLayerTile>? _secondLayerTiles;
    public ElectricityRandomiser[]? ElectricityNodeControllers { get; private set; }

    public event EventHandler? OnBlobStruckByElectricity;

    public ElectricityGatesAndNodesEngine(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        ConveyorNasties conveyorNasties)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;

        _conveyorNasties = conveyorNasties;

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
        else
        {
            if (_graphicsSet.IsRedrawn)
            {
                _electricityAnimation.GetNextFrame();
                _electricityAnimation2.GetNextFrame();
            }
        }
    }

    /// <summary>
    ///     Drops the controllers, so arriving on a screen builds a set for whatever nodes that screen
    ///     has rather than carrying the last screen's over.
    /// </summary>
    public void PerformPostChangeScreenActions()
    {
        ElectricityNodeControllers = null;
    }

    public void Draw()
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
                                    SpriteCollection electricityNodeElectricity1 = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);
                                    electricityNodeElectricity1.Begin(BlendState.Additive);
                                    electricityNodeElectricity1.DrawSprite(_electricityAnimation.CurrentFrame, secondLayerTile.TileLocation.X + 28, secondLayerTile.TileLocation.Y - 18, Color.White);
                                    electricityNodeElectricity1.End();
                                }
                                else
                                {
                                    SpriteCollection electricityOriginal = _resources.GetSpriteCollection(Assets.Graphics.ElectricityOriginal);
                                    electricityOriginal.Begin(BlendState.Additive);
                                    electricityOriginal.DrawSprite(_electricityAnimationOriginal.CurrentFrame, secondLayerTile.TileLocation.X + OriginalArcXOffset, secondLayerTile.TileLocation.Y - OriginalNodeArcYRise, Color.White);
                                    electricityOriginal.End();
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
                                    SpriteCollection electricityNodeElectricity1 = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);
                                    electricityNodeElectricity1.Begin(BlendState.Additive);
                                    electricityNodeElectricity1.DrawSprite(_electricityAnimation.CurrentFrame, secondLayerTile.TileLocation.X + 28, secondLayerTile.TileLocation.Y + 14, Color.White);
                                    electricityNodeElectricity1.End();
                                }
                                else
                                {
                                    SpriteCollection electricityOriginal = _resources.GetSpriteCollection(Assets.Graphics.ElectricityOriginal);
                                    electricityOriginal.Begin(BlendState.Additive);
                                    electricityOriginal.DrawSprite(_electricityAnimationOriginal.CurrentFrame, secondLayerTile.TileLocation.X + OriginalArcXOffset, secondLayerTile.TileLocation.Y + OriginalGateArcYOffset, Color.White);
                                    electricityOriginal.End();
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

        DrawConveyorNastyArcs();
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

    private void DrawConveyorNastyArcs()
    {
        foreach (ConveyorNasty nasty in _conveyorNasties.Nasties)
        {
            if (!nasty.NastyActive)
            {
                continue;
            }

            DrawLightPair(
                "GateLights",
                new Point(nasty.XLocation - ConveyorGateLightNearXOffset, nasty.YLocation + GateLightYOffset),
                new Point(nasty.XLocation + ConveyorGateLightXOffset, nasty.YLocation + GateLightYOffset));

            SpriteCollection electricityNodeElectricity1 = _resources.GetSpriteCollection(Assets.Graphics.ElectricityNodeElectricity1);
            electricityNodeElectricity1.Begin(BlendState.Additive);
            electricityNodeElectricity1.DrawSprite(
                _electricityAnimation.CurrentFrame,
                nasty.XLocation,
                nasty.YLocation + ConveyorArcYOffset,
                Color.White,
                ConveyorArcScale,
                0);
            electricityNodeElectricity1.End();
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

}
