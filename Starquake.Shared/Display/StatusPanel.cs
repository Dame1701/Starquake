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
    private readonly GameEngine GameEngine;

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

    private Collectables[]? _blobsInventory;

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
    {
        GameEngine = gameEngineRef;

        _inventoryScanner = new KeyScanner(gameEngineRef.ContentResourceManager, gameEngineRef.GraphicsSet, gameEngineRef.RandomNumberGenerator, false);
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

    public void BlobHasPerformedAnAction(BlobAction action, object? data)
    {
        switch (action)
        {
            case BlobAction.FiredLazer:
            {
                FirePower--;

                break;
            }
            case BlobAction.LaidPlatform:
            {
                Platforms--;

                break;
            }
            case BlobAction.GotPowerUp:
            {
                // The only action that carries anything, and what it carries is the power up's type
                if (data is not int powerUpType)
                {
                    throw new ArgumentException(
                        "A power up action needs the type of power up collected.", nameof(data));
                }

                CollectPowerUp(powerUpType);

                break;
            }
        }
    }

    private void CollectPowerUp(int powerUpType)
    {
        if (powerUpType == PowerUpEnergyMedium)
        {
            TopUpEnergy(MediumTopUp);
        }
        else if (powerUpType == PowerUpEnergySmall)
        {
            TopUpEnergy(SmallTopUp);
        }
        else if (powerUpType == PowerUpEnergyFull)
        {
            TopUpEnergy(FullTopUp);
        }
        else if (powerUpType == PowerUpFirePowerMedium)
        {
            TopUpFirePower(MediumTopUp);
        }
        else if (powerUpType == PowerUpFirePowerSmall)
        {
            TopUpFirePower(SmallTopUp);
        }
        else if (powerUpType == PowerUpPlatforms)
        {
            TopUpPlatforms(SmallTopUp);
        }
        else if (powerUpType == PowerUpWhateverIsLowest)
        {
            TopUpWhateverIsLowest();
        }
        else if (powerUpType == PowerUpExtraLife)
        {
            GiveExtraLife();
        }
    }

    /// <summary>
    ///     The wildcard power up. A life if the blob has none left, otherwise whichever of his three
    ///     supplies he is shortest of - and platforms are compared at their energy cost rather than
    ///     their count, since one platform is worth several units of anything else.
    /// </summary>
    private void TopUpWhateverIsLowest()
    {
        if (BlobLives == 0)
        {
            GiveExtraLife();

            return;
        }

        var platformsWorth = Platforms * PlatformCost;

        if (platformsWorth <= EnergyLevel && platformsWorth <= FirePower)
        {
            TopUpPlatforms(SmallTopUp);
        }
        else if (FirePower <= platformsWorth && FirePower <= EnergyLevel)
        {
            TopUpFirePower(MediumTopUp);
        }
        else if (EnergyLevel <= platformsWorth && EnergyLevel <= FirePower)
        {
            TopUpEnergy(LargeTopUp);
        }
    }

    private void TopUpEnergy(int amount)
    {
        EnergyLevel += amount;
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp2", false);
    }

    private void TopUpFirePower(int amount)
    {
        FirePower += amount;
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp3", 0.5f, false);
    }

    private void TopUpPlatforms(int amount)
    {
        Platforms += amount;
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("PowerUp1", false);
    }

    private void GiveExtraLife()
    {
        BlobLives++;
        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("LifePowerUp", false);
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
                if (GameEngine.ScreenDirectory.Current == GameScreenType.MainGame)
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
        if (slotNo is >= 0 and <= 3) // && slotToScan == -1)
        {
            if (_inventoryScanner is
                {
                    ScanStarted: true,
                    Scanned: false
                })
            {
                return;
            }
            _inventoryScanner.Reset(true);
            _inventoryScanner.StartScan();
            _slotToScan = slotNo;
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
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.Pickups).Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            for (var a = 0; a < _blobsInventory.Length; a++)
            {
                // Draw numbers above object
                if (_blobsInventory[a] == Collectables.None)
                {
                    PanelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), GameEngine.ContentResourceManager, gameTime, Color.White);
                }
                else
                {
                    PanelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), GameEngine.ContentResourceManager, gameTime, Color.OrangeRed);

                    if (_slotToScan != a) // Don't draw the slot being scanned
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.Pickups).DrawSprite((int)_blobsInventory[a], InventorySlotSpacing * a + InventoryItemXPos, InventoryItemYPos, Color.White);
                    }
                }
            }
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.GraphicsSet.Names.Pickups).End();
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
        PanelNumbers.Draw(ScoreXPos, ScoreYPos, PlayerScore.ToString("000000"), GameEngine.ContentResourceManager, gameTime, Color.White);

        // Draw remaining lives
        PanelNumbers.Draw(LivesXPos, LivesYPos, BlobLives.ToString("00"), GameEngine.ContentResourceManager, gameTime, Color.White);

        // draw overlay
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").Begin(BlendState.AlphaBlend, gameTime);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").DrawSprite(0, 22, 26, Color.White);
        GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelOverlay").End();
    }

    #endregion

}
