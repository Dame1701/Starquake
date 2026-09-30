// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Starquake.Constants;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.GameComponents.PresentationScreens;

public class OptionsScreen : GameScreenBase, IGameScreen
{

    #region IGameScreen Members

    private bool _definingKeys;
    private readonly SpriteCollection _backgroundImage;
    private readonly SpriteCollection _foregroundImage;
    private readonly List<Option> _options;
    private readonly KeyDefiner _keyDefiner;

    public bool Success => true;

    public OptionsScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;

        _keyDefiner = new KeyDefiner(gameEngineReference);
        _keyDefiner.KeyDefinitionComplete += keyDefiner_KeyDefinitionComplete;

        _backgroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("OptionScreenBackground");
        _foregroundImage = GameEngine.ContentResourceManager.GetSpriteCollection("OptionScreenForeground");

        _options =
        [
            new Option("1", "Joystick 0", false, Color.White, true),
            new Option("2", "Cursor Keys, Left Ctrl", true, Color.White),
            new Option("3", "Keyboard . . O, P, A, Q, SP", false, Color.White),
            new Option("4", "UDK Keyboard . . " + _keyDefiner.GetKeys(), false, Color.White),
            new Option("5", "Define your own keys", false, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full)),
            new Option("0", "Start game", false, ColourHelper.GetXna4Colour(130, 130, ColourLevels.Full)),
            new Option("Q", "Quit", false, Color.White)
        ];
    }

    private void keyDefiner_KeyDefinitionComplete(object? sender, EventArgs e)
    {
        _options[3] = new Option("4", "UDK Keyboard . . " + _keyDefiner.GetKeys(), false, Color.White);

        ClearSelectedOptions();
        _options[3].Selected = true;

        _definingKeys = false;
    }

    public override void LoadContent()
    {
        GameEngine.Font2.Flash = true;
    }

    public override void Update(GameTime gameTime)
    {
        if (!_definingKeys)
        {
            if (ToggleKeys.GetPressedKey() == Keys.D0)
            {
                if (_options[0].Selected)
                {
                    // Player keys
                    var playerKeys = new PlayerKeySet();
                    playerKeys.Up = Keys.None;
                    playerKeys.Down = Keys.None;
                    playerKeys.Left = Keys.None;
                    playerKeys.Right = Keys.None;
                    playerKeys.Fire = Keys.None;

                    // Set quit and pause only
                    playerKeys.Pause = Keys.P;
                    playerKeys.Quit = Keys.Q;

                    GameEngine.PlayerKeys = playerKeys;
                }
                else if (_options[1].Selected)
                {
                    // Player keys
                    var playerKeys = new PlayerKeySet();
                    playerKeys.Up = Keys.Up;
                    playerKeys.Down = Keys.Down;
                    playerKeys.Left = Keys.Left;
                    playerKeys.Right = Keys.Right;
                    playerKeys.Fire = Keys.LeftControl;

                    playerKeys.Pause = Keys.P;
                    playerKeys.Quit = Keys.Q;

                    GameEngine.PlayerKeys = playerKeys;
                }
                else if (_options[2].Selected)
                {
                    // Player keys
                    var playerKeys = new PlayerKeySet();
                    playerKeys.Up = Keys.Q;
                    playerKeys.Down = Keys.A;
                    playerKeys.Left = Keys.O;
                    playerKeys.Right = Keys.P;
                    playerKeys.Fire = Keys.Space;

                    playerKeys.Pause = Keys.S;
                    playerKeys.Quit = Keys.D;

                    GameEngine.PlayerKeys = playerKeys;
                }
                else if (_options[3].Selected)
                {
                    // Player keys
                    var playerKeys = new PlayerKeySet();
                    playerKeys.Up = _keyDefiner.DefinedKeys[0];
                    playerKeys.Down = _keyDefiner.DefinedKeys[1];
                    playerKeys.Left = _keyDefiner.DefinedKeys[2];
                    playerKeys.Right = _keyDefiner.DefinedKeys[3];
                    playerKeys.Fire = _keyDefiner.DefinedKeys[4];

                    playerKeys.Pause = _keyDefiner.DefinedKeys[5];
                    playerKeys.Quit = _keyDefiner.DefinedKeys[6];

                    GameEngine.PlayerKeys = playerKeys;
                }

                GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.ScreenClearColour);
                GameEngine.CurrentGameScreen = GameScreenType.ComputerReport;
                GameEngine.GetGameScreen(GameScreenType.ComputerReport).PerformPostScreenChangeActions(GameScreenType.Options, null);
            }
            else if (ToggleKeys.GetPressedKey() == Keys.D1)
            {
                if (!_options[0].Selected)
                {
                    ClearSelectedOptions();
                    _options[0].Selected = true;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
            }
            else if (ToggleKeys.GetPressedKey() == Keys.D2)
            {
                if (!_options[1].Selected)
                {
                    ClearSelectedOptions();
                    _options[1].Selected = true;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
            }
            else if (ToggleKeys.GetPressedKey() == Keys.D3)
            {
                if (!_options[2].Selected)
                {
                    ClearSelectedOptions();
                    _options[2].Selected = true;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
            }
            else if (ToggleKeys.GetPressedKey() == Keys.D4)
            {
                if (!_options[3].Selected)
                {
                    ClearSelectedOptions();
                    _options[3].Selected = true;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
            }
            else if (ToggleKeys.GetPressedKey() == Keys.D5)
            {
                _definingKeys = true;
                _keyDefiner.Reset();
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            }

            GameEngine.Font2.Update();
        }
        else
        {
            _keyDefiner.Update(gameTime);
        }

        base.Update(gameTime);
    }

    private void ClearSelectedOptions()
    {
        foreach (var opt in _options)
        {
            opt.Selected = false;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GameEngine.ScreenEffectEngine.SelectRenderViewPort("EntireScreen");

        _backgroundImage.Begin(BlendState.AlphaBlend, gameTime);
        _backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        _backgroundImage.End();

        GameEngine.Font2.Begin(BlendState.AlphaBlend);
        if (!_definingKeys)
        {
            var optionXPos = 320;
            var optionYPos = 260;

            foreach (var opt in _options)
            {
                if (opt.Unavailable)
                {
                    GameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, ColourHelper.GetXna4Colour(100, 0, 0), (byte)ScreenFade);
                    GameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText + " (offline)", 2, ColourHelper.GetXna4Colour(100, 0, 0), (byte)ScreenFade);
                }
                else
                {
                    if (opt.Selected)
                    {
                        GameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, (byte)ScreenFade);
                        GameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText, 2, (byte)ScreenFade);
                    }
                    else
                    {
                        GameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, opt.Colour, (byte)ScreenFade);
                        GameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText, 2, opt.Colour, (byte)ScreenFade);
                    }
                }

                optionYPos = optionYPos + 50;
            }
        }
        else
        {
            _keyDefiner.Draw(gameTime, ScreenFade);
        }

        GameEngine.Font2.WriteText(555, 640, "2017", 2, new Color(112, 155, 155), (byte)ScreenFade);

        GameEngine.Font2.End();

        _foregroundImage.Begin(BlendState.AlphaBlend, gameTime);
        _foregroundImage.DrawSprite(0, 230, 120, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)ScreenFade));
        _foregroundImage.End();
    }

    public void Reset(object? data)
    {

    }

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object? param)
    {
        ScreenFade = 0;
        ScreenFadeSpeed = 5;
    }

    #endregion

}