using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bech32cosmos
{
    public static class Bech32cosmos
    {
        private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        private const char Separator = ':';

        private static readonly Dictionary<char, int> CharsetMap = Charset.Select((c, i) => new { c, i }).ToDictionary(x => x.c, x => x.i);

        /// <summary>
        /// Decode decodes a bech32cosmos encoded string
        /// </summary>
        /// <returns>The human-readable part and the data part excluding the checksum</returns>
        public static (string, byte[]) Decode(string bech, int limit = 90)
        {
            if (string.IsNullOrEmpty(bech))
            {
                throw new Exception($"empty bech32 string");
            }
            // The maximum allowed length for a bech32 string is 90. It must also
            // be at least 8 characters, since it needs a non-empty HRP, a
            // separator, and a 6 character checksum.
            if (bech.Length < 8 || bech.Length > limit)
            {
                throw new Exception($"invalid bech32 string length {bech.Length}");
            }
            // Only	ASCII characters between 33 and 126 are allowed.
            foreach (var c in bech)
            {
                if (c < 33 || c > 126)
                    throw new Exception($"invalid character in string: '{c}'");
            }

            // The characters must be either all lowercase or all uppercase.
            // We'll work with the lowercase string from now on.
            bech = bech.ToLowerInvariant();

            // The string is invalid if the last ':' is non-existent, it is the
            // first character of the string (no human-readable part) or one of the
            // last 6 characters of the string (since checksum cannot contain '1'),
            // or if the string is more than 90 characters in total.
            var sep = bech.LastIndexOf(Separator);
            if (sep < 1 || sep + 7 > bech.Length)
            {
                throw new Exception($"invalid index of {Separator}");
            }
            // The human-readable part is everything before the last ':'.
            var hrp = bech.Substring(0, sep);
            var data = bech.Substring(sep + 1);

            // Each character corresponds to the byte with value of the index in 'charset'.
            var decoded = ToBytes(data);
            var decodedChecksum = decoded.Take(decoded.Length - 6).ToArray();
            if (!Bech32VerifyChecksum(hrp, decoded.Select(x => (int)x)))
            {
                var checksum = bech.Substring(bech.Length - 6);
                var expected = ToChars(Bech32Checksum(hrp, decodedChecksum));
                throw new Exception($"Checksum failed. Expected {expected}, got {checksum}");
            }
            // We exclude the last 6 bytes, which is the checksum.
            return (hrp, decodedChecksum);
        }

        /// <summary>
        /// Encode encodes a byte slice into a bech32cosmos string with the
        /// human-readable part hrb. Note that the bytes must each encode 5 bits
        /// (base32).
        /// </summary>
        public static string Encode(string hrp, byte[] data, int limit = 90)
        {
            if ((hrp.Length + 7 + data.Length) > limit) throw new Exception("Exceeds length limit");

            // Calculate the checksum of the data and append it at the end.
            hrp = hrp.ToLowerInvariant();
            var checksum = Bech32Checksum(hrp, data);
            var combined = data.Concat(checksum);

            // The resulting bech32 string is the concatenation of the hrp, the
            // separator 1, data and checksum. Everything after the separator is
            // represented using the specified charset.
            try
            {
                return $"{hrp}{Separator}{ToChars(combined)}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to convert data bytes to chars: {ex.Message}");
            }
        }

        /// <summary>
        /// ConvertBits converts a byte slice where each byte is encoding fromBits bits,
        /// to a byte slice where each byte is encoding toBits bits.
        /// </summary>
        public static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
        {
            if (fromBits < 1 || fromBits > 8 || toBits < 1 || toBits > 8)
            {
                throw new Exception("only bit groups between 1 and 8 allowed");
            }

            // The final bytes, each byte encoding toBits bits.
            var regrouped = new List<byte>();

            // Keep track of the next byte we create and how many bits we have
            // added to it out of the toBits goal.
            var nextByte = (byte)0;
            var filledBits = 0;

            foreach (var d in data)
            {
                // Discard unused bits.
                var b = (byte)(d << (8 - fromBits));

                // How many bits remaining to extract from the input data.
                var remFromBits = fromBits;

                while (remFromBits > 0)
                {
                    // How many bits remaining to be added to the next byte.
                    var remToBits = toBits - filledBits;

                    // The number of bytes to next extract is the minimum of
                    // remFromBits and remToBits.
                    var toExtract = remFromBits;

                    if (remToBits < toExtract)
                    {
                        toExtract = remToBits;
                    }

                    // Add the next bits to nextByte, shifting the already
                    // added bits to the left.
                    nextByte = (byte)((nextByte << toExtract) | (b >> (8 - toExtract)));

                    // Discard the bits we just extracted and get ready for
                    // next iteration.
                    b = (byte)(b << toExtract);

                    remFromBits -= toExtract;
                    filledBits += toExtract;

                    // If the nextByte is completely filled, we add it to
                    // our regrouped bytes and start on the next byte.
                    if (filledBits == toBits)
                    {
                        regrouped.Add(nextByte);
                        filledBits = 0;
                        nextByte = 0;
                    }
                }
            }

            // We pad any unfinished group if specified.
            if (pad && filledBits > 0)
            {
                nextByte = (byte)(nextByte << (toBits - filledBits));
                regrouped.Add(nextByte);
                filledBits = 0;
                nextByte = 0;
            }

            // Any incomplete group must be <= 4 bits, and all zeroes.
            if (filledBits > 0 && (filledBits > 4 || nextByte != 0))
            {
                throw new Exception("invalid incomplete group");
            }
            return regrouped.ToArray();
        }

        /// <summary>
        /// toBytes converts each character in the string 'chars' to the value 
        /// of the index of the correspoding character in 'charset'.
        /// </summary>
        private static byte[] ToBytes(string chars)
        {
            try
            {
                return chars
                    .Select(c => CharsetMap.TryGetValue(c, out int i)
                        ? (byte)i
                        : throw new Exception($"invalid character not part of charset: {c}"))
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed converting data to bytes: {ex.Message}");
            }
        }

        /// <summary>
        /// toChars converts the byte slice 'data' to a string where each byte in 'data'
        /// encodes the index of a character in 'charset'.
        /// </summary>
        public static string ToChars(IEnumerable<byte> data)
        {
            return new string(
                data.Select(b => b >= Charset.Length
                    ? throw new Exception($"invalid data byte: {b}")
                    : Charset[b]).ToArray()
            );
        }

        /// <summary>
        /// For more details on the checksum calculation, please refer to BIP 173.
        /// </summary>
        private static byte[] Bech32Checksum(string hrp, IEnumerable<byte> data)
        {
            // Convert the bytes to list of integers, as this is needed for the
            // checksum calculation.
            var values = Bech32HrpExpand(hrp)
                .Concat(data.Select(x => (int)x))
                .Concat(new int[] { 0, 0, 0, 0, 0, 0 });

            var polymod = Bech32Polymod(values) ^ 1;
            var res = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                res[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
            }
            return res;
        }

        /// <summary>
        /// For more details on the polymod calculation, please refer to BIP 173.
        /// </summary>
        private static int Bech32Polymod(IEnumerable<int> values)
        {
            var chk = 1;
            foreach (var v in values)
            {
                var b = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ v;
                if (((b >> 0) & 1) == 1) chk ^= 0x3b6a57b2;
                if (((b >> 1) & 1) == 1) chk ^= 0x26508e6d;
                if (((b >> 2) & 1) == 1) chk ^= 0x1ea119fa;
                if (((b >> 3) & 1) == 1) chk ^= 0x3d4233dd;
                if (((b >> 4) & 1) == 1) chk ^= 0x2a1462b3;
            }
            return chk;
        }

        /// <summary>
        /// For more details on HRP expansion, please refer to BIP 173.
        /// </summary>
        private static IEnumerable<int> Bech32HrpExpand(string hrp)
        {
            foreach (var c in hrp)
            {
                yield return c >> 5;
            }
            yield return 0;
            foreach (var c in hrp)
            {
                yield return c & 31;
            }
        }

        /// <summary>
        /// For more details on the checksum verification, please refer to BIP 173.
        /// </summary>
        private static bool Bech32VerifyChecksum(string hrp, IEnumerable<int> data)
        {
            var concat = Bech32HrpExpand(hrp).Concat(data);
            return Bech32Polymod(concat) == 1;
        }
    }
}
