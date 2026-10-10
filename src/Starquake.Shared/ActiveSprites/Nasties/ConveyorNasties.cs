// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Collisions;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Structure;
using Starquake.World;

namespace Starquake.ActiveSprites;

/// <param name="onNastyExploded">
///     Called when one of these blows apart, and handed on to each of them. A nasty on a rail is a
///     machine, so all of them explode when shot.
/// </param>
internal class ConveyorNasties(ResourceManager resources,
    GameScreenCollection gameScreens,
    Blob blob,
    Random randomNumberGenerator,
    Action onNastyExploded)
{
    /// <summary>
    ///     How far Blob's box is pulled in each side before a touch counts, so passing close by a
    ///     nasty is not the same as walking into one.
    /// </summary>
    private const int BlobBoxInset = 8;

    /// <summary>The size of Blob's shot, for testing it against a nasty.</summary>
    private const int FireWidth = TileMetrics.CollisionCellWidth;

    /// <inheritdoc cref="FireWidth" />
    private const int FireHeight = TileMetrics.CollisionCellHeight;

    /// <summary>
    ///     How many conveyor nasties exist at once. Three is the most any screen asks for, and they
    ///     are reused from screen to screen rather than rebuilt.
    /// </summary>
    private const int ConveyorNastyCount = 3;

    /// <summary>
    ///     How fast a nasty may be set running, in pixels a frame. The upper bound is exclusive, the
    ///     way <see cref="Random.Next(int,int)" /> takes it, so a nasty runs at one or two.
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
    ///     Puts the nasties where the new screen's markers ask for them. All are switched off first,
    ///     so a screen asking for fewer than the last leaves the rest idle rather than still running.
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
    ///     Starts one nasty running along the rail it is marked onto, at a random speed. The
    ///     animation delay is derived from the speed, so faster legs come round sooner.
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

    /// <summary>How long each frame of the walk is held for at a given speed.</summary>
    /// <remarks>
    ///     Only speeds of one and two are ever drawn, so the third rung is unreachable. It stays so
    ///     the ladder still reads as a ladder.
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
    ///     Tests Blob against the nasties. Blob's box is pulled in each side and lifted onto the
    ///     rail, so walking under one does not count as being hit. The touch animation is a direct
    ///     call; the event is what tells the status panel.
    /// </summary>
    private void TouchBlob(ConveyorNasty nasty)
    {
        if (!CollisionDetection.Check(
                blob.XPos + BlobBoxInset,
                blob.YPos - SpriteMetrics.SpriteHeight / 2,
                blob.Width - BlobBoxInset * 2,
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

        blob.TouchedByNasty = true;
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
