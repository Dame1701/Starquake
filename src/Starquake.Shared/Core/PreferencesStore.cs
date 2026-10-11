// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
namespace Starquake.Core;

/// <summary>
///     Where <see cref="Preferences" /> are kept between runs, beside the hi-score table.
/// </summary>
/// <remarks>
///     One <c>name=value</c> line per setting, text for the same reasons as <c>HiScoreStore</c>.
///     Unlike the table, each line stands alone: a line that is missing, unknown or unreadable
///     leaves that one setting at its default rather than throwing the rest away, so a file from an
///     older build, or one edited by hand, still keeps everything it can.
///     <para>
///         Input and output failures are swallowed in both directions, as the table's are. A
///         preference that does not persist is not worth stopping the game for.
///     </para>
/// </remarks>
internal sealed class PreferencesStore
{
    private const char Separator = '=';

    private const string CrtKey = "Crt";
    private const string GraphicsKey = "Graphics";
    private const string BackdropsKey = "Backdrops";
    private const string HertzKey = "Hertz";
    private const string AnalogueWalkKey = "AnalogueWalk";

    private const string Original = "Original";
    private const string Enhanced = "Enhanced";
    private const string On = "On";
    private const string Off = "Off";

    private readonly string _path;

    /// <summary>The store the game uses.</summary>
    public PreferencesStore()
        : this(DefaultPath)
    {
    }

    /// <summary>A store over a named file, which is how the tests get one that is not the player's.</summary>
    public PreferencesStore(string path) => _path = path;

    /// <summary>Beside <c>HiScoreStore.DefaultPath</c>, for the reasons given there.</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Starquake",
        "Preferences.txt");

    /// <summary>The saved preferences, with the default standing in for anything not saved.</summary>
    public Preferences Read()
    {
        string[] lines;

        try
        {
            if (!File.Exists(_path))
            {
                return Preferences.Defaults;
            }

            lines = File.ReadAllLines(_path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Preferences.Defaults;
        }

        Preferences preferences = Preferences.Defaults;

        foreach (string line in lines)
        {
            int separator = line.IndexOf(Separator);

            if (separator < 0)
            {
                continue;
            }

            string name = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();

            preferences = name switch
            {
                CrtKey when Enum.TryParse(value, out CrtMode mode) && Enum.IsDefined(mode)
                    => preferences with { CrtMode = mode },
                GraphicsKey when value is Original or Enhanced
                    => preferences with { OriginalGraphics = value == Original },
                BackdropsKey when value is On or Off
                    => preferences with { Backdrops = value == On },
                HertzKey when value is "50" or "60"
                    => preferences with { Hertz = new LogicRate(int.Parse(value)).Hertz },
                AnalogueWalkKey when value is On or Off
                    => preferences with { AnalogueWalk = value == On },
                _ => preferences
            };
        }

        return preferences;
    }

    public void Write(Preferences preferences)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);

            if (directory is { Length: > 0 })
            {
                Directory.CreateDirectory(directory);
            }

            // Built whole and moved into place, as the hi-score table is, so a failure part way
            // through cannot leave half a file behind.
            string temporaryPath = _path + ".tmp";

            File.WriteAllLines(
                temporaryPath,
                [
                    $"{CrtKey}{Separator}{preferences.CrtMode}",
                    $"{GraphicsKey}{Separator}{(preferences.OriginalGraphics ? Original : Enhanced)}",
                    $"{BackdropsKey}{Separator}{(preferences.Backdrops ? On : Off)}",
                    $"{HertzKey}{Separator}{preferences.Hertz}",
                    $"{AnalogueWalkKey}{Separator}{(preferences.AnalogueWalk ? On : Off)}"
                ]);
            File.Move(temporaryPath, _path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // See the class remarks.
        }
    }
}
