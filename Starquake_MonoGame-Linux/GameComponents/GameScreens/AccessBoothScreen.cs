using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StarquakeXNA.Helpers;

namespace StarquakeXNA.GameComponents
{
    public class AccessBoothScreen : GameComponentBase, IGameScreen
    {
        int screenFade;
        public int ScreenFade
        {
            get { return screenFade; }
            set { screenFade = value; }
        }

        int[] accessCode;
        int[] inventorySlotToScan;
        bool soundPlayed = false;

        int resultMessageFade;
        
        BlobAccessBoothAnim blobAnim1;
        BlobAccessBoothAnim blobAnim2;
        
        KeyScanner[] accessCodes;
        DateTime screenReadWait;

        bool codeSucceeded;
        public bool Success
        {
            get { return codeSucceeded; }
        }

        public AccessBoothScreen(GameEngine GameEngineReference)
            : base(GameEngineReference)
        {
            accessCode = new int[3]{-1,-1,-1};
            accessCodes = new KeyScanner[3];
            inventorySlotToScan = new int[3];

            for (int a = 0; a < accessCodes.Length; a++)
            {
                accessCodes[a] = new KeyScanner(gameEngine, true);
            }

            blobAnim1 = new BlobAccessBoothAnim(GameEngineReference, 164, 800);
            blobAnim2 = new BlobAccessBoothAnim(GameEngineReference, 164, 800);

            Reset(null);
        }
        
        #region IGameScreen Members

        
        public void Reset(object Data)
        {
            soundPlayed = false;
            blobAnim1.Reset(enDirection.Left);
            blobAnim2.Reset(enDirection.Right);

            if (gameEngine.GameScreens.CurrentGameScreenData.AccessCode != null)
            {
                for (int a = 0; a < accessCode.Length; a++)
                {
                    accessCode[a] = Int32.Parse(gameEngine.GameScreens.CurrentGameScreenData.AccessCode.Substring(a, 1));
                }
            }

            screenReadWait = new DateTime(0);
            resultMessageFade = 255;
  
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
            
            // Move blob
            if (Data != null)
            {
                object[] temp = Data as object[];

                if (temp != null)
                {

                    if (((enDirection)temp[0]) == enDirection.Left)
                    {
                        if (codeSucceeded)
                        {
                            gameEngine.Player.BlobXPos = (((int)temp[1] - 1) * 128) + 16;
                            gameEngine.Player.BlobFrame = 0;
                        }
                        else
                        {
                            gameEngine.Player.BlobXPos = (((int)temp[1]) * 128) + 48;
                            gameEngine.Player.BlobFrame = 0;
                        }
                    }
                    else if (((enDirection)temp[0]) == enDirection.Right)
                    {
                        if (codeSucceeded)
                        {
                            gameEngine.Player.BlobXPos = (((int)temp[1] + 1) * 128) + 48;
                            gameEngine.Player.BlobFrame = 10;
                        }
                        else
                        {
                            gameEngine.Player.BlobXPos = (((int)temp[1]) * 128) + 16;
                            gameEngine.Player.BlobFrame = 10;
                        }
                    }
                }

            }
        }

        public void PerformPostScreenChangeActions(enGameScreen TriggeringScreen, object Param)
        {
            //throw new NotImplementedException();
        }

        #endregion

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("MachineryBackground", 0.5f, true);

            blobAnim1.Update();
            blobAnim2.Update();

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
                    if (a < 2 && !accessCodes[a+1].Scanned)
                    {
                        accessCodes[a + 1].StartScan();
                        gameEngine.BlobStatusPanel.ScanSlot(inventorySlotToScan[a+1]);
                    }
                }
                accessCodes[a].Update();
            }

            resultMessageFade = gameEngine.RandomNumberGenerator.Next(200, 256);

            if (screenReadWait.Ticks > 0)
            {
                if (screenReadWait + new TimeSpan(0, 0, 1) < System.DateTime.Now)
                {
                    gameEngine.GetGameScreen(enGameScreen.MainGame).ScreenFade = 40;
                    gameEngine.GetGameScreen(enGameScreen.MainGame).PerformPostScreenChangeActions(enGameScreen.AccessBooth, codeSucceeded);

                    gameEngine.CurrentGameScreen = enGameScreen.MainGame;
                }
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            gameEngine.Font2.WriteText(350, 100, "SECURITY  DOOR", 2, Color.White, 255);
            gameEngine.Font2.WriteText(370, 300, "ACCESS CODE", 2, Color.White, 255);
            gameEngine.Font2.End();

            //gameEngine.SQText.WriteText("SECURITY  DOOR", 250, 100, gameTime, Color.White);
            //gameEngine.SQText.WriteText("ACCESS  CODE", 270, 300, gameTime, Color.White);

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).DrawSprite(399, 380, 160);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).DrawSprite(400, 508, 160);

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).End();

            blobAnim1.Draw(gameTime);
            blobAnim2.Draw(gameTime);

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).DrawSprite(9, 380, 160);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).DrawSprite(10, 508, 160);
            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).End();

            for (int a = 0; a < 3; a++)
            {
                accessCodes[a].Draw(accessCode[a] + 10, 348 + ((a * 128)), 364, gameTime);
            }
            
            // Draw the result
            if (accessCodes[2].Scanned)
            {
                if (codeSucceeded)
                {
                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(167, 498, 40, 40), new Color(0, (byte)resultMessageFade, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(215, 500, 36, 36), new Color(0, (byte)resultMessageFade, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(259, 504, 28, 28), new Color(0, (byte)resultMessageFade, 0));
                    gameEngine.ShapeDrawer.End();

                    gameEngine.Font2.Begin(BlendState.AlphaBlend);
                    gameEngine.Font2.WriteText(310, 500, "ACCESS AUTHORISED", 2, new Color(0, 255, 0), (byte)resultMessageFade);
                    gameEngine.Font2.End();

                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(756, 504, 28, 28), new Color(0, (byte)resultMessageFade, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(792, 500, 36, 36), new Color(0, (byte)resultMessageFade, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(836, 498, 40, 40), new Color(0, (byte)resultMessageFade, 0));
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
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(157, 498, 40, 40), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(205, 500, 36, 36), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(249, 504, 28, 28), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.End();

                    gameEngine.Font2.Begin(BlendState.AlphaBlend);
                    gameEngine.Font2.WriteText(300, 500, "ACCESS CODE INVALID", 2, Color.Red, (byte)resultMessageFade);
                    gameEngine.Font2.End();

                    gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(746, 504, 28, 28), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(782, 500, 36, 36), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.FillRectangle(new Rectangle(826, 498, 40, 40), new Color(resultMessageFade, 0, 0, 0));
                    gameEngine.ShapeDrawer.End();

                    // Don't allow the sound to be called more than once
                    if (!soundPlayed)
                    {
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("AccessFailed", 1f, false);
                        soundPlayed = true;
                    }
                }

                // Start the pause timer
                if (screenReadWait.Ticks == 0)
                {
                    screenReadWait = System.DateTime.Now;
                }

                soundPlayed = true;
            }
        }
    }
}
