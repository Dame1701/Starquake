// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA.Helpers;

namespace StarquakeXNA.GameComponents;

public class KeyDefiner
{

    private readonly GameEngine gameEngine;
    private readonly string[] KeysToDefine = new[]
    {
        "UP",
        "DOWN",
        "LEFT",
        "RIGHT",
        "FIRE",
        "PAUSE",
        "QUIT"
    };
    private bool Alldone;

    private int DefinedKeyIndex;

    public KeyDefiner(GameEngine GameEngineReference)
    {
        gameEngine = GameEngineReference;
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
        var KeyString = string.Empty;

        for (var a = 0; a < 5; a++)
        {
            KeyString = KeyString + ToggleKeys.GetFriendlyName(DefinedKeys[a]) + ", ";
        }

        return KeyString.Substring(0, KeyString.Length - 2);
    }

    public void Reset()
    {
        DefinedKeyIndex = 0;
        Alldone = false;
        for (var a = 0; a < DefinedKeys.Length; a++)
        {
            DefinedKeys[a] = Keys.None;
        }
    }

    private bool KeyAlreadyUsed(Keys Key)
    {
        if (DefinedKeys.Contains(Key))
        {
            return true;
        }

        return false;
    }

    public void Update(GameTime gameTime)
    {
        var PressedKey = ToggleKeys.GetPressedKey();

        if (!Alldone)
        {
            if (DefinedKeyIndex < DefinedKeys.Length)
            {
                if (PressedKey != Keys.None && !KeyAlreadyUsed(PressedKey))
                {
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                    DefinedKeys[DefinedKeyIndex] = PressedKey;
                    DefinedKeyIndex++;
                }
            }
            else
            {
                if (KeyDefinitionComplete != null)
                {
                    Alldone = true;

                }
            }
        }
        else
        {
            if (PressedKey == Keys.Y)
            {
                KeyDefinitionComplete(this, EventArgs.Empty);
            }
            else if (PressedKey == Keys.N)
            {
                Reset();
            }
        }
    }

    public void Draw(GameTime gameTime, int screenFade)
    {
        gameEngine.Font2.WriteText(480, 236, "Press a key for:", 2, ColourHelper.GetXNA4Colour(130, 130, 255), (byte)screenFade);

        var keyDefineYPos = 290;
        for (var a = 0; a < KeysToDefine.Length; a++)
        {
            gameEngine.Font2.WriteText(500, keyDefineYPos, KeysToDefine[a] + ":", 2, Color.White, (byte)screenFade);
            keyDefineYPos += 40;
        }

        keyDefineYPos = 290;
        for (var a = 0; a < DefinedKeys.Length; a++)
        {
            if (DefinedKeys[a] != Keys.None)
            {
                gameEngine.Font2.WriteText(660, keyDefineYPos, ToggleKeys.GetFriendlyName(DefinedKeys[a]), 2, ColourHelper.GetXNA4Colour(130, 130, 255), (byte)screenFade);
            }

            keyDefineYPos += 40;
        }

        if (Alldone)
        {
            gameEngine.Font2.WriteText(540, 590, "OK ?  ( Y / N )", ColourHelper.GetXNA4Colour(130, 130, 255), (byte)screenFade);
        }
    }
}