// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Sprites;

namespace Starquake.Animations;

internal class ColourAnimationEngine
{

    private readonly List<ColourFaderAnimation> _colourAnimationsRegistered;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    public ColourAnimationEngine(ResourceManager resources, GameScreenCollection gameScreens, GraphicsSet graphicsSet)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;

        _colourAnimationsRegistered = new List<ColourFaderAnimation>();
    }

    public void RegisterTile(ColourFaderAnimation faderAnimation)
    {
        _colourAnimationsRegistered.Add(faderAnimation);
    }

    public void Start(int tileNo, int loopCount)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                if (!colourAnimation.Running)
                {
                    Begin(colourAnimation, loopCount);
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour, int intensity)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                if (!colourAnimation.Running)
                {
                    colourAnimation.TransitionColour = transitionColour;
                    colourAnimation.Intensity = intensity;

                    Begin(colourAnimation, loopCount);
                }
            }
        }
    }

    /// <summary>
    ///     Starts one registered animation from the top: invisible, fading up, with its loop count
    ///     reset. The transition colour's alpha is zeroed here rather than by the caller, so an
    ///     animation always begins invisible whichever overload asked for it.
    /// </summary>
    private static void Begin(ColourFaderAnimation colourAnimation, int loopCount)
    {
        colourAnimation.TransitionColour.A = 0;
        colourAnimation.Running = true;
        colourAnimation.Direction = ColourAnimationDirection.FadeUp;
        colourAnimation.Stopping = false;
        colourAnimation.LoopCount = loopCount;
        colourAnimation.LoopCounter = 0;
    }

    public void Stop(int tileNo)
    {
        for (int a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                if (_colourAnimationsRegistered[a].Running &&
                    !_colourAnimationsRegistered[a].Stopping)
                {
                    _colourAnimationsRegistered[a].Stopping = true;
                }
            }
        }
    }

    public void Update()
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.Running)
            {
                if (colourAnimation.Direction == ColourAnimationDirection.FadeUp)
                {
                    if (colourAnimation.TransitionColour.A < ColourLevels.Full)
                    {
                        int value = colourAnimation.TransitionColour.A;
                        value += colourAnimation.Speed;

                        if (value > ColourLevels.Full)
                        {
                            value = ColourLevels.Full;
                        }

                        colourAnimation.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        colourAnimation.Direction = ColourAnimationDirection.FadeDown;
                    }
                }
                else if (colourAnimation.Direction == ColourAnimationDirection.FadeDown)
                {
                    if (colourAnimation.TransitionColour.A > 0)
                    {
                        int value = colourAnimation.TransitionColour.A;
                        value -= colourAnimation.Speed;

                        if (value < 0)
                        {
                            value = 0;
                        }

                        colourAnimation.TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        if (!colourAnimation.Stopping &&
                            colourAnimation.LoopCounter < colourAnimation.LoopCount - 1)
                        {
                            if (colourAnimation.LoopCount >= 0)
                            {
                                colourAnimation.LoopCounter++;
                            }

                            colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                        }
                        else
                        {
                            if (colourAnimation.Stopping ||
                                colourAnimation.LoopCount >= 0)
                            {
                                colourAnimation.Running = false;
                                colourAnimation.LoopCounter = 0;
                            }
                            else
                            {
                                colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                            }
                        }
                    }
                }
            }
        }
    }

    public void Draw() => DrawLayer(_graphicsSet.Names.LowerScreenTiles, false);

    public void DrawUpperLayer() => DrawLayer(_graphicsSet.Names.UpperScreenTiles, true);

    /// <summary>
    ///     Draws every pulsing tile on the screen, from one sheet, as one batch. The lower layer
    ///     repaints the tile that is already there; the upper one lays a second tile over it, and a
    ///     pulse that has no second tile is skipped - which is the only difference between the two
    ///     passes, and the whole of why they were written out twice.
    /// </summary>
    private void DrawLayer(string sheetName, bool upperLayer)
    {
        SpriteCollection tiles = _resources.GetSpriteCollection(sheetName);

        tiles.Begin(BlendState.Additive);

        for (int y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (int x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
                {
                    if (!colourAnimation.Running)
                    {
                        continue;
                    }

                    int tileToDraw = upperLayer ? colourAnimation.UpperTileToDraw : colourAnimation.TileNo;

                    if (upperLayer && tileToDraw < 0)
                    {
                        continue;
                    }

                    if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage != colourAnimation.TileNo)
                    {
                        continue;
                    }

                    DrawLayeredUp(tiles, tileToDraw, x, y, colourAnimation);
                }
            }
        }

        tiles.End();
    }

    /// <summary>
    ///     One pulsing tile, drawn over itself as many times as its intensity asks for. The blend is
    ///     additive, so each pass deepens the shade rather than replacing it.
    /// </summary>
    private static void DrawLayeredUp(
        SpriteCollection tiles,
        int tileToDraw,
        int x,
        int y,
        ColourFaderAnimation colourAnimation)
    {
        for (int layer = 0; layer < colourAnimation.Intensity; layer++)
        {
            tiles.DrawSprite(
                tileToDraw,
                x * TileMetrics.TileWidth,
                y * TileMetrics.TileHeight,
                colourAnimation.TransitionColour);
        }
    }
}
