// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

using Starquake.Scoring;
using Xunit;

namespace Starquake.Tests;

/// <summary>
///     The hi-score table surviving a restart.
/// </summary>
/// <remarks>
///     Each test gets a path of its own under the temporary directory and deletes it afterwards, so
///     none of them touches the table belonging to whoever is running them.
///     <see cref="HiScoreStore.DefaultPath" /> is deliberately never used here.
/// </remarks>
public sealed class HiScoreStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "starquake-hiscores", Guid.NewGuid().ToString("N"));

    private string ScoreFilePath => Path.Combine(_directory, "HiScores.txt");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    /// <summary>
    ///     <see cref="HiScoreDefaults.TableSize" /> is declared on trust. If the two drift apart,
    ///     every saved file is the wrong length and every saved table is silently thrown away for
    ///     the defaults.
    /// </summary>
    [Fact]
    public void TheDeclaredTableSize_IsTheNumberOfDefaultsThereActuallyAre()
        => Assert.Equal(HiScoreDefaults.TableSize, HiScoreDefaults.Create().Count);

    [Fact]
    public void WithNoFileAtAll_TheDefaultsAreLoaded()
    {
        // Act
        var table = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal(
            HiScoreDefaults.Create().Select(score => score.Name),
            table.Scores.Select(score => score.Name));
    }

    [Fact]
    public void WithNoFileAtAll_TheFileIsWrittenSoItIsThereNextTime()
    {
        // Act
        _ = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.True(File.Exists(ScoreFilePath));
        Assert.Equal(HiScoreDefaults.TableSize, File.ReadAllLines(ScoreFilePath).Length);
    }

    /// <summary>
    ///     The headline behaviour. A score survives the process ending.
    /// </summary>
    [Fact]
    public void AScoreSaved_IsInTheTableTheNextTimeTheGameStarts()
    {
        // Arrange
        var firstRun = new HiScores(new HiScoreStore(ScoreFilePath));

        firstRun.AddScoreToTable(new HiScore
        {
            Name = "ZOE",
            Score = 99999,
            AdventureScore = 42
        });

        // Act
        firstRun.Save();

        var secondRun = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal("ZOE", secondRun.Scores[0].Name);
        Assert.Equal(99999, secondRun.Scores[0].Score);
        Assert.Equal(42, secondRun.Scores[0].AdventureScore);
        Assert.Equal(HiScoreDefaults.TableSize, secondRun.Scores.Count);
    }

    /// <summary>
    ///     Deleting the file is the supported way to reset the table, so it has to behave exactly as
    ///     a first run does rather than leaving the game without a table.
    /// </summary>
    [Fact]
    public void AFileThatIsDeleted_BringsTheDefaultsBackAndWritesItselfOutAgain()
    {
        // Arrange - a table that has been played and saved
        var played = new HiScores(new HiScoreStore(ScoreFilePath));

        played.AddScoreToTable(new HiScore
        {
            Name = "ZOE",
            Score = 99999,
            AdventureScore = 42
        });
        played.Save();

        // Act
        File.Delete(ScoreFilePath);

        var afterTheDelete = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.DoesNotContain("ZOE", afterTheDelete.Scores.Select(score => score.Name));
        Assert.Equal(
            HiScoreDefaults.Create().Select(score => score.Name),
            afterTheDelete.Scores.Select(score => score.Name));
        Assert.True(File.Exists(ScoreFilePath));
    }

    /// <summary>
    ///     A name is typed from the letters A to Z and the space bar, so a space has to survive the
    ///     round trip - which is why the file separates its fields with a tab and not a space.
    /// </summary>
    [Fact]
    public void ANameWithASpaceInIt_SurvivesTheRoundTrip()
    {
        // Arrange
        var firstRun = new HiScores(new HiScoreStore(ScoreFilePath));

        firstRun.AddScoreToTable(new HiScore
        {
            Name = "A B C",
            Score = 99999,
            AdventureScore = 1
        });
        firstRun.Save();

        // Act
        var secondRun = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal("A B C", secondRun.Scores[0].Name);
    }

    /// <summary>
    ///     Pressing return without typing anything is allowed by the name-entry screen, so an empty
    ///     name is a real row and not a malformed one.
    /// </summary>
    [Fact]
    public void AnEmptyName_IsSavedAndReadBackAsEmptyRatherThanRejected()
    {
        // Arrange
        var firstRun = new HiScores(new HiScoreStore(ScoreFilePath));

        firstRun.AddScoreToTable(new HiScore
        {
            Name = string.Empty,
            Score = 99999,
            AdventureScore = 1
        });
        firstRun.Save();

        // Act
        var secondRun = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal(string.Empty, secondRun.Scores[0].Name);
        Assert.Equal(99999, secondRun.Scores[0].Score);
    }

    [Theory]
    [InlineData("not a score at all")]
    [InlineData("NAME\tnot-a-number\t5")]
    [InlineData("NAME\t500\tnot-a-number")]
    [InlineData("NAME\t500")]
    public void AFileWithALineThatDoesNotParse_FallsBackToTheDefaults(string brokenLine)
    {
        // Arrange - a table with one line replaced
        _ = new HiScores(new HiScoreStore(ScoreFilePath));

        string[] lines = File.ReadAllLines(ScoreFilePath);
        lines[3] = brokenLine;
        File.WriteAllLines(ScoreFilePath, lines);

        // Act
        var table = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal(
            HiScoreDefaults.Create().Select(score => score.Name),
            table.Scores.Select(score => score.Name));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(HiScoreDefaults.TableSize - 1)]
    public void AFileWithTheWrongNumberOfLines_FallsBackToTheDefaults(int lineCount)
    {
        // Arrange
        _ = new HiScores(new HiScoreStore(ScoreFilePath));

        File.WriteAllLines(ScoreFilePath, File.ReadAllLines(ScoreFilePath).Take(lineCount));

        // Act
        var table = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal(HiScoreDefaults.TableSize, table.Scores.Count);
        Assert.Equal(
            HiScoreDefaults.Create().Select(score => score.Name),
            table.Scores.Select(score => score.Name));
    }

    /// <summary>
    ///     The table is ordered highest first and <c>AddScoreToTable</c> relies on it - a score goes
    ///     in above the first one it beats. A hand-edited file out of order would otherwise put the
    ///     next score in the wrong place, so loading re-establishes the order.
    /// </summary>
    [Fact]
    public void AHandEditedFileOutOfOrder_IsSortedWhenItIsLoaded()
    {
        // Arrange - the lowest score moved to the top of the file
        _ = new HiScores(new HiScoreStore(ScoreFilePath));

        List<string> lines = [.. File.ReadAllLines(ScoreFilePath)];
        string lowest = lines[^1];
        lines.RemoveAt(lines.Count - 1);
        lines.Insert(0, lowest);
        File.WriteAllLines(ScoreFilePath, lines);

        // Act
        var table = new HiScores(new HiScoreStore(ScoreFilePath));

        // Assert
        Assert.Equal(
            table.Scores.Select(score => score.Score).OrderByDescending(score => score),
            table.Scores.Select(score => score.Score));
    }

    /// <summary>
    ///     A table that cannot be written is not worth stopping the game for, so the failure has to
    ///     stay inside the store rather than reaching the frame loop. The unwritable location here
    ///     is a directory path that is really a file.
    /// </summary>
    [Fact]
    public void AStoreThatCannotWrite_DoesNotThrow()
    {
        // Arrange - a file where the store wants a directory
        Directory.CreateDirectory(_directory);

        string fileInTheWay = Path.Combine(_directory, "blocked");
        File.WriteAllText(fileInTheWay, "not a directory");

        var store = new HiScoreStore(Path.Combine(fileInTheWay, "HiScores.txt"));

        // Act
        var table = new HiScores(store);

        // Assert - the defaults are still in memory, and nothing threw
        Assert.Equal(HiScoreDefaults.TableSize, table.Scores.Count);
        table.Save();
    }
}
