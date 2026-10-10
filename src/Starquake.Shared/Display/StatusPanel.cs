// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.GameComponents;
using Starquake.Scoring;
using Starquake.Sprites;
using Starquake.Tuning;

namespace Starquake.Display;

/// <summary>
///     The strip of HUD along the top: the score, the lives, the three supply bars and the four
///     inventory slots with their scanner.
/// </summary>
/// <remarks>
///     Draws only. The scoring and power-up rules, and the supplies themselves, belong to
///     <see cref="PlayerStats" />, which this reads.
/// </remarks>
internal class StatusPanel(ResourceManager resources,
    GraphicsSet graphicsSet,
    ScreenDirectory screenDirectory,
    Random randomNumberGenerator,
    PlayerStats stats)
{
    /// <summary>
    ///     The inventory strip: how far apart the slots are and where the first starts. The slot
    ///     number, the item in it and the scanner sweeping it are each drawn at their own offset
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
    ///     supply by how much of the bar one unit is worth.
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

    /// <summary>Where the score and the lives remaining are printed.</summary>
    private const int ScoreXPos = 39;

    /// <inheritdoc cref="ScoreXPos" />
    private const int ScoreYPos = 38;

    /// <inheritdoc cref="ScoreXPos" />
    private const int LivesXPos = 244;

    /// <inheritdoc cref="ScoreXPos" />
    private const int LivesYPos = 113;

    /// <summary>Where the overlay sits. The only panel graphic not drawn at the origin.</summary>
    private const int OverlayXPos = 22;

    /// <inheritdoc cref="OverlayXPos" />
    private const int OverlayYPos = 26;


    private readonly KeyScanner _inventoryScanner = new(resources, graphicsSet, randomNumberGenerator, false);

    private Collectables[]? _blobsInventory;
    private int _slotToScan = -1;

    /// <summary>
    ///     The slot the core has picked out, or -1 for none. Drawn glowing and shaking, so a match
    ///     shows at both ends, in Blob's hands and in the core, rather than a part vanishing from one
    ///     and appearing in the other.
    /// </summary>
    private int _slotIdentified = -1;

    /// <inheritdoc cref="_slotIdentified" />
    private int _identifiedFrame;

    public int Width => resources.GetSpriteCollection(Assets.Graphics.StatusPanelGraphic).SpriteWidth;

    public int Height => resources.GetSpriteCollection(Assets.Graphics.StatusPanelGraphic).SpriteHeight;

    /// <summary>
    ///     Steps the panel and says what must happen to Blob as a result. It is told Blob's state and
    ///     whether the intro is at a point where energy should drain, because the panel is built
    ///     first: Blob reads his supplies from the panel, so the panel cannot hold Blob.
    /// </summary>
    /// <remarks>
    ///     Returns a <see cref="BlobInstruction" /> rather than acting on Blob, so this names no type
    ///     from <c>ActiveSprites</c>. The engine holds both and applies the answer.
    /// </remarks>
    public BlobInstruction Update(Collectables[] blobsInventory, BlobState blobsState, bool energyMayDrain)
    {
        if (blobsState == BlobState.Normal)
        {
            _blobsInventory = blobsInventory;

            BlobInstruction instruction = BlobInstruction.None;

            // Blob is only drained while walking the planet. A teleporter, a booth or a pyramid is a
            // safe place to stand. PlayerStats decides whether the drain has killed him.
            if (stats.DrainEnergy(energyMayDrain && screenDirectory.Current == GameScreenType.MainGame))
            {
                stats.EnergyLevel = 0;
                instruction = BlobInstruction.Overload;
            }

            if (_inventoryScanner.Scanned)
            {
                _slotToScan = -1;
            }
            else
            {
                _inventoryScanner.Update();
            }

            return instruction;
        }

        if (blobsState != BlobState.Dead)
        {
            return BlobInstruction.None;
        }

        return stats.TakeALife() ? BlobInstruction.EndTheGame : BlobInstruction.Revive;
    }

    /// <summary>
    ///     Says which inventory slot the core is drawing a part out of, or -1 when it is not.
    /// </summary>
    public void IdentifySlot(int slotNo)
    {
        if (slotNo != _slotIdentified)
        {
            _identifiedFrame = 0;
        }

        _slotIdentified = slotNo;
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

    public void Draw()
    {
        DrawPanelGraphic(Assets.Graphics.StatusPanelGraphic);

        DrawInventory();
        DrawInventoryScanner();

        DrawBar(Assets.Graphics.EnergyBar, EnergyBarYPos, stats.EnergyLevel * EnergyBarScale);
        DrawBar(Assets.Graphics.JumpBar, JumpBarYPos, stats.Platforms * JumpBarScale);
        DrawBar(Assets.Graphics.AmmoBar, AmmoBarYPos, stats.FirePower * AmmoBarScale);

        PanelNumbers.Draw(ScoreXPos, ScoreYPos, stats.PlayerScore.ToString("000000"), resources, Color.White);
        PanelNumbers.Draw(LivesXPos, LivesYPos, stats.BlobLives.ToString("00"), resources, Color.White);

        DrawPanelGraphic(Assets.Graphics.StatusPanelOverlay, OverlayXPos, OverlayYPos);
    }

    /// <summary>
    ///     The panel's own artwork: the backdrop everything else is drawn on, and the overlay laid
    ///     over the finished panel.
    /// </summary>
    private void DrawPanelGraphic(string assetName, int xPos = 0, int yPos = 0)
    {
        SpriteCollection graphic = resources.GetSpriteCollection(assetName);

        graphic.Begin(BlendState.AlphaBlend);
        graphic.DrawSprite(0, xPos, yPos, Color.White);
        graphic.End();
    }

    /// <summary>
    ///     One of the three bars down the middle of the panel. Each is a single sprite stretched to
    ///     the level it shows, so they differ only in artwork, the row they sit on, and how many
    ///     pixels one unit is worth.
    /// </summary>
    private void DrawBar(string assetName, int yPos, double level)
    {
        SpriteCollection bar = resources.GetSpriteCollection(assetName);

        bar.Begin(BlendState.AlphaBlend);
        bar.DrawSprite(0, BarXPos, yPos, (int)level, BarHeight, Color.White);
        bar.End();
    }

    /// <summary>
    ///     The inventory slots, each numbered above. A full slot has its number in orange with the
    ///     item below; an empty one has a white number and nothing else. The slot being scanned is
    ///     left out, because <see cref="DrawInventoryScanner" /> draws that one.
    /// </summary>
    private void DrawInventory()
    {
        if (_blobsInventory == null)
        {
            return;
        }

        SpriteCollection pickups = resources.GetSpriteCollection(graphicsSet.Names.Pickups);

        pickups.Begin(BlendState.AlphaBlend);

        for (int slot = 0; slot < _blobsInventory.Length; slot++)
        {
            bool holdingSomething = _blobsInventory[slot] != Collectables.None;

            PanelNumbers.Draw(
                InventorySlotSpacing * slot + InventoryNumberXPos,
                InventoryNumberYPos,
                (slot + 1).ToString(),
                resources,
                holdingSomething ? Color.OrangeRed : Color.White);

            if (!holdingSomething || _slotToScan == slot)
            {
                continue;
            }

            // The slot the core has picked out rattles and glows while the others sit still.
            Vector2 shake = slot == _slotIdentified
                ? CoreReplacementSettings.ShakeAt(_identifiedFrame)
                : Vector2.Zero;

            pickups.DrawSprite(
                (int)_blobsInventory[slot],
                InventorySlotSpacing * slot + InventoryItemXPos + (int)shake.X,
                InventoryItemYPos + (int)shake.Y,
                Color.White);
        }

        pickups.End();

        DrawIdentifiedSlotGlow();
    }

    /// <summary>
    ///     The light rising and falling on the part the core is drawing out, laid over the plain
    ///     artwork so the pulse reads as light on the part rather than a tint of it.
    /// </summary>
    private void DrawIdentifiedSlotGlow()
    {
        if (_slotIdentified < 0 || _blobsInventory == null ||
            _blobsInventory[_slotIdentified] == Collectables.None)
        {
            return;
        }

        _identifiedFrame++;

        Vector2 shake = CoreReplacementSettings.ShakeAt(_identifiedFrame);
        float pulse = CoreReplacementSettings.PulseAt(_identifiedFrame);

        SpriteCollection illuminated =
            resources.GetSpriteCollection(graphicsSet.Names.PickupsIlluminated);

        illuminated.Begin(BlendState.Additive);
        illuminated.DrawSprite(
            (int)_blobsInventory[_slotIdentified],
            InventorySlotSpacing * _slotIdentified + InventoryItemXPos + (int)shake.X,
            InventoryItemYPos + (int)shake.Y,
            Colours.WhiteFadedTo((byte)(pulse * CoreReplacementSettings.GlowAmount
                                             * ColourLevels.Full)));
        illuminated.End();
    }

    /// <summary>
    ///     The scan line running down the slot a door is reading, drawn in place of the item.
    /// </summary>
    private void DrawInventoryScanner()
    {
        if (_slotToScan < 0 || _blobsInventory == null)
        {
            return;
        }

        _inventoryScanner.Draw(
            (int)_blobsInventory[_slotToScan],
            InventorySlotSpacing * _slotToScan + InventoryScannerXPos,
            InventoryScannerYPos);
    }
}
