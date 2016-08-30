using System;
using System.IO;
using System.Text;

namespace Common
{
    /// <summary>
    /// Converts binary data into HEX presentation for debugging.
    /// </summary>
    public static class HexDump
    {
        public static string Convert(byte[] data, int rowlength = 16)
        {
            return Convert(data, 0, data.Length, rowlength);
        }

        public static string Convert(byte[] data, int offset, int length, int rowlength = 16)
        {
            var sb = new StringBuilder();
            int n = (length + rowlength - 1) / rowlength * rowlength;
            var limit = offset + length;

            for(int j = offset; j < offset + length; j += rowlength)
            {
                for (int k = 0; k < rowlength; ++k)
                {
                    var i = j + k;
                    if (i < limit)
                    {
                        sb.AppendFormat("{0:X2} ", data[i]);
                    }
                    else
                    {
                        sb.Append("   ");
                    }
                }

                for(int k = 0; k < rowlength; ++k)
                {
                    var i = j + k;
                    if (i < limit)
                    {
                        var c = data[i];
                        if (0x20 <= c && c <= 0x80)
                        {
                            sb.Append((char)c);
                        }
                        else
                        {
                            sb.Append('.');
                        }
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append("\n");
            }

            return sb.ToString();
        }

        public static string Convert(Stream stream)
        {
            if(!stream.CanSeek || !stream.CanRead)
            {
                throw new Exception("cannot read the stream for hexdump.");
            }

            var p = stream.Position;
            stream.Position = 0;
            var s = new MemoryStream();
            stream.CopyTo(s);
            stream.Position = p;

            return Convert(s.ToArray());
        }
    }
}
