using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.GameComponents;
using Microsoft.Xna.Framework;

namespace StarquakeXNA
{
    public class StatusPanel : GameComponentBase
    {
        private double firePower;
        public double FirePower
        {
            get { return firePower; }
            set
            {
                firePower = value;
                if (firePower > 128)
                {
                    firePower = 128;
                }

                if (firePower < 0)
                {
                    firePower = 0;
                }
            }
        }

        private double energyLevel;
        public double EnergyLevel
        {
            get { return energyLevel; }
            set
            {
                energyLevel = value;

                if (energyLevel > 128)
                {
                    energyLevel = 128;
                }

                if (energyLevel < 0)
                {
                    energyLevel = 0;
                }
            }
        }

        private double platforms;
        public double Platforms
        {
            get { return platforms; }
            set
            {
                platforms = value;

                if (platforms > 64)
                {
                    platforms = 64;
                }

                if (platforms < 0)
                {
                    platforms = 0;
                }
            }
        }

        private int blobLives;
        public int BlobLives
        {
            get { return blobLives; }
            set { blobLives = value; }
        }

        private int playerScore;
        public int PlayerScore
        {
            get { return playerScore; }
            set { playerScore = value; }
        }

        bool removeEnergyTimerStarted = false;
        DateTime energyTime;

        enCollectables[] blobsInventory;

        PanelNumbers panelNumbers;

        KeyScanner inventoryScanner;
        int slotToScan = -1;

        public int Width
        {
            get { return gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").SpriteWidth; }
        }

        public int Height
        {
            get { return gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").SpriteHeight; }
        }

        #region IGameComponent Members

        public StatusPanel(GameEngine GameEngineRef)
            : base(GameEngineRef)
        {
            inventoryScanner = new KeyScanner(GameEngineRef, false);
            panelNumbers = new PanelNumbers();
        }

        public void LoadContent(StarquakeXNA.ContentManagement.ContentList Content)
        {
        }

        public void Initialise(List<EffectSprite> EffectSprites)
        {

        }

        public void Reset()
        {
            blobLives = 4;
            firePower = 128;
            energyLevel = 128;
            platforms = 32;
            playerScore = 0;
        }

        public void BlobHasPerformedAnAction(enBlobAction Action, object Data)
        {
            if (Action == enBlobAction.FiredLazer)
            {
                FirePower--;
            }
            else if (Action == enBlobAction.LaidPlatform)
            {
                Platforms = Platforms - 1;
            }
            else if (Action == enBlobAction.GotPowerUp)
            {
                int PowerUpType = (int)Data;

                if (PowerUpType == 0)
                {
                    EnergyLevel = EnergyLevel + 64;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp2", false);
                }
                else if (PowerUpType == 1)
                {
                    EnergyLevel = EnergyLevel + 32;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp2", false);
                }
                else if (PowerUpType == 2)
                {
                    EnergyLevel = EnergyLevel + 128;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp2", false);
                }
                else if (PowerUpType == 3)
                {
                    FirePower = FirePower + 64;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp3", 0.5f, false);
                }
                else if (PowerUpType == 4)
                {
                    FirePower = FirePower + 32;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp3", 0.5f, false);
                }
                else if (PowerUpType == 5)
                {
                    Platforms = Platforms + 32;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp1", false);
                }
                else if (PowerUpType == 6)
                {
                    if (BlobLives == 0)
                    {
                        BlobLives++;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("LifePowerUp", false); // Extra life
                    }
                    else if (
                        (Platforms * 2) <= EnergyLevel && // Top Up Jump Platforms
                        (Platforms * 2) <= FirePower)
                    {
                        Platforms = Platforms + 32;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp1", false);
                    }
                    else if (
                        FirePower <= (Platforms * 2) && // Fire power
                        FirePower <= EnergyLevel)
                    {
                        FirePower = FirePower + 64;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp3", 0.5f, false);
                    }
                    else if (
                        EnergyLevel <= (Platforms * 2) && // Energy
                        EnergyLevel <= FirePower)
                    {
                        EnergyLevel = EnergyLevel + 96;
                        gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("PowerUp2", false);
                    }
                }
                else if (PowerUpType == 7)
                {
                    BlobLives++;
                    gameEngine.ContentResourceManager.GetSoundFXEngine().PlaySound("LifePowerUp", false);
                }
            }
        }

        public void PerformPostChangeScreenActions()
        {
            throw new NotImplementedException();
        }


        public void Update(enCollectables[] BlobsInventory)
        {
            if (gameEngine.Player.BlobsState == enBlobState.Normal)
            {
                blobsInventory = BlobsInventory;

                if (!gameEngine.IntroSequence.SequenceRunning || gameEngine.IntroSequence.SequenceStage == enIntroSequenceStage.EndPause)
                {
                    // Delete Blob'ss energy one bar every 4 seconds, but only if we're not in a teleporter
                    if (gameEngine.CurrentGameScreen == enGameScreen.MainGame)
                    {
                        if (!removeEnergyTimerStarted)
                        {
                            energyTime = System.DateTime.Now;
                            removeEnergyTimerStarted = true;
                        }
                        else if (removeEnergyTimerStarted)
                        {
                            if (System.DateTime.Now - energyTime > new TimeSpan(0, 0, 4))
                            {
                                removeEnergyTimerStarted = false;
                                energyLevel = energyLevel - 1;
                            }
                        }
                    }

                    if (gameEngine.BlobStatusPanel.EnergyLevel <= 0)
                    {
                        // Lose a life and put us into the overloading state
                        gameEngine.Player.KillBlob(false, true);
                        gameEngine.BlobStatusPanel.EnergyLevel = 0;
                    }
                }

                if (inventoryScanner.Scanned)
                {
                    slotToScan = -1;
                }
                else
                {
                    inventoryScanner.Update();
                }
            }
            else if (gameEngine.Player.BlobsState == enBlobState.Dead)
            {
                blobLives = blobLives - 1;

                if (blobLives < 0)
                {
                    // That's all folks
                    gameEngine.Player.BlobsState = enBlobState.Gameover;
                    blobLives = 0;
                }
                else
                {
                    // Bring him back to life
                    EnergyLevel = 128;

                    // If he's out of platforms, give him some back
                    if (Platforms <= 8)
                    {
                        Platforms = 8;
                    }

                    gameEngine.Player.BlobsState = enBlobState.Normal;
                }
            }
        }

        public void ScanSlot(int SlotNo)
        {
            if (SlotNo >= 0 && SlotNo <= 3)// && slotToScan == -1)
            {
                if (inventoryScanner.ScanStarted == false ||
                    inventoryScanner.Scanned)
                {
                    inventoryScanner.Reset(true);
                    inventoryScanner.StartScan();
                    slotToScan = SlotNo;
                }
            }
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").DrawSprite(0, 0, 0, Color.White);
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").End();

            // Draw inventory
            if (blobsInventory != null)
            {
                // Draw items
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                for (int a = 0; a < blobsInventory.Length; a++)
                {
                    // Draw numbers above object
                    if (blobsInventory[a] == enCollectables.None)
                    {
                        panelNumbers.Draw((78 * a) + 700, 37, (a + 1).ToString(), gameEngine, gameTime, Color.White);
                    }
                    else
                    {
                        panelNumbers.Draw((78 * a) + 700, 37, (a + 1).ToString(), gameEngine, gameTime, Color.OrangeRed);

                        if (slotToScan != a) // Don't draw the slot being scanned
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite((int)blobsInventory[a], (78 * a) + 683, 78, Color.White);
                        }
                    }
                }
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }

            if (slotToScan >= 0)
            {
                inventoryScanner.Draw((int)blobsInventory[slotToScan], (78 * slotToScan) + 681, 72, gameTime);
            }

            // Draw energybars
            gameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").DrawSprite(0, 408, 45, (int)(energyLevel * 2), 30, Color.White);
            gameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").End();

            gameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").DrawSprite(0, 408, 78, (int)(platforms * 4), 30, Color.White);
            gameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").End();

            gameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").DrawSprite(0, 408, 110, (int)(firePower * 2), 30, Color.White);
            gameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").End();

            // Draw score
            panelNumbers.Draw(39, 38, playerScore.ToString("000000"), gameEngine, gameTime, Color.White);

            // Draw remaining lives
            panelNumbers.Draw(244, 113, blobLives.ToString("00"), gameEngine, gameTime, Color.White);

            // draw overlay
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").Begin(BlendState.AlphaBlend, gameTime);
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").DrawSprite(0, 22, 26, Color.White);
            gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").End();
        }
        #endregion
    }

    public class PanelNumbers
    {
        public void Draw(int xPos, int yPos, string score, GameEngine gameEngine, GameTime gameTime, Color color)
        {
            gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").Begin(BlendState.AlphaBlend, gameTime);
            for (int a = 0; a < score.Length; a++)
            {
                string subScore = score.Substring(a, 1);
                gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").DrawSprite(Int32.Parse(subScore), xPos + (31 * a), yPos, color);
            }
            gameEngine.ContentResourceManager.GetSpriteCollection("PanelNumbers").End();
        }
    }
}
