// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.WorldGeneration;

namespace Starquake.Display;

internal class GameScreenCollection
{


    /// <summary>
    ///     A whole percentage, used for reporting how much of the map has been explored.
    /// </summary>
    private const int FullPercentage = 100;

    private readonly CoreElements _elements;

    private readonly GameScreenData[] _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    private readonly WorldPopulator _worldPopulator;
    private readonly ResourceManager _resources;

    private readonly int _tileHeight;

    private readonly int _tileWidth;

    private List<KeyLockDoor>? _keyLockDoors;



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

        Renderer = new ScreenRenderer(
            resources,
            graphicsSet,
            _gameScreens,
            ColourAnimations,
            (IStandardParticleEffect)resources.GetParticleEffectInstance(
                "ForceField", ParticleEffectType.Continual, null));

        BlobsShipSmoke1 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.Continual, null);
        BlobsShipSmoke2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("BasicSmokePlume2",
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

    /// <summary>
    ///     What draws the screen being shown. Kept pointed at the same screen as
    ///     <see cref="Collisions" />.
    /// </summary>
    public ScreenRenderer Renderer { get; }

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

    public void Initialise()
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
        Renderer.ScreenNumber = ScreenNumber;
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
    ///         The planet is only ever stepped by name from <c>MainGameScreen</c>, never
    ///         polymorphically, which is what lets this take arguments at all. Being told instead
    ///         of asking is in turn what lets the planet be built before the blob rather than
    ///         after it, which is the whole of the cycle between them.
    ///     </para>
    /// </summary>
    public void Update(GameTime gameTime, int blobXPos, int blobYPos, bool blobHasKey)
    {
        _keyLockDoors ??= GetKeyLockDoors();
        Renderer.KeyLockDoors = _keyLockDoors;

        if (_keyLockDoors != null)
        {
            Renderer.StartForceField();

            foreach (KeyLockDoor keyLockDoor in _keyLockDoors)
            {
                UpdateKeyLockDoor(keyLockDoor, gameTime, blobHasKey);
            }
        }

        Renderer.TrackBlobWithOllysEyes(blobXPos, blobYPos);

        _resources.GetAnimationEngine().Update();
        ColourAnimations.Update();
    }

    /// <summary>
    ///     One key-locked door: whether it lets blob through, and the force field that says so.
    ///     Passable to him if he is carrying the key, solid if not - and solid to nasties either
    ///     way, which is why the tiles flip between <see cref="CollisionTileType.NotBlob" /> and
    ///     <see cref="CollisionTileType.EveryoneHits" /> rather than being removed.
    /// </summary>
    private void UpdateKeyLockDoor(KeyLockDoor keyLockDoor, GameTime gameTime, bool blobHasKey)
    {
        if (keyLockDoor.CollisionTiles.Count == 0)
        {
            AddKeyLockDoorCollisionTiles(keyLockDoor);
        }

        foreach (CollisionTile collisionTile in keyLockDoor.CollisionTiles)
        {
            collisionTile.CollisionTileType = blobHasKey
                ? CollisionTileType.NotBlob
                : CollisionTileType.EveryoneHits;
        }

        Renderer.AnimateForceField(keyLockDoor, gameTime, blobHasKey);
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

    /// <inheritdoc cref="ScreenRenderer.Draw" />
    public void Draw(Color colour, bool introSequenceRunning = false,
        IntroSequenceStage introSequenceStage = IntroSequenceStage.None)
        => Renderer.Draw(colour, introSequenceRunning, introSequenceStage);

    /// <inheritdoc cref="ScreenRenderer.DrawUpperLayer" />
    public void DrawUpperLayer(Color colour)
        => Renderer.DrawUpperLayer(colour);

    /// <inheritdoc cref="ScreenRenderer.DrawShadowing" />
    public void DrawShadowing()
        => Renderer.DrawShadowing();

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
