// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class CoreElement
{

    private readonly FloatSubtractor _currentElementDownScaler;
    private readonly IntegerSubtractor _elementFader;

    private readonly ElementFader _fader1;
    private readonly ElementFader _fader2;
    private readonly ElementFader _fader3;

    private readonly FloatAdder _finalElementUpScaler;

    private readonly GameEngine _gameEngine;
    private readonly IntegerSubtractor _screenFlashFader;
    private IStandardParticleEffect _coreReplacementEffect;

    private bool _elementReplacementStarted;

    private int _xPos;
    private int _yPos;

    public CoreElement(GameEngine gameEngineInstance, int currentElement, int finalElement, int fadeSpeed, bool startFadeUp)
    {
        _currentElementDownScaler = new FloatSubtractor(3, 1, 0.1f, false);
        _finalElementUpScaler = new FloatAdder(0, 1, 0.05f, false);
        _elementFader = new IntegerSubtractor(255, 0, 8, false);
        _screenFlashFader = new IntegerSubtractor(255, 0, 5, true);

        ElementState = CoreElementState.Missing;
        this.CurrentElement = currentElement;
        this.FinalElement = finalElement;
        _gameEngine = gameEngineInstance;

        if (startFadeUp)
        {
            _fader1 = new ElementFader(_gameEngine, Direction.Up);
            _fader2 = new ElementFader(_gameEngine, Direction.Down);
            _fader3 = new ElementFader(_gameEngine, Direction.Up);
        }
        else
        {
            _fader1 = new ElementFader(_gameEngine, Direction.Down);
            _fader2 = new ElementFader(_gameEngine, Direction.Up);
            _fader3 = new ElementFader(_gameEngine, Direction.Down);
        }

    }

    public CoreElementState ElementState { get; set; }

    public int CurrentElement { get; }

    public int FinalElement { get; }

    public event RaiseEffectComplete OnCoreElementReplaced;

    public void SetFadeToZero()
    {
        _fader1.SetFadeToZero();
        _fader2.SetFadeToZero();
        _fader3.SetFadeToZero();
    }

    public void LoadContent()
    {
        _coreReplacementEffect = _gameEngine.ContentResourceManager.GetParticleEffectInstance("CoreReplacementParticleEffect", ParticleEffectType.Continual, new TimeSpan(0, 0, 0, 0, 200)) as IStandardParticleEffect;
        _coreReplacementEffect.ParticleEffectComplete += coreReplacementEffect_ParticleEffectComplete;
    }

    private void coreReplacementEffect_ParticleEffectComplete()
    {
        // We're all done
        if (OnCoreElementReplaced != null)
        {
            OnCoreElementReplaced();
        }
    }

    public void Initialise()
    {

    }

    public void PerformPostChangeScreenActions()
    {

    }

    public void Update(GameTime gameTime)
    {
        if (ElementState == CoreElementState.Missing)
        {
            _fader1.Update();
            _fader2.Update();
            _fader3.Update();
        }
        else if (ElementState == CoreElementState.Replacing)
        {
            if (_elementReplacementStarted)
            {
                // Scale current element down
                _currentElementDownScaler.Update();
                _finalElementUpScaler.Update();
                if (_finalElementUpScaler.Modifier > 0.95)
                {
                    if (ElementState == CoreElementState.Replacing)
                    {
                        if (!_coreReplacementEffect.EffectActive)
                        {
                            _coreReplacementEffect.Start();
                        }
                    }

                    // Flash the screen white then fade out
                    _screenFlashFader.Trigger();
                }
                _elementFader.Update();
            }
        }
        if (_screenFlashFader.Triggered)
        {
            _gameEngine.BackgroundColour = ColourHelper.GetXna4Colour(_screenFlashFader.ByteModifier, _screenFlashFader.ByteModifier, _screenFlashFader.ByteModifier);
            _screenFlashFader.Update();
        }

        if (_elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            _coreReplacementEffect.Update(_xPos + 32, _yPos + 32, gameTime);
        }

    }

    public void Draw(int xPos, int yPos, int fade, GameTime gameTime)
    {
        if (ElementState == CoreElementState.Replaced)
        {
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.AlphaBlend, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)fade));
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();
        }
        else if (ElementState == CoreElementState.Missing)
        {
            _gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").Begin(BlendState.AlphaBlend, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)fade));
            _gameEngine.ContentResourceManager.GetSpriteCollection("AccessCodesGray").End();
        }
    }

    public void Draw(int xPos, int yPos, GameTime gameTime, bool replacingElement)
    {
        _xPos = xPos;
        _yPos = yPos;

        if (ElementState == CoreElementState.Missing)
        {
            if (replacingElement)
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, 50));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();
            }
            else
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_fader1.FadeLevel));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();

                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).Begin(BlendState.AlphaBlend, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_fader2.FadeLevel));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesFailedName).End();

                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).Begin(BlendState.AlphaBlend, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)_fader3.FadeLevel));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.AccessCodesPassedName).End();
            }
        }
        else if (ElementState == CoreElementState.Replacing)
        {
            if (!_elementReplacementStarted)
            {
                _elementReplacementStarted = true;
            }

            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsIlluminatedName).Begin(BlendState.Additive, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsIlluminatedName).DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, _elementFader.ByteModifier), _currentElementDownScaler.Modifier, 0);
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsIlluminatedName).End();

            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, (byte)(255 - _elementFader.ByteModifier)), _finalElementUpScaler.Modifier, 0);
            _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();
        }
        else if (ElementState == CoreElementState.Replaced)
        {
            if (replacingElement)
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, 50));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();
            }
            else
            {
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).Begin(BlendState.Additive, gameTime);
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(255, 255, 255, 255));
                _gameEngine.ContentResourceManager.GetSpriteCollection(_gameEngine.PickupsName).End();
            }
        }

        if (_elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            _coreReplacementEffect.Draw();
        }
    }

    internal class ElementFader
    {

        private readonly int _fadeSpeed;
        private Direction _fadeDirection;

        public ElementFader(GameEngine gameEngineInstance, Direction fadeDirection)
        {
            _fadeDirection = fadeDirection;
            _fadeSpeed = gameEngineInstance.RandomNumberGenerator.Next(8, 16);
            FadeLevel = gameEngineInstance.RandomNumberGenerator.Next(50, 200);
        }
        public int FadeLevel { get; private set; }

        public void SetFadeToZero()
        {
            FadeLevel = 0;
            _fadeDirection = Direction.Up;
        }

        public void Update()
        {
            if (_fadeDirection == Direction.Up)
            {
                FadeLevel = FadeLevel + _fadeSpeed;
            }
            else if (_fadeDirection == Direction.Down)
            {
                FadeLevel = FadeLevel - _fadeSpeed;
            }

            if (FadeLevel > 255)
            {
                FadeLevel = 255;
                _fadeDirection = Direction.Down;
            }
            else if (FadeLevel < 0)
            {
                FadeLevel = 0;
                _fadeDirection = Direction.Up;
            }

        }
    }
}