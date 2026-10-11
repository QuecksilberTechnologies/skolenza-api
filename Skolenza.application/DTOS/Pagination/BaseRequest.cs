using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Pagination
{
   
        public class BaseRequest
        {
        public int PageNumber { get; set; }
        public int PageSize { get; set; } 
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "desc";
        public string? UserStaffMemberId { get; set; } 
 
      

      }

        /// <summary>
        /// Provides paging fields together with the module-operation context
        /// required by institution runtime authorization.
        /// </summary>
        public class PermissionPagedRequestDTO : PermissionRequestDTO
        {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "desc";
        public string? UserStaffMemberId { get; set; }
        }



}
