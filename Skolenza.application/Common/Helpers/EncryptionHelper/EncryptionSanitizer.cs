using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers.EncryptionHelper
{
    public static class EncryptionSanitizer
    {
        /// <summary>
        /// Super Duper Sanitizer 😎
        /// Cleans any encrypted/Base64/Base64Url string so that it can safely be decrypted.
        /// Handles spaces, newlines, URL encoding, quotes, backslashes, JSON escapes, emojis (🤯), etc.
        /// </summary>
        public static string SuperSanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string s = input;

            // 🔹 1️⃣ Basic Trim & Cleanup
            s = s.Trim();
            s = s.Trim('"', '\'', '`');               // remove quotes
            s = s.Replace("\\", "");                  // remove escape backslashes
            s = s.Replace(" ", "");                   // remove spaces
            s = s.Replace("\r", "").Replace("\n", ""); // remove newlines

            // 🔹 2️⃣ Handle common URL encoded Base64 chars
            s = Uri.UnescapeDataString(s);             // decode %2B, %2F, %3D etc.
            s = s.Replace("%2B", "+")
                 .Replace("%2F", "/")
                 .Replace("%3D", "=")
                 .Replace("-", "+")                    // Base64Url fix
                 .Replace("_", "/");

            // 🔹 3️⃣ Remove any invisible zero-width or Unicode control chars
            s = Regex.Replace(s, @"[\u200B-\u200D\uFEFF]", "");

            // 🔹 4️⃣ Fix padding (Base64 should be multiple of 4)
            s = s.TrimEnd('='); // remove junk padding first
            while (s.Length % 4 != 0)
                s += "=";

            // 🔹 5️⃣ Remove any stray non-Base64 characters
            s = Regex.Replace(s, @"[^A-Za-z0-9\+/=]", "");

            return s;
        }

        public static string CleanEncodedInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string s = input.Trim('"', '\'', '`', ' ', '\r', '\n'); // Remove wrapping quotes or spaces

            // Fix accidental JSON escape or slashes
            s = s.Replace("\\", "");  // remove \\
            s = s.Replace("/", "");   // remove forward slashes (for safety)

            // Handle URL-encoded chars (like %2F)
            s = Uri.UnescapeDataString(s);

            return s;
        }

    }

}
