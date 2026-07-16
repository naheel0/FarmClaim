namespace FarmClaim.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadAsync(Stream file, string fileName, string folder, CancellationToken ct = default);
        Task DeleteAsync(string publicId, CancellationToken ct = default);
    }

    public class FileUploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } = string.Empty;
    }
}