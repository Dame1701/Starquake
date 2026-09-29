// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starquake.Engines;
using Starquake.Fonts;
using Starquake.Helpers;

namespace Starquake.Scoring;

public class HiScores
{
    private readonly List<HiScore> _scores;

    public HiScores()
    {
        _scores = new List<HiScore>();

        _scores.Add(new HiScore
        {
            Name = "Damien",
            Score = 55250,
            AdventureScore = 36
        });
        _scores.Add(new HiScore
        {
            Name = "Lucy",
            Score = 40350,
            AdventureScore = 30
        });
        _scores.Add(new HiScore
        {
            Name = "Chris",
            Score = 27150,
            AdventureScore = 26
        });
        _scores.Add(new HiScore
        {
            Name = "Swainy",
            Score = 20750,
            AdventureScore = 20
        });
        _scores.Add(new HiScore
        {
            Name = "Matt",
            Score = 16500,
            AdventureScore = 15
        });
        _scores.Add(new HiScore
        {
            Name = "Glenn",
            Score = 10250,
            AdventureScore = 11
        });
        _scores.Add(new HiScore
        {
            Name = "Andy",
            Score = 5250,
            AdventureScore = 5
        });
        _scores.Add(new HiScore
        {
            Name = "Nick",
            Score = 2150,
            AdventureScore = 3
        });
    }

    /// <summary>
    ///     Checks to see if the player has achieved a score high enough for the table
    /// </summary>
    /// <param name="score"></param>
    /// <returns></returns>
    public bool GotHighScore(int score)
    {
        for (var a = 0; a < _scores.Count; a++)
        {
            if (score > _scores[a].Score)
            {
                return true;
            }
        }

        return false;
    }

    // Insert a player into the table
    public void AddScoreToTable(HiScore scoreToAdd)
    {
        for (var a = 0; a < _scores.Count; a++)
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

    public void Draw(GameEngine gameEngine, GameTime gameTime, int xPos, int yPos, int fade)
    {
        var currentYPos = yPos;
        var currentXPos = xPos;

        for (var a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, (a + 1) + ".", gameEngine, gameTime, ColourHelper.GetXna4Colour(255, 255, 255, fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos, currentYPos, (a + 1) + ".", gameEngine, gameTime, ColourHelper.GetXna4Colour(150, 150, 150, fade));
            }

            currentYPos = currentYPos + 50;
        }

        gameEngine.Font2.Begin(BlendState.AlphaBlend);
        currentYPos = yPos;

        for (var a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                gameEngine.Font2.WriteText(currentXPos + 55, currentYPos, _scores[a].Name, 2, new Color(255, 255, 255), (byte)fade);
            }
            else
            {
                gameEngine.Font2.WriteText(currentXPos + 55, currentYPos, _scores[a].Name, 2, new Color(150, 150, 150), (byte)fade);
            }

            currentYPos = currentYPos + 50;
        }

        currentYPos = yPos;

        for (var a = 0; a < _scores.Count; a++)
        {
            if (a % 2 == 0)
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", gameEngine, gameTime, ColourHelper.GetXna4Colour(255, 255, 255, fade));
            }
            else
            {
                ProportionalNumbers.Draw(currentXPos + 250, currentYPos, _scores[a].Score.ToString("000000") + " " + _scores[a].AdventureScoreString + "%", gameEngine, gameTime, ColourHelper.GetXna4Colour(150, 150, 150, fade));
            }
            currentYPos = currentYPos + 50;
        }

        gameEngine.Font2.End();
    }

    public void Reset()
    {
    }
}