using skolenza.application.Interfaces.IEncryptionService;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace skolenza.infrastructure.EncryptionService
{
    public class AesEncryptionService : IEncryptionService
    {
        // 🔹 Encrypt string using AES-CBC + PKCS7, returns standard Base64
        public string Encrypt(string plainText, string institutionKey)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var sha = SHA256.Create();
            var key = sha.ComputeHash(Encoding.UTF8.GetBytes(institutionKey ?? ""));

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);

            return Convert.ToBase64String(result);
        }

        // 🔹 Decrypt string safely
        public string Decrypt(string cipherText, string institutionKey)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            // ✅ Super Duper Sanitizer
            string sanitized = SanitizeInput(cipherText);

            byte[] encryptedBytes;
            try
            {
                encryptedBytes = Convert.FromBase64String(sanitized);
            }
            catch
            {
                return string.Empty; // invalid Base64
            }

            using var sha = SHA256.Create();
            var key = sha.ComputeHash(Encoding.UTF8.GetBytes(institutionKey ?? ""));

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[encryptedBytes.Length - iv.Length];
            Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        // 🔹 Generate institution key (32 bytes Base64)
        public string GenerateKey()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        #region Helper: Super Duper Input Sanitizer
        private string SanitizeInput(string input)
        {
            string s = input.Trim();
            s = s.Trim('"', '\'', '`');
            s = s.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\\", "");
            s = Uri.UnescapeDataString(s); // handle %2B, %2F, etc
            s = s.Replace("%2B", "+").Replace("%2F", "/").Replace("%3D", "=");
            s = Regex.Replace(s, @"[\u200B-\u200D\uFEFF]", ""); // zero-width
            while (s.Length % 4 != 0) s += "=";
            s = Regex.Replace(s, @"[^A-Za-z0-9\+/=]", ""); // remove junk
            return s;
        }
        #endregion
    }
}
