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
using Starquake.Structure;

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
                    // Make colour initially invisible
                    colourAnimation.TransitionColour.A = 0;
                    colourAnimation.Running = true;
                    colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                    colourAnimation.Stopping = false;
                    colourAnimation.LoopCount = loopCount;

                    // Reset the loop counter
                    colourAnimation.LoopCounter = 0;
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                if (!colourAnimation.Running)
                {
                    // Make colour initially invisible
                    colourAnimation.TransitionColour = transitionColour;
                    colourAnimation.TransitionColour.A = 0;
                    colourAnimation.Running = true;
                    colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                    colourAnimation.Stopping = false;
                    colourAnimation.LoopCount = loopCount;

                    // Reset the loop counter
                    colourAnimation.LoopCounter = 0;
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
                    // Make colour initially invisible
                    colourAnimation.TransitionColour = transitionColour;
                    colourAnimation.TransitionColour.A = 0;
                    colourAnimation.Intensity = intensity;
                    colourAnimation.Running = true;
                    colourAnimation.Direction = ColourAnimationDirection.FadeUp;
                    colourAnimation.Stopping = false;
                    colourAnimation.LoopCount = loopCount;

                    // Reset the loop counter
                    colourAnimation.LoopCounter = 0;
                }
            }
        }
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
                    // Schedule a stop
                    _colourAnimationsRegistered[a].Stopping = true;
                }
            }
        }
    }

    public void ChangeSpeed(int tileNo, int speed)
    {
        foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                colourAnimation.Speed = speed;
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
                        value = value + colourAnimation.Speed;

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
                        value = value - colourAnimation.Speed;

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

    public void Draw()
    {
        SpriteCollection lowerTiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        lowerTiles.Begin(BlendState.Additive);

        for (int y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (int x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (int b = 0; b < colourAnimation.Intensity; b++)
                            {
                                lowerTiles.DrawSprite(
                                    colourAnimation.TileNo,
                                    x * TileMetrics.TileWidth,
                                    y * TileMetrics.TileHeight,
                                    colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        lowerTiles.End();
    }

    public void Draw(SpriteCollection sprites)
    {
        sprites.Begin(BlendState.Additive);

        for (int y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (int x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running &&
                        colourAnimation.TileNo >= 0)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (int b = 0; b < colourAnimation.Intensity; b++)
                            {
                                sprites.DrawSprite(
                                    colourAnimation.TileNo,
                                    x * TileMetrics.TileWidth,
                                    y * TileMetrics.TileHeight,
                                    colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        sprites.End();
    }

    public void DrawUpperLayer()
    {
        SpriteCollection upperTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);

        upperTiles.Begin(BlendState.Additive);

        for (int y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (int x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (ColourFaderAnimation colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running &&
                        colourAnimation.UpperTileToDraw >= 0)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (int b = 0; b < colourAnimation.Intensity; b++)
                            {
                                upperTiles.DrawSprite(
                                    colourAnimation.UpperTileToDraw,
                                    x * TileMetrics.TileWidth,
                                    y * TileMetrics.TileHeight,
                                    colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        upperTiles.End();
    }
}
