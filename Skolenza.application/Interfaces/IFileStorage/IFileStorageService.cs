
using skolenza.domain.Entity; using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace skolenza.application.Interfaces.IFileStorage
{
    //public interface IFileStorageService
    //{
    //    Task<string> SaveFileAsync(byte[] fileData, string fileName, string folderPath);
    //    Task<byte[]> GetFileAsync(string filePath);

    //    string GetInstitutionFolderPath(long? institutionId, string subFolder);
    //    string  GetStaffMemberFolderPath(long institutionId, long insideInstitution, string subFolder);
    //    string GenerateFilePath(long? institutionId, string subFolder, string fileName);
    //    string GenerateFullFilePath(string? fullFolderPath, string? fileName);
    //    string GetPublicUrl(string relativePath);
    //    string GetRelativePath(string fullPath);

    //    // ✅ Add these
    //    string GetDefaultImagePath();
    //    string GetDefaultImageUrl();

    //}
    // Application/Interfaces/IFileService.cs
    //public interface IFileService
    //{
    //    Task<FileRecord> UploadAsync(IFormFile file, string institutionEncoded, string staffMemberEncoded, string category, bool isSensitive);
    //    Task<(Stream Stream, string ContentType, string FileName)> GetFileStreamAsync(Guid fileId, ClaimsPrincipal user);
    //    Task<string> GetSignedUrlAsync(Guid fileId, TimeSpan ttl, ClaimsPrincipal user);
    //    Task DeleteAsync(Guid fileId);
       
       
    //}
    public interface IFileStorageService
    {

        //    string GetDefaultImagePath();
        //    string GetDefaultImageUrl();
        Task<string> UploadFileAsync(IFormFile file, string folderPath, string fileName);
        Task<string> UploadMultiFileAsync(MemoryStream memoryStream, string folderPath, string fileName, string contentType, string fileExtension);


        string GetFileUrl(string key, int expiryMinutes = 30);
        Task<bool> DeleteFileAsync(string fileKey);
    }
}
