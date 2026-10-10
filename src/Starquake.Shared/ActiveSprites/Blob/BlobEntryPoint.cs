// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     Where Blob arrived on the current screen, and the pose arrived in. A death by spikes returns
///     Blob here rather than costing the whole screen.
/// </summary>
/// <remarks>
///     Written from two places, because arriving happens two ways. Walking or falling onto a screen is
///     recorded by <c>Blob.Update</c> at the end of the frame that changed screens. Being <em>placed</em>
///     by a teleport booth, an access booth or the core exit is recorded by the position setters,
///     because the screen doing the placing sets a position and nothing else.
/// </remarks>
internal sealed class BlobEntryPoint
{
    public int XPos { get; set; }

    public int YPos { get; set; }

    /// <summary>
    ///     The walk-cycle frame on arrival, so Blob put back does not snap to a standing pose.
    /// </summary>
    public int Frame { get; set; }

    /// <summary>
    ///     Whether the shuttle was being ridden, which decides the height Blob is put back at and
    ///     whether the pad goes back too.
    /// </summary>
    public bool OnShuttle { get; set; }

    /// <summary>Records a whole arrival, after walking or falling onto a screen.</summary>
    public void Record(int xPos, int yPos, int frame, bool onShuttle)
    {
        XPos = xPos;
        YPos = yPos;
        Frame = frame;
        OnShuttle = onShuttle;
    }
}
