// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Converts Host-facing encoded Institution identifiers to and from repository-safe numeric identifiers.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IEncryptionService;

namespace skolenza.application.Common.Helpers;

/// <summary>
/// Converts Institution identifiers at the Host API boundary through the established global ID encoder.
/// </summary>
public static class HostInstitutionIdentifierProtector
{
    /// <summary>
    /// Encodes a persisted Institution identifier for a Host-facing response.
    /// </summary>
    /// <param name="institutionId">The numeric Institution identifier from persistence.</param>
    /// <param name="institutionEncryptionKey">The trusted Host token key, sanitized before encoder use.</param>
    /// <param name="idEncoderService">The established application ID encoder.</param>
    /// <returns>The encoded Institution identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a persisted identifier cannot be protected.</exception>
    public static string Encrypt(
        long institutionId,
        string institutionEncryptionKey,
        IIdEncoderService idEncoderService)
    {
        if (institutionId <= 0 || string.IsNullOrWhiteSpace(institutionEncryptionKey))
        {
            throw new InvalidOperationException("A Host-facing Institution identifier could not be protected.");
        }

        var trustedInstitutionKey = EncryptionSanitizer.SuperSanitize(institutionEncryptionKey);
        var encodedInstitutionId = idEncoderService.EncodeId_long(institutionId, trustedInstitutionKey);
        if (string.IsNullOrWhiteSpace(encodedInstitutionId))
        {
            throw new InvalidOperationException("A Host-facing Institution identifier could not be protected.");
        }

        return encodedInstitutionId;
    }

    /// <summary>
    /// Decodes and validates a Host-facing Institution identifier before repository access.
    /// </summary>
    /// <param name="encryptedInstitutionId">The encrypted identifier submitted by the client.</param>
    /// <param name="institutionEncryptionKey">The trusted Host token key, sanitized before encoder use.</param>
    /// <param name="idEncoderService">The established application ID encoder.</param>
    /// <returns>The numeric Institution identifier for repository use.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the submitted identifier is malformed or raw.</exception>
    public static long Decrypt(
        string? encryptedInstitutionId,
        string institutionEncryptionKey,
        IIdEncoderService idEncoderService)
    {
        if (string.IsNullOrWhiteSpace(encryptedInstitutionId) ||
            string.IsNullOrWhiteSpace(institutionEncryptionKey) ||
            long.TryParse(encryptedInstitutionId, out _))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var trustedInstitutionKey = EncryptionSanitizer.SuperSanitize(institutionEncryptionKey);
        var institutionId = idEncoderService.DecodeId_long(
            EncryptionSanitizer.CleanEncodedInput(encryptedInstitutionId),
            trustedInstitutionKey);
        if (institutionId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        return institutionId;
    }
}
