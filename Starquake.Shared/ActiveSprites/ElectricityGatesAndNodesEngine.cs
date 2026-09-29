// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Display;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class ElectricityGatesAndNodesEngine : GameComponentBase
{
    private readonly ConveyerNasties _conveyerNasties;
    private Animation _electricityAnimation;
    private Animation _electricityAnimation2;
    private Animation _electricityAnimationOriginal;

    private List<SecondLayerTile> _secondLayerTiles;
    public ElectricityRandomiser[] ElectricityNodeControllers { get; private set; }

    #region IGameComponent Members

    public event EventHandler OnBlobStruckByElectricity;

    public ElectricityGatesAndNodesEngine(GameEngine gameEngineReference,
        ConveyerNasties convyrNasties) : base(gameEngineReference)
    {
        _conveyerNasties = convyrNasties;
    }

    public override void Initialise(object data)
    {
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
            var electricityNodeCounter = 0;

            for (var a = 0; a < _secondLayerTiles.Count; a++)
            {
                if (_secondLayerTiles[a].TileType == TileType.ElectricityGate ||
                    _secondLayerTiles[a].TileType == TileType.ElectricityNode)
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
            var electricityNodeCounter = 0;
            _secondLayerTiles = GameEngine.GameScreens.GetListOfSecondLayerTiles();

            if (_secondLayerTiles != null)
            {
                for (var a = 0; a < _secondLayerTiles.Count; a++)
                {

                    if (_secondLayerTiles[a].TileType == TileType.ElectricityNode)
                    {
                        if (GameEngine.Player.BlobXPos + GameEngine.Player.BlobWidth > ElectricityNodeControllers[electricityNodeCounter].Location.X + 40 &&
                            GameEngine.Player.BlobXPos < ElectricityNodeControllers[electricityNodeCounter].Location.X + 88 &&
                            GameEngine.Player.BlobYPos + 64 > ElectricityNodeControllers[electricityNodeCounter].Location.Y &&
                            GameEngine.Player.BlobYPos < ElectricityNodeControllers[electricityNodeCounter].Location.Y + 32)
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
                    else if (_secondLayerTiles[a].TileType == TileType.ElectricityGate)
                    {
                        if (GameEngine.Player.BlobXPos + GameEngine.Player.BlobWidth > ElectricityNodeControllers[electricityNodeCounter].Location.X + 32 &&
                            GameEngine.Player.BlobXPos < ElectricityNodeControllers[electricityNodeCounter].Location.X + 96 &&
                            GameEngine.Player.BlobYPos + 64 > ElectricityNodeControllers[electricityNodeCounter].Location.Y + 40 &&
                            GameEngine.Player.BlobYPos < ElectricityNodeControllers[electricityNodeCounter].Location.Y + 56)
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

            if (!GameEngine.UsingOldGraphics)
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
            if (!GameEngine.UsingOldGraphics)
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
            var electricityNodeCounter = 0;

            for (var a = 0; a < _secondLayerTiles.Count; a++)
            {
                if (_secondLayerTiles[a].TileType == TileType.ElectricityNode)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (!GameEngine.UsingOldGraphics)
                                {
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(_electricityAnimation.CurrentFrame, _secondLayerTiles[a].TileLocation.X + 28, _secondLayerTiles[a].TileLocation.Y - 18, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                }
                                else
                                {
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").DrawSprite(_electricityAnimationOriginal.CurrentFrame, _secondLayerTiles[a].TileLocation.X + 32, _secondLayerTiles[a].TileLocation.Y - 16, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;

                }
                else if (_secondLayerTiles[a].TileType == TileType.ElectricityGate)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (!GameEngine.UsingOldGraphics)
                                {
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(_electricityAnimation.CurrentFrame, _secondLayerTiles[a].TileLocation.X + 28, _secondLayerTiles[a].TileLocation.Y + 14, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                                }
                                else
                                {
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").DrawSprite(_electricityAnimationOriginal.CurrentFrame, _secondLayerTiles[a].TileLocation.X + 32, _secondLayerTiles[a].TileLocation.Y + 16, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityOriginal").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;
                }
            }
        }
    }

    public void DrawUpperLayer(GameTime gameTime, int screenFade)
    {
        // Draw electricity nodes etc bottom layer
        var secondLayerTiles = GameEngine.GameScreens.GetListOfSecondLayerTiles();

        if (secondLayerTiles != null && secondLayerTiles.Count > 0)
        {
            var electricityNodeCounter = 0;
            for (var a = 0; a < secondLayerTiles.Count; a++)
            {
                if (secondLayerTiles[a].TileType == TileType.ElectricityNode)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (!GameEngine.UsingOldGraphics)
                                {
                                    // Left node
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X - 12, secondLayerTiles[a].TileLocation.Y - 14, Color.Blue);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X - 12, secondLayerTiles[a].TileLocation.Y - 14, Color.Cyan);

                                    // Right node
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 75, secondLayerTiles[a].TileLocation.Y - 14, Color.Blue);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 75, secondLayerTiles[a].TileLocation.Y - 14, Color.Cyan);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("NodeLights").End();

                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").DrawSprite(_electricityAnimation2.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y - 14, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;

                }
                else if (secondLayerTiles[a].TileType == TileType.ElectricityGate)
                {
                    if (ElectricityNodeControllers != null)
                    {
                        if (ElectricityNodeControllers.Length > electricityNodeCounter)
                        {
                            if (ElectricityNodeControllers[electricityNodeCounter].ElectricityOn)
                            {
                                if (!GameEngine.UsingOldGraphics)
                                {
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").DrawSprite(_electricityAnimation2.CurrentFrame, secondLayerTiles[a].TileLocation.X + 28, secondLayerTiles[a].TileLocation.Y + 14, Color.White);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity2").End();

                                    // Left node
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 8, secondLayerTiles[a].TileLocation.Y + 27, Color.Blue);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 8, secondLayerTiles[a].TileLocation.Y + 27, Color.Cyan);

                                    // Right node
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 78, secondLayerTiles[a].TileLocation.Y + 27, Color.Blue);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, secondLayerTiles[a].TileLocation.X + 78, secondLayerTiles[a].TileLocation.Y + 27, Color.Cyan);
                                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").End();
                                }
                            }
                        }
                    }

                    electricityNodeCounter++;
                }
            }

            if (ElectricityNodeControllers == null)
            {
                ElectricityNodeControllers = new ElectricityRandomiser[electricityNodeCounter];
                for (var a = 0; a < electricityNodeCounter; a++)
                {
                    ElectricityNodeControllers[a] = new ElectricityRandomiser(GameEngine);
                    ElectricityNodeControllers[a].Location = new Point(secondLayerTiles[a].TileLocation.X, secondLayerTiles[a].TileLocation.Y);
                }
            }

        }
        if (_conveyerNasties != null)
        {
            for (var a = 0; a < _conveyerNasties.Nasties.Count; a++)
            {
                if (_conveyerNasties.Nasties[a].NastyActive)
                {
                    // Left node
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, _conveyerNasties.Nasties[a].XLocation - 6, _conveyerNasties.Nasties[a].YLocation + 27, Color.Blue);
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, _conveyerNasties.Nasties[a].XLocation - 6, _conveyerNasties.Nasties[a].YLocation + 27, Color.Cyan);

                    // Right node
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, _conveyerNasties.Nasties[a].XLocation + 33, _conveyerNasties.Nasties[a].YLocation + 27, Color.Blue);
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").DrawSprite(0, _conveyerNasties.Nasties[a].XLocation + 33, _conveyerNasties.Nasties[a].YLocation + 27, Color.Cyan);
                    GameEngine.ContentResourceManager.GetSpriteCollection("GateLights").End();

                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").Begin(BlendState.Additive, SpriteSortMode.BackToFront, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").DrawSprite(_electricityAnimation.CurrentFrame, _conveyerNasties.Nasties[a].XLocation, _conveyerNasties.Nasties[a].YLocation + 10, Color.White, 0.5f, 0);
                    GameEngine.ContentResourceManager.GetSpriteCollection("ElectricityNodeElectricity1").End();
                }
            }
        }
    }

    #endregion

}