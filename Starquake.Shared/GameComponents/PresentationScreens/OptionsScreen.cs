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

        _keyDefiner = new KeyDefiner(gameEngineReference.ContentResourceManager, gameEngineReference.Font2);
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

    /// <summary>
    ///     The four presets the player chooses between, which are the first four entries of
    ///     <see cref="_options" />. The entries after them - define keys, start, quit - are not
    ///     presets and are handled separately.
    /// </summary>
    private const int SelectableOptionCount = 4;

    public override void Update(GameTime gameTime)
    {
        if (_definingKeys)
        {
            _keyDefiner.Update(gameTime);
        }
        else
        {
            HandleOptionKeys();
            GameEngine.Font2.Update();
        }

        base.Update(gameTime);
    }

    /// <summary>
    ///     0 confirms whatever is selected, 5 goes off to define keys by hand, and 1 to 4 pick one of the
    ///     four presets.
    /// </summary>
    private void HandleOptionKeys()
    {
        var pressed = ToggleKeys.GetPressedKey();

        if (pressed == Keys.D0)
        {
            ConfirmSelectedOption();
            return;
        }

        if (pressed == Keys.D5)
        {
            _definingKeys = true;
            _keyDefiner.Reset();
            GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
            return;
        }

        // 1 to 4 pick the option of that number, and were four copies of this
        var option = pressed - Keys.D1;

        if (option < 0 || option >= SelectableOptionCount || _options[option].Selected)
        {
            return;
        }

        ClearSelectedOptions();
        _options[option].Selected = true;
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
    }

    /// <summary>
    ///     Applies the selected preset and leaves for the report screen. If nothing is selected the keys are
    ///     left as they were, which is what the original chain of four tests also did.
    /// </summary>
    private void ConfirmSelectedOption()
    {
        for (var option = 0; option < SelectableOptionCount; option++)
        {
            if (!_options[option].Selected)
            {
                continue;
            }

            GameEngine.PlayerKeys = KeysForOption(option);
            break;
        }

        GameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(GameEngine.Presentation.ScreenClearColour);
        GameEngine.ScreenDirectory.Current = GameScreenType.ComputerReport;
        GameEngine.ScreenDirectory.Get(GameScreenType.ComputerReport).PerformPostScreenChangeActions(GameScreenType.Options, null);
    }

    /// <summary>
    ///     The four key presets. Each was written out as seven assignments to a fresh PlayerKeySet, so the
    ///     four are now four object initialisers and the differences between them can be read down a column.
    /// </summary>
    private PlayerKeySet KeysForOption(int option)
    {
        return option switch
        {
            // Joystick: every movement key is unbound, so only pause and quit still answer
            0 => new PlayerKeySet
            {
                Up = Keys.None,
                Down = Keys.None,
                Left = Keys.None,
                Right = Keys.None,
                Fire = Keys.None,
                Pause = Keys.P,
                Quit = Keys.Q
            },
            1 => new PlayerKeySet
            {
                Up = Keys.Up,
                Down = Keys.Down,
                Left = Keys.Left,
                Right = Keys.Right,
                Fire = Keys.LeftControl,
                Pause = Keys.P,
                Quit = Keys.Q
            },
            // The original Spectrum layout
            2 => new PlayerKeySet
            {
                Up = Keys.Q,
                Down = Keys.A,
                Left = Keys.O,
                Right = Keys.P,
                Fire = Keys.Space,
                Pause = Keys.S,
                Quit = Keys.D
            },
            _ => new PlayerKeySet
            {
                Up = _keyDefiner.DefinedKeys[0],
                Down = _keyDefiner.DefinedKeys[1],
                Left = _keyDefiner.DefinedKeys[2],
                Right = _keyDefiner.DefinedKeys[3],
                Fire = _keyDefiner.DefinedKeys[4],
                Pause = _keyDefiner.DefinedKeys[5],
                Quit = _keyDefiner.DefinedKeys[6]
            }
        };
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