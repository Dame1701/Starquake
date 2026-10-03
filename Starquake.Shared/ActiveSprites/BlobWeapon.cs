// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Animations;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Scoring;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     Blob's lazer: what decides a shot is fired, which way it leaves, and everything that is
///     drawn because one is in flight. <see cref="BlobFire" /> is the shot itself and knows nothing but
///     its own position; this is the end that holds the trigger.
/// </summary>
/// <remarks>
///     The two halves were split apart for the same reason, one at a time. The shot came out first, and
///     <see cref="IFireSurroundings" /> is the five questions it has to ask of the world - which is what
///     let its stepping rules be tested with no screen and no graphics device. This half stayed on
///     <see cref="Blob" />, as nine fields and seven methods among fifty others, so the rule deciding
///     which way a shot leaves the shuttle could not be reached without a whole Blob.
/// </remarks>
internal sealed class BlobWeapon
{
    /// <summary>
    ///     Where the shot starts relative to Blob's corner, and where the trail is emitted from
    ///     relative to the shot's. Both are about a quarter of a sprite in.
    /// </summary>
    private const int FireOffset = 16;

    /// <inheritdoc cref="FireOffset" />
    private const int FireTrailOffset = 14;

    /// <summary>
    ///     How fast the shot's additive glow comes up to full, in colour levels a logic frame.
    /// </summary>
    private const int FireFadeStep = 16;

    /// <summary>
    ///     How fast the shuttle's flame turns, in degrees a logic frame, and one whole revolution -
    ///     which is where the angle wraps.
    /// </summary>
    private const float ShuttleFireRotateSpeed = 10f;

    /// <inheritdoc cref="ShuttleFireRotateSpeed" />
    private const float FullTurn = 360f;

    /// <summary>
    ///     The two flame cycles: the one drawn on foot, and the slower, longer one drawn on the shuttle.
    /// </summary>
    private const int FireAnimLastFrame = 3;

    /// <inheritdoc cref="FireAnimLastFrame" />
    private const int ShuttleFireAnimLastFrame = 23;

    /// <inheritdoc cref="FireAnimLastFrame" />
    private const int AnimationDelay = 2;

    /// <inheritdoc cref="FireAnimLastFrame" />
    private const int ShuttleFireAnimationDelay = 1;

    private readonly Animation _animFire1;
    private readonly Animation _animFire2;
    private readonly BlobFire _fire;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    /// <summary>
    ///     Raised when a shot leaves, which is how the score hears about it. The weapon is told how to
    ///     say so rather than holding the thing it says it to.
    /// </summary>
    private readonly Action _onFired;

    private readonly ResourceManager _resources;
    private readonly IPositionalTriggerParticleEffect _sparks;
    private readonly PlayerStats _stats;
    private readonly IStandardParticleEffect _trail;

    private bool _canFire = true;

    private Direction _directionFired = Direction.Right;

    private int _fade;

    /// <summary>
    ///     Whether Blob was on the shuttle as the shot left. Latched while nothing is in flight, so
    ///     a shot keeps flying the way the shuttle fires even if Blob gets off behind it.
    /// </summary>
    private bool _onShuttleWhenFired;

    private float _rotation;

    public BlobWeapon(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        PlayerStats stats,
        Random randomNumberGenerator,
        Action onFired)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _stats = stats;
        _onFired = onFired;

        _trail = (IStandardParticleEffect)_resources.GetParticleEffectInstance(
            Assets.Effects.FireTrail, ParticleEffectType.Continual, null);

        _sparks = (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance(
            Assets.Effects.FireSparks, ParticleEffectType.TriggeredMultiLocationTimed, null);

        // The sparks rather than Blob, which is what FireSurroundings used to be handed for this one
        // call - and the only reason Blob had to give out a reference to itself mid-construction.
        _fire = new BlobFire(new FireSurroundings(gameScreens, randomNumberGenerator, _sparks));

        _animFire1 = Animation.Looping(0, FireAnimLastFrame, AnimationDelay, true);
        _animFire2 = Animation.Looping(0, ShuttleFireAnimLastFrame, ShuttleFireAnimationDelay, true);
    }

    /// <summary>
    ///     Which way a shot leaves. Riding the shuttle he fires whichever way he is moving, including up
    ///     and down; on foot a shot only ever goes left or right, so standing still - or having last
    ///     moved up or down a lift - he fires the way he last walked.
    /// </summary>
    /// <remarks>
    ///     Static and pure, which is the only part of the weapon that can be: the rest of it needs two
    ///     particle effects and a sprite sheet out of the resource manager before it exists.
    /// </remarks>
    public static Direction DirectionOfShot(Direction directionBlobMoved, bool onShuttle, Direction lastFired)
    {
        if (onShuttle)
        {
            return directionBlobMoved;
        }

        return directionBlobMoved is Direction.Left or Direction.Right
            ? directionBlobMoved
            : lastFired;
    }

    /// <summary>Whether a shot is in flight, which is what a nasty checks before testing for a hit.</summary>
    public bool HasFired { get; private set; }

    /// <inheritdoc cref="BlobFire.X" />
    public int X => _fire.X;

    /// <inheritdoc cref="BlobFire.Y" />
    public int Y => _fire.Y;

    /// <summary>
    ///     The shot hit something. Called by whatever was hit, which is why it does not decide anything
    ///     itself - the nasty knows whether it was struck and the weapon only has to stop flying.
    /// </summary>
    public void RegisterHit()
    {
        HasFired = false;
        _canFire = true;
    }

    /// <summary>
    ///     One logic frame of the weapon: fire if asked and able, carry any shot already in flight, and
    ///     settle the effects once nothing is flying.
    /// </summary>
    /// <param name="firePressed">Whether the fire control is down this frame.</param>
    /// <param name="blob">Where Blob is and how big he is, which is where a shot starts from.</param>
    /// <param name="directionBlobMoved">The way he last moved, which is the way a shot goes.</param>
    /// <param name="onShuttle">Whether he is riding the shuttle, which fires in all four directions.</param>
    public void Step(bool firePressed, Rectangle blob, Direction directionBlobMoved, bool onShuttle)
    {
        StartFiring(firePressed, blob, directionBlobMoved, onShuttle);
        CarryTheShot(blob);
        SettleEffectsWhenNothingIsFlying(onShuttle);
    }

    /// <summary>
    ///     The particle effects, which run on wall-clock time inside the particle engine rather than on
    ///     the frame counter, so they are stepped with the frame's elapsed time.
    /// </summary>
    public void UpdateEffects(GameTime gameTime)
    {
        _sparks.Update(gameTime);
        _trail.Update(X + FireTrailOffset, Y + FireTrailOffset, gameTime);
    }

    /// <summary>
    ///     The shot's trail, drawn under Blob rather than over him - which is why it is not part of
    ///     <see cref="Draw" />.
    /// </summary>
    public void DrawTrail() => _trail.Draw();

    /// <summary>
    ///     The flame at the head of the shot. Only the redrawn artwork draws one on foot; the original
    ///     draws nothing unless Blob is on the shuttle.
    /// </summary>
    public void Draw(int screenFade, bool onShuttle)
    {
        if (!HasFired)
        {
            return;
        }

        if (_graphicsSet.IsOriginal)
        {
            if (onShuttle)
            {
                DrawFlame(Assets.Graphics.BlobFire2Original, _animFire1.CurrentFrame, Y, ColourLevels.Full,
                    screenFade, 0f);
            }
            else
            {
                DrawFlame(Assets.Graphics.BlobFire1, _animFire1.CurrentFrame, Y - FireOffset, ColourLevels.Full,
                    screenFade, 0f);
            }
        }
        else if (onShuttle)
        {
            DrawFlame(Assets.Graphics.BlobFire2, _animFire2.CurrentFrame, Y, _fade, screenFade,
                MathHelper.ToRadians(_rotation));
        }
    }

    /// <summary>
    ///     The sparks a deflected shot throws off. Drawn with the upper layer, over the scenery, because
    ///     a shot is deflected by the scenery it hits.
    /// </summary>
    public void DrawSparks(int screenFade) => _sparks.Draw(screenFade);

    /// <summary>
    ///     Puts the weapon away with nothing in flight and the trail stopped, for Blob that is in the
    ///     middle of dying.
    /// </summary>
    public void Disarm()
    {
        HasFired = false;
        _canFire = true;
        _trail.Stop();
    }

    /// <summary>
    ///     Ready to fire again from where Blob now is. A shot does not carry across a screen change -
    ///     it would arrive mid-flight on a screen it was never fired on.
    /// </summary>
    public void ResetTo(int blobXPos, int blobYPos)
    {
        HasFired = false;
        _canFire = true;

        _fire.Launch(blobXPos + FireOffset, blobYPos + FireOffset);
        _fade = 0;
    }

    /// <summary>
    ///     Clears the particles still in the air. Separate from <see cref="ResetTo" /> because Blob
    ///     moves between the two - it is put through the tunnel it walked into first.
    /// </summary>
    public void ResetEffects()
    {
        _sparks.Reset();
        _trail.Reset();
    }

    /// <remarks>
    ///     The three nested conditions and the two-armed direction test are kept exactly as they were
    ///     found. Flattening them is one of <c>REFACTORING.md</c>'s open follow-ups, and a move is the
    ///     wrong commit to spend the risk of inverting a guard in.
    /// </remarks>
    private void StartFiring(bool firePressed, Rectangle blob, Direction directionBlobMoved, bool onShuttle)
    {
        if (firePressed)
        {
            if (_stats.FirePower > 0)
            {
                if (_canFire)
                {
                    // Play sound
                    _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobLazer);

                    _fire.Launch(blob.X + FireOffset, blob.Y + FireOffset);
                    _fade = 0;

                    _canFire = false;
                    HasFired = true;

                    _onFired();

                    _directionFired = DirectionOfShot(directionBlobMoved, onShuttle, _directionFired);
                }
            }
        }
    }

    private void CarryTheShot(Rectangle blob)
    {
        if (!HasFired)
        {
            return;
        }

        _trail.Start();

        if (!_fire.Advance(_directionFired, _onShuttleWhenFired, PointBlankReach(blob)))
        {
            HasFired = false;
            _canFire = true;
        }

        _animFire1.GetNextFrame();
        _animFire2.GetNextFrame();

        AdvanceAppearance();
    }

    /// <summary>
    ///     How the shot looks this frame: its additive glow comes up to full over the first few frames of
    ///     flight so it does not appear at full brightness on the frame it is launched, and the shuttle's
    ///     flame turns as it goes.
    /// </summary>
    /// <remarks>
    ///     Both of these were done while drawing, which ran them at the display's refresh rate rather
    ///     than the logic clock. The angle was the sprite sheet's own and advanced once per draw batch.
    /// </remarks>
    private void AdvanceAppearance()
    {
        // Not on the frame the shot expires. Advance ran after the step that can end the flight, and
        // this used to carry its own guard on having fired - so a dying shot advanced its animations
        // and not its glow.
        if (!HasFired)
        {
            return;
        }

        if (_fade < ColourLevels.Full)
        {
            _fade += FireFadeStep;
        }

        if (_fade > ColourLevels.Full)
        {
            _fade = ColourLevels.Full;
        }

        _rotation = _rotation < FullTurn
            ? _rotation + ShuttleFireRotateSpeed
            : 0f;
    }

    private void SettleEffectsWhenNothingIsFlying(bool onShuttle)
    {
        if (!HasFired)
        {
            _trail.Stop();

            _onShuttleWhenFired = onShuttle;
        }
    }

    /// <summary>
    ///     How far a shot has to travel before the thing Blob is standing on is allowed to stop it,
    ///     or null if he is standing on nothing. Without this a shot fired while stood over a collectable
    ///     dies on the spot.
    /// </summary>
    private int? PointBlankReach(Rectangle blob)
        => StandingOnSomething(blob, out int offset)
            ? offset + blob.Width / 2
            : null;

    private bool StandingOnSomething(Rectangle blob, out int offset)
    {
        offset = 0;

        foreach (InteractiveItem item in CollidableItemsOnThisScreen())
        {
            if (!CollisionDetection.Check(
                    blob,
                    new Rectangle(item.XLocation, item.YLocation, SpriteMetrics.SpriteWidth,
                        SpriteMetrics.SpriteHeight)))
            {
                continue;
            }

            offset = blob.X - item.XLocation;
            if (offset < 0)
            {
                offset = -offset;
            }

            return true;
        }

        return false;
    }

    private List<InteractiveItem> CollidableItemsOnThisScreen()
    {
        var items = new List<InteractiveItem>();

        if (_gameScreens.CurrentGameScreenData.CheopsPyramid != null)
        {
            items.Add(_gameScreens.CurrentGameScreenData.CheopsPyramid);
        }

        foreach (InteractiveItem item in _gameScreens.CurrentGameScreenData.PickUps)
        {
            items.Add(item);
        }

        return items;
    }

    private void DrawFlame(string sheetName, int frame, int yPos, int alpha, int screenFade, float rotation)
    {
        SpriteCollection flame = _resources.GetSpriteCollection(sheetName);
        flame.Begin(BlendState.Additive);
        flame.DrawSprite(
            frame,
            X,
            yPos,
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                (byte)(alpha * ((double)screenFade / ColourLevels.Full))),
            1.0f,
            rotation);
        flame.End();
    }

    /// <summary>
    ///     Joins a <see cref="BlobFire" /> to the game it is flying through. Everything the shot asks
    ///     about is answered from here, so the shot itself knows about nothing but its own position -
    ///     which is what lets its rules be tested without a screen or a graphics device.
    /// </summary>
    private sealed class FireSurroundings(
        GameScreenCollection gameScreens,
        Random randomNumberGenerator,
        IPositionalTriggerParticleEffect sparks)
        : IFireSurroundings
    {
        public int ScreenWidth => gameScreens.ScreenWidth;

        public int ScreenHeight => gameScreens.ScreenHeight;

        /// <inheritdoc />
        /// <remarks>
        ///     The shot is not Blob, so it gets none of the leniency the collision map gives him.
        /// </remarks>
        public int DistanceClear(Direction direction, int x, int y, int width, int height, int distance)
            => gameScreens.CheckCollision(direction, x, y, width, height, distance, false);

        /// <inheritdoc />
        public FireDeflection ChooseDeflection()
            => (FireDeflection)randomNumberGenerator.Next(
                (int)FireDeflection.Left,
                (int)FireDeflection.Right + 1);

        /// <inheritdoc />
        public void ThrowSparks(int x, int y) => sparks.Start(x, y);
    }
}
