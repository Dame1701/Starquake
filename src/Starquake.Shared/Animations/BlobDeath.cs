// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Display;
using Starquake.Particles;
using Starquake.Tuning;
using Starquake.World;

namespace Starquake.Animations;

internal class BlobDeath
{
    /// <summary>
    ///     The six fragment sprites Blob comes apart into. Deliberately not a straight run: the
    ///     repeated 1 and the gap before 5 match the artwork and must not be tidied into 0 to 5.
    /// </summary>
    private static readonly int[] FragmentSpriteNumbers = [0, 1, 1, 2, 3, 5];

    private readonly Animation _blobDeathParticlesOriginal;
    private readonly List<BouncingParticle> _bouncingParticles;
    private readonly GameScreenCollection _gameScreens;
    private readonly GraphicsSet _graphicsSet;
    private readonly Random _randomNumberGenerator;
    private readonly ResourceManager _resources;

    /// <summary>
    ///     The ring thrown out as Blob comes apart. One for the whole death rather than one per
    ///     fragment, because six rings would read as a smear rather than a blast.
    /// </summary>
    private readonly Shockwave _shockwave;

    private IPositionalTriggerParticleEffect? _blobFragmentBounceSparks;

    private bool _drawBlobParts;

    public BlobDeath(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        GraphicsSet graphicsSet,
        Random randomNumberGenerator)
    {
        _resources = resources;
        _gameScreens = gameScreens;
        _graphicsSet = graphicsSet;
        _randomNumberGenerator = randomNumberGenerator;

        _bouncingParticles = new List<BouncingParticle>(FragmentSpriteNumbers.Length);

        _shockwave = new Shockwave(
            resources, randomNumberGenerator, ShockwaveSettings.BlobDiameter, ShockwaveSettings.BlobFrames);

        _blobDeathParticlesOriginal = new Animation
        {
            FirstFrame = 0,
            CurrentFrame = 0,
            LastFrame = 3,
            Yoyo = false,
            Delay = 2,
            Repeat = true,
            AnimateForwards = true
        };
    }

    public bool Triggered
    {
        get
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // True while anything still needs updating or drawing. The fragments' particles outlast the
    // bounce itself, so this stays set until the tail has faded.
    public bool Active
    {
        get
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered || bouncingParticle.HasActiveParticles)
                {
                    return true;
                }
            }

            // The shockwave is deliberately not counted. This holds Blob dead until the death has
            // finished playing, and the ring is shorter than the fragments, so counting it could
            // only ever hold him dead longer than the death lasts.
            return _blobFragmentBounceSparks is { EffectActive: true };
        }
    }

    public void LoadContent()
    {
        _blobFragmentBounceSparks = (IPositionalTriggerParticleEffect)_resources
            .GetParticleEffectInstance(Assets.Effects.FireSparks, ParticleEffectType.TriggeredMultiLocationTimed, null);

        var surroundings = new FragmentSurroundings(_resources, _gameScreens, _blobFragmentBounceSparks);

        // Each fragment needs its own explosion emitter, but all of them share one effect.
        _bouncingParticles.Clear();
        foreach (int fragmentSpriteNumber in FragmentSpriteNumbers)
        {
            _bouncingParticles.Add(new BouncingParticle(surroundings, _randomNumberGenerator, fragmentSpriteNumber)
            {
                Fireball = (IStandardParticleEffect)_resources
                    .GetParticleEffectInstance(Assets.Effects.BlobFragmentExplosion, ParticleEffectType.Continual, null)
            });
        }
    }

    /// <remarks>
    ///     Both of Blob's deaths come through here, the slow overload and the instant one a spike or
    ///     gate gives, because an instant death is an overload that begins already exploded. The
    ///     shockwave is started once here and belongs to both.
    /// </remarks>
    public void Trigger(int effectXPosition, int effectYPosition, bool drawBlobParts, int effectMinDuration = 50,
        int effectMaxDuration = 100)
    {
        _drawBlobParts = drawBlobParts;

        _shockwave.Trigger(effectXPosition, effectYPosition);

        // One turn for the whole star, so the parts stay evenly spread but the burst is not in the
        // same attitude every time Blob dies.
        double spin = _randomNumberGenerator.NextDouble() * 2 * Math.PI;

        for (int point = 0; point < _bouncingParticles.Count; point++)
        {
            _bouncingParticles[point].Trigger(
                effectXPosition, effectYPosition, point, _bouncingParticles.Count, spin,
                effectMinDuration, effectMaxDuration);
        }
    }

    /// <summary>
    ///     Stops everything dead: the fragments, the particles still in the air behind them, and the
    ///     shockwave.
    /// </summary>
    /// <remarks>
    ///     It must clear the emitters too, not just the bounces, or <see cref="Active" /> stays true
    ///     on the particles' account and the clangs carry on. The intro's crash is the one caller,
    ///     and it needs this to mean stop completely.
    /// </remarks>
    public void Reset()
    {
        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            bouncingParticle.Reset();
            bouncingParticle.Fireball.Stop();
            bouncingParticle.Fireball.Reset();
        }

        _blobFragmentBounceSparks?.Stop();
        _shockwave.Stop();
    }

    public void Update(GameTime gameTime)
    {
        foreach (BouncingParticle bouncingParticle in _bouncingParticles)
        {
            if (bouncingParticle.Triggered || bouncingParticle.HasActiveParticles)
            {
                bouncingParticle.Update(gameTime);
            }
        }

        if (_graphicsSet.IsOriginal)
        {
            _blobDeathParticlesOriginal.GetNextFrame();
        }

        if (_blobFragmentBounceSparks is { EffectActive: true })
        {
            _blobFragmentBounceSparks.Update(gameTime);
        }

        _shockwave.Update();
    }

    public void Draw(int screenFade)
    {
        // Under the fragments, so they come out of it rather than through it. Drawn for both sets of
        // artwork, because the ring is the blast rather than part of the redrawn look.
        _shockwave.Draw(screenFade);

        if (_graphicsSet.IsOriginal)
        {
            _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original)
                .Begin(BlendState.AlphaBlend);

            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle.Triggered)
                {
                    bouncingParticle.Draw(
                        _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original),
                        screenFade,
                        _blobDeathParticlesOriginal.CurrentFrame);
                }
            }

            _resources.GetSpriteCollection(Assets.Graphics.BlobFire2Original).End();
        }
        else
        {
            foreach (BouncingParticle bouncingParticle in _bouncingParticles)
            {
                if (bouncingParticle is { Triggered: false, HasActiveParticles: false })
                {
                    continue;
                }

                if (_drawBlobParts)
                {
                    _resources.GetSpriteCollection(Assets.Graphics.BlobParts)
                        .Begin(BlendState.AlphaBlend);

                    bouncingParticle.Draw(_resources.GetSpriteCollection(Assets.Graphics.BlobParts), screenFade);

                    _resources.GetSpriteCollection(Assets.Graphics.BlobParts).End();
                }
                else
                {
                    bouncingParticle.Draw(null, screenFade);
                }
            }

            if (_blobFragmentBounceSparks is { EffectActive: true })
            {
                _blobFragmentBounceSparks.Draw(screenFade);
            }
        }
    }

    /// <summary>
    ///     Joins the parts to the screen they come apart over. Answering everything a part asks here
    ///     is what keeps it knowing nothing but its own position, so its stepping and bouncing can be
    ///     exercised without a screen or a graphics device.
    /// </summary>
    private sealed class FragmentSurroundings(
        ResourceManager resources,
        GameScreenCollection gameScreens,
        IPositionalTriggerParticleEffect sparks)
        : IFragmentSurroundings
    {
        public int ScreenWidth => gameScreens.ScreenWidth;

        public int ScreenHeight => gameScreens.ScreenHeight;

        /// <inheritdoc />
        /// <remarks>
        ///     Asked as Blob, so the map does not hold the part inside the screen. The part keeps
        ///     itself inside, and it bounces off the edges rather than being stopped dead at them.
        /// </remarks>
        public int DistanceClear(Direction direction, int x, int y, int width, int height, int distance)
            => gameScreens.Collisions.CheckCollision(direction, x, y, width, height, distance, true);

        /// <inheritdoc />
        public void ThrowSparks(int x, int y) => sparks.Start(x, y);

        /// <inheritdoc />
        public void Clang() => resources.GetSoundFxEngine().PlaySound(Assets.Sounds.BlobPartClang, false);
    }
}
