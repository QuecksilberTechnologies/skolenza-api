using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.DocFile
{
    namespace skolenza.application.DTOS.FileUpload
    {
        /// <summary>
        /// Generic DTO for any type of file upload (Image, Document, Video, etc.)
        /// </summary>
        public class UploadFileDTO
        {
            /// <summary>
            /// Institution (Company / Client) identifier.
            /// </summary>
            public  long  InstitutionId { get; set; }    
            
            public required long Id { get; set; }

            /// <summary>
            /// Base64 encoded file string (can be image, pdf, doc, etc.)
            /// </summary>
            public string? FileBase64 { get; set; } 

            /// <summary>
            /// File extension (like png, jpg, pdf, docx, etc.)
            /// </summary>
            public string? FileExtension { get; set; }

            /// <summary>
            /// File category/folder type (e.g. "Assets", "Documents", "Invoices", "QRCodes", etc.)
            /// </summary>
            public int? FileType { get; set; }

        }
    }

}
