// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Particles;
using Starquake.Scoring;
using Starquake.SharedLibs;
using Starquake.Sprites;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.ActiveSprites;

/// <summary>
///     Blob's lazer: what decides a shot is fired, which way it leaves, and everything that is
///     drawn because one is in flight. <see cref="BlobFire" /> is the shot itself and knows nothing but
///     its own position; this is the end that holds the trigger.
/// </summary>
/// <remarks>
///     <see cref="IFireSurroundings" /> is the five questions a shot asks of the world, which lets the
///     stepping rules be tested with no screen and no graphics device.
/// </remarks>
internal sealed class BlobWeapon
{
    /// <summary>
    ///     Where a shot starts relative to Blob's corner, and where the trail is emitted from
    ///     relative to the shot's. Both about a quarter of a sprite in.
    /// </summary>
    private const int FireOffset = 16;

    /// <inheritdoc cref="FireOffset" />
    private const int FireTrailOffset = 14;

    /// <summary>How fast the shot's additive glow reaches full, in colour levels a logic frame.</summary>
    private const int FireFadeStep = 16;

    /// <summary>
    ///     How fast the shuttle's flame turns, in degrees a logic frame, and the full revolution the
    ///     angle wraps at.
    /// </summary>
    private const float ShuttleFireRotateSpeed = 10f;

    /// <inheritdoc cref="ShuttleFireRotateSpeed" />
    private const float FullTurn = 360f;

    /// <summary>
    ///     The two flame cycles: the one drawn on foot, and the slower, longer one on the shuttle.
    /// </summary>
    private const int FireAnimLastFrame = 3;


    /// <inheritdoc cref="FireAnimLastFrame" />
    private const int AnimationDelay = 2;


    private readonly Animation _animFire1;
    private readonly BlobFire _fire;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    /// <summary>
    ///     Raised when a shot leaves, which is how the score hears about it. A callback rather than a
    ///     reference to the scorer, which the weapon has no business holding.
    /// </summary>
    private readonly Action _onFired;

    private readonly ResourceManager _resources;
    private readonly IPositionalTriggerParticleEffect _sparks;
    private readonly PlayerStats _stats;
    private readonly IStandardParticleEffect _trail;

    /// <summary>
    ///     The batch and the single white texel the beam is laid over. There is no beam artwork,
    ///     because the shader works out every pixel and only needs something to work them out on.
    /// </summary>
    private readonly SpriteBatch _beam;

    /// <inheritdoc cref="_beam" />
    private readonly Texture2D _beamQuad;

    /// <summary>
    ///     Logic frames the weapon has run for, which drives the beam's writhing. Counted on update
    ///     and never in draw: <c>GameHost</c> draws once per vertical retrace, so a wave counted in
    ///     drawn frames travels faster on a better monitor.
    /// </summary>
    private int _beamFrame;

    private bool _canFire = true;

    private Direction _directionFired = Direction.Right;

    private int _fade;

    /// <summary>
    ///     Whether Blob was on the shuttle when the shot left. Latched while nothing is in flight, so
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

        // The sparks, not Blob. Passing Blob here would mean handing out a reference to it
        // mid-construction.
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
    ///     Which way a shot leaves. On the shuttle it follows the direction of movement, including up
    ///     and down. On foot a shot only goes left or right, so standing still, or having last moved
    ///     up or down a lift, fires the way Blob last walked.
    /// </summary>
    /// <remarks>
    ///     The only part of the weapon that can be static and pure. The rest needs two particle
    ///     effects and a sprite sheet from the resource manager before it exists.
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
    ///     Reports that the shot hit something. Called by whatever was hit, because the nasty knows
    ///     it was struck and the weapon only has to stop flying.
    /// </summary>
    public void RegisterHit()
    {
        HasFired = false;
        _canFire = true;
    }

    /// <summary>
    ///     One logic frame of the weapon: fire if asked and able, carry any shot in flight, and settle
    ///     the effects once nothing is flying.
    /// </summary>
    /// <param name="firePressed">Whether the fire control is down this frame.</param>
    /// <param name="blob">Blob's position and size, which is where a shot starts from.</param>
    /// <param name="directionBlobMoved">The direction Blob last moved, which a shot follows.</param>
    /// <param name="onShuttle">Whether Blob is on the shuttle, which fires in all four directions.</param>
    public void Step(bool firePressed, Rectangle blob, Direction directionBlobMoved, bool onShuttle)
    {
        StartFiring(firePressed, blob, directionBlobMoved, onShuttle);
        CarryTheShot(blob);
        SettleEffectsWhenNothingIsFlying(onShuttle);
    }

    /// <summary>
    ///     Steps the particle effects. The particle engine runs on wall-clock time rather than the
    ///     frame counter, so these take the frame's elapsed time.
    /// </summary>
    public void UpdateEffects(GameTime gameTime)
    {
        _sparks.Update(gameTime);
        _trail.Update(X + FireTrailOffset, Y + FireTrailOffset, gameTime);

        _beamFrame++;
    }

    /// <summary>
    ///     The shot's trail, drawn under Blob rather than over, which is why it is separate from
    ///     <see cref="Draw" />.
    /// </summary>
    public void DrawTrail() => _trail.Draw();

    /// <summary>
    ///     What is drawn at the head of the shot. The original artwork uses a flame sprite, on foot
    ///     and on the shuttle alike; the redrawn set uses <see cref="DrawBeam" /> instead.
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

            return;
        }

        DrawBeam(screenFade, onShuttle);
    }

    /// <summary>
    ///     The beam itself, drawn out of nothing over a plain filled rectangle turned to the way
    ///     the shot is going.
    /// </summary>
    /// <remarks>
    ///     No flame sprite is involved. The light is the shot's own, so the beam needs no artwork at
    ///     all.
    /// </remarks>
    private void DrawBeam(int screenFade, bool onShuttle)
    {
        Effect laser = _resources.GetEffect(Assets.Shaders.Laser);

        // Reciprocals are worked out here to save a divide on every pixel. They never change between
        // frames, and the pass has no slots to spare.
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

        // Turned to the way the shot travels. On the shuttle it can go any of four ways and deflect
        // as well, so the shot's own angle is used. On foot it is one of two.
        float along = onShuttle
            ? MathHelper.ToRadians(_rotation)
            : _directionFired == Direction.Left ? MathHelper.Pi : 0f;

        _beam.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, laser);
        // Centred where the trail is emitted from, which is the middle of the shot. Offsetting by
        // half a sprite instead would hang the beam off the shot on both axes.
        _beam.Draw(
            _beamQuad,
            new Vector2(X + FireTrailOffset, Y + FireTrailOffset),
            null,
            Colours.WhiteFadedTo((byte)(_fade * ((double)screenFade / ColourLevels.Full))),
            along,
            new Vector2(0.5f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
        _beam.End();
    }

    /// <summary>
    ///     The sparks a deflected shot throws off. Drawn over the scenery, because the scenery is
    ///     what deflected the shot.
    /// </summary>
    public void DrawSparks(int screenFade) => _sparks.Draw(screenFade);

    /// <summary>
    ///     Puts the weapon away with nothing in flight and the trail stopped, for a Blob that is
    ///     dying.
    /// </summary>
    public void Disarm()
    {
        HasFired = false;
        _canFire = true;
        _trail.Stop();
    }

    /// <summary>
    ///     Ready to fire again from Blob's new position. A shot does not carry across a screen
    ///     change, or it would arrive mid-flight on a screen it was never fired on.
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
    ///     moves between the two calls, going through the tunnel walked into first.
    /// </summary>
    public void ResetEffects()
    {
        _sparks.Reset();
        _trail.Reset();
    }

    /// <remarks>
    ///     The three nested conditions and the two-armed direction test are kept as found.
    ///     Flattening them is an open follow-up in <c>REFACTORING.md</c>.
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
    ///     How the shot looks this frame. The glow rises to full over the first few frames of flight
    ///     rather than appearing at full brightness on launch, and the shuttle's shot turns as it
    ///     goes. Both run on the logic clock, not the display's refresh rate.
    /// </summary>
    private void AdvanceAppearance()
    {
        // Not on the frame the shot expires. Advance runs after the step that can end the flight.
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
    ///     How far a shot must travel along the axis it was fired on before the item Blob is standing
    ///     over can stop it. Without this a shot fired over a collectable dies on the spot, because
    ///     the item's cells stop a shot the way they stop a nasty and the shot is born inside them.
    /// </summary>
    /// <remarks>
    ///     Measured along the fired axis, which is the axis the shot has to clear the item on. Half
    ///     of Blob is added, because a shot launches from inside Blob rather than from the edge.
    /// </remarks>
    public static int PointBlankReach(Rectangle blob, Rectangle item, Direction firedIn)
        => firedIn is Direction.Up or Direction.Down
            ? Math.Abs(blob.Y - item.Y) + blob.Height / 2
            : Math.Abs(blob.X - item.X) + blob.Width / 2;

    /// <summary>
    ///     The reach for the item Blob is currently standing over, or null if standing on nothing.
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

    /// <summary>One flame, lit as a beam along the direction of travel.</summary>
    /// <remarks>
    ///     The hot line down the middle comes from <c>Shaders/Laser.fx</c>, not a second sprite laid
    ///     over the top. The core is the heat of the flame, so it belongs in the same pass, where one
    ///     intensity governs both.
    /// </remarks>
    private void DrawFlame(string sheetName, int frame, int yPos, int alpha, int screenFade, float rotation)
    {
        SpriteCollection flame = _resources.GetSpriteCollection(sheetName);

        flame.Begin(BlendState.Additive);
        flame.DrawSprite(
            frame,
            X,
            yPos,
            Colours.WhiteFadedTo((byte)(alpha * ((double)screenFade / ColourLevels.Full))),
            1.0f,
            rotation);
        flame.End();
    }

    /// <summary>
    ///     Joins a <see cref="BlobFire" /> to the game it flies through. Answering everything the
    ///     shot asks here is what keeps the shot knowing nothing but its own position, so its rules
    ///     can be tested without a screen or a graphics device.
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
            => gameScreens.Collisions.CheckCollision(direction, x, y, width, height, distance, false);

        /// <inheritdoc />
        public FireDeflection ChooseDeflection()
            => (FireDeflection)randomNumberGenerator.Next(
                (int)FireDeflection.Left,
                (int)FireDeflection.Right + 1);

        /// <inheritdoc />
        public void ThrowSparks(int x, int y) => sparks.Start(x, y);
    }
}
