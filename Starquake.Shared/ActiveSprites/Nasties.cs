// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Helpers;
using Starquake.Input;
using Starquake.Particles;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

// This wraps multiple nasties
internal class Nasties : GameComponentBase
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
    private readonly Blob _blob;
    private readonly ConveyerNasties _conveyerNasties;
    private readonly ElectricityGatesAndNodesEngine _electricityNodeGates;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly ToggleKeys _input;

    private readonly List<Nasty> _nasties;
    private readonly int _nastyCount;
    private readonly IPositionalTriggerParticleEffect _nastySmokes;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;
    private readonly GameSession _session;
    private int _previousScreenVisited;

    public Nasties(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        GameSession session,
        Blob blob,
        Random randomNumberGenerator,
        ConveyerNasties conveyerNasties,
        ElectricityGatesAndNodesEngine electricityNodeGates,
        int nastyCount,
        ToggleKeys input)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _session = session;
        _blob = blob;
        _randomNumberGenerator = randomNumberGenerator;
        _conveyerNasties = conveyerNasties;
        _electricityNodeGates = electricityNodeGates;
        _input = input;

        _nastyCount = nastyCount;
        _nasties = new List<Nasty>();
        _nastySmokes =
            (IPositionalTriggerParticleEffect)_resources.GetParticleEffectInstance("BasicSmokePlume",
                ParticleEffectType.TriggeredMultiLocationTimed, null);

    }
    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    #region IGameComponent Members

    public override void LoadContent()
    {
        // Create double the number needed for screen swap operations
        _nasties.Clear();
        for (int a = 0; a < _nastyCount * 2; a++)
        {
            _nasties.Add(
                new Nasty(
                    _resources,
                    _gameScreens,
                    _graphicsSet,
                    _session,
                    _blob,
                    _randomNumberGenerator,
                    0,
                    0,
                    3));
        }
    }

    public void Initialise()
    {
        for (int a = 0; a < _nastyCount * 2; a++)
        {
            _nasties[a].Initialise();
        }

        // Maybe we should call this on start of the game for all involved....?
        PerformPostChangeScreenActions();
    }

    public void Reset()
    {
        for (int a = 0; a < _nastyCount; a++)
        {
            _nasties[a].NastyState = NastyState.PreConception;
        }

        PerformPostChangeScreenActions();
    }

    public override void Update(GameTime gameTime)
    {
        if (_blob.BlobsState != BlobState.Normal)
        {
            Reset();
            return;
        }

        // Cancel any nasty collision effect
        _blob.BlobTouchedByNasty = false;

        if (_input.KeyboardState.IsKeyDown(Keys.R))
        {
            for (int a = 0; a < _nastyCount; a++)
            {
                _nasties[a].NastyState = NastyState.Dead;
            }
        }

        // Only the first nasty touching blob drains his energy - all of them at once is too quick
        // and unfair
        bool nastyInteractingWithBlob = false;
        int nastySpawnedCounter = 0;

        for (int a = 0; a < _nastyCount; a++)
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
            _resources.GetSoundFxEngine().EnablePeriodicBackgroundSounds();
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
                _blob.BlobXPos + 8,
                _blob.BlobYPos + 8,
                _blob.BlobWidth - 16,
                _blob.BlobHeight - 16,
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
                _blob.BlobTouchedByNasty = true;
            }
        }
    }

    private void TakeAnyFireHit(int a)
    {
        if (_blob.BlobHasFired)
        {
            if (CollisionDetection.Check(
                    _blob.BlobFireX,
                    _blob.BlobFireY,
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
                    _blob.RegisterFireHit();
                    _nasties[a].NastyState = NastyState.Shot;

                    if (_nasties[a].NastyType == NastyType.Spiky)
                    {
                        _resources.GetSoundFxEngine().PlaySound("BlobExplodes");
                    }
                    else
                    {
                        _resources.GetSoundFxEngine().PlaySound("NastyDeath");
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

        if (_electricityNodeGates.ElectricityNodeControllers != null)
        {
            for (int b = 0;
                 b < _electricityNodeGates.ElectricityNodeControllers.Length;
                 b++)
            {
                if (_electricityNodeGates.ElectricityNodeControllers[b].ElectricityOn)
                {
                    if (CollisionDetection.Check(
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .X,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .Y,
                            ElectricityArcWidth,
                            ElectricityArcHeight,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight))
                    {
                        Direction nastyTravelDirection = CollisionDetection.CheckCollisionDirection(
                            _nasties[a].Speed,
                            _nasties[a].XPos,
                            _nasties[a].YPos,
                            _nasties[a].NastyWidth,
                            _nasties[a].NastyHeight,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
                                .X,
                            _electricityNodeGates.ElectricityNodeControllers[b].Location
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
        bool goingBackTheWayWeCame = _gameScreens.ScreenNumber == _previousScreenVisited;

        if (goingBackTheWayWeCame)
        {
            for (int a = 0; a < _nastyCount; a++)
            {
                Swap(_nasties, a, a + _nastyCount);
            }
        }
        else
        {
            StoreNastiesFromScreenWeLeft();
            CreateFreshNasties();
        }

        for (int a = 0; a < _nastyCount; a++)
        {
            _nasties[a].PerformPostChangeScreenActions();
        }

        DisableNastiesDisplacedByConveyerNasties();

        _nastySmokes?.Reset();

        _previousScreenVisited = _gameScreens.PreviousScreenNumber;
    }

    /// <summary>
    ///     Saves the nasties of the screen being left into the second half of the array, where they
    ///     wait in case the blob comes straight back. Any that were dead are revived first, so a screen
    ///     is never returned to empty.
    /// </summary>
    private void StoreNastiesFromScreenWeLeft()
    {
        for (int a = 0; a < _nastyCount; a++)
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
        GameScreenData screenData = _gameScreens.CurrentGameScreenData;

        screenData.KeepNastiesTheSame =
            _randomNumberGenerator.Next(0, SameNastyScreenOdds) == 0;

        NastyType nastyType = screenData.KeepNastiesTheSame
            ? Nasty.GetRandomNastyType(_randomNumberGenerator)
            : NastyType.Ghost;

        for (int a = 0; a < _nastyCount; a++)
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
        for (int a = 0; a < _conveyerNasties.NastiesActive; a++)
        {
            _nasties[a].NastyState = NastyState.Disabled;
        }
    }

    public override void Draw(GameTime gameTime, int screenFade)
    {
        if (_blob.BlobsState == BlobState.Normal)
        {
            for (int a = 0; a < _nastyCount; a++)
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
