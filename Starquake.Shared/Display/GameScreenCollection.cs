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
using Starquake.Fonts;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.WorldGeneration;

namespace Starquake.Display;

internal class GameScreenCollection : IDynamicCollisionCells
{


    /// <summary>
    ///     A whole percentage, used for reporting how much of the map has been explored.
    /// </summary>
    private const int FullPercentage = 100;

    /// <summary>
    ///     What <c>ColourAnimationEngine.Start</c> is given for an animation that should pulse until
    ///     it is stopped rather than for a set number of loops.
    /// </summary>
    private const int LoopForever = -1;

    /// <summary>
    ///     How loudly a key-locked door says it has given way. The booth's own success tone, which
    ///     is what the planet already uses for access granted - quietly, because a door may be
    ///     opened again every time Blob walks back onto its screen.
    /// </summary>
    private const float DoorGivingWayVolume = 0.25f;

    /// <summary>
    ///     The three tiles that mark a shuttle pad, in the order they were always started in. Their
    ///     colours are <c>ColourAnimatedTiles</c>'s; this is only which three they are.
    /// </summary>
    private static readonly ActionTiles[] ShuttleTags =
    [
        ActionTiles.ShuttleTag1,
        ActionTiles.ShuttleTag3,
        ActionTiles.ShuttleTag2
    ];

    private readonly CoreElements _elements;

    private readonly GameScreenData[] _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    private readonly int _tileHeight;

    private readonly int _tileWidth;

    private readonly WorldPopulator _worldPopulator;

    private List<KeyLockDoor>? _keyLockDoors;


    private int _screenNumber;

    public GameScreenCollection(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        CoreElements elements,
        Random randomNumberGenerator,
        Shapes shapes,
        BitmapFont signpostFont,
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

        _gameScreens = (GameScreenData[])_resources.GetDataResource(Assets.Data.GameScreens);

        LandingSite = LandingSite.FindIn(_gameScreens);
        CoreExit = CoreExit.FindIn(_gameScreens);
        CoreSite = CoreSite.FindIn(_gameScreens);

        Collisions = new ScreenCollisionMap(
            _gameScreens,
            (int[,,])_resources.GetDataResource(Assets.Data.ScreenCollisionData));

        // The doors' force fields are cells the tile map does not describe, so the collision map has
        // to be able to ask for them again every time it is rebuilt - see IDynamicCollisionCells.
        Collisions.RegisterDynamicCells(this);

        _worldPopulator = new WorldPopulator(_gameScreens, Collisions, randomNumberGenerator);

        Renderer = new ScreenRenderer(
            resources,
            graphicsSet,
            _gameScreens,
            ColourAnimations,
            (IStandardParticleEffect)resources.GetParticleEffectInstance(Assets.Effects.ForceField, ParticleEffectType.Continual, null),
            shapes,
            signpostFont);

        BlobsShipSmoke1 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BasicSmokePlume,
                ParticleEffectType.Continual, null);
        BlobsShipSmoke2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BasicSmokePlume2,
                ParticleEffectType.Continual, null);

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

    /// <summary>
    ///     Where Blob's ship came down, read off the map rather than written into the game. Looked
    ///     up once: the map does not change while a game is running, and every frame of the wreck's
    ///     smoke would otherwise walk all 512 screens looking for the marker again.
    /// </summary>
    public LandingSite LandingSite { get; }

    /// <summary>
    ///     The core's doorstep, read off the map. Looked up once for the same reason as
    ///     <see cref="LandingSite" />: three things ask about it, one of them every frame.
    /// </summary>
    public CoreExit CoreExit { get; }

    /// <summary>
    ///     Where the planet core is, read off the map. Looked up once, like the other authored positions.
    /// </summary>
    public CoreSite CoreSite { get; }

    /// <summary>
    ///     The core's top left corner on the screen that carries it.
    /// </summary>
    public Point CorePosition => new(CoreSite.XPos, CoreSite.YPos);

    /// <summary>
    ///     Where Blob stands at the start of a game: centred across the square the author marked,
    ///     with its feet on the bottom of it.
    /// </summary>
    public Point BlobStartPosition => StandingIn(LandingSite.BlobStartTileX, LandingSite.BlobStartTileY);

    /// <summary>
    ///     Where Blob stands on leaving the core: the square the author marked, the same way round
    ///     as <see cref="BlobStartPosition" />.
    /// </summary>
    public Point CoreExitPosition => StandingIn(CoreExit.TileX, CoreExit.TileY);

    /// <summary>
    ///     The middle of the square the ship comes to rest in. The ship is drawn from its top left and
    ///     the two graphics sets draw it at different sizes, so the square gives the centre and the
    ///     caller takes half its own sprite off that.
    /// </summary>
    public Point ShipCrashCentre => new(
        LandingSite.CrashTileX * TileMetrics.TileWidth + TileMetrics.TileWidth / 2,
        LandingSite.CrashTileY * TileMetrics.TileHeight + TileMetrics.TileHeight / 2);

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

    /// <summary>
    ///     The cells of the key-locked doors found on the screen being shown. A door only hunts for
    ///     its own cells when it has none - see <see cref="UpdateKeyLockDoor" /> - so without this a
    ///     rebuild left the door holding cells that were no longer in the map, and its force field let
    ///     Blob walk through for the rest of the visit.
    /// </summary>
    public IEnumerable<CollisionTile> CellsOnScreen(int screenNumber)
    {
        if (screenNumber != _screenNumber || _keyLockDoors == null)
        {
            yield break;
        }

        foreach (KeyLockDoor keyLockDoor in _keyLockDoors)
        {
            foreach (CollisionTile collisionTile in keyLockDoor.CollisionTiles)
            {
                yield return collisionTile;
            }
        }
    }

    /// <summary>
    ///     Blob standing in a marked square: centred across it, with his feet on the bottom of it.
    /// </summary>
    private static Point StandingIn(int tileX, int tileY) => new(
        tileX * TileMetrics.TileWidth + (TileMetrics.TileWidth - SpriteMetrics.SpriteWidth) / 2,
        tileY * TileMetrics.TileHeight + TileMetrics.TileHeight - SpriteMetrics.SpriteHeight);

    public GameScreenData GetGameScreen(int number)
        => _gameScreens[number];

    /// <summary>
    ///     Which screen a teleport destination code reaches, or -1 for a code no screen answers to.
    ///     The codes are authored in the screen editor and carried in the screen data, so an
    ///     unrecognised code is an ordinary outcome rather than a missing one - it is what the booth
    ///     says CODE NOT RECOGNISED to.
    /// </summary>
    public int FindScreenByTeleportCode(string code)
    {
        for (int screen = 0; screen < _gameScreens.Length; screen++)
        {
            if (string.Equals(_gameScreens[screen].TeleportCode, code, StringComparison.OrdinalIgnoreCase))
            {
                return screen;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Builds the collision map for the first time. The cells are finer than the tiles -
    ///     <see cref="TileMetrics.CollisionCellWidth" /> square - so a sprite can stand part way
    ///     across a tile rather than snapping to it.
    /// </summary>
    public void Initialise() => UpdateCollisionMap(true);

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

    /// <summary>
    ///     Every empty tile on this screen that a nasty may be born on. A tile is offered if it is
    ///     empty scenery and nothing in the collision map underneath it is closed to the nasties.
    /// </summary>
    public List<Point> GetNastyPotentialLocations()
    {
        List<Point> potentialLocations = [];

        for (int y = 0; y < TilesDown; y++)
        {
            for (int x = 0; x < TilesAcross; x++)
            {
                if (CurrentGameScreenData.ScreenTiles[x, y].TileImage == -1 && NastiesMayStandOn(x, y))
                {
                    potentialLocations.Add(ConvertToScreenCoords(x, y));
                }
            }
        }

        return potentialLocations;
    }

    /// <summary>
    ///     Whether the collision cells under one tile are all open to a nasty. A cell marked
    ///     <see cref="CollisionTileType.NotBlob" /> is one the nasties are kept out of, so a tile
    ///     with any of those under it is not offered as somewhere to be born.
    /// </summary>
    private bool NastiesMayStandOn(int tileX, int tileY)
    {
        for (int cellDown = 0; cellDown < TileMetrics.CollisionCellsDownTile; cellDown++)
        {
            for (int cellAcross = 0; cellAcross < TileMetrics.CollisionCellsAcrossTile; cellAcross++)
            {
                int cellX = tileX * _tileWidth + cellAcross * CollAreaWidth;
                int cellY = tileY * _tileHeight + cellDown * CollAreaHeight;

                if (Collisions.HasNastyOnlyCellAt(cellX, cellY))
                {
                    return false;
                }
            }
        }

        return true;
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

                            StartShuttleTagAnimations();
                        }
                    }
                    else
                    {
                        if (gameScreen.ScreenTiles[x, y].TileImage == (int)ActionTiles.RemovedShuttlePlatform)
                        {
                            gameScreen.ScreenTiles[x, y].TileImage = (int)ActionTiles.ShuttlePlatform;

                            StopShuttleTagAnimations();
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
    ///     Stepped with where Blob is and whether he is carrying a key, rather than holding
    ///     Blob and asking. Two things on the planet follow Blob - the force field on a
    ///     key-locked door, which turns green when Blob can open it, and Olly's eyes, which
    ///     drift after it - and neither needs Blob himself, only three values it can be told.
    ///     <para>
    ///         The planet is only ever stepped by name from <c>MainGameScreen</c>, never
    ///         polymorphically, which is what lets this take arguments at all. Being told instead
    ///         of asking is in turn what lets the planet be built before Blob rather than
    ///         after it, which is the whole of the cycle between them.
    ///     </para>
    /// </summary>
    public void Update(GameTime gameTime, int blobXPos, int blobYPos, bool blobHasKey)
    {
        _keyLockDoors ??= GetKeyLockDoors();
        Renderer.KeyLockDoors = _keyLockDoors;

        // Counted rather than tested for null. GetKeyLockDoors returns an empty list for a screen with
        // no doors on it, never null, so asking whether it is null said yes on all 512 screens - which
        // left the force field running and being drawn everywhere once any door had started it.
        if (_keyLockDoors.Count > 0)
        {
            Renderer.StartForceField();

            foreach (KeyLockDoor keyLockDoor in _keyLockDoors)
            {
                UpdateKeyLockDoor(keyLockDoor, gameTime, blobHasKey, blobXPos, blobYPos);
            }
        }
        else
        {
            // Nothing on this screen is emitting, so neither should anything left over from the last
            // screen that was. Clearing rather than stopping, because stopping would leave the
            // particles already in the air hanging there unaged.
            Renderer.ClearForceField();
        }

        Renderer.TrackBlobWithOllysEyes(blobXPos, blobYPos);

        _resources.GetAnimationEngine().Update();
        ColourAnimations.Update();
    }

    /// <summary>
    ///     One key-locked door: whether it lets Blob through, and the force field that says so.
    ///     Passable to him if he is carrying the key, solid if not - and solid to nasties either
    ///     way, which is why the tiles flip between <see cref="CollisionTileType.NotBlob" /> and
    ///     <see cref="CollisionTileType.EveryoneHits" /> rather than being removed.
    /// </summary>
    private void UpdateKeyLockDoor(
        KeyLockDoor keyLockDoor, GameTime gameTime, bool blobHasKey, int blobXPos, int blobYPos)
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

        StepTheForceField(keyLockDoor, blobHasKey, blobXPos, blobYPos);

        Renderer.AnimateForceField(keyLockDoor, gameTime);
    }

    /// <summary>
    ///     Moves the door's own state on a frame: how far open it is drawn, what is left of the
    ///     burst at the moment it gave way, and the clock the field's bands and scanner run off.
    /// </summary>
    /// <remarks>
    ///     The opening is light and nothing else - the cells above have already flipped on the key
    ///     alone, so a door caught halfway through opening is passable. What it buys is the moment:
    ///     a door that is shut when Blob walks onto the screen and opens in front of him says he
    ///     has the key for it far better than one that was already green when the screen appeared.
    ///     <para>
    ///         A door is found afresh every time its screen is shown - see
    ///         <c>UpdateCollisionMap</c> - so that moment is had on each arrival rather than once
    ///         in the game.
    ///     </para>
    /// </remarks>
    private void StepTheForceField(KeyLockDoor keyLockDoor, bool blobHasKey, int blobXPos, int blobYPos)
    {
        keyLockDoor.Frame++;
        keyLockDoor.Brace = ForceFieldSettings.BraceAgainst(keyLockDoor.Doorway, blobXPos, blobYPos);

        if (blobHasKey && !keyLockDoor.WasOpen)
        {
            keyLockDoor.Flash = ForceFieldSettings.NewFlash();

            _resources.GetSoundFxEngine()
                .PlaySound(Assets.Sounds.AccessSuccess, DoorGivingWayVolume, false);
        }

        keyLockDoor.WasOpen = blobHasKey;
        keyLockDoor.Openness = ForceFieldSettings.OpennessAfter(keyLockDoor.Openness, blobHasKey);
        keyLockDoor.Flash = ForceFieldSettings.FlashAfter(keyLockDoor.Flash);
    }

    /// <summary>
    ///     Pulses the three tags that mark where the shuttle pad used to be, so that a pad Blob has
    ///     taken the shuttle off still says where it was. Indefinitely - the tags pulse until the
    ///     shuttle is put back.
    /// </summary>
    private void StartShuttleTagAnimations()
    {
        foreach (ActionTiles tag in ShuttleTags)
        {
            ColourAnimations.Start((int)tag, LoopForever);
        }
    }

    /// <inheritdoc cref="StartShuttleTagAnimations" />
    private void StopShuttleTagAnimations()
    {
        foreach (ActionTiles tag in ShuttleTags)
        {
            ColourAnimations.Stop((int)tag);
        }
    }

    private void AddKeyLockDoorCollisionTiles(KeyLockDoor keyLockDoor)
    {
        Rectangle doorway = keyLockDoor.Doorway;

        for (int c = 0; c < TileMetrics.CollisionCellsDownTile; c++)
        {
            CollisionTile keyLockCollisionTile =
                CollisionTile.ForObject(
                    CollisionObjectType.KeyAccessDoor,
                    doorway.Left,
                    doorway.Top + c * TileMetrics.CollisionCellHeight,
                    CollisionTileType.None);

            // The door keeps its own tiles so it can take them out again, and the collision map is
            // what anything walking into the door actually tests against.
            keyLockDoor.CollisionTiles.Add(keyLockCollisionTile);
            Collisions.Add(keyLockCollisionTile);
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
    ///     Starts a new game on this planet: Blob back at the wreck, and the whole map scattered
    ///     with things to find.
    /// </summary>
    public void Reset()
    {
        ScreenNumber = LandingSite.ScreenNumber;

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
