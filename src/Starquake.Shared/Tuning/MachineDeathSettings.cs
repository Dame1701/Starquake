// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Tuning;

/// <summary>
///     How long a machine's wreck lies in its own explosion, and how it goes. The spiky nasty and the
///     conveyor one share this, because they explode the same way and should look as though they do.
/// </summary>
/// <remarks>
///     The sibling of <see cref="SplatSettings" />: a creature is eaten away from its edges and a
///     machine burns out whole. The two want quite different lengths, since a splat is over in an
///     eyeblink where a wreck wants to be seen sitting in the fire, which is why they are two sets of
///     numbers. Letting each nasty hold its own lets them drift apart.
/// </remarks>
internal static class MachineDeathSettings
{
    /// <summary>
    ///     How much of the wreck's fade goes each logic frame, against a full level of 255. Thirteen
    ///     frames, about a quarter of a second, which is long enough for the body to read as
    ///     something the explosion happens <em>to</em> rather than something it replaces.
    /// </summary>
    /// <remarks>
    ///     This ends the wreck, not the shrink: both nasties draw the body only while something of
    ///     the fade is left.
    /// </remarks>
    public const int FadeStep = 20;

    /// <summary>
    ///     How much of its size the wreck draws in each logic frame. Slow against the fade on
    ///     purpose: over the thirteen frames it is on screen it pulls in about an eighth, which reads
    ///     as a body collapsing rather than one being sucked away.
    /// </summary>
    public const float ShrinkStep = 0.01f;
}
