using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.AssetDTO.type
{
    public class GetTypeResponseDTO : BaseRequest
    {
        public int Id { get; set; }
        public long? InstitutionId { get; set; }
        public long? AssetCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public long? AddedById { get; set; }
        public DateTime? AddedDateTime { get; set; }
        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
       



    }
}
