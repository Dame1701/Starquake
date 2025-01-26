using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMercury.Emitters;
using SharedLibs;
using StarquakeXNA.Particles;

namespace StarquakeXNA
{
    public class GameScreenCollection : GameComponentBase
    {
        private int tilesAcross;

        public int TilesAcross
        {
            get { return tilesAcross; }
            set { tilesAcross = value; }
        }

        private int tilesDown;

        public int TilesDown
        {
            get { return tilesDown; }
            set { tilesDown = value; }
        }

        private int[,,] screenCollisionDataArray;

        private const int c_maxCheops = 40;
        private const int c_maxCoreElements = 18;

        private GameScreenData[] gameScreens;

        public GameScreenData CurrentGameScreenData
        {
            get { return gameScreens[ScreenNumber]; }
        }

        public GameScreenData GetGameScreen(int Number)
        {
            return gameScreens[Number];
        }

        private List<CollisionTile> detectedCollisionMap;
        private IStandardParticleEffect blobsShipSmoke1;

        public IStandardParticleEffect BlobsShipSmoke1
        {
            get { return blobsShipSmoke1; }
        }

        private IStandardParticleEffect blobsShipSmoke2;

        public IStandardParticleEffect BlobsShipSmoke2
        {
            get { return blobsShipSmoke2; }
        }

        private IStandardParticleEffect keyDoorForceFieldNoAccess;

        public int ScreenWidth
        {
            get
            {
                return gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                    .SpriteWidth * tilesAcross;
            }
        }

        public int ScreenHeight
        {
            get
            {
                return gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                    .SpriteHeight * tilesDown;
            }
        }

        private int previousScreenNumber = 0;

        public int PreviousScreenNumber
        {
            get { return previousScreenNumber; }
        }

        private int screenNumber = 0;

        public int ScreenNumber
        {
            get { return screenNumber; }
            set
            {
                previousScreenNumber = screenNumber;
                screenNumber = value;
                BuildScreenCollisionMap(true);
            }
        }

        private List<KeyLockDoor> keyLockDoors = null;
        private int tileWidth;
        private int tileHeight;

        private int collAreaWidth;

        public int CollAreaWidth
        {
            get { return collAreaWidth; }
        }

        private int collAreaHeight;

        public int CollAreaHeight
        {
            get { return collAreaHeight; }
        }

        private int oldXoffset;
        private int oldYoffset;
        private int pickUpCount = 100;


        // olly
        int ollyEyesX;
        int ollyEyesY;

        public GameScreenCollection(GameEngine GameEngineRef, int TilesAcross, int TilesDown, int XOffset, int YOffset,
            int CollAreaWidth, int CollAreaHeight) : base(GameEngineRef)
        {
            oldXoffset = XOffset;
            oldYoffset = YOffset;

            tilesAcross = TilesAcross;
            tilesDown = TilesDown;

            collAreaWidth = CollAreaWidth;
            collAreaHeight = CollAreaHeight;

            detectedCollisionMap = new List<CollisionTile>();
        }

        public new void LoadContent()
        {
            // Load screen data
            gameScreens = (GameScreenData[])gameEngine.ContentResourceManager.GetDataResource("GameScreens");
            screenCollisionDataArray =
                (int[,,])gameEngine.ContentResourceManager.GetDataResource("ScreenCollisionData");

            blobsShipSmoke1 =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                    enParticleEffectType.Continual, null) as IStandardParticleEffect;
            blobsShipSmoke2 =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume2",
                    enParticleEffectType.Continual, null) as IStandardParticleEffect;

            keyDoorForceFieldNoAccess =
                gameEngine.ContentResourceManager.GetParticleEffectInstance("ForceField",
                    enParticleEffectType.Continual, null) as IStandardParticleEffect;

            // Make this better in the future...?
            tileWidth = 128;
            tileHeight = 96;

            // Save out the data to MessagePack
            //SaveData();
        }

        public override void Initialise(object Data)
        {
            // Setup Collision map, we're using 8 x 8 blocks for each collision area for improved accuracy
            BuildScreenCollisionMap(true);
        }

        public int GetPercentageOfScreensVisited()
        {
            int screensVisited = 0;

            for (int a = 0; a < gameScreens.Length; a++)
            {
                if (gameScreens[a].ScreenVisited)
                {
                    screensVisited++;
                }
            }

            return (int)Math.Ceiling((double)((double)screensVisited / (double)gameScreens.Length) * 100);
        }

        private bool CheckAccessCodeValidity(string AccessCode)
        {
            bool unique = true;

            for (int a = 0; a < AccessCode.Length; a++)
            {
                for (int b = 0; b < AccessCode.Length; b++)
                {
                    if (AccessCode.Substring(b, 1) == AccessCode.Substring(a, 1) &&
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
                        bool addLocation = true;

                        // Now check the collision map for this tile
                        for (int y1 = 0; y1 < 3; y1++)
                        {
                            for (int x1 = 0; x1 < 4; x1++)
                            {
                                for (int a = 0; a < gameScreens[ScreenNumber].CollisionMap.Count; a++)
                                {
                                    if (gameScreens[ScreenNumber].CollisionMap[a].XPos ==
                                        (x * tileWidth) + (x1 * collAreaWidth) &&
                                        gameScreens[ScreenNumber].CollisionMap[a].YPos ==
                                        (y * tileHeight) + (y1 * collAreaHeight))
                                    {
                                        if (gameScreens[ScreenNumber].CollisionMap[a].CollisionTileType ==
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

        private InteractiveItem GetPreferredLocation(int ScreenNumber)
        {
            for (int y = 1; y < gameScreens[ScreenNumber].ScreenTiles.GetLength(1); y++)
            {
                for (int x = 0; x < gameScreens[ScreenNumber].ScreenTiles.GetLength(0); x++)
                {
                    if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 392)
                    {
                        return new InteractiveItem(
                            ((x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth) + 32),
                            ((y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight) + 32), 0);
                    }
                }
            }

            return null;
        }

        private List<InteractiveItem> GetPotentialLocations(int ScreenNumber)
        {
            List<InteractiveItem> toReturn = new List<InteractiveItem>();

            for (int y = 1; y < gameScreens[ScreenNumber].ScreenTiles.GetLength(1); y++)
            {
                for (int x = 0; x < gameScreens[ScreenNumber].ScreenTiles.GetLength(0); x++)
                {
                    if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage >= 0)
                    {
                        // Valid tile to place above...?
                        if (
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.ShuttlePlatform &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.RemovedShuttlePlatform &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.ElectricityNode &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.ShuttleTag1 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.ShuttleTag2 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.ShuttleTag3 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.Spike1 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.Spike2 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.Spike3 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.ElectricityGate &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.BlankFiller &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.ConveyerNasty &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.BreakableBridges &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != 120 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != 121 &&
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage !=
                            (int)enActionTiles.PreferredCoreLocation)
                        {
                            // Check for powerups in the way - useful for pickup sweep
                            bool powerUpInWay = false;
                            if (gameScreens[screenNumber].PowerUp != null)
                            {
                                if (gameScreens[screenNumber].PowerUp.XLocation == x + 32 &&
                                    gameScreens[screenNumber].PowerUp.YLocation == y + 32)
                                {
                                    powerUpInWay = true;
                                }
                            }

                            if (powerUpInWay == false)
                            {
                                // Anything above it....?
                                if (gameScreens[ScreenNumber].ScreenTiles[x, y - 1].TileImage == -1)
                                {
                                    // now lets check the collision map and only place on tiles with a collision blocks below the powerup
                                    bool locationFound = false;

                                    if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage >= 0)
                                    {
                                        if (screenCollisionDataArray[
                                                gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage, 1, 0] == 1 &&
                                            screenCollisionDataArray[
                                                gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage, 2, 0] == 1)
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
                                            ((x * gameEngine.ContentResourceManager
                                                 .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth) +
                                             32),
                                            ((y * gameEngine.ContentResourceManager
                                                 .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight) -
                                             64), 0));
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
            var gameScreensData = MessagePackSerializer.Serialize(gameScreens);
            var collisionData = MessagePackSerializer.Serialize(screenCollisionDataArray);

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

        public void UpdateCollisionMap(bool ScreenChanged)
        {
            BuildScreenCollisionMap(ScreenChanged);
        }

        private void BuildScreenCollisionMap(bool ScreenChanged)
        {
            // Grab any collision tiles that are special cases i.e. Jump Platforms etc.
            List<CollisionTile> collisionsToAppend = null;

            if (ScreenChanged)
            {
                keyLockDoors = null;
            }

            if (!ScreenChanged)
            {
                if (collisionsToAppend == null)
                {
                    collisionsToAppend = new List<CollisionTile>();
                }

                for (int a = 0; a < gameScreens[ScreenNumber].CollisionMap.Count; a++)
                {
                    if (gameScreens[ScreenNumber].CollisionMap[a].TileImage == (int)CollisionObjectType.GameObject)
                    {
                        // Remove any game object collision tiles becuase we are about to update them
                        gameScreens[ScreenNumber].CollisionMap.Remove(gameScreens[ScreenNumber].CollisionMap[a]);
                    }
                }
            }

            // Create new collision map
            gameScreens[ScreenNumber].CollisionMap = new List<CollisionTile>();

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage > -1)
                    {
                        for (int y1 = 0; y1 < 3; y1++)
                        {
                            for (int x1 = 0; x1 < 4; x1++)
                            {
                                if (screenCollisionDataArray[gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage, x1,
                                        y1] == 1)
                                {
                                    gameScreens[ScreenNumber].CollisionMap.Add(
                                        new CollisionTile(
                                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage,
                                            ((x * gameEngine.ContentResourceManager
                                                 .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth) +
                                             (x1 * collAreaWidth)),
                                            ((y * gameEngine.ContentResourceManager
                                                 .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight) +
                                             (y1 * collAreaHeight)),
                                            CollisionTileType.EveryoneHits));
                                }
                            }
                        }

                        int tileX = 0;
                        int tileY = 0;

                        // Add collision map for inside of teleporters, access booths and tunnel lifts
                        if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 397 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 399 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 400 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 407 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 409 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 412 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 413 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 414 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 415)
                        {
                            // Cover the whole tile since it doesn't matter - nasties can't touch any of it anyway
                            tileX = x * 128;
                            tileY = y * 96;

                            for (int a = 0; a < 3; a++)
                            {
                                for (int b = 0; b < 4; b++)
                                {
                                    gameScreens[ScreenNumber].CollisionMap.Add(
                                        new CollisionTile(
                                            (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                            (b * 32) + tileX,
                                            (a * 32) + tileY,
                                            CollisionTileType.NotBlob));
                                }
                            }
                        }

                        // Add nasty collision map for spiky tiles
                        tileX = x * 128;
                        tileY = y * 96;

                        // Central spiky tiles and Broken bottle spiky tiles
                        if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 405 ||
                            gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 416)
                        {
                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    32 + tileX,
                                    32 + tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    32 + tileX,
                                    64 + tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    64 + tileX,
                                    32 + tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    64 + tileX,
                                    64 + tileY,
                                    CollisionTileType.NotBlob));
                        }

                        // Right side mounted spikey tiles
                        if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == 404)
                        {
                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    64 + tileX,
                                    tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    96 + tileX,
                                    tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    64 + tileX,
                                    32 + tileY,
                                    CollisionTileType.NotBlob));

                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    96 + tileX,
                                    32 + tileY,
                                    CollisionTileType.NotBlob));
                        }

                        // Add collision map for the secret passages
                        tileX = x * 128;
                        tileY = y * 96;

                        for (int a = 0; a < 10; a++)
                        {
                            int tunnelTile = a + 420;
                            if (tunnelTile % 2 == 0)
                            {
                                // Left passages
                                if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == tunnelTile)
                                {
                                    for (int b = 0; b < 3; b++)
                                    {
                                        gameScreens[ScreenNumber].CollisionMap.Add(
                                            new CollisionTile(
                                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                                tileX,
                                                (b * 32) + tileY,
                                                CollisionTileType.NotBlob));
                                    }
                                }
                            }
                            else
                            {
                                // Right passages
                                if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == tunnelTile)
                                {
                                    for (int b = 0; b < 3; b++)
                                    {
                                        gameScreens[ScreenNumber].CollisionMap.Add(
                                            new CollisionTile(
                                                (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                                tileX + 96,
                                                (b * 32) + tileY,
                                                CollisionTileType.NotBlob));
                                    }
                                }
                            }
                        }
                    }
                }
            }


            // Add secondary maps for Cheops pyramids etc.
            if (gameScreens[ScreenNumber].PowerUp != null)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        if (ScreenChanged)
                        {
                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    gameScreens[ScreenNumber].PowerUp.XLocation + (x * 32),
                                    gameScreens[ScreenNumber].PowerUp.YLocation + (y * 32),
                                    CollisionTileType.NotBlob));
                        }
                        else
                        {
                            collisionsToAppend.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    gameScreens[ScreenNumber].PowerUp.XLocation + (x * 32),
                                    gameScreens[ScreenNumber].PowerUp.YLocation + (y * 32),
                                    CollisionTileType.NotBlob));
                        }
                    }
                }
            }

            if (gameScreens[ScreenNumber].PickUps != null)
            {
                for (int a = 0; a < gameScreens[ScreenNumber].PickUps.Count; a++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int x = 0; x < 2; x++)
                        {
                            if (ScreenChanged)
                            {
                                gameScreens[ScreenNumber].CollisionMap.Add(
                                    new CollisionTile(
                                        (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                        gameScreens[ScreenNumber].PickUps[a].XLocation + (x * 32),
                                        gameScreens[ScreenNumber].PickUps[a].YLocation + (y * 32),
                                        CollisionTileType.NotBlob));
                            }
                            else
                            {
                                collisionsToAppend.Add(
                                    new CollisionTile(
                                        (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                        gameScreens[ScreenNumber].PickUps[a].XLocation + (x * 32),
                                        gameScreens[ScreenNumber].PickUps[a].YLocation + (y * 32),
                                        CollisionTileType.NotBlob));
                            }
                        }
                    }
                }
            }

            if (gameScreens[ScreenNumber].CheopsPyramid != null)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        if (ScreenChanged)
                        {
                            gameScreens[ScreenNumber].CollisionMap.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    gameScreens[ScreenNumber].CheopsPyramid.XLocation + (x * 32),
                                    gameScreens[ScreenNumber].CheopsPyramid.YLocation + (y * 32),
                                    CollisionTileType.NotBlob));
                        }
                        else
                        {
                            collisionsToAppend.Add(
                                new CollisionTile(
                                    (int)CollisionObjectType.GameObject, // Random image that's meaningless
                                    gameScreens[ScreenNumber].CheopsPyramid.XLocation + (x * 32),
                                    gameScreens[ScreenNumber].CheopsPyramid.YLocation + (y * 32),
                                    CollisionTileType.NotBlob));
                        }
                    }
                }
            }

            // Check to see if we need to append any special cases
            if (collisionsToAppend != null)
            {
                // Add them to the tile map
                gameScreens[ScreenNumber].CollisionMap.AddRange(collisionsToAppend);
            }
        }

        public void ChangeShuttlePlatformState(bool PadRemoved)
        {
            for (int a = 0; a < gameScreens.Length; a++)
            {
                for (int y = 0; y < tilesDown; y++)
                {
                    for (int x = 0; x < tilesAcross; x++)
                    {
                        if (PadRemoved)
                        {
                            if (gameScreens[a].ScreenTiles[x, y].TileImage == (int)enActionTiles.ShuttlePlatform)
                            {
                                gameScreens[a].ScreenTiles[x, y].TileImage = (int)enActionTiles.RemovedShuttlePlatform;

                                // Trigger Colour animation
                                gameEngine.ColourAnimationEngine.Start(401, -1);
                                gameEngine.ColourAnimationEngine.Start(417, -1);
                                gameEngine.ColourAnimationEngine.Start(418, -1);
                            }
                        }
                        else
                        {
                            if (gameScreens[a].ScreenTiles[x, y].TileImage == (int)enActionTiles.RemovedShuttlePlatform)
                            {
                                gameScreens[a].ScreenTiles[x, y].TileImage = (int)enActionTiles.ShuttlePlatform;

                                // Cancel Colour animation
                                gameEngine.ColourAnimationEngine.Stop(401);
                                gameEngine.ColourAnimationEngine.Stop(417);
                                gameEngine.ColourAnimationEngine.Stop(418);
                            }
                        }
                    }
                }
            }
        }

        public int CheckRectangles(
            int PlayerXPos,
            int PlayerYPos,
            int PlayerWidth,
            int PlayerHeight,
            bool IsBlob)
        {
            bool Collide = false;

            // Cycle through the collision map
            for (int a = 0; a < gameScreens[screenNumber].CollisionMap.Count; a++)
            {
                Collide =
                    CollisionDetection.Check(
                        new Rectangle(PlayerXPos, PlayerYPos, PlayerWidth, PlayerHeight),
                        new Rectangle(gameScreens[screenNumber].CollisionMap[a].XPos,
                            gameScreens[screenNumber].CollisionMap[a].YPos, collAreaWidth, collAreaHeight));

                if (Collide)
                {
                    if (IsBlob && gameScreens[screenNumber].CollisionMap[a].CollisionTileType ==
                        CollisionTileType.NotBlob)
                    {
                        Collide = false;
                    }
                    else
                    {
                        return gameScreens[screenNumber].CollisionMap[a].TileImage;
                    }
                }
            }

            return -1;
        }

        public int CheckCollision(
            enDirection PlayerDirection,
            int PlayerXPos,
            int PlayerYPos,
            int PlayerWidth,
            int PlayerHeight,
            int Velocity,
            bool IsBlob)
        {
            int CheckMovementAmount = Velocity;
            //int CheckMovementAmount = Velocity;
            detectedCollisionMap.Clear();

            // Cycle through the collision map
            for (int a = 0; a < gameScreens[screenNumber].CollisionMap.Count; a++)
            {
                CollisionTile colTile = gameScreens[screenNumber].CollisionMap[a];
                detectedCollisionMap.Add(colTile);

                int CheckMovementAmountTemp =
                    CollisionDetection.CheckDirection(
                        PlayerDirection,
                        Velocity,
                        PlayerXPos,
                        PlayerYPos,
                        PlayerWidth,
                        PlayerHeight,
                        colTile.XPos,
                        colTile.YPos,
                        collAreaWidth,
                        collAreaHeight
                    );


                if (colTile.CollisionTileType == CollisionTileType.NotBlob &&
                    IsBlob)
                {
                    CheckMovementAmountTemp = Velocity;
                }

                if (colTile.TileImage > 999 && !IsBlob)
                {
                    if (CollisionDetection.Check(
                            new Rectangle(PlayerXPos, PlayerYPos, PlayerWidth, PlayerHeight),
                            new Rectangle(colTile.XPos, colTile.YPos, 32, 32)))
                    {
                        if ((CollisionObjectType)colTile.TileImage == CollisionObjectType.JumpPlatform ||
                            (CollisionObjectType)colTile.TileImage == CollisionObjectType.CoreElement ||
                            (CollisionObjectType)colTile.TileImage == CollisionObjectType.GameObject)
                        {
                            // Now see if it's actually embedded
                            // And if it is, let it out
                            CheckMovementAmountTemp = Velocity;
                        }
                    }
                }

                if (CheckMovementAmountTemp <= CheckMovementAmount)
                {
                    CheckMovementAmount = CheckMovementAmountTemp;
                }
            }

            // Check screen bounds, but only for nasties
            if (!IsBlob)
            {
                if (PlayerDirection == enDirection.Up)
                {
                    int BoundaryDistance = (PlayerYPos - CheckMovementAmount);
                    if (BoundaryDistance < 0)
                    {
                        // if we're blob we can change screens!
                        CheckMovementAmount = CheckMovementAmount + BoundaryDistance;
                    }
                }
                else if (PlayerDirection == enDirection.Down)
                {
                    int BoundaryDistance = (PlayerYPos + PlayerHeight + CheckMovementAmount);
                    if (BoundaryDistance > ScreenHeight)
                    {
                        // if we're blob we can change screens!
                        CheckMovementAmount = CheckMovementAmount - (BoundaryDistance - ScreenHeight);
                    }
                }
                else if (PlayerDirection == enDirection.Left)
                {
                    int BoundaryDistance = (PlayerXPos - CheckMovementAmount);
                    if (BoundaryDistance < 0)
                    {
                        // if we're blob we can change screens!
                        CheckMovementAmount = CheckMovementAmount + BoundaryDistance;
                    }
                }
                else if (PlayerDirection == enDirection.Right)
                {
                    int BoundaryDistance = (PlayerXPos + PlayerWidth + CheckMovementAmount);
                    if (BoundaryDistance > ScreenWidth)
                    {
                        // if we're blob we can change screens!
                        CheckMovementAmount = CheckMovementAmount - (BoundaryDistance - ScreenWidth);
                    }
                }
            }

            return CheckMovementAmount;
        }

        public Point ConvertToScreenCoords(int tileMapXPos, int tileMapYPos)
        {
            return new Point(
                (tileMapXPos * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                    .SpriteWidth),
                (tileMapYPos * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                    .SpriteHeight));
        }

        public List<Point> GetCoordsOfTiles(int TileType)
        {
            List<Point> ReturnedTiles = null;

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[screenNumber].ScreenTiles[x, y].TileImage == TileType)
                    {
                        if (ReturnedTiles == null)
                        {
                            ReturnedTiles = new List<Point>();
                        }

                        ReturnedTiles.Add(new Point(
                            (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth),
                            (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight)));
                    }
                }
            }

            return ReturnedTiles;
        }

        public Point[] GetCoordsOfTiles(int[] TileTypes)
        {
            List<Point> ReturnedTiles = null;

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (TileTypes.Contains(gameScreens[screenNumber].ScreenTiles[x, y].TileImage))
                    {
                        if (ReturnedTiles == null)
                        {
                            ReturnedTiles = new List<Point>();
                        }

                        ReturnedTiles.Add(new Point(
                            (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth),
                            (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight)));
                    }
                }
            }

            if (ReturnedTiles != null && ReturnedTiles.Count > 0)
            {
                return ReturnedTiles.OrderByDescending(rt => rt.Y).ToArray();
            }

            return null;
        }

        public List<KeyLockDoor> GetKeyLockDoors()
        {
            List<KeyLockDoor> ReturnedTiles = null;

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[screenNumber].ScreenTiles[x, y].TileImage == 419)
                    {
                        if (ReturnedTiles == null)
                        {
                            ReturnedTiles = new List<KeyLockDoor>();
                        }

                        ReturnedTiles.Add(new KeyLockDoor()
                        {
                            Location = new Point(
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight))
                        });
                    }
                }
            }

            return ReturnedTiles;
        }

        public List<Point> GetCoordsOfTiles(int TileType, int ScreenNumber)
        {
            List<Point> ReturnedTiles = null;

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage == TileType)
                    {
                        if (ReturnedTiles == null)
                        {
                            ReturnedTiles = new List<Point>();
                        }

                        ReturnedTiles.Add(new Point(
                            (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth),
                            (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight)));
                    }
                }
            }

            return ReturnedTiles;
        }

        public TileDetected CheckTileByObject(
            enDirection DirectionToCheck,
            int PlayerXPos,
            int PlayerYPos,
            int PlayerWidth,
            int PlayerHeight,
            int Velocity)
        {
            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[screenNumber].ScreenTiles[x, y].TileImage > -1)
                    {
                        Rectangle tileRect = new Rectangle(
                            (x * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth),
                            (y * gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight),
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteWidth,
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .SpriteHeight);

                        int Xpos = 0;
                        int Ypos = 0;

                        if (DirectionToCheck == enDirection.Up)
                        {
                            if (CollisionDetection.Check(
                                    new Rectangle(PlayerXPos, PlayerYPos - Velocity, PlayerWidth, PlayerHeight),
                                    tileRect))
                            {
                                Xpos = PlayerXPos - (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth);
                                Ypos = (PlayerYPos + PlayerHeight) - (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight);

                                return new TileDetected(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, Xpos,
                                    Ypos, x, y);
                            }
                        }
                        else if (DirectionToCheck == enDirection.Down)
                        {
                            if (CollisionDetection.Check(
                                    new Rectangle(PlayerXPos, PlayerYPos + PlayerHeight, PlayerWidth, Velocity),
                                    tileRect))
                            {
                                Xpos = PlayerXPos - (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth);
                                Ypos = (PlayerYPos + PlayerHeight) - (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight);

                                return new TileDetected(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, Xpos,
                                    Ypos, x, y);
                            }
                        }
                        else if (DirectionToCheck == enDirection.Left)
                        {
                            if (CollisionDetection.Check(
                                    new Rectangle(PlayerXPos - Velocity, PlayerYPos, PlayerWidth, PlayerHeight),
                                    tileRect))
                            {
                                Xpos = (PlayerXPos) - (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth);
                                Ypos = PlayerYPos - (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight);

                                return new TileDetected(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, Xpos,
                                    Ypos, x, y);
                            }
                        }
                        else if (DirectionToCheck == enDirection.Right)
                        {
                            if (CollisionDetection.Check(
                                    new Rectangle(PlayerXPos, PlayerYPos, PlayerWidth + Velocity, PlayerHeight),
                                    tileRect))
                            {
                                Xpos = (PlayerXPos) - (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth);
                                Ypos = PlayerYPos - (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight);

                                return new TileDetected(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, Xpos,
                                    Ypos, x, y);
                            }
                        }
                        else if (DirectionToCheck == enDirection.None)
                        {
                            if (CollisionDetection.Check(
                                    new Rectangle(PlayerXPos, PlayerYPos, PlayerWidth, PlayerHeight),
                                    tileRect))
                            {
                                Xpos = (PlayerXPos) - (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth);
                                Ypos = PlayerYPos - (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight);

                                return new TileDetected(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, Xpos,
                                    Ypos, x, y);
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
            if (keyLockDoors == null)
            {
                keyLockDoors = GetKeyLockDoors();
            }

            if (keyLockDoors != null)
            {
                if (keyDoorForceFieldNoAccess != null)
                {
                    keyDoorForceFieldNoAccess.Start();

                    for (int b = 0; b < keyLockDoors.Count; b++)
                    {
                        // Create the collision map
                        if (keyLockDoors[b].CollisionTiles.Count == 0) // If the collision map doesn't exist, create it
                        {
                            CollisionTile keyLockCollisionTile = null;

                            for (int c = 0; c < 3; c++)
                            {
                                keyLockCollisionTile =
                                    new CollisionTile(
                                        (int)CollisionObjectType.KeyAccessDoor, // Random image that's meaningless
                                        keyLockDoors[b].Location.X + 48,
                                        keyLockDoors[b].Location.Y + (c * 32),
                                        CollisionTileType.None);

                                // Add the tile to the keylock door so we can remove it later
                                keyLockDoors[b].CollisionTiles.Add(keyLockCollisionTile);

                                // Add the tile to the collision map
                                gameScreens[ScreenNumber].CollisionMap.Add(keyLockCollisionTile);
                            }
                        }

                        keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                            new Color(100, 100, 100).ToVector3();
                        keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseQuantity = 100;
                        keyDoorForceFieldNoAccess.Effect["Emitter01"].BlendMode = EmitterBlendMode.Alpha;

                        keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                            new Color(100, 100, 100).ToVector3();
                        keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseQuantity = 100;
                        keyDoorForceFieldNoAccess.Effect["Emitter05"].BlendMode = EmitterBlendMode.Alpha;

                        for (int c = 0; c < 4; c++)
                        {
                            keyDoorForceFieldNoAccess.Update(keyLockDoors[b].Location.X + 52 + c,
                                keyLockDoors[b].Location.Y + 48, gameTime);
                        }

                        for (int c = 0; c < 4; c++)
                        {
                            keyDoorForceFieldNoAccess.Update(keyLockDoors[b].Location.X + 76 + c,
                                keyLockDoors[b].Location.Y + 48, gameTime);
                        }

                        // Change colour of the gate depending on whether blob has the access key or not
                        if (gameEngine.Player.BlobInventory.Contains(enCollectables.Key))
                        {
                            keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                                new Color(0, 255, 0).ToVector3();
                            keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                                new Color(0, 255, 0).ToVector3();

                            // Change collision tiles to anyone but Blob
                            for (int c = 0; c < keyLockDoors[b].CollisionTiles.Count; c++)
                            {
                                // Remove the tile from screen and from the key door lock collection
                                keyLockDoors[b].CollisionTiles[c].CollisionTileType = CollisionTileType.NotBlob;
                            }
                        }
                        else
                        {
                            keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseColour =
                                new Color(255, 0, 0).ToVector3();
                            keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseColour =
                                new Color(255, 0, 0).ToVector3();

                            // Change collision tiles to everyone
                            for (int c = 0; c < keyLockDoors[b].CollisionTiles.Count; c++)
                            {
                                // Remove the tile from screen and from the key door lock collection
                                keyLockDoors[b].CollisionTiles[c].CollisionTileType = CollisionTileType.EveryoneHits;
                            }
                        }

                        keyDoorForceFieldNoAccess.Effect["Emitter01"].ReleaseQuantity = 10;
                        keyDoorForceFieldNoAccess.Effect["Emitter01"].BlendMode = EmitterBlendMode.Add;
                        keyDoorForceFieldNoAccess.Effect["Emitter05"].ReleaseQuantity = 10;
                        keyDoorForceFieldNoAccess.Effect["Emitter05"].BlendMode = EmitterBlendMode.Add;

                        for (int c = 0; c < 12; c++)
                        {
                            keyDoorForceFieldNoAccess.Update(keyLockDoors[b].Location.X + 60 + c,
                                keyLockDoors[b].Location.Y + 48, gameTime);
                        }
                    }
                }
            }

            if (ScreenNumber == 477)
            {
                ollyEyesX = 486;
                ollyEyesY = 206;

                ollyEyesX = ollyEyesX + ((gameEngine.Player.BlobXPos - 350) / 44);
                ollyEyesY = ollyEyesY + (gameEngine.Player.BlobYPos / 20);
            }

            gameEngine.ContentResourceManager.GetAnimationEngine().Update(gameTime);
            gameEngine.ColourAnimationEngine.Update(gameTime);
        }

        public void DrawCoreElements(int XPos, int YPos, GameTime gameTime)
        {
            int objNo = 0;

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName)
                .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName)
                        .DrawSprite(objNo, (x * 64) + XPos, (y * 64) + YPos, Color.White);
                    objNo++;
                }
            }

            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
        }

        public void Draw(Color Colour, GameTime gameTime, bool IntroSequenceRunning = false,
            enIntroSequenceStage IntroSequenceStage = enIntroSequenceStage.None)
        {
            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    if (gameScreens[screenNumber].ScreenTiles[x, y].TileImage > -1 &&
                        gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)enActionTiles.BlankFiller && // The blank spacer tile
                        gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)enActionTiles.ConveyerNasty && // Tile representing conveyer nasties
                        gameScreens[ScreenNumber].ScreenTiles[x, y].TileImage != (int)enActionTiles.TunnelLiftTile &&
                        gameScreens[screenNumber].ScreenTiles[x, y].TileImage !=
                        (int)enActionTiles.PreferredCoreLocation) // Tile representing preferred pickup location

                    {
                        // Avoid the tunnel tiles
                        if (gameScreens[screenNumber].ScreenTiles[x, y].TileImage < 419)
                        {
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .DrawSprite(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, (x * tileWidth),
                                    (y * tileHeight), Colour);
                            gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                                .End();
                        }

                        // Draw animation portions
                        gameEngine.ContentResourceManager.GetAnimationEngine().Draw(
                            gameScreens[screenNumber].ScreenTiles[x, y].TileImage, (x * tileWidth), (y * tileHeight),
                            Colour, gameTime);
                    }
                }
            }

            // Draw Animation engine normal layer
            gameEngine.ColourAnimationEngine.Draw(gameTime, Colour.A);

            // Draw Powerups
            if ((!IntroSequenceRunning || IntroSequenceStage == enIntroSequenceStage.EndPause) &&
                CurrentGameScreenData.PowerUp != null)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PowerUpsName)
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PowerUpsName).DrawSprite(
                    CurrentGameScreenData.PowerUp.Type, CurrentGameScreenData.PowerUp.XLocation,
                    CurrentGameScreenData.PowerUp.YLocation, Colour);
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PowerUpsName).End();
            }

            // Draw Pickups
            if ((!IntroSequenceRunning || IntroSequenceStage == enIntroSequenceStage.EndPause) &&
                CurrentGameScreenData.PickUps != null)
            {
                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName)
                    .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                for (int a = 0; a < CurrentGameScreenData.PickUps.Count; a++)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).DrawSprite(
                        CurrentGameScreenData.PickUps[a].Type, CurrentGameScreenData.PickUps[a].XLocation,
                        CurrentGameScreenData.PickUps[a].YLocation, Colour);
                }

                gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.PickupsName).End();
            }

            // Draw Cheops pyramids
            if ((!IntroSequenceRunning || IntroSequenceStage == enIntroSequenceStage.EndPause) &&
                CurrentGameScreenData.CheopsPyramid != null)
            {
                if (gameEngine.UsingOldGraphics)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").DrawSprite(0,
                        CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                        Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramidOriginal").End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid")
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").DrawSprite(0,
                        CurrentGameScreenData.CheopsPyramid.XLocation, CurrentGameScreenData.CheopsPyramid.YLocation,
                        Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("CheopsPyramid").End();
                }
            }

            // Draw olly background
            if (ScreenNumber == 477)
            {
                if (!gameEngine.UsingOldGraphics)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground")
                        .Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground")
                        .DrawSprite(0, 382, 146, Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyBackground").End();
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

        public void DrawUpperLayer(Color Colour, GameTime gameTime)
        {
            // Draw Tunnel Patches

            for (int y = 0; y < TilesDown; y++)
            {
                for (int x = 0; x < TilesAcross; x++)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                        .Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                    if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 396)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(0,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 407)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(0,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 408)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(1,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 409)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(2,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 412)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(3,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 413)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(4,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 414)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(5,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 415)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(6,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 397) // Teleport booths
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(7,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
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
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(8,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                ((y + 1) * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 399) // Access booth left
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(9,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }
                    else if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == 400) // Access booth right
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(10,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
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
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(11,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
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
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName)
                            .DrawSprite(12,
                                (x * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteWidth),
                                (y * gameEngine.ContentResourceManager
                                    .GetSpriteCollection(gameEngine.LowerScreenTilesName).SpriteHeight), Colour);
                    }

                    gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.UpperScreenTilesName).End();

                    // Draw Tunnel entrances
                    if (CurrentGameScreenData.ScreenTiles[x, y].TileImage > 418)
                    {
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                            .Begin(BlendState.AlphaBlend, SpriteSortMode.Immediate, gameTime);
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName)
                            .DrawSprite(gameScreens[screenNumber].ScreenTiles[x, y].TileImage, (x * tileWidth),
                                (y * tileHeight), Colour);
                        gameEngine.ContentResourceManager.GetSpriteCollection(gameEngine.LowerScreenTilesName).End();
                    }
                }
            }

            // Draw olly
            if (ScreenNumber == 477)
            {
                if (gameEngine.UsingOldGraphics)
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal")
                        .Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal")
                        .DrawSprite(0, 384, 100, Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyOriginal").End();
                }
                else
                {
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyLower")
                        .Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyLower").DrawSprite(0, 382, 146, Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyLower").End();

                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes")
                        .Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes")
                        .DrawSprite(0, ollyEyesX, ollyEyesY, Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyEyes").End();

                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper")
                        .Begin(BlendState.AlphaBlend, gameTime);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper").DrawSprite(0, 450, 166, Colour);
                    gameEngine.ContentResourceManager.GetSpriteCollection("OllyUpper").End();
                }
            }

            // Draw Keydoor forcefields
            if (keyLockDoors != null)
            {
                if (keyDoorForceFieldNoAccess != null)
                {
                    keyDoorForceFieldNoAccess.Draw();
                }
            }

            // Draw Colour Animated Tiles
            gameEngine.ColourAnimationEngine.DrawUpperLayer(gameTime, Colour.A);
        }

        public void DrawShadowing(GameTime gameTime)
        {
            if (!gameEngine.UsingOldGraphics)
            {
                // Status panel shadow
                gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow")
                    .Begin(BlendState.AlphaBlend, gameTime);
                gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow")
                    .DrawSprite(0, 0, 0, Color.White);
                gameEngine.ContentResourceManager.GetSpriteCollection("StatusPanelShadow").End();
            }
        }

        public List<SecondLayerTile> GetListOfSecondLayerTiles()
        {
            List<SecondLayerTile> tilesToReturn = new List<SecondLayerTile>();

            for (int y = 0; y < tilesDown; y++)
            {
                for (int x = 0; x < tilesAcross; x++)
                {
                    int tileType = gameScreens[screenNumber].ScreenTiles[x, y].TileImage;
                    enTileType tiletypeFound = (enTileType)tileType;

                    if (Enum.IsDefined(typeof(enTileType), tiletypeFound))
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
            Random r = new Random((int)DateTime.Now.Ticks);

            // Select 15 pickups to place on random screens
            List<int> powerUpScreens = new List<int>();
            for (int a = 0; a < pickUpCount; a++)
            {
                // Only place on screens where we can have more than 1 object
                int screenChosen = -1;
                while (screenChosen < 0)
                {
                    int temp = r.Next(0, 511);

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
            for (int a = 0; a < gameScreens.Length; a++)
            {
                if (a != 199) // Core Screen
                {
                    // Setup Powerups and pickups
                    //
                    // Get a list of potential locations
                    // Start at line 1 - blob can't get stuff above here....
                    gameScreens[a].PickUps = new List<InteractiveItem>();

                    List<InteractiveItem> PotentialLocations = GetPotentialLocations(a);
                    bool placePickup = false;
                    for (int b = 0; b < powerUpScreens.Count; b++)
                    {
                        // Does the screen we're currently on match a power up screen designated in our list?
                        if (a == powerUpScreens[b])
                        {
                            // Yep so we can place a powerup here...
                            placePickup = true;
                        }
                    }

                    if (PotentialLocations.Count > 0)
                    {
                        int powerUpToSelect = r.Next(PotentialLocations.Count);
                        gameScreens[a].PowerUp = new InteractiveItem(PotentialLocations[powerUpToSelect].XLocation,
                            PotentialLocations[powerUpToSelect].YLocation, r.Next(8));

                        // Take the powerup location out of the list
                        PotentialLocations.Remove(PotentialLocations[powerUpToSelect]);

                        // Place a pickup
                        if (placePickup)
                        {
                            // Now select another location for our pickup
                            int pickUpToSelect = r.Next(PotentialLocations.Count);

                            // Check to see if this screen has a preferred location
                            InteractiveItem preferredLocation = GetPreferredLocation(a);

                            // Place the item in the preferred locateion
                            if (preferredLocation != null)
                            {
                                preferredLocation.Type = r.Next(28);
                                gameScreens[a].PickUps.Add(preferredLocation);
                                PotentialLocations.Remove(PotentialLocations[pickUpToSelect]);
                            }
                            else
                            {
                                // Place it in the normal randomly selected place
                                gameScreens[a].PickUps.Add(new InteractiveItem(
                                    PotentialLocations[pickUpToSelect].XLocation,
                                    PotentialLocations[pickUpToSelect].YLocation, r.Next(28)));
                                PotentialLocations.Remove(PotentialLocations[pickUpToSelect]);
                            }
                        }
                    }

                    // Add a cheops pyramid
                    // Note in the final version we'll randomly decide if we are to place one on a particular screeen...
                    // Probably need a system to ensure that we place a minimum amount of these guys....
                    if (PotentialLocations.Count > 0)
                    {
                        int cheopsLocationToSelect = r.Next(PotentialLocations.Count);
                        gameScreens[a].CheopsPyramid = new InteractiveItem(
                            PotentialLocations[cheopsLocationToSelect].XLocation,
                            PotentialLocations[cheopsLocationToSelect].YLocation, 0);

                        // Setup the access code for the pyramid
                        // Ensure we get a unique code
                        do
                        {
                            gameScreens[a].CheopsAccessCode = gameEngine.RandomNumberGenerator.Next(10, 100).ToString();
                        } while (!CheckAccessCodeValidity(gameScreens[a].CheopsAccessCode));
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
                            gameScreens[a].AccessCode = gameEngine.RandomNumberGenerator.Next(100, 1000).ToString();
                        } while (!CheckAccessCodeValidity(gameScreens[a].AccessCode));
                    }
                }
            }

            // Remove most of the Cheops pyramids - we'll use the same technique for core elements
            List<GameScreenData> screensContainingCheopsPyramid = new List<GameScreenData>();
            List<GameScreenData> screensContainingCoreElements = new List<GameScreenData>();

            for (int a = 0; a < gameScreens.Length; a++)
            {
                // Only add screens with a cheops pyramid assigned
                if (gameScreens[a].CheopsPyramid != null)
                {
                    screensContainingCheopsPyramid.Add(gameScreens[a]);
                }

                // Only add screens with a core element assigned
                if (gameScreens[a].PickUps != null &&
                    gameScreens[a].PickUps.Count > 0)
                {
                    for (int b = 0; b < gameScreens[a].PickUps.Count; b++)
                    {
                        if ((enCollectables)gameScreens[a].PickUps[b].Type != enCollectables.AccessCard ||
                            (enCollectables)gameScreens[a].PickUps[b].Type != enCollectables.Key)
                        {
                            screensContainingCoreElements.Add(gameScreens[a]);
                            break;
                        }
                    }
                }
            }

            // Remove pyramids until we reach the max number we're allowed to have
            while (screensContainingCheopsPyramid.Count > c_maxCheops)
            {
                // Randomly select a cheops from the list of screens
                GameScreenData temp =
                    screensContainingCheopsPyramid[
                        gameEngine.RandomNumberGenerator.Next(0, screensContainingCheopsPyramid.Count)];

                // Remove it
                temp.CheopsPyramid = null;

                // Remove the screen from the list
                screensContainingCheopsPyramid.Remove(temp);
            }

            // Remove core elements until we reach the max number we're allowed to have
            while (screensContainingCoreElements.Count > c_maxCoreElements)
            {
                // Randomly select a pickup from the list of screens
                GameScreenData temp =
                    screensContainingCoreElements[
                        gameEngine.RandomNumberGenerator.Next(0, screensContainingCoreElements.Count)];

                // Remove them
                temp.PickUps = new List<InteractiveItem>();

                // Remove the screen from the list
                screensContainingCoreElements.Remove(temp);
            }

            // Set some of the core elements to match the core
            List<CoreElement> tempElements = new List<CoreElement>();

            for (int a = 0; a < gameEngine.Elements.GetActiveElements().Count; a++)
            {
                tempElements.Add(gameEngine.Elements.GetActiveElements()[a]);
            }

            // Make a random number of the core elements on the map match what we need
            int matchNumber = gameEngine.RandomNumberGenerator.Next(2, 6);

            for (int a = 0; a < matchNumber; a++)
            {
                int elementIndex = gameEngine.RandomNumberGenerator.Next(0, tempElements.Count);
                screensContainingCoreElements[a].PickUps[0].Type = tempElements[elementIndex].CurrentElement;
                tempElements.Remove(tempElements[elementIndex]);
            }
        }
    }

    public class SecondLayerTile
    {
        public SecondLayerTile(enTileType tileType, Point tileLocation)
        {
            TileType = tileType;
            TileLocation = tileLocation;
        }

        public enTileType TileType;
        public Point TileLocation;
    }

    public class TileDetected
    {
        private int tileNumber;

        public int TileNumber
        {
            get { return tileNumber; }
            set { tileNumber = value; }
        }

        private int xPosition;

        public int XPosition
        {
            get { return xPosition; }
            set { xPosition = value; }
        }

        private int yPosition;

        public int YPosition
        {
            get { return yPosition; }
            set { yPosition = value; }
        }

        private int mapXPosition;

        public int MapXPosition
        {
            get { return mapXPosition; }
            set { mapXPosition = value; }
        }

        private int mapYPosition;

        public int MapYPosition
        {
            get { return mapYPosition; }
            set { mapYPosition = value; }
        }

        public TileDetected(int tileNumber, int xPosition, int yPosition, int mapXPosition, int mapYPosition)
        {
            TileNumber = tileNumber;
            XPosition = xPosition;
            YPosition = yPosition;
            MapXPosition = mapXPosition;
            MapYPosition = mapYPosition;
        }

        public override string ToString()
        {
            return "TileNo: " + TileNumber.ToString() + " " + "XPos: " + XPosition.ToString() + " " + "YPos " +
                   YPosition.ToString();
        }
    }

    public class ResetTile
    {
        private TileDetected m_TileToReset;

        public TileDetected TileToReset
        {
            get { return m_TileToReset; }
            set { m_TileToReset = value; }
        }

        private int m_ScreenNumber;

        public int ScreenNumber
        {
            get { return m_ScreenNumber; }
            set { m_ScreenNumber = value; }
        }

        public ResetTile(TileDetected tileToReset, int screenNumber)
        {
            m_TileToReset = tileToReset;
            m_ScreenNumber = screenNumber;
        }
    }

    public class KeyLockDoor
    {
        public Point Location;
        public List<CollisionTile> CollisionTiles;

        public KeyLockDoor()
        {
            CollisionTiles = new List<CollisionTile>();
        }
    }
}