using skolenza.application.Interfaces.ILogger;
using skolenza.application.Mappings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace skolenza.application
{
    public static class ServiceExtentions
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddScoped<Common.Helpers.BulkImportPreviewService>();
            services.AddScoped<Common.Helpers.BulkImportWorkflowService>();
            services.AddScoped<Common.Helpers.HostBulkImportWorkflowService>();
            // Registration the services
            //
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddMediatR(conf => conf.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddTransient( typeof(MediatR.IPipelineBehavior<,>),typeof(Features.StaffMemberCmd.StaffMemberInstitutionPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.StudentCmd.StudentPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DepartmentCmd.DepartmentPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.StaffEmploymentTypeCmd.StaffEmploymentTypePermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.GenericPolicyCmd.GenericPolicyPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.HolidayCmd.HolidayPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.RoleCmd.RolePermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.InstitutionManagementCmd.InstitutionManagementPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.InstitutionConfigurationCmd.InstitutionLocationPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DefaultEmailConfigCmd.DefaultEmailConfigPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.InstitutionEmailConfigCmd.InstitutionEmailConfigPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.EmailTemplateCmd.EmailTemplatePermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.InstitutionEmailTemplateCmd.InstitutionEmailTemplatePermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.HostDeviceCmd.InstitutionDeviceConfigurationPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DeviceCommandCmd.DeviceCommandPermissionBehavior<,>));
            services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Features.DeviceCommandCmd.DeviceHttpsGatewaySecurityBehavior<,>));
           

            services.AddAutoMapper(typeof(MappingProfile).Assembly);
             //registration of fluent validation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            //services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviors<,>));

        }
    }
}
