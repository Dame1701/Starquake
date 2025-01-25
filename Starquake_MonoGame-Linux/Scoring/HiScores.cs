using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarquakeXNA.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Fonts;

namespace StarquakeXNA.Scoring
{
    public class HiScores
    {
        private List<HiScore> scores;

        public HiScores()
        {
            scores = new List<HiScore>();

            scores.Add(new HiScore() { Name = "Damien", Score = 55250, AdventureScore = 36 });
            scores.Add(new HiScore() { Name = "Lucy", Score = 40350, AdventureScore = 30 });
            scores.Add(new HiScore() { Name = "Chris", Score = 27150, AdventureScore = 26 });
            scores.Add(new HiScore() { Name = "Swainy", Score = 20750, AdventureScore = 20 });
            scores.Add(new HiScore() { Name = "Matt", Score = 16500, AdventureScore = 15 });
            scores.Add(new HiScore() { Name = "Glenn", Score = 10250, AdventureScore = 11 });
            scores.Add(new HiScore() { Name = "Andy", Score = 5250, AdventureScore = 5 });
            scores.Add(new HiScore() { Name = "Nick", Score = 2150, AdventureScore = 3 });
        }

        /// <summary>
        /// Checks to see if the player has achieved a score high enough for the table
        /// </summary>
        /// <param name="Score"></param>
        /// <returns></returns>
        public bool GotHighScore(int Score)
        {
            for (int a = 0; a < scores.Count; a++)
            {
                if (Score > scores[a].Score)
                {
                    return true;
                }
            }

            return false;
        }

        // Insert a player into the table
        public void AddScoreToTable(HiScore ScoreToAdd)
        {
            for (int a = 0; a < scores.Count; a++)
            {
                if (ScoreToAdd.Score > scores[a].Score)
                {
                    // Insert the score above the one that's less than the new one
                    scores.Insert(a, ScoreToAdd);

                    // Remove the score at the bottom of the list
                    scores.RemoveAt(scores.Count - 1);

                    break;
                }
            }
        }

        public void Draw(GameEngine gameEngine, GameTime gameTime, int XPos, int YPos, int Fade)
        {
            int yPos = YPos;
            int xPos = XPos;

            for (int a = 0; a < scores.Count; a++)
            {
                if (a % 2 == 0)
                {
                    ProportionalNumbers.Draw(xPos, yPos, (a + 1).ToString() + ".", gameEngine, gameTime, ColourHelper.GetXNA4Colour(255, 255, 255, Fade));
                }
                else
                {
                    ProportionalNumbers.Draw(xPos, yPos, (a + 1).ToString() + ".", gameEngine, gameTime, ColourHelper.GetXNA4Colour(150, 150, 150, Fade));
                }

                yPos = yPos + 50;
            }
            
            gameEngine.Font2.Begin(BlendState.AlphaBlend);
            yPos = YPos;

            for (int a = 0; a < scores.Count; a++)
            {
                if (a % 2 == 0)
                {
                    gameEngine.Font2.WriteText(xPos + 55, yPos, scores[a].Name, 2, new Color(255, 255, 255), (byte)Fade);
                }
                else
                {
                    gameEngine.Font2.WriteText(xPos + 55, yPos, scores[a].Name, 2, new Color(150, 150, 150), (byte)Fade);
                }

                yPos = yPos + 50;
            }

            yPos = YPos;

            for (int a = 0; a < scores.Count; a++)
            {
                if (a % 2 == 0)
                {
                    ProportionalNumbers.Draw(xPos + 250, yPos, scores[a].Score.ToString("000000") + " " + scores[a].AdventureScoreString + "%", gameEngine, gameTime, ColourHelper.GetXNA4Colour(255, 255, 255, Fade));
                }
                else
                {
                    ProportionalNumbers.Draw(xPos + 250, yPos, scores[a].Score.ToString("000000") + " " + scores[a].AdventureScoreString + "%", gameEngine, gameTime, ColourHelper.GetXNA4Colour(150, 150, 150, Fade));
                }
                yPos = yPos + 50;
            }

            gameEngine.Font2.End();
        }

        public void Reset()
        {
        }
    }

    public class HiScore
    {
        public string Name;
        public int Score;
        public int AdventureScore;

        public string AdventureScoreString
        {
            get
            {
                string temp = AdventureScore.ToString();

                if (temp.Length == 1)
                {
                    temp = " " + temp;
                }

                return temp;
            }
        }
    }
}
