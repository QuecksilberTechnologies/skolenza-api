// ================================================================
// Purpose : Host-only procurement inventory for physical Institution cards.
// ================================================================

using System.Security.Cryptography;
using System.Text;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostDeviceCmd.Handlers;

public sealed class CreateInstitutionCardMasterCommand(CreateInstitutionCardMasterRequestDTO dto) : IRequest<ApiResponse<InstitutionCardMasterResponseDTO>> { public CreateInstitutionCardMasterRequestDTO DTO { get; } = dto; }
public sealed class UpdateInstitutionCardMasterCommand(UpdateInstitutionCardMasterRequestDTO dto) : IRequest<ApiResponse<InstitutionCardMasterResponseDTO>> { public UpdateInstitutionCardMasterRequestDTO DTO { get; } = dto; }
public sealed class UpdateInstitutionCardMasterStatusCommand(UpdateInstitutionCardMasterStatusRequestDTO dto) : IRequest<ApiResponse<InstitutionCardMasterResponseDTO>> { public UpdateInstitutionCardMasterStatusRequestDTO DTO { get; } = dto; }
public sealed class DeleteInstitutionCardMasterCommand(string id, InstitutionDeviceAccessRequestDTO access) : IRequest<ApiResponse<bool>> { public string Id { get; } = id; public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = access; }
public sealed class GetInstitutionCardMasterByIdQuery(string id, InstitutionDeviceAccessRequestDTO access) : IRequest<ApiResponse<InstitutionCardMasterResponseDTO>> { public string Id { get; } = id; public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = access; }
public sealed class GetInstitutionCardMastersQuery(InstitutionCardMasterFilterRequestDTO filter) : IRequest<ApiResponse<List<InstitutionCardMasterResponseDTO>>> { public InstitutionCardMasterFilterRequestDTO Filter { get; } = filter; }

public abstract class InstitutionCardMasterHandlerBase(IUnitOfWork u, ICommonRequestService c, IIdEncoderService ids, IEncryptionService encryption, ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l)
    : InstitutionDeviceAccessHandlerBase(u, c, ids, l)
{
    protected IEncryptionService Encryption { get; } = encryption;
    /// <summary>Reuses the manual procurement validation for import preview and worker.</summary>
    public static void ValidateImport(InstitutionCardMasterRequestDTO request, bool numberRequired)
    {
        Validate(request, numberRequired);
    }

    /// <summary>Uses the same inventory lookup identity as manual creation.</summary>
    public static string ImportHash(string number, string key)
    {
        return Hash(number, key);
    }

    /// <summary>Applies the existing procurement calculation and field mapping.</summary>
    public static void ApplyImport(InstitutionCardMaster entity, InstitutionCardMasterRequestDTO request)
    {
        Apply(entity, request);
    }
    protected static string Hash(string number, string key) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(number))).ToLowerInvariant();
    protected static void Validate(InstitutionCardMasterRequestDTO d, bool numberRequired)
    {
        if (numberRequired && (string.IsNullOrWhiteSpace(d.CardNumber) || d.CardNumber.Length > 19 || d.CardNumber.Any(x => !char.IsAsciiDigit(x)))) throw new ValidationErrorException("Card number must contain up to 19 digits.");
        if (d.UnitPurchasePriceExcludingTax < 0 || d.CgstAmount < 0 || d.SgstAmount < 0 || d.IgstAmount < 0 || d.ForeignTaxAmount < 0 || d.CustomsDutyAmount < 0 || d.FreightAmount < 0 || string.IsNullOrWhiteSpace(d.PurchaseCurrencyCode) || d.PurchaseCurrencyCode.Trim().Length != 3) throw new ValidationErrorException("Enter valid invoice purchase and tax amounts.");
    }
    protected InstitutionCardMasterResponseDTO Map(InstitutionCardMaster e, InstitutionDeviceAccessScope s)
    {
        var card = Encryption.Decrypt(e.CardNumberEncrypted, s.InstitutionEncryptionKey);
        return new InstitutionCardMasterResponseDTO { Id = EncryptIdentifier(e.Id, s.InstitutionEncryptionKey), InstitutionId = HostInstitutionIdentifierProtector.Encrypt(e.InstitutionId, s.InstitutionEncryptionKey, IdEncoder), MaskedCardNumber = card.Length <= 4 ? "****" : $"****{card[^4..]}", CardReference = e.CardReference, CardStatus = (InstitutionCardStatus)e.CardStatus, PurchaseCurrencyCode = e.PurchaseCurrencyCode, UnitPurchasePriceExcludingTax = e.UnitPurchasePriceExcludingTax, SupplierCountryId = e.SupplierCountryId, SupplierStateId = e.SupplierStateId, PlaceOfSupplyCountryId = e.PlaceOfSupplyCountryId, PlaceOfSupplyStateId = e.PlaceOfSupplyStateId, SupplierName = e.SupplierName, SupplierTaxRegistrationNumber = e.SupplierTaxRegistrationNumber, PurchaseInvoiceNumber = e.PurchaseInvoiceNumber, PurchaseInvoiceDate = e.PurchaseInvoiceDate, TaxTreatment = (InstitutionCardTaxTreatment)e.TaxTreatment, CgstRate = e.CgstRate, CgstAmount = e.CgstAmount, SgstRate = e.SgstRate, SgstAmount = e.SgstAmount, IgstRate = e.IgstRate, IgstAmount = e.IgstAmount, ForeignTaxLabel = e.ForeignTaxLabel, ForeignTaxRate = e.ForeignTaxRate, ForeignTaxAmount = e.ForeignTaxAmount, CustomsDutyAmount = e.CustomsDutyAmount, FreightAmount = e.FreightAmount, LandedCost = e.LandedCost, IsActive = e.IsActive, AddedDateTime = e.AddedDateTime, UpdatedDateTime = e.UpdatedDateTime };
    }
    protected IIdEncoderService IdEncoder { get; } = ids;
    protected static void Apply(InstitutionCardMaster e, InstitutionCardMasterRequestDTO d) { e.CardReference = d.CardReference?.Trim(); e.PurchaseCurrencyCode = d.PurchaseCurrencyCode.Trim().ToUpperInvariant(); e.UnitPurchasePriceExcludingTax = d.UnitPurchasePriceExcludingTax; e.SupplierCountryId = d.SupplierCountryId; e.SupplierStateId = d.SupplierStateId; e.PlaceOfSupplyCountryId = d.PlaceOfSupplyCountryId; e.PlaceOfSupplyStateId = d.PlaceOfSupplyStateId; e.SupplierName = d.SupplierName?.Trim(); e.SupplierTaxRegistrationNumber = d.SupplierTaxRegistrationNumber?.Trim(); e.PurchaseInvoiceNumber = d.PurchaseInvoiceNumber?.Trim(); e.PurchaseInvoiceDate = d.PurchaseInvoiceDate; e.TaxTreatment = (short)d.TaxTreatment; e.CgstRate = d.CgstRate; e.CgstAmount = d.CgstAmount; e.SgstRate = d.SgstRate; e.SgstAmount = d.SgstAmount; e.IgstRate = d.IgstRate; e.IgstAmount = d.IgstAmount; e.ForeignTaxLabel = d.ForeignTaxLabel?.Trim(); e.ForeignTaxRate = d.ForeignTaxRate; e.ForeignTaxAmount = d.ForeignTaxAmount; e.CustomsDutyAmount = d.CustomsDutyAmount; e.FreightAmount = d.FreightAmount; e.LandedCost = d.UnitPurchasePriceExcludingTax + d.CgstAmount + d.SgstAmount + d.IgstAmount + d.ForeignTaxAmount + d.CustomsDutyAmount + d.FreightAmount; e.IsActive = d.IsActive; }
}

public sealed class CreateInstitutionCardMasterCommandHandler(IUnitOfWork u, ICommonRequestService c, IIdEncoderService ids, IEncryptionService e, ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l) : InstitutionCardMasterHandlerBase(u,c,ids,e,l), IRequestHandler<CreateInstitutionCardMasterCommand,ApiResponse<InstitutionCardMasterResponseDTO>>
{ public async Task<ApiResponse<InstitutionCardMasterResponseDTO>> Handle(CreateInstitutionCardMasterCommand r, CancellationToken ct) { var d=r.DTO??throw new ValidationErrorException("Card data is required."); var s=await ResolveHostDeviceAssignmentScopeAsync(d,ct); Validate(d,true); var n=d.CardNumber.Trim(); if(await UnitOfWork.InstitutionCardMasterRepository.CardHashExistsAsync(s.InstitutionId,Hash(n,s.InstitutionEncryptionKey),null,ct))throw new ConflictException("This card already exists in the selected Institution inventory."); var x=new InstitutionCardMaster{InstitutionId=s.InstitutionId,CardNumberEncrypted=Encryption.Encrypt(n,s.InstitutionEncryptionKey),CardNumberLookupHash=Hash(n,s.InstitutionEncryptionKey),CardStatus=(short)InstitutionCardStatus.Available,AddedById=s.ActorId,AddedDateTime=DateTime.UtcNow};Apply(x,d);await UnitOfWork.InstitutionCardMasterRepository.AddAsync(x,ct);await UnitOfWork.SaveChangesAsync(ct);return ApiResponse<InstitutionCardMasterResponseDTO>.Success(Map(x,s),"Institution card inventory item created successfully.");} }
public sealed class UpdateInstitutionCardMasterCommandHandler(IUnitOfWork u,ICommonRequestService c,IIdEncoderService ids,IEncryptionService e,ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l) : InstitutionCardMasterHandlerBase(u,c,ids,e,l),IRequestHandler<UpdateInstitutionCardMasterCommand,ApiResponse<InstitutionCardMasterResponseDTO>>
{ public async Task<ApiResponse<InstitutionCardMasterResponseDTO>> Handle(UpdateInstitutionCardMasterCommand r,CancellationToken ct){var d=r.DTO??throw new ValidationErrorException("Card data is required.");var s=await ResolveHostDeviceAssignmentScopeAsync(d,ct);var x=await UnitOfWork.InstitutionCardMasterRepository.GetForUpdateAsync(s.InstitutionId,DecodeIdentifier(d.Id,s,"InstitutionCardId"),ct)??throw new NotFoundException("Institution card was not found.");if(x.CardStatus==(short)InstitutionCardStatus.Assigned)throw new ValidationErrorException("An assigned card cannot be edited. Unbind it first.");Validate(d,!string.IsNullOrWhiteSpace(d.CardNumber));if(!string.IsNullOrWhiteSpace(d.CardNumber)){var n=d.CardNumber.Trim();if(await UnitOfWork.InstitutionCardMasterRepository.CardHashExistsAsync(s.InstitutionId,Hash(n,s.InstitutionEncryptionKey),x.Id,ct))throw new ConflictException("This card already exists in the selected Institution inventory.");x.CardNumberEncrypted=Encryption.Encrypt(n,s.InstitutionEncryptionKey);x.CardNumberLookupHash=Hash(n,s.InstitutionEncryptionKey);}Apply(x,d);x.UpdatedById=s.ActorId;x.UpdatedDateTime=DateTime.UtcNow;await UnitOfWork.SaveChangesAsync(ct);return ApiResponse<InstitutionCardMasterResponseDTO>.Success(Map(x,s),"Institution card inventory item updated successfully.");} }
public sealed class GetInstitutionCardMasterByIdQueryHandler(IUnitOfWork u,ICommonRequestService c,IIdEncoderService ids,IEncryptionService e,ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l):InstitutionCardMasterHandlerBase(u,c,ids,e,l),IRequestHandler<GetInstitutionCardMasterByIdQuery,ApiResponse<InstitutionCardMasterResponseDTO>>
{public async Task<ApiResponse<InstitutionCardMasterResponseDTO>> Handle(GetInstitutionCardMasterByIdQuery r,CancellationToken ct){var s=await ResolveHostDeviceAssignmentScopeAsync(r.AccessRequest,ct);var x=await UnitOfWork.InstitutionCardMasterRepository.GetByIdAsync(s.InstitutionId,DecodeIdentifier(r.Id,s,"InstitutionCardId"),ct)??throw new NotFoundException("Institution card was not found.");return ApiResponse<InstitutionCardMasterResponseDTO>.Success(Map(x,s));}}
public sealed class GetInstitutionCardMastersQueryHandler(IUnitOfWork u,ICommonRequestService c,IIdEncoderService ids,IEncryptionService e,ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l):InstitutionCardMasterHandlerBase(u,c,ids,e,l),IRequestHandler<GetInstitutionCardMastersQuery,ApiResponse<List<InstitutionCardMasterResponseDTO>>>
{public async Task<ApiResponse<List<InstitutionCardMasterResponseDTO>>> Handle(GetInstitutionCardMastersQuery r,CancellationToken ct){var d=r.Filter??new();var s=await ResolveHostDeviceAssignmentScopeAsync(d,ct);var p=await UnitOfWork.InstitutionCardMasterRepository.GetPagedAsync(s.InstitutionId,d,ct);return ApiResponse<List<InstitutionCardMasterResponseDTO>>.SuccessPaginated(p.Data.Select(x=>Map(x,s)).ToList(),p.PageNumber,p.PageSize,p.TotalCount,(int)Math.Ceiling(p.TotalCount/(double)p.PageSize),"Institution card inventory retrieved successfully.");}}
public sealed class UpdateInstitutionCardMasterStatusCommandHandler(IUnitOfWork u,ICommonRequestService c,IIdEncoderService ids,IEncryptionService e,ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l):InstitutionCardMasterHandlerBase(u,c,ids,e,l),IRequestHandler<UpdateInstitutionCardMasterStatusCommand,ApiResponse<InstitutionCardMasterResponseDTO>>
{public async Task<ApiResponse<InstitutionCardMasterResponseDTO>> Handle(UpdateInstitutionCardMasterStatusCommand r,CancellationToken ct){var d=r.DTO??throw new ValidationErrorException("Card status is required.");var s=await ResolveHostDeviceAssignmentScopeAsync(d,ct);var x=await UnitOfWork.InstitutionCardMasterRepository.GetForUpdateAsync(s.InstitutionId,DecodeIdentifier(d.Id,s,"InstitutionCardId"),ct)??throw new NotFoundException("Institution card was not found.");if(x.CardStatus==(short)InstitutionCardStatus.Assigned&&!d.IsActive)throw new ValidationErrorException("Unbind an assigned card before deactivation.");x.IsActive=d.IsActive;x.UpdatedById=s.ActorId;x.UpdatedDateTime=DateTime.UtcNow;await UnitOfWork.SaveChangesAsync(ct);return ApiResponse<InstitutionCardMasterResponseDTO>.Success(Map(x,s),"Institution card status updated successfully.");}}
public sealed class DeleteInstitutionCardMasterCommandHandler(IUnitOfWork u,ICommonRequestService c,IIdEncoderService ids,IEncryptionService e,ILogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase> l):InstitutionCardMasterHandlerBase(u,c,ids,e,l),IRequestHandler<DeleteInstitutionCardMasterCommand,ApiResponse<bool>>
{public async Task<ApiResponse<bool>> Handle(DeleteInstitutionCardMasterCommand r,CancellationToken ct){var s=await ResolveHostDeviceAssignmentScopeAsync(r.AccessRequest,ct);var x=await UnitOfWork.InstitutionCardMasterRepository.GetForUpdateAsync(s.InstitutionId,DecodeIdentifier(r.Id,s,"InstitutionCardId"),ct)??throw new NotFoundException("Institution card was not found.");if(await UnitOfWork.InstitutionCardMasterRepository.HasActiveEnrollmentAssignmentAsync(s.InstitutionId,x.Id,ct))throw new ValidationErrorException("Unbind the card from active staffMember enrollments before deleting it.");x.IsActive=false;x.IsSoftDeleted=true;x.SoftDeletedById=s.ActorId;x.SoftDeletedDateTime=DateTime.UtcNow;await UnitOfWork.SaveChangesAsync(ct);return ApiResponse<bool>.Success(true,"Institution card inventory item deleted successfully.");}}
