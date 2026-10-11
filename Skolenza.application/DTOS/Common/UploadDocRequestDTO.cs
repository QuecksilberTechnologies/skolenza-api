using skolenza.application.Interfaces.IFileStorage;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
    public class UploadDocRequestDTO
    {
        public IFormFile? File { get; set; }
        public string SubFolderName { get; set; } = "";
        public long InstitutionId { get; set; }
        public long StaffMemberId { get; set; }
        public string FilePrefix { get; set; } = "";   // EDU, EXP, GAP, IMG etc.
         public  IFileStorageService? fileStorageService { get; set; }
        public int FileType { get; set; }              // 1 = PDF, 2 = PNG
    }

}
