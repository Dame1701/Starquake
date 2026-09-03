using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;
using StarquakeXNA.Structure;

namespace StarquakeXNA.GameComponents
{
    public class Option
    {
        public string ActivationKey;
        public string OptionText;
        public bool Selected;
        public bool Unavailable;
        public Color Colour;

        public Option(string ActivationKey, string OptionText, bool Selected, Color Colour, bool Unavailable = false)
        {
            this.ActivationKey = ActivationKey;
            this.OptionText = OptionText;
            this.Selected = Selected;
            this.Colour = Colour;
            this.Unavailable = Unavailable;
        }
    }

    public class OptionsScreen : GameScreenBase, IGameScreen
    {
        #region IGameScreen Members

        private bool DefiningKeys = false;
        private SpriteCollection backgroundImage;
        private SpriteCollection foregroundImage;
        private List<Option> options;
        private KeyDefiner keyDefiner;

        public bool Success
        {
            get { return true; }
        }

        public OptionsScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            screenFade = 0;
            screenFadeSpeed = 5;

            keyDefiner = new KeyDefiner(GameEngineReference);
            keyDefiner.KeyDefinitionComplete += keyDefiner_KeyDefinitionComplete;
        }

        void keyDefiner_KeyDefinitionComplete(object sender, EventArgs e)
        {
            options[3] = new Option("4", "UDK Keyboard . . " + keyDefiner.GetKeys(), false, Color.White);

            ClearSelectedOptions();
            options[3].Selected = true;
            
            DefiningKeys = false;
        }

        public override void LoadContent()
        {
            backgroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("OptionScreenBackground");
            foregroundImage = gameEngine.ContentResourceManager.GetSpriteCollection("OptionScreenForeground");

            options = new List<Option>();
            options.Add(new Option("1", "Joystick 0", false, Color.White, true));
            options.Add(new Option("2", "Cursor Keys, Left Ctrl", true, Color.White));
            options.Add(new Option("3", "Keyboard . . O, P, A, Q, SP", false, Color.White));
            options.Add(new Option("4", "UDK Keyboard . . " + keyDefiner.GetKeys(), false, Color.White));
            options.Add(new Option("5", "Define your own keys", false,  ColourHelper.GetXNA4Colour(130,130,255)));
            options.Add(new Option("0", "Start game", false,  ColourHelper.GetXNA4Colour(130,130,255)));
            options.Add(new Option("Q", "Quit", false, Color.White));

            gameEngine.Font2.Flash = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (!DefiningKeys)
            {
                if (ToggleKeys.GetPressedKey() == Keys.D0)
                {
                    if (options[0].Selected)
                    {
                        // Player keys
                        PlayerKeySet PlayerKeys = new PlayerKeySet();
                        PlayerKeys.Up = Keys.None;
                        PlayerKeys.Down = Keys.None;
                        PlayerKeys.Left = Keys.None;
                        PlayerKeys.Right = Keys.None;
                        PlayerKeys.Fire = Keys.None;

                        // Set quit and pause only
                        PlayerKeys.Pause = Keys.P;
                        PlayerKeys.Quit = Keys.Q;

                        gameEngine.PlayerKeys = PlayerKeys;
                    }
                    else if (options[1].Selected)
                    {
                        // Player keys
                        PlayerKeySet PlayerKeys = new PlayerKeySet();
                        PlayerKeys.Up = Keys.Up;
                        PlayerKeys.Down = Keys.Down;
                        PlayerKeys.Left = Keys.Left;
                        PlayerKeys.Right = Keys.Right;
                        PlayerKeys.Fire = Keys.LeftControl;

                        PlayerKeys.Pause = Keys.P;
                        PlayerKeys.Quit = Keys.Q;

                        gameEngine.PlayerKeys = PlayerKeys;
                    }
                    else if (options[2].Selected)
                    {
                        // Player keys
                        PlayerKeySet PlayerKeys = new PlayerKeySet();
                        PlayerKeys.Up = Keys.Q;
                        PlayerKeys.Down = Keys.A;
                        PlayerKeys.Left = Keys.O;
                        PlayerKeys.Right = Keys.P;
                        PlayerKeys.Fire = Keys.Space;
                        
                        PlayerKeys.Pause = Keys.S;
                        PlayerKeys.Quit = Keys.D;

                        gameEngine.PlayerKeys = PlayerKeys;
                    }
                    else if (options[3].Selected)
                    {
                        // Player keys
                        PlayerKeySet PlayerKeys = new PlayerKeySet();
                        PlayerKeys.Up = keyDefiner.DefinedKeys[0];
                        PlayerKeys.Down = keyDefiner.DefinedKeys[1];
                        PlayerKeys.Left = keyDefiner.DefinedKeys[2];
                        PlayerKeys.Right = keyDefiner.DefinedKeys[3];
                        PlayerKeys.Fire = keyDefiner.DefinedKeys[4];

                        PlayerKeys.Pause = keyDefiner.DefinedKeys[5];
                        PlayerKeys.Quit = keyDefiner.DefinedKeys[6];

                        gameEngine.PlayerKeys = PlayerKeys;
                    }

                    gameEngine.GraphicsDeviceMgr.GraphicsDevice.Clear(gameEngine.ScreenClearColour);
                    gameEngine.CurrentGameScreen = enGameScreen.ComputerReport;
                    gameEngine.GetGameScreen(enGameScreen.ComputerReport).PerformPostScreenChangeActions(enGameScreen.Options, null);
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D1)
                {
                    if (options[0].Selected == false)
                    {
                        ClearSelectedOptions();
                        options[0].Selected = true;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                    }
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D2)
                {
                    if (options[1].Selected == false)
                    {
                        ClearSelectedOptions();
                        options[1].Selected = true;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                    }
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D3)
                {
                    if (options[2].Selected == false)
                    {
                        ClearSelectedOptions();
                        options[2].Selected = true;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                    }
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D4)
                {
                    if (options[3].Selected == false)
                    {
                        ClearSelectedOptions();
                        options[3].Selected = true;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                    }
                }
                else if (ToggleKeys.GetPressedKey() == Keys.D5)
                {
                    DefiningKeys = true;
                    keyDefiner.Reset();
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }

                gameEngine.Font2.Update();
            }
            else
            {
                keyDefiner.Update(gameTime);
            }

            base.Update(gameTime);
        }

        private void ClearSelectedOptions()
        {
            foreach (Option opt in options)
            {
                opt.Selected = false;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            gameEngine.ScreenEffctEngine.SelectRenderViewPort("EntireScreen");

            backgroundImage.Begin(BlendState.AlphaBlend, gameTime);
            backgroundImage.DrawSprite(0, 0, 0, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            backgroundImage.End();

            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            if (!DefiningKeys)
            {
                int optionXPos = 320;
                int optionYPos = 260;

                foreach (Option opt in options)
                {
                    if (opt.Unavailable)
                    {
                        gameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, ColourHelper.GetXNA4Colour(100, 0, 0), (byte)screenFade);
                        gameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText + " (offline)", 2, ColourHelper.GetXNA4Colour(100, 0, 0), (byte)screenFade);
                    }
                    else
                    {
                        if (opt.Selected)
                        {
                            gameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, (byte)screenFade);
                            gameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText, 2, (byte)screenFade);
                        }
                        else
                        {
                            gameEngine.Font2.WriteText(optionXPos, optionYPos, opt.ActivationKey + ".", 2, opt.Colour, (byte)screenFade);
                            gameEngine.Font2.WriteText(optionXPos + 50, optionYPos, opt.OptionText, 2, opt.Colour, (byte)screenFade);
                        }
                    }

                    optionYPos = optionYPos + 50;
                }
            }
            else
            {
                keyDefiner.Draw(gameTime, screenFade);
            }

            gameEngine.Font2.WriteText(555, 640, "2017", 2, new Color(112,155,155), (byte)screenFade);

            gameEngine.Font2.End();

            foregroundImage.Begin(BlendState.AlphaBlend, gameTime);
            foregroundImage.DrawSprite(0, 230, 120, ColourHelper.GetXNA4Colour(255, 255, 255, (byte)screenFade));
            foregroundImage.End();
        }

        public void Reset(object Data)
        {
            
        }

        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            screenFade = 0;
            screenFadeSpeed = 5;
        }

        #endregion
    }
}
