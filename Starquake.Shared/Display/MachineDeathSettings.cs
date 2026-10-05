// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Display;

/// <summary>
///     How long a machine's wreck lies in its own explosion, and how it goes. The two nasties
///     that are machines - the spiky one and the one on the conveyor rail - share this, because
///     they go off the same way and should look as though they do.
/// </summary>
/// <remarks>
///     The sibling of <see cref="SplatSettings" />, which is the other half of the same question:
///     a creature is eaten away from its edges and a machine simply burns out whole. The two
///     deaths want quite different lengths - a splat is over in an eyeblink and a wreck wants to
///     be seen sitting in the fire - which is why they are two sets of numbers rather than one.
///     <para>
///         They were two sets by accident before they were two on purpose. Each nasty held its
///         own fade and shrink, they drifted - the spiky one ended up four times the conveyor
///         one's speed - and a spiky nasty vanished the instant it was shot while a conveyor
///         nasty lay in its explosion for a quarter of a second.
///     </para>
/// </remarks>
internal static class MachineDeathSettings
{
    /// <summary>
    ///     How much of the wreck's fade goes each logic frame, against a full level of 255. That
    ///     is thirteen frames, about a quarter of a second - long enough that the body is
    ///     something the explosion happens *to* rather than something it replaces.
    /// </summary>
    /// <remarks>
    ///     This is what ends the wreck, not the shrink: both nasties draw the body only while
    ///     something of the fade is left.
    /// </remarks>
    public const int FadeStep = 20;

    /// <summary>
    ///     How much of its size the wreck draws in each logic frame. Slow against the fade on
    ///     purpose: over the thirteen frames it is on screen it pulls in by about an eighth,
    ///     which reads as a body collapsing rather than as one being sucked away.
    /// </summary>
    public const float ShrinkStep = 0.01f;
}
