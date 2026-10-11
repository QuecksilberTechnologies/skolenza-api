using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Module.CommonModule
{
    /// <summary>
    /// Represents the request model used to create a new parent module in the system.
    /// </summary>
    /// <remarks>
    /// This API allows the creation of a new common parent module by providing essential details like
    /// staffMember, institution, and role identifiers, along with display configurations for both web and mobile UI.
    /// </remarks>
    public class CreateCommonModuleRequestDTO
    {
        /// <summary>
        /// Gets or sets the ID of the staffMember who owns or creates the module.
        /// </summary>
        /// <example>101</example>
        public required long StaffMemberId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the institution under which the module is created.
        /// </summary>
        /// <example>2001</example>
        public required long InstitutionId { get; set; }

        /// <summary>
        /// Gets or sets the role ID associated with the module.
        /// </summary>
        /// <example>3</example>
        public required long RoleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        /// <remarks>
        /// The name is required and should not exceed 50 characters.
        /// </remarks>
        /// <example>HR Management</example>
        [MaxLength(50)]
        public required string ModuleName { get; set; }

        /// <summary>
       
        /// Gets or sets the display name of the module as it should appear in the UI.
        /// </summary>
      
        /// <example>Human Resource</example>
        public required string DisplayName { get; set; }

        /// <summary>
       
        /// Gets or sets the path-name of the module.
         /// </summary>
        /// <example>human-resource</example>
       
    
        /// 
        public required string URLPath { get; set; }

        public bool IsModuleDisplayInUI { get; set; } = false;

        /// <summary>
        /// Indicates whether the module is currently active.
        /// </summary>
        /// <example>true</example>
        public required bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the web icon image URL or path for the module.
        /// </summary>
        /// <remarks>Maximum length is 200 characters.</remarks>
        /// <example>/images/modules/hr_icon.png</example>
        [MaxLength(200)]
        public string? ImageIconWeb { get; set; }

        /// <summary>
        /// Gets or sets the mobile icon image URL or path for the module.
        /// </summary>
        /// <remarks>Maximum length is 200 characters.</remarks>
        /// <example>/images/modules/hr_icon_mobile.png</example>
        [MaxLength(200)]
        public string? ImageIconMobile { get; set; }

        /// <summary>
        /// Gets or sets the display order or priority of the module in the menu.
        /// </summary>
        /// <example>1</example>
        public required int ItemPriority { get; set; }

        /// <summary>
        /// Gets or sets any remarks or notes associated with the module.
        /// </summary>
        /// <remarks>Maximum length is 500 characters.</remarks>
        /// <example>This module manages all HR-related functionalities.</example>
        [MaxLength(500)]
        public string? Remark { get; set; }

    }


}
