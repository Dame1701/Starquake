// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Scoring;

/// <summary>
///     The table a fresh installation starts with - the eight names that used to be written into
///     <see cref="HiScores" />'s field initialiser, when they were the whole of the feature.
/// </summary>
/// <remarks>
///     They are still the table's starting point rather than a placeholder for one: deleting the
///     saved file brings these back, which is the closest thing the game has to a reset. Returned
///     as a new list each call because the caller owns it and sorts and splices it in place.
/// </remarks>
internal static class HiScoreDefaults
{
    /// <summary>
    ///     How many places the table has, and so the number of lines a saved file must have to be
    ///     believed. A test holds it against <see cref="Create" />'s own length, because the two
    ///     disagreeing would silently throw away every saved table.
    /// </summary>
    public const int TableSize = 8;

    public static List<HiScore> Create() =>
    [
        new()
        {
            Name = "Damien",
            Score = 55250,
            AdventureScore = 36
        },
        new()
        {
            Name = "Jacqui",
            Score = 40350,
            AdventureScore = 30
        },
        new()
        {
            Name = "Chris",
            Score = 27150,
            AdventureScore = 26
        },
        new()
        {
            Name = "Rowan",
            Score = 20750,
            AdventureScore = 20
        },
        new()
        {
            Name = "Matt",
            Score = 16500,
            AdventureScore = 15
        },
        new()
        {
            Name = "Pete",
            Score = 10250,
            AdventureScore = 11
        },
        new()
        {
            Name = "Andy",
            Score = 5250,
            AdventureScore = 5
        },
        new()
        {
            Name = "Maui",
            Score = 2150,
            AdventureScore = 3
        }
    ];
}
