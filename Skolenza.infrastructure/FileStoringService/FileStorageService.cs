using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using skolenza.application.Interfaces.IFileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.infrastructure.FileStoringService
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public FileStorageService(IConfiguration configuration)
        {
            var accessKey = configuration["AWSUploads:AccesskeyID"];
            var secretKey = configuration["AWSUploads:Secretaccesskey"];
            var region = configuration["AWSUploads:Region"];
            _bucketName = configuration["AWSUploads:BucketName"]!;


            _s3Client = new AmazonS3Client(
                accessKey,
                secretKey,
                RegionEndpoint.GetBySystemName(region)
            );

        }
        public async Task<string> UploadFileAsync(IFormFile file, string folderPath, string fileName)
        {
            if (file == null || file.Length == 0)
                throw new Exception("File is empty.");

            var fileExtension = Path.GetExtension(file.FileName);
            // var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = $"{folderPath}/{fileName}{fileExtension}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            return key;
        }
        public string GetFileUrl(string key, int expiryMinutes = 30)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            return _s3Client.GetPreSignedURL(request);
        }


        public async Task<bool> DeleteFileAsync(string fileKey)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                   response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        public async Task<string> UploadMultiFileAsync(  MemoryStream memoryStream, string folderPath, string fileName, string contentType, string fileExtension)
        {
            if (memoryStream == null || memoryStream.Length == 0)
                throw new Exception("File is empty.");

            // ✅ Ensure stream position start se ho
            memoryStream.Position = 0;

            // ✅ Final S3 Key
            var key = $"{folderPath}/{fileName}{fileExtension}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = memoryStream,   // ✅ Direct stream use
                ContentType = contentType     // ✅ Proper content type
            };

            await _s3Client.PutObjectAsync(request);

            return key;
        }
    }
}







