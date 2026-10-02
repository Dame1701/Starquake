// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.ActiveSprites.NastyBehaviour;
using Starquake.Animations;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

internal class Nasty
{

    // The number of frames for the birth to appear (60 frames = 1 second)
    private const int BirthPeriod = 60;

    /// <summary>
    ///     The nasty's walk cycle. It plays back and forth rather than looping, which is what gives
    ///     the legs their scuttle.
    /// </summary>
    private const int NastyLastFrame = 3;

    /// <inheritdoc cref="NastyLastFrame" />
    private const int NastyAnimationDelay = 4;

    /// <summary>
    ///     How many frames of the sprite sheet each kind of nasty occupies, which is what turns a
    ///     <see cref="NastyType" /> into the first sprite of its walk cycle.
    /// </summary>
    private const int FramesPerNasty = 4;

    /// <summary>
    ///     How fast the nasty spins as it materialises, and how fast it is still spinning by the time
    ///     it has finished. It slows from the one to the other over the second half of the birth.
    /// </summary>
    private const float BirthStartRotateSpeed = 8.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthRespawnRotateSpeed = 32.0f;

    /// <inheritdoc cref="BirthStartRotateSpeed" />
    private const float BirthEndRotateSpeed = 16f;

    /// <summary>
    ///     The longest the birth will keep spinning past <see cref="BirthPeriod" /> waiting to come
    ///     to rest upright. One revolution at the speed the spin settles to is about twenty-four
    ///     frames, so this is a little over a full turn - long enough that the nasty normally does
    ///     land square, short enough that it is not left spinning if it never gets the chance to.
    /// </summary>
    private const int BirthSpinOutLimit = 30;

    /// <summary>
    ///     The colours only fade in over the back half of the birth, so both fades count up at twice
    ///     the rate the birth itself runs at.
    /// </summary>
    private const int BirthColourFadeHalf = 2;

    /// <summary>
    ///     How long a nasty lies dead before it is allowed to come back, and the longest it then
    ///     waits before doing so. A nasty is never gone for good - it is only ever biding its time.
    /// </summary>
    private const int DeadPauseFrames = 50;

    /// <inheritdoc cref="DeadPauseFrames" />
    private const int LongestConceptionWait = 500;

    /// <inheritdoc cref="DeadPauseFrames" />
    private const int ShortestConceptionWaitOnCoreScreen = 60;

    /// <summary>
    ///     How quickly a dead nasty shrinks away, and how fast the corpse spins as it goes.
    /// </summary>
    private const float DeathShrinkStep = 0.01f;

    /// <inheritdoc cref="DeathShrinkStep" />
    private const float DeathRotateSpeed = 16f;

    /// <inheritdoc cref="DeathShrinkStep" />
    private const int DeathFadeStep = 20;

    /// <summary>
    ///     How fast a live nasty rocks from side to side as it walks.
    /// </summary>
    private const float NastyRockSpeed = 4f;

    /// <summary>
    ///     One whole revolution in degrees, which is where <see cref="_rotation" /> wraps.
    /// </summary>
    private const float FullTurn = 360f;

    /// <summary>
    ///     The colours the different nasties are tinted: grey for one that has been shot, red for a
    ///     spiky one, green for a conveyer one.
    /// </summary>
    private const int ShotNastyGrey = 50;

    /// <inheritdoc cref="ShotNastyGrey" />
    private const int ConveyerNastyGreen = 100;
    private readonly Blob _blob;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;

    // Animation
    private readonly Animation _nastyAnim;
    private readonly IStandardParticleEffect _nastyDies;

    private readonly Animation _nastyDiesAnimation;
    private readonly IStandardParticleEffect _nastyDiesType2;

    private readonly SpriteCollection _nastySpriteNew;
    private readonly SpriteCollection _nastySpriteOriginal;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    private int _birthColourLevel;

    // Birth stuff
    private int _birthFadeLevel;
    private float _birthRotateSpeed = BirthStartRotateSpeed;

    private float _birthScale;
    private int _birthTimer;

    // Counters
    private int _conceptionWaitCounter;

    private bool _deathAnticlockwise;

    // Death stuff
    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    // Behaviour

    private int _nastyDeadPauseCounter;
    //bool attractedToBlobFlag;


    // When shot monsters usually disappear for good but there is a small chance they'll regenerate
    // Tech stuff
    private SpriteCollection _nastySprite;

    private NastyType _nastyType;

    /// <summary>
    ///     Which way round this nasty is drawn, in degrees. The angle used to live on this nasty's
    ///     cloned sprite sheet, advanced once per draw batch - so the spin ran at the display's refresh
    ///     rate rather than the logic clock. It is not carried across a screen swap, which is the same
    ///     as it was: the clones belong to the nasty, and a swap resets a birth or a death anyway.
    /// </summary>
    private float _rotation;

    // Position

    public Nasty(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Blob blob,
        Random randomNumberGenerator,
        int xposition,
        int yposition,
        int moveSpeed)
    {
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

        _nastyDies = (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.NastyDies,
            ParticleEffectType.Triggered, null);
        _nastyDies.ParticleEffectComplete += NastyDies_ParticleEffectComplete;

        _nastyDiesType2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance(Assets.Effects.NastySplat,
                ParticleEffectType.Triggered, null);
        _nastyDiesType2.ParticleEffectComplete += NastyDies_ParticleEffectComplete;
    }

    /// <summary>
    ///     Where this nasty is going and how it decided to. Everything to do with movement lives
    ///     there; this class is its lifecycle and its picture.
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
    ///     Where the nasty is. Held by <see cref="Movement" />, which is the only thing that changes
    ///     it, and forwarded here because everything that draws or tests a nasty asks the nasty.
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
    ///     Takes a snapshot of another nasty, which is how leaving a screen works: twice the number of
    ///     nasties needed are kept, and the set being left is copied into the spare half in case the blob
    ///     comes back to it.
    /// </summary>
    /// <remarks>
    ///     The movement half used to be seventeen assignments here, two of them written twice, out of a
    ///     class with about thirty-five fields - so anything added to the class afterwards was silently
    ///     dropped at every screen change and nothing said so. It is one assignment of a record struct
    ///     now, and what is inside that struct is the answer to which movement state carries.
    ///     <para>
    ///         The lifecycle half is the list below, and the rule it follows is that a counter carries
    ///         only if a state the snapshot can be restored into reads it before setting it. Working
    ///         through the five states a snapshot can hold:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="NastyState.Conception" /> reads <c>_conceptionWaitCounter</c> in
    ///             <see cref="WaitToBeBorn" /> without setting it, so that one has to carry. It did not,
    ///             and a nasty part way through waiting to appear came back from a screen with whatever
    ///             count the spare happened to hold - nought, usually, which made it appear at once.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.Birth" /> is turned into <see cref="NastyState.Normal" /> below,
    ///             so the five birth counters are never read from a snapshot. They are also all set
    ///             together in <see cref="WaitToBeBorn" /> on the frame a birth begins.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.Dead" /> cannot be in a snapshot at all: <c>Nasties</c> turns a
    ///             dead or shot nasty into <see cref="NastyState.PreConception" /> before copying it, so
    ///             a screen is never returned to empty. The death values are set in
    ///             <see cref="StartDying" /> anyway. <c>_nastyDeadPauseCounter</c> is carried regardless,
    ///             so that this method is a complete snapshot on its own terms rather than on the
    ///             caller's.
    ///         </item>
    ///         <item>
    ///             <see cref="NastyState.PreConception" /> sets everything it needs in
    ///             <see cref="BeginConception" />, and <see cref="NastyState.Disabled" /> reads nothing.
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <c>_rotation</c> is deliberately not carried. It is only ever drawn with, and a restored
    ///         nasty starting at the angle the spare was left at looks no different from one starting at
    ///         the angle it had - there is nothing to be right about.
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

        // Ensure that we don't carry on generating after moving screens
        NastyState = copySource.NastyState == NastyState.Birth
            ? NastyState.Normal
            : copySource.NastyState;
    }

    /// <summary>
    ///     Picks somewhere on the current screen for this nasty to appear, or <c>null</c> if the
    ///     screen offers nowhere.
    /// </summary>
    /// <remarks>
    ///     Nowhere used to be reported as <c>Point(0, 0)</c>, which a real location can never be -
    ///     every one of them is offset by half a sprite - but which reads like one at the call site.
    /// </remarks>
    private Point? GetNastySpawnLocation()
    {
        // Get all locations where we can have nasties from the current game screen
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
            // Extend this to pause for random time before bringing a nasty back to life
            _nastyDeadPauseCounter = 0;
            NastyState = NastyState.PreConception;
            _conceptionWaitCounter = _randomNumberGenerator.Next(0, LongestConceptionWait);
        }
    }

    /// <summary>
    ///     Picks a nasty at random. The range runs one past the fifteen types so that Spiky, the
    ///     last, comes up twice as often as the rest - Bird is also drawn twice, at 0 and at 14.
    /// </summary>
    public static NastyType GetRandomNastyType(Random randomNumberGenerator)
    {
        // Go to more than max nasty types to increase chance of four spiky nasties
        int temp = randomNumberGenerator.Next(0, 16);

        if (temp == 0)
        {
            return NastyType.Bird;
        }
        if (temp == 1)
        {
            return NastyType.Helmet;
        }
        if (temp == 2)
        {
            return NastyType.FriedEgg;
        }
        if (temp == 3)
        {
            return NastyType.CurleyWurley;
        }
        if (temp == 4)
        {
            return NastyType.Shellfish;
        }
        if (temp == 5)
        {
            return NastyType.Cushion;
        }
        if (temp == 6)
        {
            return NastyType.Gonzo;
        }
        if (temp == 7)
        {
            return NastyType.Snail;
        }
        if (temp == 8)
        {
            return NastyType.Ghost;
        }
        if (temp == 9)
        {
            return NastyType.Centipede;
        }
        if (temp == 10)
        {
            return NastyType.Scorpion;
        }
        if (temp == 11)
        {
            return NastyType.DragonFly;
        }
        if (temp == 12)
        {
            return NastyType.Hippo;
        }
        if (temp == 13)
        {
            return NastyType.Helicopter;
        }
        if (temp == 14)
        {
            return NastyType.Bird;
        }
        return NastyType.Spiky;
    }

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

        Movement.Step(_blob.BlobXPos, _blob.BlobYPos);
    }

    /// <summary>
    ///     Picks how long this nasty waits to be conceived, and what kind of nasty it is going to be
    ///     when it gets there.
    /// </summary>
    private void BeginConception()
    {
        _conceptionWaitCounter = _randomNumberGenerator.Next(0, ShortestConceptionWaitOnCoreScreen);
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
            // Where it is going has to be settled before anything announces that it is going: a
            // screen with nowhere to put it disables it instead, and the appear sound, the first
            // movement and the birth counters all belong to a nasty that is actually being born.
            // This used to set Disabled and then overwrite it with Birth on the next line, so a
            // nasty with nowhere to go was born at whatever position it last held.
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
            _birthScale = _birthScale + 1f / BirthPeriod;
        }

        if (_birthScale > 1.0f)
        {
            _birthScale = 1.0f;
        }


        // Fade the nasty in
        if (_birthFadeLevel < ColourLevels.Full)
        {
            _birthFadeLevel =
                _birthFadeLevel +
                (int)(ColourLevels.Full /
                      ((double)BirthPeriod /
                       BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
        }


        // Once we're halfway through fade in the colours
        if (_birthTimer > BirthPeriod / BirthColourFadeHalf)
        {
            if (_birthColourLevel < ColourLevels.Full)
            {
                _birthColourLevel =
                    _birthColourLevel +
                    (int)(ColourLevels.Full /
                          ((double)BirthPeriod /
                           BirthColourFadeHalf)); // We only have half the time to do the colour fade in 
            }

            if (_birthRotateSpeed > BirthEndRotateSpeed)
            {
                _birthRotateSpeed = _birthRotateSpeed - BirthEndRotateSpeed / ((float)BirthPeriod / BirthColourFadeHalf);
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
            // The nasty prefers to stop spinning square, but it must stop either way.
            //
            // This used to end the birth only on a frame where the rotation read exactly zero, and
            // zero is read for a single frame in every revolution - the wrap assigns the literal,
            // and the settled speed does not divide 360. The angle was also advanced on the draw
            // path while this ran on the update path, so a frame drawn without an update spun it
            // past zero with nobody watching, and a missed frame cost a whole further revolution.
            // That is the nasty that span and never stabilised. The angle is now advanced by this
            // nasty's own update, so the two cannot drift apart, and the test is a range rather
            // than an equality. The spin-out limit stays as the backstop.
            if (_rotation < _birthRotateSpeed ||
                _birthTimer > BirthPeriod + BirthSpinOutLimit)
            {
                NastyState = NastyState.Normal;
                _birthTimer = 0;
            }
        }

        _birthTimer++;
    }

    private void StartDying()
    {
        _deathFadeLevel = ColourLevels.Full;
        _deathScale = 1.0f;

        if (_nastyType == NastyType.Spiky ||
            _nastyType == NastyType.Conveyer)
        {
            _nastyDies.Start();
        }
        else
        {
            NastyHelpers.ApplyNastyColours(_nastyType, _nastyDiesType2.Effect);
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
        // Fade the nasty out as it explodes
        if (_deathFadeLevel > 0)
        {
            _deathFadeLevel = _deathFadeLevel - DeathFadeStep;
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

        if (_graphicsSet.IsOriginal)
        {
            _nastyDiesAnimation.GetNextFrame();

            if (_nastyDeadPauseCounter < DeadPauseFrames)
            {
                _nastyDeadPauseCounter++;
            }
            else
            {
                NastyState = NastyState.PreConception;
                _conceptionWaitCounter = _randomNumberGenerator.Next(0, LongestConceptionWait);
                _nastyDeadPauseCounter = 0;
            }
        }
    }

    private void UpdateDeathEffects(GameTime gameTime)
    {
        // Update the explosion
        _nastyDies.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);

        // Update the splat
        _nastyDiesType2.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
    }

    /// <summary>
    ///     A nasty is only animated once it is fully real - or a quarter of the way through being born,
    ///     which is where its legs start showing.
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
        // Stop the explosion effect leaking onto the next screen
        _nastyDies.Stop();
        _nastyDiesType2.Stop();

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
    ///     One logic frame of spin. Which speed applies is decided by what this nasty is doing, which
    ///     is the same thing that decides which of the four draw methods runs - the guardian rocks,
    ///     a nasty being born spins down to square, a corpse spins away, and a live nasty is still.
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

        // Choose the image
        _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade), 1.0f, MathHelper.ToRadians(_rotation));
        _nastySprite.End();
    }

    /// <summary>
    ///     A nasty materialises spinning and part-coloured, so it is drawn at its own scale and rotation
    ///     rather than the walk cycle's.
    /// </summary>
    private void DrawBirth(int nastyImage, int screenFade)
    {
        _nastySprite.Begin(BlendState.AlphaBlend);

        // Choose the image
        _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
            ColourHelper.GetXna4Colour((byte)_birthColourLevel, (byte)_birthColourLevel, ColourLevels.Full,
                (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), _birthScale,
            MathHelper.ToRadians(_rotation));
        _nastySprite.End();
    }

    private void DrawLiveNasty(int nastyImage, int screenFade)
    {
        // Choose the image
        if (Smoking)
        {
            _nastySprite.Begin(BlendState.Additive);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ShotNastyGrey, ShotNastyGrey, ShotNastyGrey, (byte)screenFade));
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, 0, 0, (byte)screenFade));
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(0, ConveyerNastyGreen, 0, (byte)screenFade));
        }
        else
        {
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade));
        }

        _nastySprite.End();
    }

    /// <summary>
    ///     The corpse shrinks and spins away while the explosion plays over it.
    /// </summary>
    private void DrawDeath(int nastyImage, int screenFade)
    {
        // Draw the explosion and the splat
        _nastyDies.Draw();
        _nastyDiesType2.Draw();

        if (_deathFadeLevel > 0)
        {
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                    (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale,
                MathHelper.ToRadians(_rotation));
            _nastySprite.End();
        }

        if (_graphicsSet.IsOriginal)
        {
            SpriteCollection blobFire2Original = _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original);
            blobFire2Original.Begin(BlendState.AlphaBlend);
            blobFire2Original.DrawSprite(
                _nastyDiesAnimation.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, screenFade), 1f, 0f);
            blobFire2Original.End();
        }
    }

}
