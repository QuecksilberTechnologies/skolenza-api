using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Category
{
    public class CategoryResponseDTO : BaseRequest
    {
       
        public string Name { get; set; }
        //public string Description { get; set; }
        //public string Code { get; set; }
        public int Depth { get; set; }
        public string Tags { get; set; }
        public bool IsActive { get; set; }

        // Optional: Include child categories if needed
        //public List<CategoryResponseDTO> SubCategories { get; set; }
    }

}
