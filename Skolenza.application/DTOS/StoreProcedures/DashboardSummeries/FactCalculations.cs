// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Fact Calculations data transfer model.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace skolenza.application.DTOS.StoreProcedures.DashboardSummeries
{


    /// <summary>
    /// Represents  StaffMember statistics for the Admin Dashboard.
    /// </summary>
    ///


    public class StaffMemberStatsResponseDTO : BaseRequest
    {
        //  StaffMember Section
        public StaffMemberStats stat { get; set; }
    }

    /// <summary>
    /// Represents  Asset statistics for the Admin Dashboard.
    /// </summary>
    ///
    public class AssetStatResponseDTO
    {
        //  Asset Section
        public AssetStats stats { get; set; }
    }

    /// <summary>
    /// StaffMember-related dashboard statistics.
    /// </summary>
    public class StaffMemberStats
    {
        public int TotalStaffMembers { get; set; }
        public int NewHiresThisMonth { get; set; }
        public int OpenPositions { get; set; }
        public int PendingApprovals { get; set; }
    }


    /// <summary>
    /// StaffMember-related dashboard statistics.
    /// </summary>
    ///
    [Keyless]
    public class StaffMemberCountResponseStatsSp
    {

        public long TotalCount { get; set; }
        public long ActiveCount { get; set; }
        public long InActiveCount { get; set; }
        public int OnLeaveCount { get; set; }
    }

    /// <summary>
    /// Represents the StaffMemberCountRequestStatsSp application component.
    /// </summary>
    public class StaffMemberCountRequestStatsSp
    {

        public required string UserStaffMemberId { get; set; }
    }

    /// <summary>
    /// Asset-related dashboard statistics.
    /// </summary>
    public class AssetStats
    {
        public int TotalAssets { get; set; }
        public int AssignedAssets { get; set; }
        public int AvailableAssets { get; set; }
        public int UnderMaintenance { get; set; }
    }
}
