using skolenza.application.Interfaces.ICacheService;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IQRService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Common.Models;

using skolenza.infrastructure.BackgroundJob;
using skolenza.infrastructure.CacheMemory;
using skolenza.infrastructure.CommonRequest;
using skolenza.infrastructure.DeviceCommunication.Mqtt;
using skolenza.infrastructure.EncryptionService;
using skolenza.infrastructure.FileStoringService;
using skolenza.infrastructure.Logging;
using skolenza.infrastructure.MailService;
using skolenza.infrastructure.Permission;
using skolenza.infrastructure.QRServies;
using skolenza.infrastructure.Security.HashedService;
using skolenza.infrastructure.Token;
using skolenza.persistance.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
 

namespace skolenza.infrastructure
{
    public static class ServiceExtentions
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<StaffMemberMonitoringStorageOptions>()
                .Bind(configuration.GetSection(StaffMemberMonitoringStorageOptions.SectionName))
                .Validate(value => string.Equals(value.Provider, "FileSystem", StringComparison.OrdinalIgnoreCase),
                    "StaffMemberMonitoringStorage:Provider must be FileSystem for the configured adapter.")
                .Validate(value => !string.IsNullOrWhiteSpace(value.RootPath),
                    "StaffMemberMonitoringStorage:RootPath is required.")
                .Validate(value => value.MaximumUploadBytes > 0,
                    "StaffMemberMonitoringStorage:MaximumUploadBytes must be positive.")
                .Validate(value => value.AllowedContentTypes.Length > 0,
                    "StaffMemberMonitoringStorage:AllowedContentTypes is required.")
                .ValidateOnStart();
            services.AddOptions<skolenza.application.Common.Models.BulkImportOptions>()
                .Bind(configuration.GetSection(skolenza.application.Common.Models.BulkImportOptions.SectionName))
                .Validate(value => value.PollIntervalSeconds is >= 1 and <= 300,
                    "BulkImport:PollIntervalSeconds must be between 1 and 300.")
                .Validate(value => value.BatchSize is >= 1 and <= 200,
                    "BulkImport:BatchSize must be between 1 and 200.")
                .Validate(value => value.BatchTimeoutSeconds is >= 5 and <= 300,
                    "BulkImport:BatchTimeoutSeconds must be between 5 and 300.")
                .ValidateOnStart();
            services.AddOptions<skolenza.application.Common.Models.BillingOptions>()
                .Bind(configuration.GetSection(skolenza.application.Common.Models.BillingOptions.SectionName))
                .Validate(value => value.PaymentRetryCount is >= 1 and <= 10,
                    "Billing:PaymentRetryCount must be between 1 and 10.")
                .Validate(value => value.GracePeriodDays is >= 0 and <= 30,
                    "Billing:GracePeriodDays must be between 0 and 30.")
                .Validate(value => value.WebhookProcessingRetryCount is >= 1 and <= 20,
                    "Billing:WebhookProcessingRetryCount must be between 1 and 20.")
                .Validate(value => value.ReconciliationLookbackDays is >= 1 and <= 90,
                    "Billing:ReconciliationLookbackDays must be between 1 and 90.")
                .Validate(value => value.PaymentTimeoutMinutes is >= 5 and <= 1440,
                    "Billing:PaymentTimeoutMinutes must be between 5 and 1440.")
                .ValidateOnStart();
            services.AddHostedService<BulkImportWorker>();
            services.AddHostedService<EmailQueueWorker>();
            // Register background service
             services.AddHostedService<CommonBackgroundService>();  // ✅ This is mandatory
             services.AddMemoryCache();

            // One shared broker client serves every Institution and device. Broker credentials are
            // bound from secure application/environment configuration, not Institution settings.
            services.AddOptions<SkolenzaMqttOptions>()
                .Bind(configuration.GetSection(SkolenzaMqttOptions.SectionName));
            services.AddSingleton<DeviceMqttMessageRouter>();
            services.AddSingleton<SkolenzaMqttClient>();
            services.AddSingleton<ISkolenzaMqttPublisher>(serviceProvider =>
                serviceProvider.GetRequiredService<SkolenzaMqttClient>());
            services.AddScoped<IDeviceCommandManualDispatcher, DeviceCommandManualDispatcher>();
            services.AddHostedService<SkolenzaMqttHostedService>();
            services.AddHostedService<DeviceCommandDispatcherWorker>();

            // Register repositories & services

            // Register repositories & services
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IInstitutionEmailConfigRepository, InstitutionEmailConfigRepository>();
            services.AddScoped<IQRService, QRService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IStaffMemberScreenshotStorage, StaffMemberScreenshotFileStorage>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ICommonRequestService, CommonRequestService>();
            

            // ✅ Add missing repository registration
            services.AddScoped<IPermissionRepository, PermissionRepository>();

            // Register permission + encryption
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();

            services.AddScoped<IEncryptionService, AesEncryptionService>();
            services.AddScoped<IIdEncoderService, IdEncoderService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IInstitutionKeyCache, InstitutionKeyMemoryCache>();
            services.AddScoped<IInstitutionKeyResolver, InstitutionKeyResolver>();

        }
    }


}
