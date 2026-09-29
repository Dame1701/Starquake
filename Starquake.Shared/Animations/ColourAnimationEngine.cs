// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Animations;

public class ColourAnimationEngine : GameComponentBase
{
    private readonly List<ColourFaderAnimation> _colourAnimationsRegistered;

    public ColourAnimationEngine(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        _colourAnimationsRegistered = new List<ColourFaderAnimation>();
    }

    public void RegisterTile(ColourFaderAnimation faderAnimation)
    {
        _colourAnimationsRegistered.Add(faderAnimation);
    }

    public void Start(int tileNo, int loopCount)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour = transitionColour;
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
                }
            }
        }
    }

    public void Start(int tileNo, int loopCount, Color transitionColour, int intensity)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour = transitionColour;
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Intensity = intensity;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
                }
            }
        }
    }

    public void Start(string name, int loopCount)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Name == name)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
                }
            }
        }
    }

    public void Start(string name, int loopCount, Color transitionColour)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Name == name)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour = transitionColour;
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
                }
            }
        }
    }

    public void Start(string name, int loopCount, Color transitionColour, int intensity)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Name == name)
            {
                if (!_colourAnimationsRegistered[a].Running)
                {
                    // Make colour initially invisible
                    _colourAnimationsRegistered[a].TransitionColour = transitionColour;
                    _colourAnimationsRegistered[a].TransitionColour.A = 0;
                    _colourAnimationsRegistered[a].Intensity = intensity;
                    _colourAnimationsRegistered[a].Running = true;
                    _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                    _colourAnimationsRegistered[a].Stopping = false;
                    _colourAnimationsRegistered[a].LoopCount = loopCount;

                    // Reset the loop counter
                    _colourAnimationsRegistered[a].LoopCounter = 0;
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

    public void Stop(string name)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Name == name)
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
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].TileNo == tileNo)
            {
                _colourAnimationsRegistered[a].Speed = speed;
            }
        }
    }

    public void ChangeSpeed(string name, int speed)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Name == name)
            {
                _colourAnimationsRegistered[a].Speed = speed;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
        {
            if (_colourAnimationsRegistered[a].Running)
            {
                if (_colourAnimationsRegistered[a].Direction == ColourAnimationDirection.FadeUp)
                {
                    if (_colourAnimationsRegistered[a].TransitionColour.A < 255)
                    {
                        int value = _colourAnimationsRegistered[a].TransitionColour.A;
                        value = value + _colourAnimationsRegistered[a].Speed;

                        if (value > 255)
                        {
                            value = 255;
                        }

                        _colourAnimationsRegistered[a].TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeDown;
                    }
                }
                else if (_colourAnimationsRegistered[a].Direction == ColourAnimationDirection.FadeDown)
                {
                    if (_colourAnimationsRegistered[a].TransitionColour.A > 0)
                    {
                        int value = _colourAnimationsRegistered[a].TransitionColour.A;
                        value = value - _colourAnimationsRegistered[a].Speed;

                        if (value < 0)
                        {
                            value = 0;
                        }

                        _colourAnimationsRegistered[a].TransitionColour.A = (byte)value;
                    }
                    else
                    {
                        if (!_colourAnimationsRegistered[a].Stopping &&
                            _colourAnimationsRegistered[a].LoopCounter < _colourAnimationsRegistered[a].LoopCount - 1)
                        {
                            if (_colourAnimationsRegistered[a].LoopCount >= 0)
                            {
                                _colourAnimationsRegistered[a].LoopCounter++;
                            }

                            _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                        }
                        else
                        {
                            if (_colourAnimationsRegistered[a].Stopping ||
                                _colourAnimationsRegistered[a].LoopCount >= 0)
                            {
                                _colourAnimationsRegistered[a].Running = false;
                                _colourAnimationsRegistered[a].LoopCounter = 0;
                            }
                            else
                            {
                                _colourAnimationsRegistered[a].Direction = ColourAnimationDirection.FadeUp;
                            }
                        }
                    }
                }
            }
        }
    }

    public void Draw(GameTime gameTime, byte screenFade)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < GameEngine.GameScreens.TilesDown; y++)
        {
            for (var x = 0; x < GameEngine.GameScreens.TilesAcross; x++)
            {
                for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
                {
                    if (_colourAnimationsRegistered[a].Running)
                    {
                        if (GameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == _colourAnimationsRegistered[a].TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < _colourAnimationsRegistered[a].Intensity; b++)
                            {
                                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).DrawSprite(_colourAnimationsRegistered[a].TileNo, x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                                    y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, _colourAnimationsRegistered[a].TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).End();
    }

    public void Draw(SpriteCollection sprites, GameTime gameTime, byte screenFade)
    {
        sprites.Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < GameEngine.GameScreens.TilesDown; y++)
        {
            for (var x = 0; x < GameEngine.GameScreens.TilesAcross; x++)
            {
                for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
                {
                    if (_colourAnimationsRegistered[a].Running &&
                        _colourAnimationsRegistered[a].TileNo >= 0)
                    {
                        if (GameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == _colourAnimationsRegistered[a].TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < _colourAnimationsRegistered[a].Intensity; b++)
                            {
                                sprites.DrawSprite(_colourAnimationsRegistered[a].TileNo, x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                                    y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, _colourAnimationsRegistered[a].TransitionColour);
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
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);

        for (var y = 0; y < GameEngine.GameScreens.TilesDown; y++)
        {
            for (var x = 0; x < GameEngine.GameScreens.TilesAcross; x++)
            {
                for (var a = 0; a < _colourAnimationsRegistered.Count; a++)
                {
                    if (_colourAnimationsRegistered[a].Running &&
                        _colourAnimationsRegistered[a].UpperTileToDraw >= 0)
                    {
                        if (GameEngine.GameScreens.CurrentGameScreenData.ScreenTiles[x, y].TileImage == _colourAnimationsRegistered[a].TileNo) // Teleport booths
                        {
                            // Layer up the tiles to make the intensity deeper
                            for (var b = 0; b < _colourAnimationsRegistered[a].Intensity; b++)
                            {
                                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).DrawSprite(_colourAnimationsRegistered[a].UpperTileToDraw, x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                                    y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).SpriteHeight, _colourAnimationsRegistered[a].TransitionColour);
                            }
                        }
                    }
                }
            }
        }
        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();
    }
}