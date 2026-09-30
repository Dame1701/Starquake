// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Text;

namespace Starquake.Helpers;

public class TextHelper
{
    public static int GetAsciiCode(string character)
    {
        if (character.Length == 1)
        {
            Encoding ascii = Encoding.ASCII;
            byte[] encodedBytes = ascii.GetBytes(character);

            return encodedBytes[0];
        }

        return -1;
    }
}
