using skolenza.application.Interfaces.IEncryptionService;
using HashidsNet;
using System;
using System.Text;

namespace skolenza.infrastructure.EncryptionService
{
    public class IdEncoderService : IIdEncoderService
    {
        private const string GlobalSalt = "Skolenza_Global_Static_Salt";

        // 🔹 Common salt generator
        private string GetSalt(string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(institutionKey))
                institutionKey = "default";
            return $"{GlobalSalt}_{institutionKey.Trim()}";
        }

        // ✅ Encode numeric ID
        public string EncodeId_long(long id, string institutionKey)
        {
            if (id <= 0)
                return string.Empty;

            var salt = GetSalt(GlobalSalt);
            var hashids = new Hashids(salt, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");
            return hashids.EncodeLong(id);
        }
      

        // ✅ Decode numeric ID
        public long DecodeId_long(string encodedId, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(encodedId))
                return 0;

            var salt = GetSalt(GlobalSalt);
            var hashids = new Hashids(salt, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");

            var decoded = hashids.DecodeLong(encodedId);
            return decoded.Length > 0 ? decoded[0] : 0;
        }

        // ✅ Decode numeric ID int
        public int DecodeId_int(string encodedId, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(encodedId))
                return 0;

            var salt = GetSalt(GlobalSalt);
            var hashids = new Hashids(
                salt,
                8,
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
            );

            var decoded = hashids.DecodeLong(encodedId);

            if (decoded.Length == 0)
                return 0;

            // Overflow protection
            if (decoded[0] > int.MaxValue)
                throw new OverflowException("Decoded value exceeds Int32 range.");

            return (int)decoded[0];
        }

        public string EncodeId_int(int id, string institutionKey)
        {
            if (id <= 0)
                return string.Empty;

            var salt = GetSalt(GlobalSalt);
            var hashids = new Hashids(
                salt,
                8, // length
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
            );

            return hashids.EncodeLong((int)id);
        }

        // ✅ Encode string → string (convert string to bytes → numeric → encode)

        public string EncodeString(string input, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(input);
            long numericValue = BitConverter.ToInt64(PadBytes(bytes), 0);

            var salt = GetSalt(GlobalSalt);
            var hashids = new Hashids(salt, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");
            return hashids.EncodeLong(numericValue);
        }

        // ✅ Decode string → original text
        public string DecodeString(string encoded, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(encoded))
                return string.Empty;

            var salt = GetSalt(institutionKey);
            var hashids = new Hashids(salt, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");

            var decoded = hashids.DecodeLong(encoded);
            if (decoded.Length == 0)
                return string.Empty;

            var bytes = BitConverter.GetBytes(decoded[0]);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        // Helper to make sure byte array has at least 8 bytes
        private byte[] PadBytes(byte[] bytes)
        {
            if (bytes.Length >= 8)
                return bytes;

            var padded = new byte[8];
            Array.Copy(bytes, padded, bytes.Length);
            return padded;
        }
    }
}
