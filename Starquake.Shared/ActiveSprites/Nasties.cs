// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Engines;
using Starquake.Helpers;
using Starquake.Particles;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

// This wraps multiple nasties
public class Nasties : GameComponentBase
{
    /// <summary>
    ///     A nasty parked this far off screen is one that has not been given a home yet, so it is
    ///     skipped rather than drawn or tested.
    /// </summary>
    private const int UnplacedNastyPosition = -1000;

    /// <summary>
    ///     The size of the blob's shot, for testing it against a nasty.
    /// </summary>
    private const int FireWidth = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="FireWidth" />
    private const int FireHeight = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     The reach of an electricity node's arc, which spans a whole tile across but only a
    ///     collision cell down.
    /// </summary>
    private const int ElectricityArcWidth = TileMetrics.TileWidth;

    /// <inheritdoc cref="ElectricityArcWidth" />
    private const int ElectricityArcHeight = TileMetrics.CollisionCellHeight;

    private readonly List<Nasty> _nasties;
    private readonly int _nastyCount = 1;
    private readonly IPositionalTriggerParticleEffect _nastySmokes;
    private int _previousScreenVisited;

    public Nasties(GameEngine gameEngineReference, int nastyCount)
        : base(gameEngineReference)
    {
        _nastyCount = nastyCount;
        _nasties = new List<Nasty>();
        _nastySmokes =
            (IPositionalTriggerParticleEffect)GameEngine.ContentResourceManager.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

    }
    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    #region IGameComponent Members

    public void LoadContent(ContentList content)
    {
        // Create double the number needed for screen swap operations
        _nasties.Clear();
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties.Add(
                new Nasty(
                    GameEngine,
                    0,
                    0,
                    3));
        }
    }

    public void Initialise()
    {
        for (var a = 0; a < _nastyCount * 2; a++)
        {
            _nasties[a].Initialise();
        }

        // Maybe we should call this on start of the game for all involved....?
        PerformPostChangeScreenActions();
    }

    public void Reset()
    {
        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].NastyState = NastyState.PreConception;
        }

        PerformPostChangeScreenActions();
    }

    public override void Update(GameTime gameTime)
    {
        if (GameEngine.Player.BlobsState != BlobState.Normal)
        {
            Reset();
            return;
        }

        // Cancel any nasty collision effect
        GameEngine.Player.BlobTouchedByNasty = false;

        if (Keyboard.GetState().IsKeyDown(Keys.R))
        {
                for (var a = 0; a < _nastyCount; a++)
                {
                    _nasties[a].NastyState = NastyState.Dead;
                }
        }

        // Only the first nasty touching blob drains his energy - all of them at once is too quick
        // and unfair
        var nastyInteractingWithBlob = false;
        var nastySpawnedCounter = 0;

        for (var a = 0; a < _nastyCount; a++)
        {
            if (_nasties[a].NastyState == NastyState.Normal)
            {
                nastySpawnedCounter++;
            }

            // Setting the coordinates to a setting less than 0 will suspect all updates
            if (_nasties[a].XPos <= UnplacedNastyPosition ||
                _nasties[a].YPos <= UnplacedNastyPosition)
            {
                continue;
            }

            _nasties[a].Update(gameTime);

            TouchBlob(a, ref nastyInteractingWithBlob);
            TakeAnyFireHit(a);
            ElectrifyOnGateContact(a);
        }

        if (nastySpawnedCounter == _nastyCount)
        {
                GameEngine.ContentResourceManager.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
        }

        if (_nastySmokes != null)
        {
                _nastySmokes.Update(gameTime);
        }
    }

    /// <summary>
    ///     The blob's box is pulled in by eight pixels all round so that a near miss is not counted as a
    ///     touch. Only the first nasty to make contact in a frame raises the event.
    /// </summary>
    private void TouchBlob(int a, ref bool nastyInteractingWithBlob)
    {
        if (CollisionDetection.Check(
                GameEngine.Player.BlobXPos + 8,
                GameEngine.Player.BlobYPos + 8,
                GameEngine.Player.BlobWidth - 16,
                GameEngine.Player.BlobHeight - 16,
                _nasties[a].XPos,
                _nasties[a].YPos,
                _nasties[a].NastyWidth,
                _nasties[a].NastyHeight))
        {
            if (_nasties[a].NastyState == NastyState.Normal)
            {
                // Raise the event
                if (OnNastyInteractionWithBlob != null)
                {
                    if (!nastyInteractingWithBlob)
                    {
                        nastyInteractingWithBlob = true;
                        OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, _nasties[a].NastyType);
                    }
                }

                // Trigger the blob animation
                GameEngine.Player.BlobTouchedByNasty = true;
            }
        }
    }

    private void TakeAnyFireHit(int a)
    {
        if (GameEngine.Player.BlobHasFired)
        {
            if (CollisionDetection.Check(
                    GameEngine.Player.BlobFireX,
                    GameEngine.Player.BlobFireY,
                    FireWidth,
                    FireHeight,
                    _nasties[a].XPos,
                    _nasties[a].YPos,
                    _nasties[a].NastyWidth,
                    _nasties[a].NastyHeight))
            {
                if (_nasties[a].NastyState == NastyState.Normal ||
                    _nasties[a].NastyState == NastyState.Birth)
                {
                    GameEngine.Player.RegisterFireHit();
                    _nasties[a].NastyState = NastyState.Shot;

                    if (_nasties[a].NastyType == NastyType.Spiky)
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("BlobExplodes");
                    }
                    else
                    {
                        GameEngine.ContentResourceManager.GetSoundFxEngine().PlaySound("NastyDeath");
                    }

                    if (OnNastyInteractionWithBlob != null)
                    {
                        OnNastyInteractionWithBlob(NastyInteractionType.BlobShot,
                            _nasties[a].NastyType);
                    }
                }
            }
        }
    }

    private void ElectrifyOnGateContact(int a)
    {
        _nasties[a].Smoking = false;

        if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers != null)
        {
            for (var b = 0;
                 b < GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers.Length;
                 b++)
            {
                if (GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].ElectricityOn)
                {
                    if (CollisionDetection.Check(
                            GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                .X,
                            GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                .Y,
                            ElectricityArcWidth,
                            ElectricityArcHeight,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight))
                    {
                        var nastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                            _nasties[a].Speed,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight,
                            GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                .X,
                            GameEngine.ElectricityNodeGateEngine.ElectricityNodeControllers[b].Location
                                .Y,
                            ElectricityArcWidth,
                            ElectricityArcHeight);

                        if (nastyTravelDirection != Direction.None &&
                            _nasties[a].NastyState == NastyState.Normal)
                        {
                            if (_nastySmokes != null)
                            {
                                _nasties[a].Smoking = true;
                                _nastySmokes.Start(
                                    SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                        _nasties[a].NastyWidth, _nasties[a].NastyHeight).X +
                                    _nasties[a].XPos,
                                    SpriteHelpers.GetSpriteLocationOffset(_nasties[a].Direction,
                                        _nasties[a].NastyWidth, _nasties[a].NastyHeight).Y +
                                    _nasties[a].YPos);
                            }
                        }
                    }
                }
            }
        }
    }

    private void Swap(List<Nasty> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }

    /// <summary>
    ///     One screen in this many is populated with a single kind of nasty instead of a mixture.
    /// </summary>
    private const int SameNastyScreenOdds = 4;

    /// <summary>
    ///     Hands the screen's nasties over on a screen change. Walking back the way you came gets the
    ///     same nasties you left, kept in the second half of the array; going anywhere else gets a
    ///     fresh batch and puts the ones you left behind into that half.
    /// </summary>
    public void PerformPostChangeScreenActions()
    {
        var goingBackTheWayWeCame = GameEngine.GameScreens.ScreenNumber == _previousScreenVisited;

        if (goingBackTheWayWeCame)
        {
            for (var a = 0; a < _nastyCount; a++)
            {
                Swap(_nasties, a, a + _nastyCount);
            }
        }
        else
        {
            StoreNastiesFromScreenWeLeft();
            CreateFreshNasties();
        }

        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].PerformPostChangeScreenActions();
        }

        DisableNastiesDisplacedByConveyerNasties();

        _nastySmokes?.Reset();

        _previousScreenVisited = GameEngine.GameScreens.PreviousScreenNumber;
    }

    /// <summary>
    ///     Saves the nasties of the screen being left into the second half of the array, where they
    ///     wait in case the blob comes straight back. Any that were dead are revived first, so a screen
    ///     is never returned to empty.
    /// </summary>
    private void StoreNastiesFromScreenWeLeft()
    {
        for (var a = 0; a < _nastyCount; a++)
        {
            if (_nasties[a].NastyState is NastyState.Dead or NastyState.Shot)
            {
                _nasties[a].NastyState = NastyState.PreConception;
            }

            _nasties[a + _nastyCount].CopyFromSource(_nasties[a]);
        }
    }

    /// <summary>
    ///     A new screen's worth of nasties. One screen in four is populated with a single kind rather
    ///     than a mixture, which is what makes some rooms feel like they belong to something.
    /// </summary>
    private void CreateFreshNasties()
    {
        var screenData = GameEngine.GameScreens.CurrentGameScreenData;

        screenData.KeepNastiesTheSame =
            GameEngine.RandomNumberGenerator.Next(0, SameNastyScreenOdds) == 0;

        var nastyType = screenData.KeepNastiesTheSame
            ? Nasty.GetRandomNastyType(GameEngine, true)
            : NastyType.Ghost;

        for (var a = 0; a < _nastyCount; a++)
        {
            _nasties[a].KeepNastyTheSame = screenData.KeepNastiesTheSame;
            _nasties[a].NastyState = NastyState.PreConception;

            if (screenData.KeepNastiesTheSame)
            {
                _nasties[a].NastyType = nastyType;
            }
        }
    }

    /// <summary>
    ///     A conveyer nasty takes the place of an ordinary one rather than adding to the count, so the
    ///     first few are switched off.
    /// </summary>
    private void DisableNastiesDisplacedByConveyerNasties()
    {
        for (var a = 0; a < GameEngine.ConveyerNastiesInstance.NastiesActive; a++)
        {
            _nasties[a].NastyState = NastyState.Disabled;
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (GameEngine.Player.BlobsState == BlobState.Normal)
        {
            for (var a = 0; a < _nastyCount; a++)
            {
                if (_nasties[a].XPos > UnplacedNastyPosition &&
                    _nasties[a].YPos > UnplacedNastyPosition)
                {
                    _nasties[a].Draw(gameTime, screenFade);
                }
            }

            if (_nastySmokes != null)
            {
                _nastySmokes.Draw(screenFade);
            }
        }
    }

    #endregion

}