// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    private const int AnimationDelay = 2;


    private readonly Animation _animFire1;
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

    /// <summary>
    ///     The batch and the one white texel the beam is laid over. There is no artwork for a
    ///     beam and there does not need to be: the shader works every pixel of it out, so all it
    ///     wants is something to work them out on.
    /// </summary>
    private readonly SpriteBatch _beam;

    /// <inheritdoc cref="_beam" />
    private readonly Texture2D _beamQuad;

    /// <summary>
    ///     Logic frames the weapon has been running, which drives the beam's writhing. Counted on
    ///     the update and never in the draw - <c>GameHost</c> draws once per vertical retrace, so a
    ///     wave counted in drawn frames travels faster on a better monitor.
    /// </summary>
    private int _beamFrame;

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
        GraphicsDeviceManager graphics,
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

        _beam = new SpriteBatch(graphics.GraphicsDevice);
        _beamQuad = new Texture2D(graphics.GraphicsDevice, 1, 1);
        _beamQuad.SetData([Color.White]);

        _animFire1 = Animation.Looping(0, FireAnimLastFrame, AnimationDelay, true);
    }

    /// <summary>Whether a shot is in flight, which is what a nasty checks before testing for a hit.</summary>
    public bool HasFired { get; private set; }

    /// <inheritdoc cref="BlobFire.X" />
    public int X => _fire.X;

    /// <inheritdoc cref="BlobFire.Y" />
    public int Y => _fire.Y;

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

        _beamFrame++;
    }

    /// <summary>
    ///     The shot's trail, drawn under Blob rather than over him - which is why it is not part of
    ///     <see cref="Draw" />.
    /// </summary>
    public void DrawTrail() => _trail.Draw();

    /// <summary>
    ///     The flame at the head of the shot.
    /// </summary>
    /// <remarks>
    ///     This used to say that only the redrawn artwork drew one on foot and the original drew
    ///     nothing off the shuttle, which is the wrong way round twice over: the original drew one
    ///     in both cases and the redrawn set drew <em>nothing at all</em> on foot. So shooting
    ///     while walking - which is most shooting - put out a shot with no flame on it and nothing
    ///     but a two-particle trail behind it. It draws one now, turned to the way the shot is
    ///     going.
    ///     <para>
    ///         Every flame also gets a white core laid over it from <c>BlobFire2Overlay</c>, a glow
    ///         that was drawn and registered and then never put on screen.
    ///     </para>
    /// </remarks>
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

            return;
        }

        DrawBeam(screenFade, onShuttle);
    }

    /// <summary>
    ///     The beam itself, drawn out of nothing over a plain filled rectangle turned to the way
    ///     the shot is going.
    /// </summary>
    /// <remarks>
    ///     No flame sprite is drawn on foot, which is how the game has always been: one was taken
    ///     out years ago for adding nothing, and putting it back to make the shot read better
    ///     proved the point rather than fixing it. The light is the shot's own now, so the on-foot
    ///     case needs no artwork at all and the shuttle's flame has a beam through it.
    /// </remarks>
    private void DrawBeam(int screenFade, bool onShuttle)
    {
        Effect laser = _resources.GetEffect(Assets.Shaders.Laser);

        // The reciprocals are worked out here rather than costing a divide on every pixel - the
        // pass has no slots to spare, and these never change between frames.
        laser.Parameters["OverCoreWidth"].SetValue(1f / LaserSettings.CoreWidth);
        laser.Parameters["CoreColour"].SetValue(LaserSettings.CoreColour);
        laser.Parameters["EdgeColour"].SetValue(LaserSettings.EdgeColour);
        laser.Parameters["Spread"].SetValue(LaserSettings.Spread);
        (int length, int thickness, float intensity) = LaserSettings.BeamFor(onShuttle);

        laser.Parameters["Intensity"].SetValue(intensity);
        laser.Parameters["WaveAmplitude"].SetValue(LaserSettings.WaveAmplitude);
        laser.Parameters["WaveFrequency"].SetValue(LaserSettings.WaveFrequency);
        laser.Parameters["WavePhase"].SetValue(LaserSettings.WavePhaseAt(_beamFrame));
        laser.Parameters["OverEndFade"].SetValue(1f / LaserSettings.EndFade);
        laser.Parameters["OverHeadSize"].SetValue(1f / LaserSettings.HeadSize);
        laser.Parameters["HeadStretch"].SetValue(LaserSettings.HeadStretch);
        laser.Parameters["HeadBoost"].SetValue(LaserSettings.HeadBoost);
        laser.Parameters["BackShare"].SetValue(LaserSettings.BackShare);

        // Turned to the way the shot travels. On the shuttle it may be going any of the four ways
        // and deflecting as well, so the shot's own angle is used; on foot it is one of two.
        float along = onShuttle
            ? MathHelper.ToRadians(_rotation)
            : _directionFired == Direction.Left ? MathHelper.Pi : 0f;

        _beam.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, laser);
        // Centred where the trail is emitted from, which is the middle of the shot. It used to be
        // offset by half a *sprite* - thirty-two, half of sixty-four - where the shot is two by
        // thirty-two, so the beam hung sixteen pixels off it on both axes and read as something
        // flying alongside rather than as the shot itself.
        _beam.Draw(
            _beamQuad,
            new Vector2(X + FireTrailOffset, Y + FireTrailOffset),
            null,
            ColourHelper.WhiteFadedTo((byte)(_fade * ((double)screenFade / ColourLevels.Full))),
            along,
            new Vector2(0.5f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
        _beam.End();
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

        if (!_fire.Advance(_directionFired, _onShuttleWhenFired, PointBlankReachForShot(blob)))
        {
            HasFired = false;
            _canFire = true;
        }

        _animFire1.GetNextFrame();

        AdvanceAppearance();
    }

    /// <summary>
    ///     How the shot looks this frame: its glow comes up to full over the first few frames of
    ///     flight so it does not appear at full brightness on the frame it is launched, and the
    ///     shuttle's shot turns as it goes.
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
    ///     How far a shot has to travel along the axis it was fired on before the item Blob is
    ///     standing over is allowed to stop it. Without this a shot fired while stood over a
    ///     collectable dies on the spot, because the item's cells stop a shot the same way they
    ///     stop a nasty and the shot is born inside them.
    /// </summary>
    /// <remarks>
    ///     Measured along the fired axis because that is the axis the shot has to clear the item
    ///     along. It used to measure the horizontal gap whatever the direction, and
    ///     <see cref="BlobFire.Advance" /> then applied it to horizontal shots only - so firing up
    ///     or down from a shuttle over a collectable killed the shot on the spot. Half of Blob is
    ///     added because the shot is launched from inside him rather than from his edge.
    /// </remarks>
    public static int PointBlankReach(Rectangle blob, Rectangle item, Direction firedIn)
        => firedIn is Direction.Up or Direction.Down
            ? Math.Abs(blob.Y - item.Y) + blob.Height / 2
            : Math.Abs(blob.X - item.X) + blob.Width / 2;

    /// <summary>
    ///     The reach for the item Blob is standing over right now, or null if he is standing on
    ///     nothing.
    /// </summary>
    private int? PointBlankReachForShot(Rectangle blob)
        => StandingOnSomething(blob, out Rectangle item)
            ? PointBlankReach(blob, item, _directionFired)
            : null;

    private bool StandingOnSomething(Rectangle blob, out Rectangle standingOn)
    {
        standingOn = Rectangle.Empty;

        foreach (InteractiveItem item in CollidableItemsOnThisScreen())
        {
            var itemBounds = new Rectangle(
                item.XLocation, item.YLocation, SpriteMetrics.SpriteWidth, SpriteMetrics.SpriteHeight);

            if (!CollisionDetection.Check(blob, itemBounds))
            {
                continue;
            }

            standingOn = itemBounds;

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

    /// <summary>
    ///     One flame, lit as a beam along the way the shot is going.
    /// </summary>
    /// <remarks>
    ///     The hot line down the middle is <c>Shaders/Laser.fx</c> rather than a second sprite laid
    ///     over the top. A white glow was tried that way first - <c>BlobFire2Overlay</c>, which had
    ///     been drawn for exactly that and never used - and it was both too bright and one
    ///     mechanism too many: the core is the heat of the flame and belongs in the same pass as
    ///     the flame, where a single intensity governs it.
    /// </remarks>
    private void DrawFlame(string sheetName, int frame, int yPos, int alpha, int screenFade, float rotation)
    {
        SpriteCollection flame = _resources.GetSpriteCollection(sheetName);

        flame.Begin(BlendState.Additive);
        flame.DrawSprite(
            frame,
            X,
            yPos,
            ColourHelper.WhiteFadedTo((byte)(alpha * ((double)screenFade / ColourLevels.Full))),
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
