// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.World;

namespace Starquake.Animations;

internal class BouncingParticle
{

    /// <summary>
    ///     Reads as an axis at the call site: <c>StepAlongAxis(Vertical, p.Y)</c>.
    /// </summary>
    private const bool Vertical = true;

    /// <inheritdoc cref="Vertical" />
    private const bool Horizontal = false;

    /// <summary>
    ///     A part is one collision cell across, and a step is never longer than that. A longer one
    ///     could pass straight through a wall without ever being tested inside it.
    /// </summary>
    private const int ParticleSize = 32;

    /// <summary>Where the fireball trails from, measured in from the part's top left corner.</summary>
    private const int FireballXOffset = 20;

    /// <inheritdoc cref="FireballXOffset" />
    private const int FireballYOffset = 5;

    /// <summary>
    ///     How hard a part is thrown, in pixels a logic frame, and the gravity that brings it back
    ///     down.
    /// </summary>
    /// <remarks>
    ///     Parts are thrown along the points of a star, evenly around the circle, so Blob bursts
    ///     rather than spouts. Thrown hard, because a part going sideways has no height to fall from
    ///     and needs the speed to get anywhere.
    /// </remarks>
    private const int SlowestBurst = 19;

    /// <inheritdoc cref="SlowestBurst" />
    private const int FastestBurst = 29;

    /// <inheritdoc cref="SlowestBurst" />
    private const double Gravity = 1.0;

    private readonly GameScreenCollection _gameScreens;
    private readonly Bounce _particleBounce;
    private readonly Random _randomNumberGenerator;

    private int _oldXPos;
    private int _oldYPos;

    public BouncingParticle(ResourceManager resources, GameScreenCollection gameScreens, Random randomNumberGenerator, int spriteNo)
    {
        _gameScreens = gameScreens;
        _randomNumberGenerator = randomNumberGenerator;
        _particleBounce = new Bounce(resources, randomNumberGenerator, spriteNo);
    }

    public required IStandardParticleEffect Fireball { get; init; }

    public int XPos { get; private set; }

    public int YPos { get; private set; }

    public bool Triggered => _particleBounce.Triggered;

    /// <summary>
    ///     True while the emitter still holds particles, which outlasts the bounce itself, so the
    ///     last few released fade out under their own modifiers instead of vanishing with the
    ///     fragment.
    /// </summary>
    public bool HasActiveParticles => Fireball.Effect.ActiveParticlesCount > 0;

    public required IPositionalTriggerParticleEffect SparkEffect { get; init; }

    /// <summary>
    ///     Throws this part out of Blob along its own point of the star.
    /// </summary>
    /// <param name="point">Which point of the star this part goes along.</param>
    /// <param name="points">How many points there are, which is how many parts Blob comes apart into.</param>
    /// <param name="spin">
    ///     Turns the whole star, in radians. Drawn once per death and given to every part, so they
    ///     stay evenly spread but the burst is not in the same attitude every time.
    /// </param>
    public void Trigger(
        int xPos,
        int yPos,
        int point,
        int points,
        double spin,
        int effectMinDuration = 50,
        int effectMaxDuration = 100)
    {
        XPos = xPos + 16;
        YPos = yPos + 16;

        _oldYPos = YPos;
        _oldXPos = XPos;

        // Clear particles left over from the last death. They are still in the emitter's buffer at
        // the positions they froze at when the bounce stopped.
        Fireball.Reset();

        Fireball.Start();

        Point thrown = StarburstVelocity(
            point, points, _randomNumberGenerator.Next(SlowestBurst, FastestBurst), spin);

        _particleBounce.Trigger(
            Gravity, thrown.Y, thrown.X, effectMinDuration, effectMaxDuration);
    }

    /// <summary>
    ///     Which way one point of the star is thrown, and how fast.
    /// </summary>
    /// <remarks>
    ///     Y counts downward on the screen, so a part on the upper half of the circle comes back
    ///     negative and is thrown up. The parts below the middle are thrown into the floor on
    ///     purpose: they are travelling hardest when they meet it and come off it again, which is
    ///     half of what makes the burst read as one.
    /// </remarks>
    public static Point StarburstVelocity(int point, int points, int speed, double spin)
    {
        double around = spin + point * (2 * Math.PI / Math.Max(points, 1));

        return new Point(
            WithoutRoundingItAway(Math.Cos(around) * speed),
            (int)Math.Round(Math.Sin(around) * speed));
    }

    /// <summary>
    ///     A sideways throw in whole pixels a frame, never rounded to none at all.
    /// </summary>
    /// <remarks>
    ///     Nothing ever changes a part's sideways speed: no gravity acts along it and nothing damps
    ///     it. So a part thrown within a degree of the vertical, whose cosine would round away to
    ///     nothing, would go straight up, come straight back down the same line, and bounce on that
    ///     spot until its time ran out. Rounding up instead keeps the star even and still throws the
    ///     part as near to straight up as the arithmetic allows.
    /// </remarks>
    private static int WithoutRoundingItAway(double sideways)
    {
        int wholePixels = (int)Math.Round(sideways);

        if (wholePixels != 0)
        {
            return wholePixels;
        }

        return sideways < 0 ? -1 : 1;
    }

    public void Reset()
    {
        _particleBounce.Reset();
    }

    public void Update(GameTime gameTime)
    {
        if (!_particleBounce.Triggered)
        {
            // The bounce has finished, so stop releasing particles but keep updating the effect, so
            // the ones still in flight age, move and retire rather than freezing in the buffer.
            Fireball.Stop();
            Fireball.Update(XPos + FireballXOffset, YPos + FireballYOffset, gameTime);

            return;
        }

        Point p = _particleBounce.Update();

        // Vertically first, with the horizontal step taken from where that left it.
        YPos += StepAlongAxis(Vertical, p.Y);
        _oldYPos = YPos;

        XPos += StepAlongAxis(Horizontal, p.X);
        _oldXPos = XPos;

        Fireball.Update(XPos + FireballXOffset, YPos + FireballYOffset, gameTime);
    }

    /// <summary>
    ///     Moves the part one step along one axis, bouncing off anything it meets and off the screen
    ///     edges.
    /// </summary>
    /// <remarks>
    ///     The sign decides everything: which way to probe, whether the result is negated, and which
    ///     screen edge to stop at.
    ///     <para>
    ///         A step of nothing is not a wall. <c>CheckCollision</c> answers with the distance it was
    ///         handed and can only lower it, so asking for nought gets nought back. Reading that as
    ///         blocked would turn the part round with a clang and sparks on every frame it was not
    ///         moving along that axis.
    ///     </para>
    /// </remarks>
    /// <returns>How far the part actually got.</returns>
    private int StepAlongAxis(bool vertical, int requestedStep)
    {
        if (requestedStep == 0)
        {
            return 0;
        }

        bool movingPositive = requestedStep > 0;

        // Never more than its own size in one step, or it could pass clean through a wall.
        int step = movingPositive
            ? Math.Min(requestedStep, ParticleSize)
            : Math.Max(requestedStep, -ParticleSize);

        Direction direction = vertical
            ? movingPositive ? Direction.Down : Direction.Up
            : movingPositive
                ? Direction.Right
                : Direction.Left;

        // CheckCollision wants a distance rather than a signed step. The direction is already in the
        // argument above it.
        int speed = _gameScreens.Collisions.CheckCollision(
            direction,
            XPos,
            YPos,
            ParticleSize,
            ParticleSize,
            Math.Abs(step),
            true);

        if (speed == 0)
        {
            BounceBackTo(vertical, vertical ? _oldYPos : _oldXPos);
        }

        if (!movingPositive)
        {
            // CheckCollision answers a distance, so the sign the step came in with is put back.
            speed = -speed;
        }

        int position = vertical ? YPos : XPos;
        int screenSize = vertical
            ? _gameScreens.ScreenHeight
            : _gameScreens.ScreenWidth;

        // Bounced off the screen edges as well as the scenery, which keeps the fragments in view.
        if (movingPositive && position > screenSize - ParticleSize)
        {
            BounceBackTo(vertical, screenSize - ParticleSize);
        }
        else if (!movingPositive && position < ParticleSize)
        {
            BounceBackTo(vertical, ParticleSize);
        }

        return speed;
    }

    /// <summary>
    ///     Puts the part back where it can be, turns it around on that axis and throws a spark from
    ///     where it struck.
    /// </summary>
    private void BounceBackTo(bool vertical, int position)
    {
        if (vertical)
        {
            YPos = position;
            _particleBounce.YBounce();
        }
        else
        {
            XPos = position;
            _particleBounce.XBounce();
        }

        SparkEffect.Start(XPos, YPos);
    }

    public void Draw(SpriteCollection? spriteToDraw, int screenFade, int spriteNumber = -1)
    {
        if (Triggered && spriteToDraw != null)
        {
            _particleBounce.Draw(spriteToDraw, XPos + 4, YPos + 4, spriteNumber);
        }

        // Keep drawing after the bounce has finished, so the last particles released fade out under
        // their own modifiers instead of vanishing with the fragment.
        if (Triggered || HasActiveParticles)
        {
            Fireball.Draw(screenFade);
        }
    }
}
