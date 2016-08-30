using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Common
{
    public enum QuoteFormat
    {
        SingleQuoteEscapeDuplicate,
        DoubleQuoteEscapeDuplicate,
        DoubleQuoteCStyle
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Combines a list of object string values into a separated string.
        /// </summary>
        /// <param name="list">If null or empty, the empty string is returned.</param>
        /// <param name="sep"></param>
        /// <returns>The combined string.</returns>
        public static string ToSeparatorList(this IEnumerable<object> list, string sep = ", ")
        {
            var sb = new StringBuilder();
            if (null != list)
            {
                foreach (var s in list)
                {
                    if (0 < sb.Length) sb.Append(sep);
                    sb.AppendFormat("{0}", s);
                }
            }

            return sb.ToString();
        }

        public static string ToSeparatorList(this IEnumerable<object> list, char sep)
        {
            var sb = new StringBuilder();
            if (null != list)
            {
                foreach (var s in list)
                {
                    if (0 < sb.Length) sb.Append(sep);
                    sb.AppendFormat("{0}", s);
                }
            }

            return sb.ToString();
        }

        public static string ToPath(this IEnumerable<string> list)
        {
            return list.ToSeparatorList("/");
        }

        public static byte[] ToUTF8(this string s)
        {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        public static byte[] ToSHA256(this string s)
        {
            return SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        }

        public static string TrimSuffix(this string s, string suffix)
        {
            if(s.EndsWith(suffix))
            {
                s = s.Substring(0, s.Length - suffix.Length);
            }

            return s;
        }

        public static string Quote(this string s, QuoteFormat format = QuoteFormat.SingleQuoteEscapeDuplicate)
        {
            if (format == QuoteFormat.SingleQuoteEscapeDuplicate)
            {
                return "'" + s.Replace("'", "''") + "'";
            }
            else if (format == QuoteFormat.DoubleQuoteEscapeDuplicate)
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            else if (format == QuoteFormat.DoubleQuoteCStyle)
            {
                var output = new StringBuilder();
                output.Append('\"');
                foreach (var c in s)
                {
                    switch(c)
                    {
                        case '\"':
                            output.Append("\\\"");
                            break;

                        case '\r':
                            output.Append("\\r");
                            break;

                        case '\n':
                            output.Append("\\n");
                            break;

                        default:
                            output.Append(c);
                            break;
                    }
                }

                output.Append('\"');
                return output.ToString();
            }
            else
            {
                throw new ArgumentException("invalid format parameter.");
            }
        }

        public static string QuoteHeavy(this string s)
        {
            return Quote(s, QuoteFormat.DoubleQuoteEscapeDuplicate);
        }

        public static string CQuote(this string s)
        {
            return Quote(s, QuoteFormat.DoubleQuoteCStyle);
        }
    }
}
