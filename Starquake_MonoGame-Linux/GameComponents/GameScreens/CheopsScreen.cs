using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedLibs;

namespace StarquakeXNA.GameComponents
{
    public class CheopsScreen : GameComponentBase, IGameScreen
    {
        #region IGameScreen Members

        int resultMessageFade;
        int[] accessCode;
        int[] inventorySlotToScan;
        KeyScanner[] accessCodes;
        bool codeSucceeded;
        int screenFade;
        DateTime screenReadWait;
        bool elementSelectionMode;
        bool soundPlayed = false;

        Item[] itemsToSelect;
        int itemToSwap;

        public int ScreenFade
        {
            get
            {
                return screenFade;
            }
            set
            {
                screenFade = value;
            }
        }

        public bool Success
        {
            get { return true; }
        }

        public CheopsScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            accessCode = new int[2] { -1, -1 };
            accessCodes = new KeyScanner[2];
            inventorySlotToScan = new int[2];

            for (int a = 0; a < accessCodes.Length; a++)
            {
                accessCodes[a] = new KeyScanner(gameEngine, true);
            }

            Reset(null);
        }

        public void Reset(object Data)
        {
            soundPlayed = false;
            screenReadWait = new DateTime(0);
            resultMessageFade = 255;
            elementSelectionMode = false;

            if (gameEngine.GameScreens.CurrentGameScreenData.CheopsAccessCode != null)
            {
                for (int a = 0; a < accessCode.Length; a++)
                {
                    accessCode[a] = Int32.Parse(gameEngine.GameScreens.CurrentGameScreenData.CheopsAccessCode.Substring(a, 1));
                }
            }

            // Scan blob's inventory to see if we have the right stuff
            codeSucceeded = true;

            for (int b = 0; b < accessCode.Length; b++)
            {
                bool match = false;

                int a;
                for (a = 0; a < gameEngine.Player.BlobInventory.Length; a++)
                {
                    if ((int)gameEngine.Player.BlobInventory[a] - 10 == accessCode[b] ||
                        gameEngine.Player.BlobInventory[a] == enCollectables.AccessCard)
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    inventorySlotToScan[b] = a;
                }
                else
                {
                    inventorySlotToScan[b] = -1;
                }

                // if we don't have a match the shows over!
                if (!match)
                {
                    codeSucceeded = match;
                }

                accessCodes[b].Reset(match);
            }

            // Pick an item from the inventory to swap
            List<int> slotsToSelectFrom = new List<int>();
            itemToSwap = 255;

            for (int a = 0; a < gameEngine.Player.BlobInventory.Length; a++)
            {
                if (gameEngine.Player.BlobInventory[a] != enCollectables.AccessCard &&
                    gameEngine.Player.BlobInventory[a] != enCollectables.Key &&
                    gameEngine.Player.BlobInventory[a] != enCollectables.None)
                {
                    slotsToSelectFrom.Add(a);
                }

                
            }

            if (slotsToSelectFrom.Count == 0)
            {
                for (int a = 0; a < gameEngine.Player.BlobInventory.Length; a++)
                {
                    if (gameEngine.Player.BlobInventory[a] == enCollectables.Key)
                    {
                        itemToSwap = a;
                    }
                }

                // No key so use the access card
                if (itemToSwap == 255)
                {
                    for (int a = 0; a < gameEngine.Player.BlobInventory.Length; a++)
                    {
                        if (gameEngine.Player.BlobInventory[a] == enCollectables.AccessCard)
                        {
                            itemToSwap = a;
                        }
                    }
                }
            }
            else
            {
                itemToSwap = slotsToSelectFrom[gameEngine.RandomNumberGenerator.Next(0, slotsToSelectFrom.Count)];
            }

            // Assign 5 elements to choose from, one being a required element, the other beingthe item to swap
            if (codeSucceeded)
            {
                itemsToSelect = new Item[5];
                
                // Get an element from the core if there is one, if there isn't we've completed the game and this should never happen....
                List<CoreElement> activeElements = gameEngine.Elements.GetActiveElements();
                if (activeElements.Count > 0)
                {
                    itemsToSelect[gameEngine.RandomNumberGenerator.Next(0, 5)] =
                        new Item(
                            0,
                            0,
                            activeElements[gameEngine.RandomNumberGenerator.Next(0, activeElements.Count)].CurrentElement);
                }

                bool populatedSwapItem = false;

                // Populate the other 3 slots
                for (int a = 0; a < 5; a++)
                {
                    if (itemsToSelect[a] == null)
                    {
                        if (!populatedSwapItem)
                        {
                            populatedSwapItem = true;
                            itemsToSelect[a] = new Item(0, 0, (int)gameEngine.Player.BlobInventory[itemToSwap]);
                        }
                        else
                        {
                            itemsToSelect[a] = new Item(0, 0, gameEngine.RandomNumberGenerator.Next(0, 28));
                        }
                    }
                }
            }
        }


        public override void Update(GameTime gameTime)
        {
            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("MachineryBackground", 0.5f, true);

            if (!elementSelectionMode)
            {
                // Start first scan off....
                if (!accessCodes[0].Scanned)
                {
                    accessCodes[0].StartScan();
                    gameEngine.BlobStatusPanel.ScanSlot(inventorySlotToScan[0]);
                }

                // Follow with the subsequent ones...
                for (int a = 0; a < accessCodes.Length; a++)
                {
                    if (accessCodes[a].Scanned)
                    {
                        if (a < 1 && !accessCodes[a + 1].Scanned)
                        {
                            accessCodes[a + 1].StartScan();
                            gameEngine.BlobStatusPanel.ScanSlot(inventorySlotToScan[a + 1]);
                        }
                    }
                    accessCodes[a].Update();
                }

                resultMessageFade = gameEngine.RandomNumberGenerator.Next(200, 256);
            }

            if (screenReadWait.Ticks > 0)
            {
                if (screenReadWait + new TimeSpan(0, 0, 1) < DateTime.Now)
                {
                    if (codeSucceeded) // move into selection mode
                    {
                        elementSelectionMode = true;
                    }
                    else // Kick you out
                    {
                        gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                        gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    }
                }
            }
            
        }

        
        public override void Draw(GameTime gameTime)
        {
            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            gameEngine.Font2.WriteText(298, 100, "CHEOPS PYRAMID", 2, Color.White, 255);
            gameEngine.Font2.End();

            //gameEngine.SQText.WriteText("CHEOPS PYRAMID", 198, 100, gameTime, Color.White);

            if (gameEngine.UsingOldGraphics)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0, 750, 69);
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").End();
            }
            else
            {
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").DrawSprite(0, 750, 69);
                gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").End();
            }

            if (!elementSelectionMode)
            {
                gameEngine.Font2.Begin(BlendState.AlphaBlend);
                gameEngine.Font2.WriteText(366, 200, "CHEOPS KEY CODE", 2, Color.White, 255);
                gameEngine.Font2.End();

                for (int a = 0; a < 2; a++)
                {
                    accessCodes[a].Draw(accessCode[a] + 10, 420 + ((a * 128)), 260, gameTime);
                }

                // Draw the result
                if (accessCodes[1].Scanned)
                {
                    // Start the pause timer
                    if (screenReadWait.Ticks == 0)
                    {
                        screenReadWait = DateTime.Now;
                    }

                    if (codeSucceeded)
                    {
                        gameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.End();

                        gameEngine.Font2.Begin(BlendState.AlphaBlend);
                        gameEngine.Font2.WriteText(340, 500, "ACCESS AUTHORISED", 2, new Color(0, 255, 0), (byte)resultMessageFade);
                        gameEngine.Font2.End();

                        gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(0, resultMessageFade, 0));
                        gameEngine.ShapeDrawer.End();

                        // Don't allow the sound to be called more than once
                        if (!soundPlayed)
                        {
                            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("AccessSuccess", 1f, false);
                            soundPlayed = true;
                        }
                    }
                    else
                    {
                        gameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(187, 498, 40, 40), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(235, 500, 36, 36), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(279, 504, 28, 28), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.End();

                        gameEngine.Font2.Begin(BlendState.AlphaBlend);
                        gameEngine.Font2.WriteText(330, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)resultMessageFade);
                        gameEngine.Font2.End();

                        gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(776, 504, 28, 28), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(812, 500, 36, 36), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.FillRectangle(new Rectangle(856, 498, 40, 40), new Color(resultMessageFade, 0, 0, 0));
                        gameEngine.ShapeDrawer.End();

                        // Don't allow the sound to be called more than once
                        if (!soundPlayed)
                        {
                            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("AccessFailed", 1f, false);
                            soundPlayed = true;
                        }
                    }
                }
            }
            else // We're in selection mode
            {
                gameEngine.Font2.Begin(BlendState.AlphaBlend);
                gameEngine.Font2.WriteText(378, 190, "EXCHANGE       FOR", 2, Color.White, 255);
                gameEngine.Font2.WriteText(65, 266, "1.              2.             3.            4.             5.", 2, Color.OrangeRed, 255);
                gameEngine.Font2.WriteText(327, 432, "HIT ANY KEY FROM  TO", 2, Color.CornflowerBlue,255);
                gameEngine.Font2.WriteText(690, 432, "1", 2, Color.White, 255);
                gameEngine.Font2.WriteText(783, 432, "5", 2, Color.White, 255);

                gameEngine.Font2.End();

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);

                // Need to select something from the inventory randomly ignoring access card and key unless they are the only items....
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite((int)gameEngine.Player.BlobInventory[itemToSwap], 590, 168, Color.White);

                for (int a = 0; a < 5; a++)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(itemsToSelect[a].Type, (a * 216) + 100, 298, Color.White);
                }

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();

                if (Keyboard.GetState().IsKeyDown(Keys.D1))
                {
                    gameEngine.Player.BlobInventory[itemToSwap] = (enCollectables)itemsToSelect[0].Type;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D2))
                {
                    gameEngine.Player.BlobInventory[itemToSwap] = (enCollectables)itemsToSelect[1].Type;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D3))
                {
                    gameEngine.Player.BlobInventory[itemToSwap] = (enCollectables)itemsToSelect[2].Type;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D4))
                {
                    gameEngine.Player.BlobInventory[itemToSwap] = (enCollectables)itemsToSelect[3].Type;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D5))
                {
                    gameEngine.Player.BlobInventory[itemToSwap] = (enCollectables)itemsToSelect[4].Type;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                    gameEngine.GameScreens.CurrentGameScreenData.CheopsPyramid = null;
                    gameEngine.GameScreens.UpdateCollisionMap(false);
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("BlobPickUp", 0.5f, false);
                }
            }
        }

        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
           
        }

        #endregion
    }
}
