// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Microsoft.Xna.Framework;

namespace Starquake.Animations;

/// <summary>
///     Blob's ship falling out of the sky onto the square the author marked. It comes in off the top
///     of the screen and to the left, crosses at a steady rate and drops at an accelerating one, so
///     the path bends over as it goes and the ship arrives nose down rather than on a straight line.
/// </summary>
/// <remarks>
///     The arithmetic of the intro, separate from its sound, particles and drawing. It needs the
///     crash site and nothing else, so it can be tested without a resource manager or a graphics
///     device.
/// </remarks>
internal class ShipDescent
{
    /// <summary>
    ///     Where the ship comes into view, off the top of the screen and left of where it lands.
    ///     Not on the map, unlike the crash site: it is only far enough out to be off the picture
    ///     when the fall starts.
    /// </summary>
    private const int EntryXPos = 200;

    /// <inheritdoc cref="EntryXPos" />
    private const int EntryYPos = -160;

    /// <summary>
    ///     How many frames the sideways travel is paced over. The drop is not paced: it starts at a
    ///     standstill and accelerates, so it takes longer and the ship is still moving across when it
    ///     arrives.
    /// </summary>
    private const int DescentFrames = 20;

    /// <summary>
    ///     How much more of the drop rate the ship picks up each frame, as a fraction of the paced
    ///     rate. The first frame does not move down at all and the eleventh moves a full step.
    /// </summary>
    private const float AccelerationStep = 0.1f;

    /// <summary>
    ///     How close to the ground the ship stops accelerating and falls at the full paced rate.
    ///     Without it the last frames of a long drop would overshoot the crash site badly.
    /// </summary>
    private const float FinalApproachHeight = 100;

    /// <summary>The drop rate once acceleration ends: one whole paced step per frame.</summary>
    private const float FullSpeed = 1f;

    private readonly float _acrossPerFrame;
    private readonly float _downPerFrame;

    private float _acceleration;

    /// <param name="rest">
    ///     Where the ship ends up, as the top left corner it is drawn from rather than the centre of
    ///     the crash site.
    /// </param>
    public ShipDescent(Point rest)
    {
        Rest = rest;
        Position = new Vector2(EntryXPos, EntryYPos);

        _acrossPerFrame = (rest.X - (float)EntryXPos) / DescentFrames;
        _downPerFrame = (rest.Y - (float)EntryYPos) / DescentFrames;
    }

    public Point Rest { get; }

    public Vector2 Position { get; private set; }

    /// <summary>
    ///     Whether the ship has reached the ground. The fall overshoots by up to a frame's travel
    ///     rather than landing exactly, so anything drawing the ship at rest must use
    ///     <see cref="Rest" /> and not this frame's <see cref="Position" />.
    /// </summary>
    public bool Landed => Position.Y >= Rest.Y;

    /// <summary>One frame of the fall.</summary>
    public void Step()
    {
        Position = new Vector2(
            Position.X + _acrossPerFrame,
            Position.Y + _downPerFrame * _acceleration);

        _acceleration = Position.Y + FinalApproachHeight > Rest.Y
            ? FullSpeed
            : _acceleration + AccelerationStep;
    }
}
