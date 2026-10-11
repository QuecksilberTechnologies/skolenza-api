using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Contexts
{
     
        public class InstitutionContext
        {
            public long InstitutionId { get; set; }
            public string InstitutionEncryptionKey { get; set; } = string.Empty;
        }
     

}
