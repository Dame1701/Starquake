// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.ActiveSprites;
using StarquakeXNA.ContentManagement;
using StarquakeXNA.Display;
using StarquakeXNA.Engines;
using StarquakeXNA.Helpers;
using StarquakeXNA.Particles;
using StarquakeXNA.Structure;

namespace StarquakeXNA.GameComponents.GameScreens;

public class PlanetCoreScreen : GameComponentBase, IRealTimeGameScreen
{
    private int _blankingTilesOpacity;
    private int _coreAlienCount = 4;

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
            _coreSparks.Update(512, 288, gameTime);
        }

        if (_coreComputationEffect != null)
        {
            _coreComputationEffect.Start();
            _coreComputationEffect.Update(512, 288, gameTime);
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_justEnteredCore)
        {
            _blankingTilesOpacity = 255;
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
                _blankingTilesOpacity = _blankingTilesOpacity - 16;
            }

            if (_blankingTilesOpacity < 0)
            {
                _blankingTilesOpacity = 0;
            }
        }

        // Leave the core room
        if (!_replacingElements && _entryTime > 240)
        {
            GameEngine.GameScreens.ScreenNumber = 198;
            GameEngine.CurrentGameScreen = GameScreenType.MainGame;
            GameEngine.Player.BlobXPos = GameEngine.GameScreens.ScreenWidth - GameEngine.Player.BlobWidth;
            GameEngine.GetGameScreen(GameScreenType.MainGame).PerformPostScreenChangeActions(GameScreenType.PlanetCore, null);

            _justEnteredCore = true;
            GameEngine.BackgroundColour = Color.Black;
        }
        else if (!_replacingElements && GameEngine.Elements.GetElementsReplacedCount() == 9)
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
            for (var a = 0; a < 6; a++)
            {
                for (var b = 0; b < 6; b++)
                {
                    GameEngine.ContentResourceManager.GetSpriteCollection("BlankingTile").DrawSprite(0, b * 128 + 128, a * 96, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_blankingTilesOpacity));
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
                _coreAliens[a].Draw(gameTime, (byte)(255 - _blankingTilesOpacity));
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
                9);

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
            GameEngine.GameScreens.ScreenNumber = 199;

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
        if (elementsLeftToReplace > 8)
        {
            _coreAlienCount = 0;
        }
        else if (elementsLeftToReplace <= 8 && elementsLeftToReplace > 6)
        {
            _coreAlienCount = 1;
        }
        else if (elementsLeftToReplace <= 6 && elementsLeftToReplace > 4)
        {
            _coreAlienCount = 2;
        }
        else if (elementsLeftToReplace <= 4 && elementsLeftToReplace > 2)
        {
            _coreAlienCount = 3;
        }
        else if (elementsLeftToReplace < 2)
        {
            _coreAlienCount = 4;
        }
    }
}