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

namespace Starquake.Display;

public class GameScreenCollection : GameComponentBase
{


    /// <summary>
    ///     How many Cheops pyramids and core elements are scattered across the map at the start of a
    ///     game.
    /// </summary>
    private const int CMaxCheops = 40;

    /// <inheritdoc cref="CMaxCheops" />
    private const int CMaxCoreElements = 18;

    /// <summary>
    ///     A whole percentage, used for reporting how much of the map has been explored.
    /// </summary>
    private const int FullPercentage = 100;

    /// <summary>
    ///     Where a dropped or placed item sits inside its tile - one collision cell in from the left,
    ///     and either one cell down or a sprite's height up depending on whether it is being stood on
    ///     or hung from.
    /// </summary>
    private const int ItemTileXOffset = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="ItemTileXOffset" />
    private const int ItemTileYOffset = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     The two tiles the map uses as plain scenery either side of a doorway. They are recognised
    ///     by number only so that an item is never placed on top of one.
    /// </summary>
    private const int SceneryTile1 = 120;

    /// <inheritdoc cref="SceneryTile1" />
    private const int SceneryTile2 = 121;

    /// <summary>
    ///     Where an item is drawn relative to the tile it stands on: one collision cell in from the
    ///     tile's left edge, and two cells above its top.
    /// </summary>
    private const int PlacementXInset = 32;

    /// <inheritdoc cref="PlacementXInset" />
    private const int PlacementYLift = 64;

    /// <summary>
    ///     Anything numbered above this is scenery drawn on the upper layer rather than a tile the
    ///     blob can stand on, so the collision map skips it.
    /// </summary>
    private const int LastCollidableTile = (int)ActionTiles.ShuttleTag2;

    /// <summary>
    ///     Collision tiles carrying an image number beyond this are game objects rather than scenery -
    ///     a nasty, a pickup, a core element - and are tested against a sprite-sized box.
    /// </summary>
    private const int FirstGameObjectImage = 999;

    /// <summary>
    ///     The box a game object is tested against when checking whether a nasty has ended up embedded
    ///     in one. Half a sprite, not a whole one, and not a collision cell either - it is its own
    ///     number and was written inline as a bare 32.
    /// </summary>
    private const int GameObjectProbeSize = 32;

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
    ///     The range the two kinds of access code are drawn from: a two digit code for a Cheops
    ///     pyramid, a three digit one for an access booth. The upper bound is exclusive, as
    ///     Random.Next takes it.
    /// </summary>
    private const int CheopsCodeLowest = 10;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int CheopsCodeExclusiveUpperBound = 100;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int AccessCodeLowest = 100;

    /// <inheritdoc cref="CheopsCodeLowest" />
    private const int AccessCodeExclusiveUpperBound = 1000;

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
    ///     The tiles no pick-up, power-up or pyramid may be placed on, because each of them already
    ///     means something to the game - a shuttle, a spike, a bridge, a marker. This was a fifteen-term
    ///     chain of inequalities.
    /// </summary>
    private static readonly HashSet<int> TilesNothingMayBePlacedOn =
    [
        (int)ActionTiles.ShuttlePlatform,
        (int)ActionTiles.RemovedShuttlePlatform,
        (int)ActionTiles.ElectricityNode,
        (int)ActionTiles.ShuttleTag1,
        (int)ActionTiles.ShuttleTag2,
        (int)ActionTiles.ShuttleTag3,
        (int)ActionTiles.Spike1,
        (int)ActionTiles.Spike2,
        (int)ActionTiles.Spike3,
        (int)ActionTiles.ElectricityGate,
        (int)ActionTiles.BlankFiller,
        (int)ActionTiles.ConveyerNasty,
        (int)ActionTiles.BreakableBridges,
        SceneryTile1,
        SceneryTile2,
        (int)ActionTiles.PreferredCoreLocation
    ];

    /// <summary>
    ///     The two emitters that make up a key door's force field, and the two quantities it is run at:
    ///     a dim alpha-blended pass for the body of the field and a bright additive one over the middle.
    /// </summary>
    private static readonly string[] ForceFieldEmitterNames = ["Emitter01", "Emitter05"];

    private readonly List<CollisionTile> _detectedCollisionMap;
    private readonly CoreElements _elements;

    private readonly GameScreenData[] _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    private readonly IStandardParticleEffect _keyDoorForceFieldNoAccess;
    private readonly int _pickUpCount = 100;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private readonly int[,,] _screenCollisionDataArray;
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
        int xOffset,
        int yOffset,
        int collAreaWidth,
        int collAreaHeight)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;
        _elements = elements;
        _randomNumberGenerator = randomNumberGenerator;

        // The colour animations are animations of this planet's own tiles, and nothing but the
        // planet and the main screen drives them - so the planet owns one and hands itself to it.
        // That is the last cycle in the graph: the animation engine needs the planet to read tiles
        // from, and the planet was reaching back through the engine to find the animation engine.
        ColourAnimations = new ColourAnimationEngine(resources, this, graphicsSet);


        TilesAcross = tilesAcross;
        TilesDown = tilesDown;

        CollAreaWidth = collAreaWidth;
        CollAreaHeight = collAreaHeight;

        _detectedCollisionMap = new List<CollisionTile>();

        // Load screen data
        _gameScreens = (GameScreenData[])_resources.GetDataResource("GameScreens");
        _screenCollisionDataArray =
            (int[,,])_resources.GetDataResource("ScreenCollisionData");

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
            BuildScreenCollisionMap(true);
        }
    }

    public int CollAreaWidth { get; }

    public int CollAreaHeight { get; }

    public GameScreenData GetGameScreen(int number)
        => _gameScreens[number];

    public override void Initialise(object? data)
    {
        // Setup Collision map, we're using 8 x 8 blocks for each collision area for improved accuracy
        BuildScreenCollisionMap(true);
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


    private bool CheckAccessCodeValidity(string accessCode)
    {
        bool unique = true;

        for (int a = 0; a < accessCode.Length; a++)
        {
            for (int b = 0; b < accessCode.Length; b++)
            {
                if (accessCode.Substring(b, 1) == accessCode.Substring(a, 1) &&
                    a != b)
                {
                    unique = false;
                    break;
                }
            }

            if (!unique)
            {
                break;
            }
        }

        return unique;
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

    private InteractiveItem? GetPreferredLocation(int screenNumber)
    {
        for (int y = 1; y < _gameScreens[screenNumber].ScreenTiles.GetLength(1); y++)
        {
            for (int x = 0; x < _gameScreens[screenNumber].ScreenTiles.GetLength(0); x++)
            {
                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.PreferredCoreLocation)
                {
                    return new InteractiveItem(
                        x * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteWidth + 32,
                        y * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                            .SpriteHeight + 32, 0);
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Every tile on a screen that an item could sit on top of: solid enough to stand on, with a
    ///     clear space above it, and not one of the tiles that already mean something.
    /// </summary>
    private List<InteractiveItem> GetPotentialLocations(int screenNumber)
    {
        var toReturn = new List<InteractiveItem>();
        SpriteCollection tiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        // Start at row 1: nothing above that row is reachable
        for (int y = 1; y < _gameScreens[screenNumber].ScreenTiles.GetLength(1); y++)
        {
            for (int x = 0; x < _gameScreens[screenNumber].ScreenTiles.GetLength(0); x++)
            {
                int tileImage = _gameScreens[screenNumber].ScreenTiles[x, y].TileImage;

                if (tileImage < 0 || TilesNothingMayBePlacedOn.Contains(tileImage))
                {
                    continue;
                }

                // Check for powerups in the way - useful for pickup sweep
                if (PowerUpIsInTheWay(x, y))
                {
                    continue;
                }

                // Anything above it....?
                if (_gameScreens[screenNumber].ScreenTiles[x, y - 1].TileImage != -1)
                {
                    continue;
                }

                // Only place on tiles with collision blocks below the powerup
                if (_screenCollisionDataArray[tileImage, 1, 0] != 1 ||
                    _screenCollisionDataArray[tileImage, 2, 0] != 1)
                {
                    continue;
                }

                toReturn.Add(new InteractiveItem(
                    x * tiles.SpriteWidth + PlacementXInset,
                    y * tiles.SpriteHeight - PlacementYLift,
                    0));
            }
        }

        return toReturn;
    }

    /// <summary>
    ///     Whether the screen's power-up already occupies a tile.
    /// </summary>
    /// <remarks>
    ///     Kept exactly as found, and both of its oddities are deliberate for now: it compares the
    ///     power-up's pixel location against a tile index, and it reads the power-up of the screen
    ///     currently being displayed rather than the one being filled in. Either could be a defect and
    ///     neither belongs in a move.
    /// </remarks>
    private bool PowerUpIsInTheWay(int x, int y)
    {
        InteractiveItem? powerUpOnScreen = _gameScreens[_screenNumber].PowerUp;

        return powerUpOnScreen != null &&
               powerUpOnScreen.XLocation == x + 32 &&
               powerUpOnScreen.YLocation == y + 32;
    }

    public void SaveData()
    {
        byte[] gameScreensData = MessagePackSerializer.Serialize(_gameScreens);
        byte[] collisionData = MessagePackSerializer.Serialize(_screenCollisionDataArray);

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

    public void UpdateCollisionMap(bool screenChanged)
    {
        BuildScreenCollisionMap(screenChanged);
    }

    private void BuildScreenCollisionMap(bool screenChanged)
    {
        // Special cases such as jump platforms are held back and appended once the map is rebuilt
        var collisionsToAppend = new List<CollisionTile>();

        if (screenChanged)
        {
            _keyLockDoors = null;
        }

        if (!screenChanged)
        {
            RemoveGameObjectCollisionTiles();
        }

        _gameScreens[ScreenNumber].CollisionMap = new List<CollisionTile>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage <= -1)
                {
                    continue;
                }

                AddTileCollisionCells(x, y);
                AddInteriorCollisionCells(x, y);
                AddCentralSpikeCollisionCells(x, y);
                AddWallSpikeCollisionCells(x, y);
                AddSecretPassageCollisionCells(x, y);
            }
        }

        AddScreenObjectCollisionTiles(screenChanged, collisionsToAppend);

        _gameScreens[ScreenNumber].CollisionMap.AddRange(collisionsToAppend);
    }

    /// <summary>
    ///     Game object collision tiles are about to be rebuilt, so the stale ones come off first.
    /// </summary>
    private void RemoveGameObjectCollisionTiles()
    {
        for (int a = 0; a < _gameScreens[ScreenNumber].CollisionMap.Count; a++)
        {
            if (_gameScreens[ScreenNumber].CollisionMap[a].TileImage == (int)CollisionObjectType.GameObject)
            {
                // Remove any game object collision tiles becuase we are about to update them
                _gameScreens[ScreenNumber].CollisionMap.Remove(_gameScreens[ScreenNumber].CollisionMap[a]);
            }
        }
    }

    /// <summary>
    ///     The cells of one tile that everything collides with, read from the collision data that ships
    ///     alongside the screens.
    /// </summary>
    private void AddTileCollisionCells(int x, int y)
    {
        for (int y1 = 0; y1 < TileMetrics.CollisionCellsDownTile; y1++)
        {
            for (int x1 = 0; x1 < TileMetrics.CollisionCellsAcrossTile; x1++)
            {
                if (_screenCollisionDataArray[_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage, x1,
                        y1] == 1)
                {
                    _gameScreens[ScreenNumber].CollisionMap.Add(
                        new CollisionTile(
                            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage,
                            x * _resources
                                .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteWidth +
                            x1 * CollAreaWidth,
                            y * _resources
                                .GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).SpriteHeight +
                            y1 * CollAreaHeight,
                            CollisionTileType.EveryoneHits));
                }
            }
        }
    }

    /// <summary>
    ///     Teleporters, access booths and tunnel lifts. Blob walks into these on purpose, so their cells
    ///     stop everything except him.
    /// </summary>
    private void AddInteriorCollisionCells(int x, int y)
    {
        if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TeleportBooth ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.AccessBoothLeft ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.AccessBoothRight ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftTop1 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftShaft1 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftShaft2 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftShaft3 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftTop2 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.TunnelLiftTop3)
        {
            // Cover the whole tile since it doesn't matter - nasties can't touch any of it anyway
            int tileX = x * TileMetrics.TileWidth;
            int tileY = y * TileMetrics.TileHeight;

            for (int a = 0; a < TileMetrics.CollisionCellsDownTile; a++)
            {
                for (int b = 0; b < TileMetrics.CollisionCellsAcrossTile; b++)
                {
                    _gameScreens[ScreenNumber].CollisionMap.Add(
                        new CollisionTile(
                            (int)CollisionObjectType.GameObject, // Random image that's meaningless
                            b * TileMetrics.CollisionCellWidth + tileX,
                            a * TileMetrics.CollisionCellHeight + tileY,
                            CollisionTileType.NotBlob));
                }
            }
        }
    }

    /// <summary>
    ///     Central spikes and broken bottles. Only the four middle cells, and only against nasties - blob
    ///     is meant to be able to walk onto them and die.
    /// </summary>
    private void AddCentralSpikeCollisionCells(int x, int y)
    {
        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.Spike2 ||
            _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.Spike3)
        {
            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth + tileX,
                    TileMetrics.CollisionCellHeight + tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth + tileX,
                    TileMetrics.CollisionCellHeight * 2 + tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 2 + tileX,
                    TileMetrics.CollisionCellHeight + tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 2 + tileX,
                    TileMetrics.CollisionCellHeight * 2 + tileY,
                    CollisionTileType.NotBlob));
        }
    }

    /// <summary>
    ///     Spikes mounted on a right-hand wall, so their cells run down the tile rather than across its
    ///     middle.
    /// </summary>
    private void AddWallSpikeCollisionCells(int x, int y)
    {
        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == (int)ActionTiles.Spike1)
        {
            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 2 + tileX,
                    tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 3 + tileX,
                    tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 2 + tileX,
                    TileMetrics.CollisionCellHeight + tileY,
                    CollisionTileType.NotBlob));

            _gameScreens[ScreenNumber].CollisionMap.Add(
                new CollisionTile(
                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                    TileMetrics.CollisionCellWidth * 3 + tileX,
                    TileMetrics.CollisionCellHeight + tileY,
                    CollisionTileType.NotBlob));
        }
    }

    private void AddSecretPassageCollisionCells(int x, int y)
    {
        int tileX = x * TileMetrics.TileWidth;
        int tileY = y * TileMetrics.TileHeight;

        for (int a = 0; a < TunnelTiles.MouthPairs; a++)
        {
            int tunnelTile = a + TunnelTiles.FirstMouth;
            if (tunnelTile % 2 == 0)
            {
                // Left passages
                if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == tunnelTile)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                tileX,
                                b * 32 + tileY,
                                CollisionTileType.NotBlob));
                    }
                }
            }
            else
            {
                // Right passages
                if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == tunnelTile)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                tileX + 96,
                                b * 32 + tileY,
                                CollisionTileType.NotBlob));
                    }
                }
            }
        }
    }

    /// <summary>
    ///     The power-up, the pick-ups and the pyramid. On a screen change these go straight onto the map
    ///     just rebuilt; otherwise they are held back and appended to it.
    /// </summary>
    private void AddScreenObjectCollisionTiles(bool screenChanged, List<CollisionTile> collisionsToAppend)
    {
        List<CollisionTile> objectCollisions = screenChanged ? _gameScreens[ScreenNumber].CollisionMap : collisionsToAppend;

        InteractiveItem? powerUp = _gameScreens[ScreenNumber].PowerUp;
        if (powerUp != null)
        {
            AddObjectCollisionTiles(objectCollisions, powerUp);
        }

        foreach (InteractiveItem pickUp in _gameScreens[ScreenNumber].PickUps)
        {
            AddObjectCollisionTiles(objectCollisions, pickUp);
        }

        InteractiveItem? cheopsPyramid = _gameScreens[ScreenNumber].CheopsPyramid;
        if (cheopsPyramid != null)
        {
            AddObjectCollisionTiles(objectCollisions, cheopsPyramid);
        }
    }

    /// <summary>
    ///     An item sitting on a screen occupies two tiles each way, and nothing but blob collides
    ///     with one.
    /// </summary>
    private static void AddObjectCollisionTiles(List<CollisionTile> collisionMap, InteractiveItem item)
    {
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                collisionMap.Add(
                    new CollisionTile(
                        (int)CollisionObjectType.GameObject, // Random image that's meaningless
                        item.XLocation + x * 32,
                        item.YLocation + y * 32,
                        CollisionTileType.NotBlob));
            }
        }
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

    public int CheckRectangles(
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        bool isBlob)
    {
        // Cycle through the collision map
        foreach (CollisionTile collisionTile in _gameScreens[_screenNumber].CollisionMap)
        {
            var collide =
                CollisionDetection.Check(
                    new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                    new Rectangle(collisionTile.XPos,
                        collisionTile.YPos, CollAreaWidth, CollAreaHeight));

            // A NotBlob tile is there for the nasties to walk on, so the blob passes through it
            // and we carry on looking for something it does hit.
            if (collide && !(isBlob && collisionTile.CollisionTileType == CollisionTileType.NotBlob))
            {
                return collisionTile.TileImage;
            }
        }

        return -1;
    }

    public int CheckCollision(
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
    {
        int checkMovementAmount = velocity;

        _detectedCollisionMap.Clear();

        // Cycle through the collision map
        foreach (CollisionTile colTile in _gameScreens[_screenNumber].CollisionMap)
        {
            _detectedCollisionMap.Add(colTile);

            int checkMovementAmountTemp = StepAllowedBy(
                colTile,
                playerDirection,
                playerXPos,
                playerYPos,
                playerWidth,
                playerHeight,
                velocity,
                isBlob);

            if (checkMovementAmountTemp <= checkMovementAmount)
            {
                checkMovementAmount = checkMovementAmountTemp;
            }
        }

        if (!isBlob)
        {
            checkMovementAmount = ClampToScreenBounds(
                playerDirection,
                playerXPos,
                playerYPos,
                playerWidth,
                playerHeight,
                checkMovementAmount);
        }

        return checkMovementAmount;
    }

    /// <summary>
    ///     How far one collision tile allows the mover to travel. Two kinds of tile let it pass
    ///     regardless: those marked <see cref="CollisionTileType.NotBlob" /> when the mover is blob,
    ///     and a game object a nasty has ended up embedded inside, which is released rather than
    ///     trapped.
    /// </summary>
    private int StepAllowedBy(
        CollisionTile colTile,
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity,
        bool isBlob)
    {
        if (colTile.CollisionTileType == CollisionTileType.NotBlob && isBlob)
        {
            return velocity;
        }

        if (colTile.TileImage > FirstGameObjectImage &&
            !isBlob &&
            (CollisionObjectType)colTile.TileImage is CollisionObjectType.JumpPlatform or
            CollisionObjectType.CoreElement or
            CollisionObjectType.GameObject &&
            CollisionDetection.Check(
                new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                new Rectangle(colTile.XPos, colTile.YPos, GameObjectProbeSize, GameObjectProbeSize)))
        {
            return velocity;
        }

        return CollisionDetection.CheckDirection(
            playerDirection,
            velocity,
            playerXPos,
            playerYPos,
            playerWidth,
            playerHeight,
            colTile.XPos,
            colTile.YPos,
            CollAreaWidth,
            CollAreaHeight);
    }

    /// <summary>
    ///     Stops a mover at the edge of the screen. Only nasties are clamped: blob walking off an edge
    ///     is how he changes screens.
    /// </summary>
    /// <remarks>
    ///     This was four arms, one per direction, which were two shapes written twice. Up and left run
    ///     into nought; down and right run into the screen's own width or height less the mover's size.
    /// </remarks>
    private int ClampToScreenBounds(
        Direction playerDirection,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int checkMovementAmount)
    {
        (int position, int size, int limit) = playerDirection switch
        {
            Direction.Up or Direction.Down => (playerYPos, playerHeight, ScreenHeight),
            Direction.Left or Direction.Right => (playerXPos, playerWidth, ScreenWidth),
            _ => (0, 0, 0)
        };

        if (playerDirection is Direction.Up or Direction.Left)
        {
            int overshoot = position - checkMovementAmount;

            return overshoot < 0 ? checkMovementAmount + overshoot : checkMovementAmount;
        }

        if (playerDirection is Direction.Down or Direction.Right)
        {
            int overshoot = position + size + checkMovementAmount - limit;

            return overshoot > 0 ? checkMovementAmount - overshoot : checkMovementAmount;
        }

        return checkMovementAmount;
    }

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

    private List<Point> GetCoordsOfTiles(int tileType, int screenNumber)
    {
        var returnedTiles = new List<Point>();

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage == tileType)
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

    public TileDetected? CheckTileByObject(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
    {
        if (ProbeRectangleFor(directionToCheck, playerXPos, playerYPos, playerWidth, playerHeight, velocity)
            is not { } probe)
        {
            return null;
        }

        SpriteCollection tiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        // A check along the vertical axis reports where the object's foot sits inside the tile; every
        // other check reports where its top does.
        bool measureFromFoot = directionToCheck is Direction.Up or Direction.Down;

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage <= -1)
                {
                    continue;
                }

                var tileRect = new Rectangle(
                    x * tiles.SpriteWidth,
                    y * tiles.SpriteHeight,
                    tiles.SpriteWidth,
                    tiles.SpriteHeight);

                if (!CollisionDetection.Check(probe, tileRect))
                {
                    continue;
                }

                int xpos = playerXPos - x * tiles.SpriteWidth;
                int ypos = (measureFromFoot ? playerYPos + playerHeight : playerYPos) - y * tiles.SpriteHeight;

                return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos, ypos, x, y);
            }
        }

        return null;
    }

    /// <summary>
    ///     The box to look for a tile in, given which way the object is about to move. Null for a
    ///     direction that is not checked, which is every diagonal.
    /// </summary>
    /// <remarks>
    ///     The four straight directions each probe a different shape, which is the only real difference
    ///     between the five arms this replaced: up and left shift the whole box, down probes a thin
    ///     strip below the object's feet, and right stretches the box rather than shifting it.
    /// </remarks>
    private static Rectangle? ProbeRectangleFor(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
        => directionToCheck switch
        {
            Direction.Up => new Rectangle(playerXPos, playerYPos - velocity, playerWidth, playerHeight),
            Direction.Down => new Rectangle(playerXPos, playerYPos + playerHeight, playerWidth, velocity),
            Direction.Left => new Rectangle(playerXPos - velocity, playerYPos, playerWidth, playerHeight),
            Direction.Right => new Rectangle(playerXPos, playerYPos, playerWidth + velocity, playerHeight),
            Direction.None => new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
            _ => null
        };

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

    public void DrawCoreElements(int xPos, int yPos, GameTime gameTime)
    {
        int objNo = 0;

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
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
                DrawLowerTile(x, y, colour, gameTime);
            }
        }

        ColourAnimations.Draw(gameTime, colour.A);

        // The intro sequence flies over a screen whose objects have not been placed yet
        bool objectsArePlaced = !introSequenceRunning || introSequenceStage == IntroSequenceStage.EndPause;

        if (objectsArePlaced)
        {
            DrawPowerUp(colour, gameTime);
            DrawPickUps(colour, gameTime);
            DrawCheopsPyramid(colour, gameTime);
        }

        DrawOllyBackground(colour, gameTime);
    }

    private void DrawLowerTile(int x, int y, Color colour, GameTime gameTime)
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

            tiles.Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
            tiles.DrawSprite(tileImage, x * _tileWidth, y * _tileHeight, colour);
            tiles.End();
        }

        // Draw animation portions
        _resources.GetAnimationEngine()
            .Draw(tileImage, x * _tileWidth, y * _tileHeight, colour, gameTime);
    }

    private void DrawPowerUp(Color colour, GameTime gameTime)
    {
        if (CurrentGameScreenData.PowerUp == null)
        {
            return;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps).DrawSprite(
            CurrentGameScreenData.PowerUp.Type, CurrentGameScreenData.PowerUp.XLocation,
            CurrentGameScreenData.PowerUp.YLocation, colour);
        _resources.GetSpriteCollection(_graphicsSet.Names.PowerUps).End();
    }

    private void DrawPickUps(Color colour, GameTime gameTime)
    {
        if (CurrentGameScreenData.PickUps == null)
        {
            return;
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
        foreach (InteractiveItem pickUp in CurrentGameScreenData.PickUps)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).DrawSprite(
                pickUp.Type, pickUp.XLocation,
                pickUp.YLocation, colour);
        }

        _resources.GetSpriteCollection(_graphicsSet.Names.Pickups).End();
    }

    private void DrawCheopsPyramid(Color colour, GameTime gameTime)
    {
        if (CurrentGameScreenData.CheopsPyramid == null)
        {
            return;
        }

        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection("CheopsPyramidOriginal")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0,
                CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                colour);
            _resources.GetSpriteCollection("CheopsPyramidOriginal").End();
        }
        else
        {
            _resources.GetSpriteCollection("CheopsPyramid")
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            _resources.GetSpriteCollection("CheopsPyramid").DrawSprite(0,
                CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                colour);
            _resources.GetSpriteCollection("CheopsPyramid").End();
        }
    }

    /// <summary>
    ///     Olly has no backdrop in the original graphics, which is why the old set skips this.
    /// </summary>
    private void DrawOllyBackground(Color colour, GameTime gameTime)
    {
        if (ScreenNumber != OllyScreenNumber || _graphicsSet.IsOriginal)
        {
            return;
        }

        _resources.GetSpriteCollection("OllyBackground")
            .Begin(BlendState.AlphaBlend, gameTime);
        _resources.GetSpriteCollection("OllyBackground")
            .DrawSprite(0, 382, 146, colour);
        _resources.GetSpriteCollection("OllyBackground").End();
    }

    public void DrawUpperLayer(Color colour, GameTime gameTime)
    {
        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                DrawUpperTilePatch(x, y, colour, gameTime);
                DrawTunnelEntrance(x, y, colour, gameTime);
            }
        }

        DrawOlly(colour, gameTime);
        DrawKeyDoorForceFields();

        ColourAnimations.DrawUpperLayer(gameTime, colour.A);
    }

    /// <summary>
    ///     Lays the upper-layer patch over one tile - the inside of a lift shaft, a booth, a tunnel mouth,
    ///     the shadow another tile casts. A tile with no patch is left as the lower layer drew it.
    /// </summary>
    private void DrawUpperTilePatch(int x, int y, Color colour, GameTime gameTime)
    {
        if (UpperLayerPatchFor(CurrentGameScreenData.ScreenTiles[x, y].TileImage) is not var (sprite, rowOffset))
        {
            return;
        }

        SpriteCollection upperTiles = _resources.GetSpriteCollection(_graphicsSet.Names.UpperScreenTiles);
        SpriteCollection lowerTiles = _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles);

        upperTiles.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
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

    private void DrawTunnelEntrance(int x, int y, Color colour, GameTime gameTime)
    {
        if (CurrentGameScreenData.ScreenTiles[x, y].TileImage > LastCollidableTile)
        {
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .DrawSprite(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, x * _tileWidth,
                    y * _tileHeight, colour);
            _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles).End();
        }
    }

    private void DrawOlly(Color colour, GameTime gameTime)
    {
        if (ScreenNumber == OllyScreenNumber)
        {
            if (_graphicsSet.IsOriginal)
            {
                _resources.GetSpriteCollection("OllyOriginal")
                    .Begin(BlendState.AlphaBlend, gameTime);
                _resources.GetSpriteCollection("OllyOriginal")
                    .DrawSprite(0, 384, 100, colour);
                _resources.GetSpriteCollection("OllyOriginal").End();
            }
            else
            {
                _resources.GetSpriteCollection("OllyLower")
                    .Begin(BlendState.AlphaBlend, gameTime);
                _resources.GetSpriteCollection("OllyLower").DrawSprite(0, 382, 146, colour);
                _resources.GetSpriteCollection("OllyLower").End();

                _resources.GetSpriteCollection("OllyEyes")
                    .Begin(BlendState.AlphaBlend, gameTime);
                _resources.GetSpriteCollection("OllyEyes")
                    .DrawSprite(0, _ollyEyesX, _ollyEyesY, colour);
                _resources.GetSpriteCollection("OllyEyes").End();

                _resources.GetSpriteCollection("OllyUpper")
                    .Begin(BlendState.AlphaBlend, gameTime);
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

    public void DrawShadowing(GameTime gameTime)
    {
        if (_graphicsSet.IsRedrawn)
        {
            // Status panel shadow
            _resources.GetSpriteCollection("StatusPanelShadow")
                .Begin(BlendState.AlphaBlend, gameTime);
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
    ///     Scatters the pick-ups, power-ups, pyramids and core elements over the whole world map. Called
    ///     once at the start of a game, not on a screen change.
    /// </summary>
    /// <remarks>
    ///     The scatter used to draw from a Random of its own, seeded from the clock, while the class was
    ///     already holding one from its constructor. It uses that one now, so a world can be reproduced
    ///     from the seed that made it - which is the only way a layout described in a bug report can be
    ///     got back, and the only way any of this can be tested.
    /// </remarks>
    public void Reset()
    {
        ScreenNumber = WorldMap.ShipWreckScreenNumber;

        List<int> powerUpScreens = ChoosePowerUpScreens();
        PlaceScreenObjects(powerUpScreens);

        var screensContainingCheopsPyramid = new List<GameScreenData>();
        var screensContainingCoreElements = new List<GameScreenData>();
        CollectScreensWithCheopsAndCoreElements(screensContainingCheopsPyramid, screensContainingCoreElements);

        TrimTo(screensContainingCheopsPyramid, CMaxCheops, screen => screen.CheopsPyramid = null);
        TrimTo(screensContainingCoreElements, CMaxCoreElements,
            screen => screen.PickUps = new List<InteractiveItem>());

        MatchSomeCoreElementsToTheCore(screensContainingCoreElements);
    }

    /// <summary>
    ///     Picks the screens a power-up goes on. Only screens that can hold more than one object qualify,
    ///     because a power-up has to share with whatever is already there.
    /// </summary>
    private List<int> ChoosePowerUpScreens()
    {
        var powerUpScreens = new List<int>();

        for (int a = 0; a < _pickUpCount; a++)
        {
            // Only place on screens where we can have more than 1 object
            int screenChosen = -1;
            while (screenChosen < 0)
            {
                int temp = _randomNumberGenerator.Next(0, WorldMap.LastScreenNumber);

                // Ensure more than one location
                if (GetPotentialLocations(temp).Count > 1)
                {
                    // Ensure we haven't chosen this screen already
                    if (!powerUpScreens.Contains(temp))
                    {
                        screenChosen = temp;
                    }
                }
            }

            powerUpScreens.Add(screenChosen);
        }

        return powerUpScreens;
    }

    /// <summary>
    ///     Fills in every screen but the core: its pick-ups, its power-up if it drew one, its pyramid and
    ///     its access code.
    /// </summary>
    private void PlaceScreenObjects(List<int> powerUpScreens)
    {
        for (int a = 0; a < _gameScreens.Length; a++)
        {
            // The core screen is seeded differently and is left out of the scatter
            if (a == WorldMap.CoreScreenNumber)
            {
                continue;
            }

            _gameScreens[a].PickUps = new List<InteractiveItem>();

            List<InteractiveItem> potentialLocations = GetPotentialLocations(a);
            bool placePickup = powerUpScreens.Contains(a);

            if (potentialLocations.Count > 0)
            {
                PlacePowerUpAndPickUp(a, potentialLocations, placePickup);
            }

            if (potentialLocations.Count > 0)
            {
                PlaceCheopsPyramid(a, potentialLocations);
            }

            AssignAccessBoothCode(a);
        }
    }

    /// <summary>
    ///     The power-up first, then a pick-up on a different tile if this screen drew one. Each takes its
    ///     location out of the list so the two cannot land on top of each other.
    /// </summary>
    private void PlacePowerUpAndPickUp(
        int screenNumber,
        List<InteractiveItem> potentialLocations,
        bool placePickup)
    {
        int powerUpToSelect = _randomNumberGenerator.Next(potentialLocations.Count);
        _gameScreens[screenNumber].PowerUp = new InteractiveItem(potentialLocations[powerUpToSelect].XLocation,
            potentialLocations[powerUpToSelect].YLocation, _randomNumberGenerator.Next(8));

        // Take the powerup location out of the list
        potentialLocations.Remove(potentialLocations[powerUpToSelect]);

        // Place a pickup
        if (placePickup)
        {
            // Now select another location for our pickup
            int pickUpToSelect = _randomNumberGenerator.Next(potentialLocations.Count);

            // Check to see if this screen has a preferred location
            InteractiveItem? preferredLocation = GetPreferredLocation(screenNumber);

            // Place the item in the preferred locateion
            if (preferredLocation != null)
            {
                preferredLocation.Type = _randomNumberGenerator.Next(28);
                _gameScreens[screenNumber].PickUps.Add(preferredLocation);
                potentialLocations.Remove(potentialLocations[pickUpToSelect]);
            }
            else
            {
                // Place it in the normal randomly selected place
                _gameScreens[screenNumber].PickUps.Add(new InteractiveItem(
                    potentialLocations[pickUpToSelect].XLocation,
                    potentialLocations[pickUpToSelect].YLocation, _randomNumberGenerator.Next(28)));
                potentialLocations.Remove(potentialLocations[pickUpToSelect]);
            }
        }
    }

    /// <summary>
    ///     Every screen with room gets one at this stage. Reset thins them down to CMaxCheops afterwards,
    ///     which is why there is no attempt to be selective here.
    /// </summary>
    private void PlaceCheopsPyramid(
        int screenNumber,
        List<InteractiveItem> potentialLocations)
    {
        int cheopsLocationToSelect = _randomNumberGenerator.Next(potentialLocations.Count);
        _gameScreens[screenNumber].CheopsPyramid = new InteractiveItem(
            potentialLocations[cheopsLocationToSelect].XLocation,
            potentialLocations[cheopsLocationToSelect].YLocation, 0);

        // Setup the access code for the pyramid
        // Ensure we get a unique code
        string cheopsAccessCode;
        do
        {
            cheopsAccessCode = _randomNumberGenerator.Next(CheopsCodeLowest, CheopsCodeExclusiveUpperBound).ToString();
        } while (!CheckAccessCodeValidity(cheopsAccessCode));

        _gameScreens[screenNumber].CheopsAccessCode = cheopsAccessCode;
    }

    /// <summary>
    ///     A screen with a booth on either side gets one code, which is what the booth on the far side of
    ///     the door asks for.
    /// </summary>
    private void AssignAccessBoothCode(int screenNumber)
    {
        List<Point> leftBooths = GetCoordsOfTiles((int)ActionTiles.AccessBoothLeft, screenNumber);
        List<Point> rightBooths = GetCoordsOfTiles((int)ActionTiles.AccessBoothRight, screenNumber);
        List<Point> accessBooths = leftBooths.Count > 0 ? leftBooths : rightBooths;

        if (accessBooths.Count > 0)
        {
            // Ensure we get a unique code
            string accessCode;
            do
            {
                accessCode = _randomNumberGenerator.Next(AccessCodeLowest, AccessCodeExclusiveUpperBound).ToString();
            } while (!CheckAccessCodeValidity(accessCode));

            _gameScreens[screenNumber].AccessCode = accessCode;
        }
    }

    private void CollectScreensWithCheopsAndCoreElements(
        List<GameScreenData> screensContainingCheopsPyramid,
        List<GameScreenData> screensContainingCoreElements)
    {
        for (int a = 0; a < _gameScreens.Length; a++)
        {
            // Only add screens with a cheops pyramid assigned
            if (_gameScreens[a].CheopsPyramid != null)
            {
                screensContainingCheopsPyramid.Add(_gameScreens[a]);
            }

            // Only add screens with a core element assigned
            if (_gameScreens[a].PickUps.Count > 0)
            {
                foreach (InteractiveItem pickUp in _gameScreens[a].PickUps)
                {
                    if ((Collectables)pickUp.Type != Collectables.AccessCard &&
                        (Collectables)pickUp.Type != Collectables.Key)
                    {
                        screensContainingCoreElements.Add(_gameScreens[a]);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Thins a list of screens down to at most <paramref name="maximum" /> of them by picking one at
    ///     random and stripping what it was listed for. The pyramids and the core elements were two copies
    ///     of this loop differing only in what they cleared.
    /// </summary>
    private void TrimTo(
        List<GameScreenData> screens,
        int maximum,
        Action<GameScreenData> removeFrom)
    {
        while (screens.Count > maximum)
        {
            GameScreenData screen = screens[_randomNumberGenerator.Next(0, screens.Count)];

            removeFrom(screen);
            screens.Remove(screen);
        }
    }

    /// <summary>
    ///     Between two and five of the core elements lying about the map are set to ones the core actually
    ///     wants, so that a game is always winnable without being trivially so.
    /// </summary>
    private void MatchSomeCoreElementsToTheCore(List<GameScreenData> screensContainingCoreElements)
    {
        var tempElements = new List<CoreElement>();

        for (int a = 0; a < _elements.GetActiveElements().Count; a++)
        {
            tempElements.Add(_elements.GetActiveElements()[a]);
        }

        // Make a random number of the core elements on the map match what we need. Neither list is a
        // fixed length - one is however many screens the generator gave a pick-up to, the other however
        // many elements the core still wants - so the draw is clamped to what is actually there rather
        // than trusted to fit. A hundred pick-up screens is what makes five safe today, not construction.
        int matchesToMake = Math.Min(
            _randomNumberGenerator.Next(2, 6),
            Math.Min(screensContainingCoreElements.Count, tempElements.Count));

        for (int a = 0; a < matchesToMake; a++)
        {
            int elementIndex = _randomNumberGenerator.Next(0, tempElements.Count);
            screensContainingCoreElements[a].PickUps[0].Type = tempElements[elementIndex].CurrentElement;
            tempElements.Remove(tempElements[elementIndex]);
        }
    }
}
