// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.ActiveSprites;

internal class ConveyorNasty
{

    /// <summary>
    ///     A conveyor nasty hangs from the rail it runs along, so it is drawn and tested a collision
    ///     cell below the position it is tracked at.
    /// </summary>
    private const int HangsBelowRailBy = TileMetrics.CollisionCellHeight;


    private readonly Animation _anim;
    private readonly IStandardParticleEffect _conveyorNastyDies;
    private readonly GameScreenCollection _gameScreens;

    /// <summary>
    ///     Called when this nasty blows apart, so the picture can be shaken. A callback rather than
    ///     the thing that does the shaking, which a nasty has no business holding.
    /// </summary>
    private readonly Action _onExploded;

    private readonly SpriteCollection _nastySprite;
    private readonly Random _randomNumberGenerator;

    /// <summary>
    ///     The ring it throws as it goes off. The same blast a spiky nasty makes, because both are
    ///     machines and explode the same way.
    /// </summary>
    private readonly Shockwave _shockwave;

    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    private Direction _nastyDirection;

    public ConveyorNasty(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        Random randomNumberGenerator,
        Action onExploded,
        int xposition,
        int yposition,
        int moveSpeed,
        bool active)
    {
        _gameScreens = gameScreens;
        _randomNumberGenerator = randomNumberGenerator;
        _onExploded = onExploded;

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

        _nastySprite = resources.GetSpriteCollection(Assets.Graphics.ConveyorNasty);
        _conveyorNastyDies = (IStandardParticleEffect)resources.GetParticleEffectInstance(Assets.Effects.NastyDies, ParticleEffectType.Triggered, null);
        _shockwave = new Shockwave(
            resources, randomNumberGenerator, ShockwaveSettings.NastyDiameter, ShockwaveSettings.NastyFrames);
        _conveyorNastyDies.ParticleEffectComplete += ConveyorNastyDies_ParticleEffectComplete;
    }

    public int XLocation { get; set; }

    public NastyState NastyState { get; set; }

    public int YLocation { get; set; }

    public int MoveSpeed { get; set; }

    public bool NastyActive { get; set; }

    public int AnimationDelay
    {
        set => _anim.Delay = value;
    }

    private void ConveyorNastyDies_ParticleEffectComplete()
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
        _conveyorNastyDies.Stop();
        _shockwave.Stop();

        NastyActive = false;
    }

    public void Update(GameTime gameTime)
    {
        _conveyorNastyDies.Update(XLocation + SpriteMetrics.HalfSpriteWidth, YLocation + SpriteMetrics.SpriteHeight, gameTime);
        _shockwave.Update();

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
                int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Left, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation -= amountToMove;
                }
                else
                {
                    _nastyDirection = Direction.Right;
                    _anim.AnimateForwards = true;
                }
            }
            else if (_nastyDirection == Direction.Right)
            {
                int amountToMove = _gameScreens.Collisions.CheckCollision(Direction.Right, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
                if (amountToMove > 0)
                {
                    XLocation += amountToMove;
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

            _conveyorNastyDies.Start();
            _shockwave.Trigger(XLocation + SpriteMetrics.HalfSpriteWidth, YLocation + SpriteMetrics.SpriteHeight);

            // A machine, so it explodes like one, using the same effect the spiky nasties throw.
            // The jolt is raised through the callback, since this is not a Nasty.
            _onExploded();

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
    ///     takes the same time on every machine rather than following the display's refresh rate.
    /// </summary>
    private void FadeAndShrinkTheCorpse()
    {
        _deathFadeLevel = Ramp.Down(_deathFadeLevel, MachineDeathSettings.FadeStep, ColourLevels.None);
        _deathScale = Ramp.Down(_deathScale, MachineDeathSettings.ShrinkStep, 0f);
    }

    public void Draw(int screenFade)
    {
        if (NastyState == NastyState.Normal)
        {
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, Colours.WhiteFadedTo((byte)screenFade));
            _nastySprite.End();
        }
        else if (NastyState == NastyState.Dead)
        {
            // Under the explosion, so the flames and sparks come out of the ring rather than
            // through it.
            _shockwave.Draw(screenFade);

            _conveyorNastyDies.Draw();

            if (_deathFadeLevel > 0)
            {
                _nastySprite.Begin(BlendState.AlphaBlend);
                _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, Colours.WhiteFadedTo((byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale, 0f);
                _nastySprite.End();
            }
        }
    }
}
