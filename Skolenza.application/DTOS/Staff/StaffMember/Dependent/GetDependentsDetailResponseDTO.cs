using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Staff.Dependent
{
    public class GetDependentsDetailResponseDTO
    {
        public int TotalDependents { get; set; }
        public int TotalChilds { get; set; }
        public int TotalSpouses { get; set; }
         public int TotalParents { get; set; }
        public int TotalInLaws { get; set; }
        public List<GetDependentResponseDTO> Dependents { get; set; } = new List<GetDependentResponseDTO>();
    }
}
