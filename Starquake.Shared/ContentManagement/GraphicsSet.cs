// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.ContentManagement;

/// <summary>
///     Which of the two sets of artwork the game is currently drawn with. F8 switches between the
///     redrawn artwork and the original 1985 graphics, and two kinds of thing follow the switch:
///     the ten sprite sheets that exist in both sets, named by <see cref="Names" />, and the
///     handful of details that only one set has at all - a backdrop, an electricity arc, a lit
///     node - which ask <see cref="IsRedrawn" /> whether to draw.
///     <para>
///         These were two separate members on the engine, a <c>bool</c> and a
///         <see cref="ResourceNames" />, kept in step by hand in the switch. They are one decision,
///         so they are one object, and it cannot now be half switched.
///     </para>
///     <para>
///         Anything that draws has to read this live rather than copy it, because the switch
///         happens mid-game: a class handed a <c>bool</c> at construction would keep drawing the
///         set that was in play when it was built.
///     </para>
/// </summary>
internal sealed class GraphicsSet
{
    /// <summary>
    ///     The names the ten swappable sprite sheets are registered under for whichever set is in
    ///     play.
    /// </summary>
    public ResourceNames Names { get; private set; } = ResourceNames.Redrawn;

    /// <summary>
    ///     Whether the original 1985 graphics are in play.
    /// </summary>
    public bool IsOriginal { get; private set; }

    /// <summary>
    ///     Whether the redrawn artwork is in play. The same question as <see cref="IsOriginal" />
    ///     asked the other way round, because most of the callers are asking whether to draw
    ///     something only the redrawn set has, and read better without a negation.
    /// </summary>
    public bool IsRedrawn => !IsOriginal;

    /// <summary>
    ///     Swaps to the other set of artwork.
    /// </summary>
    public void Switch()
    {
        IsOriginal = !IsOriginal;
        Names = IsOriginal ? ResourceNames.Original : ResourceNames.Redrawn;
    }
}
