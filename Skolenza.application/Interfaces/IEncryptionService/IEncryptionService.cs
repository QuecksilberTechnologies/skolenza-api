using System;

namespace skolenza.application.Interfaces.IEncryptionService
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypt plain text using institutionKey.
        /// Returns Base64 string (IV + Cipher)
        /// </summary>
        string Encrypt(string plainText, string institutionKey);

        /// <summary>
        /// Decrypt encrypted text using institutionKey.
        /// Input is sanitized automatically to handle spaces, newlines, Base64Url, etc.
        /// </summary>
        string Decrypt(string cipherText, string institutionKey);

        /// <summary>
        /// Generates a random 32-byte Base64 key for a institution.
        /// </summary>
        string GenerateKey();
    }
}
