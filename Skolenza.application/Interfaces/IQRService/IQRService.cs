using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IQRService
{
    public interface IQRService
    {
      //  byte[] GenerateQrCode(string jsonData, int pixelsPerModule = 20);
        Task<string> SaveQrCodeToFileAsync(string jsonData, string filePath, int pixelsPerModule = 20);
    }

}
