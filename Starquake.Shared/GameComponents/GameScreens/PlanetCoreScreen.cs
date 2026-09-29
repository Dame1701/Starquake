// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines;
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
    private int _blankingTilesOpacity;
    private int _coreAlienCount = MaxCoreAliens;

    private List<Nasty> _coreAliens;
    private IStandardParticleEffect _coreComputationEffect;

    //private MercuryParticleEngine.MercuryParticleEffect coreVortex;
    private IStandardParticleEffect _coreSparks;

    private CoreElements _elements;

    private Queue<int> _elementsToReplace;
    private int _entryTime;

    private bool _justEnteredCore = true;
    private Random _randomNumberGenerator;

    private bool _replacingElements;


    public PlanetCoreScreen(GameEngine gameEngineReference)
        : base(gameEngineReference)
    {

    }

    public GameScreenCollection GameScreens { get; set; }

    public bool Success => true;

    public void Reset(object data)
    {
    }

    /// <summary>
    ///     Some screens need to keep events moving even when they are not showing
    /// </summary>
    /// <param name="gameTime"></param>
    public void UpdateOffScreen(GameTime gameTime)
    {
        for (var a = 0; a < _coreAlienCount; a++)
        {
            _coreAliens[a].Update(gameTime);
        }

        _elements.Update(gameTime);

        if (_coreSparks != null)
        {
            _coreSparks.Start();
            _coreSparks.Update(CoreCentreX, CoreCentreY, gameTime);
        }

        if (_coreComputationEffect != null)
        {
            _coreComputationEffect.Start();
            _coreComputationEffect.Update(CoreCentreX, CoreCentreY, gameTime);
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_justEnteredCore)
        {
            _blankingTilesOpacity = ColourLevels.Full;
            _replacingElements = false;
            Initialise(null);
            _justEnteredCore = false;

            CalculateCoreAlienCount();

            _elementsToReplace = new Queue<int>();

            // Check to see if blob is carrying any elements to replace
            var activeElements = _elements.GetActiveElements();

            for (var a = 0; a < GameEngine.Player.BlobInventory.Length; a++)
            {
                for (var b = 0; b < activeElements.Count; b++)
                {
                    if ((int)GameEngine.Player.BlobInventory[a] == activeElements[b].CurrentElement)
                    {
                        GameEngine.Player.BlobInventory[a] = Collectables.None;
                        _elementsToReplace.Enqueue(activeElements[b].CurrentElement);
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
                elements_OnCoreElementReplaced();
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
            GameEngine.GameScreens.ScreenNumber = WorldMap.CoreApproachScreenNumber;
            GameEngine.CurrentGameScreen = GameScreenType.MainGame;
            GameEngine.Player.BlobXPos = GameEngine.GameScreens.ScreenWidth - GameEngine.Player.BlobWidth;
            GameEngine.GetGameScreen(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.PlanetCore, null);

            _justEnteredCore = true;
            GameEngine.BackgroundColour = Color.Black;
        }
        else if (!_replacingElements && GameEngine.Elements.GetElementsReplacedCount() == CoreElementsToReplace)
        {
            // Finished the game!
            GameEngine.CurrentGameScreen = GameScreenType.GameComplete;
            GameEngine.GetGameScreen(GameScreenType.GameComplete).PerformPostScreenChangeActions(GameScreenType.MainGame, null);
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
            GameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").Begin(BlendState.AlphaBlend, gameTime);
            for (var a = 0; a < BlankingTilesDown; a++)
            {
                for (var b = 0; b < BlankingTilesAcross; b++)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").DrawSprite(0, b * TileMetrics.TileWidth + BlankingTilesFirstColumn, a * TileMetrics.TileHeight, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_blankingTilesOpacity));
                }
            }
            GameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").End();
        }

        GameEngine.GameScreens.Draw(Color.White, gameTime);

        // Draw the core aliens
        if (!_replacingElements)
        {
            for (var a = 0; a < _coreAlienCount; a++)
            {
                _coreAliens[a].Draw(gameTime, (byte)(ColourLevels.Full - _blankingTilesOpacity));
            }
        }

        _elements.Draw(gameTime, _replacingElements);
    }

    #region IGameScreen Members

    public int ScreenFade
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    #endregion

    #region IGameScreen Members

    public void PerformPostScreenChangeActions(GameScreenType triggeringScreen, object param)
    {
        throw new NotImplementedException();
    }

    #endregion

    public override void LoadContent()
    {
        _elements.LoadContent();
        _coreAliens = new List<Nasty>();
        var nastyGraphics = new ContentList(GameEngine.ContentMgr);
        nastyGraphics.AddContent("nastySprite", "Graphics\\nastiesNew");
        _randomNumberGenerator = new Random((int)DateTime.Now.Ticks);

        for (var a = 0; a < _coreAlienCount; a++)
        {
            var coreAlien = new Nasty(
                GameEngine,
                0,
                0,
                CoreElementsToReplace);

            coreAlien.LoadContent(nastyGraphics);
            coreAlien.NastyType = NastyType.CoreGuardian;
            coreAlien.Initialise(null);
            _coreAliens.Add(coreAlien);
        }
        _coreComputationEffect = GameEngine.ContentResourceManager.GetParticleEffectInstance("BinaryEffect", ParticleEffectType.Continual, null) as IStandardParticleEffect;
        _coreSparks = GameEngine.ContentResourceManager.GetParticleEffectInstance("CoreSparks", ParticleEffectType.Continual, null) as IStandardParticleEffect;
    }

    public override void Initialise(object data)
    {
        if (data != null)
        {
            _elements = data as CoreElements;
            _elements.OnCoreElementReplaced += elements_OnCoreElementReplaced;
        }
        else
        {
            GameEngine.GameScreens.ScreenNumber = WorldMap.CoreScreenNumber;

            // Add the collision map for the core elements
            GameEngine.GameScreens.CurrentGameScreenData.CollisionMap.AddRange(_elements.GetCollisionMap());

            var potentialLocations = GameEngine.GameScreens.GetNastyPotentialLocations();
            if (potentialLocations.Count < _coreAlienCount)
            {
                _coreAlienCount = potentialLocations.Count;
            }

            for (var a = 0; a < _coreAlienCount; a++)
            {
                _coreAliens[a].XPos = potentialLocations[a].X;
                _coreAliens[a].YPos = potentialLocations[a].Y;
                _coreAliens[a].NastyType = NastyType.CoreGuardian;
                _coreAliens[a].NastyState = NastyState.Normal;
            }
        }
    }

    private void elements_OnCoreElementReplaced()
    {
        if (_elementsToReplace != null &&
            _elementsToReplace.Count > 0)
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
        var elementsLeftToReplace = _elements.GetActiveElements().Count;
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