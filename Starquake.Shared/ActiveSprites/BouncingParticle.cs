// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System;
using Microsoft.Xna.Framework;
using Starquake.Animations;
using Starquake.ContentManagement;
using Starquake.Display;
using Starquake.Engines;
using Starquake.Particles;
using Starquake.Sprites;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

public class BouncingParticle
{

    private readonly GameScreenCollection _gameScreens;
    private readonly Random _randomNumberGenerator;
    private readonly BouncingAnimation _particleBounce;

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

    // True while the emitter still holds particles, which outlives the bounce itself
    public bool HasActiveParticles => Fireball.Effect.ActiveParticlesCount > 0;

    public required IPositionalTriggerParticleEffect SparkEffect { get; init; }

    public void XBounce()
    {
        _particleBounce.XBounce();
    }

    public void YBounce()
    {
        _particleBounce.YBounce();
    }

    public void Trigger(int xPos, int yPos, int effectMinDuration = 50, int effectMaxDuration = 100)
    {
        XPos = xPos + 16;
        YPos = yPos + 16;

        _oldYPos = YPos;
        _oldXPos = XPos;

        // Clear any particles left over from the last death, they are still sitting in the
        // emitter's buffer at the positions they froze at when the bounce stopped
        Fireball.Reset();

        var xSpeed = _randomNumberGenerator.Next(4, 12);

        var xDirection = _randomNumberGenerator.Next(0, 2);
        if (xDirection == 0)
        {
            xSpeed = -xSpeed;
        }

        Fireball.Start();

        _particleBounce.Trigger(
            _randomNumberGenerator.Next(1, 2),
            _randomNumberGenerator.Next(1, 20),
            xSpeed, effectMinDuration, effectMaxDuration);
    }

    public void Reset()
    {
        _particleBounce.Reset();
    }

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

        var p = _particleBounce.Update();

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
    /// </remarks>
    private int StepAlongAxis(bool vertical, int requestedStep)
    {
        var movingPositive = requestedStep > 0;

        // Ensure we don't move more than the size of the collision tilemap
        var step = movingPositive
            ? Math.Min(requestedStep, ParticleSize)
            : Math.Max(requestedStep, -ParticleSize);

        var direction = vertical
            ? movingPositive ? Direction.Down : Direction.Up
            : movingPositive ? Direction.Right : Direction.Left;

        // Absolute value used here to convert the negative to positive for col routine
        var speed = _gameScreens.CheckCollision(
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
            // Convert back to negative number
            speed = -speed;
        }

        var position = vertical ? YPos : XPos;
        var screenSize = vertical
            ? _gameScreens.ScreenHeight
            : _gameScreens.ScreenWidth;

        // Keep the parts within the screen bounds, makes for a better effect
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