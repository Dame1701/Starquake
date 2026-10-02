// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.Animations;
using Starquake.Display;

namespace Starquake.Engines;

/// <summary>
///     Everything that cannot exist until the game's content has loaded: the planet, the blob, and the
///     seven things that chase him around it or come apart with him.
/// </summary>
/// <remarks>
///     This exists to say "not yet" once rather than nine times. <see cref="GameEngine" /> is built before
///     any content is - it registers the manifest, starts the load, and draws the loading screen while it
///     runs - so for as long as that takes, none of these nine exists. That used to be nine nullable
///     fields behind nine identical guards, which read as nine separate doubts rather than the one real
///     one: whether the game has started yet.
///     <para>
///         Every property here is non-null, because the object is only ever built once all nine have
///         been. The one place that can still answer "not yet" is <c>GameEngine.Game</c>.
///     </para>
/// </remarks>
internal sealed class RunningGame
{
    /// <summary>The planet: its screens, their collision maps and everything drawn on them.</summary>
    public required GameScreenCollection GameScreens { get; init; }

    /// <summary>The blob.</summary>
    public required Blob Player { get; init; }

    /// <summary>The bridges that give way under him.</summary>
    public required BreakableBridges BreakableBridges { get; init; }

    /// <summary>The lifts that carry him up a shaft.</summary>
    public required TunnelLiftAnimationEngine TunnelAnimations { get; init; }

    /// <summary>His ship coming down over the planet, which is how a game opens.</summary>
    public required IntroSequence IntroSequence { get; init; }

    /// <summary>The nasties that wander each screen.</summary>
    public required Nasties Nasties { get; init; }

    /// <summary>How he comes apart when one of them gets him.</summary>
    public required BlobDeathAnimation BlobKiller { get; init; }

    /// <summary>The nasties that run along conveyer rails.</summary>
    public required ConveyerNasties ConveyerNasties { get; init; }

    /// <summary>The electricity gates and the nodes that feed them.</summary>
    public required ElectricityGatesAndNodesEngine ElectricityNodeGates { get; init; }
}
