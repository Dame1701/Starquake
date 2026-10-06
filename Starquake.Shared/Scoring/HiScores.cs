// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Core;
using Starquake.Fonts;

namespace Starquake.Scoring;

/// <summary>
///     The table of eight best scores, loaded from <see cref="HiScoreStore" /> at construction and
///     written back when a newly typed name completes an entry.
/// </summary>
/// <remarks>
///     The original game never remembered scores and nor did this port: these eight names were a
///     field initialiser, and a score beating them lasted until the process ended. They are now
///     <see cref="HiScoreDefaults" />, and a missing file falls back to them *and writes itself
///     out*, so deleting the file is how the table is reset rather than a state the game limps
///     along in.
/// </remarks>
internal class HiScores
{
    private readonly List<HiScore> _scores;

    private readonly HiScoreStore _store;

    public HiScores(HiScoreStore store)
    {
        _store = store;

        List<HiScore>? saved = store.Read();

        _scores = saved ?? HiScoreDefaults.Create();

        // A first run, or a table the player has deleted. Putting the defaults straight back on
        // disk is what makes the file's absence a reset rather than a thing to be rediscovered
        // every launch, and it means the file the player goes looking for is always there.
        if (saved is null)
        {
            _store.Write(_scores);
        }
    }

    /// <summary>
    ///     The table, highest first. Read-only because a score goes in through
    ///     <see cref="AddScoreToTable" />, which is what keeps the length and the order right.
    /// </summary>
    public IReadOnlyList<HiScore> Scores => _scores;

    /// <summary>
    ///     Writes the table out as it currently stands.
    /// </summary>
    /// <remarks>
    ///     Separate from <see cref="AddScoreToTable" /> because the name is not known when the
    ///     score goes in. The screen puts the entry into the table with an empty name, the player
    ///     types into that same object letter by letter, and only pressing return says the entry is
    ///     finished - so that is where the save belongs, and saving on insert would persist a row
    ///     with no name against it.
    /// </remarks>
    public void Save() => _store.Write(_scores);

    /// <summary>
    ///     Checks to see if the player has achieved a score high enough for the table
    /// </summary>
    /// <param name="score"></param>
    /// <returns></returns>
    public bool GotHighScore(int score)
    {
        foreach (HiScore scoreItem in _scores)
        {
            if (score > scoreItem.Score)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     Puts a score into the table, above the first one it beats, and drops whatever was last to
    ///     keep the table the length it was. A score that beats nothing in the table is not added.
    /// </summary>
    public void AddScoreToTable(HiScore scoreToAdd)
    {
        for (int a = 0; a < _scores.Count; a++)
        {
            if (scoreToAdd.Score > _scores[a].Score)
            {
                _scores.Insert(a, scoreToAdd);
                _scores.RemoveAt(_scores.Count - 1);

                break;
            }
        }
    }

    public void Draw(ResourceManager resources, BitmapFont font, int xPos, int yPos, int fade)
    {
        int currentYPos = yPos;
        int currentXPos = xPos;

        for (int a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, Colours.WhiteFadedTo(fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, Colours.GetXna4Colour(150, 150, 150, fade));
            }

            currentYPos += 50;
        }

        font.Begin(BlendState.AlphaBlend);
        currentYPos = yPos;

        for (int a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                font.WriteText(currentXPos + 55, currentYPos, _scores[a].Name, 2, new Color(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full), (byte)fade);
            }
            else
            {
                font.WriteText(currentXPos + 55, currentYPos, _scores[a].Name, 2, new Color(150, 150, 150), (byte)fade);
            }

            currentYPos += 50;
        }
        currentYPos = yPos;

        for (int a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, Colours.WhiteFadedTo(fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, Colours.GetXna4Colour(150, 150, 150, fade));
            }
            currentYPos += 50;
        }
        font.End();
    }
}
