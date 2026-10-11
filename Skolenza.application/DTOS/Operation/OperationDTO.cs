using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{
    public class OperationDTO
    {
        public int Id { get; set; }
        public string OperationName { get; set; }
        public string Remark { get; set; }
        public bool IsActive { get; set; }
        public long? AddedById { get; set; }
        public DateTime AddedDateTime { get; set; }
        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }

}
