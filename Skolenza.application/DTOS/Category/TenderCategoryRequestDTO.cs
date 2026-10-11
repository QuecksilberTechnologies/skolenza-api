using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Category
{
    public class TenderCategoryRequestDTO : BaseRequest
    {
        public long Id { get; set; }
        public int CategoryId { get; set; }


    }
}
