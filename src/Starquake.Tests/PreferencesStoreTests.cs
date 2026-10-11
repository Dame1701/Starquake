// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Starquake.Core;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The function keys' settings surviving a restart.
/// </summary>
/// <remarks>
///     Each test gets a path of its own under the temporary directory and deletes it afterwards, so
///     none of them touches the preferences belonging to whoever is running them.
///     <see cref="PreferencesStore.DefaultPath" /> is deliberately never used here.
/// </remarks>
public sealed class PreferencesStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "starquake-preferences", Guid.NewGuid().ToString("N"));

    private string PreferencesFilePath => Path.Combine(_directory, "Preferences.txt");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [Fact]
    public void WithNoFileAtAll_TheDefaultsAreRead()
        => Assert.Equal(Preferences.Defaults, new PreferencesStore(PreferencesFilePath).Read());

    /// <summary>The headline behaviour. Every setting comes back as it was written.</summary>
    [Fact]
    public void PreferencesWritten_AreReadBackTheNextTimeTheGameStarts()
    {
        // Arrange
        var written = new Preferences(CrtMode.Tube, true, false, LogicRate.Ntsc, true);

        // Act
        new PreferencesStore(PreferencesFilePath).Write(written);

        // Assert
        Assert.Equal(written, new PreferencesStore(PreferencesFilePath).Read());
    }

    /// <summary>
    ///     A line the store cannot make sense of loses only that setting, so a file edited by hand
    ///     or written by another build keeps everything else.
    /// </summary>
    [Fact]
    public void ABadLine_LeavesOnlyItsOwnSettingAtTheDefault()
    {
        // Arrange
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(PreferencesFilePath, ["Crt=Phosphor", "Graphics=Sideways", "Hertz=75", "Nonsense", "Backdrops=Off"]);

        // Act
        Preferences read = new PreferencesStore(PreferencesFilePath).Read();

        // Assert
        Assert.Equal(Preferences.Defaults with { CrtMode = CrtMode.Phosphor, Backdrops = false }, read);
    }

    /// <summary>
    ///     A number in place of a mode's name parses as an enum, so it has to be turned away
    ///     separately or it would land the presenter on a mode that does not exist.
    /// </summary>
    [Fact]
    public void ACrtModeOutOfRange_IsLeftAtTheDefault()
    {
        // Arrange
        Directory.CreateDirectory(_directory);
        File.WriteAllLines(PreferencesFilePath, ["Crt=42"]);

        // Act & Assert
        Assert.Equal(Preferences.Defaults, new PreferencesStore(PreferencesFilePath).Read());
    }
}
