using System.Security.Cryptography;
using System.Text;

namespace Common
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] barray)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in barray) sb.AppendFormat("{0:X2}", b);
            return sb.ToString();
        }

        public static string ToBase32(this byte[] data)
        {
            return Base32Encoding.GetString(data);
        }

        public static byte[] ToSHA256(this byte[] data)
        {
            return SHA256.Create().ComputeHash(data);
        }

    }
}
