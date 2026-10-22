// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.World;

namespace Starquake.GameComponents.GameScreens;

/// <summary>
///     Everything that cannot exist until the game's content has loaded: the planet, Blob, and the
///     seven things that chase him around it or come apart with him.
/// </summary>
/// <remarks>
///     This exists to say "not yet" once rather than nine times. <c>GameEngine</c> is built before any
///     content is, registering the manifest, starting the load and drawing the loading screen while it
///     runs, so for as long as that takes none of these nine exists.
///     <para>
///         Every property here is non-null, because the object is only built once all nine are. The
///         one place that can answer "not yet" is <c>GameEngine.Game</c>.
///     </para>
///     <para>
///         It lives here rather than beside the engine that builds it, because
///         <see cref="MainGameScreen" /> is handed one instead of the engine and
///         <see cref="IntroSequence" /> is one of the nine. Any namespace below this one holding the
///         record would point back up at it; here the reference is to a neighbour, and
///         <c>Engines</c> depends on this namespace one way.
///     </para>
/// </remarks>
internal sealed class RunningGame
{
    /// <summary>The planet: its screens, their collision maps and everything drawn on them.</summary>
    public required GameScreenCollection GameScreens { get; init; }

    /// <summary>Blob.</summary>
    public required Blob Player { get; init; }

    /// <summary>The bridges that give way under Blob.</summary>
    public required BreakableBridges BreakableBridges { get; init; }

    /// <summary>The lifts that carry Blob up a shaft.</summary>
    public required TunnelLiftArrows TunnelLiftArrows { get; init; }

    /// <summary>Blob's ship coming down over the planet, which is how a game opens.</summary>
    public required IntroSequence IntroSequence { get; init; }

    /// <summary>The nasties that wander each screen.</summary>
    public required Nasties Nasties { get; init; }

    /// <summary>How Blob comes apart when one of them gets him.</summary>
    public required BlobDeath BlobKiller { get; init; }

    /// <summary>The nasties that run along conveyor rails.</summary>
    public required ConveyorNasties ConveyorNasties { get; init; }

    /// <summary>The electricity gates and the nodes that feed them.</summary>
    public required Electricity Electricity { get; init; }
}
