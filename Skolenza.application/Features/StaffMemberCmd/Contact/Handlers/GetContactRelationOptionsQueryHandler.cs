// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Returns the centralized StaffMember contact relation catalogue.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StaffMemberCmd.Contact.Handlers;

#region Query

/// <summary>
/// Represents the token-authenticated request for StaffMember contact relation options.
/// </summary>
public sealed class GetContactRelationOptionsQuery
    : IRequest<ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>
{
}

#endregion

#region Handler

/// <summary>
/// Handles StaffMember contact relation option requests without a database read.
/// </summary>
public sealed class GetContactRelationOptionsQueryHandler
    : IRequestHandler<
        GetContactRelationOptionsQuery,
        ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>
{
    public Task<ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>> Handle(
        GetContactRelationOptionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ContactRelationOptionResponseDTO> options =
            Enum.GetValues<EmergencyContactRelation>()
                .OrderBy(relation => (int)relation)
                .Select(relation => new ContactRelationOptionResponseDTO
                {
                    Id = (int)relation,
                    Code = relation.ToString().ToUpperInvariant(),
                    Label = relation.ToString()
                })
                .ToList();

        return Task.FromResult(
            ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>.Success(
                options,
                AppConstants.SuccessMessages.ContactRelationOptionsRetrieved));
    }
}

#endregion
