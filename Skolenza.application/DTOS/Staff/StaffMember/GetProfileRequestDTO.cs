using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff
{
    /// <summary>
    /// post-request get self profile info
    /// </summary>

    public class GetProfileRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        /// <summary> self user login id Required</summary>

        [Required]
        public string SelfLoginId { get; set; }
    }
}
