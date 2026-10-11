namespace skolenza.application.Interfaces.IEmail;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(
        string toEmail,
        string subject,
        string body,
        long? InstitutionId,
        string otp);

    /// <summary>
    /// Uses the Institution SMTP configuration first and falls back to the active
    /// Host default SMTP configuration when the Institution configuration is absent
    /// or incomplete.
    /// </summary>
    Task<bool> SendTemplatedEmailAsync(
        string templateCode,
        string toEmail,
        long? InstitutionId,
        Dictionary<string, string> placeholders);

    /// <summary>
    /// Sends through the active Host default SMTP configuration. Used by
    /// self-registration before the Institution SMTP configuration is used.
    /// </summary>
    Task<bool> SendTemplatedEmailUsingHostConfigAsync(
        string templateCode,
        string toEmail,
        long? InstitutionId,
        Dictionary<string, string> placeholders);

    Task<int?> QueueTemplatedEmailAsync(
        string templateCode,
        string toEmail,
        long? InstitutionId,
        Dictionary<string, string> placeholders);
}
