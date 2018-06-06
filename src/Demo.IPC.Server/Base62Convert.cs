using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.IPC.Server.Utils
{
    public class Base62Convert
    {
        private static readonly long[] power = new long[]
        {
            1,
            62,
            3844,
            238328,
            14776336,
            916132832,
            56800235584,
            3521614606208,
            218340105584896,
            13537086546263552,
            839299365868340224
        };

        public static string ToBase62String(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be negative");
            }

            var size = 1;
            for (int i = 1; i < power.Length; i++)
            {
                if (value < power[i])
                {
                    size = i;
                    break;
                }
            }
            var output = new char[size];

            long r;
            for (int i = size - 1; i >= 0; i--)
            {
                value = Math.DivRem(value, 62, out r);
                output[i] = ToBase62Char((byte)r);
            }
            return new string(output);
        }

        public static long FromBase62String(string text)
        {
            long value = 0;

            for (int i = 0; i < text.Length; i++)
            {
                value += FromBase62Char(text[i]) * (long)Math.Pow(62, text.Length - i - 1);
            }
            return value;
        }


        public static byte FromBase62Char(char c)
        {
            var value = (byte)c;
            if (48 <= value && value <= 57)
            {
                value -= 48;
            }
            else if (65 <= value && value <= 90)
            {
                value -= 55;
            }
            else if (97 <= value && value <= 122)
            {
                value -= 61;
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
            return value;
        }

        public static char ToBase62Char(byte value)
        {
            if (value < 0 || 61 < value)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (value < 10)
            {
                return (char)(value + 48);
            }
            else if (value < 36)
            {
                return (char)(value + 55);
            }
            else
            {
                return (char)(value + 61);
            }
        }
    }
}
