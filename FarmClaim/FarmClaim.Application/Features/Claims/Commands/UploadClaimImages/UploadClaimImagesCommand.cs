using FarmClaim.Application.Features.Claims.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Claims.Commands.UploadClaimImages
{
    public record UploadClaimImageFile
    {
        public Stream Content { get; init; } = Stream.Null;
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public long Length { get; init; }
    }

    public record UploadClaimImagesCommand(
        Guid ClaimId,
        Guid UserId,
        List<UploadClaimImageFile> Images,
        string? CropType
    ) : IRequest<List<ClaimImageDto>>;
}
