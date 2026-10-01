// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class CoreElement
{
    /// <summary>
    ///     How an element being put back is animated: the one the blob is carrying shrinks from three
    ///     times size down to normal while the replacement grows up to it, and the swap is treated as
    ///     finished once the incoming one is all but full size.
    /// </summary>
    private const float ElementShrinkFrom = 3;

    /// <inheritdoc cref="ElementShrinkFrom" />
    private const float ElementScaleTo = 1;

    /// <inheritdoc cref="ElementShrinkFrom" />
    private const float ElementShrinkStep = 0.1f;

    /// <inheritdoc cref="ElementShrinkFrom" />
    private const float ElementGrowStep = 0.05f;

    /// <inheritdoc cref="ElementShrinkFrom" />
    private const double ElementGrownEnough = 0.95;

    /// <summary>
    ///     How long the core's replacement effect runs for.
    /// </summary>
    private const int ReplacementEffectMilliseconds = 200;

    /// <summary>
    ///     How fast an element fades out once it has been put back, and how fast the flash that goes
    ///     with it clears.
    /// </summary>
    private const int ElementFadeStep = 8;

    /// <inheritdoc cref="ElementFadeStep" />
    private const int ScreenFlashFadeStep = 5;

    /// <summary>
    ///     How faintly a core element still to be replaced is drawn, so the gaps in the core read as
    ///     gaps without disappearing altogether.
    /// </summary>
    private const int MissingElementAlpha = 50;
    private readonly IStandardParticleEffect _coreReplacementEffect;


    private readonly FloatSubtractor _currentElementDownScaler;
    private readonly IntegerSubtractor _elementFader;

    private readonly ElementFader _fader1;
    private readonly ElementFader _fader2;
    private readonly ElementFader _fader3;

    private readonly FloatAdder _finalElementUpScaler;

    private readonly GraphicsSet _graphicsSet;
    private readonly PresentationSettings _presentation;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly IntegerSubtractor _screenFlashFader;

    private bool _elementReplacementStarted;

    private int _xPos;
    private int _yPos;

    public CoreElement(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        PresentationSettings presentation,
        Random randomNumberGenerator,
        int currentElement,
        int finalElement,
        bool startFadeUp)
    {
        _currentElementDownScaler = new FloatSubtractor(ElementShrinkFrom, ElementScaleTo, ElementShrinkStep, false);
        _finalElementUpScaler = new FloatAdder(0, ElementScaleTo, ElementGrowStep, false);
        _elementFader = new IntegerSubtractor(ColourLevels.Full, ColourLevels.None, ElementFadeStep, false);
        _screenFlashFader = new IntegerSubtractor(ColourLevels.Full, ColourLevels.None, ScreenFlashFadeStep, true);

        ElementState = CoreElementState.Missing;
        CurrentElement = currentElement;
        FinalElement = finalElement;
        _resources = resources;
        _graphicsSet = graphicsSet;
        _presentation = presentation;
        _randomNumberGenerator = randomNumberGenerator;

        if (startFadeUp)
        {
            _fader1 = new ElementFader(_randomNumberGenerator, Direction.Up);
            _fader2 = new ElementFader(_randomNumberGenerator, Direction.Down);
            _fader3 = new ElementFader(_randomNumberGenerator, Direction.Up);
        }
        else
        {
            _fader1 = new ElementFader(_randomNumberGenerator, Direction.Down);
            _fader2 = new ElementFader(_randomNumberGenerator, Direction.Up);
            _fader3 = new ElementFader(_randomNumberGenerator, Direction.Down);
        }

        _coreReplacementEffect = (IStandardParticleEffect)_resources.GetParticleEffectInstance("CoreReplacementParticleEffect", ParticleEffectType.Continual, new TimeSpan(0, 0, 0, 0, ReplacementEffectMilliseconds));
        _coreReplacementEffect.ParticleEffectComplete += CoreReplacementEffect_ParticleEffectComplete;
    }

    public CoreElementState ElementState { get; set; }

    public int CurrentElement { get; }

    private int FinalElement { get; }

    public event RaiseEffectComplete? OnCoreElementReplaced;

    public void SetFadeToZero()
    {
        _fader1.SetFadeToZero();
        _fader2.SetFadeToZero();
        _fader3.SetFadeToZero();
    }

    private void CoreReplacementEffect_ParticleEffectComplete()
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
                if (_finalElementUpScaler.Modifier > ElementGrownEnough)
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
            _presentation.BackgroundColour = ColourHelper.GetXna4Colour(_screenFlashFader.ByteModifier, _screenFlashFader.ByteModifier, _screenFlashFader.ByteModifier);
            _screenFlashFader.Update();
        }

        if (_elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            _coreReplacementEffect.Update(_xPos + SpriteMetrics.HalfSpriteWidth, _yPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }

    }

    public void Draw(int xPos, int yPos, int fade)
    {
        if (ElementState == CoreElementState.Replaced)
        {
            SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
            pickups.Begin(BlendState.AlphaBlend);
            pickups.DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)fade));
            pickups.End();
        }
        else if (ElementState == CoreElementState.Missing)
        {
            SpriteCollection accessCodesGray = _resources.GetSpriteCollection("AccessCodesGray");
            accessCodesGray.Begin(BlendState.AlphaBlend);
            accessCodesGray.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)fade));
            accessCodesGray.End();
        }
    }

    public void Draw(int xPos, int yPos, bool replacingElement)
    {
        _xPos = xPos;
        _yPos = yPos;

        if (ElementState == CoreElementState.Missing)
        {
            if (replacingElement)
            {
                SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
                pickups.Begin(BlendState.Additive);
                pickups.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MissingElementAlpha));
                pickups.End();
            }
            else
            {
                SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
                pickups.Begin(BlendState.Additive);
                pickups.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_fader1.FadeLevel));
                pickups.End();

                SpriteCollection accessCodesFailed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesFailed);
                accessCodesFailed.Begin(BlendState.AlphaBlend);
                accessCodesFailed.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_fader2.FadeLevel));
                accessCodesFailed.End();

                SpriteCollection accessCodesPassed = _resources.GetSpriteCollection(_graphicsSet.Names.AccessCodesPassed);
                accessCodesPassed.Begin(BlendState.AlphaBlend);
                accessCodesPassed.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)_fader3.FadeLevel));
                accessCodesPassed.End();
            }
        }
        else if (ElementState == CoreElementState.Replacing)
        {
            if (!_elementReplacementStarted)
            {
                _elementReplacementStarted = true;
            }

            SpriteCollection pickupsIlluminated = _resources.GetSpriteCollection(_graphicsSet.Names.PickupsIlluminated);
            pickupsIlluminated.Begin(BlendState.Additive);
            pickupsIlluminated.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, _elementFader.ByteModifier), _currentElementDownScaler.Modifier, 0);
            pickupsIlluminated.End();

            SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
            pickups.Begin(BlendState.Additive);
            pickups.DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)(ColourLevels.Full - _elementFader.ByteModifier)), _finalElementUpScaler.Modifier, 0);
            pickups.End();
        }
        else if (ElementState == CoreElementState.Replaced)
        {
            if (replacingElement)
            {
                SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
                pickups.Begin(BlendState.Additive);
                pickups.DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, MissingElementAlpha));
                pickups.End();
            }
            else
            {
                SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
                pickups.Begin(BlendState.Additive);
                pickups.DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ColourLevels.Full));
                pickups.End();
            }
        }

        if (_elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            _coreReplacementEffect.Draw();
        }
    }
}
