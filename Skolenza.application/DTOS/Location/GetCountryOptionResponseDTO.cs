using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Location
{
    public class GetCountryOptionResponseDTO 
    {
        public int Id { get; set; }

        public string CountryName { get; set; } = null!;
        public string CountryCode { get; set; } = null!;
        public   string STDCode { get; set; } = null!;

        public bool? IsActive { get; set; }
    }
    public class GetStateOptionResponseDTO
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string CountryCode { get; set; } = null!;
        public string StateName { get; set; } = null!;
        public string StateCode { get; set; } = null!;
        public string STDCode { get; set; } = null!;
        public bool? IsActive { get; set; }
    }

    public class GetDistrictOptionResponseDTO
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string CountryCode { get; set; } = null!;
        public string STDCode { get; set; } = null!;
        public string DistrictName { get; set; } = null!;
        public string DistrictCode { get; set; } = null!;

        public bool? IsActive { get; set; }
    }

    public class GetLocalityOptionResponseDTO
    {
        public int Id { get; set; }
        public int DistrictId { get; set; }
        public int StateId { get; set; }
        public int LocalityTypeId { get; set; }
        public string LocalityName { get; set; } = null!;
        public string LocalityCode { get; set; } = null!;
        public string? PostalCode { get; set; }
        public string LocalityTypeName { get; set; } = null!;
        public bool? IsActive { get; set; }
    }

    public class GetLocalityTypeOptionResponseDTO
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = null!;
    }

}
