using System;
using System.Text;

namespace Common
{
    /// <summary>
    /// Implements Base32 encoding according to RFC 4648.
    /// </summary>
    [GeneratorIgnore]
    public class Base32Encoding
    {
        #region Private

        /// <summary>According to RFC 4648</summary>
        private static string table = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        #endregion

        public static string Table { get { return table; } }

        #region Public Methods

        public static string GetString(byte[] buffer)
        {
            StringBuilder sb = new StringBuilder();

            int a = 0, r = 0;
            int j;
            for (j = 0; j < buffer.Length; ++j)
            {
                byte c = buffer[j];

                switch (j % 5)
                {
                    case 0:
                        a = c >> 3;
                        sb.Append(table[a]);
                        r = (c & 7) << 2;
                        break;

                    case 1:
                        a = r | (c >> 6);
                        sb.Append(table[a]);

                        a = (c & 0x3E) >> 1;
                        sb.Append(table[a]);

                        r = (c & 1) << 4;
                        break;

                    case 2:
                        a = r | (c >> 4);
                        sb.Append(table[a]);
                        r = (c & 15) << 1;
                        break;

                    case 3:
                        a = r | (c >> 7);
                        sb.Append(table[a]);

                        a = (c & 0x7C) >> 2;
                        sb.Append(table[a]);
                        r = (c & 3) << 3;
                        break;

                    case 4:
                        a = r | (c >> 5);
                        sb.Append(table[a]);
                        a = c & 0x1F;
                        sb.Append(table[a]);
                        break;
                }
            }

            if (0 != j % 5)
            {
                sb.Append(table[r]);
            }

            return sb.ToString();
        }

        public static byte[] GetBytes(String s)
        {
            int n = s.Length * 5 / 8;
            int j, k;

            byte[] result = new byte[n];
            int r = 0, a = 0;

            for (j = 0, k = 0; j < s.Length; ++j)
            {
                char letter = s[j];
                int c = table.IndexOf(letter);
                if (c < 0)
                {
                    throw new Exception("malformed base32 data");
                }

                switch (j % 8)
                {
                    case 0:
                        a = c << 3;
                        break;

                    case 1:
                        a |= c >> 2;
                        r = (c & 3) << 6;
                        result[k++] = (byte)a;
                        break;

                    case 2:
                        a = r | (c << 1);
                        break;

                    case 3:
                        a |= c >> 4;
                        r = (c & 15) << 4;
                        result[k++] = (byte)a;
                        break;

                    case 4:
                        a = r | (c >> 1);
                        r = (c & 1) << 7;
                        result[k++] = (byte)a;
                        break;

                    case 5:
                        a = r | (c << 2);
                        break;

                    case 6:
                        a |= c >> 3;
                        r = (c & 7) << 5;
                        result[k++] = (byte)a;
                        break;

                    case 7:
                        a = r | c;
                        result[k++] = (byte)a;
                        break;
                }
            }

            return result;
        }

        #endregion
    }
}
