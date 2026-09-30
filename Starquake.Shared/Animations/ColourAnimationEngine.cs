// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Animations;

public class ColourAnimationEngine : GameComponentBase
{
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    private readonly List<ColourFaderAnimation> _colourAnimationsRegistered;

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
        foreach (var colourAnimation in _colourAnimationsRegistered)
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
        foreach (var colourAnimation in _colourAnimationsRegistered)
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
        foreach (var colourAnimation in _colourAnimationsRegistered)
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
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
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
        foreach (var colourAnimation in _colourAnimationsRegistered)
        {
            if (colourAnimation.TileNo == tileNo)
            {
                colourAnimation.Speed = speed;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        foreach (var colourAnimation in _colourAnimationsRegistered)
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

    public void Draw(GameTime gameTime, byte screenFade)
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (var x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (var colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < colourAnimation.Intensity; b++)
                            {
                                _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).DrawSprite(colourAnimation.TileNo, x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth,
                                    y * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteHeight, colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();
    }

    public void Draw(SpriteCollection sprites, GameTime gameTime, byte screenFade)
    {
        sprites.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (var x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (var colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running &&
                        colourAnimation.TileNo >= 0)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < colourAnimation.Intensity; b++)
                            {
                                sprites.DrawSprite(colourAnimation.TileNo, x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth,
                                    y * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteHeight, colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        sprites.End();
    }

    public void DrawUpperLayer(GameTime gameTime, byte screenFade)
    {
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < _gameScreens.TilesDown; y++)
        {
            for (var x = 0; x < _gameScreens.TilesAcross; x++)
            {
                foreach (var colourAnimation in _colourAnimationsRegistered)
                {
                    if (colourAnimation.Running &&
                        colourAnimation.UpperTileToDraw >= 0)
                    {
                        if (_gameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == colourAnimation.TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < colourAnimation.Intensity; b++)
                            {
                                _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).DrawSprite(colourAnimation.UpperTileToDraw, x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth,
                                    y * _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).SpriteHeight, colourAnimation.TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles).End();
    }
}