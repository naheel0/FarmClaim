using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Claims.Commands.UploadClaimImages;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public static class FileValidationHelper
    {
        private static readonly Dictionary<string, byte[][]> FileSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } },
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private const int MaxImagesPerClaim = 10;
        private const int MaxImagesPerBatch = 5;

        public static async Task ValidateFileAsync(UploadClaimImageFile file, CancellationToken ct = default)
        {
            if (file.Length == 0)
                throw new ValidationException(new List<string> { $"File {file.FileName} is empty" });

            if (!AllowedMimeTypes.Contains(file.ContentType))
                throw new ValidationException(new List<string> { $"File {file.FileName}: only jpg, png, webp allowed" });

            if (file.Length > MaxFileSizeBytes)
                throw new ValidationException(new List<string> { $"File {file.FileName} exceeds 10MB" });

            var extension = Path.GetExtension(file.FileName);
            if (!FileSignatures.ContainsKey(extension))
                throw new ValidationException(new List<string> { $"File {file.FileName}: unsupported file extension" });

            var signatures = FileSignatures[extension];
            var headerBytes = new byte[signatures[0].Length];

            var bytesRead = await file.Content.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length), ct);

            if (bytesRead < headerBytes.Length)
                throw new ValidationException(new List<string> { $"File {file.FileName}: file is too small to be valid" });

            if (file.Content.CanSeek)
                file.Content.Seek(0, SeekOrigin.Begin);

            var isValid = signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));

            if (!isValid)
                throw new ValidationException(new List<string> { $"File {file.FileName}: file content does not match expected format" });
        }

        public static void ValidateBatchLimits(int currentCount, int incomingCount)
        {
            if (incomingCount > MaxImagesPerBatch)
                throw new ValidationException(new List<string> { $"Maximum {MaxImagesPerBatch} images per upload" });

            if (currentCount + incomingCount > MaxImagesPerClaim)
                throw new ValidationException(new List<string> { $"Maximum {MaxImagesPerClaim} images per claim" });
        }

        public static string? ExtractPublicId(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex < 0 || uploadIndex >= segments.Length - 1)
                    return null;

                var publicIdWithExt = string.Join('/', segments.Skip(uploadIndex + 1));
                var dotIndex = publicIdWithExt.LastIndexOf('.');
                return dotIndex > 0 ? publicIdWithExt.Substring(0, dotIndex) : publicIdWithExt;
            }
            catch
            {
                return null;
            }
        }
    }
}
