// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Fonts;
using Starquake.Input;

namespace Starquake.GameComponents.PresentationScreens;

internal class KeyDefiner(ResourceManager resources, BitmapFont font, ToggleKeys input)
{
    private readonly string[] _keysToDefine =
    [
        "UP",
        "DOWN",
        "LEFT",
        "RIGHT",
        "FIRE",
        "PAUSE",
        "QUIT"
    ];

    private bool _allDone;
    private int _definedKeyIndex;

    public Keys[] DefinedKeys { get; } =
    [
        Keys.O,
        Keys.P,
        Keys.A,
        Keys.Q,
        Keys.Space,
        Keys.H,
        Keys.Escape
    ];

    public event EventHandler? KeyDefinitionComplete;

    public string GetKeys()
    {
        string keyString = string.Empty;

        for (int a = 0; a < 5; a++)
        {
            keyString = keyString + ToggleKeys.GetFriendlyName(DefinedKeys[a]) + ", ";
        }

        return keyString.Substring(0, keyString.Length - 2);
    }

    public void Reset()
    {
        _definedKeyIndex = 0;
        _allDone = false;
        for (int a = 0; a < DefinedKeys.Length; a++)
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

    public void Update()
    {
        Keys pressedKey = input.GetPressedKey();

        if (!_allDone)
        {
            if (_definedKeyIndex < DefinedKeys.Length)
            {
                if (pressedKey != Keys.None && !KeyAlreadyUsed(pressedKey))
                {
                    resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobPickUp, 0.5f, false);
                    DefinedKeys[_definedKeyIndex] = pressedKey;
                    _definedKeyIndex++;
                }
            }
            else
            {
                if (KeyDefinitionComplete != null)
                {
                    _allDone = true;

                }
            }
        }
        else
        {
            if (pressedKey == Keys.Y)
            {
                KeyDefinitionComplete?.Invoke(this, EventArgs.Empty);
            }
            else if (pressedKey == Keys.N)
            {
                Reset();
            }
        }
    }

    public void Draw(int screenFade)
    {
        font.WriteText(480, 236, "Press a key for:", 2, Colours.GetXna4Colour(130, 130, ColourLevels.Full), (byte)screenFade);

        int keyDefineYPos = 290;
        foreach (string keysToDefine in _keysToDefine)
        {
            font.WriteText(500, keyDefineYPos, keysToDefine + ":", 2, Color.White, (byte)screenFade);
            keyDefineYPos += 40;
        }

        keyDefineYPos = 290;
        foreach (Keys definedKey in DefinedKeys)
        {
            if (definedKey != Keys.None)
            {
                font.WriteText(660, keyDefineYPos, ToggleKeys.GetFriendlyName(definedKey), 2, Colours.GetXna4Colour(130, 130, ColourLevels.Full), (byte)screenFade);
            }

            keyDefineYPos += 40;
        }

        if (_allDone)
        {
            font.WriteText(540, 590, "OK ?  (Y / N )", Colours.GetXna4Colour(130, 130, ColourLevels.Full), (byte)screenFade);
        }
    }
}
