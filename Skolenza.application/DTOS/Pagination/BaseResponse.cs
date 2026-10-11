using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Pagination
{
   
        public class BaseResponse
       {
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public string? SortBy { get; set; }
            public string? SortOrder { get; set; }="desc";
            public string? SearchKeyword { get; set; }           
            public string Id { get; set; } = string.Empty ;            
           
       }

    

}
