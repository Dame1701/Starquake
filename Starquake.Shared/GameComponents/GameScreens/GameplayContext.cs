// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.ActiveSprites;
using Starquake.ContentManagement;
using Starquake.Engines.ScreenEffects;
using Starquake.Input;
using Starquake.Scoring;
using Starquake.Structure;

namespace Starquake.GameComponents.GameScreens;

/// <summary>
///     The nine standing collaborators <see cref="MainGameScreen" /> plays a game with: what to draw
///     with, what to draw through, what the player is pressing, where the other screens are, and the
///     score, the lives and the core that outlive any one game.
/// </summary>
/// <remarks>
///     The counterpart to <see cref="RunningGame" />, and the split between the two is lifetime.
///     Everything in that record comes into being when content finishes loading and is replaced when
///     it reloads; everything here is built in <c>GameEngine.Initialise</c> before any content exists
///     and lasts as long as the process. Folding the two together would have meant one record whose
///     own doc comment could not say when its properties were safe to read.
///     <para>
///         This pair is what <c>MainGameScreen</c> holds instead of the engine. It held a
///         <c>GameEngine</c> and reached eighteen of its members across a hundred and seventeen call
///         sites, which made it the only class in the codebase still holding one and the last
///         namespace cycle in the graph - the engine builds the screens, so the screens naming the
///         engine closed a loop. <c>GameScreenBase</c>'s remarks argue against fixing it on the
///         grounds that the screen would then hold that base class *and* the engine; the answer is
///         that it holds neither.
///     </para>
/// </remarks>
internal sealed class GameplayContext
{
    /// <summary>The sprite sheets, the sounds and the shaders.</summary>
    public required ResourceManager Resources { get; init; }

    /// <summary>The render targets the picture is composed through, and the effects over it.</summary>
    public required ScreenEffectEngine ScreenEffects { get; init; }

    /// <summary>The frame's input, read once by <c>GameHost</c>.</summary>
    public required ToggleKeys Input { get; init; }

    /// <summary>Where the finished picture goes and what colour sits behind it.</summary>
    public required PresentationSettings Presentation { get; init; }

    /// <summary>Which screen is being shown, and where to find the others.</summary>
    public required ScreenDirectory ScreenDirectory { get; init; }

    /// <summary>Pause and the player's key set.</summary>
    public required GameSession Session { get; init; }

    /// <summary>The score, the lives and the energy bar.</summary>
    public required PlayerStats Stats { get; init; }

    /// <summary>The nine parts the planet's core is missing.</summary>
    public required CoreElements Elements { get; init; }

    /// <summary>The game's one generator. Not <c>Random.Shared</c> - see <c>ResourceManager</c>.</summary>
    public required Random RandomNumberGenerator { get; init; }
}
