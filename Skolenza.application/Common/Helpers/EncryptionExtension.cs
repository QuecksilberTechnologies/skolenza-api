
using skolenza.application.Interfaces.IEncryptionService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class EncryptionHelper1
    {
        /// <summary>
        /// Encrypts a numeric ID using institution-specific key.
        /// </summary>
        public static string EncryptId(IEncryptionService encryptionService, long id, string institutionKey)
        {
            if (id <= 0)
                return string.Empty;

            return encryptionService.Encrypt(id.ToString(), institutionKey);
        }

        /// <summary>
        /// Decrypts an encrypted ID back to long.
        /// </summary>
        public static long DecryptId(IEncryptionService encryptionService, string encryptedId, string institutionKey)
        {
            if (string.IsNullOrEmpty(encryptedId))
                return 0;

            try
            {
                string decryptedValue = encryptionService.Decrypt(encryptedId, institutionKey);
                return long.TryParse(decryptedValue, out long id) ? id : 0;
            }
            catch
            {
                // optional: log error here if needed
                return 0; // fail-safe return
            }
        }
    }


}
