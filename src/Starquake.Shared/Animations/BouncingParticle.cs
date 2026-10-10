// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Core;
using Starquake.Particles;
using Starquake.Sprites;

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

    private readonly Bounce _particleBounce;
    private readonly Random _randomNumberGenerator;
    private readonly IFragmentSurroundings _surroundings;

    public BouncingParticle(IFragmentSurroundings surroundings, Random randomNumberGenerator, int spriteNo)
    {
        _surroundings = surroundings;
        _randomNumberGenerator = randomNumberGenerator;
        _particleBounce = new Bounce(randomNumberGenerator, spriteNo);
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
        // Started within the edges whatever Blob's own position was. Everything below holds a part
        // between them by refusing it the step that would take it out, so a part born outside them
        // would have nowhere legal to go and would sit there turning around.
        XPos = Math.Clamp(xPos + 16, 0, LastPositionOnScreen(Horizontal));
        YPos = Math.Clamp(yPos + 16, 0, LastPositionOnScreen(Vertical));

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
        StepAlongAxis(Vertical, p.Y);
        StepAlongAxis(Horizontal, p.X);

        Fireball.Update(XPos + FireballXOffset, YPos + FireballYOffset, gameTime);
    }

    /// <summary>
    ///     Moves the part one step along one axis, bouncing off anything it meets and off the screen
    ///     edges.
    /// </summary>
    /// <remarks>
    ///     The sign decides everything: which way to probe, which edge is the one ahead, and which
    ///     way the position moves.
    ///     <para>
    ///         Both limits are worked out as a distance the part is allowed to travel, and the part is
    ///         only ever moved by a distance something has agreed to. Nothing writes a position that
    ///         has not been asked about, because the edges are not only edges: the cells along them
    ///         are usually solid floor and wall, and a part overlapping a solid cell is blocked in
    ///         every direction at once, so one put down inside one would stay there, clanging and
    ///         throwing sparks, until its time ran out.
    ///     </para>
    ///     <para>
    ///         A step of nothing is not a wall. <c>DistanceClear</c> answers with the distance it was
    ///         handed and can only lower it, so asking for nought gets nought back. Reading that as
    ///         blocked would turn the part round with a clang and sparks on every frame it was not
    ///         moving along that axis.
    ///     </para>
    /// </remarks>
    private void StepAlongAxis(bool vertical, int requestedStep)
    {
        if (requestedStep == 0)
        {
            return;
        }

        bool movingPositive = requestedStep > 0;

        // Never more than its own size in one step, or it could pass clean through a wall.
        int distance = Math.Min(Math.Abs(requestedStep), ParticleSize);

        int position = vertical ? YPos : XPos;

        // Measured before the step is taken rather than after, so the edge shortens the step instead
        // of catching the part beyond it.
        int toTheEdge = movingPositive
            ? LastPositionOnScreen(vertical) - position
            : position;

        int allowed = Math.Min(distance, toTheEdge);

        if (allowed > 0)
        {
            Direction direction = vertical
                ? movingPositive ? Direction.Down : Direction.Up
                : movingPositive
                    ? Direction.Right
                    : Direction.Left;

            // The scenery is asked for a distance rather than a signed step. The direction is
            // already in the argument above it.
            allowed = _surroundings.DistanceClear(
                direction, XPos, YPos, ParticleSize, ParticleSize, allowed);
        }

        // Nowhere to go on this axis, so the part turns around where it stands. Once, whether it was
        // the scenery or the edge that stopped it: turning around twice in a frame would leave the
        // part still travelling into whatever had just stopped it.
        if (allowed <= 0)
        {
            BounceOffWhatItMet(vertical);

            return;
        }

        MoveAlong(vertical, movingPositive ? position + allowed : position - allowed);
    }

    /// <summary>The furthest along an axis a part can sit and still be wholly on screen.</summary>
    private int LastPositionOnScreen(bool vertical)
        => (vertical ? _surroundings.ScreenHeight : _surroundings.ScreenWidth) - ParticleSize;

    private void MoveAlong(bool vertical, int position)
    {
        if (vertical)
        {
            YPos = position;
        }
        else
        {
            XPos = position;
        }
    }

    /// <summary>
    ///     Turns the part around on one axis and throws a spark from where it struck.
    /// </summary>
    private void BounceOffWhatItMet(bool vertical)
    {
        if (vertical)
        {
            _particleBounce.YBounce();
        }
        else
        {
            _particleBounce.XBounce();
        }

        _surroundings.Clang();
        _surroundings.ThrowSparks(XPos, YPos);
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
