// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Emitters;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Fonts;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.Display;

/// <summary>
///     Draws one screen of the planet: its two layers of tiles, whatever is lying on it, the force
///     field across a key-locked door, and Olly.
/// </summary>
/// <remarks>
///     This was 400 lines of <c>GameScreenCollection</c>, which is named for what it holds rather
///     than for drawing it. Taking it out leaves that class as the screen array, the screen number,
///     and the things that answer questions about them, which is what its name has always claimed.
///     <para>
///         There is no test payoff here and none was expected: drawing needs a graphics device, so
///         none of this becomes reachable from a test. What it does buy is a place for the tile
///         batching to go - <c>DrawLowerTile</c> and <c>DrawUpperTilePatch</c> run 48 times a frame
///         each and open and close a sprite batch every time, which is phase 5's first item and is
///         now a change to one class.
///     </para>
/// </remarks>
internal class ScreenRenderer
{
    /// <summary>
    ///     Anything numbered above this is scenery drawn on the upper layer rather than a tile Blob
    ///     can stand on, so the collision map skips it.
    /// </summary>
    private const int LastCollidableTile = (int)ActionTiles.ShuttleTag2;

    /// <summary>
    ///     The eight lower tiles that cast a shadow onto the tile below them, and the upper-layer
    ///     sprite that shadow is drawn with. They come in pairs with a gap between each pair, which is
    ///     why this is eight numbers rather than a range.
    /// </summary>
    private const int ShadowTile1 = 377;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile2 = 378;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile3 = 380;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile4 = 381;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile5 = 385;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile6 = 386;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile7 = 388;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowTile8 = 389;

    /// <inheritdoc cref="ShadowTile1" />
    private const int ShadowSprite = 8;

    /// <summary>
    ///     The two upper-layer sprites that cover a tunnel mouth. Every mouth tile of a side maps to
    ///     the same one.
    /// </summary>
    private const int LeftTunnelMouthSprite = 11;

    /// <inheritdoc cref="LeftTunnelMouthSprite" />
    private const int RightTunnelMouthSprite = 12;

    /// <summary>
    ///     Olly, the eye that watches Blob from the wall of one particular screen, and the screen
    ///     it lives on. Its pupil tracks Blob across the room at a fraction of Blob's speed.
    /// </summary>
    private const int OllyScreenNumber = 477;

    /// <inheritdoc cref="OllyScreenNumber" />
    private const int OllyEyesRestX = 486;

    /// <inheritdoc cref="OllyScreenNumber" />
    private const int OllyEyesRestY = 206;

    /// <inheritdoc cref="OllyScreenNumber" />
    private const int OllyGazeCentreX = 350;

    /// <inheritdoc cref="OllyScreenNumber" />
    private const int OllyGazeDamping = 44;

    /// <inheritdoc cref="OllyScreenNumber" />
    private const int OllyVerticalGazeDamping = 20;

    /// <inheritdoc cref="ForceFieldEmitterNames" />
    private const int DimFieldQuantity = 100;

    /// <inheritdoc cref="ForceFieldEmitterNames" />
    private const int BrightFieldQuantity = 10;

    /// <summary>
    ///     How solid that ink is. Short of opaque, because the letters the sign used to carry were
    ///     painted thinly enough to let the board show through them, and ink that covers it
    ///     completely reads as a label stuck on the sign rather than as part of it.
    /// </summary>
    private const int SignpostInkLevel = 190;

    /// <summary>
    ///     How far the code is written a second time, to thicken it. The font is a light one at the
    ///     size a board leaves for five letters, so a single pass comes out finer than the lettering
    ///     it stands in for; a pass offset by a pixel is the cheapest way to put the weight back.
    /// </summary>
    private const int SignpostStrokeWidening = 1;

    /// <summary>
    ///     The two emitters that make up a key door's force field, and the two quantities it is run at:
    ///     a dim alpha-blended pass for the body of the field and a bright additive one over the middle.
    /// </summary>
    private static readonly string[] ForceFieldEmitterNames = ["Emitter01", "Emitter05"];

    /// <summary>
    ///     The colour a signpost's code is written in. The board the artwork draws is gold, and black
    ///     on it is what reads from across a room.
    /// </summary>
    private static readonly Color SignpostInk = Color.Black;


    private readonly ColourAnimationEngine _colourAnimations;
    private readonly GraphicsSet _graphicsSet;
    private readonly IStandardParticleEffect _keyDoorForceFieldNoAccess;
    private readonly ResourceManager _resources;
    private readonly GameScreenData[] _screens;
    private readonly BitmapFont _signpostFont;

    /// <summary>
    ///     Where Olly's eyes are looking. He is the face in the backdrop, and his eyes follow Blob
    ///     about the screen.
    /// </summary>
    private int _ollyEyesX;
    private int _ollyEyesY;

    public ScreenRenderer(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        GameScreenData[] screens,
        ColourAnimationEngine colourAnimations,
        IStandardParticleEffect keyDoorForceField,
        BitmapFont signpostFont)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _screens = screens;
        _colourAnimations = colourAnimations;
        _keyDoorForceFieldNoAccess = keyDoorForceField;
        _signpostFont = signpostFont;
    }

    /// <summary>
    ///     Which screen is being drawn. Set by the collection that owns the screens, at the same
    ///     moment it tells the collision map.
    /// </summary>
    public int ScreenNumber { get; set; }

    /// <summary>
    ///     The doors on this screen whose force field is drawn. Handed over by the collection, which
    ///     owns the key-door state itself - this only paints it.
    /// </summary>
    public List<KeyLockDoor>? KeyLockDoors { get; set; }

    /// <summary>
    ///     Runs one frame of the force field across a key-locked door. Drawn twice: a dim grey pass
    ///     for the body of it, then a bright additive pass over the middle, which is what gives it
    ///     its core. It turns green when Blob is carrying the key that opens the door.
    /// </summary>
    public void AnimateForceField(KeyLockDoor keyLockDoor, GameTime gameTime, bool blobHasKey)
    {
        SetForceFieldEmitters(new Color(100, 100, 100), DimFieldQuantity, EmitterBlendMode.Alpha);

        for (int c = 0; c < 4; c++)
        {
            _keyDoorForceFieldNoAccess.Update(keyLockDoor.Location.X + 52 + c, keyLockDoor.Location.Y + 48, gameTime);
        }

        for (int c = 0; c < 4; c++)
        {
            _keyDoorForceFieldNoAccess.Update(keyLockDoor.Location.X + 76 + c, keyLockDoor.Location.Y + 48, gameTime);
        }

        SetForceFieldColour(blobHasKey ? new Color(0, ColourLevels.Full, 0) : new Color(ColourLevels.Full, 0, 0));

        SetForceFieldEmitters(null, BrightFieldQuantity, EmitterBlendMode.Add);

        for (int c = 0; c < 12; c++)
        {
            _keyDoorForceFieldNoAccess.Update(keyLockDoor.Location.X + 60 + c, keyLockDoor.Location.Y + 48, gameTime);
        }
    }

    /// <summary>
    ///     Starts the force field emitting, once per screen that has a key-locked door on it.
    /// </summary>
    public void StartForceField()
        => _keyDoorForceFieldNoAccess.Start();

    /// <summary>
    ///     Stops the force field and kills the particles it has in the air, for a screen with no door
    ///     on it to emit them.
    /// </summary>
    public void ClearForceField()
        => _keyDoorForceFieldNoAccess.Reset();

    /// <summary>
    ///     The force field is two emitters that are always set together, so this sets both.
    ///     <paramref name="colour" /> is null to leave the colour as it is.
    /// </summary>
    private void SetForceFieldEmitters(Color? colour, int quantity, EmitterBlendMode blendMode)
    {
        foreach (string name in ForceFieldEmitterNames)
        {
            Emitter? emitter = _keyDoorForceFieldNoAccess!.Effect[name];

            if (colour is { } releaseColour)
            {
                emitter.ReleaseColour = releaseColour.ToVector3();
            }

            emitter.ReleaseQuantity = quantity;
            emitter.BlendMode = blendMode;
        }
    }

    /// <inheritdoc cref="SetForceFieldEmitters" />
    private void SetForceFieldColour(Color colour)
    {
        foreach (string name in ForceFieldEmitterNames)
        {
            _keyDoorForceFieldNoAccess!.Effect[name].ReleaseColour = colour.ToVector3();
        }
    }

    /// <summary>
    ///     Olly's eyes follow Blob about, damped so they drift rather than snap.
    /// </summary>
    public void TrackBlobWithOllysEyes(int blobXPos, int blobYPos)
    {
        if (ScreenNumber != OllyScreenNumber)
        {
            return;
        }

        _ollyEyesX = OllyEyesRestX + (blobXPos - OllyGazeCentreX) / OllyGazeDamping;
        _ollyEyesY = OllyEyesRestY + blobYPos / OllyVerticalGazeDamping;
    }

    public void Draw(Color colour, bool introSequenceRunning = false,
        IntroSequenceStage introSequenceStage = IntroSequenceStage.None)
    {
        DrawLowerTiles(colour);
        DrawTileAnimations(colour);
        DrawSignposts(colour);

        _colourAnimations.Draw();

        // The intro sequence flies over a screen whose objects have not been placed yet
        bool objectsArePlaced = !introSequenceRunning || introSequenceStage == IntroSequenceStage.EndPause;

        if (objectsArePlaced)
        {
            DrawPowerUp(colour);
            DrawPickUps(colour);
            DrawCheopsPyramid(colour);
        }

        DrawOllyBackground(colour);
    }

    /// <summary>
    ///     The whole lower layer, as one batch. Every tile on the screen comes off the same sheet, so
    ///     there is one <c>Begin</c>/<c>End</c> pair for the forty-eight of them rather than one each.
    /// </summary>
    /// <remarks>
    ///     A <c>Begin</c>/<c>End</c> pair flushes a draw call, so this used to issue up to 2,880 of
    ///     them a second to draw at most 48 sprites - batching is the entire purpose of
    ///     <c>SpriteBatch</c>. The sort mode is <c>Deferred</c>, which draws in the order the calls
    ///     were made: <c>Immediate</c> would flush per sprite and undo the batching, and
    ///     <c>BackToFront</c> sorts by a layer depth none of these set, which leaves the order among
    ///     them unspecified.
    /// </remarks>
    private void DrawLowerTiles(Color colour)
    {
        SpriteCollection tiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        tiles.Begin(BlendState.AlphaBlend);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tileImage = _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

                // The tunnel tiles and everything past them are drawn by their own engines, so the
                // lower layer stops short of them.
                if (IsDrawn(tileImage) && tileImage < (int)ActionTiles.AccessForceField)
                {
                    tiles.DrawSprite(tileImage, x * TileMetrics.TileWidth, y * TileMetrics.TileHeight, colour);
                }
            }
        }

        tiles.End();
    }

    /// <summary>
    ///     The animated patches that sit over a tile - a breakable bridge, a shuttle pad. Drawn after
    ///     the whole lower layer rather than tile by tile with it, which is the same picture: every
    ///     animated patch is at most one tile big, so none of them can be covered by a tile drawn later.
    /// </summary>
    private void DrawTileAnimations(Color colour)
    {
        AnimTileEngine animations = _resources.GetAnimationEngine();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tileImage = _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

                if (IsDrawn(tileImage))
                {
                    animations.Draw(tileImage, x * TileMetrics.TileWidth, y * TileMetrics.TileHeight, colour);
                }
            }
        }
    }

    /// <summary>
    ///     Writes the code the signposts on this screen advertise across the board each of them draws.
    ///     The whole screen shares one code, so it is looked up and laid out once however many signs
    ///     are standing on it.
    /// </summary>
    /// <remarks>
    ///     The code is fitted to the board rather than drawn at a size chosen here: as tall as the
    ///     board's lettering unless five letters of it would then run off the end, in which case it is
    ///     the width that decides. That is two sums rather than a constant because the only thing that
    ///     knows how wide a code comes out is the font, and a signpost has no room to be wrong in.
    /// </remarks>
    private void DrawSignposts(Color colour)
    {
        // The original sheet has nothing drawn at 364 at all - the signpost is the redrawn artwork's
        // own - so there is no board to write on and the code would hang in mid-air.
        if (_graphicsSet.IsOriginal || Signposts.CodeShownOn(_screens, ScreenNumber) is not { } code)
        {
            return;
        }

        float scale = Math.Min(
            (float)Signposts.TextHeight / _signpostFont.CapitalHeight,
            Signposts.TextWidth / _signpostFont.MeasureString(code).X);

        int left = Signposts.TextLeft + (int)Math.Round((Signposts.TextWidth - _signpostFont.MeasureString(code).X * scale) / 2);
        int top = Signposts.TextTop - (int)Math.Round(_signpostFont.CapitalTop * scale);

        // The screen's own fade carries the ink's, so that a sign dims with the room it stands in
        // rather than staying put while everything around it goes dark.
        byte ink = (byte)(colour.A * SignpostInkLevel / ColourLevels.Full);

        _signpostFont.Begin(BlendState.AlphaBlend);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (_screens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.TeleportSignpost)
                {
                    continue;
                }

                int textX = x * TileMetrics.TileWidth + left;
                int textY = y * TileMetrics.TileHeight + top;

                _signpostFont.WriteScaledText(textX, textY, code, SignpostInk, ink, scale);
                _signpostFont.WriteScaledText(textX + SignpostStrokeWidening, textY, code, SignpostInk, ink, scale);
            }
        }

        _signpostFont.End();
    }

    /// <summary>
    ///     Whether a tile is drawn at all. A <see cref="MarkerTile" /> is a position the author marked
    ///     rather than scenery, and the foot of a lift shaft is solid but deliberately invisible;
    ///     neither the tile nor any animation of it is drawn for any of them.
    /// </summary>
    private static bool IsDrawn(int tileImage)
        => tileImage > -1 &&
           tileImage != (int)ActionTiles.TunnelLiftTile &&
           !MarkerTiles.Contains(tileImage);

    private void DrawPowerUp(Color colour)
    {
        if (_screens[ScreenNumber].PowerUp is not { } powerUp)
        {
            return;
        }

        SpriteCollection powerUps = _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps);
        powerUps.Begin(BlendState.AlphaBlend);
        powerUps.DrawSprite(powerUp.Type, powerUp.XLocation, powerUp.YLocation, colour);
        powerUps.End();
    }

    private void DrawPickUps(Color colour)
    {
        if (_screens[ScreenNumber].PickUps == null)
        {
            return;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
            .Begin(BlendState.AlphaBlend);
        foreach (InteractiveItem pickUp in _screens[ScreenNumber].PickUps)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite(
                pickUp.Type, pickUp.XLocation,
                pickUp.YLocation, colour);
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();
    }

    private void DrawCheopsPyramid(Color colour)
    {
        if (_screens[ScreenNumber].CheopsPyramid is not { } pyramid)
        {
            return;
        }

        SpriteCollection sprites = _resources.GetSpriteCollection(
            _graphicsSet.IsOriginal ? "CheopsPyramidOriginal" : "CheopsPyramid");

        sprites.Begin(BlendState.AlphaBlend);
        sprites.DrawSprite(0, pyramid.XLocation, pyramid.YLocation, colour);
        sprites.End();
    }

    /// <summary>
    ///     Olly has no backdrop in the original graphics, which is why the old set skips this.
    /// </summary>
    private void DrawOllyBackground(Color colour)
    {
        if (ScreenNumber != OllyScreenNumber || _graphicsSet.IsOriginal)
        {
            return;
        }

        SpriteCollection ollyBackground = _resources.GetSpriteCollection(Assets.Graphics.OllyBackground);
        ollyBackground.Begin(BlendState.AlphaBlend);
        ollyBackground.DrawSprite(0, 382, 146, colour);
        ollyBackground.End();
    }

    public void DrawUpperLayer(Color colour)
    {
        DrawUpperTilePatches(colour);
        DrawTunnelEntrances(colour);

        DrawOlly(colour);
        DrawKeyDoorForceFields();

        _colourAnimations.DrawUpperLayer();
    }

    /// <summary>
    ///     Lays the upper-layer patch over one tile - the inside of a lift shaft, a booth, a tunnel mouth,
    ///     the shadow another tile casts. A tile with no patch is left as the lower layer drew it.
    /// </summary>
    /// <remarks>
    ///     One batch for the whole layer, as with the lower one. The patches and the tunnel entrances
    ///     come off different sheets and so are two passes rather than one interleaved loop; that is
    ///     the same picture because every patch is drawn on its own row or the one below it, never
    ///     above, so a tunnel entrance that shares a cell with one was always drawn after it anyway.
    /// </remarks>
    private void DrawUpperTilePatches(Color colour)
    {
        SpriteCollection upperTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);

        upperTiles.Begin(BlendState.AlphaBlend);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                if (UpperLayerPatchFor(_screens[ScreenNumber].ScreenTiles[x, y].TileImage)
                    is not var (sprite, rowOffset))
                {
                    continue;
                }

                upperTiles.DrawSprite(
                    sprite,
                    x * TileMetrics.TileWidth,
                    (y + rowOffset) * TileMetrics.TileHeight,
                    colour);
            }
        }

        upperTiles.End();
    }

    /// <summary>
    ///     Which sprite of the upper tile set covers a given lower tile, and how many rows below that tile
    ///     it is drawn. This was a fourteen-arm if/else ladder in which every arm made the same draw call
    ///     with a different sprite number.
    /// </summary>
    /// <remarks>
    ///     The row offset exists for one case. A shadow belongs to the tile above it, so it is recorded
    ///     against that tile and drawn a row further down; everything else draws in place.
    /// </remarks>
    private static (int Sprite, int RowOffset)? UpperLayerPatchFor(int tileImage)
    {
        if (tileImage is ShadowTile1 or
            ShadowTile2 or
            ShadowTile3 or
            ShadowTile4 or
            ShadowTile5 or
            ShadowTile6 or
            ShadowTile7 or
            ShadowTile8)
        {
            return (ShadowSprite, 1);
        }

        if (TunnelTiles.IsLeftMouth(tileImage))
        {
            return (LeftTunnelMouthSprite, 0);
        }

        if (TunnelTiles.IsRightMouth(tileImage))
        {
            return (RightTunnelMouthSprite, 0);
        }

        return (ActionTiles)tileImage switch
        {
            ActionTiles.TunnelLiftTopAlternate => (0, 0),
            ActionTiles.TunnelLiftTop1 => (0, 0),
            ActionTiles.TunnelLiftTile => (1, 0),
            ActionTiles.TunnelLiftShaft1 => (2, 0),
            ActionTiles.TunnelLiftShaft2 => (3, 0),
            ActionTiles.TunnelLiftShaft3 => (4, 0),
            ActionTiles.TunnelLiftTop2 => (5, 0),
            ActionTiles.TunnelLiftTop3 => (6, 0),
            ActionTiles.TeleportBooth => (7, 0),
            ActionTiles.AccessBoothLeft => (9, 0),
            ActionTiles.AccessBoothRight => (10, 0),
            _ => null
        };
    }

    /// <summary>
    ///     The tunnel mouths, which are lower-layer tiles numbered above everything Blob can stand
    ///     on and so are drawn over the upper layer rather than under it. A marker is numbered above
    ///     them again and is not a tile at all, which is why being past <see cref="LastCollidableTile" />
    ///     is no longer enough on its own.
    /// </summary>
    private void DrawTunnelEntrances(Color colour)
    {
        SpriteCollection tiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        tiles.Begin(BlendState.AlphaBlend);

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
            {
                int tileImage = _screens[ScreenNumber].ScreenTiles[x, y].TileImage;

                if (tileImage > LastCollidableTile && IsDrawn(tileImage))
                {
                    tiles.DrawSprite(tileImage, x * TileMetrics.TileWidth, y * TileMetrics.TileHeight, colour);
                }
            }
        }

        tiles.End();
    }

    private void DrawOlly(Color colour)
    {
        if (ScreenNumber == OllyScreenNumber)
        {
            if (_graphicsSet.IsOriginal)
            {
                SpriteCollection ollyOriginal = _resources.GetSpriteCollection(Assets.Graphics.OllyOriginal);
                ollyOriginal.Begin(BlendState.AlphaBlend);
                ollyOriginal.DrawSprite(0, 384, 100, colour);
                ollyOriginal.End();
            }
            else
            {
                SpriteCollection ollyLower = _resources.GetSpriteCollection(Assets.Graphics.OllyLower);
                ollyLower.Begin(BlendState.AlphaBlend);
                ollyLower.DrawSprite(0, 382, 146, colour);
                ollyLower.End();

                SpriteCollection ollyEyes = _resources.GetSpriteCollection(Assets.Graphics.OllyEyes);
                ollyEyes.Begin(BlendState.AlphaBlend);
                ollyEyes.DrawSprite(0, _ollyEyesX, _ollyEyesY, colour);
                ollyEyes.End();

                SpriteCollection ollyUpper = _resources.GetSpriteCollection(Assets.Graphics.OllyUpper);
                ollyUpper.Begin(BlendState.AlphaBlend);
                ollyUpper.DrawSprite(0, 450, 166, colour);
                ollyUpper.End();
            }
        }
    }

    private void DrawKeyDoorForceFields()
    {
        // Only where there is a door. An empty list is not null, so this used to draw the field on
        // every screen in the game - see GameScreenCollection.Update.
        if (KeyLockDoors is { Count: > 0 })
        {
            _keyDoorForceFieldNoAccess.Draw();
        }
    }

    public void DrawShadowing()
    {
        if (_graphicsSet.IsRedrawn)
        {
            SpriteCollection statusPanelShadow = _resources.GetSpriteCollection(Assets.Graphics.StatusPanelShadow);
            statusPanelShadow.Begin(BlendState.AlphaBlend);
            statusPanelShadow.DrawSprite(0, 0, 0, Color.White);
            statusPanelShadow.End();
        }
    }
}
