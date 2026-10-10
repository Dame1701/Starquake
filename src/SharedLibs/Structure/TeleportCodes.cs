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
///     A code belongs to the screen it reaches, not the booth it is typed into, so it is stored on
///     the screen (<see cref="GameScreenData.TeleportCode" />) and authored in the screen editor. The
///     game only looks codes up. The rules for what a code may be live here because the editor is the
///     only thing that creates one.
/// </remarks>
public static class TeleportCodes
{

    /// <summary>
    ///     How many letters a code has. Fixed by the artwork: the booth's display has five slots and
    ///     teleports as soon as the fifth is filled.
    /// </summary>
    public const int CodeLength = 5;

    /// <summary>
    ///     The fallback codes, keyed by the screen each one reaches. The editor fills these in for a
    ///     map with no codes stored, so opening an older file does not leave every teleporter dead.
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
    ///     Whether the booth could actually be made to spell this code. Its keypad holds the 26
    ///     letters and nothing else, in five slots, so any other length or character can never be
    ///     entered.
    /// </summary>
    public static bool IsWellFormed(string? code)
        => code is { Length: CodeLength } && code.All(char.IsAsciiLetter);

    /// <summary>
    ///     Puts a code in the single form it is stored in. The booth's keypad is caseless and its
    ///     monitor shows codes in upper case, so that is what the editor saves.
    /// </summary>
    public static string Normalise(string code)
        => code.Trim().ToUpperInvariant();
}
