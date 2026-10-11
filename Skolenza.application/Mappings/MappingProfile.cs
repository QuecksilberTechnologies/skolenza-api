// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Configures application DTO-to-domain mapping rules.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Client;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.DefaultEmailConfig;
using skolenza.application.DTOs.Designation;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.Staff.AccessControlReadOnlyType;
using skolenza.application.DTOs.Gender;
using skolenza.application.DTOs.Leave;
using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOs.ModuleOperation;
using skolenza.application.DTOs.Operation;
using skolenza.application.DTOs.Holiday;
using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.InstitutionType;
using skolenza.application.DTOs.Transport;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.DTOS.Department;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Gender;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Location;
using skolenza.application.DTOS.Module.CommonModule;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.DTOS.Role;
using skolenza.application.DTOS.SubscriptionModule;


//using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.DTOS.UserRoles;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.DTOS.Configruations;
using skolenza.application.Features.TickeAllCmd.Classification;
using skolenza.domain.Entity;
using FluentValidation;


namespace skolenza.application.Mappings
{
    public class MappingProfile : Profile
    {
        private static string CleanSkillSet(string skillSet)
        {
            if (string.IsNullOrEmpty(skillSet))
                return string.Empty;

            return string.Join(",", skillSet.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
            );
        }

        private static string StaffMemberName(StaffMember? staffMember) =>
            string.Join(' ', new[] { staffMember?.FirstName, staffMember?.MiddleName, staffMember?.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));


        public MappingProfile()
        {
            CreateMap<DefaultEmailConfig, DefaultEmailConfigResponseDTO>()
                .ForMember(destination => destination.HasSmtpPassword,
                    options => options.MapFrom(source =>
                        !string.IsNullOrWhiteSpace(source.SecrateKey) ||
                        !string.IsNullOrWhiteSpace(source.SmtpPasswordEncrypted)));

            CreateMap<InstitutionEmailConfig, InstitutionEmailConfigResponseDTO>()
                .ForMember(destination => destination.InstitutionId, options => options.Ignore())
                .ForMember(destination => destination.InstitutionName,
                    options => options.MapFrom(source => source.Institution != null ? source.Institution.CompanyName : null))
                .ForMember(destination => destination.HasSmtpPassword,
                    options => options.MapFrom(source =>
                        !string.IsNullOrWhiteSpace(source.SecrateKey) ||
                        !string.IsNullOrWhiteSpace(source.SmtpPasswordEncrypted)));


            #region Host Device Management Mappings

            CreateMap<CreateDeviceMasterRequestDTO, DeviceMaster>()
                .ForMember(
                    d => d.DeviceType,
                    o => o.MapFrom(s => (short)s.DeviceType))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore())
                .ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore())
                .ForMember(d => d.InstitutionDevice, o => o.Ignore());

            CreateMap<UpdateDeviceMasterRequestDTO, DeviceMaster>()
                .ForMember(
                    d => d.DeviceType,
                    o => o.MapFrom(s => (short)s.DeviceType))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore())
                .ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore())
                .ForMember(d => d.InstitutionDevice, o => o.Ignore());

            CreateMap<DeviceMaster, DeviceMasterResponseDTO>()
                .ForMember(
                    d => d.DeviceType,
                    o => o.MapFrom(s => (DeviceType)s.DeviceType))
                .ForMember(
                    d => d.DeviceTypeName,
                    o => o.MapFrom(s => ((DeviceType)s.DeviceType).ToString()));

            CreateMap<CreateInstitutionDeviceRequestDTO, InstitutionDevice>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore())
                .ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore())
                .ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.Campus, o => o.Ignore())
                .ForMember(d => d.DeviceMaster, o => o.Ignore())
                .ForMember(d => d.InstitutionDeviceConfiguration, o => o.Ignore());

            CreateMap<UpdateInstitutionDeviceRequestDTO, InstitutionDevice>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore())
                .ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore())
                .ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.Campus, o => o.Ignore())
                .ForMember(d => d.DeviceMaster, o => o.Ignore())
                .ForMember(d => d.InstitutionDeviceConfiguration, o => o.Ignore());

            CreateMap<InstitutionDevice, InstitutionDeviceResponseDTO>()
                .ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(
                    d => d.InstitutionName,
                    o => o.MapFrom(s =>
                        s.Institution != null
                            ? s.Institution.CompanyName
                            : null))
                .ForMember(
                    d => d.CampusName,
                    o => o.MapFrom(s =>
                        s.Campus != null
                            ? s.Campus.LocationName
                            : null))
                .ForMember(
                    d => d.LocationCode,
                    o => o.MapFrom(s =>
                        s.Campus != null
                            ? s.Campus.LocationCode
                            : null))
                .ForMember(
                    d => d.DeviceMasterName,
                    o => o.MapFrom(s =>
                        s.DeviceMaster != null
                            ? s.DeviceMaster.DeviceName
                            : null))
                .ForMember(
                    d => d.DeviceMasterModelNo,
                    o => o.MapFrom(s =>
                        s.DeviceMaster != null
                            ? s.DeviceMaster.ModelNo
                            : null))
                .ForMember(d => d.HasConfiguration, o => o.MapFrom(s => s.InstitutionDeviceConfiguration != null));

            CreateMap<CreateInstitutionDeviceConfigurationRequestDTO, InstitutionDeviceConfiguration>()
                .ForMember(d => d.InstitutionDeviceId, o => o.Ignore())
                .ForMember(d => d.MqttTransport, o => o.MapFrom(s =>
                    s.MqttTransport.HasValue &&
                    (s.MqttTransport.Value == DeviceCommunicationProtocol.Mqtt ||
                     s.MqttTransport.Value == DeviceCommunicationProtocol.Mqtts)
                        ? (short?)s.MqttTransport.Value : null))
                .ForMember(d => d.CommandTransport, o => o.MapFrom(s =>
                    s.CommandTransport.HasValue
                        ? (short?)s.CommandTransport.Value
                        : s.MqttTransport.HasValue ? (short?)s.MqttTransport.Value : null))
                .ForMember(d => d.HttpsIngressTokenHash, o => o.Ignore())
                .ForMember(d => d.PendingHttpsIngressTokenHash, o => o.Ignore())
                .ForMember(d => d.PendingHttpsIngressTokenExpiresDateTime, o => o.Ignore())
                .ForMember(d => d.CommunicationType, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.LastHeartbeatDateTime, o => o.Ignore())
                .ForMember(d => d.LastSyncDateTime, o => o.Ignore())
                .ForMember(d => d.LastAttendanceReceivedDateTime, o => o.Ignore())
                .ForMember(d => d.LastSuccessfulConnectionDateTime, o => o.Ignore())
                .ForMember(d => d.LastFailedConnectionDateTime, o => o.Ignore())
                .ForMember(d => d.LastConnectionError, o => o.Ignore())
                .ForMember(d => d.InstitutionDevice, o => o.Ignore());

            CreateMap<UpdateInstitutionDeviceConfigurationRequestDTO, InstitutionDeviceConfiguration>()
                .ForMember(d => d.InstitutionDeviceId, o => o.Ignore())
                .ForMember(d => d.MqttTransport, o => o.MapFrom(s =>
                    s.MqttTransport.HasValue &&
                    (s.MqttTransport.Value == DeviceCommunicationProtocol.Mqtt ||
                     s.MqttTransport.Value == DeviceCommunicationProtocol.Mqtts)
                        ? (short?)s.MqttTransport.Value : null))
                .ForMember(d => d.CommandTransport, o => o.MapFrom(s =>
                    s.CommandTransport.HasValue
                        ? (short?)s.CommandTransport.Value
                        : s.MqttTransport.HasValue ? (short?)s.MqttTransport.Value : null))
                .ForMember(d => d.HttpsIngressTokenHash, o => o.Ignore())
                .ForMember(d => d.PendingHttpsIngressTokenHash, o => o.Ignore())
                .ForMember(d => d.PendingHttpsIngressTokenExpiresDateTime, o => o.Ignore())
                .ForMember(d => d.CommunicationType, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore())
                .ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore())
                .ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.LastHeartbeatDateTime, o => o.Ignore())
                .ForMember(d => d.LastSyncDateTime, o => o.Ignore())
                .ForMember(d => d.LastAttendanceReceivedDateTime, o => o.Ignore())
                .ForMember(d => d.LastSuccessfulConnectionDateTime, o => o.Ignore())
                .ForMember(d => d.LastFailedConnectionDateTime, o => o.Ignore())
                .ForMember(d => d.LastConnectionError, o => o.Ignore())
                .ForMember(d => d.InstitutionDevice, o => o.Ignore());

            CreateMap<InstitutionDeviceConfiguration, InstitutionDeviceConfigurationResponseDTO>()
                .ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.MqttTransport, o => o.MapFrom(s => s.MqttTransport.HasValue ? (DeviceCommunicationProtocol?)s.MqttTransport.Value : null))
                .ForMember(d => d.CommandTransport, o => o.MapFrom(s =>
                    s.CommandTransport.HasValue
                        ? (DeviceCommunicationProtocol?)s.CommandTransport.Value
                        : s.MqttTransport.HasValue ? (DeviceCommunicationProtocol?)s.MqttTransport.Value : null))
                .ForMember(d => d.DeviceCode, o => o.MapFrom(s => s.InstitutionDevice != null ? s.InstitutionDevice.DeviceCode : null))
                .ForMember(d => d.DeviceName, o => o.MapFrom(s => s.InstitutionDevice != null ? s.InstitutionDevice.DeviceName : null))
                .ForMember(d => d.DeviceMasterName, o => o.MapFrom(s => s.InstitutionDevice != null && s.InstitutionDevice.DeviceMaster != null ? s.InstitutionDevice.DeviceMaster.DeviceName : null))
                .ForMember(d => d.DeviceMasterSNo, o => o.MapFrom(s => s.InstitutionDevice != null && s.InstitutionDevice.DeviceMaster != null ? s.InstitutionDevice.DeviceMaster.SNo : null));

            #endregion

            #region Institution Configuration Mappings

            CreateMap<CreateCampusRequestDTO, Campus>()
                .ForMember(d => d.LocationType, o => o.MapFrom(s => checked((short)s.LocationType)))
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.Country, o => o.Ignore()).ForMember(d => d.Locality, o => o.Ignore())
                .ForMember(d => d.InstitutionDevice, o => o.Ignore()).ForMember(d => d.StaffMemberLocationAssignment, o => o.Ignore())
                .ForMember(d => d.StaffMemberWorkArrangement, o => o.Ignore()).ForMember(d => d.StaffMemberWorkPattern, o => o.Ignore())
                .ForMember(d => d.StaffMemberWorkModeOverrideRequest, o => o.Ignore());
            CreateMap<UpdateCampusRequestDTO, Campus>()
                .IncludeBase<CreateCampusRequestDTO, Campus>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateAttendancePolicyRequestDTO, AttendancePolicy>()
                .ForMember(d => d.AttendanceLocationScope, o => o.MapFrom(s => checked((short)s.AttendanceLocationScope)))
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.PolicyType, o => o.Ignore()).ForMember(d => d.StaffMemberWorkArrangement, o => o.Ignore());
            CreateMap<UpdateAttendancePolicyRequestDTO, AttendancePolicy>()
                .IncludeBase<CreateAttendancePolicyRequestDTO, AttendancePolicy>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateStaffMemberLocationAssignmentRequestDTO, StaffMemberLocationAssignment>()
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.StaffMember, o => o.Ignore()).ForMember(d => d.Campus, o => o.Ignore());
            CreateMap<UpdateStaffMemberLocationAssignmentRequestDTO, StaffMemberLocationAssignment>()
                .IncludeBase<CreateStaffMemberLocationAssignmentRequestDTO, StaffMemberLocationAssignment>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateStaffMemberDeviceEnrollmentRequestDTO, StaffMemberDeviceEnrollment>()
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.InstitutionDeviceId, o => o.Ignore())
                .ForMember(d => d.CampusId, o => o.Ignore())
                .ForMember(d => d.InstitutionCardMasterId, o => o.Ignore())
                .ForMember(d => d.EnrollId, o => o.Ignore())
                .ForMember(d => d.CardNumber, o => o.Ignore())
                .ForMember(d => d.FaceImageHash, o => o.Ignore())
                .ForMember(d => d.IsFaceEnrolled, o => o.Ignore())
                .ForMember(d => d.IsFingerprintEnrolled, o => o.Ignore())
                .ForMember(d => d.IsCardEnrolled, o => o.Ignore())
                .ForMember(d => d.FaceDeploymentStatus, o => o.Ignore())
                .ForMember(d => d.CardDeploymentStatus, o => o.Ignore())
                .ForMember(d => d.PinDeploymentStatus, o => o.Ignore())
                .ForMember(d => d.FaceDeviceCommandId, o => o.Ignore())
                .ForMember(d => d.CardDeviceCommandId, o => o.Ignore())
                .ForMember(d => d.PinDeviceCommandId, o => o.Ignore())
                .ForMember(d => d.UserActivationDeviceCommandId, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.LastSyncedDateTime, o => o.Ignore()).ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.StaffMember, o => o.Ignore()).ForMember(d => d.InstitutionDevice, o => o.Ignore())
                .ForMember(d => d.Campus, o => o.Ignore()).ForMember(d => d.InstitutionCardMaster, o => o.Ignore())
                .ForMember(d => d.FaceDeviceCommand, o => o.Ignore()).ForMember(d => d.CardDeviceCommand, o => o.Ignore()).ForMember(d => d.PinDeviceCommand, o => o.Ignore()).ForMember(d => d.UserActivationDeviceCommand, o => o.Ignore())
                .ForMember(d => d.StaffMemberDeviceAccessWindows, o => o.Ignore());
            CreateMap<UpdateStaffMemberDeviceEnrollmentRequestDTO, StaffMemberDeviceEnrollment>()
                .IncludeBase<CreateStaffMemberDeviceEnrollmentRequestDTO, StaffMemberDeviceEnrollment>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateStaffMemberWorkArrangementRequestDTO, StaffMemberWorkArrangement>()
                .ForMember(d => d.WorkMode, o => o.MapFrom(s => checked((short)s.WorkMode)))
                .ForMember(d => d.HybridType, o => o.MapFrom(s => s.HybridType.HasValue ? checked((short)s.HybridType.Value) : (short?)null))
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.StaffMember, o => o.Ignore()).ForMember(d => d.AttendancePolicy, o => o.Ignore())
                .ForMember(d => d.AttendancePolicyId, o => o.Ignore())
                .ForMember(d => d.PolicyVersion, o => o.Ignore())
                .ForMember(d => d.PrimaryCampus, o => o.Ignore()).ForMember(d => d.StaffMemberWorkPattern, o => o.Ignore())
                .ForMember(d => d.StaffMemberWorkModeOverrideRequest, o => o.Ignore());
            CreateMap<UpdateStaffMemberWorkArrangementRequestDTO, StaffMemberWorkArrangement>()
                .IncludeBase<CreateStaffMemberWorkArrangementRequestDTO, StaffMemberWorkArrangement>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateStaffMemberWorkPatternRequestDTO, StaffMemberWorkPattern>()
                .ForMember(d => d.DayOfWeek, o => o.MapFrom(s => checked((short)s.DayOfWeek)))
                .ForMember(d => d.WorkMode, o => o.MapFrom(s => checked((short)s.WorkMode)))
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.StaffMemberWorkArrangement, o => o.Ignore()).ForMember(d => d.Campus, o => o.Ignore());
            CreateMap<UpdateStaffMemberWorkPatternRequestDTO, StaffMemberWorkPattern>()
                .IncludeBase<CreateStaffMemberWorkPatternRequestDTO, StaffMemberWorkPattern>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CreateStaffMemberWorkModeOverrideRequestDTO, StaffMemberWorkModeOverrideRequest>()
                .ForMember(d => d.RequestedWorkMode, o => o.MapFrom(s => checked((short)s.RequestedWorkMode)))
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.Id, o => o.Ignore()).ForMember(d => d.InstitutionId, o => o.Ignore())
                .ForMember(d => d.ApprovalStatus, o => o.Ignore()).ForMember(d => d.ApprovedById, o => o.Ignore()).ForMember(d => d.ApprovedDateTime, o => o.Ignore())
                .ForMember(d => d.ApprovalRemark, o => o.Ignore()).ForMember(d => d.RejectedById, o => o.Ignore()).ForMember(d => d.RejectedDateTime, o => o.Ignore()).ForMember(d => d.RejectionRemark, o => o.Ignore())
                .ForMember(d => d.AddedById, o => o.Ignore()).ForMember(d => d.AddedDateTime, o => o.Ignore())
                .ForMember(d => d.UpdatedById, o => o.Ignore()).ForMember(d => d.UpdatedDateTime, o => o.Ignore())
                .ForMember(d => d.SoftDeletedById, o => o.Ignore()).ForMember(d => d.SoftDeletedDateTime, o => o.Ignore())
                .ForMember(d => d.IsSoftDeleted, o => o.Ignore()).ForMember(d => d.Institution, o => o.Ignore())
                .ForMember(d => d.StaffMember, o => o.Ignore()).ForMember(d => d.StaffMemberWorkArrangement, o => o.Ignore())
                .ForMember(d => d.Campus, o => o.Ignore());
            CreateMap<UpdateStaffMemberWorkModeOverrideRequestDTO, StaffMemberWorkModeOverrideRequest>()
                .IncludeBase<CreateStaffMemberWorkModeOverrideRequestDTO, StaffMemberWorkModeOverrideRequest>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<Campus, CampusResponseDTO>()
                .ForMember(d => d.LocationType, o => o.MapFrom(s => (CampusType)s.LocationType))
                .ForMember(d => d.LocationTypeName, o => o.MapFrom(s => ((CampusType)s.LocationType).ToString()))
                .ForMember(d => d.CountryName, o => o.MapFrom(s => s.Country != null ? s.Country.CountryName : string.Empty))
                .ForMember(d => d.StateName, o => o.MapFrom(_ => (string?)null))
                .ForMember(d => d.DistrictName, o => o.MapFrom(s => s.District != null ? s.District.DistrictName : null))
                .ForMember(d => d.LocalityName, o => o.MapFrom(s => s.Locality != null ? s.Locality.LocalityName : null));
            CreateMap<AttendancePolicy, AttendancePolicyResponseDTO>()
                .ForMember(d => d.AttendanceLocationScope, o => o.MapFrom(s => (AttendanceLocationScope)s.AttendanceLocationScope))
                .ForMember(d => d.PolicyTypeName, o => o.MapFrom(s => s.PolicyType != null ? s.PolicyType.PolicyName : string.Empty))
                .ForMember(d => d.AttendanceLocationScopeName, o => o.MapFrom(s => ((AttendanceLocationScope)s.AttendanceLocationScope).ToString()));
            CreateMap<StaffMemberLocationAssignment, StaffMemberLocationAssignmentResponseDTO>()
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.StaffMemberName, o => o.MapFrom(s => StaffMemberName(s.StaffMember)))
                .ForMember(d => d.StaffMemberCode, o => o.MapFrom(s => s.StaffMember != null ? s.StaffMember.EmployementCode : null))
                .ForMember(d => d.CampusName, o => o.MapFrom(s => s.Campus != null ? s.Campus.LocationName : string.Empty))
                .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Campus != null ? s.Campus.LocationCode : string.Empty));
            CreateMap<StaffMemberDeviceEnrollment, StaffMemberDeviceEnrollmentResponseDTO>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.InstitutionDeviceId, o => o.Ignore())
                .ForMember(d => d.CampusId, o => o.Ignore())
                .ForMember(d => d.InstitutionCardId, o => o.Ignore())
                .ForMember(d => d.MaskedCardNumber, o => o.Ignore())
                .ForMember(d => d.StaffMemberName, o => o.MapFrom(s => StaffMemberName(s.StaffMember)))
                .ForMember(d => d.StaffMemberCode, o => o.MapFrom(s => s.StaffMember != null ? s.StaffMember.EmployementCode ?? string.Empty : string.Empty))
                .ForMember(d => d.DeviceCode, o => o.MapFrom(s => s.InstitutionDevice != null ? s.InstitutionDevice.DeviceCode : string.Empty))
                .ForMember(d => d.DeviceName, o => o.MapFrom(s => s.InstitutionDevice != null ? s.InstitutionDevice.DeviceName : null))
                .ForMember(d => d.CampusName, o => o.MapFrom(s => s.Campus != null ? s.Campus.LocationName : string.Empty))
                .ForMember(d => d.FaceDeploymentStatus, o => o.MapFrom(s => (DeviceCredentialDeploymentStatus)s.FaceDeploymentStatus))
                .ForMember(d => d.CardDeploymentStatus, o => o.MapFrom(s => (DeviceCredentialDeploymentStatus)s.CardDeploymentStatus))
                .ForMember(d => d.PinDeploymentStatus, o => o.MapFrom(s => (DeviceCredentialDeploymentStatus)s.PinDeploymentStatus))
                .ForMember(d => d.FaceCommandStatus, o => o.Ignore())
                .ForMember(d => d.CardCommandStatus, o => o.Ignore())
                .ForMember(d => d.PinCommandStatus, o => o.Ignore())
                .ForMember(d => d.UserActivationCommandStatus, o => o.Ignore())
                .ForMember(d => d.AccessWindows, o => o.MapFrom(s => s.StaffMemberDeviceAccessWindows));
            CreateMap<StaffMemberDeviceAccessWindow, StaffMemberDeviceAccessWindowResponseDTO>()
                .ForMember(d => d.DayOfWeek, o => o.MapFrom(s => (WorkPatternDay)s.DayOfWeek));
            CreateMap<StaffMemberWorkArrangement, StaffMemberWorkArrangementResponseDTO>()
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.WorkMode, o => o.MapFrom(s => (WorkMode)s.WorkMode))
                .ForMember(d => d.HybridType, o => o.MapFrom(s => s.HybridType.HasValue ? (HybridType?)s.HybridType.Value : null))
                .ForMember(d => d.StaffMemberName, o => o.MapFrom(s => StaffMemberName(s.StaffMember)))
                .ForMember(d => d.PolicyVersionId, o => o.MapFrom(s => s.PolicyVersionId ?? 0))
                .ForMember(d => d.PolicyId, o => o.MapFrom(s => s.PolicyVersion != null ? s.PolicyVersion.PolicyId : 0))
                .ForMember(d => d.AttendancePolicyName, o => o.MapFrom(s => s.PolicyVersion != null && s.PolicyVersion.Policy != null ? s.PolicyVersion.Policy.PolicyName : string.Empty))
                .ForMember(d => d.PrimaryCampusName, o => o.MapFrom(s => s.PrimaryCampus != null ? s.PrimaryCampus.LocationName : null))
                .ForMember(d => d.WorkModeName, o => o.MapFrom(s => ((WorkMode)s.WorkMode).ToString()))
                .ForMember(d => d.HybridTypeName, o => o.MapFrom(s => s.HybridType.HasValue ? ((HybridType)s.HybridType.Value).ToString() : null));
            CreateMap<StaffMemberWorkPattern, StaffMemberWorkPatternResponseDTO>()
                .ForMember(d => d.StaffMemberId, o => o.Ignore())
                .ForMember(d => d.StaffMemberName, o => o.MapFrom(s => StaffMemberName(s.StaffMemberWorkArrangement.StaffMember)))
                .ForMember(d => d.DayOfWeek, o => o.MapFrom(s => (WorkPatternDay)s.DayOfWeek))
                .ForMember(d => d.WorkMode, o => o.MapFrom(s => (WorkMode)s.WorkMode))
                .ForMember(d => d.DayOfWeekName, o => o.MapFrom(s => ((WorkPatternDay)s.DayOfWeek).ToString()))
                .ForMember(d => d.WorkModeName, o => o.MapFrom(s => ((WorkMode)s.WorkMode).ToString()))
                .ForMember(d => d.CampusName, o => o.MapFrom(s => s.Campus != null ? s.Campus.LocationName : null));
            CreateMap<StaffMemberWorkModeOverrideRequest, StaffMemberWorkModeOverrideResponseDTO>()
                .ForMember(d => d.RequestedWorkMode, o => o.MapFrom(s => (WorkMode)s.RequestedWorkMode))
                .ForMember(d => d.ApprovalStatus, o => o.MapFrom(s => (WorkModeOverrideApprovalStatus)s.ApprovalStatus))
                .ForMember(d => d.StaffMemberName, o => o.MapFrom(s => StaffMemberName(s.StaffMember)))
                .ForMember(d => d.RequestedWorkModeName, o => o.MapFrom(s => ((WorkMode)s.RequestedWorkMode).ToString()))
                .ForMember(d => d.CampusName, o => o.MapFrom(s => s.Campus != null ? s.Campus.LocationName : null))
                .ForMember(d => d.ApprovalStatusName, o => o.MapFrom(s => ((WorkModeOverrideApprovalStatus)s.ApprovalStatus).ToString()));

            #endregion

            #region New Login Mappings

            CreateMap<NewLoginBootstrapReadModel, NewLoginUserContextDTO>()
                .ForMember(destination => destination.StaffMemberId, options => options.Ignore())
                .ForMember(destination => destination.FullName, options => options.Ignore())
                .ForMember(destination => destination.ProfileImageUrl, options => options.Ignore())
                .ForMember(destination => destination.InstitutionId, options => options.Ignore())
                .ForMember(destination => destination.PrimaryRole, options => options.Ignore())
                .ForMember(destination => destination.SecondaryRoles, options => options.Ignore());

            CreateMap<Role, NewLoginRoleDTO>()
                .ForMember(destination => destination.RoleId, options => options.MapFrom(source => source.Id))
                .ForMember(destination => destination.RoleName, options => options.MapFrom(source => source.RoleName ?? string.Empty))
                .ForMember(destination => destination.RoleTypeId, options => options.MapFrom(source => source.RoleType))
                .ForMember(
                    destination => destination.RoleTypeName,
                    options => options.MapFrom(source => ConstantValues.GetRoleTypeDisplayName(source.RoleType)));

            #endregion


            CreateMap<AddAssetRequestDTO, Asset>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Qrcode, opt => opt.Ignore())
                .ForMember(dest => dest.AssetAssignment, opt => opt.Ignore())
                .ForMember(dest => dest.AssetImage, opt => opt.Ignore())
                .ForMember(dest => dest.AssetStatus, opt => opt.Ignore())
                .ForMember(dest => dest.AssetType, opt => opt.Ignore());

            CreateMap<AssetCategory, GetCategoryResponseDTO>().ReverseMap();
            CreateMap<AssetStatus, GetStatusResponseDTO>().ReverseMap();
            CreateMap<AssetCategory, GetCategoryResponseDTO>().ReverseMap();

            CreateMap<AddCategoryReqestDTO, AssetCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.AssetType, opt => opt.Ignore())
                .ForMember(dest => dest.Institution, opt => opt.Ignore());

            CreateMap<UpdateCategoryReqestDTO, AssetCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AssetType, opt => opt.Ignore())
                .ForMember(dest => dest.Institution, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<Gender, GetGenderResponseDTO>().ReverseMap();
            CreateMap<Gender, GetGenderOptionResponseDTO>().ReverseMap();

            // CreateMap<Asset, GetAllAssetWithDependentEntityDTO>();

            CreateMap<AddTicketTypeRequestDTO, TicketType>().ReverseMap();

            CreateMap<TicketType, GetTicketTypeResponseDTO>()

                // 🔹 Responsible Role
                .ForMember(dest => dest.ResponsibleRoleName,
                    opt => opt.MapFrom(src =>
                        src.ResponsibleRole != null
                            ? src.ResponsibleRole.RoleName
                            : null))

                // 🔹 Approval Role
                .ForMember(dest => dest.ApprovalRoleName,
                    opt => opt.MapFrom(src =>
                        src.ApprovalRole != null
                            ? src.ApprovalRole.RoleName
                            : null))

                // 🔹 Header
                .ForMember(dest => dest.TicketHeaderName,
                    opt => opt.MapFrom(src =>
                        src.TicketHeader != null
                            ? src.TicketHeader.HeaderName
                            : null));


            CreateMap<GetClassificationRequestDTO, TicketClassification>().ReverseMap();
            CreateMap<GetClassificationResponseDTO, TicketClassification>().ReverseMap();
            CreateMap<AddClassificationRequestDTO, TicketClassification>().ReverseMap();
            CreateMap<UpdateClassificationRequestDTO, TicketClassification>().ReverseMap();
            CreateMap<GetClassificationByIdQuery, TicketClassification>().ReverseMap();


            CreateMap<TicketHeader, GetHeaderResponseDTO>().ReverseMap();
            CreateMap<AddHeaderRequestDTO, TicketHeader>().ReverseMap();
            CreateMap<UpdateHeaderRequestDTO, TicketHeader>().ReverseMap();
            CreateMap<GetHeaderRequestDTO, TicketHeader>().ReverseMap();


            CreateMap<skolenza.domain.Entity.WorkflowStage, GetWorkflowStageResponseDTO>().ReverseMap();




            // -------------------------
            // GetAll Mapping (DTO ↔ Entity)
            // -------------------------

            // -------------------------
            // Create Mapping (Insert) - Ignore Identity column
            // -------------------------

            // -------------------------
            // Response Mapping (DTO ↔ Entity)
            CreateMap<AssetType, GetTypeResponseDTO>().ReverseMap();
            // .ForMember(dest => dest.AssetCategoryId, opt => opt.MapFrom(src => src.AssetCategoryId))
            //.ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            //CreateMap<AddTypeRequestDTO, AssetType>()
            //    .ForMember(dest => dest.Id, opt => opt.Ignore())
            //    .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<AddTypeRequestDTO, AssetType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore());

            //CreateMap<GetTypeResponseDTO, AssetType>()
            //    .ForMember(dest => dest.AssetCategoryId, opt => opt.MapFrom(src => src.AssetCategoryId))
            //    .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));


            // -------------------------
            // Update Mapping (DTO ↔ Entity)
            // -------------------------
            CreateMap<UpdateTypeRequestDTO, AssetType>()
                .ForMember(dest => dest.AssetCategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // -------------------------
            // Delete Mapping (DTO ↔ Entity)
            // -------------------------

            CreateMap<Asset, GetAssetResponseDTO>();
            //.ForMember(dest => dest.WarrantyExpiryDate,
            //           opt => opt.MapFrom(src => src.WarrantyExpiryDate.HasValue
            //                                       ? DateOnly.FromDateTime(src.WarrantyExpiryDate.Value)
            //                                       : default));



            CreateMap<CreateHostUserRequestDTO, HostUser>()
                .ForMember(destination => destination.AddedById, options => options.Ignore())
                .ForMember(destination => destination.AddedDateTime, options => options.Ignore())
                .ForMember(destination => destination.UpdatedById, options => options.Ignore())
                .ForMember(destination => destination.UpdatedDateTime, options => options.Ignore())
                .ForMember(destination => destination.DeletedById, options => options.Ignore())
                .ForMember(destination => destination.DeletedDateTime, options => options.Ignore())
                .ForMember(destination => destination.IsSoftDeleted, options => options.Ignore())
                .ReverseMap();
            CreateMap<CreateHostUserResponseDTO, HostUser>().ReverseMap();
            CreateMap<CreateHostRoleRequestDTO, HostRole>()
                .ForMember(destination => destination.AddedById, options => options.Ignore())
                .ForMember(destination => destination.AddedDateTime, options => options.Ignore())
                .ForMember(destination => destination.UpdatedById, options => options.Ignore())
                .ForMember(destination => destination.UpdatedDateTime, options => options.Ignore())
                .ForMember(destination => destination.DeletedById, options => options.Ignore())
                .ForMember(destination => destination.DeletedDateTime, options => options.Ignore())
                .ForMember(destination => destination.IsSoftDeleted, options => options.Ignore())
                .ReverseMap();
            CreateMap<CreateHostRoleResponseDTO, HostRole>().ReverseMap();
   

            CreateMap<CreateSubscriptionRequestDTO, SubscriptionPlan>()
                .ForMember(destination => destination.AddedById, options => options.Ignore())
                .ForMember(destination => destination.AddedDateTime, options => options.Ignore())
                .ForMember(destination => destination.UpdatedById, options => options.Ignore())
                .ForMember(destination => destination.UpdatedDateTime, options => options.Ignore())
                .ForMember(destination => destination.IsSoftDeleted, options => options.Ignore())
                .ForMember(destination => destination.DeletedById, options => options.Ignore())
                .ForMember(destination => destination.DeletedDateTime, options => options.Ignore())
                .ReverseMap();
            CreateMap<SubscriptionActivePlanDTO, SubscriptionPlan>().ReverseMap(); 
            CreateMap<UpdateSubscriptionRequestDTO, SubscriptionPlan>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.AddedById, options => options.Ignore())
                .ForMember(destination => destination.AddedDateTime, options => options.Ignore())
                .ForMember(destination => destination.UpdatedById, options => options.Ignore())
                .ForMember(destination => destination.UpdatedDateTime, options => options.Ignore())
                .ForMember(destination => destination.IsSoftDeleted, options => options.Ignore())
                .ForMember(destination => destination.DeletedById, options => options.Ignore())
                .ForMember(destination => destination.DeletedDateTime, options => options.Ignore())
                .ReverseMap();


            CreateMap<GetAssetResponseDTO, Asset>().ReverseMap();








            //   CreateMap<AssetType, AssetTypeResponseDTO>().ReverseMap(); // 🔥 Yeh zaroori hai




            CreateMap<CreateStatusRequestDTO, AssetStatus>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedDateTime, opt => opt.Ignore());

            CreateMap<UpdateStatusRequestDTO, AssetStatus>()
                .ForMember(dest => dest.InstitutionId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsSoftDeleted, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));







            CreateMap<CreateDesignationRequestDTO, Designation>().ReverseMap();
            CreateMap<Designation, GetDesignationRequestDTO>().ReverseMap();
            CreateMap<UpdateDesignationRequestDTO, Designation>().ReverseMap();
            CreateMap<GetDesignationResponseDTO, Designation>().ReverseMap();


            CreateMap<skolenza.application.DTOS.Staff.Type.CreateStaffEmploymentTypeDTO, StaffEmploymentType>();
            CreateMap<skolenza.application.DTOS.Staff.Type.UpdateStaffEmploymentTypeRequestDTO, StaffEmploymentType>();
            CreateMap<StaffEmploymentType, skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO>();
            CreateMap<CreateDepartmentRequestDTO, Department>().ReverseMap();
            CreateMap<Department, GetSingleDepartmentResponseDTO>().ReverseMap();
            CreateMap<Department, GetDepartmentRequestDTO>().ReverseMap();
            CreateMap<UpdateDepartmentRequestDTO, Department>().ReverseMap();



            CreateMap<GetParentModuleResponseDTO, domain.Entity.Module>().ReverseMap();

            CreateMap<CreateParentModuleRequestDTO, domain.Entity.Module>().ReverseMap();
            CreateMap<GetCommonModuleResponseDTO, domain.Entity.Module>().ReverseMap();

            CreateMap<CreateCommonModuleRequestDTO, domain.Entity.Module>().ReverseMap();



            CreateMap<domain.Entity.Module, CommonItemDTO>().ReverseMap();


            CreateMap<MainModuleResponseDTO, domain.Entity.Module>();
            CreateMap<domain.Entity.Module, MainModuleResponseDTO>();

            CreateMap<CreateSubModuleRequestDTO, domain.Entity.Module>();
            CreateMap<domain.Entity.Module, CreateSubModuleRequestDTO>();





            CreateMap<domain.Entity.Operation, CreateOperationRequestDTO>()
                .ForMember(dest => dest.ProductOwnerId, opt => opt.MapFrom(src => (long)src.AddedById));



            CreateMap<CreateOperationRequestDTO, domain.Entity.Operation>()
             .ForMember(dest => dest.AddedById, opt => opt.MapFrom(src => (long)src.ProductOwnerId));


            CreateMap<domain.Entity.Operation, CreateOperationRequestDTO>();


            CreateMap<domain.Entity.Operation, GetOperationResponseDTO>();
            CreateMap<GetOperationResponseDTO, domain.Entity.Operation>();


            CreateMap<UpdateOperationRequestDTO, domain.Entity.Operation>();


            CreateMap<domain.Entity.Operation, UpdateOperationRequestDTO>();
            // Create
           

            // Get
            CreateMap<CreateClientTypeDTO, ClientType>();
            CreateMap<ClientType, GetClientTypeDTO>();
            CreateMap<UpdateClientTypeDTO, ClientType>();  // ✅ Yeh likhna hoga!
            CreateMap<CreateTravelModeDTO, TravelMode>();
            CreateMap<TravelMode, GetAllTravelModeDTO>();
            CreateMap<UpdateTravelModeDTO, TravelMode>();
            CreateMap<CreateLeaveTypeRequestDTO, LeaveType>();

            CreateMap<LeaveType, GetLeaveTypResponseDTO>();
            CreateMap<UpdateLeaveTypeRequestDTO, LeaveType>();  // ✅ Yeh likhna hoga!


            CreateMap<Role, GetSingleRoleResponseDTO>().ReverseMap();
            CreateMap<Role, GetTicketTypeResponseDTO>().ReverseMap();


            // Audit fields are set from trusted request context in the handlers.
            CreateMap<CreateRoleRequestDTO, Role>();
            CreateMap<UpdateRoleRequestDTO, Role>();


            // Role Entity to GetAllRoleDTO Mapping
            // Direct entity to DTO mapping
            //   CreateMap<GetActiveRoleRequestDTO,Role >()

            //    .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));// Example

            CreateMap<Role, GetRoleResponseDTO>()
                .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id.ToString()))
                .ForMember(destination => destination.RoleType, options => options.MapFrom(source => source.RoleType.ToString()))
                .ForMember(
                    destination => destination.RoleTypeName,
                    options => options.MapFrom(source => ConstantValues.GetRoleTypeDisplayName(source.RoleType)))
                .ForMember(
                    destination => destination.Remark,
                    options => options.MapFrom(source =>
                        ConstantValues.GetRoleRemarkForDisplay(source.RoleType, source.Remark)))
                .ReverseMap()
                .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id))
                .ForMember(destination => destination.RoleType, options => options.MapFrom(source => source.RoleType))
                .ForMember(destination => destination.Remark, options => options.MapFrom(source => source.Remark));



            CreateMap<Department, GetDepartmentResponseDTO>();
            CreateMap<GetDepartmentResponseDTO, Department>();


            // CreateMap<RoleResponseDTO, Role>().ReverseMap();
            // CreateMap<GetRoleSummaryResponseDTO, Role>();
            //  CreateMap<Role, GetRoleSummaryResponseDTO>().ReverseMap();

            //   CreateMap<InstitutionCreateResponseDTO, InstitutionType>();
            CreateMap<InstitutionSubscriptionPlanResponseDTO, InstitutionSubscription>().ReverseMap();
            CreateMap<InstitutionType, InstitutionTypeResponseDTO>();



            CreateMap<UserRole, UserRoleDTO>()
                          .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName)) // Example
                          .ForMember(dest => dest.RoleType, opt => opt.MapFrom(src => src.Role.RoleType)) // Example
                          .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Role.Id)) // Example
                          .ForMember(dest => dest.UserRoleId, opt => opt.MapFrom(src => src.Id))  
                          .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive == true));

            // CreateRoleDTO to Role Mapping (for creating roles)


            #region 🔹 Bank Mappings

            #endregion

            #region 🔹 Education Mappings
            CreateMap<StaffMemberEducation, StaffMemberEducationEditableFieldsDTO>().ReverseMap();
            CreateMap<StaffMemberEducation, GetEducationResponseDTO>()
       .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
       .ForMember(dest => dest.StaffMemberId, opt => opt.MapFrom(src => src.StaffMemberId.ToString()))
       .ForMember(dest => dest.Degree, opt => opt.MapFrom(src => src.Degree))
       .ForMember(dest => dest.InstituteName, opt => opt.MapFrom(src => src.InstituteName))
       .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
       .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
       .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
       .ForMember(dest => dest.ScoreValue, opt => opt.MapFrom(src => src.ScoreValue))
       .ForMember(dest => dest.GradeDivision, opt => opt.MapFrom(src => src.GradeDivision))
       .ForMember(dest => dest.ScoreType, opt => opt.MapFrom(src => src.ScoreType.ToString()))
       .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
       .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType))
       .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
       .ForMember(dest => dest.EducationGap, opt => opt.MapFrom(src => src.EducationGap))
       .ForMember(dest => dest.IsEditAllowed, opt => opt.MapFrom(src => src.IsEditAllowed))
       .ForMember(dest => dest.GapYears, opt => opt.MapFrom(src => src.GapYears))
       .ForMember(dest => dest.IsInfoVerified, opt => opt.MapFrom(src => src.IsInfoVerified))
       .ForMember(dest => dest.ReasonOfEducationGap, opt => opt.MapFrom(src => src.ReasonOfEducationGap))
       .ForMember(dest => dest.InfoVerifiedById, opt => opt.MapFrom(src => src.InfoVerifiedById.ToString()))
       .ForMember(dest => dest.HasEducationDocUploded, opt => opt.MapFrom(src => src.HasEducationDocUploded))
       .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));



            CreateMap<StaffMemberEducation, GetEducationResponseDTO>().ReverseMap();

            CreateMap<CreateEducationRequestDTO, StaffMemberEducation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())

                .ForMember(dest => dest.StartDate,
                           opt => opt.MapFrom(src => src.StartDate.HasValue
                                ? DateOnly.FromDateTime(src.StartDate.Value)
                                : (DateOnly?)null))
                .ForMember(dest => dest.EndDate,
                           opt => opt.MapFrom(src => src.EndDate.HasValue
                                ? DateOnly.FromDateTime(src.EndDate.Value)
                                : (DateOnly?)null))

                // BOOL → BOOL
                .ForMember(dest => dest.EducationGap,
                           opt => opt.MapFrom(src => src.IsEducationGapBeforeDegree))
                // BOOL → BOOL
                .ForMember(dest => dest.GapYears,
                           opt => opt.MapFrom(src => src.GapYears))

                // ScoreValue → string
                .ForMember(dest => dest.ScoreValue,
                           opt => opt.MapFrom(src => src.ScoreValue != null
                                ? src.ScoreValue.ToString()
                                : null))

                // GradeDivision → string
                .ForMember(dest => dest.GradeDivision,
                           opt => opt.MapFrom(src => src.GradeDivision != null
                                ? src.GradeDivision.ToString()
                                : null))

                .ForMember(dest => dest.ScoreType,
                           opt => opt.MapFrom(src => int.Parse(src.ScoreType!.Trim())));



            #region 🔹 Education Mappings
            CreateMap<CreateStaffMemberImageRequestDTO, StaffMemberImage>()
     .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())   // ❗ ignore
     .ForMember(dest => dest.FilePath, opt => opt.Ignore())     // ❗ file path later set hoga
     .ForMember(dest => dest.FileName, opt => opt.Ignore())     // ❗ file name later set hoga
     .ForMember(dest => dest.HasImageUploaded, opt => opt.Ignore())
     .ForMember(dest => dest.IsPrimary, opt => opt.Ignore())
     .ForMember(dest => dest.InstitutionId, opt => opt.Ignore()).ReverseMap();

            #endregion
            CreateMap<UpdateBankReqestDTO, GetBankResponseDTO>().ReverseMap();
            CreateMap<UpdateBankReqestDTO, StaffMemberBankDetail>().ReverseMap();

            CreateMap<CreateBankRequestDTO, StaffMemberBankDetail>()
           .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())
            .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.AccountType))
           .ForMember(dest => dest.IsPrimaryAccount, opt => opt.MapFrom(src => src.IsPrimaryAccount))
            .ForMember(dest => dest.Upiid, opt => opt.MapFrom(src => src.UPIId));

            CreateMap<CreateContactRequestDTO, StaffMemberContact>()
    // ❌ StaffMemberId handler me set hoga
    .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())

    // 🔹 Contact Info
    .ForMember(dest => dest.ContactNumber, opt => opt.MapFrom(src => src.ContactNumber))
    .ForMember(dest => dest.AlternateNumber, opt => opt.MapFrom(src => src.AlternateNumber))
    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
    .ForMember(dest => dest.ContactName,
        opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.ContactName) ? null : src.ContactName))

     // 🔹 Address (string → int safe)
     .ForMember(dest => dest.Relation, opt => opt.MapFrom(src => src.Relation))
     .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src => src.CountryId))
     .ForMember(dest => dest.DistrictId, opt => opt.MapFrom(src => src.DistrictId))
     .ForMember(dest => dest.StateId, opt => opt.MapFrom(src => src.StateId))
     .ForMember(dest => dest.LocalityId, opt => opt.MapFrom(src => src.LocalityId))

    // 🔹 ContactType (ENUM → INT) ✅
    .ForMember(dest => dest.ContactType,
        opt => opt.MapFrom(src => (int)src.ContactType))

    // 🔹 IsPrimary (nullable safe)
    .ForMember(dest => dest.IsPrimary,
        opt => opt.MapFrom(src => src.IsPrimary))

    // 🔹 Address fields
    .ForMember(dest => dest.HouseNo, opt => opt.MapFrom(src => src.HouseNo))
    .ForMember(dest => dest.LandMark, opt => opt.MapFrom(src => src.LandMark))
    .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
    .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))

    // 🔹 Meta
    .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
    .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));



            // 🔁 ReverseMap (DTO → Entity)


            #endregion

            #region 🔹 Base StaffMember Mappings

            CreateMap<CreateBaseStaffMemberRequestDTO, StaffMember>();
            //.ForMember(dest => dest.CountryId,
            //      opt => opt.MapFrom(src => src.CountryId)).ReverseMap();


            CreateMap<StaffMember, GetBaseStaffMemberResponseDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)) // already int -> no need tostring
                .ForMember(dest => dest.EmployementCode, opt => opt.MapFrom(src => src.EmployementCode))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.GenderId, opt => opt.MapFrom(src => src.GenderId)) // int
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.DateOfOnBoarding, opt => opt.MapFrom(src => src.DateOfOnBoarding))
                .ForMember(dest => dest.DateOfExit, opt => opt.MapFrom(src => src.DateOfExit))
                .ForMember(dest => dest.DesignationId, opt => opt.MapFrom(src => src.DesignationId))  // int now
                .ForMember(dest => dest.StaffEmploymentTypeId, opt => opt.MapFrom(src => src.StaffEmploymentTypeId)) // int now
                .ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.DepartmentId)) // int now
                .ForMember(dest => dest.OfficialEmail, opt => opt.MapFrom(src => src.OfficialEmail))
                .ForMember(dest => dest.HasPermanent, opt => opt.MapFrom(src => src.HasPermanent))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsEditAllowed, opt => opt.MapFrom(src => src.IsEditAllowed))
                .ForMember(dest => dest.IsInfoVerified, opt => opt.MapFrom(src => src.IsInfoVerified));
              //  .ForMember(dest => dest.CountryId, opt => opt.MapFrom(src => src.CountryId));
            

            #endregion




            //.ForMember(dest => dest.DesignationId, opt => opt.MapFrom(src => src.DesignationId != null ? src.DesignationId.ToString() : null))
            //.ForMember(dest => dest.StaffEmploymentTypeId, opt => opt.MapFrom(src => src.StaffEmploymentTypeId != null ? src.StaffEmploymentTypeId.ToString() : null))
            //.ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.DepartmentId != null ? src.DepartmentId.ToString() : null))


            CreateMap<GetMinimalStaffMemberResponseDTO, CreateBaseStaffMemberRequestDTO>().ReverseMap();
            CreateMap<GetMinimalStaffMemberResponseDTO, GetBaseStaffMemberResponseDTO>().ReverseMap();
            CreateMap<GetMinimalStaffMemberResponseDTO, StaffMemberInfoEditableFieldsDTO>().ReverseMap();


            #region Dependent
            CreateMap<CreateDependentRequestDTO, StaffMemberDependent>()
.ForMember(dest => dest.StaffMemberId, opt => opt.Ignore()) // manually assign later
.ForMember(dest => dest.DependentName, opt => opt.MapFrom(src => src.DependentName))
.ForMember(dest => dest.Relation, opt => opt.MapFrom(src => src.Relation))
.ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
.ForMember(dest => dest.IsCoveredInPolicy, opt => opt.MapFrom(src => src.IsCoveredInPolicy))
.ForMember(dest => dest.IsMarried, opt => opt.MapFrom(src => src.IsMarried))
.ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
.ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            CreateMap<StaffMemberDependent, GetDependentResponseDTO>()
    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
    .ForMember(dest => dest.StaffMemberId, opt => opt.MapFrom(src => src.StaffMemberId.ToString()))
    .ForMember(dest => dest.DependentName, opt => opt.MapFrom(src => src.DependentName))
    .ForMember(dest => dest.Relation, opt => opt.MapFrom(src => src.Relation))
    .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
    .ForMember(dest => dest.IsCoveredInPolicy, opt => opt.MapFrom(src => src.IsCoveredInPolicy))
    .ForMember(dest => dest.IsMarried, opt => opt.MapFrom(src => src.IsMarried))
    .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
    .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
     .ForMember(dest => dest.InfoVerifiedById, opt => opt.MapFrom(src => src.InfoVerifiedById.HasValue ? src.InfoVerifiedById.Value.ToString() : null))
    .ForMember(dest => dest.IsInfoVerified, opt => opt.MapFrom(src => src.IsInfoVerified))
    .ForMember(dest => dest.InfoVerifiedDateTime, opt => opt.MapFrom(src => src.InfoVerifiedDateTime))
    .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
    .ForMember(dest => dest.HasProofUploaded, opt => opt.MapFrom(src => src.HasProofUploaded));


            #endregion

            #region 🔹 Contact Mappings
            CreateMap<StaffMemberContact, StaffMemberContactEditableFieldDTO>().ReverseMap();
            CreateMap<GetContactResponseDTO, StaffMemberContact>().ReverseMap();
            #endregion

            #region 🔹 Personal Detail Mappings
            //     CreateMap<StaffMemberPersonalDetail, CreateIdentityRequestDTO>()
            //.ForMember(dest => dest.UserStaffMemberId, opt => opt.Ignore())
            //.ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())
            //.ForMember(dest => dest.AadhaarDocFile, opt => opt.Ignore())
            //.ForMember(dest => dest.PanDocFile, opt => opt.Ignore())
            //.ForMember(dest => dest.PassportDocFile, opt => opt.Ignore());

            //CreateMap<CreateIdentityRequestDTO, IdentityCategory>()
            //    .ForMember(dest => dest.Id, opt => opt.Ignore())  // Primary key
            //    .ForMember(dest => dest.AddedById, opt => opt.Ignore())
            //    .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
            //    .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
            //     .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore())
            //     .ForMember(dest => dest.SoftDeletedById, opt => opt.Ignore())


            //    .ForMember(dest => dest.IsActive, opt => opt.Ignore())


            //    .ForMember(dest => dest.HasEPFAccount, opt => opt.MapFrom(src => src.HasEPFAccount))
            //    .ForMember(dest => dest.UANNumber, opt => opt.MapFrom(src => src.UANNumber))
            //    .ForMember(dest => dest.AadhaarNumber, opt => opt.MapFrom(src => src.AadhaarNumber))
            //    .ForMember(dest => dest.PanNumber, opt => opt.MapFrom(src => src.PanNumber))
            //    .ForMember(dest => dest.PassportNumber, opt => opt.MapFrom(src => src.PassportNumber))
            //    .ForMember(dest => dest.DrivingLicenseNumber, opt => opt.MapFrom(src => src.DrivingLicenseNumber))
            //    .ForMember(dest => dest.VoterId, opt => opt.MapFrom(src => src.VoterId))
            //    .ForMember(dest => dest.BloodGroup, opt => opt.MapFrom(src => src.BloodGroup))
            //    .ForMember(dest => dest.MaritalStatus, opt => opt.MapFrom(src => src.MaritalStatus))
            //    .ForMember(dest => dest.Nationality, opt => opt.MapFrom(src => src.Nationality))
            //    .ForMember(dest => dest.EmergencyContactName, opt => opt.MapFrom(src => src.EmergencyContactName))
            //    .ForMember(dest => dest.EmergencyContactRelation, opt => opt.MapFrom(src => src.EmergencyContactRelation))
            //    .ForMember(dest => dest.EmergencyContactNumber, opt => opt.MapFrom(src => src.EmergencyContactNumber));

            #endregion

            #region 🔹 Experience Mappings

            CreateMap<CreateExperienceRequestDTO, StaffMemberExperience>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore())
                 .ForMember(dest => dest.StaffMemberId, opt => opt.Ignore()) // 🔥 set manually (decoded)
                 .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                 .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                 .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                 .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                 .ForMember(dest => dest.StaffMember, opt => opt.Ignore()) // navigation
                 .ForMember(dest => dest.StaffMemberExperienceDocument, opt => opt.Ignore()); // handle manually


            CreateMap<CreateExperienceDocumentDTO, StaffMemberExperienceDocument>()
                  .ForMember(dest => dest.Id, opt => opt.Ignore())
                  .ForMember(dest => dest.StaffMemberExperienceId, opt => opt.Ignore()) // 🔥 set by EF relation
                  .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                  .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                  .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                  .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                  .ForMember(dest => dest.StaffMemberExperience, opt => opt.Ignore()); // navigation
 
            #endregion

            #region 🔹 Login / Info Mappings

            CreateMap<GetStaffMemberLoginInfoResponseDTO, LoginResponseDTO>()
                .ForMember(dest => dest.StaffMemberInfo, opt => opt.MapFrom(src => src));
            CreateMap<GetMinimalStaffMemberResponseDTO, GetStaffMemberLoginInfoResponseDTO>()
                .ForMember(dest => dest.StaffMemberId, opt => opt.MapFrom(src => src.Id))  // now int
                .ForMember(dest => dest.StaffMemberFullName, opt => opt.MapFrom(src =>
                    $"{src.FirstName} {(string.IsNullOrWhiteSpace(src.MiddleName) ? "" : src.MiddleName + " ")}{src.LastName}".Trim()))
                .ForMember(dest => dest.StaffEmploymentTypeId, opt => opt.MapFrom(src => src.StaffEmploymentTypeId)) // now int?
                .ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.DepartmentId))
                .ForMember(dest => dest.DesignationId, opt => opt.MapFrom(src => src.DesignationId))

                .ForMember(dest => dest.OfficialEmail, opt => opt.MapFrom(src => src.OfficialEmail));

            #endregion

            // Agar reverse mapping chahiye toh, isse bhi add kar sakte hain
            CreateMap<GetReportingTypeResponseDTO, ReportingType>().ReverseMap();


            CreateMap<Holiday, HolidayDTO>();
            CreateMap<HolidayDTO, Holiday>();

            // `StaffMember.OfficialEmail` ➝ `StaffMemberInfoDTO.OfficialEmail`


            // Add mapping for List<Role> to List<GetAllRoleDTO>
            // CreateMap<List<Role>, List<GetAllRoleDTO>>();  // Add this line

            // CreateMap<List<Role>, List<GetAllRoleDTO>>()
            //.ForMember(dest => dest, opt => opt.MapFrom(src => src.Select(x => new GetAllRoleDTO { /* Manually map properties here */ }).ToList()));


            // Mapping StaffMember entity to LoginStaffMemberInfoDTO
            //CreateMap<StaffMember, StaffMemberInfoDTO>()
            //    .ForMember(dest => dest.StaffMemberId, opt => opt.MapFrom(src => src.Id)) // Map StaffMemberId
            //    .ForMember(dest => dest.LoginId, opt => opt.MapFrom((src, dest, _, context) => context.Itskolenza["LoginId"] as string)) // Map LoginId from context
            //                                                                                                                        //  .ForMember(dest => dest.StaffMemberFirstName, opt => opt.MapFrom(src => src.FirstName)) // Map UserName
            //                                                                                                                        //.ForMember(dest => dest.StaffMemberMiddleName, opt => opt.MapFrom(src => src.MiddleName)) // Map UserName
            //                                                                                                                        // .ForMember(dest => dest.StaffMemberLastName, opt => opt.MapFrom(src => src.LastName)) // Map UserName
            //    .ForMember(dest => dest.StaffMemberFullName, opt => opt.MapFrom(src => ((src.FirstName) + src.MiddleName) + src.LastName));
            //    // Map UserName
            //.ForMember(dest => dest.StaffEmploymentTypeId, opt => opt.MapFrom(src => src.StaffEmploymentTypeId.ToString())) // Map StaffEmploymentTypeId
            // .ForMember(dest => dest.StaffEmploymentType, opt => opt.MapFrom(src => src.EmployementType.ToString())) // Map StaffEmploymentTypeId
            // .ForMember(dest => dest.StaffEmploymentType, opt => opt.MapFrom(src => src.StaffEmploymentType.TypeName)) // Map StaffEmploymentType Name
            //.ForMember(dest => dest.StaffMemberAssignedRoles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => new RoleInfoDTO
            //{
            //    Id = ur.RoleId.GetValueOrDefault(),  // ✅ Fix: Converts nullable int? to int (Defaults to 0 if null)
            //    RoleName = ur.Role.RoleName,
            //    Description = ur.Remark
            //})));

            //CreateMap<EmailTemplate, EmailTemplateDTO>();
            CreateMap<EmailTemplate, EmailTemplateDTO>().ReverseMap();
            CreateMap<EmailTemplate, EmailTemplateResponseDTO>();
            CreateMap<InstitutionEmailTemplate, InstitutionEmailTemplateResponseDTO>();

            CreateMap<DTOs.Registration.InstitutionCreationRequestDTO, Institution>();
            CreateMap<Institution, DTOs.Registration.InstitutionCreateRequestDTO>().ReverseMap();
            CreateMap<LoginCredential, GetMinimalStaffMemberResponseDTO>().ReverseMap();

            CreateMap<Country, GetCountryOptionResponseDTO>().ReverseMap();
            CreateMap<State, GetStateOptionResponseDTO>().ReverseMap();
            CreateMap<District, GetDistrictOptionResponseDTO>().ReverseMap();


            CreateMap<Institution, DTOs.Institution.InstitutionRequestDTO>().ReverseMap();
            CreateMap<InstitutionResponseDTO, Institution>().ReverseMap();
            CreateMap<Institution, HostInstitutionResponseDTO>()
                .ForMember(destination => destination.Id, options => options.Ignore())
                .ForMember(destination => destination.StaffMemberId, options => options.Ignore());

            CreateMap<SubscriptionPlan, SubscriptionPlanResponseDTO>()
               .ForMember(dest => dest.Modules, opt => opt.MapFrom(src => src.PlanModuleMapping
               .Where(pmm => pmm.Module != null)
                .Select(pmm => new ModuleResponseDTO
                {
                    ModuleId = pmm.Module.Id,
                    ModuleName = pmm.Module.ModuleName,
                    ParentModuleId = pmm.Module.ParentModuleId
                }).ToList()));

            CreateMap<ModuleOperationMapping, InstitutionEnabledOperation>()
               .ForMember(dest => dest.Id, opt => opt.Ignore()) // IMPORTANT
.ForMember(dest => dest.ModuleId,
          opt => opt.MapFrom(src => src.ModuleId))

.ForMember(dest => dest.OperationId,
          opt => opt.MapFrom(src => src.OperationId))

.ForMember(dest => dest.IsEnabled,
          opt => opt.MapFrom(src => src.IsActive ?? true))

.ForMember(dest => dest.IsOperationUsed,
          opt => opt.MapFrom(src => true))

.ForMember(dest => dest.AddedById,
          opt => opt.MapFrom(src => src.AddedById))

.ForMember(dest => dest.AddedDateTime,
          opt => opt.MapFrom(src => src.AddedDateTime))

// ignore navigation properties
.ForMember(dest => dest.Module, opt => opt.Ignore())
.ForMember(dest => dest.Operation, opt => opt.Ignore())
.ForMember(dest => dest.Institution, opt => opt.Ignore());

            CreateMap<InstitutionEnabledOperationRequestDTO, ModuleOperationMapping>();

            CreateMap<InstitutionEnabledOperation, InstitutionEnabledOperationRequestDTO>();
            CreateMap<InstitutionEnabledOperationRequestDTO, InstitutionEnabledOperation>();







            CreateMap<OperationResponseDTO, InstitutionEnabledOperation>()
               .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => true))
              .ForMember(dest => dest.OperationId, opt => opt.MapFrom(src => src.OperationId)).ForAllMembers(opt => opt.Ignore());

            CreateMap<InstitutionSubscriptionPlanRequestDTO, InstitutionSubscription>().ReverseMap();
            CreateMap<InstitutionSubscriptionPlanResponseDTO, InstitutionSubscription>().ReverseMap();


            CreateMap<CreateModuleOperationRequestDTO, ModuleOperationMapping>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PageUrl, opt => opt.MapFrom(src => src.PageURL))
                .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => src.IconURL))
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Module, opt => opt.Ignore())
                .ForMember(dest => dest.Operation, opt => opt.Ignore())
                .ForMember(dest => dest.DataViewStructure, opt => opt.Ignore())
                .ForMember(dest => dest.PageType, opt => opt.Ignore());

            CreateMap<UpdateModuleOperationMappingByProductOwnerRequestDTO, ModuleOperationMapping>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PageUrl, opt => opt.MapFrom(src => src.PageURL))
                .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => src.IconURL))
                .ForMember(dest => dest.AddedById, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedById, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Module, opt => opt.Ignore())
                .ForMember(dest => dest.Operation, opt => opt.Ignore())
                .ForMember(dest => dest.DataViewStructure, opt => opt.Ignore())
                .ForMember(dest => dest.PageType, opt => opt.Ignore());

            CreateMap<ModuleOperationMapping, ModuleOperationMappingByProductOwnerResponseDTO>()
                .ForMember(dest => dest.PageURL, opt => opt.MapFrom(src => src.PageUrl))
                .ForMember(dest => dest.IconURL, opt => opt.MapFrom(src => src.IconUrl))
                .ForMember(dest => dest.ModuleName, opt => opt.MapFrom(src => src.Module != null ? src.Module.ModuleName : null))
                .ForMember(dest => dest.OperationName, opt => opt.MapFrom(src => src.Operation.OperationName))
                .ForMember(dest => dest.DataViewStructureDisplayOn, opt => opt.MapFrom(src => src.DataViewStructure != null ? src.DataViewStructure.DisplayOn : null))
                .ForMember(dest => dest.PageTypeName, opt => opt.MapFrom(src => src.PageType != null ? src.PageType.PageTypeName : null));





            CreateMap<CandidateRequestDTO, Candidate>()
         .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
         .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
         .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
         .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
         .ForMember(dest => dest.Pan, opt => opt.MapFrom(src => src.Pan))
         .ForMember(dest => dest.Aadhaar, opt => opt.MapFrom(src => src.Aadhaar))
         .ForMember(dest => dest.CandidateReferenceCode, opt => opt.MapFrom(src => src.CandidateReferenceCode))
         .ForMember(dest => dest.ResumeUrl, opt => opt.MapFrom(src => src.ResumeUrl))
         .ForMember(dest => dest.ExperienceYears, opt => opt.MapFrom(src => src.ExperienceYears))
         .ForMember(dest => dest.CurrentLocation, opt => opt.MapFrom(src => src.CurrentLocation))
         .ForMember(dest => dest.ExpectedSalary, opt => opt.MapFrom(src => src.ExpectedSalary))
         .ForMember(dest => dest.CurrentCompany, opt => opt.MapFrom(src => src.CurrentCompany))
         .ForMember(dest => dest.NoticePeriod, opt => opt.MapFrom(src => src.NoticePeriod))
         .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
         .ForMember(dest => dest.AppliedDate, opt => opt.MapFrom(src => src.AppliedDate))
         .ForMember(dest => dest.SkillSet, static opt => opt.MapFrom(static src => CleanSkillSet(src.SkillSet)))
         .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
         .ForMember(dest => dest.ActionStatus, opt => opt.MapFrom(src => src.ActionStatus))
         .ForMember(dest => dest.Education, opt => opt.MapFrom(src => src.Education))
         .ForMember(dest => dest.IsFresher, opt => opt.MapFrom(src => src.IsFresher))
         //.ForMember(dest => dest.Resume, opt =>opt.MapFrom(src => src.ResumeUpload != null ? Convert.ToBase64String(src.ResumeUpload) : null))
         .ForMember(dest => dest.ResumePath, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ResumeUpload) ? Convert.FromBase64String(src.ResumeUpload) : Array.Empty<byte>()))

         .ForMember(dest => dest.IsBlacklisted, opt => opt.MapFrom(src => false)) // Default value
         .ForMember(dest => dest.LastUpdatedDateTime, opt => opt.MapFrom(src => DateTime.UtcNow));

        }


    }
}

