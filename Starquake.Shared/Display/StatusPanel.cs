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
    /// <summary>
    ///     How much of each supply the blob can hold, and how much it starts a life with. The bars on
    ///     the panel are drawn full at these, so a pick-up that would take a supply over its ceiling
    ///     is simply capped.
    /// </summary>
    private const int MaxFirePower = 128;

    /// <inheritdoc cref="MaxFirePower" />
    private const int MaxEnergyLevel = 128;

    /// <inheritdoc cref="MaxFirePower" />
    private const int MaxPlatforms = 64;

    /// <inheritdoc cref="MaxFirePower" />
    private const int StartingPlatforms = 32;

    /// <summary>
    ///     What each power-up is worth. There are three sizes of each, plus a top-up that gives
    ///     whichever supply is running lowest and an extra life.
    /// </summary>
    private const int SmallTopUp = 32;

    /// <inheritdoc cref="SmallTopUp" />
    private const int MediumTopUp = 64;

    /// <inheritdoc cref="SmallTopUp" />
    private const int LargeTopUp = 96;

    /// <inheritdoc cref="SmallTopUp" />
    private const int FullTopUp = 128;

    /// <summary>
    ///     The power-ups, in the order the tile sheet holds them.
    /// </summary>
    private const int PowerUpEnergyMedium = 0;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpEnergySmall = 1;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpEnergyFull = 2;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpFirePowerMedium = 3;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpFirePowerSmall = 4;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpPlatforms = 5;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpWhateverIsLowest = 6;

    /// <inheritdoc cref="PowerUpEnergyMedium" />
    private const int PowerUpExtraLife = 7;

    /// <summary>
    ///     A platform costs twice what a shot does, so the top-up power-up weighs the platform count
    ///     double when deciding which supply is running lowest.
    /// </summary>
    private const int PlatformCost = 2;

    /// <summary>
    ///     The inventory strip: how far apart the slots are, and where the first one starts. The slot
    ///     number, the item in it, and the scanner that sweeps it are each drawn at their own offset
    ///     within the slot.
    /// </summary>
    private const int InventorySlotSpacing = 78;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryNumberXPos = 700;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryNumberYPos = 37;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryItemXPos = 683;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryItemYPos = 78;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryScannerXPos = 681;

    /// <inheritdoc cref="InventorySlotSpacing" />
    private const int InventoryScannerYPos = 72;

    /// <summary>
    ///     The three supply bars. They share a left edge and a height, and each is scaled from its
    ///     supply by however much of the bar one unit of it is worth.
    /// </summary>
    private const int BarXPos = 408;

    /// <inheritdoc cref="BarXPos" />
    private const int BarHeight = 30;

    /// <inheritdoc cref="BarXPos" />
    private const int EnergyBarYPos = 45;

    /// <inheritdoc cref="BarXPos" />
    private const int JumpBarYPos = 78;

    /// <inheritdoc cref="BarXPos" />
    private const int AmmoBarYPos = 110;

    /// <inheritdoc cref="BarXPos" />
    private const int EnergyBarScale = 2;

    /// <inheritdoc cref="BarXPos" />
    private const int JumpBarScale = 4;

    /// <inheritdoc cref="BarXPos" />
    private const int AmmoBarScale = 2;

    /// <summary>
    ///     Where the score and the lives remaining are printed.
    /// </summary>
    private const int ScoreXPos = 39;

    /// <inheritdoc cref="ScoreXPos" />
    private const int ScoreYPos = 38;

    /// <inheritdoc cref="ScoreXPos" />
    private const int LivesXPos = 244;

    /// <inheritdoc cref="ScoreXPos" />
    private const int LivesYPos = 113;


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
            if (_firePower > MaxFirePower)
            {
                _firePower = MaxFirePower;
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

            if (_energyLevel > MaxEnergyLevel)
            {
                _energyLevel = MaxEnergyLevel;
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

            if (_platforms > MaxPlatforms)
            {
                _platforms = MaxPlatforms;
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
        _firePower = MaxFirePower;
        _energyLevel = MaxEnergyLevel;
        _platforms = StartingPlatforms;
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

            if (powerUpType == PowerUpEnergyMedium)
            {
                EnergyLevel = EnergyLevel + MediumTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == PowerUpEnergySmall)
            {
                EnergyLevel = EnergyLevel + SmallTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == PowerUpEnergyFull)
            {
                EnergyLevel = EnergyLevel + FullTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
            }
            else if (powerUpType == PowerUpFirePowerMedium)
            {
                FirePower = FirePower + MediumTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
            }
            else if (powerUpType == PowerUpFirePowerSmall)
            {
                FirePower = FirePower + SmallTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
            }
            else if (powerUpType == PowerUpPlatforms)
            {
                Platforms = Platforms + SmallTopUp;
                GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp1", false);
            }
            else if (powerUpType == PowerUpWhateverIsLowest)
            {
                if (BlobLives == 0)
                {
                    BlobLives++;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("LifePowerUp", false); // Extra life
                }
                else if (
                    Platforms * PlatformCost <= EnergyLevel && // Top Up Jump Platforms
                    Platforms * PlatformCost <= FirePower)
                {
                    Platforms = Platforms + SmallTopUp;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp1", false);
                }
                else if (
                    FirePower <= Platforms * PlatformCost && // Fire power
                    FirePower <= EnergyLevel)
                {
                    FirePower = FirePower + MediumTopUp;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
                }
                else if (
                    EnergyLevel <= Platforms * PlatformCost && // Energy
                    EnergyLevel <= FirePower)
                {
                    EnergyLevel = EnergyLevel + LargeTopUp;
                    GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
                }
            }
            else if (powerUpType == PowerUpExtraLife)
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
                EnergyLevel = MaxEnergyLevel;

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
                    _panelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), GameEngine, gameTime, Color.White);
                }
                else
                {
                    _panelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), GameEngine, gameTime, Color.OrangeRed);

                    if (_slotToScan != a) // Don't draw the slot being scanned
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).DrawSprite((int)_blobsInventory[a], InventorySlotSpacing * a + InventoryItemXPos, InventoryItemYPos, Color.White);
                    }
                }
            }
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).End();
        }

        if (_slotToScan >= 0 && _blobsInventory != null)
        {
            _inventoryScanner.Draw((int)_blobsInventory[_slotToScan], InventorySlotSpacing * _slotToScan + InventoryScannerXPos, InventoryScannerYPos, gameTime);
        }

        // Draw energybars
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").DrawSprite(0, BarXPos, EnergyBarYPos, (int)(_energyLevel * EnergyBarScale), BarHeight, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("EnerygyBar").End();

        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").DrawSprite(0, BarXPos, JumpBarYPos, (int)(_platforms * JumpBarScale), BarHeight, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("JumpBar").End();

        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").DrawSprite(0, BarXPos, AmmoBarYPos, (int)(_firePower * AmmoBarScale), BarHeight, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("AmmoBar").End();

        // Draw score
        _panelNumbers.Draw(ScoreXPos, ScoreYPos, PlayerScore.ToString("000000"), GameEngine, gameTime, Color.White);

        // Draw remaining lives
        _panelNumbers.Draw(LivesXPos, LivesYPos, BlobLives.ToString("00"), GameEngine, gameTime, Color.White);

        // draw overlay
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").DrawSprite(0, 22, 26, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").End();
    }

    #endregion

}