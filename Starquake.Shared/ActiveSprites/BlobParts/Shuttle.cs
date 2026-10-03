// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Animations;
using Starquake.Constants;
using Starquake.SharedLibs;
using Starquake.Structure;

namespace Starquake.ActiveSprites.BlobParts;

/// <summary>
///     The shuttle the blob rides, and the pad he takes it from. Press up while standing squarely on
///     an intact pad and it is his; steer left or right while on it and he steps off, wherever he
///     happens to be, and the pad he took it from comes back.
/// </summary>
/// <remarks>
///     Riding the shuttle makes the blob taller, and the two facts were separate fields that had to
///     be written together - four places in <see cref="Blob" /> set a flag and a height in the same
///     breath, and getting one without the other would have put him half inside the floor. Here the
///     height is <em>derived</em> from the flag and there is nothing left to keep in step.
///     <para>
///         What stays behind is everything that is not the state machine: the world's own record of
///         which pads have been taken, the collision map that follows from it, and the drawing. This
///         holds no screen, no resources and no graphics device.
///     </para>
/// </remarks>
internal sealed class Shuttle
{
    /// <summary>
    ///     How much taller the pair is than the blob alone. The shuttle sits directly beneath him, so
    ///     this is what the collision code has to allow for while he is aboard.
    /// </summary>
    private const int ShuttleHeight = 32;

    /// <summary>
    ///     Where on the pad tile he has to be standing for it to count: squarely in the middle of it,
    ///     and with his feet exactly on top rather than anywhere further down. The X is derived
    ///     because it is the centring that matters, not the number it works out to.
    /// </summary>
    private const int PadXPosition = (TileMetrics.TileWidth - SpriteMetrics.SpriteWidth) / 2;

    /// <inheritdoc cref="PadXPosition" />
    private const int PadYPosition = 0;

    /// <summary>
    ///     The shuttle's own animation: four frames, advanced one per two logic frames. It runs only
    ///     while he is being steered, which is why <see cref="Advance" /> is called from the walk and
    ///     not from the update.
    /// </summary>
    private const int LastFrame = 3;

    /// <inheritdoc cref="LastFrame" />
    private const int AnimationDelay = 2;

    private readonly Animation _animation = Animation.Looping(0, LastFrame, AnimationDelay, true);

    /// <summary>Whether the blob is riding the shuttle.</summary>
    public bool Riding { get; private set; }

    /// <summary>
    ///     Whether he is standing squarely on a pad. Read from last frame's tile and acted on at the
    ///     start of the next - see <c>Blob.UpdateNormalState</c> for why the reading and the flag have
    ///     to be a matched pair.
    /// </summary>
    public bool OnThePad { get; private set; }

    /// <summary>
    ///     How tall the blob is, shuttle included. Derived rather than stored, so it cannot drift from
    ///     <see cref="Riding" />.
    /// </summary>
    public int BlobHeight => Riding ? SpriteMetrics.SpriteHeight + ShuttleHeight : SpriteMetrics.SpriteHeight;

    /// <summary>The frame of the shuttle's animation to draw.</summary>
    public int Frame => _animation.CurrentFrame;

    /// <summary>
    ///     Notes whether the tile under him this frame is a pad he is squarely on. Both states of the
    ///     pad count: an intact one is what he boards from, and the gap it leaves is what he has to be
    ///     standing on to step off again.
    /// </summary>
    public void NoticeThePadUnderfoot(TileDetected? below)
        => OnThePad = below is { XPosition: PadXPosition, YPosition: PadYPosition } &&
                      (ActionTiles)below.TileNumber is ActionTiles.ShuttlePlatform or ActionTiles.RemovedShuttlePlatform;

    /// <summary>
    ///     One frame of standing on a pad, and what it came to. Changing the world's record of which
    ///     pads have been taken is the caller's job, because only the caller has the screens.
    /// </summary>
    /// <param name="below">The tile under him, from the same reading <see cref="NoticeThePadUnderfoot" /> was given.</param>
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

    /// <summary>One frame of the shuttle's animation, for a blob being steered on it.</summary>
    public void Advance() => _animation.GetNextFrame();

    /// <summary>
    ///     Puts him back on or off the shuttle, for a blob returned to where he arrived on the screen
    ///     after a death by spikes.
    /// </summary>
    public void PutBack(bool riding) => Riding = riding;

    /// <summary>Off the shuttle, for a blob starting a life.</summary>
    public void Reset() => Riding = false;
}
