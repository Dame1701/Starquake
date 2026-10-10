// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Text;

namespace Starquake.Fonts;

internal static class AsciiCodes
{
    public static int GetAsciiCode(string character)
    {
        if (character.Length != 1)
        {
            return -1;
        }

        Encoding ascii = Encoding.ASCII;
        byte[] encodedBytes = ascii.GetBytes(character);
        return encodedBytes[0];
    }
}
