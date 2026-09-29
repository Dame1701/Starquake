// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Text;

namespace StarquakeXNA.Helpers;

public class TextHelper
{
    public static int GetAsciiCode(string character)
    {
        if (character.Length == 1)
        {
            var ascii = Encoding.ASCII;
            var encodedBytes = ascii.GetBytes(character);

            return encodedBytes[0];
        }

        return -1;
    }
}