using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.domain.Common
{
    public abstract class BaseEntity
    {
        public bool? IsSoftDeleted { get; set; }
        public bool IsActive { get; set; }
        public long? AddedById { get; set; }
        public DateTime AddedDateTime { get; set; }
        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public long? SoftDeletedById { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }

}
