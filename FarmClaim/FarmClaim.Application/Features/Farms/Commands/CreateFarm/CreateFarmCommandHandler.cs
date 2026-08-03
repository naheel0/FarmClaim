using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farms.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Farms.Commands.CreateFarm;

public class CreateFarmCommandHandler : IRequestHandler<CreateFarmCommand, FarmResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateFarmCommandHandler> _logger;

    public CreateFarmCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateFarmCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FarmResponseDto> Handle(CreateFarmCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Creating new farm for user: {UserId}", command.UserId);

        var userExists = await _context.Users
            .AnyAsync(u => u.Id == command.UserId && !u.IsDeleted, ct);
        if (!userExists)
            throw new NotFoundException(nameof(User), command.UserId);

        var farm = new Farm
        {
            UserId = command.UserId,
            Name = command.Request.Name.Trim(),
            AreaInHectares = command.Request.AreaInHectares,
            Address = command.Request.Address?.Trim(),
            CropType = command.Request.CropType?.Trim(),
            // GEO: Persist coordinates required for weather/AI analysis on claims
            Latitude = command.Request.Latitude,
            Longitude = command.Request.Longitude
        };

        await _context.Farms.AddAsync(farm, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Farm created: {FarmId}, Name: {Name}", farm.Id, farm.Name);

        return MapToDto(farm);
    }

    private static FarmResponseDto MapToDto(Farm farm)
    {
        return new FarmResponseDto
        {
            Id = farm.Id,
            UserId = farm.UserId,
            Name = farm.Name,
            AreaInHectares = farm.AreaInHectares,
            Address = farm.Address,
            Latitude = farm.Latitude,
            Longitude = farm.Longitude,
            LocationGeoJson = farm.LocationGeoJson,
            CreatedAt = farm.CreatedAt
        };
    }
}