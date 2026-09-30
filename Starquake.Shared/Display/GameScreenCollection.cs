// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Emitters;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Structure;
using Starquake.WorldGeneration;

namespace Starquake.Display;

public class GameScreenCollection : GameComponentBase
{


    /// <summary>
    ///     A whole percentage, used for reporting how much of the map has been explored.
    /// </summary>
    private const int FullPercentage = 100;

    /// <summary>
    ///     Anything numbered above this is scenery drawn on the upper layer rather than a tile the
    ///     blob can stand on, so the collision map skips it.
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
    ///     Olly, the eye that watches the blob from the wall of one particular screen, and the screen
    ///     it lives on. Its pupil tracks the blob across the room at a fraction of the blob's speed.
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
    ///     The two emitters that make up a key door's force field, and the two quantities it is run at:
    ///     a dim alpha-blended pass for the body of the field and a bright additive one over the middle.
    /// </summary>
    private static readonly string[] ForceFieldEmitterNames = ["Emitter01", "Emitter05"];

    private readonly CoreElements _elements;

    private readonly GameScreenData[] _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    private readonly IStandardParticleEffect _keyDoorForceFieldNoAccess;
    private readonly WorldPopulator _worldPopulator;
    private readonly ResourceManager _resources;

    private readonly int _tileHeight;

    private readonly int _tileWidth;

    private List<KeyLockDoor>? _keyLockDoors;



    // olly
    private int _ollyEyesX;
    private int _ollyEyesY;

    private int _screenNumber;

    public GameScreenCollection(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        CoreElements elements,
        Random randomNumberGenerator,
        int tilesAcross,
        int tilesDown,
        int collAreaWidth,
        int collAreaHeight)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _elements = elements;

        // The colour animations are animations of this planet's own tiles, and nothing but the
        // planet and the main screen drives them - so the planet owns one and hands itself to it.
        // That is the last cycle in the graph: the animation engine needs the planet to read tiles
        // from, and the planet was reaching back through the engine to find the animation engine.
        ColourAnimations = new ColourAnimationEngine(resources, this, graphicsSet);


        TilesAcross = tilesAcross;
        TilesDown = tilesDown;

        CollAreaWidth = collAreaWidth;
        CollAreaHeight = collAreaHeight;

        // Load screen data
        _gameScreens = (GameScreenData[])_resources.GetDataResource("GameScreens");

        Collisions = new ScreenCollisionMap(
            _gameScreens,
            (int[,,])_resources.GetDataResource("ScreenCollisionData"));

        _worldPopulator = new WorldPopulator(_gameScreens, Collisions, randomNumberGenerator);

        BlobsShipSmoke1 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.Continual, null);
        BlobsShipSmoke2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("BasicSmokePlume2",
                ParticleEffectType.Continual, null);

        _keyDoorForceFieldNoAccess =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("ForceField",
                ParticleEffectType.Continual, null);

        // Make this better in the future...?
        _tileWidth = TileMetrics.TileWidth;
        _tileHeight = TileMetrics.TileHeight;
    }

    /// <summary>
    ///     What can be walked into on the screen being shown, and how far a mover may travel before
    ///     it is. Rebuilt by <see cref="UpdateCollisionMap" /> whenever the screen changes or
    ///     something on it does.
    /// </summary>
    public ScreenCollisionMap Collisions { get; }

    public int TilesAcross { get; }

    public int TilesDown { get; }

    public GameScreenData CurrentGameScreenData => _gameScreens[ScreenNumber];

    public IStandardParticleEffect BlobsShipSmoke1 { get; }

    public IStandardParticleEffect BlobsShipSmoke2 { get; }

    /// <summary>
    ///     The tiles on this planet that pulse through colours. Owned here because they are this
    ///     planet's tiles; <c>MainGameScreen</c> starts them for the teleporter and access booths.
    /// </summary>
    public ColourAnimationEngine ColourAnimations { get; }

    public int ScreenWidth => _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
        .SpriteWidth * TilesAcross;

    public int ScreenHeight => _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
        .SpriteHeight * TilesDown;

    public int PreviousScreenNumber { get; private set; }

    public int ScreenNumber
    {
        get => _screenNumber;
        set
        {
            PreviousScreenNumber = _screenNumber;
            _screenNumber = value;
            UpdateCollisionMap(true);
        }
    }

    public int CollAreaWidth { get; }

    public int CollAreaHeight { get; }

    public GameScreenData GetGameScreen(int number)
        => _gameScreens[number];

    public override void Initialise(object? data)
    {
        // Setup Collision map, we're using 8 x 8 blocks for each collision area for improved accuracy
        UpdateCollisionMap(true);
    }

    public int GetPercentageOfScreensVisited()
    {
        int screensVisited = 0;

        foreach (GameScreenData gameScreen in _gameScreens)
        {
            if (gameScreen.ScreenVisited)
            {
                screensVisited++;
            }
        }

        return (int)Math.Ceiling(screensVisited / (double)_gameScreens.Length * FullPercentage);
    }
    // Scan the screen for collision tiles
    public List<Point> GetNastyPotentialLocations()
    {
        List<Point> potentialLocations = [];

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == -1)
                {
                    bool addLocation = true;

                    // Now check the collision map for this tile
                    for (int y1 = 0; y1 < TileMetrics.CollisionCellsDownTile; y1++)
                    {
                        for (int x1 = 0; x1 < TileMetrics.CollisionCellsAcrossTile; x1++)
                        {
                            foreach (CollisionTile collisionTile in _gameScreens[ScreenNumber].CollisionMap)
                            {
                                if (collisionTile.XPos ==
                                    x * _tileWidth + x1 * CollAreaWidth &&
                                    collisionTile.YPos ==
                                    y * _tileHeight + y1 * CollAreaHeight)
                                {
                                    if (collisionTile.CollisionTileType ==
                                        CollisionTileType.NotBlob)
                                    {
                                        addLocation = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (addLocation)
                    {
                        potentialLocations.Add(ConvertToScreenCoords(x, y));
                    }
                }
            }
        }

        return potentialLocations;
    }

    public void SaveData()
    {
        byte[] gameScreensData = MessagePackSerializer.Serialize(_gameScreens);
        byte[] collisionData = MessagePackSerializer.Serialize(Collisions.TileCollisionData);

        using (var fs = new FileStream("SQScreenData.dat", FileMode.Create, FileAccess.Write))
        {
            // Write the length of the first array
            fs.Write(BitConverter.GetBytes(gameScreensData.Length), 0, sizeof(int));

            // Write the first array
            fs.Write(gameScreensData, 0, gameScreensData.Length);

            // Write the length of the second array
            fs.Write(BitConverter.GetBytes(collisionData.Length), 0, sizeof(int));

            // Write the second array
            fs.Write(collisionData, 0, collisionData.Length);
        }

        //var newGameScreensData = MessagePackSerializer.Deserialize<GameScreen[]>(gameScreensData);
        //var newCollisionData = MessagePackSerializer.Deserialize<int[,,]>(collisionData);
    }

    /// <summary>
    ///     Rebuilds the collision map for the screen being shown.
    /// </summary>
    /// <param name="screenChanged">
    ///     Whether this is a new screen rather than a change on the one already up. All it controls
    ///     here is the key-door cache: the doors are found once per screen and held.
    /// </param>
    public void UpdateCollisionMap(bool screenChanged)
    {
        if (screenChanged)
        {
            _keyLockDoors = null;
        }

        Collisions.Rebuild(ScreenNumber);
    }

    public void ChangeShuttlePlatformState(bool padRemoved)
    {
        foreach (GameScreenData gameScreen in _gameScreens)
        {
            for (int y = 0; y < TilesDown; y++)
            {
                for (int x = 0; x < TilesAcross; x++)
                {
                    if (padRemoved)
                    {
                        if (gameScreen.ScreenTiles[x, y].TileImage == (int)ActionTiles.ShuttlePlatform)
                        {
                            gameScreen.ScreenTiles[x, y].TileImage = (int)ActionTiles.RemovedShuttlePlatform;

                            // Trigger Colour animation
                            ColourAnimations.Start((int)ActionTiles.ShuttleTag1, -1);
                            ColourAnimations.Start((int)ActionTiles.ShuttleTag3, -1);
                            ColourAnimations.Start((int)ActionTiles.ShuttleTag2, -1);
                        }
                    }
                    else
                    {
                        if (gameScreen.ScreenTiles[x, y].TileImage == (int)ActionTiles.RemovedShuttlePlatform)
                        {
                            gameScreen.ScreenTiles[x, y].TileImage = (int)ActionTiles.ShuttlePlatform;

                            // Cancel Colour animation
                            ColourAnimations.Stop((int)ActionTiles.ShuttleTag1);
                            ColourAnimations.Stop((int)ActionTiles.ShuttleTag3);
                            ColourAnimations.Stop((int)ActionTiles.ShuttleTag2);
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc cref="ScreenCollisionMap.CheckRectangles" />
    public int CheckRectangles(
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        bool isBlob)
        => Collisions.CheckRectangles(playerXPos, playerYPos, playerWidth, playerHeight, isBlob);

    /// <inheritdoc cref="ScreenCollisionMap.CheckCollision" />
    public int CheckCollision(
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
        => Collisions.CheckCollision(
            playerDirection, playerXPos, playerYPos, playerWidth, playerHeight, velocity, isBlob);

    public Point ConvertToScreenCoords(int tileMapXPos, int tileMapYPos)
        => new(
            tileMapXPos * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .SpriteWidth,
            tileMapYPos * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .SpriteHeight);

    public List<Point> GetCoordsOfTiles(int tileType)
    {
        var returnedTiles = new List<Point>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage == tileType)
                {
                    returnedTiles.Add(new Point(
                        x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteWidth,
                        y * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteHeight));
                }
            }
        }

        return returnedTiles;
    }

    public Point[] GetCoordsOfTiles(int[] tileTypes)
    {
        var returnedTiles = new List<Point>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (tileTypes.Contains(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage))
                {
                    returnedTiles.Add(new Point(
                        x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteWidth,
                        y * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteHeight));
                }
            }
        }

        return returnedTiles.OrderByDescending(rt => rt.Y).ToArray();
    }

    private List<KeyLockDoor> GetKeyLockDoors()
    {
        var returnedTiles = new List<KeyLockDoor>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.AccessForceField)
                {
                    returnedTiles.Add(new KeyLockDoor
                    {
                        Location = new Point(
                            x * _resources
                                .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth,
                            y * _resources
                                .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteHeight)
                    });
                }
            }
        }

        return returnedTiles;
    }

    /// <inheritdoc cref="ScreenCollisionMap.CheckTileByObject" />
    public TileDetected? CheckTileByObject(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
        => Collisions.CheckTileByObject(
            directionToCheck, playerXPos, playerYPos, playerWidth, playerHeight, velocity);

    /// <summary>
    ///     Stepped with where the blob is and whether it is carrying a key, rather than holding
    ///     the blob and asking. Two things on the planet follow the blob - the force field on a
    ///     key-locked door, which turns green when the blob can open it, and Olly's eyes, which
    ///     drift after it - and neither needs the blob itself, only three values it can be told.
    ///     <para>
    ///         Not an override of <see cref="GameComponentBase.Update" /> any more, which it can
    ///         afford not to be: the planet is only ever stepped by name from
    ///         <c>MainGameScreen</c>, never polymorphically with the components in the screen
    ///         directory. Being told instead of asking is what lets the planet be built before the
    ///         blob rather than after it, which is the whole of the cycle between them.
    ///     </para>
    /// </summary>
    public void Update(GameTime gameTime, int blobXPos, int blobYPos, bool blobHasKey)
    {
        _keyLockDoors ??= GetKeyLockDoors();

        if (_keyLockDoors != null && _keyDoorForceFieldNoAccess != null)
        {
            _keyDoorForceFieldNoAccess.Start();

            foreach (KeyLockDoor keyLockDoor in _keyLockDoors)
            {
                UpdateKeyLockDoor(keyLockDoor, gameTime, blobHasKey);
            }
        }

        if (ScreenNumber == OllyScreenNumber)
        {
            TrackBlobWithOllysEyes(blobXPos, blobYPos);
        }

        _resources.GetAnimationEngine().Update(gameTime);
        ColourAnimations.Update(gameTime);
    }

    /// <summary>
    ///     One key-locked door: its force field, and whether it lets blob through. Green and passable
    ///     to him if he is carrying the key, red and solid if not - and solid to nasties either way,
    ///     which is why the tiles flip between <see cref="CollisionTileType.NotBlob" /> and
    ///     <see cref="CollisionTileType.EveryoneHits" /> rather than being removed.
    /// </summary>
    private void UpdateKeyLockDoor(KeyLockDoor keyLockDoor, GameTime gameTime, bool blobHasKey)
    {
        if (keyLockDoor.CollisionTiles.Count == 0)
        {
            AddKeyLockDoorCollisionTiles(keyLockDoor);
        }

        // The field is drawn twice: a dim grey pass for the body of it, then a bright additive pass
        // over the middle, which is what gives it its core.
        SetForceFieldEmitters(new Color(100, 100, 100), DimFieldQuantity, EmitterBlendMode.Alpha);

        for (int c = 0; c < 4; c++)
        {
            _keyDoorForceFieldNoAccess!.Update(keyLockDoor.Location.X + 52 + c, keyLockDoor.Location.Y + 48, gameTime);
        }

        for (int c = 0; c < 4; c++)
        {
            _keyDoorForceFieldNoAccess!.Update(keyLockDoor.Location.X + 76 + c, keyLockDoor.Location.Y + 48, gameTime);
        }

        SetForceFieldColour(blobHasKey ? new Color(0, ColourLevels.Full, 0) : new Color(ColourLevels.Full, 0, 0));

        foreach (CollisionTile collisionTile in keyLockDoor.CollisionTiles)
        {
            collisionTile.CollisionTileType = blobHasKey
                ? CollisionTileType.NotBlob
                : CollisionTileType.EveryoneHits;
        }

        SetForceFieldEmitters(null, BrightFieldQuantity, EmitterBlendMode.Add);

        for (int c = 0; c < 12; c++)
        {
            _keyDoorForceFieldNoAccess!.Update(keyLockDoor.Location.X + 60 + c, keyLockDoor.Location.Y + 48, gameTime);
        }
    }

    private void AddKeyLockDoorCollisionTiles(KeyLockDoor keyLockDoor)
    {
        for (int c = 0; c < 3; c++)
        {
            var keyLockCollisionTile =
                new CollisionTile(
                    (int)CollisionObjectType.KeyAccessDoor, // Random image that's meaningless
                    keyLockDoor.Location.X + 48,
                    keyLockDoor.Location.Y + c * 32,
                    CollisionTileType.None);

            // Add the tile to the keylock door so we can remove it later
            keyLockDoor.CollisionTiles.Add(keyLockCollisionTile);

            // Add the tile to the collision map
            _gameScreens[ScreenNumber].CollisionMap.Add(keyLockCollisionTile);
        }
    }

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
    ///     Olly's eyes follow the blob about, damped so they drift rather than snap.
    /// </summary>
    private void TrackBlobWithOllysEyes(int blobXPos, int blobYPos)
    {
        _ollyEyesX = OllyEyesRestX + (blobXPos - OllyGazeCentreX) / OllyGazeDamping;
        _ollyEyesY = OllyEyesRestY + blobYPos / OllyVerticalGazeDamping;
    }

    public void DrawCoreElements(int xPos, int yPos)
    {
        int objNo = 0;

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
                    .DrawSprite(objNo, x * 64 + xPos, y * 64 + yPos, Color.White);
                objNo++;
            }
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();
    }

    public void Draw(Color colour, GameTime gameTime, bool introSequenceRunning = false,
        IntroSequenceStage introSequenceStage = IntroSequenceStage.None)
    {
        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                DrawLowerTile(x, y, colour);
            }
        }

        ColourAnimations.Draw(gameTime, colour.A);

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

    private void DrawLowerTile(int x, int y, Color colour)
    {
        int tileImage = _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage;

        if (tileImage <= -1 || tileImage is
                (int)ActionTiles.BlankFiller or // The blank spacer tile
                (int)ActionTiles.ConveyerNasty or // Tile representing conveyer nasties
                (int)ActionTiles.TunnelLiftTile or
                (int)ActionTiles.PreferredCoreLocation) // Tile representing preferred pickup location
        {
            return;
        }

        // Avoid the tunnel tiles
        if (tileImage < (int)ActionTiles.AccessForceField)
        {
            SpriteCollection tiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

            tiles.Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
            tiles.DrawSprite(tileImage, x * _tileWidth, y * _tileHeight, colour);
            tiles.End();
        }

        // Draw animation portions
        _resources.GetAnimationEngine()
            .Draw(tileImage, x * _tileWidth, y * _tileHeight, colour);
    }

    private void DrawPowerUp(Color colour)
    {
        if (CurrentGameScreenData.PowerUp == null)
        {
            return;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps).DrawSprite(
            CurrentGameScreenData.PowerUp.Type, CurrentGameScreenData.PowerUp.XLocation,
            CurrentGameScreenData.PowerUp.YLocation, colour);
        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps).End();
    }

    private void DrawPickUps(Color colour)
    {
        if (CurrentGameScreenData.PickUps == null)
        {
            return;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
        foreach (InteractiveItem pickUp in CurrentGameScreenData.PickUps)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite(
                pickUp.Type, pickUp.XLocation,
                pickUp.YLocation, colour);
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();
    }

    private void DrawCheopsPyramid(Color colour)
    {
        if (CurrentGameScreenData.CheopsPyramid == null)
        {
            return;
        }

        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection("CheopsPyramidOriginal")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0,
                CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                colour);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").End();
        }
        else
        {
            _resources.GetSpriteCollection("CheopsPyramid")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront);
            _resources.GetSpriteCollection("CheopsPyramid").DrawSprite(0,
                CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                colour);
            _resources.GetSpriteCollection("CheopsPyramid").End();
        }
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

        _resources.GetSpriteCollection("OllyBackground")
            .Begin(BlendState.AlphaBlend);
        _resources.GetSpriteCollection("OllyBackground")
            .DrawSprite(0, 382, 146, colour);
        _resources.GetSpriteCollection("OllyBackground").End();
    }

    public void DrawUpperLayer(Color colour)
    {
        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                DrawUpperTilePatch(x, y, colour);
                DrawTunnelEntrance(x, y, colour);
            }
        }

        DrawOlly(colour);
        DrawKeyDoorForceFields();

        ColourAnimations.DrawUpperLayer();
    }

    /// <summary>
    ///     Lays the upper-layer patch over one tile - the inside of a lift shaft, a booth, a tunnel mouth,
    ///     the shadow another tile casts. A tile with no patch is left as the lower layer drew it.
    /// </summary>
    private void DrawUpperTilePatch(int x, int y, Color colour)
    {
        if (UpperLayerPatchFor(CurrentGameScreenData.ScreenTiles[x, y].TileImage) is not var (sprite, rowOffset))
        {
            return;
        }

        SpriteCollection upperTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);
        SpriteCollection lowerTiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        upperTiles.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront);
        upperTiles.DrawSprite(
            sprite,
            x * lowerTiles.SpriteWidth,
            (y + rowOffset) * lowerTiles.SpriteHeight,
            colour);
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

        if (tileImage is
            TunnelTiles.LeftMouth1 or
            TunnelTiles.LeftMouth2 or
            TunnelTiles.LeftMouth3 or
            TunnelTiles.LeftMouth4 or
            TunnelTiles.LeftMouth5 or
            TunnelTiles.LeftMouth6 or
            TunnelTiles.LeftMouth7 or
            TunnelTiles.LeftMouth8 or
            TunnelTiles.LeftMouth9 or
            TunnelTiles.LeftMouth10)
        {
            return (LeftTunnelMouthSprite, 0);
        }

        if (tileImage is
            TunnelTiles.RightMouth1 or
            TunnelTiles.RightMouth2 or
            TunnelTiles.RightMouth3 or
            TunnelTiles.RightMouth4 or
            TunnelTiles.RightMouth5 or
            TunnelTiles.RightMouth6 or
            TunnelTiles.RightMouth7 or
            TunnelTiles.RightMouth8 or
            TunnelTiles.RightMouth9 or
            TunnelTiles.RightMouth10)
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

    private void DrawTunnelEntrance(int x, int y, Color colour)
    {
        if (CurrentGameScreenData.ScreenTiles[x, y].TileImage > LastCollidableTile)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate);
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .DrawSprite(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, x * _tileWidth,
                    y * _tileHeight, colour);
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();
        }
    }

    private void DrawOlly(Color colour)
    {
        if (ScreenNumber == OllyScreenNumber)
        {
            if (_graphicsSet.IsOriginal)
            {
                _resources.GetSpriteCollection("OllyOriginal")
                    .Begin(BlendState.AlphaBlend);
                _resources.GetSpriteCollection("OllyOriginal")
                    .DrawSprite(0, 384, 100, colour);
                _resources.GetSpriteCollection("OllyOriginal").End();
            }
            else
            {
                _resources.GetSpriteCollection("OllyLower")
                    .Begin(BlendState.AlphaBlend);
                _resources.GetSpriteCollection("OllyLower").DrawSprite(0, 382, 146, colour);
                _resources.GetSpriteCollection("OllyLower").End();

                _resources.GetSpriteCollection("OllyEyes")
                    .Begin(BlendState.AlphaBlend);
                _resources.GetSpriteCollection("OllyEyes")
                    .DrawSprite(0, _ollyEyesX, _ollyEyesY, colour);
                _resources.GetSpriteCollection("OllyEyes").End();

                _resources.GetSpriteCollection("OllyUpper")
                    .Begin(BlendState.AlphaBlend);
                _resources.GetSpriteCollection("OllyUpper").DrawSprite(0, 450, 166, colour);
                _resources.GetSpriteCollection("OllyUpper").End();
            }
        }
    }

    private void DrawKeyDoorForceFields()
    {
        if (_keyLockDoors != null)
        {
            if (_keyDoorForceFieldNoAccess != null)
            {
                _keyDoorForceFieldNoAccess.Draw();
            }
        }
    }

    public void DrawShadowing()
    {
        if (_graphicsSet.IsRedrawn)
        {
            // Status panel shadow
            _resources.GetSpriteCollection("StatusPanelShadow")
                .Begin(BlendState.AlphaBlend);
            _resources.GetSpriteCollection("StatusPanelShadow")
                .DrawSprite(0, 0, 0, Color.White);
            _resources.GetSpriteCollection("StatusPanelShadow").End();
        }
    }

    public List<SecondLayerTile> GetListOfSecondLayerTiles()
    {
        var tilesToReturn = new List<SecondLayerTile>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                int tileType = _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage;
                var tiletypeFound = (TileType)tileType;

                if (Enum.IsDefined(typeof(TileType), tiletypeFound))
                {
                    tilesToReturn.Add(new SecondLayerTile(tiletypeFound, new Point(x * TileMetrics.TileWidth, y * TileMetrics.TileHeight)));
                }
            }
        }

        return tilesToReturn;
    }

    /// <summary>
    ///     Starts a new game on this planet: the blob back at the wreck, and the whole map scattered
    ///     with things to find.
    /// </summary>
    public void Reset()
    {
        ScreenNumber = WorldMap.ShipWreckScreenNumber;

        // The elements the core is asking for are read off here rather than in the populator,
        // which would drag the graphics in behind them.
        var coreElementsWanted = new List<int>();
        foreach (CoreElement element in _elements.GetActiveElements())
        {
            coreElementsWanted.Add(element.CurrentElement);
        }

        _worldPopulator.Populate(coreElementsWanted);
    }
}
