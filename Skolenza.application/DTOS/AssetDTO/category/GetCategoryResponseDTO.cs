using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.AssetDTO.category
{
    public class GetCategoryResponseDTO : BaseRequest
    {
        

        public long Id { get; set; }
        public string? CategoryName { get; set; }

        public string? Remark { get; set; }

        public bool IsActive { get; set; }
        public bool HasMultipleUser { get; set; } 

       




    }
}
