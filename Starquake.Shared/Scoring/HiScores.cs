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

public class HiScores
{
    private readonly List<HiScore> _scores;

    public HiScores()
    {
        _scores =
        [
            new HiScore
            {
                Name = "Damien",
                Score = 55250,
                AdventureScore = 36
            },

            new HiScore
            {
                Name = "Lucy",
                Score = 40350,
                AdventureScore = 30
            },

            new HiScore
            {
                Name = "Chris",
                Score = 27150,
                AdventureScore = 26
            },

            new HiScore
            {
                Name = "Swainy",
                Score = 20750,
                AdventureScore = 20
            },

            new HiScore
            {
                Name = "Matt",
                Score = 16500,
                AdventureScore = 15
            },

            new HiScore
            {
                Name = "Glenn",
                Score = 10250,
                AdventureScore = 11
            },

            new HiScore
            {
                Name = "Andy",
                Score = 5250,
                AdventureScore = 5
            },

            new HiScore
            {
                Name = "Nick",
                Score = 2150,
                AdventureScore = 3
            }

        ];

    }

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

    // Insert a player into the table
    public void AddScoreToTable(HiScore scoreToAdd)
    {
        for (int a = 0; a < _scores.Count; a++)
        {
            if (scoreToAdd.Score > _scores[a].Score)
            {
                // Insert the score above the one that's less than the new one
                _scores.Insert(a, scoreToAdd);

                // Remove the score at the bottom of the list
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
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, a + 1 + ".", resources, ColourHelper.GetXna4Colour(150, 150, 150, fade));
            }

            currentYPos = currentYPos + 50;
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

            currentYPos = currentYPos + 50;
        }

        currentYPos = yPos;

        for (int a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, ColourHelper.GetXna4Colour(ColourLevels.Full, ColourLevels.Full, ColourLevels.Full, fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", resources, ColourHelper.GetXna4Colour(150, 150, 150, fade));
            }
            currentYPos = currentYPos + 50;
        }

        font.End();
    }

    public void Reset()
    {
    }
}
