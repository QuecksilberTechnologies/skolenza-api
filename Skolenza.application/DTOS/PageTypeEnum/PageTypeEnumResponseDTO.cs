using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.PageTypeEnum
{
    public class PageTypeEnumResponseDTO
    {
        public int Id { get; set; }
        public string PageTypeName { get; set; }
    }
    public static class StaticPageTypeData
    {
        public static List<PageTypeEnumResponseDTO> GetSamplePageTypes()
        {
            return new List<PageTypeEnumResponseDTO>
         {
            new PageTypeEnumResponseDTO { Id = 1, PageTypeName = "Master" },
            new PageTypeEnumResponseDTO { Id = 2, PageTypeName = "Transaction" },
            new PageTypeEnumResponseDTO { Id = 3, PageTypeName = "View" },
            new PageTypeEnumResponseDTO { Id = 4, PageTypeName = "Common" },
            new PageTypeEnumResponseDTO { Id = 5, PageTypeName = "Report" }
        };
        }
    }

}
