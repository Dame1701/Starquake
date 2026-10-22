// ---------------------------------------------
// CapriceNET Copyright (c) 2026 Damien C Green.
// ---------------------------------------------

namespace Starquake.Scoring;

internal class HiScore
{
    public int AdventureScore;
    public string Name = string.Empty;
    public int Score;

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
