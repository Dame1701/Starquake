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
using Starquake.Collisions;
using Starquake.Engines;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.Display;

public class GameScreenCollection : GameComponentBase
{

    private const int CMaxCheops = 40;
    private const int CMaxCoreElements = 18;

    private readonly List<CollisionTile> _detectedCollisionMap;
    private readonly int _pickUpCount = 100;

    private GameScreenData[] _gameScreens;

    private IStandardParticleEffect _keyDoorForceFieldNoAccess;

    private List<KeyLockDoor> _keyLockDoors;

    private int _oldXoffset;
    private int _oldYoffset;


    // olly
    private int _ollyEyesX;
    private int _ollyEyesY;

    private int[,,] _screenCollisionDataArray;

    private int _screenNumber;
    private int _tileHeight;

    private int _tileWidth;

    public GameScreenCollection(GameEngine gameEngineRef, int tilesAcross, int tilesDown, int xOffset, int yOffset,
        int collAreaWidth, int collAreaHeight) : base(gameEngineRef)
    {
        _oldXoffset = xOffset;
        _oldYoffset = yOffset;

        this.TilesAcross = tilesAcross;
        this.TilesDown = tilesDown;

        this.CollAreaWidth = collAreaWidth;
        this.CollAreaHeight = collAreaHeight;

        _detectedCollisionMap = new List<CollisionTile>();
    }

    public int TilesAcross { get; set; }

    public int TilesDown { get; set; }

    public GameScreenData CurrentGameScreenData => _gameScreens[ScreenNumber];

    public IStandardParticleEffect BlobsShipSmoke1 { get; private set; }

    public IStandardParticleEffect BlobsShipSmoke2 { get; private set; }

    public int ScreenWidth => GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
        .SpriteWidth * TilesAcross;

    public int ScreenHeight => GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
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

    public new void LoadContent()
    {
        // Load screen data
        _gameScreens = (GameScreenData[])GameEngine.ContentResourceManager.GetDataResource("GameScreens");
        _screenCollisionDataArray =
            (int[,,])GameEngine.ContentResourceManager.GetDataResource("ScreenCollisionData");

        BlobsShipSmoke1 =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.Continual, null) as IStandardParticleEffect;
        BlobsShipSmoke2 =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume2",
                ParticleEffectType.Continual, null) as IStandardParticleEffect;

        _keyDoorForceFieldNoAccess =
            GameEngine.ContentResourceManager.GetParticleEffectInstance("ForceField",
                ParticleEffectType.Continual, null) as IStandardParticleEffect;

        // Make this better in the future...?
        _tileWidth = 128;
        _tileHeight = 96;

        // Save out the data to MessagePack
        //SaveData();
    }

    public override void Initialise(object data)
    {
        // Setup Collision map, we're using 8 x 8 blocks for each collision area for improved accuracy
        BuildScreenCollisionMap(true);
    }

    public int GetPercentageOfScreensVisited()
    {
        var screensVisited = 0;

        for (var a = 0; a < _gameScreens.Length; a++)
        {
            if (_gameScreens[a].ScreenVisited)
            {
                screensVisited++;
            }
        }

        return (int)Math.Ceiling(screensVisited / (double)_gameScreens.Length * 100);
    }

    private bool CheckAccessCodeValidity(string accessCode)
    {
        var unique = true;

        for (var a = 0; a < accessCode.Length; a++)
        {
            for (var b = 0; b < accessCode.Length; b++)
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

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == -1)
                {
                    var addLocation = true;

                    // Now check the collision map for this tile
                    for (var y1 = 0; y1 < 3; y1++)
                    {
                        for (var x1 = 0; x1 < 4; x1++)
                        {
                            for (var a = 0; a < _gameScreens[ScreenNumber].CollisionMap.Count; a++)
                            {
                                if (_gameScreens[ScreenNumber].CollisionMap[a].XPos ==
                                    x * _tileWidth + x1 * CollAreaWidth &&
                                    _gameScreens[ScreenNumber].CollisionMap[a].YPos ==
                                    y * _tileHeight + y1 * CollAreaHeight)
                                {
                                    if (_gameScreens[ScreenNumber].CollisionMap[a].CollisionTileType ==
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

    private InteractiveItem GetPreferredLocation(int screenNumber)
    {
        for (var y = 1; y < _gameScreens[screenNumber].ScreenTiles.GetLength(1); y++)
        {
            for (var x = 0; x < _gameScreens[screenNumber].ScreenTiles.GetLength(0); x++)
            {
                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage == 392)
                {
                    return new InteractiveItem(
                        x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth + 32,
                        y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight + 32, 0);
                }
            }
        }

        return null;
    }

    private List<InteractiveItem> GetPotentialLocations(int screenNumber)
    {
        var toReturn = new List<InteractiveItem>();

        for (var y = 1; y < _gameScreens[screenNumber].ScreenTiles.GetLength(1); y++)
        {
            for (var x = 0; x < _gameScreens[screenNumber].ScreenTiles.GetLength(0); x++)
            {
                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage >= 0)
                {
                    // Valid tile to place above...?
                    if (
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.ShuttlePlatform &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.RemovedShuttlePlatform &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.ElectricityNode &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.ShuttleTag1 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.ShuttleTag2 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.ShuttleTag3 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.Spike1 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.Spike2 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.Spike3 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.ElectricityGate &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.BlankFiller &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.ConveyerNasty &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.BreakableBridges &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != 120 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage != 121 &&
                        _gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)ActionTiles.PreferredCoreLocation)
                    {
                        // Check for powerups in the way - useful for pickup sweep
                        var powerUpInWay = false;
                        if (_gameScreens[_screenNumber].PowerUp != null)
                        {
                            if (_gameScreens[_screenNumber].PowerUp.XLocation == x + 32 &&
                                _gameScreens[_screenNumber].PowerUp.YLocation == y + 32)
                            {
                                powerUpInWay = true;
                            }
                        }

                        if (!powerUpInWay)
                        {
                            // Anything above it....?
                            if (_gameScreens[screenNumber].ScreenTiles[x, y - 1].TileImage == -1)
                            {
                                // now lets check the collision map and only place on tiles with a collision blocks below the powerup
                                var locationFound = false;

                                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage >= 0)
                                {
                                    if (_screenCollisionDataArray[
                                            _gameScreens[screenNumber].ScreenTiles[x, y].TileImage, 1, 0] == 1 &&
                                        _screenCollisionDataArray[
                                            _gameScreens[screenNumber].ScreenTiles[x, y].TileImage, 2, 0] == 1)
                                    {
                                        locationFound = true;
                                    }
                                }
                                else
                                {
                                    locationFound = false;
                                }

                                if (locationFound)
                                {
                                    toReturn.Add(new InteractiveItem(
                                        x * GameEngine.ContentResourceManager
                                            .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth +
                                        32,
                                        y * GameEngine.ContentResourceManager
                                            .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight -
                                        64, 0));
                                }
                            }
                        }
                    }
                }
            }
        }

        return toReturn;
    }

    public void SaveData()
    {
        var gameScreensData = MessagePackSerializer.Serialize(_gameScreens);
        var collisionData = MessagePackSerializer.Serialize(_screenCollisionDataArray);

        using (var fs = new FileStream("SQScreenData2.dat", FileMode.Create, FileAccess.Write))
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
        // Grab any collision tiles that are special cases i.e. Jump Platforms etc.
        List<CollisionTile> collisionsToAppend = null;

        if (screenChanged)
        {
            _keyLockDoors = null;
        }

        if (!screenChanged)
        {
            if (collisionsToAppend == null)
            {
                collisionsToAppend = new List<CollisionTile>();
            }

            for (var a = 0; a < _gameScreens[ScreenNumber].CollisionMap.Count; a++)
            {
                if (_gameScreens[ScreenNumber].CollisionMap[a].TileImage == (int)CollisionObjectType.GameObject)
                {
                    // Remove any game object collision tiles becuase we are about to update them
                    _gameScreens[ScreenNumber].CollisionMap.Remove(_gameScreens[ScreenNumber].CollisionMap[a]);
                }
            }
        }

        // Create new collision map
        _gameScreens[ScreenNumber].CollisionMap = new List<CollisionTile>();

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage > -1)
                {
                    for (var y1 = 0; y1 < 3; y1++)
                    {
                        for (var x1 = 0; x1 < 4; x1++)
                        {
                            if (_screenCollisionDataArray[_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage, x1,
                                    y1] == 1)
                            {
                                _gameScreens[ScreenNumber].CollisionMap.Add(
                                    new CollisionTile(
                                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage,
                                        x * GameEngine.ContentResourceManager
                                            .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth +
                                        x1 * CollAreaWidth,
                                        y * GameEngine.ContentResourceManager
                                            .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight +
                                        y1 * CollAreaHeight,
                                        CollisionTileType.EveryoneHits));
                            }
                        }
                    }

                    var tileX = 0;
                    var tileY = 0;

                    // Add collision map for inside of teleporters, access booths and tunnel lifts
                    if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 397 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 399 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 400 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 407 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 409 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 412 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 413 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 414 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 415)
                    {
                        // Cover the whole tile since it doesn't matter - nasties can't touch any of it anyway
                        tileX = x * 128;
                        tileY = y * 96;

                        for (var a = 0; a < 3; a++)
                        {
                            for (var b = 0; b < 4; b++)
                            {
                                _gameScreens[ScreenNumber].CollisionMap.Add(
                                    new CollisionTile(
                                        (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                        b * 32 + tileX,
                                        a * 32 + tileY,
                                        CollisionTileType.NotBlob));
                            }
                        }
                    }

                    // Add nasty collision map for spiky tiles
                    tileX = x * 128;
                    tileY = y * 96;

                    // Central spiky tiles and Broken bottle spiky tiles
                    if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 405 ||
                        _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 416)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                32 + tileX,
                                32 + tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                32 + tileX,
                                64 + tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                64 + tileX,
                                32 + tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                64 + tileX,
                                64 + tileY,
                                CollisionTileType.NotBlob));
                    }

                    // Right side mounted spikey tiles
                    if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 404)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                64 + tileX,
                                tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                96 + tileX,
                                tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                64 + tileX,
                                32 + tileY,
                                CollisionTileType.NotBlob));

                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                96 + tileX,
                                32 + tileY,
                                CollisionTileType.NotBlob));
                    }

                    // Add collision map for the secret passages
                    tileX = x * 128;
                    tileY = y * 96;

                    for (var a = 0; a < 10; a++)
                    {
                        var tunnelTile = a + 420;
                        if (tunnelTile % 2 == 0)
                        {
                            // Left passages
                            if (_gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == tunnelTile)
                            {
                                for (var b = 0; b < 3; b++)
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
                                for (var b = 0; b < 3; b++)
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
            }
        }


        // Add secondary maps for Cheops pyramids etc.
        if (_gameScreens[ScreenNumber].PowerUp != null)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    if (screenChanged)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                _gameScreens[ScreenNumber].PowerUp.XLocation + x * 32,
                                _gameScreens[ScreenNumber].PowerUp.YLocation + y * 32,
                                CollisionTileType.NotBlob));
                    }
                    else
                    {
                        collisionsToAppend.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                _gameScreens[ScreenNumber].PowerUp.XLocation + x * 32,
                                _gameScreens[ScreenNumber].PowerUp.YLocation + y * 32,
                                CollisionTileType.NotBlob));
                    }
                }
            }
        }

        if (_gameScreens[ScreenNumber].PickUps != null)
        {
            for (var a = 0; a < _gameScreens[ScreenNumber].PickUps.Count; a++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < 2; x++)
                    {
                        if (screenChanged)
                        {
                            _gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    _gameScreens[ScreenNumber].PickUps[a].XLocation + x * 32,
                                    _gameScreens[ScreenNumber].PickUps[a].YLocation + y * 32,
                                    CollisionTileType.NotBlob));
                        }
                        else
                        {
                            collisionsToAppend.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    _gameScreens[ScreenNumber].PickUps[a].XLocation + x * 32,
                                    _gameScreens[ScreenNumber].PickUps[a].YLocation + y * 32,
                                    CollisionTileType.NotBlob));
                        }
                    }
                }
            }
        }

        if (_gameScreens[ScreenNumber].CheopsPyramid != null)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 2; x++)
                {
                    if (screenChanged)
                    {
                        _gameScreens[ScreenNumber].CollisionMap.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                _gameScreens[ScreenNumber].CheopsPyramid.XLocation + x * 32,
                                _gameScreens[ScreenNumber].CheopsPyramid.YLocation + y * 32,
                                CollisionTileType.NotBlob));
                    }
                    else
                    {
                        collisionsToAppend.Add(
                            new CollisionTile(
                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                _gameScreens[ScreenNumber].CheopsPyramid.XLocation + x * 32,
                                _gameScreens[ScreenNumber].CheopsPyramid.YLocation + y * 32,
                                CollisionTileType.NotBlob));
                    }
                }
            }
        }

        // Check to see if we need to append any special cases
        if (collisionsToAppend != null)
        {
            // Add them to the tile map
            _gameScreens[ScreenNumber].CollisionMap.AddRange(collisionsToAppend);
        }
    }

    public void ChangeShuttlePlatformState(bool padRemoved)
    {
        for (var a = 0; a < _gameScreens.Length; a++)
        {
            for (var y = 0; y < TilesDown; y++)
            {
                for (var x = 0; x < TilesAcross; x++)
                {
                    if (padRemoved)
                    {
                        if (_gameScreens[a].ScreenTiles[x, y].TileImage == (int)ActionTiles.ShuttlePlatform)
                        {
                            _gameScreens[a].ScreenTiles[x, y].TileImage = (int)ActionTiles.RemovedShuttlePlatform;

                            // Trigger Colour animation
                            GameEngine.ColourAnimationEngine.Start(401, -1);
                            GameEngine.ColourAnimationEngine.Start(417, -1);
                            GameEngine.ColourAnimationEngine.Start(418, -1);
                        }
                    }
                    else
                    {
                        if (_gameScreens[a].ScreenTiles[x, y].TileImage == (int)ActionTiles.RemovedShuttlePlatform)
                        {
                            _gameScreens[a].ScreenTiles[x, y].TileImage = (int)ActionTiles.ShuttlePlatform;

                            // Cancel Colour animation
                            GameEngine.ColourAnimationEngine.Stop(401);
                            GameEngine.ColourAnimationEngine.Stop(417);
                            GameEngine.ColourAnimationEngine.Stop(418);
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
        var collide = false;

        // Cycle through the collision map
        for (var a = 0; a < _gameScreens[_screenNumber].CollisionMap.Count; a++)
        {
            collide =
                CollisionDetection.Check(
                    new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                    new Rectangle(_gameScreens[_screenNumber].CollisionMap[a].XPos,
                        _gameScreens[_screenNumber].CollisionMap[a].YPos, CollAreaWidth, CollAreaHeight));

            if (collide)
            {
                if (isBlob && _gameScreens[_screenNumber].CollisionMap[a].CollisionTileType ==
                    CollisionTileType.NotBlob)
                {
                    collide = false;
                }
                else
                {
                    return _gameScreens[_screenNumber].CollisionMap[a].TileImage;
                }
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
        var checkMovementAmount = velocity;
        //int CheckMovementAmount = Velocity;
        _detectedCollisionMap.Clear();

        // Cycle through the collision map
        for (var a = 0; a < _gameScreens[_screenNumber].CollisionMap.Count; a++)
        {
            var colTile = _gameScreens[_screenNumber].CollisionMap[a];
            _detectedCollisionMap.Add(colTile);

            var checkMovementAmountTemp =
                CollisionDetection.CheckDirection(
                    playerDirection,
                    velocity,
                    playerXPos,
                    playerYPos,
                    playerWidth,
                    playerHeight,
                    colTile.XPos,
                    colTile.YPos,
                    CollAreaWidth,
                    CollAreaHeight
                );


            if (colTile.CollisionTileType == CollisionTileType.NotBlob &&
                isBlob)
            {
                checkMovementAmountTemp = velocity;
            }

            if (colTile.TileImage > 999 && !isBlob)
            {
                if (CollisionDetection.Check(
                        new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                        new Rectangle(colTile.XPos, colTile.YPos, 32, 32)))
                {
                    if ((CollisionObjectType)colTile.TileImage == CollisionObjectType.JumpPlatform ||
                        (CollisionObjectType)colTile.TileImage == CollisionObjectType.CoreElement ||
                        (CollisionObjectType)colTile.TileImage == CollisionObjectType.GameObject)
                    {
                        // Now see if it's actually embedded
                        // And if it is, let it out
                        checkMovementAmountTemp = velocity;
                    }
                }
            }

            if (checkMovementAmountTemp <= checkMovementAmount)
            {
                checkMovementAmount = checkMovementAmountTemp;
            }
        }

        // Check screen bounds, but only for nasties
        if (!isBlob)
        {
            if (playerDirection == Direction.Up)
            {
                var boundaryDistance = playerYPos - checkMovementAmount;
                if (boundaryDistance < 0)
                {
                    // if we're blob we can change screens!
                    checkMovementAmount = checkMovementAmount + boundaryDistance;
                }
            }
            else if (playerDirection == Direction.Down)
            {
                var boundaryDistance = playerYPos + playerHeight + checkMovementAmount;
                if (boundaryDistance > ScreenHeight)
                {
                    // if we're blob we can change screens!
                    checkMovementAmount = checkMovementAmount - (boundaryDistance - ScreenHeight);
                }
            }
            else if (playerDirection == Direction.Left)
            {
                var boundaryDistance = playerXPos - checkMovementAmount;
                if (boundaryDistance < 0)
                {
                    // if we're blob we can change screens!
                    checkMovementAmount = checkMovementAmount + boundaryDistance;
                }
            }
            else if (playerDirection == Direction.Right)
            {
                var boundaryDistance = playerXPos + playerWidth + checkMovementAmount;
                if (boundaryDistance > ScreenWidth)
                {
                    // if we're blob we can change screens!
                    checkMovementAmount = checkMovementAmount - (boundaryDistance - ScreenWidth);
                }
            }
        }

        return checkMovementAmount;
    }

    public Point ConvertToScreenCoords(int tileMapXPos, int tileMapYPos)
        => new(
            tileMapXPos * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                .SpriteWidth,
            tileMapYPos * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                .SpriteHeight);

    public List<Point> GetCoordsOfTiles(int tileType)
    {
        List<Point> returnedTiles = null;

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage == tileType)
                {
                    if (returnedTiles == null)
                    {
                        returnedTiles = new List<Point>();
                    }

                    returnedTiles.Add(new Point(
                        x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth,
                        y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight));
                }
            }
        }

        return returnedTiles;
    }

    public Point[] GetCoordsOfTiles(int[] tileTypes)
    {
        List<Point> returnedTiles = null;

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (tileTypes.Contains(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage))
                {
                    if (returnedTiles == null)
                    {
                        returnedTiles = new List<Point>();
                    }

                    returnedTiles.Add(new Point(
                        x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth,
                        y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight));
                }
            }
        }

        if (returnedTiles != null && returnedTiles.Count > 0)
        {
            return returnedTiles.OrderByDescending(rt => rt.Y).ToArray();
        }

        return null;
    }

    public List<KeyLockDoor> GetKeyLockDoors()
    {
        List<KeyLockDoor> returnedTiles = null;

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage == 419)
                {
                    if (returnedTiles == null)
                    {
                        returnedTiles = new List<KeyLockDoor>();
                    }

                    returnedTiles.Add(new KeyLockDoor
                    {
                        Location = new Point(
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight)
                    });
                }
            }
        }

        return returnedTiles;
    }

    public List<Point> GetCoordsOfTiles(int tileType, int screenNumber)
    {
        List<Point> returnedTiles = null;

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[screenNumber].ScreenTiles[x, y].TileImage == tileType)
                {
                    if (returnedTiles == null)
                    {
                        returnedTiles = new List<Point>();
                    }

                    returnedTiles.Add(new Point(
                        x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth,
                        y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight));
                }
            }
        }

        return returnedTiles;
    }

    public TileDetected CheckTileByObject(
        Direction directionToCheck,
        int playerXPos,
        int playerYPos,
        int playerWidth,
        int playerHeight,
        int velocity)
    {
        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage > -1)
                {
                    var tileRect = new Rectangle(
                        x * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth,
                        y * GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight,
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteWidth,
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .SpriteHeight);

                    var xpos = 0;
                    var ypos = 0;

                    if (directionToCheck == Direction.Up)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(playerXPos, playerYPos - velocity, playerWidth, playerHeight),
                                tileRect))
                        {
                            xpos = playerXPos - x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth;
                            ypos = playerYPos + playerHeight - y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight;

                            return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos,
                                ypos, x, y);
                        }
                    }
                    else if (directionToCheck == Direction.Down)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(playerXPos, playerYPos + playerHeight, playerWidth, velocity),
                                tileRect))
                        {
                            xpos = playerXPos - x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth;
                            ypos = playerYPos + playerHeight - y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight;

                            return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos,
                                ypos, x, y);
                        }
                    }
                    else if (directionToCheck == Direction.Left)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(playerXPos - velocity, playerYPos, playerWidth, playerHeight),
                                tileRect))
                        {
                            xpos = playerXPos - x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth;
                            ypos = playerYPos - y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight;

                            return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos,
                                ypos, x, y);
                        }
                    }
                    else if (directionToCheck == Direction.Right)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(playerXPos, playerYPos, playerWidth + velocity, playerHeight),
                                tileRect))
                        {
                            xpos = playerXPos - x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth;
                            ypos = playerYPos - y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight;

                            return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos,
                                ypos, x, y);
                        }
                    }
                    else if (directionToCheck == Direction.None)
                    {
                        if (CollisionDetection.Check(
                                new Rectangle(playerXPos, playerYPos, playerWidth, playerHeight),
                                tileRect))
                        {
                            xpos = playerXPos - x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth;
                            ypos = playerYPos - y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight;

                            return new TileDetected(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, xpos,
                                ypos, x, y);
                        }
                    }
                }
            }
        }


        return null;
    }

    public override void Update(GameTime gameTime)
    {
        // 419 is the key access door tile
        if (_keyLockDoors == null)
        {
            _keyLockDoors = GetKeyLockDoors();
        }

        if (_keyLockDoors != null)
        {
            if (_keyDoorForceFieldNoAccess != null)
            {
                _keyDoorForceFieldNoAccess.Start();

                for (var b = 0; b < _keyLockDoors.Count; b++)
                {
                    // Create the collision map
                    if (_keyLockDoors[b].CollisionTiles.Count == 0) // If the collision map doesn't exist, create it
                    {
                        CollisionTile keyLockCollisionTile = null;

                        for (var c = 0; c < 3; c++)
                        {
                            keyLockCollisionTile =
                                new CollisionTile(
                                    (int)CollisionObjectType.KeyAccessDoor, // Random image that's meaningless
                                    _keyLockDoors[b].Location.X + 48,
                                    _keyLockDoors[b].Location.Y + c * 32,
                                    CollisionTileType.None);

                            // Add the tile to the keylock door so we can remove it later
                            _keyLockDoors[b].CollisionTiles.Add(keyLockCollisionTile);

                            // Add the tile to the collision map
                            _gameScreens[ScreenNumber].CollisionMap.Add(keyLockCollisionTile);
                        }
                    }

                    _keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                        new Color(100, 100, 100).ToVector3();
                    _keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseQuantity = 100;
                    _keyDoorForceFieldNoAccess.Effect["Emitter01"].BlendMode = EmitterBlendMode.Alpha;

                    _keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                        new Color(100, 100, 100).ToVector3();
                    _keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseQuantity = 100;
                    _keyDoorForceFieldNoAccess.Effect["Emitter05"].BlendMode = EmitterBlendMode.Alpha;

                    for (var c = 0; c < 4; c++)
                    {
                        _keyDoorForceFieldNoAccess.Update(_keyLockDoors[b].Location.X + 52 + c,
                            _keyLockDoors[b].Location.Y + 48, gameTime);
                    }

                    for (var c = 0; c < 4; c++)
                    {
                        _keyDoorForceFieldNoAccess.Update(_keyLockDoors[b].Location.X + 76 + c,
                            _keyLockDoors[b].Location.Y + 48, gameTime);
                    }

                    // Change colour of the gate depending on whether blob has the access key or not
                    if (GameEngine.Player.BlobInventory.Contains(Collectables.Key))
                    {
                        _keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                            new Color(0, 255, 0).ToVector3();
                        _keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                            new Color(0, 255, 0).ToVector3();

                        // Change collision tiles to anyone but Blob
                        for (var c = 0; c < _keyLockDoors[b].CollisionTiles.Count; c++)
                        {
                            // Remove the tile from screen and from the key door lock collection
                            _keyLockDoors[b].CollisionTiles[c].CollisionTileType = CollisionTileType.NotBlob;
                        }
                    }
                    else
                    {
                        _keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                            new Color(255, 0, 0).ToVector3();
                        _keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                            new Color(255, 0, 0).ToVector3();

                        // Change collision tiles to everyone
                        for (var c = 0; c < _keyLockDoors[b].CollisionTiles.Count; c++)
                        {
                            // Remove the tile from screen and from the key door lock collection
                            _keyLockDoors[b].CollisionTiles[c].CollisionTileType = CollisionTileType.EveryoneHits;
                        }
                    }

                    _keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseQuantity = 10;
                    _keyDoorForceFieldNoAccess.Effect["Emitter01"].BlendMode = EmitterBlendMode.Add;
                    _keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseQuantity = 10;
                    _keyDoorForceFieldNoAccess.Effect["Emitter05"].BlendMode = EmitterBlendMode.Add;

                    for (var c = 0; c < 12; c++)
                    {
                        _keyDoorForceFieldNoAccess.Update(_keyLockDoors[b].Location.X + 60 + c,
                            _keyLockDoors[b].Location.Y + 48, gameTime);
                    }
                }
            }
        }

        if (ScreenNumber == 477)
        {
            _ollyEyesX = 486;
            _ollyEyesY = 206;

            _ollyEyesX = _ollyEyesX + (GameEngine.Player.BlobXPos - 350) / 44;
            _ollyEyesY = _ollyEyesY + GameEngine.Player.BlobYPos / 20;
        }

        GameEngine.ContentResourceManager.GetAnimationEngine().Update(gameTime);
        GameEngine.ColourAnimationEngine.Update(gameTime);
    }

    public void DrawCoreElements(int xPos, int yPos, GameTime gameTime)
    {
        var objNo = 0;

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName)
            .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName)
                    .DrawSprite(objNo, x * 64 + xPos, y * 64 + yPos, Color.White);
                objNo++;
            }
        }

        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).End();
    }

    public void Draw(Color colour, GameTime gameTime, bool introSequenceRunning = false,
        IntroSequenceStage introSequenceStage = IntroSequenceStage.None)
    {
        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage > -1 &&
                    _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage !=
                    (int)ActionTiles.BlankFiller && // The blank spacer tile
                    _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage !=
                    (int)ActionTiles.ConveyerNasty && // Tile representing conveyer nasties
                    _gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)ActionTiles.TunnelLiftTile &&
                    _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage !=
                    (int)ActionTiles.PreferredCoreLocation) // Tile representing preferred pickup location

                {
                    // Avoid the tunnel tiles
                    if (_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage < 419)
                    {
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .DrawSprite(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, x * _tileWidth,
                                y * _tileHeight, colour);
                        GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                            .End();
                    }

                    // Draw animation portions
                    GameEngine.ContentResourceManager.GetAnimationEngine().Draw(
                        _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, x * _tileWidth, y * _tileHeight,
                        colour, gameTime);
                }
            }
        }

        // Draw Animation engine normal layer
        GameEngine.ColourAnimationEngine.Draw(gameTime, colour.A);

        // Draw Powerups
        if ((!introSequenceRunning || introSequenceStage == IntroSequenceStage.EndPause) &&
            CurrentGameScreenData.PowerUp != null)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PowerUpsName)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PowerUpsName).DrawSprite(
                CurrentGameScreenData.PowerUp.Type, CurrentGameScreenData.PowerUp.XLocation,
                CurrentGameScreenData.PowerUp.YLocation, colour);
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PowerUpsName).End();
        }

        // Draw Pickups
        if ((!introSequenceRunning || introSequenceStage == IntroSequenceStage.EndPause) &&
            CurrentGameScreenData.PickUps != null)
        {
            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
            for (var a = 0; a < CurrentGameScreenData.PickUps.Count; a++)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).DrawSprite(
                    CurrentGameScreenData.PickUps[a].Type, CurrentGameScreenData.PickUps[a].XLocation,
                    CurrentGameScreenData.PickUps[a].YLocation, colour);
            }

            GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.PickupsName).End();
        }

        // Draw Cheops pyramids
        if ((!introSequenceRunning || introSequenceStage == IntroSequenceStage.EndPause) &&
            CurrentGameScreenData.CheopsPyramid != null)
        {
            if (GameEngine.UsingOldGraphics)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal")
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0,
                    CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                    colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").End();
            }
            else
            {
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid")
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").DrawSprite(0,
                    CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                    colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").End();
            }
        }

        // Draw olly background
        if (ScreenNumber == 477)
        {
            if (!GameEngine.UsingOldGraphics)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground")
                    .Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground")
                    .DrawSprite(0, 382, 146, colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground").End();
            }
        }

        // Draw Collision map (Debug only)
        /*
        gameEngine.ShapeDrawer.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

        for (int a = 0; a < detectedCollisionMap.Count; a++)
        {
            CollisionTile colTile = detectedCollisionMap[a];
            gameEngine.ShapeDrawer.FillRectangle(new Rectangle(colTile.XPos, colTile.YPos, collAreaWidth, collAreaHeight), ColourHelper.GetXNA4Colour(255, 0, 0, 100));
        }

        gameEngine.ShapeDrawer.End();
        */
    }

    public void DrawUpperLayer(Color colour, GameTime gameTime)
    {
        // Draw Tunnel Patches

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 396)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(0,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 407)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(0,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 408)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(1,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 409)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(2,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 412)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(3,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 413)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(4,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 414)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(5,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 415)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(6,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 397) // Teleport booths
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(7,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 377 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 378 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 380 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 381 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 385 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 386 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 388 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 389
                        ) // Shadows
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(8,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            (y + 1) * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 399) // Access booth left
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(9,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 400) // Access booth right
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(10,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 420 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 422 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 424 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 426 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 428 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 430 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 432 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 434 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 436 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 438)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(11,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }
                else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 421 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 423 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 425 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 427 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 429 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 431 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 433 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 435 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 437 ||
                         CurrentGameScreenData.ScreenTiles[x, y].TileImage == 439)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName)
                        .DrawSprite(12,
                            x * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteWidth,
                            y * GameEngine.ContentResourceManager
                                .GetSpriteCollection(GameEngine.LowerScreenTilesName).SpriteHeight, colour);
                }

                GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.UpperScreenTilesName).End();

                // Draw Tunnel entrances
                if (CurrentGameScreenData.ScreenTiles[x, y].TileImage > 418)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName)
                        .DrawSprite(_gameScreens[_screenNumber].ScreenTiles[x, y].TileImage, x * _tileWidth,
                            y * _tileHeight, colour);
                    GameEngine.ContentResourceManager.GetSpriteCollection(GameEngine.LowerScreenTilesName).End();
                }
            }
        }

        // Draw olly
        if (ScreenNumber == 477)
        {
            if (GameEngine.UsingOldGraphics)
            {
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal")
                    .Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal")
                    .DrawSprite(0, 384, 100, colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal").End();
            }
            else
            {
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyLower")
                    .Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyLower").DrawSprite(0, 382, 146, colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyLower").End();

                GameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes")
                    .Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes")
                    .DrawSprite(0, _ollyEyesX, _ollyEyesY, colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes").End();

                GameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper")
                    .Begin(BlendState.AlphaBlend, gameTime);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper").DrawSprite(0, 450, 166, colour);
                GameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper").End();
            }
        }

        // Draw Keydoor forcefields
        if (_keyLockDoors != null)
        {
            if (_keyDoorForceFieldNoAccess != null)
            {
                _keyDoorForceFieldNoAccess.Draw();
            }
        }

        // Draw Colour Animated Tiles
        GameEngine.ColourAnimationEngine.DrawUpperLayer(gameTime, colour.A);
    }

    public void DrawShadowing(GameTime gameTime)
    {
        if (!GameEngine.UsingOldGraphics)
        {
            // Status panel shadow
            GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow")
                .Begin(BlendState.AlphaBlend, gameTime);
            GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow")
                .DrawSprite(0, 0, 0, Color.White);
            GameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow").End();
        }
    }

    public List<SecondLayerTile> GetListOfSecondLayerTiles()
    {
        var tilesToReturn = new List<SecondLayerTile>();

        for (var y = 0; y < TilesDown; y++)
        {
            for (var x = 0; x < TilesAcross; x++)
            {
                var tileType = _gameScreens[_screenNumber].ScreenTiles[x, y].TileImage;
                var tiletypeFound = (TileType)tileType;

                if (Enum.IsDefined(typeof(TileType), tiletypeFound))
                {
                    tilesToReturn.Add(new SecondLayerTile(tiletypeFound, new Point(x * 128, y * 96)));
                }
            }
        }

        return tilesToReturn;
    }

    public void Reset()
    {
        // Reset back to screen 8
        ScreenNumber = 8;

        // Setup Powerups and pickups
        var r = new Random((int)DateTime.Now.Ticks);

        // Select 15 pickups to place on random screens
        var powerUpScreens = new List<int>();
        for (var a = 0; a < _pickUpCount; a++)
        {
            // Only place on screens where we can have more than 1 object
            var screenChosen = -1;
            while (screenChosen < 0)
            {
                var temp = r.Next(0, 511);

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

        // Add the access card and key to any screen up to ASCIO
        for (var a = 0; a < _gameScreens.Length; a++)
        {
            if (a != 199) // Core Screen
            {
                // Setup Powerups and pickups
                //
                // Get a list of potential locations
                // Start at line 1 - blob can't get stuff above here....
                _gameScreens[a].PickUps = new List<InteractiveItem>();

                var potentialLocations = GetPotentialLocations(a);
                var placePickup = false;
                for (var b = 0; b < powerUpScreens.Count; b++)
                {
                    // Does the screen we're currently on match a power up screen designated in our list?
                    if (a == powerUpScreens[b])
                    {
                        // Yep so we can place a powerup here...
                        placePickup = true;
                    }
                }

                if (potentialLocations.Count > 0)
                {
                    var powerUpToSelect = r.Next(potentialLocations.Count);
                    _gameScreens[a].PowerUp = new InteractiveItem(potentialLocations[powerUpToSelect].XLocation,
                        potentialLocations[powerUpToSelect].YLocation, r.Next(8));

                    // Take the powerup location out of the list
                    potentialLocations.Remove(potentialLocations[powerUpToSelect]);

                    // Place a pickup
                    if (placePickup)
                    {
                        // Now select another location for our pickup
                        var pickUpToSelect = r.Next(potentialLocations.Count);

                        // Check to see if this screen has a preferred location
                        var preferredLocation = GetPreferredLocation(a);

                        // Place the item in the preferred locateion
                        if (preferredLocation != null)
                        {
                            preferredLocation.Type = r.Next(28);
                            _gameScreens[a].PickUps.Add(preferredLocation);
                            potentialLocations.Remove(potentialLocations[pickUpToSelect]);
                        }
                        else
                        {
                            // Place it in the normal randomly selected place
                            _gameScreens[a].PickUps.Add(new InteractiveItem(
                                potentialLocations[pickUpToSelect].XLocation,
                                potentialLocations[pickUpToSelect].YLocation, r.Next(28)));
                            potentialLocations.Remove(potentialLocations[pickUpToSelect]);
                        }
                    }
                }

                // Add a cheops pyramid
                // Note in the final version we'll randomly decide if we are to place one on a particular screeen...
                // Probably need a system to ensure that we place a minimum amount of these guys....
                if (potentialLocations.Count > 0)
                {
                    var cheopsLocationToSelect = r.Next(potentialLocations.Count);
                    _gameScreens[a].CheopsPyramid = new InteractiveItem(
                        potentialLocations[cheopsLocationToSelect].XLocation,
                        potentialLocations[cheopsLocationToSelect].YLocation, 0);

                    // Setup the access code for the pyramid
                    // Ensure we get a unique code
                    do
                    {
                        _gameScreens[a].CheopsAccessCode = GameEngine.RandomNumberGenerator.Next(10, 100).ToString();
                    } while (!CheckAccessCodeValidity(_gameScreens[a].CheopsAccessCode));
                }

                // Setup access booth codes
                List<Point> accessBooths = null;
                if (GetCoordsOfTiles(399, a) != null && GetCoordsOfTiles(399, a).Count > 0)
                {
                    accessBooths = GetCoordsOfTiles(399, a);
                }
                else if (GetCoordsOfTiles(399, a) != null && GetCoordsOfTiles(399, a).Count > 0)
                {
                    accessBooths = GetCoordsOfTiles(400, a);
                }

                if (accessBooths != null)
                {
                    // Ensure we get a unique code
                    do
                    {
                        _gameScreens[a].AccessCode = GameEngine.RandomNumberGenerator.Next(100, 1000).ToString();
                    } while (!CheckAccessCodeValidity(_gameScreens[a].AccessCode));
                }
            }
        }

        // Remove most of the Cheops pyramids - we'll use the same technique for core elements
        var screensContainingCheopsPyramid = new List<GameScreenData>();
        var screensContainingCoreElements = new List<GameScreenData>();

        for (var a = 0; a < _gameScreens.Length; a++)
        {
            // Only add screens with a cheops pyramid assigned
            if (_gameScreens[a].CheopsPyramid != null)
            {
                screensContainingCheopsPyramid.Add(_gameScreens[a]);
            }

            // Only add screens with a core element assigned
            if (_gameScreens[a].PickUps != null &&
                _gameScreens[a].PickUps.Count > 0)
            {
                for (var b = 0; b < _gameScreens[a].PickUps.Count; b++)
                {
                    if ((Collectables)_gameScreens[a].PickUps[b].Type != Collectables.AccessCard ||
                        (Collectables)_gameScreens[a].PickUps[b].Type != Collectables.Key)
                    {
                        screensContainingCoreElements.Add(_gameScreens[a]);
                        break;
                    }
                }
            }
        }

        // Remove pyramids until we reach the max number we're allowed to have
        while (screensContainingCheopsPyramid.Count > CMaxCheops)
        {
            // Randomly select a cheops from the list of screens
            var temp =
                screensContainingCheopsPyramid[
                    GameEngine.RandomNumberGenerator.Next(0, screensContainingCheopsPyramid.Count)];

            // Remove it
            temp.CheopsPyramid = null;

            // Remove the screen from the list
            screensContainingCheopsPyramid.Remove(temp);
        }

        // Remove core elements until we reach the max number we're allowed to have
        while (screensContainingCoreElements.Count > CMaxCoreElements)
        {
            // Randomly select a pickup from the list of screens
            var temp =
                screensContainingCoreElements[
                    GameEngine.RandomNumberGenerator.Next(0, screensContainingCoreElements.Count)];

            // Remove them
            temp.PickUps = new List<InteractiveItem>();

            // Remove the screen from the list
            screensContainingCoreElements.Remove(temp);
        }

        // Set some of the core elements to match the core
        var tempElements = new List<CoreElement>();

        for (var a = 0; a < GameEngine.Elements.GetActiveElements().Count; a++)
        {
            tempElements.Add(GameEngine.Elements.GetActiveElements()[a]);
        }

        // Make a random number of the core elements on the map match what we need
        var matchNumber = GameEngine.RandomNumberGenerator.Next(2, 6);

        for (var a = 0; a < matchNumber; a++)
        {
            var elementIndex = GameEngine.RandomNumberGenerator.Next(0, tempElements.Count);
            screensContainingCoreElements[a].PickUps[0].Type = tempElements[elementIndex].CurrentElement;
            tempElements.Remove(tempElements[elementIndex]);
        }
    }
}