// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.ContentManagement;
using Starquake.GameComponents;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Display;

/// <summary>
///     The strip of HUD along the top: the score, the lives, the three supply bars and the four
///     inventory slots with their scanner.
/// </summary>
/// <remarks>
///     It used to own all of that as well as draw it - every scoring and power-up rule in the game
///     lived on a class named for a widget, which is why <c>Blob</c> read his own supplies off the
///     HUD. Those are <see cref="PlayerStats" /> now and this reads them to draw them.
/// </remarks>
internal class StatusPanel : GameComponentBase
{
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
    private readonly GraphicsSet _graphicsSet;


    private readonly KeyScanner _inventoryScanner;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;

    private readonly PlayerStats _stats;

    private Collectables[]? _blobsInventory;
    private int _slotToScan = -1;

    public int Width => _resources.GetSpriteCollection("StatusPanelGraphic").SpriteWidth;

    public int Height => _resources.GetSpriteCollection("StatusPanelGraphic").SpriteHeight;

    #region IGameComponent Members

    public StatusPanel(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        ScreenDirectory screenDirectory,
        Random randomNumberGenerator,
        PlayerStats stats)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _screenDirectory = screenDirectory;
        _stats = stats;

        _inventoryScanner = new KeyScanner(resources, graphicsSet, randomNumberGenerator, false);
    }

    public void Initialise()
    {

    }

    public void PerformPostChangeScreenActions()
    {
        throw new NotImplementedException();
    }


    /// <summary>
    ///     Steps the panel. Takes the blob rather than holding it, and is told whether the intro is
    ///     at a point where energy should drain, because the panel is built before either exists -
    ///     the blob needs the panel to read its supplies from, and the panel needs the blob to drain
    ///     and revive. Handing it over per call is what makes that pair acyclic.
    /// </summary>
    public void Update(Collectables[] blobsInventory, Blob blob, bool energyMayDrain)
    {
        if (blob.BlobsState == BlobState.Normal)
        {
            _blobsInventory = blobsInventory;

            // The blob is only drained while he is walking the planet - a teleporter, a booth or a
            // pyramid is a safe place to stand. PlayerStats decides whether that has killed him;
            // applying the answer to the blob is this class's job because it is the one holding one.
            if (_stats.DrainEnergy(energyMayDrain && _screenDirectory.Current == GameScreenType.MainGame))
            {
                blob.KillBlob(false, true);
                _stats.EnergyLevel = 0;
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
        else if (blob.BlobsState == BlobState.Dead)
        {
            blob.BlobsState = _stats.TakeALife() ? BlobState.Gameover : BlobState.Normal;
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
        SpriteCollection statusPanelGraphic = _resources.GetSpriteCollection("StatusPanelGraphic");
        statusPanelGraphic.Begin(BlendState.AlphaBlend);
        statusPanelGraphic.DrawSprite(0, 0, 0, Color.White);
        statusPanelGraphic.End();

        // Draw inventory
        if (_blobsInventory != null)
        {
            // Draw items
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).Begin(BlendState.AlphaBlend);
            for (int a = 0; a < _blobsInventory.Length; a++)
            {
                // Draw numbers above object
                if (_blobsInventory[a] == Collectables.None)
                {
                    PanelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), _resources, Color.White);
                }
                else
                {
                    PanelNumbers.Draw(InventorySlotSpacing * a + InventoryNumberXPos, InventoryNumberYPos, (a + 1).ToString(), _resources, Color.OrangeRed);

                    if (_slotToScan != a) // Don't draw the slot being scanned
                    {
                        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite((int)_blobsInventory[a], InventorySlotSpacing * a + InventoryItemXPos, InventoryItemYPos, Color.White);
                    }
                }
            }
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();
        }

        if (_slotToScan >= 0 && _blobsInventory != null)
        {
            _inventoryScanner.Draw((int)_blobsInventory[_slotToScan], InventorySlotSpacing * _slotToScan + InventoryScannerXPos, InventoryScannerYPos);
        }

        // Draw energybars
        SpriteCollection enerygyBar = _resources.GetSpriteCollection("EnerygyBar");
        enerygyBar.Begin(BlendState.AlphaBlend);
        enerygyBar.DrawSprite(0, BarXPos, EnergyBarYPos, (int)(_stats.EnergyLevel * EnergyBarScale), BarHeight, Color.White);
        enerygyBar.End();

        SpriteCollection jumpBar = _resources.GetSpriteCollection("JumpBar");
        jumpBar.Begin(BlendState.AlphaBlend);
        jumpBar.DrawSprite(0, BarXPos, JumpBarYPos, (int)(_stats.Platforms * JumpBarScale), BarHeight, Color.White);
        jumpBar.End();

        SpriteCollection ammoBar = _resources.GetSpriteCollection("AmmoBar");
        ammoBar.Begin(BlendState.AlphaBlend);
        ammoBar.DrawSprite(0, BarXPos, AmmoBarYPos, (int)(_stats.FirePower * AmmoBarScale), BarHeight, Color.White);
        ammoBar.End();

        // Draw score
        PanelNumbers.Draw(ScoreXPos, ScoreYPos, _stats.PlayerScore.ToString("000000"), _resources, Color.White);

        // Draw remaining lives
        PanelNumbers.Draw(LivesXPos, LivesYPos, _stats.BlobLives.ToString("00"), _resources, Color.White);

        // draw overlay
        SpriteCollection statusPanelOverlay = _resources.GetSpriteCollection("StatusPanelOverlay");
        statusPanelOverlay.Begin(BlendState.AlphaBlend);
        statusPanelOverlay.DrawSprite(0, 22, 26, Color.White);
        statusPanelOverlay.End();
    }

    #endregion

}
