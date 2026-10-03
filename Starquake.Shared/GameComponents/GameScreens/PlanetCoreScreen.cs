// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

internal class PlanetCoreScreen : IGameScreen, IDynamicCollisionCells
{


    /// <summary>
    ///     The most aliens the core room ever holds, which is also how many it holds once Blob
    ///     has nearly finished repairing it - the fuller the core, the busier its guards.
    /// </summary>
    private const int MaxCoreAliens = 4;

    /// <summary>
    ///     How many core elements there are to replace. Putting the last one back finishes the game.
    /// </summary>
    private const int CoreElementsToReplace = 9;

    /// <summary>
    ///     How busy the core room gets as it is repaired. The guards arrive as the number of elements
    ///     still missing falls past each of these, so an untouched core is unguarded and a nearly
    ///     finished one has all <see cref="MaxCoreAliens" /> of them.
    /// </summary>
    private const int OneAlienFrom = 8;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int TwoAliensFrom = 6;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int ThreeAliensFrom = 4;

    /// <inheritdoc cref="OneAlienFrom" />
    private const int FourAliensFrom = 2;

    /// <summary>
    ///     How long Blob is held in the core room before being put back outside, in logic frames.
    /// </summary>
    private const int CoreVisitFrames = 240;

    /// <summary>
    ///     How fast the tiles that blank out the room fade away as the core is revealed.
    /// </summary>
    private const int BlankingTilesFadeStep = 16;

    /// <summary>
    ///     The blanking tiles cover the room from the second column onwards, and run
    ///     <see cref="BlankingTilesAcross" /> by <see cref="BlankingTilesDown" />.
    /// </summary>
    private const int BlankingTilesAcross = 6;

    /// <inheritdoc cref="BlankingTilesAcross" />
    private const int BlankingTilesDown = 6;

    /// <inheritdoc cref="BlankingTilesAcross" />
    private const int BlankingTilesFirstColumn = TileMetrics.TileWidth;

    /// <summary>
    ///     Where in the room the core's own particle effects are emitted from: the middle of the
    ///     playfield.
    /// </summary>
    private const int CoreCentreX = TileMetrics.PlayAreaWidth / 2;

    /// <inheritdoc cref="CoreCentreX" />
    private const int CoreCentreY = TileMetrics.PlayAreaHeight / 2;
    private readonly Blob _blob;

    private readonly List<Nasty> _coreAliens;
    private readonly IStandardParticleEffect _coreComputationEffect;

    //private MercuryParticleEngine.MercuryParticleEffect coreVortex;
    private readonly IStandardParticleEffect _coreSparks;

    private readonly CoreElements _elements;

    private readonly Queue<int> _elementsToReplace = new();
    private readonly GameScreenCollection _gameScreens;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private int _blankingTilesOpacity;
    private int _coreAlienCount = MaxCoreAliens;
    private int _entryTime;

    private bool _justEnteredCore = true;

    private bool _replacingElements;


    public PlanetCoreScreen(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        ScreenDirectory screenDirectory,
        Blob blob,
        Random randomNumberGenerator,
        CoreElements elements)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _screenDirectory = screenDirectory;
        _blob = blob;

        _elements = elements;
        _elements.OnCoreElementReplaced += Elements_OnCoreElementReplaced;

        // The elements standing in the core are collision the core screen's tiles say nothing about,
        // so the collision map asks for them each time it is rebuilt - see IDynamicCollisionCells.
        _gameScreens.Collisions.RegisterDynamicCells(this);

        _coreAliens = new List<Nasty>();
        for (int a = 0; a < _coreAlienCount; a++)
        {
            var coreAlien = new Nasty(
                _resources,
                _gameScreens,
                graphicsSet,
                _blob,
                randomNumberGenerator,
                0,
                0,
                CoreElementsToReplace);

            coreAlien.NastyType = NastyType.CoreGuardian;
            _coreAliens.Add(coreAlien);
        }

        _coreComputationEffect = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BinaryEffect, ParticleEffectType.Continual, null);
        _coreSparks = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.CoreSparks, ParticleEffectType.Continual, null);
    }


    /// <summary>
    ///     Keeps the core room's events moving while Blob is away from it. The engine calls
    ///     this directly rather than through <see cref="IGameScreen" />, which the frame loop's two
    ///     members are all of - the core is the only screen that carries on off screen, so an
    ///     interface for it described one class.
    /// </summary>
    public void UpdateOffScreen(GameTime gameTime)
    {
        for (int a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].Update(gameTime);
        }

        _elements.Update(gameTime, _gameScreens.CorePosition);

        _coreSparks.Start();
        _coreSparks.Update(CoreCentreX, CoreCentreY, gameTime);

        _coreComputationEffect.Start();
        _coreComputationEffect.Update(CoreCentreX, CoreCentreY, gameTime);
    }

    public void Update(GameTime gameTime)
    {
        if (_justEnteredCore)
        {
            _blankingTilesOpacity = ColourLevels.Full;
            _replacingElements = false;
            EnterCore();
            _justEnteredCore = false;

            CalculateCoreAlienCount();

            _elementsToReplace.Clear();

            // Check to see if Blob is carrying any elements to replace
            List<CoreElement> activeElements = _elements.GetActiveElements();

            for (int a = 0; a < _blob.BlobInventory.Length; a++)
            {
                foreach (CoreElement activeElement in activeElements)
                {
                    if ((int)_blob.BlobInventory[a] == activeElement.CurrentElement)
                    {
                        _blob.BlobInventory[a] = Collectables.None;
                        _elementsToReplace.Enqueue(activeElement.CurrentElement);
                        _replacingElements = true;

                    }
                }
            }

            if (!_replacingElements)
            {
                _entryTime = 0;
            }
            else
            {
                Elements_OnCoreElementReplaced();
            }
        }

        if (!_replacingElements)
        {
            _blankingTilesOpacity = Ramp.Down(_blankingTilesOpacity, BlankingTilesFadeStep, ColourLevels.None);
        }

        // Leave the core room
        if (!_replacingElements && _entryTime > CoreVisitFrames)
        {
            // The square the author marked, rather than the screen beside the core and hard against its
            // right-hand wall. The Y is set now as well as the X: it used to be whatever height Blob
            // happened to be at inside the core room, which on any other screen could be mid air.
            Point exit = _gameScreens.CoreExitPosition;

            _gameScreens.ScreenNumber = _gameScreens.CoreExit.ScreenNumber;
            _screenDirectory.Current = GameScreenType.MainGame;
            _blob.BlobXPos = exit.X;
            _blob.BlobYPos = exit.Y;
            _screenDirectory.MainGame.PerformPostScreenChangeActions(GameScreenType.PlanetCore, new ScreenChange.FadeIn());

            _justEnteredCore = true;
        }
        else if (!_replacingElements && _elements.GetElementsReplacedCount() == CoreElementsToReplace)
        {
            // Finished the game!
            _screenDirectory.Current = GameScreenType.GameComplete;
            _screenDirectory.GameComplete.PerformPostScreenChangeActions();
        }
        else
        {
            UpdateOffScreen(gameTime);
        }

        _entryTime++;
    }

    public void Draw()
    {
        if (!_replacingElements)
        {
            _coreComputationEffect.Draw();
            _coreSparks.Draw();
        }

        if (!_replacingElements)
        {
            _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).Begin(BlendState.AlphaBlend);
            for (int a = 0; a < BlankingTilesDown; a++)
            {
                for (int b = 0; b < BlankingTilesAcross; b++)
                {
                    _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).DrawSprite(0, b * TileMetrics.TileWidth + BlankingTilesFirstColumn, a * TileMetrics.TileHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_blankingTilesOpacity));
                }
            }
            _resources.GetSpriteCollection(Assets.Graphics.BlankingTile).End();
        }

        _gameScreens.Draw(Color.White);

        // Draw the core aliens
        if (!_replacingElements)
        {
            for (int a = 0; a < _coreAlienCount; a++)
            {
                _coreAliens[a].Draw((byte)(ColourLevels.Full - _blankingTilesOpacity));
            }
        }

        // Where the core is comes off the map, so it is asked for rather than known here.
        Point core = _gameScreens.CorePosition;

        _elements.Draw(core.X, core.Y, _replacingElements);
    }

    /// <summary>
    ///     The cells of the nine elements standing in the core room, which are there only while the
    ///     core room is the screen being shown - the core sits on one of the planet's own screens, and
    ///     Blob walks that screen normally once he has been put back outside.
    /// </summary>
    public IEnumerable<CollisionTile> CellsOnScreen(int screenNumber)
    {
        if (screenNumber != _gameScreens.CoreSite.ScreenNumber ||
            _screenDirectory.Current != GameScreenType.PlanetCore)
        {
            return [];
        }

        Point core = _gameScreens.CorePosition;

        return _elements.GetCollisionMap(core.X, core.Y);
    }

    /// <summary>
    ///     Puts the core screen and its guardians in place for when he has just arrived.
    /// </summary>
    private void EnterCore()
    {
        // Setting the screen number rebuilds the collision map, and that rebuild is what puts the
        // core's elements into it - see CellsOnScreen. They used to be added here instead, which left
        // them out again the moment anything else on this screen rebuilt the map.
        _gameScreens.ScreenNumber = _gameScreens.CoreSite.ScreenNumber;

        List<Point> potentialLocations = _gameScreens.GetNastyPotentialLocations();
        if (potentialLocations.Count < _coreAlienCount)
        {
            _coreAlienCount = potentialLocations.Count;
        }

        for (int a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].XPos = potentialLocations[a].X;
            _coreAliens[a].YPos = potentialLocations[a].Y;
            _coreAliens[a].NastyType = NastyType.CoreGuardian;
            _coreAliens[a].NastyState = NastyState.Normal;
        }
    }

    private void Elements_OnCoreElementReplaced()
    {
        if (_elementsToReplace.Count > 0)
        {
            _elements.ReplaceElement(_elementsToReplace.Dequeue());
        }
        else
        {
            _entryTime = 0;
            _replacingElements = false;
            CalculateCoreAlienCount();
        }
    }

    private void CalculateCoreAlienCount()
    {
        // Figure out how many core aliens to display
        int elementsLeftToReplace = _elements.GetActiveElements().Count;
        if (elementsLeftToReplace > OneAlienFrom)
        {
            _coreAlienCount = 0;
        }
        else if (elementsLeftToReplace <= OneAlienFrom && elementsLeftToReplace > TwoAliensFrom)
        {
            _coreAlienCount = 1;
        }
        else if (elementsLeftToReplace <= TwoAliensFrom && elementsLeftToReplace > ThreeAliensFrom)
        {
            _coreAlienCount = 2;
        }
        else if (elementsLeftToReplace <= ThreeAliensFrom && elementsLeftToReplace > FourAliensFrom)
        {
            _coreAlienCount = 3;
        }
        else if (elementsLeftToReplace < FourAliensFrom)
        {
            _coreAlienCount = MaxCoreAliens;
        }
    }
}
