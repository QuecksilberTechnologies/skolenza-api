using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Location
{
    public class GetCountryOptionRequestDTO:GetOptionRequestDTO
    {
         
    }
    public class GetStateOptionRequestDTO : GetCountryOptionRequestDTO
    {       

        public required int   CountryId { get; set; }

    }

    public class GetDistrictOptionRequestDTO : GetOptionRequestDTO
    {

        public required int StateId { get; set; }

    }

    public class GetLocalityOptionRequestDTO : GetOptionRequestDTO
    {
        public required int DistrictId { get; set; }
    }

    public class GetLocalityTypeOptionRequestDTO : GetOptionRequestDTO
    {
    }
}
