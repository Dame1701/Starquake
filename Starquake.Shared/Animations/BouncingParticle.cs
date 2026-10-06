// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

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
    ///     The part is one collision cell across, and a step is never allowed to be longer than that -
    ///     a longer one could pass straight through a wall without ever being tested inside it.
    /// </summary>
    private const int ParticleSize = 32;

    /// <summary>
    ///     Where the fireball trails from, measured in from the part's top left corner.
    /// </summary>
    private const int FireballXOffset = 20;

    /// <inheritdoc cref="FireballXOffset" />
    private const int FireballYOffset = 5;

    /// <summary>
    ///     How hard a part is thrown sideways, in pixels a logic frame. Which way is a coin flip.
    /// </summary>
    /// <summary>
    ///     How hard a part is thrown, and the gravity that brings it back down.
    /// </summary>
    /// <remarks>
    ///     Every part used to be thrown <em>upward</em> - ten to twenty-two a frame - with four to
    ///     twelve of sideways either way, which is a fountain: they all went the same way and
    ///     differed only in how far they leaned. They are thrown out along the points of a star
    ///     now, evenly around the circle, so he bursts rather than spouts. Harder, too, because a
    ///     part going sideways has no height to fall from and needs the speed to get anywhere.
    ///     <para>
    ///         The throw was <em>downward</em> before even that, at one to nineteen, so the parts
    ///         dribbled off the bottom of him.
    ///     </para>
    /// </remarks>
    private const int SlowestBurst = 19;

    /// <inheritdoc cref="SlowestBurst" />
    private const int FastestBurst = 29;

    /// <inheritdoc cref="SlowestBurst" />
    private const double Gravity = 1.0;

    private readonly GameScreenCollection _gameScreens;
    private readonly BouncingAnimation _particleBounce;
    private readonly Random _randomNumberGenerator;

    private int _oldXPos;
    private int _oldYPos;

    public BouncingParticle(ResourceManager resources, GameScreenCollection gameScreens, Random randomNumberGenerator, int spriteNo)
    {
        _gameScreens = gameScreens;
        _randomNumberGenerator = randomNumberGenerator;
        _particleBounce = new BouncingAnimation(resources, randomNumberGenerator, spriteNo);
    }

    public required IStandardParticleEffect Fireball { get; init; }

    public int XPos { get; private set; }

    public int YPos { get; private set; }

    public bool Triggered => _particleBounce.Triggered;

    /// <summary>
    ///     True while the emitter still holds particles, which outlives the bounce itself - so the
    ///     last few released can fade out under their own modifiers instead of vanishing with the
    ///     fragment.
    /// </summary>
    public bool HasActiveParticles => Fireball.Effect.ActiveParticlesCount > 0;

    public required IPositionalTriggerParticleEffect SparkEffect { get; init; }

    /// <summary>
    ///     Throws this part out of Blob along its own point of the star.
    /// </summary>
    /// <remarks>
    ///     <paramref name="point" /> is which point of the star it goes along and
    ///     <paramref name="points" /> is how many there are, which is how many parts he comes
    ///     apart into. <paramref name="spin" /> turns the whole star, in radians: it is drawn once
    ///     per death and given to every part, so they stay evenly spread but the burst is not in
    ///     the same attitude every time.
    /// </remarks>
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

        // Clear any particles left over from the last death, they are still sitting in the
        // emitter's buffer at the positions they froze at when the bounce stopped
        Fireball.Reset();

        Fireball.Start();

        Point thrown = StarburstVelocity(
            point, points, _randomNumberGenerator.Next(SlowestBurst, FastestBurst), spin);

        // The gravity was a Next(1, 2), which can only ever return the one.
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
    ///     A part thrown within a degree of the vertical has a cosine small enough to round away,
    ///     and the sideways speed is the one thing nothing afterwards ever changes - no gravity
    ///     acts along it and nothing damps it - so a part that starts with none has none for the
    ///     whole of its life. It goes straight up, comes straight back down the same line, and
    ///     bounces on that spot until its time runs out. That is about one crash landing in
    ///     twenty: the star is turned by a fresh angle every time, and six points spaced sixty
    ///     degrees apart put one of them within a degree of the vertical that often.
    ///     <para>
    ///         Rounding up rather than away is the smallest answer that keeps the star even. The
    ///         part is still thrown as near to straight up as the arithmetic allows - it simply
    ///         lands somewhere other than where it left.
    ///     </para>
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
            // The bounce has finished, so stop releasing particles, but keep updating the
            // effect so the ones still in flight age, move and retire rather than freezing
            // in the emitter's buffer
            Fireball.Stop();
            Fireball.Update(XPos + FireballXOffset, YPos + FireballYOffset, gameTime);

            return;
        }

        Point p = _particleBounce.Update();

        // Vertically first, and the horizontal step is then taken from where that left it
        YPos += StepAlongAxis(Vertical, p.Y);
        _oldYPos = YPos;

        XPos += StepAlongAxis(Horizontal, p.X);
        _oldXPos = XPos;

        Fireball.Update(XPos + FireballXOffset, YPos + FireballYOffset, gameTime);
    }

    /// <summary>
    ///     Moves the part one step along one axis, bouncing it off anything it meets and off the edges
    ///     of the screen, and returns how far it actually got.
    /// </summary>
    /// <remarks>
    ///     This was four blocks - one per axis per direction - which were the same twelve lines with the
    ///     axis and the sign swapped. The sign is what decides everything: which way to probe, whether
    ///     the result is negated, and which of the two screen edges is the one to stop at.
    ///     <para>
    ///         A step of nothing is not a wall. <c>CheckCollision</c> answers with the distance it was
    ///         handed and can only ever lower it, so asking it for nought gets nought back - which read
    ///         here as blocked and turned the part round on that axis, with a clang and a shower of
    ///         sparks, on every frame it was not moving along that axis. Vertically that was the top of
    ///         every arc, where the fall passes through nought on its way back down. Horizontally it
    ///         never ended: nothing changes a part's sideways speed once it is thrown, so a part thrown
    ///         straight up spent its whole life striking sparks off thin air.
    ///     </para>
    /// </remarks>
    private int StepAlongAxis(bool vertical, int requestedStep)
    {
        if (requestedStep == 0)
        {
            return 0;
        }

        bool movingPositive = requestedStep > 0;

        // Never more than its own size in one step, or it could pass clean through a wall between
        // one frame and the next.
        int step = movingPositive
            ? Math.Min(requestedStep, ParticleSize)
            : Math.Max(requestedStep, -ParticleSize);

        Direction direction = vertical
            ? movingPositive ? Direction.Down : Direction.Up
            : movingPositive
                ? Direction.Right
                : Direction.Left;

        // CheckCollision wants a distance rather than a signed step; the direction is already in the
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

        // Bounced off the screen edges as well as the scenery, which keeps the fragments on screen
        // where they can be seen.
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

        // Carry on drawing after the bounce has finished so the last particles released can
        // fade out under their own modifiers instead of vanishing with the fragment
        if (Triggered || HasActiveParticles)
        {
            Fireball.Draw(screenFade);
        }
    }
}
