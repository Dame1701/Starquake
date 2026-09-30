// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class ConveyerNasty : GameComponentBase
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


    private readonly Animation _anim;
    private readonly int _screenShakeXOffset = 0;
    private readonly int _screenShakeYOffset = 0;

    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    public int XLocation { get; set; }

    public NastyState NastyState { get; set; }

    public int YLocation { get; set; }

    public int MoveSpeed { get; set; }

    public bool NastyActive { get; set; }

    public int AnimationDelay
    {
        get => _anim.Delay;
        set => _anim.Delay = value;
    }

    #region IGameComponent Members

    private readonly SpriteCollection _nastySprite;
    private readonly IStandardParticleEffect _conveyerNastyDies;

    private Direction _nastyDirection;

    public ConveyerNasty(
        GameEngine gameEngineReference,
        int xposition,
        int yposition,
        int moveSpeed,
        bool active,
        ContentList content) : base(gameEngineReference)
    {
        XLocation = xposition;
        YLocation = yposition;
        this.MoveSpeed = moveSpeed;
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

        _nastySprite = new SpriteCollection(GameEngine.GraphicsDeviceMgr, GameEngine.ContentMgr, content.GetContent("conveyerNasty"), SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);
        _conveyerNastyDies = (IStandardParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("NastyDies", ParticleEffectType.Triggered, null);
        _conveyerNastyDies.ParticleEffectComplete += conveyerNastyDies_ParticleEffectComplete;
    }

    private void conveyerNastyDies_ParticleEffectComplete()
    {
        NastyActive = false;
    }

    public void Initialise()
    {
        NastyState = NastyState.Conception;
        NastyActive = true;

        var randomDirection = GameEngine.RandomNumberGenerator.Next(0, 2);

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
        if (_conveyerNastyDies != null)
        {
            _conveyerNastyDies.Stop();
        }

        NastyActive = false;
    }

    public override void Update(GameTime gameTime)
    {
        if (_conveyerNastyDies != null)
        {
            // Update the explosion
            _conveyerNastyDies.Update(XLocation + _screenShakeXOffset + SpriteMetrics.HalfSpriteWidth, YLocation + _screenShakeYOffset + SpriteMetrics.SpriteHeight, gameTime);
        }

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
                var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Left, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
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
                var amountToMove = GameEngine.GameScreens.CheckCollision(Direction.Right, XLocation, YLocation + HangsBelowRailBy, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight, MoveSpeed, false);
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

            if (_conveyerNastyDies != null)
            {
                _conveyerNastyDies.Start();
            }

            NastyState = NastyState.Dead;
            NastyActive = false;
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (NastyState == NastyState.Normal)
        {
            _nastySprite.Begin(BlendState.AlphaBlend, gameTime);
            _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
            _nastySprite.End();
        }
        else if (NastyState == NastyState.Dead)
        {
            // Draw the explosion
            if (_conveyerNastyDies != null)
            {
                _conveyerNastyDies.Draw();
            }

            // Fade the nasty out as it explodes
            if (_deathFadeLevel > 0)
            {
                _deathFadeLevel = _deathFadeLevel - 20;
            }

            if (_deathFadeLevel < 0)
            {
                _deathFadeLevel = 0;
            }

            // Shrink the nasty as it explodes
            if (_deathScale > 0)
            {
                _deathScale = _deathScale - DeathShrinkStep;
            }

            if (_deathScale < 0)
            {
                _deathScale = 0;
            }

            if (_deathFadeLevel > 0)
            {
                _nastySprite.Begin(BlendState.AlphaBlend, SpriteSortMode.BackToFront, gameTime);
                _nastySprite.DrawSprite(_anim.CurrentFrame, XLocation, YLocation + HangsBelowRailBy, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale, 0f);
                _nastySprite.End();
            }
        }
    }

    #endregion

}