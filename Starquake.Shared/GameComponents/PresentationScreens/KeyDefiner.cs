// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Input;

namespace StarquakeXNA.GameComponents.PresentationScreens;

public class KeyDefiner
{

    private readonly GameEngine _gameEngine;
    private readonly string[] _keysToDefine = new[]
    {
        "UP",
        "DOWN",
        "LEFT",
        "RIGHT",
        "FIRE",
        "PAUSE",
        "QUIT"
    };
    private bool _alldone;

    private int _definedKeyIndex;

    public KeyDefiner(GameEngine gameEngineReference)
    {
        _gameEngine = gameEngineReference;
        DefinedKeys = new[]
        {
            Keys.O,
            Keys.P,
            Keys.A,
            Keys.Q,
            Keys.Space,
            Keys.H,
            Keys.Escape
        };
    }
    public Keys[] DefinedKeys { get; }

    public event EventHandler KeyDefinitionComplete;

    public string GetKeys()
    {
        var keyString = string.Empty;

        for (var a = 0; a < 5; a++)
        {
            keyString = keyString + ToggleKeys.GetFriendlyName(DefinedKeys[a]) + ", ";
        }

        return keyString.Substring(0, keyString.Length - 2);
    }

    public void Reset()
    {
        _definedKeyIndex = 0;
        _alldone = false;
        for (var a = 0; a < DefinedKeys.Length; a++)
        {
            DefinedKeys[a] = Keys.None;
        }
    }

    private bool KeyAlreadyUsed(Keys key)
    {
        if (DefinedKeys.Contains(key))
        {
            return true;
        }

        return false;
    }

    public void Update(GameTime gameTime)
    {
        var pressedKey = ToggleKeys.GetPressedKey();

        if (!_alldone)
        {
            if (_definedKeyIndex < DefinedKeys.Length)
            {
                if (pressedKey != Keys.None && !KeyAlreadyUsed(pressedKey))
                {
                    _gameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobPickUp", 0.5f, false);
                    DefinedKeys[_definedKeyIndex] = pressedKey;
                    _definedKeyIndex++;
                }
            }
            else
            {
                if (KeyDefinitionComplete != null)
                {
                    _alldone = true;

                }
            }
        }
        else
        {
            if (pressedKey == Keys.Y)
            {
                KeyDefinitionComplete(this, EventArgs.Empty);
            }
            else if (pressedKey == Keys.N)
            {
                Reset();
            }
        }
    }

    public void Draw(GameTime gameTime, int screenFade)
    {
        _gameEngine.Font2.WriteText(480, 236, "Press a key for:", 2, ColourHelper.GetXna4Colour(130, 130, 255), (byte)screenFade);

        var keyDefineYPos = 290;
        for (var a = 0; a < _keysToDefine.Length; a++)
        {
            _gameEngine.Font2.WriteText(500, keyDefineYPos, _keysToDefine[a] + ":", 2, Color.White, (byte)screenFade);
            keyDefineYPos += 40;
        }

        keyDefineYPos = 290;
        for (var a = 0; a < DefinedKeys.Length; a++)
        {
            if (DefinedKeys[a] != Keys.None)
            {
                _gameEngine.Font2.WriteText(660, keyDefineYPos, ToggleKeys.GetFriendlyName(DefinedKeys[a]), 2, ColourHelper.GetXna4Colour(130, 130, 255), (byte)screenFade);
            }

            keyDefineYPos += 40;
        }

        if (_alldone)
        {
            _gameEngine.Font2.WriteText(540, 590, "OK ?  ( Y / N )", ColourHelper.GetXna4Colour(130, 130, 255), (byte)screenFade);
        }
    }
}