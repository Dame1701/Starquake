// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class CoreElement
{
    /// <summary>
    ///     How an element being put back is animated: the one Blob is carrying shrinks from three
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

    /// <summary>
    ///     How faintly a core element still to be replaced is drawn, so the gaps in the core read as
    ///     gaps without disappearing altogether.
    /// </summary>
    private const int MissingElementAlpha = 50;
    private readonly IStandardParticleEffect _coreReplacementEffect;


    private readonly RampedValue _currentElementDownScaler;
    private readonly RampedValue _elementFader;

    private readonly ElementFader _fader1;
    private readonly ElementFader _fader2;
    private readonly ElementFader _fader3;

    private readonly RampedValue _finalElementUpScaler;

    private readonly GraphicsSet _graphicsSet;
    private readonly ResourceManager _resources;

    private bool _elementReplacementStarted;

    private int _xPos;
    private int _yPos;

    public CoreElement(
        ResourceManager resources,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator,
        int currentElement,
        int finalElement,
        bool startFadeUp)
    {
        _currentElementDownScaler = new RampedValue(ElementShrinkFrom, ElementScaleTo, ElementShrinkStep);
        _finalElementUpScaler = new RampedValue(0, ElementScaleTo, ElementGrowStep);
        _elementFader = new RampedValue(ColourLevels.Full, ColourLevels.None, ElementFadeStep);

        ElementState = CoreElementState.Missing;
        CurrentElement = currentElement;
        FinalElement = finalElement;
        _resources = resources;
        _graphicsSet = graphicsSet;

        if (startFadeUp)
        {
            _fader1 = new ElementFader(randomNumberGenerator, Direction.Up);
            _fader2 = new ElementFader(randomNumberGenerator, Direction.Down);
            _fader3 = new ElementFader(randomNumberGenerator, Direction.Up);
        }
        else
        {
            _fader1 = new ElementFader(randomNumberGenerator, Direction.Down);
            _fader2 = new ElementFader(randomNumberGenerator, Direction.Up);
            _fader3 = new ElementFader(randomNumberGenerator, Direction.Down);
        }

        _coreReplacementEffect = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.CoreReplacementParticleEffect, ParticleEffectType.Continual, new TimeSpan(0, 0, 0, 0, ReplacementEffectMilliseconds));
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

    /// <summary>
    ///     One logic frame of this element. The position is passed in because the replacement effect is
    ///     emitted from the element's centre - drawing used to latch it into a field for this to read,
    ///     which left the effect playing at wherever the element was last drawn.
    /// </summary>
    public void Update(GameTime gameTime, int xPos, int yPos)
    {
        _xPos = xPos;
        _yPos = yPos;

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
                if (_finalElementUpScaler.Value > ElementGrownEnough)
                {
                    if (ElementState == CoreElementState.Replacing)
                    {
                        if (!_coreReplacementEffect.EffectActive)
                        {
                            _coreReplacementEffect.Start();
                        }
                    }
                }
                _elementFader.Update();
            }
        }

        if (_elementReplacementStarted && ElementState == CoreElementState.Replacing)
        {
            _coreReplacementEffect.Update(_xPos + SpriteMetrics.HalfSpriteWidth, _yPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }

        // Latched at the end of the first logic frame of the replacement, so everything above starts
        // on the second one. Drawing used to set it, at the same point in the frame but on the draw
        // path - which made a logic gate wait on a frame having been drawn.
        if (ElementState == CoreElementState.Replacing)
        {
            _elementReplacementStarted = true;
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
            SpriteCollection accessCodesGray = _resources.GetSpriteCollection(Assets.Graphics.AccessCodesGray);
            accessCodesGray.Begin(BlendState.AlphaBlend);
            accessCodesGray.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)fade));
            accessCodesGray.End();
        }
    }

    public void Draw(int xPos, int yPos, bool replacingElement)
    {
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
            SpriteCollection pickupsIlluminated = _resources.GetSpriteCollection(_graphicsSet.Names.PickupsIlluminated);
            pickupsIlluminated.Begin(BlendState.Additive);
            pickupsIlluminated.DrawSprite(CurrentElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, _elementFader.ColourLevel), _currentElementDownScaler.Value, 0);
            pickupsIlluminated.End();

            SpriteCollection pickups = _resources.GetSpriteCollection(_graphicsSet.Names.Pickups);
            pickups.Begin(BlendState.Additive);
            pickups.DrawSprite(FinalElement, xPos, yPos, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, ColourLevels.Full - _elementFader.ColourLevel), _finalElementUpScaler.Value, 0);
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
