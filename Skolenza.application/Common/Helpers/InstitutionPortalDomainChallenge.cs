using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using skolenza.application.Constants;
using skolenza.application.Exceptions;

namespace skolenza.application.Common.Helpers;

public static class InstitutionPortalDomainChallenge
{
    public static string NormalizeDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("://", StringComparison.Ordinal) ||
            value.Contains('/') || value.Contains('@') || value.Contains(':') || value.Contains('*'))
        {
            throw new ValidationErrorException("Enter a hostname only, without scheme, port, path or wildcard.");
        }

        var candidate = value.Trim().TrimEnd('.');
        try
        {
            candidate = new IdnMapping().GetAscii(candidate).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            throw new ValidationErrorException("The portal domain is not a valid internationalized hostname.");
        }

        if (candidate.Length is < 4 or > 253 || IPAddress.TryParse(candidate, out _) ||
            candidate.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationErrorException("The portal domain must be a valid public hostname.");
        }

        var labels = candidate.Split('.');
        if (labels.Length < 2 || labels.Any(label =>
                label.Length is < 1 or > 63 || !char.IsAsciiLetterOrDigit(label[0]) ||
                !char.IsAsciiLetterOrDigit(label[^1]) ||
                label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            throw new ValidationErrorException("The portal domain must contain valid DNS labels.");
        }

        return candidate;
    }

    public static string CreateChallengeValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return StudentConstants.PortalDomainChallengePrefix + token;
    }

    public static string CreateRecordName(string domainName) =>
        $"{StudentConstants.PortalDomainTxtRecordPrefix}.{domainName}";

    public static string HashChallenge(string challengeValue) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(challengeValue))).ToLowerInvariant();

    public static bool Matches(string storedHash, IEnumerable<string> records)
    {
        var expected = Convert.FromHexString(storedHash);
        return records.SelectMany(ExtractTxtFragments).Any(fragment =>
        {
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(fragment));
            return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
        });
    }

    private static IEnumerable<string> ExtractTxtFragments(string record)
    {
        var fragments = new StringBuilder();
        var inQuote = false;
        for (var index = 0; index < record.Length; index++)
        {
            var character = record[index];
            if (character == '"')
            {
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote)
            {
                continue;
            }

            if (character == '\\' && index + 1 < record.Length)
            {
                index++;
                character = record[index];
            }

            fragments.Append(character);
        }

        if (fragments.Length > 0)
        {
            yield return fragments.ToString();
        }

        if (!record.Contains('"') && !string.IsNullOrWhiteSpace(record))
        {
            yield return record.Trim();
        }
    }
}
