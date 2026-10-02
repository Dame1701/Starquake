// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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

internal class ConveyerNasty
{

    /// <summary>
    ///     A conveyer nasty hangs from the rail it runs along, so it is drawn and tested a collision
    ///     cell below the position it is tracked at.
    /// </summary>
    private const int HangsBelowRailBy = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How quickly a shot conveyer nasty shrinks away.
    /// </summary>
    private const float DeathShrinkStep = 0.01f;

    /// <inheritdoc cref="DeathShrinkStep" />
    private const int DeathFadeStep = 20;


    private readonly Animation _anim;
    private readonly GameScreenCollection _gameScreens;
    private readonly Random _randomNumberGenerator;

    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    public int XLocation { get; set; }

    public NastyState NastyState { get; set; }

    public int YLocation { get; set; }

    public int MoveSpeed { get; set; }

    public bool NastyActive { get; set; }

    public int AnimationDelay
    {
        set => _anim.Delay = value;
    }

    private readonly SpriteCollection _nastySprite;
    private readonly IStandardParticleEffect _conveyerNastyDies;

    private Direction _nastyDirection;

    public ConveyerNasty(
        ResourceManager resources,
        ContentManager contentManager,
        GameScreenCollection gameScreens,
        GraphicsDeviceManager graphics,
        Random randomNumberGenerator,
        int xposition,
        int yposition,
        int moveSpeed,
        bool active,
        ContentList content)
    {
        _gameScreens = gameScreens;
        _randomNumberGenerator = randomNumberGenerator;

        XLocation = xposition;
        YLocation = yposition;
        MoveSpeed = moveSpeed;
        NastyActive = active;
        _anim = new Animation();
        _anim.FirstFrame = 0;
        _anim.LastFrame = 5;
        _anim.Repeat = true;
        _anim.Yoyo = false;
        _anim.Delay = 2;
        _anim.AnimateForwards = true;
        NastyState = NastyState.Conception;

        _deathFadeLevel = ColourLevels.Full;
        _deathScale = 1f;

        _nastySprite = new SpriteCollection(graphics, contentManager, content.GetContent("conveyerNasty"), SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        _conveyerNastyDies = (IStandardParticleEffect)resources.GetParticleEffectInstance(Assets.Effects.NastyDies, ParticleEffectType.Triggered, null);
        _conveyerNastyDies.ParticleEffectComplete += ConveyerNastyDies_ParticleEffectComplete;
    }

    private void ConveyerNastyDies_ParticleEffectComplete()
    {
        NastyActive = false;
    }

    public void Initialise()
    {
        NastyState = NastyState.Conception;
        NastyActive = true;

        int randomDirection = _randomNumberGenerator.Next(0, 2);

        if (randomDirection == 0)
        {
            _nastyDirection = Direction.Left;
        }
        else
        {
            _nastyDirection = Direction.Right;
        }
    }

    public void PerformPostChangeScreenActions()
    {
        _conveyerNastyDies.Stop();

        NastyActive = false;
    }

    public void Update(GameTime gameTime)
    {
        // Update the explosion
        _conveyerNastyDies.Update(XLocation + SpriteMetrics.HalfSpriteWidth, YLocation + SpriteMetrics.SpriteHeight, gameTime);

        if (NastyState == NastyState.Conception)
        {
            _deathFadeLevel = ColourLevels.Full;
            _deathScale = 1f;
            NastyState = NastyState.Normal;
        }
        else if (NastyState == NastyState.Normal)
        {
            if (_nastyDirection == Direction.Left)
            {
                int amountToMove = _gameScreens.CheckCollision(Direction.Left, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation = XLocation - amountToMove;
                }
                else
                {
                    _nastyDirection = Direction.Right;
                    _anim.AnimateForwards = true;
                }
            }
            else if (_nastyDirection == Direction.Right)
            {
                int amountToMove = _gameScreens.CheckCollision(Direction.Right, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation = XLocation + amountToMove;
                }
                else
                {
                    _nastyDirection = Direction.Left;
                    _anim.AnimateForwards = false;
                }
            }
            _anim.GetNextFrame();
        }
        else if (NastyState == NastyState.Shot)
        {
            _deathFadeLevel = ColourLevels.Full;
            _deathScale = 1.0f;

            _conveyerNastyDies.Start();

            NastyState = NastyState.Dead;
            NastyActive = false;
        }
        else if (NastyState == NastyState.Dead)
        {
            FadeAndShrinkTheCorpse();
        }
    }

    /// <summary>
    ///     The corpse fading and shrinking away under the explosion. One logic frame's worth, so it
    ///     takes the same time on every machine - it used to be done while drawing, which tied it to
    ///     the display's refresh rate.
    /// </summary>
    private void FadeAndShrinkTheCorpse()
    {
        if (_deathFadeLevel > 0)
        {
            _deathFadeLevel = _deathFadeLevel - DeathFadeStep;
        }

        if (_deathFadeLevel < 0)
        {
            _deathFadeLevel = 0;
        }

        if (_deathScale > 0)
        {
            _deathScale = _deathScale - DeathShrinkStep;
        }

        if (_deathScale < 0)
        {
            _deathScale = 0;
        }
    }

    public void Draw(int screenFade)
    {
        if (NastyState == NastyState.Normal)
        {
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
            _nastySprite.End();
        }
        else if (NastyState == NastyState.Dead)
        {
            // Draw the explosion
            _conveyerNastyDies.Draw();

            if (_deathFadeLevel > 0)
            {
                _nastySprite.Begin(BlendState.AlphaBlend);
                _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale, 0f);
                _nastySprite.End();
            }
        }
    }

}
