// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;
using Starquake.Core;
using Starquake.Structure;

namespace Starquake.ActiveSprites;

/// <summary>
///     The shuttle Blob rides, and the pad he takes it from. Press up while standing squarely on
///     an intact pad and it is his; steer left or right while on it and he steps off, wherever he
///     happens to be, and the pad he took it from comes back.
/// </summary>
/// <remarks>
///     Riding the shuttle makes Blob taller, so the height is <em>derived</em> from
///     <see cref="Riding" /> rather than stored alongside it. Setting one without the other would put
///     Blob half inside the floor.
///     <para>
///         The state machine only. The world's record of which pads have been taken, the collision
///         map following from it, and the drawing all stay outside, so this holds no screen, no
///         resources and no graphics device.
///     </para>
/// </remarks>
internal sealed class Shuttle
{
    /// <summary>
    ///     How much taller Blob and shuttle are together. The shuttle sits directly beneath, so this
    ///     is what the collision code allows for while aboard.
    /// </summary>
    private const int ShuttleHeight = 32;

    /// <summary>
    ///     Where on the pad tile Blob must stand for it to count: squarely in the middle, with feet
    ///     exactly on top rather than further down. The X is derived, because what matters is the
    ///     centring and not the number it works out to.
    /// </summary>
    private const int PadXPosition = (TileMetrics.TileWidth - SpriteMetrics.SpriteWidth) / 2;

    /// <inheritdoc cref="PadXPosition" />
    private const int PadYPosition = 0;

    /// <summary>
    ///     The shuttle's animation: four frames, one per two logic frames. It runs only while Blob is
    ///     being steered, which is why <see cref="Advance" /> is called from the walk rather than the
    ///     update.
    /// </summary>
    private const int LastFrame = 3;

    /// <inheritdoc cref="LastFrame" />
    private const int AnimationDelay = 2;

    private readonly Animation _animation = Animation.Looping(0, LastFrame, AnimationDelay, true);

    /// <summary>Whether Blob is riding the shuttle.</summary>
    public bool Riding { get; private set; }

    /// <summary>
    ///     Whether Blob is standing squarely on a pad. Read from last frame's tile and acted on at
    ///     the start of the next. See <c>Blob.UpdateNormalState</c> for why the reading and the flag
    ///     must be a matched pair.
    /// </summary>
    public bool OnThePad { get; private set; }

    /// <summary>
    ///     Blob's height, shuttle included. Derived rather than stored, so it cannot drift from
    ///     <see cref="Riding" />.
    /// </summary>
    public int BlobHeight => Riding ? SpriteMetrics.SpriteHeight + ShuttleHeight : SpriteMetrics.SpriteHeight;

    /// <summary>The frame of the shuttle's animation to draw.</summary>
    public int Frame => _animation.CurrentFrame;

    /// <summary>
    ///     Notes whether the tile underfoot this frame is a pad Blob is squarely on. Both states
    ///     count: an intact pad is what Blob boards from, and the gap it leaves is what Blob must
    ///     stand on to step off again.
    /// </summary>
    public void NoticeThePadUnderfoot(TileDetected? below)
        => OnThePad = below is { XPosition: PadXPosition, YPosition: PadYPosition } &&
                      (ActionTiles)below.TileNumber is ActionTiles.ShuttlePlatform or ActionTiles.RemovedShuttlePlatform;

    /// <summary>
    ///     One frame of standing on a pad, and what came of it. Updating the world's record of which
    ///     pads have been taken is the caller's job, because only the caller has the screens.
    /// </summary>
    /// <remarks>
    ///     <paramref name="below" /> is the tile underfoot, from the same reading
    ///     <see cref="NoticeThePadUnderfoot" /> was given.
    /// </remarks>
    public ShuttleBoarding Step(TileDetected? below, bool movedUp, bool movedLeft, bool movedRight)
    {
        if (!OnThePad || below == null)
        {
            return ShuttleBoarding.NotOnThePad;
        }

        switch ((ActionTiles)below.TileNumber)
        {
            case ActionTiles.ShuttlePlatform when movedUp:
            {
                Riding = true;

                return ShuttleBoarding.Boarded;
            }
            case ActionTiles.RemovedShuttlePlatform when movedLeft || movedRight:
            {
                Riding = false;

                return ShuttleBoarding.SteppedOff;
            }
            default:
            {
                return ShuttleBoarding.Standing;
            }
        }
    }

    /// <summary>One frame of the shuttle's animation, for Blob being steered on it.</summary>
    public void Advance() => _animation.GetNextFrame();

    /// <summary>
    ///     Puts Blob back on or off the shuttle, after a death by spikes returns him to where he
    ///     arrived on the screen.
    /// </summary>
    public void PutBack(bool riding) => Riding = riding;

    /// <summary>Off the shuttle, for Blob starting a life.</summary>
    public void Reset() => Riding = false;
}
