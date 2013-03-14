using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CacheCow.Server.EntityTagStore.Memcached
{
    /// <summary>
    /// Byte representation of a string
    /// 
    /// Structure:
    /// 8 byte => constant header
    /// 4 byte => length (n) based on UTF representation
    /// n bytes of actual string
    /// 2 bytes null (0x0x) terminator
    /// 
    /// </summary>
    internal sealed class LengthedPrefixedString
    {
        private readonly string _internalString;
        private readonly byte[] _bytes;

        public static readonly byte[] Header = new byte[] {39, 1, 234, 5, 89, 213, 196, 20}; // 8-byte header

        public LengthedPrefixedString(string s)
        {
            _internalString = s;
            var bytes = new List<byte>();
            bytes.AddRange(Header);
            byte[] sbytes = Encoding.UTF8.GetBytes(s);
            bytes.AddRange(BitConverter.GetBytes(sbytes.Length));
            bytes.AddRange(sbytes);
            bytes.AddRange(new byte[]{0,0});
            _bytes = bytes.ToArray();
        }

        public string InternalString
        {
            get { return _internalString; }
        }

        public static bool TryRead(Stream stream, out LengthedPrefixedString s)
        {

            s = null;
            if (!MatchHeader(stream))
                return false;

            var lengthBytes = new byte[4];
            if (stream.Read(lengthBytes, 0, 4) < 4)
                return false;

            int length = BitConverter.ToInt32(lengthBytes, 0);
            var stringBytes = new byte[length];

            if (stream.Read(stringBytes, 0, length) < length)
                return false;
            try
            {
                string data = Encoding.UTF8.GetString(stringBytes);
                s = new LengthedPrefixedString(data);
            }
            catch
            {
                return false;
            }

            if (!HasTerminator(stream))
                return false;


            return true;
        }



        private static bool MatchHeader(Stream stream)
        {
            return MatchByteArray(stream, Header);
        }

        private static bool HasTerminator(Stream stream)
        {
            return MatchByteArray(stream, new byte[] {0, 0});
        }

        private static bool MatchByteArray(Stream stream, byte[] bytes)
        {
            try
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    int readByte = stream.ReadByte();
                    if (readByte < 0)
                        return false;
                    var b = (byte) readByte;
                    if (b != bytes[i])
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        public byte[] ToByteArray()
        {
            return _bytes;
        }



    }
}
