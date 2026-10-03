// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Globalization;

namespace Starquake.Scoring;

/// <summary>
///     Where the hi-score table is kept between runs, and the only part of the game that reads or
///     writes a file of its own.
/// </summary>
/// <remarks>
///     The file is one line per place, <c>name</c>, <c>score</c> and <c>adventure score</c>
///     separated by tabs. Text rather than the MessagePack the map uses, for three reasons: it is
///     eight lines rather than a planet, deleting it by hand is a supported way to reset the table
///     and so looking at it by hand should work too, and a name is only ever the letters A to Z and
///     a space, so there is nothing a tab has to escape.
///     <para>
///         <see cref="Read" /> answers <c>null</c> for a file that is missing, unreadable or not
///         the shape this wrote, and the caller takes that as "use the defaults" without
///         distinguishing the cases - because the only thing it could do differently is tell the
///         player their table is corrupt, on a screen that has no room to say it.
///     </para>
///     <para>
///         Both directions swallow the input and output failures rather than letting them reach the
///         frame loop. That is the opposite of what <c>ResourceManager</c> does with a failed
///         content load, deliberately: content missing means the game cannot run, whereas a
///         read-only home directory means the table does not persist, and killing the game over
///         that would be the worse outcome of the two.
///     </para>
/// </remarks>
internal sealed class HiScoreStore
{
    private const char FieldSeparator = '\t';

    private const int FieldsPerLine = 3;

    private readonly string _path;

    /// <summary>
    ///     The store the game uses, writing where the running user can always be written to.
    /// </summary>
    public HiScoreStore()
        : this(DefaultPath)
    {
    }

    /// <summary>
    ///     A store over a named file, which is how the tests get one that is not the player's.
    /// </summary>
    public HiScoreStore(string path) => _path = path;

    /// <summary>
    ///     Beside the player's other application data rather than beside the executable: on a Mac
    ///     the game runs from inside a signed <c>.app</c>, and on every platform the build output
    ///     is thrown away and rewritten, so a table kept there would not survive a rebuild.
    /// </summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Starquake",
        "HiScores.txt");

    /// <summary>
    ///     The saved table, or <c>null</c> if there is not one worth having.
    /// </summary>
    public List<HiScore>? Read()
    {
        string[] lines;

        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            lines = File.ReadAllLines(_path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (lines.Length != HiScoreDefaults.TableSize)
        {
            return null;
        }

        List<HiScore> scores = [];

        foreach (string line in lines)
        {
            string[] fields = line.Split(FieldSeparator);

            if (fields.Length != FieldsPerLine ||
                !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int score) ||
                !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int adventureScore))
            {
                return null;
            }

            scores.Add(new HiScore
            {
                Name = fields[0],
                Score = score,
                AdventureScore = adventureScore
            });
        }

        // The table is ordered highest first, and everything asked of it relies on that - a score
        // is placed above the first one it beats, so a file out of order would put a new score in
        // the wrong place rather than merely display oddly. Reading is where that is re-established,
        // because a text file invites being edited by hand.
        scores.Sort((higher, lower) => lower.Score.CompareTo(higher.Score));

        return scores;
    }

    public void Write(IReadOnlyList<HiScore> scores)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);

            if (directory is { Length: > 0 })
            {
                Directory.CreateDirectory(directory);
            }

            // Built whole and moved into place, as the screen editor writes the map, so that a
            // failure part way through cannot leave a half a table behind - which would read back
            // as the wrong number of lines and be thrown away for the defaults.
            string temporaryPath = _path + ".tmp";

            File.WriteAllLines(temporaryPath, scores.Select(AsLine));
            File.Move(temporaryPath, _path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // See the class remarks: the table not persisting is not worth stopping the game for.
        }
    }

    private static string AsLine(HiScore score) => string.Join(
        FieldSeparator,
        score.Name,
        score.Score.ToString(CultureInfo.InvariantCulture),
        score.AdventureScore.ToString(CultureInfo.InvariantCulture));
}
