// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Structure;

namespace Starquake.Display;

public class StatusPanel : GameComponentBase
{

    private readonly KeyScanner _inventoryScanner;

    private readonly PanelNumbers _panelNumbers;

    private Collectables[] _blobsInventory;

    private double _energyLevel;
    private DateTime _energyTime;
    private double _firePower;

    private double _platforms;

    private bool _removeEnergyTimerStarted;
    private int _slotToScan = -1;

    public double FirePower
    {
        get => _firePower;
        set
        {
            _firePower = value;
            if (_firePower > 128)
            {
                _firePower = 128;
            }

            if (_firePower < 0)
            {
                _firePower = 0;
            }
        }
    }

    public double EnergyLevel
    {
        get => _energyLevel;
        set
        {
            _energyLevel = value;

            if (_energyLevel > 128)
            {
                _energyLevel = 128;
            }

            if (_energyLevel < 0)
            {
                _energyLevel = 0;
            }
        }
    }

    public double Platforms
    {
        get => _platforms;
        set
        {
            _platforms = value;

            if (_platforms > 64)
            {
                _platforms = 64;
            }

            if (_platforms < 0)
            {
                _platforms = 0;
            }
        }
    }

    public int BlobLives { get; set; }

    public int PlayerScore { get; set; }

    public int Width => GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").SpriteWidth;

    public int Height => GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").SpriteHeight;

    #region IGameComponent Members

    public StatusPanel(GameEngine gameEngineRef)
        : base(gameEngineRef)
    {
        _inventoryScanner = new KeyScanner(gameEngineRef, false);
        _panelNumbers = new PanelNumbers();
    }

    public void LoadContent(ContentList content)
    {
    }

    public void Initialise()
    {

    }

    public void Reset()
    {
        BlobLives = 4;
        _firePower = 128;
        _energyLevel = 128;
        _platforms = 32;
        PlayerScore = 0;
    }

    public void BlobHasPerformedAnAction(BlobAction action, object data)
    {
        if (action == BlobAction.FiredLazer)
        {
            FirePower--;
        }
        else if (action == BlobAction.LaidPlatform)
        {
            Platforms = Platforms - 1;
        }
        else if (action == BlobAction.GotPowerUp)
        {
            var powerUpType = (int)data;

            if (powerUpType == 0)
            {
                EnergyLevel = EnergyLevel + 64;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == 1)
            {
                EnergyLevel = EnergyLevel + 32;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == 2)
            {
                EnergyLevel = EnergyLevel + 128;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == 3)
            {
                FirePower = FirePower + 64;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
            }
            else if (powerUpType == 4)
            {
                FirePower = FirePower + 32;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
            }
            else if (powerUpType == 5)
            {
                Platforms = Platforms + 32;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp1", false);
            }
            else if (powerUpType == 6)
            {
                if (BlobLives == 0)
                {
                    BlobLives++;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("LifePowerUp", false); // Extra life
                }
                else if (
                    Platforms * 2 <= EnergyLevel && // Top Up Jump Platforms
                    Platforms * 2 <= FirePower)
                {
                    Platforms = Platforms + 32;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp1", false);
                }
                else if (
                    FirePower <= Platforms * 2 && // Fire power
                    FirePower <= EnergyLevel)
                {
                    FirePower = FirePower + 64;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
                }
                else if (
                    EnergyLevel <= Platforms * 2 && // Energy
                    EnergyLevel <= FirePower)
                {
                    EnergyLevel = EnergyLevel + 96;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
                }
            }
            else if (powerUpType == 7)
            {
                BlobLives++;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("LifePowerUp", false);
            }
        }
    }

    public void PerformPostChangeScreenActions()
    {
        throw new NotImplementedException();
    }


    public void Update(Collectables[] blobsInventory)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            _blobsInventory = blobsInventory;

            if (!GameEngine.IntroSequence.SequenceRunning || GameEngine.IntroSequence.SequenceStage == IntroSequenceStage.EndPause)
            {
                // Delete Blob'ss energy one bar every 4 seconds, but only if we're not in a teleporter
                if (GameEngine.CurrentGameScreen == GameScreenType.MainGame)
                {
                    if (!_removeEnergyTimerStarted)
                    {
                        _energyTime = DateTime.Now;
                        _removeEnergyTimerStarted = true;
                    }
                    else if (_removeEnergyTimerStarted)
                    {
                        if (DateTime.Now - _energyTime > new TimeSpan(0, 0, 4))
                        {
                            _removeEnergyTimerStarted = false;
                            _energyLevel = _energyLevel - 1;
                        }
                    }
                }

                if (GameEngine.BlobStatusPanel.EnergyLevel <= 0)
                {
                    // Lose a life and put us into the overloading state
                    GameEngine.Player.KillBlob(false, true);
                    GameEngine.BlobStatusPanel.EnergyLevel = 0;
                }
            }

            if (_inventoryScanner.Scanned)
            {
                _slotToScan = -1;
            }
            else
            {
                _inventoryScanner.Update();
            }
        }
        else if (GameEngine.Player.BlobsState == BlobState.Dead)
        {
            BlobLives -= 1;

            if (BlobLives < 0)
            {
                // That's all folks
                GameEngine.Player.BlobsState = BlobState.Gameover;
                BlobLives = 0;
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

                GameEngine.Player.BlobsState = BlobState.Normal;
            }
        }
    }

    public void ScanSlot(int slotNo)
    {
        if (slotNo >= 0 && slotNo <= 3) // && slotToScan == -1)
        {
            if (!_inventoryScanner.ScanStarted ||
                _inventoryScanner.Scanned)
            {
                _inventoryScanner.Reset(true);
                _inventoryScanner.StartScan();
                _slotToScan = slotNo;
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").DrawSprite(0, 0, 0, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelGraphic").End();

        // Draw inventory
        if (_blobsInventory != null)
        {
            // Draw items
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            for (var a = 0; a < _blobsInventory.Length; a++)
            {
                // Draw numbers above object
                if (_blobsInventory[a] == Collectables.None)
                {
                    _panelNumbers.Draw(78 * a + 700, 37, (a + 1).ToString(), GameEngine, gameTime, Color.White);
                }
                else
                {
                    _panelNumbers.Draw(78 * a + 700, 37, (a + 1).ToString(), GameEngine, gameTime, Color.OrangeRed);

                    if (_slotToScan != a) // Don't draw the slot being scanned
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).DrawSprite((int)_blobsInventory[a], 78 * a + 683, 78, Color.White);
                    }
                }
            }
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).End();
        }

        if (_slotToScan >= 0)
        {
            _inventoryScanner.Draw((int)_blobsInventory[_slotToScan], 78 * _slotToScan + 681, 72, gameTime);
        }

        // Draw energybars
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").DrawSprite(0, 408, 45, (int)(_energyLevel * 2), 30, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").End();

        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").DrawSprite(0, 408, 78, (int)(_platforms * 4), 30, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").End();

        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").DrawSprite(0, 408, 110, (int)(_firePower * 2), 30, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").End();

        // Draw score
        _panelNumbers.Draw(39, 38, PlayerScore.ToString("000000"), GameEngine, gameTime, Color.White);

        // Draw remaining lives
        _panelNumbers.Draw(244, 113, BlobLives.ToString("00"), GameEngine, gameTime, Color.White);

        // draw overlay
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").DrawSprite(0, 22, 26, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").End();
    }

    #endregion

}