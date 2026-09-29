// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using System.Text;

namespace StarquakeXNA.Helpers;

public class TextHelper
{
    public static int GetAsciiCode(string Character)
    {
        if (Character.Length == 1)
        {
            var ascii = Encoding.ASCII;
            var encodedBytes = ascii.GetBytes(Character);

            return encodedBytes[0];
        }

        return -1;
    }
}