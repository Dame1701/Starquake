using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StarquakeXNA;
using StarquakeXNA.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarquakeXNA.GameComponents
{
    public class KeyDefiner
    {
        private string[] KeysToDefine = new string[] { "UP", "DOWN", "LEFT", "RIGHT", "FIRE", "PAUSE", "QUIT" };
        
        private Keys[] definedKeys = null;
        public Keys[] DefinedKeys
        {
            get { return definedKeys; }
        }
        
        private int DefinedKeyIndex = 0;
        private GameEngine gameEngine;
        private bool Alldone = false;

        public event EventHandler KeyDefinitionComplete;

        public KeyDefiner(GameEngine GameEngineReference)
        {
            gameEngine = GameEngineReference;
            definedKeys = new Keys[] { Keys.O, Keys.P, Keys.A, Keys.Q, Keys.Space, Keys.H, Keys.Escape };
        }

        public string GetKeys()
        {
            string KeyString = string.Empty;

            for (int a = 0; a < 5; a++)
            {
                KeyString = KeyString + ToggleKeys.GetFriendlyName(definedKeys[a]) + ", ";
            }

            return KeyString.Substring(0, KeyString.Length - 2);
        }

        public void Reset()
        {
            DefinedKeyIndex = 0;
            Alldone = false;
            for (int a = 0; a < definedKeys.Length; a++)
            {
                definedKeys[a] = Keys.None;
            }
        }

        bool KeyAlreadyUsed(Keys Key)
        {
            if (DefinedKeys.Contains(Key))
            {
                return true;
            }

            return false;
        }

        public void Update(GameTime gameTime)
        {
            Keys PressedKey = ToggleKeys.GetPressedKey();

            if (Alldone == false)
            {
                if (DefinedKeyIndex < definedKeys.Length)
                {
                    if (PressedKey != Keys.None && !KeyAlreadyUsed(PressedKey))
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                        definedKeys[DefinedKeyIndex] = PressedKey;
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

            int keyDefineYPos = 290;
            for (int a = 0; a < KeysToDefine.Length; a++)
            {
                gameEngine.Font2.WriteText(500, keyDefineYPos, KeysToDefine[a] + ":", 2, Color.White, (byte)screenFade);
                keyDefineYPos += 40;
            }

            keyDefineYPos = 290;
            for (int a = 0; a < definedKeys.Length ; a++)
            {
                if (definedKeys[a] != Keys.None)
                {
                    gameEngine.Font2.WriteText(660, keyDefineYPos, ToggleKeys.GetFriendlyName(definedKeys[a]), 2, ColourHelper.GetXNA4Colour(130, 130, 255), (byte)screenFade);
                }

                keyDefineYPos += 40;
            }

            if (Alldone)
            {
                gameEngine.Font2.WriteText(540, 590, "OK ?  ( Y / N )", ColourHelper.GetXNA4Colour(130, 130, 255), (byte)screenFade);
            }
        }
    }
}
