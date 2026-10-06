// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.ActiveSprites;

/// <summary>
///     Where Blob arrived on the screen he is on, and how he looked arriving. A death by spikes puts
///     him back here rather than costing him the screen, so the spikes are survivable without being free.
/// </summary>
/// <remarks>
///     Arriving is two different things, which is why this is written to from two places. Walking or
///     falling onto a screen is recorded by <c>Blob.Update</c> at the end of the frame that changed
///     screens; being <em>placed</em> - out of a teleport booth, an access booth, or the planet core's
///     exit - is recorded by the position setters, because the screen doing the placing sets a position
///     and nothing else.
/// </remarks>
internal sealed class BlobEntryPoint
{
    public int XPos { get; set; }

    public int YPos { get; set; }

    /// <summary>
    ///     The walk-cycle frame he was on, so Blob put back does not also snap to a standing pose.
    /// </summary>
    public int Frame { get; set; }

    /// <summary>
    ///     Whether he was riding the shuttle, which decides how tall he is put back and whether the pad is
    ///     put back with him.
    /// </summary>
    public bool OnShuttle { get; set; }

    /// <summary>
    ///     Records a whole arrival, for when he has just walked or fallen onto a screen.
    /// </summary>
    public void Record(int xPos, int yPos, int frame, bool onShuttle)
    {
        XPos = xPos;
        YPos = yPos;
        Frame = frame;
        OnShuttle = onShuttle;
    }
}
