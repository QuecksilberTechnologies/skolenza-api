using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;

namespace skolenza.infrastructure.MailService;

/// <summary>
/// Sends rendered email templates through a Institution SMTP configuration. When a
/// Institution configuration is unavailable or invalid, the active Host default
/// configuration is used as a safe delivery fallback.
/// </summary>
public sealed class EmailService : IEmailService
{
    private const string FallbackLogoUrl = "https://cdn.skolenza.com/default-logo.png";

    private readonly IInstitutionEmailConfigRepository _institutionEmailConfigRepository;
    private readonly IDefaultEmailConfigRepository _defaultEmailConfigRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IInstitutionEmailTemplateRepository _institutionTemplateRepository;
    private readonly IInstitutionKeyResolver _institutionKeyResolver;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailQueueRepository _emailQueueRepository;

    public EmailService(
        IInstitutionEmailConfigRepository institutionEmailConfigRepository,
        IDefaultEmailConfigRepository defaultEmailConfigRepository,
        IEmailTemplateRepository templateRepository,
        IInstitutionEmailTemplateRepository institutionTemplateRepository,
        IEmailQueueRepository emailQueueRepository,
        IInstitutionKeyResolver institutionKeyResolver,
        IEncryptionService encryptionService,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _institutionEmailConfigRepository = institutionEmailConfigRepository;
        _defaultEmailConfigRepository = defaultEmailConfigRepository;
        _templateRepository = templateRepository;
        _institutionTemplateRepository = institutionTemplateRepository;
        _emailQueueRepository = emailQueueRepository;
        _institutionKeyResolver = institutionKeyResolver;
        _encryptionService = encryptionService;
        _logger = logger;
        _configuration = configuration;
    }

    public Task<bool> SendOtpEmailAsync(
        string toEmail,
        string subject,
        string body,
        long? institutionId,
        string otp) => throw new NotImplementedException();

    /// <summary>
    /// Sends using the active Institution configuration, falling back to the active
    /// Host default configuration when Institution SMTP is not usable.
    /// </summary>
    public Task<bool> SendTemplatedEmailAsync(
        string templateCode,
        string toEmail,
        long? institutionId,
        Dictionary<string, string> placeholders) =>
        SendTemplatedEmailInternalAsync(
            templateCode,
            toEmail,
            institutionId,
            placeholders,
            preferHostEmailConfiguration: false);

    /// <summary>
    /// Sends with the active Host default configuration. This is used during
    /// self-registration, before a newly created Institution's SMTP details should
    /// be trusted for delivery.
    /// </summary>
    public Task<bool> SendTemplatedEmailUsingHostConfigAsync(
        string templateCode,
        string toEmail,
        long? institutionId,
        Dictionary<string, string> placeholders) =>
        SendTemplatedEmailInternalAsync(
            templateCode,
            toEmail,
            institutionId,
            placeholders,
            preferHostEmailConfiguration: true);

    public Task<int?> QueueTemplatedEmailAsync(
        string templateCode,
        string toEmail,
        long? institutionId,
        Dictionary<string, string> placeholders) =>
        _emailQueueRepository.EnqueueAsync(
            templateCode,
            toEmail,
            institutionId,
            JsonSerializer.Serialize(placeholders));

    private async Task<bool> SendTemplatedEmailInternalAsync(
        string templateCode,
        string toEmail,
        long? institutionId,
        IReadOnlyDictionary<string, string> placeholders,
        bool preferHostEmailConfiguration)
    {
        try
        {
            var emailConfiguration = await ResolveSmtpConfigurationAsync(
                institutionId,
                preferHostEmailConfiguration);
            var recipientEmail = CleanDatabaseValue(toEmail);

            if (emailConfiguration is null || recipientEmail is null)
            {
                _logger.LogWarning(
                    "Email delivery configuration is unavailable or recipient is invalid | Template={TemplateCode} | InstitutionId={InstitutionId} | HasRecipient={HasRecipient}",
                    templateCode,
                    institutionId,
                    recipientEmail is not null);
                throw new ApiException(
                    AppConstants.ErrorCodes.SmtpConfigurationUnavailable,
                    AppConstants.ErrorMessages.SmtpConfigurationUnavailable,
                    (int)HttpStatusCode.BadGateway);
            }

            var template = await ResolveEmailTemplateAsync(
                templateCode,
                institutionId,
                emailConfiguration.Source);
            if (template is null)
            {
                _logger.LogWarning(
                    "Email template missing or inactive in both Institution and Host sources | Code={TemplateCode} | InstitutionId={InstitutionId}",
                    templateCode,
                    institutionId);
                throw new ApiException(
                    AppConstants.ErrorCodes.EmailTemplateUnavailable,
                    AppConstants.ErrorMessages.EmailTemplateUnavailable,
                    (int)HttpStatusCode.BadGateway);
            }

            var finalPlaceholders = BuildPlaceholders(emailConfiguration, placeholders);
            var subject = EmailTemplateRenderer.RenderBody(template.Subject ?? string.Empty, finalPlaceholders);
            var body = EmailTemplateRenderer.RenderBody(template.Body ?? string.Empty, finalPlaceholders);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfiguration.FromName, emailConfiguration.FromEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            var smtp = new SmtpClient { Timeout = 20_000 };
            try
            {
                await smtp.ConnectAsync(
                    emailConfiguration.SmtpHost,
                    emailConfiguration.SmtpPort,
                    SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(emailConfiguration.SmtpUsername, emailConfiguration.SmtpSecret);
                await smtp.SendAsync(message);
            }
            finally
            {
                // Connect/authenticate/send exceptions must still reach the
                // mail-specific handlers. Cleanup failures must never replace
                // that original result or turn an accepted email into failure.
                if (smtp.IsConnected)
                {
                    try
                    {
                        await smtp.DisconnectAsync(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "SMTP disconnect failed during cleanup | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                            templateCode,
                            institutionId,
                            recipientEmail);
                    }
                }

                try
                {
                    smtp.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "SMTP client disposal failed during cleanup | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                        templateCode,
                        institutionId,
                        recipientEmail);
                }
            }

            _logger.LogInformation(
                "SMTP accepted email | Template={TemplateCode} | InstitutionId={InstitutionId} | ConfigurationSource={ConfigurationSource} | To={To}",
                templateCode,
                institutionId,
                emailConfiguration.Source,
                recipientEmail);

            return true;
        }
        catch (MailKit.Security.AuthenticationException ex)
            when (ex.InnerException is SmtpCommandException smtpCommandException)
        {
            _logger.LogError(
                ex,
                "SMTP authentication rejected | Status={Status} | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                smtpCommandException.StatusCode,
                templateCode,
                institutionId,
                toEmail);
            throw CreateSmtpApiException(smtpCommandException);
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            _logger.LogError(
                ex,
                "SMTP authentication failed | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                templateCode,
                institutionId,
                toEmail);
            throw new ApiException(
                AppConstants.ErrorCodes.SmtpAuthenticationFailed,
                AppConstants.ErrorMessages.SmtpAuthenticationFailed,
                (int)HttpStatusCode.BadGateway);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "SMTP rejected email | Status={Status} | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                ex.StatusCode,
                templateCode,
                institutionId,
                toEmail);
            throw CreateSmtpApiException(ex);
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "SMTP protocol failure | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                templateCode,
                institutionId,
                toEmail);
            throw new ApiException(
                AppConstants.ErrorCodes.SmtpConnectionFailed,
                AppConstants.ErrorMessages.SmtpConnectionFailed,
                (int)HttpStatusCode.BadGateway);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email delivery failed | Template={TemplateCode} | InstitutionId={InstitutionId} | To={To}",
                templateCode,
                institutionId,
                toEmail);
            throw new ApiException(
                AppConstants.ErrorCodes.EmailDeliveryFailed,
                AppConstants.ErrorMessages.EmailDeliveryFailed,
                (int)HttpStatusCode.BadGateway);
        }
    }

    private static ApiException CreateSmtpApiException(SmtpCommandException exception)
    {
        var providerMessage = exception.Message ?? string.Empty;

        if (providerMessage.Contains("Unauthorized IP address", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiException(
                AppConstants.ErrorCodes.SmtpIpNotAuthorized,
                AppConstants.ErrorMessages.SmtpIpNotAuthorized,
                (int)HttpStatusCode.BadGateway);
        }

        if (providerMessage.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            providerMessage.Contains("credentials", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiException(
                AppConstants.ErrorCodes.SmtpAuthenticationFailed,
                AppConstants.ErrorMessages.SmtpAuthenticationFailed,
                (int)HttpStatusCode.BadGateway);
        }

        return new ApiException(
            AppConstants.ErrorCodes.SmtpProviderRejected,
            AppConstants.ErrorMessages.SmtpProviderRejected,
            (int)HttpStatusCode.BadGateway);
    }

    private async Task<ResolvedEmailTemplate?> ResolveEmailTemplateAsync(
        string templateCode,
        long? institutionId,
        string configurationSource)
    {
        if (string.Equals(configurationSource, "Institution", StringComparison.Ordinal) && institutionId is > 0)
        {
            var institutionTemplate = await _institutionTemplateRepository.GetActiveByCodeAsync(
                institutionId.Value,
                templateCode);
            if (institutionTemplate is not null)
            {
                return new ResolvedEmailTemplate(institutionTemplate.Subject, institutionTemplate.Body);
            }

            _logger.LogWarning(
                "Active Institution email template is unavailable; falling back to the Host template | Code={TemplateCode} | InstitutionId={InstitutionId}",
                templateCode,
                institutionId);
        }

        var hostTemplate = await _templateRepository.GetTemplateByCodeAsync(templateCode);
        return hostTemplate is null
            ? null
            : new ResolvedEmailTemplate(hostTemplate.Subject, hostTemplate.Body);
    }

    private async Task<ResolvedSmtpConfiguration?> ResolveSmtpConfigurationAsync(
        long? institutionId,
        bool preferHostEmailConfiguration)
    {
        InstitutionEmailConfig? institutionConfiguration = null;
        if (!preferHostEmailConfiguration && institutionId is > 0)
        {
            institutionConfiguration = await _institutionEmailConfigRepository.GetActiveEmailConfigAsync(institutionId);
        }

        var institutionContext = InstitutionEmailTemplateContext.From(institutionConfiguration);
        var institutionSmtpPassword = preferHostEmailConfiguration
            ? null
            : await ResolveInstitutionSmtpPasswordAsync(institutionConfiguration);

        if (!preferHostEmailConfiguration)
        {
            var resolvedInstitutionConfiguration = CreateSmtpConfiguration(
                institutionConfiguration?.SmtpHost,
                institutionConfiguration?.SmtpPort,
                institutionConfiguration?.SmtpUsername,
                institutionSmtpPassword,
                institutionConfiguration?.FromName,
                institutionConfiguration?.FromEmail,
                institutionContext,
                "Institution");

            if (resolvedInstitutionConfiguration is not null)
            {
                return resolvedInstitutionConfiguration;
            }

            _logger.LogWarning(
                "Institution SMTP configuration is missing or incomplete; falling back to the Host default configuration | InstitutionId={InstitutionId}",
                institutionId);
        }

        var hostConfiguration = await GetActiveHostEmailConfigurationAsync();
        var deploymentUsername = CleanDatabaseValue(_configuration["EmailConfig:SMTPUserName"]);
        var deploymentSecret = CleanDatabaseValue(_configuration["EmailConfig:Secret"]);
        var resolvedHostConfiguration = CreateSmtpConfiguration(
            hostConfiguration?.SmtpHost,
            hostConfiguration?.SmtpPort,
            CleanDatabaseValue(hostConfiguration?.SmtpUsername) ?? deploymentUsername,
            CleanDatabaseValue(hostConfiguration?.SmtpPasswordEncrypted) ?? deploymentSecret,
            hostConfiguration?.FromName,
            hostConfiguration?.FromEmail,
            institutionContext,
            "HostDefault");

        if (resolvedHostConfiguration is null)
        {
            _logger.LogError(
                "Active default Host SMTP configuration is missing or incomplete | InstitutionId={InstitutionId}",
                institutionId);
        }

        return resolvedHostConfiguration;
    }

    /// <summary>
    /// Retrieves only the Host configuration selected by both IsActive and
    /// IsDefault. Keeping this in one helper makes the fallback reusable by
    /// every mail flow without allowing an arbitrary Host record to be used.
    /// </summary>
    private Task<DefaultEmailConfig?> GetActiveHostEmailConfigurationAsync() =>
        _defaultEmailConfigRepository.GetActiveDefaultEmailConfigAsync();

    private static ResolvedSmtpConfiguration? CreateSmtpConfiguration(
        string? smtpHost,
        int? smtpPort,
        string? smtpUsername,
        string? smtpPassword,
        string? fromName,
        string? fromEmail,
        InstitutionEmailTemplateContext institutionContext,
        string source)
    {
        var cleanedHost = CleanDatabaseValue(smtpHost);
        var cleanedUsername = CleanDatabaseValue(smtpUsername);
        var cleanedSecret = CleanDatabaseValue(smtpPassword);
        var cleanedFromName = CleanDatabaseValue(fromName);
        var cleanedFromEmail = CleanDatabaseValue(fromEmail);
        var port = smtpPort.GetValueOrDefault();

        if (cleanedHost is null || cleanedUsername is null || cleanedSecret is null ||
            cleanedFromName is null || cleanedFromEmail is null || port is < 1 or > 65535)
        {
            return null;
        }

        return new ResolvedSmtpConfiguration(
            cleanedHost,
            port,
            cleanedUsername,
            cleanedSecret,
            cleanedFromName,
            cleanedFromEmail,
            institutionContext,
            source);
    }

    private async Task<string?> ResolveInstitutionSmtpPasswordAsync(InstitutionEmailConfig? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        // Records created before this change stored the raw password in SecrateKey.
        // Keep sending mail for them until an edit migrates the value to ciphertext.
        var legacyPlaintext = CleanDatabaseValue(configuration.SecrateKey);
        if (legacyPlaintext is not null)
        {
            _logger.LogWarning(
                "Institution SMTP configuration {InstitutionEmailConfigId} still uses legacy plaintext password storage and should be resaved.",
                configuration.Id);
            return legacyPlaintext;
        }

        var ciphertext = CleanDatabaseValue(configuration.SmtpPasswordEncrypted);
        if (ciphertext is null)
        {
            return null;
        }

        try
        {
            var institutionEncryptionKey = await _institutionKeyResolver.ResolveAsync(configuration.InstitutionId);
            return CleanDatabaseValue(_encryptionService.Decrypt(ciphertext, institutionEncryptionKey));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to decrypt the Institution SMTP password for configuration {InstitutionEmailConfigId}.",
                configuration.Id);
            return null;
        }
    }

    private static Dictionary<string, string> BuildPlaceholders(
        ResolvedSmtpConfiguration configuration,
        IReadOnlyDictionary<string, string> suppliedPlaceholders)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["InstitutionName"] = configuration.InstitutionContext.InstitutionName,
            ["InstitutionLogoUrl"] = configuration.InstitutionContext.InstitutionLogoUrl,
            ["SupportEmail"] = string.IsNullOrWhiteSpace(configuration.InstitutionContext.SupportEmail)
                ? configuration.FromEmail
                : configuration.InstitutionContext.SupportEmail,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        foreach (var placeholder in suppliedPlaceholders)
        {
            placeholders[placeholder.Key] = placeholder.Value;
        }

        return placeholders;
    }

    private static string? CleanDatabaseValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleanedValue = value.Trim().Trim('\u200B', '\uFEFF');
        return string.IsNullOrWhiteSpace(cleanedValue) ? null : cleanedValue;
    }

    public static async Task CheckSmtpPorts()
    {
        const string host = "smtp-relay.brevo.com";
        int[] ports = [25, 465, 587, 2525];

        foreach (var port in ports)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);

                if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
                {
                    Console.WriteLine($"✅ {host}:{port} OPEN");
                }
                else
                {
                    Console.WriteLine($"❌ {host}:{port} BLOCKED");
                }
            }
            catch
            {
                Console.WriteLine($"❌ {host}:{port} FAILED");
            }
        }
    }

    private sealed record ResolvedSmtpConfiguration(
        string SmtpHost,
        int SmtpPort,
        string SmtpUsername,
        string SmtpSecret,
        string FromName,
        string FromEmail,
        InstitutionEmailTemplateContext InstitutionContext,
        string Source);

    private sealed record ResolvedEmailTemplate(
        string Subject,
        string Body);

    private sealed record InstitutionEmailTemplateContext(
        string InstitutionName,
        string InstitutionLogoUrl,
        string SupportEmail)
    {
        public static InstitutionEmailTemplateContext From(InstitutionEmailConfig? emailConfiguration)
        {
            var institution = emailConfiguration?.Institution;
            return new InstitutionEmailTemplateContext(
                institution?.CompanyName ?? string.Empty,
                institution?.InstitutionProfile
                    .Select(profile => profile.LogoUrl)
                    .FirstOrDefault() ?? FallbackLogoUrl,
                institution?.InstitutionEmail ?? string.Empty);
        }
    }
}
