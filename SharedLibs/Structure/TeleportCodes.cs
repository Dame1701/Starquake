// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.SharedLibs;

namespace Starquake.Structure;

/// <summary>
///     What a teleport destination code may be, and the codes the planet shipped with.
/// </summary>
/// <remarks>
///     <para>
///         A code belongs to the screen it reaches rather than to the booth it is typed into, so it
///         is stored on the screen - see <see cref="GameScreenData.TeleportCode" /> - and authored in
///         the screen editor. The game only ever looks one up; everything that decides what a code
///         may be lives here, because the editor is the only thing that makes one.
///     </para>
/// </remarks>
public static class TeleportCodes
{

    /// <summary>
    ///     How many letters a code has. The booth's display has five slots and teleports the moment
    ///     the fifth is filled, so this is a fact about the artwork rather than a limit chosen here.
    /// </summary>
    public const int CodeLength = 5;

    /// <summary>
    ///     The codes the game had written into it before they were part of the map, keyed by the
    ///     screen each one reaches. The editor fills these in for a map saved before codes were
    ///     stored, so that opening an older file does not quietly leave every teleporter dead.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, string> Original = new Dictionary<int, string>
    {
        [40] = "GROIN",
        [31] = "AXIES",
        [66] = "TOOTH",
        [162] = "HANDY",
        [150] = "ALIEN",
        [213] = "PLUTO",
        [289] = "ASCII",
        [343] = "ORION",
        [457] = "SNOWY",
        [433] = "TALON",
        [380] = "ROCKY",
        [470] = "URAIQ",
        [499] = "PALMA",
        [461] = "AISLE",
        [506] = "EXEON"
    };

    /// <summary>
    ///     Whether a code is one the booth could actually be made to spell. Its keypad has the
    ///     twenty-six letters and nothing else on it, and five slots to put them in, so a code that
    ///     is any other length or holds anything else can never be entered.
    /// </summary>
    public static bool IsWellFormed(string? code)
        => code is { Length: CodeLength } && code.All(char.IsAsciiLetter);

    /// <summary>
    ///     A code in the one form it is stored in. The booth's keypad has no case to it and the code
    ///     it spells out on screen is upper case, so that is the form the editor keeps a code in and
    ///     the form the booth's monitor shows it back in.
    /// </summary>
    public static string Normalise(string code)
        => code.Trim().ToUpperInvariant();
}
