// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Constants;
using Starquake.ContentManagement;
using Starquake.Fonts;
using Starquake.Helpers;

namespace Starquake.Scoring;

internal class HiScores
{
    private readonly List<HiScore> _scores =
    [
        new()
        {
            Name = "Damien",
            Score = 55250,
            AdventureScore = 36
        },

        new()
        {
            Name = "Lucy",
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
            Name = "Swainy",
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
            Name = "Glenn",
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
            Name = "Nick",
            Score = 2150,
            AdventureScore = 3
        }

    ];

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
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, ColourHelper.WhiteFadedTo(fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, ColourHelper.GetXna4Colour(150, 150, 150, fade));
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
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, ColourHelper.WhiteFadedTo(fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, ColourHelper.GetXna4Colour(150, 150, 150, fade));
            }
            currentYPos += 50;
        }

        font.End();
    }
}
