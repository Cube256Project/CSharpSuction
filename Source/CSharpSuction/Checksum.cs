using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSuction
{
    class Checksum
    {
        private byte[] _digest;

        public string ID
        {
            get
            {
                return _digest.Select(b => b.ToString("X2")).ToSeparatorList("");
            }
        }

        public Checksum(string fullpath)
        {
            using (var stream = File.OpenRead(fullpath))
            using (var hash = SHA256.Create())
            {
                _digest = hash.ComputeHash(stream);
            }
        }
    }
}
