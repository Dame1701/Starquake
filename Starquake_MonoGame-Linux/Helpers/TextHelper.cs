using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarquakeXNA.Helpers
{
    public class TextHelper
    {
        public static int GetAsciiCode(string Character)
        {
            if (Character.Length == 1)
            {
                System.Text.Encoding ascii = System.Text.Encoding.ASCII;
                Byte[] encodedBytes = ascii.GetBytes(Character);
            
                return (int)encodedBytes[0];
            }

            return -1;
        }
    }
}
