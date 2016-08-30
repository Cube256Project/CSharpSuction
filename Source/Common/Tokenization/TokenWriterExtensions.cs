
using System;
namespace Common.Tokenization
{
    public static class TokenWriterExtensions
    {
        public static void WriteTrace(this TokenWriter writer, string format, params object[] args)
        {
            writer.WriteLine(string.Format(format, args));
        }

        public static void WriteSpace(this TokenWriter writer)
        {
            writer.Write(" ");
        }

        public static void WriteBase64SingleQuotedString(this TokenWriter writer, byte[] b)
        {
            //writer.WriteMultiLine("b'" + Convert.ToBase64String(b, Base64FormattingOptions.InsertLineBreaks) + "'");
            writer.WriteMultiLine("b'" + Convert.ToBase64String(b) + "'");
        }
    }
}
