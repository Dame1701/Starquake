// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics.CodeAnalysis;
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

internal class Nasty
{

    /// <summary>How long a nasty takes to be born, in logic frames.</summary>
    private const int BirthPeriod = 60;

    /// <inheritdoc cref="NastyTypeDraw" />
    private const int NastyTypeDrawRange = 16;

    /// <summary>
    ///     The nasty's walk cycle. It plays back and forth rather than looping, which gives the legs
    ///     their scuttle.
    /// </summary>
    private const int NastyLastFrame = 3;

    /// <inheritdoc cref="NastyLastFrame" />
    private const int NastyAnimationDelay = 4;

    /// <summary>
    ///     How many sprite sheet frames each kind of nasty occupies, which turns a
    ///     <see cref="NastyType" /> into the first sprite of its walk cycle.
    /// </summary>
    private const int FramesPerNasty = 4;

    /// <summary>
    ///     How fast the nasty spins as it materialises, and how fast it is still spinning at the end.
    ///     It slows from the first to the second over the back half of the birth.
    /// </summary>
    private const float BirthStartRotateSpeed = 8.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthRespawnRotateSpeed = 32.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthEndRotateSpeed = 16f;

    /// <summary>
    ///     The longest a birth keeps spinning past <see cref="BirthPeriod" /> waiting to come to rest
    ///     upright. A revolution at the settling speed takes about 24 frames, so this is a little
    ///     over one full turn: long enough to normally land square, short enough not to spin forever.
    /// </summary>
    private const int BirthSpinOutLimit = 30;

    /// <summary>
    ///     The colours fade in over the back half of the birth only, so both fades count up at twice
    ///     the birth's own rate.
    /// </summary>
    private const int BirthColourFadeHalf = 2;

    /// <summary>
    ///     How long a nasty lies dead before it may come back, and the longest it then waits before
    ///     doing so. A nasty is never gone for good.
    /// </summary>
    private const int DeadPauseFrames = 50;

    /// <summary>
    ///     The longest a killed nasty stays away before being conceived again, in logic frames. Ten
    ///     seconds, rolled fresh on each death, so a room Blob has cleared stays cleared for a while
    ///     and the nasty he killed does not walk straight back into his fire.
    /// </summary>
    /// <remarks>
    ///     Armed at the death, where the reason for the wait is known. <see cref="BeginConception" />
    ///     must leave it alone, or every nasty comes back inside
    ///     <see cref="LongestWaitToFirstAppear" /> instead.
    /// </remarks>
    private const int LongestWaitToComeBack = 500;

    /// <summary>
    ///     The longest a nasty waits when a screen is being populated rather than after a death, in
    ///     logic frames. Just over a second, so walking into a room fills it rather than leaving Blob
    ///     standing in an empty one.
    /// </summary>
    private const int LongestWaitToFirstAppear = 60;

    /// <summary>
    ///     How fast a creature's corpse goes, and with it the splat dissolve. The shader reads what
    ///     is left of this fade, so the two are one countdown rather than two. Eighty a frame against
    ///     a full level of 255 is four frames, which is an eyeblink at fifty a second.
    /// </summary>
    private const int CreatureFadeStep = 80;

    /// <summary>
    ///     How quickly a creature's corpse draws in as it goes. A ramp that never reaches its end:
    ///     the body is only drawn while some of the fade is left, which is four frames, by which
    ///     point the scale has come down about a tenth. So this sets the rate, not the duration.
    /// </summary>
    private const float CreatureShrinkStep = 0.04f;

    /// <summary>
    ///     How fast a creature's corpse spins as it goes, in degrees a logic frame. Fast enough that
    ///     the body whips round rather than turning sedately while its debris is already across the
    ///     room.
    /// </summary>
    private const float CreatureSpinSpeed = 32f;

    /// <summary>
    ///     Which of the two sets of numbers this nasty's death runs on. A machine's wreck lasts
    ///     thirteen frames and a creature's four, so one constant cannot serve both.
    /// </summary>
    private int DeathFadeStep => IsMechanical ? MachineDeathSettings.FadeStep : CreatureFadeStep;

    /// <inheritdoc cref="DeathFadeStep" />
    private float DeathShrinkStep => IsMechanical ? MachineDeathSettings.ShrinkStep : CreatureShrinkStep;

    /// <inheritdoc cref="DeathFadeStep" />
    /// <remarks>
    ///     A machine's wreck does not turn at all, matching the conveyor nasty's. It is sitting in a
    ///     fire, not tumbling through the air.
    /// </remarks>
    private float DeathRotateSpeed => IsMechanical ? NoSpin : CreatureSpinSpeed;

    /// <inheritdoc cref="DeathRotateSpeed" />
    private const float NoSpin = 0f;

    /// <summary>
    ///     How fast a live nasty rocks from side to side as it walks.
    /// </summary>
    private const float NastyRockSpeed = 4f;

    /// <summary>
    ///     One whole revolution in degrees, which is where <see cref="_rotation" /> wraps.
    /// </summary>
    private const float FullTurn = 360f;

    /// <summary>
    ///     The tints: grey for a nasty that has been shot, red for a spiky one, green for a conveyor
    ///     one.
    /// </summary>
    private const int ShotNastyGrey = 50;

    /// <inheritdoc cref="ShotNastyGrey" />
    private const int ConveyorNastyGreen = 100;

    /// <summary>
    ///     The sixteen draws a random nasty is picked from. Fifteen kinds walk the planet; Bird
    ///     appears at both ends of the table and Spiky takes the last draw, so those two come up
    ///     twice as often as the rest.
    /// </summary>
    private static readonly NastyType[] NastyTypeDraw =
    [
        NastyType.Bird,
        NastyType.Helmet,
        NastyType.FriedEgg,
        NastyType.CurleyWurley,
        NastyType.Shellfish,
        NastyType.Cushion,
        NastyType.Gonzo,
        NastyType.Snail,
        NastyType.Ghost,
        NastyType.Centipede,
        NastyType.Scorpion,
        NastyType.DragonFly,
        NastyType.Hippo,
        NastyType.Helicopter,
        NastyType.Bird,
        NastyType.Spiky
    ];
    private readonly Blob _blob;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    private readonly Animation _nastyAnim;
    /// <summary>
    ///     Called when this nasty blows apart, so the picture can be shaken. A callback rather than
    ///     the thing that does the shaking, which a nasty has no business holding.
    /// </summary>
    private readonly Action _onExploded;

    private readonly IStandardParticleEffect _nastyArrives;

    private readonly IStandardParticleEffect _nastyDies;

    private readonly Animation _nastyDiesAnimation;
    private readonly IStandardParticleEffect _nastyDiesType2;

    private readonly SpriteCollection _nastySpriteNew;
    private readonly SpriteCollection _nastySpriteOriginal;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    /// <summary>
    ///     The ring a machine throws as it goes off. Only machines have one: a creature coming apart
    ///     is wet rather than loud, and its splat is the whole of it.
    /// </summary>
    private readonly Shockwave _shockwave;

    private int _birthColourLevel;

    private int _birthFadeLevel;
    private float _birthRotateSpeed = BirthStartRotateSpeed;

    private float _birthScale;
    private int _birthTimer;

    private int _conceptionWaitCounter;

    private bool _deathAnticlockwise;

    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    /// <summary>
    ///     How long a dead nasty has been lying there. Every nasty regenerates after a random wait;
    ///     none is ever gone for good.
    /// </summary>
    private int _nastyDeadPauseCounter;

    private SpriteCollection _nastySprite;

    private NastyType _nastyType;

    /// <summary>
    ///     Which way round this nasty is drawn, in degrees. Advanced on the logic clock rather than
    ///     per draw batch, so the spin does not follow the display's refresh rate. Not carried across
    ///     a screen swap, since a swap resets a birth or a death anyway.
    /// </summary>
    private float _rotation;

    public Nasty(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        Action onExploded,
        int xposition,
        int yposition,
        int moveSpeed)
    {
        _onExploded = onExploded;

        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;

        Movement = new NastyBrain(gameScreens.Collisions, randomNumberGenerator, moveSpeed)
        {
            XPos = xposition,
            YPos = yposition
        };

        _nastyAnim = new Animation();
        _nastyAnim.FirstFrame = 0;
        _nastyAnim.LastFrame = NastyLastFrame;
        _nastyAnim.Repeat = true;
        _nastyAnim.AnimateForwards = true;
        _nastyAnim.Yoyo = true;
        _nastyAnim.Delay = NastyAnimationDelay;

        _nastyDiesAnimation = new Animation();
        _nastyDiesAnimation.FirstFrame = 0;
        _nastyDiesAnimation.CurrentFrame = 0;
        _nastyDiesAnimation.LastFrame = 3;
        _nastyDiesAnimation.Yoyo = false;
        _nastyDiesAnimation.Delay = 2;
        _nastyDiesAnimation.Repeat = true;
        _nastyDiesAnimation.AnimateForwards = true;

        _nastySpriteOriginal = _resources.GetSpriteCollection(Assets.Graphics.NastiesOriginal)
            .Clone();
        _nastySpriteNew = _resources.GetSpriteCollection(Assets.Graphics.Nasties).Clone();

        CheckSpritesInUse();

        _shockwave = new Shockwave(
            resources, randomNumberGenerator, ShockwaveSettings.NastyDiameter, ShockwaveSettings.NastyFrames);

        _nastyDies = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.NastyDies,
            ParticleEffectType.Triggered, null);
        _nastyDies.ParticleEffectComplete += NastyDies_ParticleEffectComplete;

        _nastyDiesType2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.NastySplat,
                ParticleEffectType.Triggered, null);
        _nastyDiesType2.ParticleEffectComplete += NastyDies_ParticleEffectComplete;

        // Blob's teleport beam, reused. A nasty arriving is the same idea and costs no new content.
        _nastyArrives =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.BeamMeUpSmall,
                ParticleEffectType.Triggered, null);
    }

    /// <summary>
    ///     Where this nasty is going and how it decided. All movement lives there; this class holds
    ///     the lifecycle and the picture.
    /// </summary>
    public NastyBrain Movement { get; }

    public Direction Direction => Movement.Direction;

    public int Speed => Movement.MoveSpeed;

    public bool KeepNastyTheSame { get; set; }

    public int NastyWidth { get; } = SpriteMetrics.SpriteWidth;

    public NastyState NastyState { get; set; }

    public int NastyHeight { get; } = SpriteMetrics.SpriteHeight;

    public bool Smoking { get; set; }

    /// <summary>
    ///     Where the nasty is. Held by <see cref="Movement" />, the only thing that changes it, and
    ///     forwarded here because everything that draws or tests a nasty asks the nasty.
    /// </summary>
    public int XPos
    {
        get => Movement.XPos;
        set => Movement.XPos = value;
    }

    /// <inheritdoc cref="XPos" />
    public int YPos
    {
        get => Movement.YPos;
        set => Movement.YPos = value;
    }

    public NastyType NastyType
    {
        get => _nastyType;
        set
        {
            _nastyType = value;
            Movement.BecomeA(value);
        }
    }

    /// <summary>
    ///     Takes a snapshot of another nasty. Twice the number of nasties needed are kept, and the set
    ///     being left is copied into the spare half in case Blob comes back.
    /// </summary>
    /// <remarks>
    ///     Movement is one assignment of a record struct, so what carries is decided by what
    ///     <see cref="NastyMovementState" /> declares.
    ///     <para>
    ///         The lifecycle half is the list below. The rule is that a counter carries only if a
    ///         state the snapshot can be restored into reads it before setting it:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="NastyState.Conception" /> reads <c>_conceptionWaitCounter</c> in
    ///             <see cref="WaitToBeBorn" /> without setting it, so that one must carry. Without it
    ///             a nasty part way through waiting appears the moment Blob returns.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.Birth" /> becomes <see cref="NastyState.Normal" /> below, so
    ///             the five birth counters are never read from a snapshot. They are all set together
    ///             in <see cref="WaitToBeBorn" /> on the frame a birth begins.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.Dead" /> never appears in a snapshot, because <c>Nasties</c>
    ///             turns a dead or shot nasty into <see cref="NastyState.PreConception" /> before
    ///             copying, so a screen is never returned to empty.
    ///             <c>_nastyDeadPauseCounter</c> carries anyway, which keeps this a complete snapshot
    ///             on its own terms rather than the caller's.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.PreConception" /> settles its kind in
    ///             <see cref="BeginConception" />, and its wait was armed on the way into that state
    ///             and carries above. <see cref="NastyState.Disabled" /> reads nothing.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <c>_rotation</c> is deliberately not carried. It is only ever drawn with, so a restored
    ///         nasty starting at the spare's angle looks no different.
    ///     </para>
    /// </remarks>
    public void CopyFromSource(Nasty copySource)
    {
        Movement.State = copySource.Movement.State;
        Movement.NastyType = copySource.Movement.NastyType;
        Movement.XPos = copySource.Movement.XPos;
        Movement.YPos = copySource.Movement.YPos;

        KeepNastyTheSame = copySource.KeepNastyTheSame;
        _nastyType = copySource._nastyType;
        _conceptionWaitCounter = copySource._conceptionWaitCounter;
        _nastyDeadPauseCounter = copySource._nastyDeadPauseCounter;

        // A nasty caught mid-birth arrives fully born, because the screen it was being born onto is
        // not the screen it is now on.
        NastyState = copySource.NastyState == NastyState.Birth
            ? NastyState.Normal
            : copySource.NastyState;
    }

    /// <summary>
    ///     Picks somewhere on the current screen for this nasty to appear, or <c>null</c> if the
    ///     screen offers nowhere.
    /// </summary>
    /// <remarks>
    ///     Null rather than <c>Point(0, 0)</c>, which no real location can be, since every one is
    ///     offset by half a sprite, but which reads like one at the call site.
    /// </remarks>
    private Point? GetNastySpawnLocation()
    {
        List<Point> potentialLocations = _gameScreens.GetNastyPotentialLocations();

        if (potentialLocations.Count == 0)
        {
            return null;
        }

        int nastyLocationIndex = _randomNumberGenerator.Next(0, potentialLocations.Count);

        return new Point(potentialLocations[nastyLocationIndex].X + SpriteMetrics.HalfSpriteWidth,
            potentialLocations[nastyLocationIndex].Y + 16);
    }

    [MemberNotNull(nameof(_nastySprite))]
    private void CheckSpritesInUse()
    {
        if (_graphicsSet.IsOriginal)
        {
            _nastySprite = _nastySpriteOriginal;
        }
        else
        {
            _nastySprite = _nastySpriteNew;
        }
    }

    private void NastyDies_ParticleEffectComplete()
    {
        if (NastyState == NastyState.Dead)
        {
            _nastyDeadPauseCounter = 0;
            AwaitConception(LongestWaitToComeBack);
        }
    }

    /// <summary>
    ///     Sends this nasty away to be conceived again, arming the wait on the way.
    /// </summary>
    /// <remarks>
    ///     The only way into <see cref="NastyState.PreConception" />. The wait must be armed here,
    ///     because the state keeps no record of why it was entered and the two reasons want waits an
    ///     order apart. <see cref="CopyFromSource" /> carries the counter, so a nasty stored while
    ///     waiting is still waiting when Blob comes back.
    /// </remarks>
    /// <param name="longestWait">The longest it may wait, in logic frames. Rolled against.</param>
    private void AwaitConception(int longestWait)
    {
        _conceptionWaitCounter = _randomNumberGenerator.Next(0, longestWait);
        NastyState = NastyState.PreConception;
    }

    /// <summary>
    ///     Sends this nasty away to be conceived as one of a screen's own rather than after being
    ///     killed, so it returns almost at once. <c>Nasties</c> populates a screen by putting every
    ///     nasty through here.
    /// </summary>
    public void AwaitFirstAppearance() => AwaitConception(LongestWaitToFirstAppear);

    /// <summary>
    ///     Picks a nasty at random, from the fifteen kinds that walk about the planet. Bird appears
    ///     twice in the table and Spiky takes the sixteenth draw, so each of those two comes up twice
    ///     as often as the rest.
    /// </summary>
    /// <remarks>
    ///     A table rather than a chain of comparisons, because the order is the point: it is what
    ///     gives Bird its second entry, and a table makes the duplicate visible.
    /// </remarks>
    public static NastyType GetRandomNastyType(Random randomNumberGenerator)
        => NastyTypeDraw[randomNumberGenerator.Next(0, NastyTypeDrawRange)];

    public void Update(GameTime gameTime)
    {
        if (_gameScreens.ScreenNumber == _gameScreens.CoreExit.ScreenNumber)
        {
            NastyState = NastyState.Disabled;
        }

        if (_gameScreens.ScreenNumber == _gameScreens.CoreSite.ScreenNumber)
        {
            NastyType = NastyType.CoreGuardian;
            Movement.StopChasing();
        }

        if (NastyState == NastyState.PreConception)
        {
            BeginConception();
        }
        else if (NastyState == NastyState.Conception)
        {
            WaitToBeBorn();
        }
        else if (NastyState == NastyState.Birth)
        {
            UpdateBirth();
        }
        else if (NastyState == NastyState.Shot)
        {
            StartDying();
        }
        else if (NastyState == NastyState.Dead)
        {
            UpdateDeath();
        }

        UpdateDeathEffects(gameTime);

        AdvanceRotation();

        if (!ShouldAnimate())
        {
            return;
        }

        _nastyAnim.GetNextFrame();

        Movement.Step(_blob.XPos, _blob.YPos);
    }

    /// <summary>
    ///     What a nasty settles before it is born: which kind it will be, and where its movement
    ///     starts from.
    /// </summary>
    /// <remarks>
    ///     The wait is not set here. It was armed by whatever sent the nasty to
    ///     <see cref="NastyState.PreConception" />, which is the only thing that knows how long it
    ///     should be.
    /// </remarks>
    private void BeginConception()
    {
        NastyState = NastyState.Conception;

        if (!KeepNastyTheSame)
        {
            NastyType = GetRandomNastyType(_randomNumberGenerator);
        }

        Movement.PrepareForConception();
    }

    private void WaitToBeBorn()
    {
        if (_conceptionWaitCounter > 0)
        {
            _conceptionWaitCounter--;
        }
        else
        {
            // Where it is going must be settled before anything announces that it is going. A screen
            // with nowhere to put it disables it instead, and the appear sound, the first movement
            // and the birth counters all belong to a nasty that is really being born.
            Point? newPosition = GetNastySpawnLocation();

            if (newPosition == null)
            {
                NastyState = NastyState.Disabled;

                return;
            }

            _resources.GetSoundFxEngine().PlaySound(Assets.Sounds.NastyAppear, 0.1F, false);
            Movement.StartFirstMovement();
            _deathFadeLevel = ColourLevels.Full;
            _deathScale = 1f;
            _birthFadeLevel = 0;
            _birthScale = 0f;
            _birthColourLevel = 0;
            _birthRotateSpeed = BirthRespawnRotateSpeed;
            _nastyArrives.Start();
            _birthTimer = 0;

            XPos = newPosition.Value.X;
            YPos = newPosition.Value.Y;

            NastyState = NastyState.Birth;
        }
    }

    private void UpdateBirth()
    {
        if (_birthScale < 1.0f)
        {
            _birthScale += 1f / BirthPeriod;
        }

        if (_birthScale > 1.0f)
        {
            _birthScale = 1.0f;
        }

        if (_birthFadeLevel < ColourLevels.Full)
        {
            _birthFadeLevel += (int)(ColourLevels.Full /
                                     ((double)BirthPeriod /
                                      BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
        }

        // Colour and the slowing of the spin both start half way through, so a nasty arrives grey
        // and spinning and settles into its own colours.
        if (_birthTimer > BirthPeriod / BirthColourFadeHalf)
        {
            if (_birthColourLevel < ColourLevels.Full)
            {
                _birthColourLevel += (int)(ColourLevels.Full /
                                           ((double)BirthPeriod /
                                            BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
            }

            if (_birthRotateSpeed > BirthEndRotateSpeed)
            {
                _birthRotateSpeed -= BirthEndRotateSpeed / ((float)BirthPeriod / BirthColourFadeHalf);
            }
        }

        if (_birthFadeLevel > ColourLevels.Full)
        {
            _birthFadeLevel = ColourLevels.Full;
        }

        if (_birthColourLevel > ColourLevels.Full)
        {
            _birthColourLevel = ColourLevels.Full;
        }

        if (_birthTimer > BirthPeriod)
        {
            // The nasty prefers to stop spinning square, but it must stop either way. The test is a
            // range rather than an equality: the settled speed does not divide 360, so an exact zero
            // can be stepped straight over. BirthSpinOutLimit is the backstop.
            if (_rotation < _birthRotateSpeed ||
                _birthTimer > BirthPeriod + BirthSpinOutLimit)
            {
                NastyState = NastyState.Normal;
                _birthTimer = 0;
            }
        }

        _birthTimer++;
    }

    /// <summary>
    ///     Whether this nasty is a machine rather than a creature. Machines explode and creatures
    ///     come apart. One property, so the death shader and the particles cannot disagree.
    /// </summary>
    private bool IsMechanical => _nastyType is NastyType.Spiky or NastyType.Conveyor;

    private void StartDying()
    {
        _deathFadeLevel = ColourLevels.Full;
        _deathScale = 1.0f;

        if (IsMechanical)
        {
            // Square, and staying square. A machine's death spin is nought, but the birth can leave
            // a few degrees on the clock, since it ends when the angle is merely near zero and its
            // backstop can end it anywhere.
            _rotation = 0f;

            _nastyDies.Start();
            _shockwave.Trigger(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight);

            // Only the machines. A creature coming apart is wet rather than loud, and jolting on
            // every nasty shot would keep the picture moving most of the time.
            _onExploded();
        }
        else
        {
            NastyColours.ApplyNastyColours(_nastyType, _nastyDiesType2.Effect);
            _nastyDiesType2.Start();
        }

        NastyState = NastyState.Dead;

        if (_randomNumberGenerator.Next(0, 2) == 0)
        {
            _deathAnticlockwise = true;
        }
        else
        {
            _deathAnticlockwise = false;
        }
    }

    private void UpdateDeath()
    {
        _deathFadeLevel = Ramp.Down(_deathFadeLevel, DeathFadeStep, ColourLevels.None);
        _deathScale = Ramp.Down(_deathScale, DeathShrinkStep, 0f);

        if (_graphicsSet.IsOriginal)
        {
            _nastyDiesAnimation.GetNextFrame();

            if (_nastyDeadPauseCounter < DeadPauseFrames)
            {
                _nastyDeadPauseCounter++;
            }
            else
            {
                AwaitConception(LongestWaitToComeBack);
                _nastyDeadPauseCounter = 0;
            }
        }
    }

    /// <summary>
    ///     Steps both death effects, whichever this nasty actually started: the explosion for a
    ///     spiky or conveyor nasty, the splat for the others. Updating the idle one costs nothing and
    ///     saves asking which is running.
    /// </summary>
    private void UpdateDeathEffects(GameTime gameTime)
    {
        _shockwave.Update();

        _nastyDies.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        _nastyDiesType2.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        _nastyArrives.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
    }

    /// <summary>
    ///     A nasty is animated once fully real, or a quarter of the way through being born, which is
    ///     where its legs start showing.
    /// </summary>
    private bool ShouldAnimate()
    {
        bool updateAnimation = false;

        if (NastyState != NastyState.Disabled &&
            NastyState != NastyState.PreConception &&
            NastyState != NastyState.Conception &&
            NastyState != NastyState.Birth)
        {
            updateAnimation = true;
        }

        if (NastyState == NastyState.Birth &&
            _birthTimer > BirthPeriod / 4)
        {
            updateAnimation = true;
        }

        return updateAnimation;
    }

    public void PerformPostChangeScreenActions()
    {
        // Stopped rather than left running, or particles still in the air would be drawn over the
        // screen Blob has just walked onto.
        _nastyDies.Stop();
        _nastyDiesType2.Stop();
        _nastyArrives.Stop();
        _shockwave.Stop();

        if (NastyState == NastyState.Disabled)
        {
            NastyState = NastyState.Conception;
        }
    }

    public void Draw(int screenFade)
    {
        CheckSpritesInUse();

        int nastyImage = (int)_nastyType * FramesPerNasty;

        if (_nastyType == NastyType.CoreGuardian)
        {
            DrawCoreGuardian(nastyImage, screenFade);

            return;
        }

        switch (NastyState)
        {
            case NastyState.Birth:
            {
                DrawBirth(nastyImage, screenFade);

                break;
            }
            case NastyState.Normal:
            {
                DrawLiveNasty(nastyImage, screenFade);

                break;
            }
            case NastyState.Dead:
            {
                DrawDeath(nastyImage, screenFade);

                break;
            }
        }
    }

    /// <summary>
    ///     One logic frame of spin. The speed follows what this nasty is doing, the same thing that
    ///     decides which of the four draw methods runs: the guardian rocks, a nasty being born spins
    ///     down to square, a corpse spins away, and a live nasty is still.
    /// </summary>
    private void AdvanceRotation()
    {
        float speed = NastyState switch
        {
            NastyState.Birth => _birthRotateSpeed,
            NastyState.Dead => DeathRotateSpeed,
            _ => NastyType == NastyType.CoreGuardian ? NastyRockSpeed : 0f
        };

        if (speed == 0f)
        {
            return;
        }

        if (NastyState == NastyState.Dead && _deathAnticlockwise)
        {
            _rotation = _rotation > 0f ? _rotation - speed : FullTurn;
        }
        else
        {
            _rotation = _rotation < FullTurn ? _rotation + speed : 0f;
        }
    }

    private void DrawCoreGuardian(int nastyImage, int screenFade)
    {
        _nastySprite.Begin(BlendState.Additive);

        _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
            Colours.WhiteFadedTo((byte)screenFade), 1.0f, MathHelper.ToRadians(_rotation));
        _nastySprite.End();
    }

    /// <summary>
    ///     The shader that eats an organic corpse away, pointed at this nasty: its own square of the
    ///     sheet, how far gone it is, and the colour it splatters.
    /// </summary>
    private Effect ComeApartShader(int frame)
    {
        Effect splat = _resources.GetEffect(Assets.Shaders.Splat);

        splat.Parameters["SpriteRect"].SetValue(_nastySprite.SpriteRectIn01(frame));
        splat.Parameters["Decay"].SetValue(
            SplatSettings.DecayFrom(_deathFadeLevel, ColourLevels.Full));
        splat.Parameters["EdgeSoftness"].SetValue(SplatSettings.EdgeSoftness);
        splat.Parameters["RimWidth"].SetValue(SplatSettings.RimWidth);
        splat.Parameters["RimColour"].SetValue(NastyColours.SplatColourFor(_nastyType));
        splat.Parameters["RimBrightness"].SetValue(SplatSettings.RimBrightness);
        splat.Parameters["Lumpiness"].SetValue(SplatSettings.Lumpiness);

        return splat;
    }

    /// <summary>
    ///     A nasty materialises spinning and part-coloured, so it is drawn at its own scale and
    ///     rotation rather than the walk cycle's.
    /// </summary>
    /// <remarks>
    ///     The spin, the growth and the blue it arrives on are the tint and the transform. The shader
    ///     adds the two things a sprite batch cannot: a line sweeping down the sprite leaving the
    ///     nasty behind it, and the three colour channels pulled apart and converging. It is given
    ///     this sprite's own rectangle in the sheet, because everything it samples must stay inside
    ///     it: the nasty's neighbours are the next frames along.
    /// </remarks>
    private void DrawBirth(int nastyImage, int screenFade)
    {
        int frame = nastyImage + _nastyAnim.CurrentFrame;

        Vector4 spriteRect = _nastySprite.SpriteRectIn01(frame);

        Effect materialise = _resources.GetEffect(Assets.Shaders.Materialise);

        materialise.Parameters["SpriteRect"].SetValue(spriteRect);
        materialise.Parameters["Progress"].SetValue(
            MaterialiseSettings.ProgressThrough(_birthTimer, BirthPeriod));

        // The spread is a fraction of the sprite but the shader samples in sheet coordinates, so it
        // is scaled by how much of the sheet this sprite takes up.
        materialise.Parameters["ChromaticSpread"].SetValue(
            new Vector2(MaterialiseSettings.ChromaticSpread * spriteRect.Z, 0f));
        materialise.Parameters["ScanSoftness"].SetValue(MaterialiseSettings.ScanSoftness);
        materialise.Parameters["BandWidth"].SetValue(MaterialiseSettings.BandWidth);
        materialise.Parameters["BandColour"].SetValue(MaterialiseSettings.BandColour);
        materialise.Parameters["BandBrightness"].SetValue(MaterialiseSettings.BandBrightness);

        // The sprite's own spin, so the shader can take it back out and sweep down the screen rather
        // than down the nasty.
        float spin = MathHelper.ToRadians(_rotation);

        materialise.Parameters["SpinSineCosine"].SetValue(
            new Vector2((float)Math.Sin(spin), (float)Math.Cos(spin)));

        _nastyArrives.Draw();

        _nastySprite.Begin(BlendState.AlphaBlend, null, materialise);

        _nastySprite.DrawSprite(frame, XPos, YPos,
            Colours.GetXna4Colour((byte)_birthColourLevel, (byte)_birthColourLevel, ColourLevels.Full,
                (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), _birthScale,
            MathHelper.ToRadians(_rotation));
        _nastySprite.End();
    }

    /// <summary>
    ///     A nasty going about its business. One shot but not yet killed is smoking, drawn as the
    ///     same frame three times over in additive red and green.
    /// </summary>
    private void DrawLiveNasty(int nastyImage, int screenFade)
    {
        if (Smoking)
        {
            _nastySprite.Begin(BlendState.Additive);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                Colours.GetXna4Colour(ShotNastyGrey, ShotNastyGrey, ShotNastyGrey, (byte)screenFade));
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                Colours.GetXna4Colour(ColourLevels.Full, 0, 0, (byte)screenFade));
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                Colours.GetXna4Colour(0, ConveyorNastyGreen, 0, (byte)screenFade));
        }
        else
        {
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                Colours.WhiteFadedTo((byte)screenFade));
        }

        _nastySprite.End();
    }

    /// <summary>
    ///     The corpse shrinks and spins away while the explosion plays over it.
    /// </summary>
    private void DrawDeath(int nastyImage, int screenFade)
    {
        // Under the explosion, so the flames and sparks come out of the ring rather than through it.
_shockwave.Draw(screenFade);

        _nastyDies.Draw();
        _nastyDiesType2.Draw();

        if (_deathFadeLevel > 0)
        {
            int frame = nastyImage + _nastyAnim.CurrentFrame;

            // A machine's corpse fades out whole. A creature's is eaten away from its edges by the
            // shader instead, so it is drawn at full strength and the dissolve does the
            // disappearing. Fading it as well would mute one against the other.
            Effect? splat = IsMechanical ? null : ComeApartShader(frame);

            byte fade = splat is null
                ? (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))
                : (byte)screenFade;

            _nastySprite.Begin(BlendState.AlphaBlend, null, splat);
            _nastySprite.DrawSprite(frame, XPos, YPos,
                Colours.WhiteFadedTo(fade), _deathScale,
                MathHelper.ToRadians(_rotation));
            _nastySprite.End();
        }

        if (_graphicsSet.IsOriginal)
        {
            SpriteCollection blobFire2Original = _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original);
            blobFire2Original.Begin(BlendState.AlphaBlend);
            blobFire2Original.DrawSprite(
                _nastyDiesAnimation.CurrentFrame, XPos, YPos,
                Colours.WhiteFadedTo(screenFade), 1f, 0f);
            blobFire2Original.End();
        }
    }
}
