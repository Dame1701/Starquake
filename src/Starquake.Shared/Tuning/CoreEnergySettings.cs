// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.Structure;

namespace Starquake.Tuning;

/// <summary>
///     The planet's core as a living thing: a funnel of energy turning about it, and beams of
///     electricity crawling across the elements set into it. Driven by
///     <c>Shaders/CoreFunnel.fx</c> and the node arcs' own shader; see <c>PlanetCoreScreen</c>.
/// </summary>
/// <remarks>
///     Drawn underneath the room's two particle effects and between the elements, not in place of
///     them.
/// </remarks>
internal static class CoreEnergySettings
{
    /// <summary>How far across the room the funnel is drawn, in pixels.</summary>
    public const int FunnelSize = 576;

    /// <summary>How many arms the spiral has.</summary>
    public const float Arms = 5f;

    /// <summary>How hard the arms wind inward. Negative turns the funnel the other way.</summary>
    public const float Twist = 0.9f;

    /// <summary>How fast it turns, in radians per logic frame.</summary>
    private const double TurnSpeed = 0.11;

    /// <summary>
    ///     How much faster than the funnel the particles behind it travel. The same way round and a
    ///     little quicker, so the two read as one body of energy turning rather than two things that
    ///     happen to be spinning.
    /// </summary>
    private const double ParticleTurnFaster = 1.35;

    /// <summary>
    ///     How far out from the middle the particles are thrown from, in pixels. Orbiting the point
    ///     they leave from is what makes them spiral: emitted from the exact centre they give an even
    ///     spray however fast anything else turns. <c>ProjectMercury</c> has no vortex of its own and
    ///     is out of bounds to add one to, so the two sums below do it instead.
    /// </summary>
    private const double EmitterOrbit = 44;

    /// <summary>
    ///     Where the funnel fades out, as a fraction of the rectangle it is drawn over. Short of a
    ///     half, so it is gone before the corners, which would otherwise show as a hard square edge
    ///     against the room.
    /// </summary>
    public const float OuterEdge = 0.46f;

    /// <summary>
    ///     Where the throat begins. The elements sit inside this, so the arms sweep around them
    ///     rather than across.
    /// </summary>
    public const float ThroatEdge = 0.17f;

    /// <summary>
    ///     How brightly the funnel burns. Drawn additively, so this is how much light it adds to the
    ///     room rather than how opaque it is. Keeping it low is what makes the funnel something the
    ///     core is seen through rather than something drawn over the core.
    /// </summary>
    /// <remarks>
    ///     Narrow usable range: much brighter draws over the core instead of around it, much dimmer
    ///     cannot be seen at all.
    /// </remarks>
    public const float Brightness = 0.5f;

    /// <summary>
    ///     How much of <see cref="Brightness" /> the throat gets. Held down separately because the
    ///     throat is one bright patch where the arms are thin lines spread over the room, so the same
    ///     number on both leaves the middle shouting and the arms whispering.
    /// </summary>
    public const float ThroatShare = 0.55f;

    /// <summary>The colour down the throat of it.</summary>
    public static Vector3 ThroatColour => new(1f, 0.72f, 0.3f);

    /// <summary>The colour of the arms further out.</summary>
    public static Vector3 ArmColour => new(0.3f, 0.55f, 1f);

    /// <summary>How many beams crawl across the elements at once.</summary>
    public const int BeamCount = 3;

    /// <summary>How long one beam holds between the same two elements, in logic frames.</summary>
    public const int BeamHoldFrames = 9;

    /// <summary>How big the arc artwork is drawn as a beam between two elements.</summary>
    public const float BeamScale = 0.85f;

    /// <summary>How many elements there are to arc between.</summary>
    public static int ElementCount => CoreSite.SpritesAcross * CoreSite.SpritesDown;

    /// <summary>How far the funnel has turned by a given logic frame.</summary>
    public static float TurnedBy(int logicFrame) => (float)(logicFrame * TurnSpeed);

    /// <summary>
    ///     Where the core's particles are thrown from: a point going round the middle rather than the
    ///     middle itself.
    /// </summary>
    /// <remarks>
    ///     Driven by how far the funnel has actually turned rather than by the frame, so the two stay
    ///     in step when it winds up for a replacement. Deriving it from the frame only holds while
    ///     the rate never changes.
    ///     <para>
    ///         The minus sign is what makes it turn the same way as the funnel. The shader's arms
    ///         hold their shape where <c>angle * Arms + Turned</c> is constant, so the angle they sit
    ///         at falls as the turn climbs, and screen angles run clockwise because y counts
    ///         downward. If the two are ever seen turning against each other, that sign is the cause.
    ///     </para>
    /// </remarks>
    public static Point EmitterAt(float funnelTurned, int centreX, int centreY)
    {
        double turned = -funnelTurned * ParticleTurnFaster;

        return new Point(
            centreX + (int)(Math.Cos(turned) * EmitterOrbit),
            centreY + (int)(Math.Sin(turned) * EmitterOrbit));
    }

    /// <summary>
    ///     How much brighter the funnel burns while a slot is being filled, and how much faster it
    ///     turns. The core is a machine being repaired, so it bears down on the part going in rather
    ///     than stopping to wait for it.
    /// </summary>
    public const float FillingBrightness = 2.1f;

    /// <inheritdoc cref="FillingBrightness" />
    public const float FillingTurnFaster = 2.6f;

    /// <summary>How many frames the funnel takes to wind up to that and back down.</summary>
    public static readonly int WindFrames = LogicClock.Seconds(0.35);

    /// <summary>
    ///     How far the core has wound up towards a filling, nought at rest and one at full bearing.
    ///     Ramped rather than switched, so the room leans into a replacement and settles out of it
    ///     instead of snapping either way.
    /// </summary>
    public static float WoundUpBy(int framesWinding)
        => Math.Clamp(framesWinding / (float)WindFrames, 0f, 1f);

    /// <summary>
    ///     Which two elements a beam joins at a given moment. Deterministic, so the core looks the
    ///     same on every machine and can be checked without running the game, but spread enough that
    ///     the beams do not fall into a visible rhythm.
    /// </summary>
    /// <remarks>
    ///     The two are never the same element: a beam from a thing to itself has no length and draws
    ///     as a blot sitting on it.
    ///     <para>
    ///         <paramref name="slotBeingFilled" /> pulls every beam onto the element going in. A beam
    ///         that would otherwise have started there keeps its other end, so the three do not
    ///         collapse onto one line. That is also why the slot is always the <c>To</c> and never
    ///         the <c>From</c>: the energy arrives at it.
    ///     </para>
    /// </remarks>
    public static (int From, int To) BeamEndsAt(int logicFrame, int beam, int? slotBeingFilled)
    {
        (int from, int to) = BeamEndsAt(logicFrame, beam);

        if (slotBeingFilled is not { } slot)
        {
            return (from, to);
        }

        // A beam already coming from there is turned around rather than dropped.
        return from == slot ? (to, slot) : (from, slot);
    }

    /// <inheritdoc cref="BeamEndsAt(int,int,System.Nullable{int})" />
    public static (int From, int To) BeamEndsAt(int logicFrame, int beam)
    {
        int step = logicFrame / BeamHoldFrames;

        // Multiplied by numbers sharing no factor with the element count, so the pairs walk the grid
        // rather than settling into the same few.
        int from = Math.Abs(step * 7 + beam * 23) % ElementCount;
        int to = Math.Abs(step * 11 + beam * 5 + 1) % (ElementCount - 1);

        // Folded past the element it started from, which keeps every other element equally likely
        // rather than making the next one twice as likely.
        if (to >= from)
        {
            to++;
        }

        return (from, to);
    }

    /// <summary>Where one element sits, as the middle of its square.</summary>
    public static Point ElementCentre(Point coreTopLeft, int element)
        => new(
            coreTopLeft.X + element % CoreSite.SpritesAcross * SpriteMetrics.SpriteWidth
            + SpriteMetrics.HalfSpriteWidth,
            coreTopLeft.Y + element / CoreSite.SpritesAcross * SpriteMetrics.SpriteHeight
            + SpriteMetrics.HalfSpriteHeight);
}
