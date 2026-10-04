// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <param name="onNastyExploded">
///     Told when one of these blows itself apart, and handed on to each of them. Every one of them
///     raises it: a nasty on a rail is a machine, and all of them go off when they are shot.
/// </param>
internal class ConveyorNasties(ResourceManager resources,
    GameScreenCollection gameScreens,
    Blob blob,
    Random randomNumberGenerator,
    Action onNastyExploded)
{
    /// <summary>
    ///     How far Blob's box is pulled in each side before a touch counts, so that passing close by a
    ///     nasty is not the same as walking into one.
    /// </summary>
    private const int BlobBoxInset = 8;

    /// <summary>
    ///     The size of Blob's shot, for testing it against a nasty.
    /// </summary>
    private const int FireWidth = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="FireWidth" />
    private const int FireHeight = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How many conveyor nasties exist at once. Three is the most any screen asks for, and they
    ///     are re-used from screen to screen rather than built again.
    /// </summary>
    private const int ConveyorNastyCount = 3;

    /// <summary>
    ///     How fast a nasty may be set running, in pixels a frame. The upper bound is exclusive, as
    ///     <see cref="Random.Next(int,int)" /> takes it, so a nasty runs at one or at two.
    /// </summary>
    private const int SlowestMoveSpeed = 1;

    /// <inheritdoc cref="SlowestMoveSpeed" />
    private const int FastestMoveSpeedExclusive = 3;

    public List<ConveyorNasty> Nasties { get; } = [];

    public int NastiesActive => gameScreens.GetCoordsOfTiles((int)MarkerTile.ConveyorNasty).Count;

    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    public void LoadContent()
    {
        Nasties.Clear();
        for (int a = 0; a < ConveyorNastyCount; a++)
        {
            Nasties.Add(
                new ConveyorNasty(resources,
                    gameScreens,
                    randomNumberGenerator,
                    onNastyExploded,
                    0,
                    0,
                    0,
                    false));
        }
    }

    /// <summary>
    ///     Puts the three nasties where the new screen's markers ask for them. Every one is switched
    ///     off first, so a screen that asks for fewer than the last one leaves the rest idle rather
    ///     than running where they were.
    /// </summary>
    public void PerformPostChangeScreenActions()
    {
        foreach (ConveyorNasty nasty in Nasties)
        {
            nasty.PerformPostChangeScreenActions();
        }

        List<Point> nastiesToGenerate = gameScreens.GetCoordsOfTiles((int)MarkerTile.ConveyorNasty);

        for (int a = 0; a < nastiesToGenerate.Count; a++)
        {
            PlaceOnRail(Nasties[a], nastiesToGenerate[a]);
        }
    }

    /// <summary>
    ///     Starts one nasty running along the rail it has been marked onto, at a speed drawn at
    ///     random. The faster it runs the sooner its legs come round, so the animation delay is read
    ///     off the speed rather than drawn separately.
    /// </summary>
    private void PlaceOnRail(ConveyorNasty nasty, Point rail)
    {
        nasty.NastyActive = true;
        nasty.XLocation = rail.X;
        nasty.YLocation = rail.Y;
        nasty.MoveSpeed = randomNumberGenerator.Next(SlowestMoveSpeed, FastestMoveSpeedExclusive);
        nasty.AnimationDelay = AnimationDelayFor(nasty.MoveSpeed);

        nasty.Initialise();
    }

    /// <summary>
    ///     How long each frame of the walk is held for at a given speed.
    /// </summary>
    /// <remarks>
    ///     Only one and two are ever drawn, so the third rung is unreachable - it was in the chain of
    ///     comparisons this replaced too, and is kept so the ladder still reads as a ladder.
    /// </remarks>
    private static int AnimationDelayFor(int moveSpeed)
        => moveSpeed switch
        {
            1 => 6,
            2 => 4,
            _ => 2
        };

    public void Update(GameTime gameTime)
    {
        foreach (ConveyorNasty nasty in Nasties)
        {
            nasty.Update(gameTime);

            if (nasty.NastyState != NastyState.Normal)
            {
                continue;
            }

            TouchBlob(nasty);
            TakeAnyFireHit(nasty);
        }
    }

    /// <summary>
    ///     Blob's box is pulled in by eight pixels each side and lifted onto the rail the nasties run
    ///     along, so that walking under one is not counted as being hit by it. Blob is held directly
    ///     rather than reached for, so the touch animation is a call; the event is what tells the
    ///     status panel.
    /// </summary>
    private void TouchBlob(ConveyorNasty nasty)
    {
        if (!CollisionDetection.Check(
                blob.BlobXPos + BlobBoxInset,
                blob.BlobYPos - SpriteMetrics.SpriteHeight / 2,
                blob.BlobWidth - BlobBoxInset * 2,
                SpriteMetrics.SpriteHeight,
                nasty.XLocation,
                nasty.YLocation,
                SpriteMetrics.SpriteWidth,
                SpriteMetrics.SpriteHeight))
        {
            return;
        }

        if (!nasty.NastyActive)
        {
            return;
        }

        if (OnNastyInteractionWithBlob != null)
        {
            OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, NastyType.Conveyor);
        }

        blob.BlobTouchedByNasty = true;
    }

    private void TakeAnyFireHit(ConveyorNasty nasty)
    {
        if (!blob.Weapon.HasFired)
        {
            return;
        }

        if (!CollisionDetection.Check(
                blob.Weapon.X,
                blob.Weapon.Y,
                FireWidth,
                FireHeight,
                nasty.XLocation,
                nasty.YLocation,
                SpriteMetrics.SpriteWidth,
                SpriteMetrics.SpriteHeight))
        {
            return;
        }

        if (!nasty.NastyActive)
        {
            return;
        }

        blob.Weapon.RegisterHit();
        nasty.NastyState = NastyState.Shot;

        resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobExplodes);

        if (OnNastyInteractionWithBlob != null)
        {
            OnNastyInteractionWithBlob(NastyInteractionType.BlobShot, NastyType.Conveyor);
        }
    }

    public void Draw(int screenFade)
    {
        foreach (ConveyorNasty nasty in Nasties)
        {
            if (nasty.NastyActive || nasty.NastyState == NastyState.Dead)
            {
                nasty.Draw(screenFade);
            }
        }
    }
}
