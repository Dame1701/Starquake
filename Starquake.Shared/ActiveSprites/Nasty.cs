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

public class Nasty : GameComponentBase
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

    /// <summary>
    ///     How fast a live nasty rocks from side to side as it walks.
    /// </summary>
    private const float NastyRockSpeed = 4f;

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
    private readonly GameSession _session;

    private int _birthColourLevel;

    // Birth stuff
    private int _birthFadeLevel;
    private float _birthRotateSpeed = BirthStartRotateSpeed;

    private float _birthScale;
    private int _birthTimer;

    // Counters
    private int _conceptionwaitCounter;

    private bool _deathAnticlockwise;

    // Death stuff
    private int _deathFadeLevel = ColourLevels.Full;
    private float _deathScale = 1f;

    // Behaviour

    private int _nastyDeadPauseCounter;
    //bool attractedToBlobFlag;


    // When shot monsters usually dissappear for good but there is a small chance they'll regenerate
    // Tech stuff
    private SpriteCollection _nastySprite;

    private NastyType _nastyType;


    // Position

    public Nasty(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        GameSession session,
        Blob blob,
        Random randomNumberGenerator,
        int xposition,
        int yposition,
        int moveSpeed)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _session = session;
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

        _nastySpriteOriginal = _resources.GetSpriteCollection("NastiesOriginal")
            .Clone();
        _nastySpriteNew = _resources.GetSpriteCollection("Nasties").Clone();

        CheckSpritesInUse();

        _nastyDies = (IStandardParticleEffect)_resources.GetParticleEffectInstance("NastyDies",
            ParticleEffectType.Triggered, null);
        _nastyDies.ParticleEffectComplete += NastyDies_ParticleEffectComplete;

        _nastyDiesType2 =
            (IStandardParticleEffect)_resources.GetParticleEffectInstance("NastySplat",
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
    ///     Takes over another nasty's state, which is how a screen swap works: twice the number of
    ///     nasties needed are kept, and the spare set is given the live set's state rather than being
    ///     built again.
    /// </summary>
    /// <remarks>
    ///     The movement half used to be seventeen assignments here, two of them written twice, out of
    ///     a class with about thirty-five fields - so anything added to the class afterwards was
    ///     silently dropped at every screen change and nothing said so. It is one assignment of a
    ///     record struct now, and what is inside that struct is the answer to which state carries.
    /// </remarks>
    public void CopyFromSource(Nasty copySource)
    {
        Movement.State = copySource.Movement.State;
        Movement.NastyType = copySource.Movement.NastyType;
        Movement.XPos = copySource.Movement.XPos;
        Movement.YPos = copySource.Movement.YPos;

        KeepNastyTheSame = copySource.KeepNastyTheSame;
        _nastyType = copySource._nastyType;
        _nastyDeadPauseCounter = copySource._nastyDeadPauseCounter;

        // If the basty is dead reset it to conceptive state
        if (copySource.NastyState == NastyState.Dead)
        {
            NastyState = NastyState.PreConception;
        }

        // Ensure that we don't carry on generating after moving screens
        if (copySource.NastyState == NastyState.Birth)
        {
            NastyState = NastyState.Normal;
        }
        else
        {
            NastyState = copySource.NastyState;
        }
    }

    #region IGameComponent Members

    private Point GetNastySpawnLocation()
    {
        // Get all locations where we can have nasties from the current game screen
        List<Point> potentialLocations = _gameScreens.GetNastyPotentialLocations();

        if (potentialLocations.Count > 0)
        {
            int nastyLocationIndex = _randomNumberGenerator.Next(0, potentialLocations.Count);

            // Return a location
            return new Point(potentialLocations[nastyLocationIndex].X + SpriteMetrics.HalfSpriteWidth,
                potentialLocations[nastyLocationIndex].Y + 16);
        }

        // this should never happen
        return new Point(0, 0);
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
            _conceptionwaitCounter = _randomNumberGenerator.Next(0, LongestConceptionWait);
        }
    }

    public void Initialise()
        => Movement.Reset();

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
            return NastyType.Scorpian;
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

    public override void Update(GameTime gameTime)
    {
        if (_gameScreens.ScreenNumber == WorldMap.CoreApproachScreenNumber)
        {
            NastyState = NastyState.Disabled;
        }

        if (_gameScreens.ScreenNumber == WorldMap.CoreScreenNumber)
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
        _conceptionwaitCounter = _randomNumberGenerator.Next(0, ShortestConceptionWaitOnCoreScreen);
        NastyState = NastyState.Conception;

        if (!KeepNastyTheSame)
        {
            NastyType = GetRandomNastyType(_randomNumberGenerator);
        }

        Movement.PrepareForConception();
    }

    private void WaitToBeBorn()
    {
        if (_conceptionwaitCounter > 0)
        {
            _conceptionwaitCounter--;
        }
        else
        {
            _resources.GetSoundFxEngine().PlaySound("NastyAppear", 0.1F, false);
            Movement.StartFirstMovement();
            _deathFadeLevel = ColourLevels.Full;
            _deathScale = 1f;
            _birthFadeLevel = 0;
            _birthScale = 0f;
            _birthColourLevel = 0;
            _birthRotateSpeed = BirthRespawnRotateSpeed;
            _birthTimer = 0;

            // Select a new location to spawn
            Point newPosition = GetNastySpawnLocation();
            if (newPosition == new Point(0, 0))
            {
                NastyState = NastyState.Disabled;
            }
            else
            {
                XPos = newPosition.X;
                YPos = newPosition.Y;
            }

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
            if (_nastySprite.SpriteRotation == 0)
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
                _conceptionwaitCounter = _randomNumberGenerator.Next(0, LongestConceptionWait);
                _nastyDeadPauseCounter = 0;
            }
        }
    }

    private void UpdateDeathEffects(GameTime gameTime)
    {
        if (_nastyDies != null)
        {
            // Update the explosion
            _nastyDies.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }

        if (_nastyDiesType2 != null)
        {
            // Update the splat
            _nastyDiesType2.Update(XPos + SpriteMetrics.HalfSpriteWidth, YPos + SpriteMetrics.HalfSpriteHeight, gameTime);
        }
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
        if (_nastyDies != null)
        {
            _nastyDies.Stop();
        }

        if (_nastyDiesType2 != null)
        {
            _nastyDiesType2.Stop();
        }

        if (NastyState == NastyState.Disabled)
        {
            NastyState = NastyState.Conception;
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
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

    private void DrawCoreGuardian(int nastyImage, int screenFade)
    {
        _nastySprite.Begin(BlendState.Additive);

        // Choose the image
        _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
            ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, (byte)screenFade), 1.0f, NastyRockSpeed, true, _session.Paused);
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
                (byte)(ColourLevels.Full * ((double)screenFade / ColourLevels.Full))), _birthScale, _birthRotateSpeed,
            true, _session.Paused);
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
        // Draw the explosion
        if (_nastyDies != null)
        {
            _nastyDies.Draw();
        }

        if (_nastyDiesType2 != null)
        {
            _nastyDiesType2.Draw();
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
            _nastySprite.Begin(BlendState.AlphaBlend);
            _nastySprite.DrawSprite(nastyImage + _nastyAnim.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full,
                    (byte)(_deathFadeLevel * ((double)screenFade / ColourLevels.Full))), _deathScale, DeathRotateSpeed,
                _deathAnticlockwise, _session.Paused);
            _nastySprite.End();
        }

        if (_graphicsSet.IsOriginal)
        {
            SpriteCollection blobFire2Original = _resources.GetSpriteCollection("BlobFire2Original");
            blobFire2Original.Begin(BlendState.AlphaBlend);
            blobFire2Original.DrawSprite(
                _nastyDiesAnimation.CurrentFrame, XPos, YPos,
                ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, screenFade), 1f, 0f);
            blobFire2Original.End();
        }
    }

    #endregion

}
