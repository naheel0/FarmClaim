using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FarmClaim.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Services
{
    public class CloudinaryStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryStorageService> _logger;

        public CloudinaryStorageService(IConfiguration config, ILogger<CloudinaryStorageService> logger)
        {
            _logger = logger;

            var account = new Account(
                config["Cloudinary:CloudName"] ?? throw new InvalidOperationException("Cloudinary:CloudName not configured."),
                config["Cloudinary:ApiKey"] ?? throw new InvalidOperationException("Cloudinary:ApiKey not configured."),
                config["Cloudinary:ApiSecret"] ?? throw new InvalidOperationException("Cloudinary:ApiSecret not configured."));

            _cloudinary = new Cloudinary(account);
        }

        public async Task<FileUploadResult> UploadAsync(Stream file, string fileName, string folder, CancellationToken ct = default)
        {
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, file),
                Folder = folder,
                PublicId = $"{Guid.NewGuid():N}",
                Overwrite = true,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
                throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");
            }

            _logger.LogInformation("Uploaded {File} to Cloudinary folder {Folder}", fileName, folder);

            return new FileUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                FileName = fileName,
                FileSizeBytes = result.Bytes,
                FileType = extension
            };
        }

        public async Task DeleteAsync(string publicId, CancellationToken ct = default)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
                _logger.LogWarning("Cloudinary delete failed: {Error}", result.Error.Message);
            else
                _logger.LogInformation("Deleted Cloudinary image: {PublicId}", publicId);
        }
    }
}