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

internal class ConveyorNasties(ResourceManager resources,
    GameScreenCollection gameScreens,
    Blob blob,
    Random randomNumberGenerator)
{
    public event RaiseInteractionWithBlob? OnNastyInteractionWithBlob;

    public List<ConveyorNasty> Nasties { get; } = [];

    public int NastiesActive => gameScreens.GetCoordsOfTiles((int)MarkerTile.ConveyorNasty).Count;

    public void LoadContent()
    {
        // Create 3 nasties that we will keep re-using
        Nasties.Clear();
        for (int a = 0; a < 3; a++)
        {
            Nasties.Add(
                new ConveyorNasty(resources,
                    gameScreens,
                    randomNumberGenerator,
                    0,
                    0,
                    0,
                    false));
        }
    }

    public void PerformPostChangeScreenActions()
    {
        // Deactivate all nasties first
        foreach (ConveyorNasty nasty in Nasties)
        {
            nasty.PerformPostChangeScreenActions();
        }

        var nastiesToGenerate = gameScreens.GetCoordsOfTiles((int)MarkerTile.ConveyorNasty);
        if (nastiesToGenerate.Count > 0)
        {
            // We have some..... So reactivate a few or all...
            for (int a = 0; a < nastiesToGenerate.Count; a++)
            {
                Nasties[a].NastyActive = true;
                Nasties[a].XLocation = nastiesToGenerate[a].X;
                Nasties[a].YLocation = nastiesToGenerate[a].Y;
                Nasties[a].MoveSpeed = randomNumberGenerator.Next(1, 3);

                if (Nasties[a].MoveSpeed == 1)
                {
                    Nasties[a].AnimationDelay = 6;
                }
                else if (Nasties[a].MoveSpeed == 2)
                {
                    Nasties[a].AnimationDelay = 4;
                }
                else if (Nasties[a].MoveSpeed == 3)
                {
                    Nasties[a].AnimationDelay = 2;
                }

                Nasties[a].Initialise();
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var nasty in Nasties)
        {
            nasty.Update(gameTime);

            // Check for a collision between a nasty and Blob and raise an event
            // Since we have a reference to Blob directly, we can send the call to animate contact directly
            // We raise the event to affect the status panel
            // Going to move the offsets etc. in a bit so it's not too precise and unfair!
            if (nasty.NastyState == NastyState.Normal)
            {
                if (CollisionDetection.Check(
                        blob.BlobXPos + 8,
                        blob.BlobYPos - 32,
                        blob.BlobWidth - 16,
                        64,
                        nasty.XLocation,
                        nasty.YLocation,
                        64,
                        64))
                {
                    // Raise the event
                    if (nasty.NastyActive)
                    {
                        if (OnNastyInteractionWithBlob != null)
                        {
                            OnNastyInteractionWithBlob(NastyInteractionType.HitBlob, NastyType.Conveyor);
                        }

                        // Trigger Blob animation
                        blob.BlobTouchedByNasty = true;
                    }
                }

                // Has Blob shot a nasty
                if (blob.Weapon.HasFired)
                {
                    if (CollisionDetection.Check(
                            blob.Weapon.X,
                            blob.Weapon.Y,
                            32,
                            32,
                            nasty.XLocation,
                            nasty.YLocation,
                            64,
                            64))
                    {
                        if (nasty.NastyActive)
                        {
                            blob.Weapon.RegisterHit();
                            nasty.NastyState = NastyState.Shot;

                            resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobExplodes);

                            if (OnNastyInteractionWithBlob != null)
                            {
                                OnNastyInteractionWithBlob(NastyInteractionType.BlobShot, NastyType.Conveyor);
                            }
                        }
                    }
                }
            }
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
