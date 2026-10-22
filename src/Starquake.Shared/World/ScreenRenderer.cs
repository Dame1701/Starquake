// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Emitters;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Fonts;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Structure;
using Starquake.Tuning;

namespace Starquake.World;

/// <summary>
///     Draws one screen of the planet: its two layers of tiles, whatever is lying on it, the force
///     field across a key-locked door, and Olly.
/// </summary>
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
    ///     The screen Olly sits on. Public because the water he sits in is a screen effect, and
    ///     whatever starts and stops that has to know which screen it is.
    /// </summary>
    public const int OllyScreenNumber = 477;

    /// <summary>
    ///     Where Olly's eyes rest, and how much of Blob's movement they follow. The damping figures
    ///     are divisors, so a bigger one means less movement.
    /// </summary>
    private const int OllyEyesRestX = 486;

    /// <inheritdoc cref="OllyEyesRestX" />
    private const int OllyEyesRestY = 206;

    /// <inheritdoc cref="OllyEyesRestX" />
    private const int OllyGazeCentreX = 350;

    /// <inheritdoc cref="OllyEyesRestX" />
    private const int OllyGazeDamping = 44;

    /// <inheritdoc cref="OllyEyesRestX" />
    private const int OllyVerticalGazeDamping = 20;

    /// <inheritdoc cref="ForceFieldEmitterNames" />
    private const int DimFieldQuantity = 100;

    /// <inheritdoc cref="ForceFieldEmitterNames" />
    private const int BrightFieldQuantity = 10;

    /// <summary>
    ///     What those two quantities fall to once the door has opened. The sheet of light draws back
    ///     onto the jambs as it opens, so crackle left burning down the middle would contradict it.
    /// </summary>
    private const int DimFieldOpenQuantity = 45;

    /// <inheritdoc cref="DimFieldOpenQuantity" />
    private const int BrightFieldOpenQuantity = 2;

    /// <summary>
    ///     Where the particles are emitted within the doorway: how far in from each jamb the two
    ///     columns of the dim pass sit, how wide each is, and how wide the bright core down the
    ///     middle is. Measured in the gap's own pixels, 32 of them across.
    /// </summary>
    private const int FieldColumnInset = 4;

    /// <inheritdoc cref="FieldColumnInset" />
    private const int FieldColumnWidth = 4;

    /// <inheritdoc cref="FieldColumnInset" />
    private const int FieldCoreWidth = 12;

    /// <summary>
    ///     How solid that ink is. Short of opaque, so the board shows through the letters the way it
    ///     does in the artwork. Fully opaque ink reads as a label stuck on the sign.
    /// </summary>
    private const int SignpostInkLevel = 190;

    /// <summary>
    ///     How far the code is offset when it is written a second time, to thicken it. The font is a
    ///     light one at the size the board leaves, so one pass comes out finer than the lettering it
    ///     stands in for.
    /// </summary>
    private const int SignpostStrokeWidening = 1;

    /// <summary>
    ///     The two emitters that make up a key door's force field, and the two quantities it is run at:
    ///     a dim alpha-blended pass for the body of the field and a bright additive one over the middle.
    /// </summary>
    private static readonly string[] ForceFieldEmitterNames = ["Emitter01", "Emitter05"];

    /// <summary>
    ///     The grey the dim pass down each jamb is released in. Dark enough to read as the body of
    ///     the field rather than as light of its own, which is the bright pass's job.
    /// </summary>
    private static readonly Vector3 DimFieldColour = new(0.4f, 0.4f, 0.4f);

    /// <summary>
    ///     The colour a signpost's code is written in. The board the artwork draws is gold, and black
    ///     on it is what reads from across a room.
    /// </summary>
    private static readonly Color SignpostInk = Color.Black;


    private readonly ColourAnimations _colourAnimations;
    private readonly GraphicsSet _graphicsSet;
    private readonly IStandardParticleEffect _keyDoorForceField;
    private readonly ResourceManager _resources;
    private readonly GameScreenData[] _screens;
    private readonly Shapes _shapes;
    private readonly BitmapFont _signpostFont;

    private int _ollyEyesX;
    private int _ollyEyesY;

    public ScreenRenderer(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        GameScreenData[] screens,
        ColourAnimations colourAnimations,
        IStandardParticleEffect keyDoorForceField,
        Shapes shapes,
        BitmapFont signpostFont)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _screens = screens;
        _colourAnimations = colourAnimations;
        _keyDoorForceField = keyDoorForceField;
        _shapes = shapes;
        _signpostFont = signpostFont;
    }

    /// <summary>
    ///     Which screen is being drawn. Set by the collection that owns the screens, at the same
    ///     moment it tells the collision map.
    /// </summary>
    public int ScreenNumber { get; set; }

    /// <summary>
    ///     The doors on this screen whose force field is drawn. The collection owns the key-door
    ///     state; this only paints it.
    /// </summary>
    public List<KeyLockDoor>? KeyLockDoors { get; set; }

    /// <summary>
    ///     Runs one frame of the crackle over a key-locked door. Emitted twice: a dim grey pass down
    ///     each jamb for the body of it, then a bright pass over the middle, which is what gives it
    ///     its core. Both thin out as the door opens, and the core's colour crossfades from red to
    ///     green with it rather than switching the moment Blob picks the key up.
    /// </summary>
    /// <remarks>
    ///     The sheet of light the crackle sits on is drawn by
    ///     <see cref="DrawKeyDoorForceFields" />, not from here. Both measure the gap off
    ///     <see cref="KeyLockDoor.Doorway" />, which is the only place its shape is written down.
    /// </remarks>
    public void AnimateForceField(KeyLockDoor keyLockDoor, GameTime gameTime)
    {
        Rectangle doorway = keyLockDoor.Doorway;
        float openness = keyLockDoor.Openness;

        SetForceFieldEmitters(
            DimFieldColour, QuantityAt(DimFieldQuantity, DimFieldOpenQuantity, openness),
            EmitterBlendMode.Alpha);

        EmitFieldColumn(doorway, doorway.Left + FieldColumnInset, FieldColumnWidth, gameTime);
        EmitFieldColumn(
            doorway, doorway.Right - FieldColumnInset - FieldColumnWidth, FieldColumnWidth, gameTime);

        SetForceFieldColour(ForceFieldSettings.ColourAt(openness));

        SetForceFieldEmitters(
            null, QuantityAt(BrightFieldQuantity, BrightFieldOpenQuantity, openness), EmitterBlendMode.Add);

        EmitFieldColumn(
            doorway, doorway.Center.X - FieldCoreWidth / 2, FieldCoreWidth, gameTime);
    }

    /// <summary>
    ///     One column of the crackle: the emitters triggered once on each pixel across it, down the
    ///     middle of the gap's height. The emitters are lines as tall as the doorway, so one trigger
    ///     is a column and this is how wide a band of them is laid down.
    /// </summary>
    private void EmitFieldColumn(Rectangle doorway, int left, int width, GameTime gameTime)
    {
        for (int c = 0; c < width; c++)
        {
            _keyDoorForceField.Update(left + c, doorway.Center.Y, gameTime);
        }
    }

    /// <summary>
    ///     How many particles a pass releases at a given openness, between what it releases across a
    ///     shut door and what it is left releasing once the door has let go.
    /// </summary>
    private static int QuantityAt(int shut, int open, float openness)
        => (int)MathHelper.Lerp(shut, open, openness);

    /// <summary>
    ///     Starts the force field emitting, once per screen that has a key-locked door on it.
    /// </summary>
    public void StartForceField()
        => _keyDoorForceField.Start();

    /// <summary>
    ///     Stops the force field and kills the particles it has in the air, for a screen with no door
    ///     on it to emit them.
    /// </summary>
    public void ClearForceField()
        => _keyDoorForceField.Reset();

    /// <summary>
    ///     The force field is two emitters that are always set together, so this sets both.
    ///     <paramref name="colour" /> is null to leave the colour as it is.
    /// </summary>
    private void SetForceFieldEmitters(Vector3? colour, int quantity, EmitterBlendMode blendMode)
    {
        foreach (string name in ForceFieldEmitterNames)
        {
            Emitter? emitter = _keyDoorForceField!.Effect[name];

            if (colour is { } releaseColour)
            {
                emitter.ReleaseColour = releaseColour;
            }

            emitter.ReleaseQuantity = quantity;
            emitter.BlendMode = blendMode;
        }
    }

    /// <inheritdoc cref="SetForceFieldEmitters" />
    private void SetForceFieldColour(Vector3 colour)
    {
        foreach (string name in ForceFieldEmitterNames)
        {
            _keyDoorForceField!.Effect[name].ReleaseColour = colour;
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
    ///     The sort mode has to stay <c>Deferred</c>, which draws in the order the calls were made.
    ///     <c>Immediate</c> flushes a draw call per sprite and undoes the batching, and
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
    ///     the whole lower layer rather than tile by tile with it. That gives the same picture,
    ///     because a patch is never bigger than its tile and so cannot be covered by a later one.
    /// </summary>
    private void DrawTileAnimations(Color colour)
    {
        AnimTiles animations = _resources.GetAnimTiles();

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
    ///     The scale is measured rather than fixed: as tall as the board's lettering, unless the code
    ///     would then run off the end, in which case the width decides. Only the font knows how wide
    ///     a code comes out, so it has to be asked.
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

        // The screen's own fade carries the ink's, so a sign dims with the room it stands in.
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
        DrawKeyDoorForceFields(colour);

        _colourAnimations.DrawUpperLayer();
    }

    /// <summary>
    ///     Lays the upper-layer patch over one tile - the inside of a lift shaft, a booth, a tunnel mouth,
    ///     the shadow another tile casts. A tile with no patch is left as the lower layer drew it.
    /// </summary>
    /// <remarks>
    ///     One batch for the whole layer, as with the lower one. The patches and the tunnel entrances
    ///     come off different sheets, so they are two passes rather than one interleaved loop. That
    ///     gives the same picture, because a patch is drawn on its own row or the one below it and
    ///     never above, so an entrance sharing a cell with one is still drawn after it.
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
    ///     Which sprite of the upper tile set covers a given lower tile, and how many rows below that
    ///     tile it is drawn. Null for a tile with no patch.
    /// </summary>
    /// <remarks>
    ///     The row offset exists for one case. A shadow belongs to the tile above it, so it is
    ///     recorded against that tile and drawn a row further down. Everything else draws in place.
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
    ///     on and so are drawn over the upper layer rather than under it. Being past
    ///     <see cref="LastCollidableTile" /> is not enough on its own, because markers are numbered
    ///     above the mouths again and are not drawn at all.
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

    /// <summary>
    ///     The force field across each key-locked door on the screen: the sheet of light first, then
    ///     the crackle over the top of it.
    /// </summary>
    /// <remarks>
    ///     One batch per door, because the shader is told what state that door is in and a batch
    ///     holds one set of values. No screen has more than a handful of doors, so the cost is small.
    /// </remarks>
    private void DrawKeyDoorForceFields(Color colour)
    {
        // An empty list is not null, so a null check alone would draw the field on every screen.
        if (KeyLockDoors is not { Count: > 0 })
        {
            return;
        }

        Effect field = _resources.GetEffect(Assets.Shaders.ForceField);

        field.Parameters["Frequency"].SetValue(ForceFieldSettings.Frequency);
        field.Parameters["OverScanWidth"].SetValue(ForceFieldSettings.OverScanWidth);
        field.Parameters["PostGlow"].SetValue(ForceFieldSettings.PostGlow);

        foreach (KeyLockDoor keyLockDoor in KeyLockDoors)
        {
            float openness = keyLockDoor.Openness;

            field.Parameters["Colour"].SetValue(ForceFieldSettings.ColourAt(openness));
            field.Parameters["DrawBack"].SetValue(ForceFieldSettings.DrawBackAt(openness));
            field.Parameters["Flash"].SetValue(keyLockDoor.Flash);
            field.Parameters["Phase"].SetValue(ForceFieldSettings.PhaseAt(keyLockDoor.Frame));
            field.Parameters["ScanAt"].SetValue(ForceFieldSettings.ScanAt(keyLockDoor.Frame));

            field.Parameters["Intensity"].SetValue(ForceFieldSettings.IntensityAt(
                openness, keyLockDoor.Brace, keyLockDoor.Frame));

            // Over a plain filled rectangle the size of the gap, because every pixel of the sheet
            // is worked out rather than sampled and the door's tile has nothing painted on it to
            // sample anyway - see Shaders/ForceField.fx.
            _shapes.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, field);
            _shapes.FillRectangle(keyLockDoor.Doorway, colour);
            _shapes.End();
        }

        _keyDoorForceField.Draw();
    }
}
