// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

public class PlanetCoreScreen : GameComponentBase, IRealTimeGameScreen
{


    /// <summary>
    ///     The most aliens the core room ever holds, which is also how many it holds once the blob
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
    ///     How long the blob is held in the core room before being put back outside, in logic frames.
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
    private readonly GraphicsSet _graphicsSet;
    private readonly PresentationSettings _presentation;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly ScreenDirectory _screenDirectory;
    private readonly GameSession _session;
    private int _blankingTilesOpacity;
    private int _coreAlienCount = MaxCoreAliens;
    private int _entryTime;

    private bool _justEnteredCore = true;

    private bool _replacingElements;


    public PlanetCoreScreen(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        PresentationSettings presentation,
        ScreenDirectory screenDirectory,
        GameSession session,
        Blob blob,
        Random randomNumberGenerator,
        CoreElements elements)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _presentation = presentation;
        _screenDirectory = screenDirectory;
        _session = session;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;

        _elements = elements;
        _elements.OnCoreElementReplaced += Elements_OnCoreElementReplaced;
        _elements.LoadContent();

        _coreAliens = new List<Nasty>();
        for (int a = 0; a < _coreAlienCount; a++)
        {
            var coreAlien = new Nasty(
                _resources,
                _gameScreens,
                _graphicsSet,
                _session,
                _blob,
                _randomNumberGenerator,
                0,
                0,
                CoreElementsToReplace);

            coreAlien.NastyType = NastyType.CoreGuardian;
            _coreAliens.Add(coreAlien);
        }

        _coreComputationEffect = (IStandardParticleEffect)_resources.GetParticleEffectInstance("BinaryEffect", ParticleEffectType.Continual, null);
        _coreSparks = (IStandardParticleEffect)_resources.GetParticleEffectInstance("CoreSparks", ParticleEffectType.Continual, null);
    }


    public void Reset(object? data)
    {
    }

    /// <summary>
    ///     Some screens need to keep events moving even when they are not showing
    /// </summary>
    /// <param name="gameTime"></param>
    public void UpdateOffScreen(GameTime gameTime)
    {
        for (int a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].Update(gameTime);
        }

        _elements.Update(gameTime);

        _coreSparks.Start();
        _coreSparks.Update(CoreCentreX, CoreCentreY, gameTime);

        _coreComputationEffect.Start();
        _coreComputationEffect.Update(CoreCentreX, CoreCentreY, gameTime);
    }

    public override void Update(GameTime gameTime)
    {
        if (_justEnteredCore)
        {
            _blankingTilesOpacity = ColourLevels.Full;
            _replacingElements = false;
            EnterCore();
            _justEnteredCore = false;

            CalculateCoreAlienCount();

            _elementsToReplace.Clear();

            // Check to see if blob is carrying any elements to replace
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
            if (_blankingTilesOpacity > 0)
            {
                _blankingTilesOpacity = _blankingTilesOpacity - BlankingTilesFadeStep;
            }

            if (_blankingTilesOpacity < 0)
            {
                _blankingTilesOpacity = 0;
            }
        }

        // Leave the core room
        if (!_replacingElements && _entryTime > CoreVisitFrames)
        {
            _gameScreens.ScreenNumber = WorldMap.CoreApproachScreenNumber;
            _screenDirectory.Current = GameScreenType.MainGame;
            _blob.BlobXPos = _gameScreens.ScreenWidth - _blob.BlobWidth;
            _screenDirectory.Get(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.PlanetCore, new ScreenChange.FadeIn());

            _justEnteredCore = true;
            _presentation.BackgroundColour = Color.Black;
        }
        else if (!_replacingElements && _elements.GetElementsReplacedCount() == CoreElementsToReplace)
        {
            // Finished the game!
            _screenDirectory.Current = GameScreenType.GameComplete;
            _screenDirectory.Get(GameScreenType.GameComplete).PerformPostScreenChangeActions(GameScreenType.MainGame, new ScreenChange.FadeIn());
        }
        else
        {
            UpdateOffScreen(gameTime);
        }

        _entryTime++;
    }

    public override void Draw(GameTime gameTime)
    {
        if (!_replacingElements)
        {
            if (_coreComputationEffect != null)
            {
                _coreComputationEffect.Draw();
            }

            if (_coreSparks != null)
            {
                _coreSparks.Draw();
            }
        }

        if (!_replacingElements)
        {
            _resources.GetSpriteCollection("BlankingTile").Begin(BlendState.AlphaBlend);
            for (int a = 0; a < BlankingTilesDown; a++)
            {
                for (int b = 0; b < BlankingTilesAcross; b++)
                {
                    _resources.GetSpriteCollection("BlankingTile").DrawSprite(0, b * TileMetrics.TileWidth + BlankingTilesFirstColumn, a * TileMetrics.TileHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_blankingTilesOpacity));
                }
            }
            _resources.GetSpriteCollection("BlankingTile").End();
        }

        _gameScreens.Draw(Color.White, gameTime);

        // Draw the core aliens
        if (!_replacingElements)
        {
            for (int a = 0; a < _coreAlienCount; a++)
            {
                _coreAliens[a].Draw(gameTime, (byte)(ColourLevels.Full - _blankingTilesOpacity));
            }
        }

        _elements.Draw(_replacingElements);
    }

    #region IGameScreen Members

    public int ScreenFade
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    #endregion

    #region IGameScreen Members

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, ScreenChange change)
    {
        throw new NotImplementedException();
    }

    #endregion

    /// <summary>
    ///     Puts the core screen and its guardians in place for a blob that has just arrived.
    /// </summary>
    private void EnterCore()
    {
        _gameScreens.ScreenNumber = WorldMap.CoreScreenNumber;

        // Add the collision map for the core elements
        _gameScreens.CurrentGameScreenData.CollisionMap.AddRange(_elements.GetCollisionMap());

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
