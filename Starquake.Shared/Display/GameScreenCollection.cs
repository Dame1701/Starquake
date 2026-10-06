// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Fonts;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;
using Starquake.WorldGeneration;

namespace Starquake.Display;

/// <summary>
///     The planet. Its 512 screens and the tiles on each, where the landing site, the core and its
///     exit are, which screens have been visited, and what is where - asked by everything that has
///     to find somewhere to be. It also builds and owns the four things that are of this planet and
///     nothing else: the collision map read off its tiles, the renderer that draws them, the colour
///     animations that pulse some of them, and the key-locked doors standing on them.
/// </summary>
/// <remarks>
///     It used to do four jobs rather than one. Two were facades: it carried
///     <c>CheckRectangles</c>, <c>CheckCollision</c> and <c>CheckTileByObject</c> straight through
///     to <see cref="Collisions" /> and <c>Draw</c>, <c>DrawUpperLayer</c> and
///     <c>DrawShadowing</c> straight through to <see cref="Renderer" />, both of which it also
///     exposes - so there were two ways to ask the same question and callers were split between
///     them, with <c>TunnelLift</c>, <c>BlobFall</c>, <c>NastyBrain</c> and <c>UnstickProbe</c>
///     already going the direct way. The forwarders are gone and everything goes the direct way.
///     <para>
///         The fourth was the key-locked doors: the one list of mutable door state in the codebase,
///         the five methods that stepped it, and the <c>IDynamicCollisionCells</c> implementation
///         that existed only to hand those doors' cells to the collision map. That is
///         <see cref="KeyLockDoors" /> now, and the interface went with it.
///     </para>
///     <para>
///         Owning a thing is not a fifth job. The renderer and the collision map are both built from
///         this planet's screen data and cannot be built without it, so the planet builds them and
///         keeps them in step - see <see cref="UpdateCollisionMap" />. What it no longer does is
///         answer for them.
///     </para>
///     <para>
///         The constructor took nine arguments and four of them were <see cref="TileMetrics" />
///         constants - the tiles across and down and the size of a collision cell - handed in by the
///         one place that builds a planet, which read them off <c>TileMetrics</c> to do it. They were
///         parameters dressed as configuration: a planet with a different grid is not a thing this
///         game has, and <c>TileMetrics</c> says why in its own remarks. They are read where they are
///         used now, along with the two more the constructor was copying into fields of its own, and
///         <c>TilesAcross</c> and <c>TilesDown</c> are no longer properties of the planet either -
///         the one caller outside it reads the constant, the same way the inside does.
///     </para>
/// </remarks>
internal class GameScreenCollection
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
    ///     The three tiles that mark a shuttle pad, in the order they were always started in. Their
    ///     colours are <c>ColourAnimatedTiles</c>'s; this is only which three they are.
    /// </summary>
    private static readonly ActionTiles[] ShuttleTags =
    [
        ActionTiles.ShuttleTag1,
        ActionTiles.ShuttleTag3,
        ActionTiles.ShuttleTag2
    ];


    private readonly GameScreenData[] _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    private readonly WorldPopulator _worldPopulator;

    private int _screenNumber;

    public GameScreenCollection(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        Shapes shapes,
        BitmapFont signpostFont)
    {
        _resources = resources;
        _graphicsSet = graphicsSet;

        // The colour animations are animations of this planet's own tiles, and nothing but the
        // planet and the main screen drives them - so the planet owns one and hands itself to it.
        // That is the last cycle in the graph: the animation engine needs the planet to read tiles
        // from, and the planet was reaching back through the engine to find the animation engine.
        ColourAnimations = new ColourAnimationEngine(resources, this, graphicsSet);


        _gameScreens = (GameScreenData[])_resources.GetDataResource(Assets.Data.GameScreens);

        LandingSite = LandingSite.FindIn(_gameScreens);
        CoreExit = CoreExit.FindIn(_gameScreens);
        CoreSite = CoreSite.FindIn(_gameScreens);

        Collisions = new ScreenCollisionMap(
            _gameScreens,
            (int[,,])_resources.GetDataResource(Assets.Data.ScreenCollisionData));

        _worldPopulator = new WorldPopulator(_gameScreens, Collisions, randomNumberGenerator);

        Renderer = new ScreenRenderer(
            resources,
            graphicsSet,
            _gameScreens,
            ColourAnimations,
            (IStandardParticleEffect)resources.GetParticleEffectInstance(Assets.Effects.ForceField, ParticleEffectType.Continual, null),
            shapes,
            signpostFont);

        // After the renderer and the collision map, because it drives the one and adds cells to the
        // other. The doors' force fields are cells the tile map does not describe, so it registers
        // itself as a source the map can ask again on every rebuild - see IDynamicCollisionCells.
        Doors = new KeyLockDoors(
            resources,
            graphicsSet,
            _gameScreens,
            Collisions,
            Renderer);
        Collisions.RegisterDynamicCells(Doors);

        BlobsShipSmoke1 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BasicSmokePlume,
                ParticleEffectType.Continual, null);
        BlobsShipSmoke2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BasicSmokePlume2,
                ParticleEffectType.Continual, null);
    }

    /// <summary>
    ///     The key-locked doors on the screen being shown, and their force fields.
    /// </summary>
    public KeyLockDoors Doors { get; }

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
        .SpriteWidth * TileMetrics.TilesAcross;

    public int ScreenHeight => _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
        .SpriteHeight * TileMetrics.TilesDown;

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

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
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
                int cellX = tileX * TileMetrics.TileWidth + cellAcross * TileMetrics.CollisionCellWidth;
                int cellY = tileY * TileMetrics.TileHeight + cellDown * TileMetrics.CollisionCellHeight;

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
            Doors.ForgetScreen();
        }

        Collisions.Rebuild(ScreenNumber);
        Renderer.ScreenNumber = ScreenNumber;
    }

    public void ChangeShuttlePlatformState(bool padRemoved)
    {
        foreach (GameScreenData gameScreen in _gameScreens)
        {
            for (int y = 0; y < TileMetrics.TilesDown; y++)
            {
                for (int x = 0; x < TileMetrics.TilesAcross; x++)
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

    public Point ConvertToScreenCoords(int tileMapXPos, int tileMapYPos)
        => new(
            tileMapXPos * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .SpriteWidth,
            tileMapYPos * _resources.GetSpriteCollection(_graphicsSet.Names.LowerScreenTiles)
                .SpriteHeight);

    public List<Point> GetCoordsOfTiles(int tileType)
    {
        var returnedTiles = new List<Point>();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
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

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
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
        Doors.Update(gameTime, ScreenNumber, blobHasKey, blobXPos, blobYPos);

        Renderer.TrackBlobWithOllysEyes(blobXPos, blobYPos);

        _resources.GetAnimationEngine().Update();
        ColourAnimations.Update();
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

    public List<SecondLayerTile> GetListOfSecondLayerTiles()
    {
        var tilesToReturn = new List<SecondLayerTile>();

        for (int y = 0; y < TileMetrics.TilesDown; y++)
        {
            for (int x = 0; x < TileMetrics.TilesAcross; x++)
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
    /// <param name="coreElementsWanted">
    ///     The parts the core is still asking for, from <c>CoreElements.ElementsWanted</c>. Passed in
    ///     rather than read off a core held here: the planet needs this list once, at the start of a
    ///     game, and holding the core for it was the planet's whole dependency on the things that
    ///     walk about on it.
    /// </param>
    public void Reset(IReadOnlyList<int> coreElementsWanted)
    {
        ScreenNumber = LandingSite.ScreenNumber;

        _worldPopulator.Populate(coreElementsWanted);
    }
}
