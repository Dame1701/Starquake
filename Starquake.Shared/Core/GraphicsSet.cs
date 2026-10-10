// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Constants;

namespace Starquake.Core;

/// <summary>
///     Which of the two sets of artwork the game is drawn with. F8 switches between the redrawn
///     artwork and the original 1985 graphics, and two kinds of thing follow: the ten sprite sheets
///     existing in both sets, named by <see cref="Names" />, and the details only one set has at all,
///     such as a backdrop or a lit node, which ask <see cref="IsRedrawn" /> whether to draw.
///     <para>
///         The flag and the names are one object so the set cannot be half switched.
///     </para>
///     <para>
///         Anything that draws must read this live rather than copy it, because the switch happens
///         mid-game. A class handed a <c>bool</c> at construction would keep drawing the set that was
///         in play when it was built.
///     </para>
/// </summary>
internal sealed class GraphicsSet
{
    /// <summary>
    ///     The names the ten swappable sprite sheets are registered under for the set in play.
    /// </summary>
    public ResourceNames Names { get; private set; } = ResourceNames.Redrawn;

    /// <summary>Whether the original 1985 graphics are in play.</summary>
    public bool IsOriginal { get; private set; }

    /// <summary>
    ///     Whether the redrawn artwork is in play. <see cref="IsOriginal" /> asked the other way
    ///     round, because most callers are asking whether to draw something only the redrawn set has
    ///     and read better without a negation.
    /// </summary>
    public bool IsRedrawn => !IsOriginal;

    /// <summary>Swaps to the other set of artwork.</summary>
    public void Switch()
    {
        IsOriginal = !IsOriginal;
        Names = IsOriginal ? ResourceNames.Original : ResourceNames.Redrawn;
    }
}
